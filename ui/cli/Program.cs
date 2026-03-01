using System.Diagnostics;
using System.Text.Json;
using DreamlandsCli;

const string DefaultUrl = "http://localhost:5064";

static string FindRepoRoot()
{
    var dir = Directory.GetCurrentDirectory();
    while (dir != null)
    {
        if (File.Exists(Path.Combine(dir, "Dreamlands.sln"))) return dir;
        dir = Path.GetDirectoryName(dir);
    }
    return Directory.GetCurrentDirectory();
}

// ── Parse global options ──

string? urlOverride = null;
string? gameIdOverride = null;
var positional = new List<string>();

for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--url" && i + 1 < args.Length) { urlOverride = args[++i]; }
    else if (args[i] == "--game-id" && i + 1 < args.Length) { gameIdOverride = args[++i]; }
    else { positional.Add(args[i]); }
}

if (positional.Count == 0)
{
    Console.Error.WriteLine("Usage: dreamlands-cli [--url <url>] [--game-id <id>] <command> [args...]");
    Console.Error.WriteLine();
    Console.Error.WriteLine("Commands:");
    Console.Error.WriteLine("  new                  Create a new game");
    Console.Error.WriteLine("  status               Get current game state");
    Console.Error.WriteLine("  move <direction>     Move (north/south/east/west)");
    Console.Error.WriteLine("  choose <index>       Choose an encounter option");
    Console.Error.WriteLine("  enter-dungeon        Enter dungeon at current location");
    Console.Error.WriteLine("  end-encounter        End current encounter");
    Console.Error.WriteLine("  end-dungeon          End dungeon (after completion/flee)");
    Console.Error.WriteLine("  camp                 Resolve end-of-day (auto-selects food+medicine)");
    Console.Error.WriteLine("  market-order <json>   Submit market buy/sell order");
    Console.Error.WriteLine("  market               Get market stock");
    return 1;
}

var command = positional[0];

// ── Resolve session ──

string ResolveUrl() => urlOverride ?? Session.Load()?.Url ?? DefaultUrl;

string ResolveGameId()
{
    if (gameIdOverride != null) return gameIdOverride;
    var session = Session.Load();
    if (session?.GameId != null) return session.GameId;
    Console.Error.WriteLine("Error: No active session. Run 'new' first or pass --game-id.");
    Environment.Exit(1);
    return ""; // unreachable
}

// ── Server auto-start ──

async Task EnsureServer(GameClient client)
{
    if (await client.IsReachable()) return;

    Console.Error.WriteLine("Server not reachable. Starting...");
    var repoRoot = FindRepoRoot();
    var serverProject = Path.Combine(repoRoot, "server", "GameServer");
    var url = ResolveUrl();

    var psi = new ProcessStartInfo
    {
        FileName = "dotnet",
        Arguments = $"run --project \"{serverProject}\" --urls {url}",
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
    };
    Process.Start(psi);

    for (int attempt = 0; attempt < 20; attempt++)
    {
        await Task.Delay(500);
        if (await client.IsReachable())
        {
            Console.Error.WriteLine("Server started.");
            return;
        }
    }

    Console.Error.WriteLine("Error: Server failed to start within 10 seconds.");
    Environment.Exit(1);
}

// ── Dispatch ──

try
{
    var url = ResolveUrl();
    var client = new GameClient(url);
    await EnsureServer(client);

    string result;
    switch (command)
    {
        case "new":
        {
            result = await client.NewGame();
            // Extract gameId from response and save session
            using var doc = JsonDocument.Parse(result);
            var gameId = doc.RootElement.GetProperty("gameId").GetString()!;
            Session.Save(new SessionData(gameId, url));
            Console.Error.WriteLine($"Game created: {gameId}");
            break;
        }

        case "status":
            result = await client.GetState(ResolveGameId());
            break;

        case "move":
        {
            if (positional.Count < 2) { Console.Error.WriteLine("Usage: move <direction>"); return 1; }
            var actionJson = JsonSerializer.Serialize(new { action = "move", direction = positional[1] });
            result = await client.Action(ResolveGameId(), actionJson);
            break;
        }

        case "choose":
        {
            if (positional.Count < 2 || !int.TryParse(positional[1], out var idx))
            {
                Console.Error.WriteLine("Usage: choose <index>");
                return 1;
            }
            var actionJson = JsonSerializer.Serialize(new { action = "choose", choiceIndex = idx });
            result = await client.Action(ResolveGameId(), actionJson);
            break;
        }

        case "enter-dungeon":
            result = await client.Action(ResolveGameId(), """{"action":"enter_dungeon"}""");
            break;

        case "end-encounter":
            result = await client.Action(ResolveGameId(), """{"action":"end_encounter"}""");
            break;

        case "end-dungeon":
            result = await client.Action(ResolveGameId(), """{"action":"end_dungeon"}""");
            break;

        case "camp":
        {
            // Auto-select: all food + all medicine from haversack
            // First get current state to read inventory
            var stateJson = await client.GetState(ResolveGameId());
            using var stateDoc = JsonDocument.Parse(stateJson);

            var food = new List<string>();
            var medicine = new List<string>();

            if (stateDoc.RootElement.TryGetProperty("inventory", out var inv)
                && inv.TryGetProperty("haversack", out var hs))
            {
                foreach (var item in hs.EnumerateArray())
                {
                    var defId = item.GetProperty("defId").GetString() ?? "";
                    if (defId.StartsWith("food_"))
                        food.Add(defId);
                    else if (!string.IsNullOrEmpty(defId))
                        medicine.Add(defId);
                }
            }

            // Take up to 3 food (try for balanced: 1 protein, 1 grain, 1 sweet)
            var selectedFood = new List<string>();
            var foodByType = food.GroupBy(f => f).ToDictionary(g => g.Key, g => g.Count());
            foreach (var type in new[] { "food_protein", "food_grain", "food_sweets" })
            {
                if (selectedFood.Count >= 3) break;
                if (foodByType.ContainsKey(type) && foodByType[type] > 0)
                {
                    selectedFood.Add(type);
                    foodByType[type]--;
                }
            }
            // Fill remaining slots
            foreach (var (type, count) in foodByType)
            {
                for (int j = 0; j < count && selectedFood.Count < 3; j++)
                    selectedFood.Add(type);
            }

            // Select medicine items that have cure properties (non-food consumables)
            var selectedMedicine = medicine.Where(m => !m.StartsWith("food_")).Take(5).ToList();

            var campJson = JsonSerializer.Serialize(new
            {
                action = "camp_resolve",
                campChoices = new { food = selectedFood, medicine = selectedMedicine },
            });
            result = await client.Action(ResolveGameId(), campJson);
            break;
        }

        case "market-order":
        {
            if (positional.Count < 2) { Console.Error.WriteLine("Usage: market-order <json>"); return 1; }
            var orderJson = positional[1];
            var actionJson = $$"""{"action":"market_order","order":{{orderJson}}}""";
            result = await client.Action(ResolveGameId(), actionJson);
            break;
        }

        case "equip":
        {
            if (positional.Count < 2) { Console.Error.WriteLine("Usage: equip <item_id>"); return 1; }
            var actionJson = JsonSerializer.Serialize(new { action = "equip", itemId = positional[1] });
            result = await client.Action(ResolveGameId(), actionJson);
            break;
        }

        case "unequip":
        {
            if (positional.Count < 2) { Console.Error.WriteLine("Usage: unequip <slot> (weapon|armor|boots)"); return 1; }
            var actionJson = JsonSerializer.Serialize(new { action = "unequip", slot = positional[1] });
            result = await client.Action(ResolveGameId(), actionJson);
            break;
        }

        case "discard":
        {
            if (positional.Count < 2) { Console.Error.WriteLine("Usage: discard <item_id>"); return 1; }
            var actionJson = JsonSerializer.Serialize(new { action = "discard", itemId = positional[1] });
            result = await client.Action(ResolveGameId(), actionJson);
            break;
        }

        case "market":
            result = await client.GetMarket(ResolveGameId());
            break;

        default:
            Console.Error.WriteLine($"Unknown command: {command}");
            return 1;
    }

    Console.WriteLine(result);
    return 0;
}
catch (GameClientException ex)
{
    Console.Error.WriteLine($"Server error ({ex.StatusCode})");
    Console.WriteLine(ex.Body);
    return 1;
}
catch (HttpRequestException ex)
{
    Console.Error.WriteLine($"Connection error: {ex.Message}");
    return 1;
}
