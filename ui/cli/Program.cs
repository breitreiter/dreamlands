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
    Console.Error.WriteLine("  inn                  Get inn/chapterhouse info at current settlement");
    Console.Error.WriteLine("  inn-recover          Full recovery at inn (costs gold)");
    Console.Error.WriteLine("  chapterhouse         Free recovery at chapterhouse");
    Console.Error.WriteLine("  market               Get market stock");
    Console.Error.WriteLine("  market-order <json>  Submit market buy/sell order");
    Console.Error.WriteLine("  equip <item_id>      Equip item from pack");
    Console.Error.WriteLine("  unequip <slot>       Unequip slot (weapon|armor|boots)");
    Console.Error.WriteLine("  discard <item_id>    Discard item from inventory");
    Console.Error.WriteLine("  inflict <condition>  Debug: add a condition to the player");
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
            // Camp resolution is fully automatic post-refactor: one ration consumed,
            // matching medicine for serious conditions, no player input required.
            result = await client.Action(ResolveGameId(), """{"action":"camp_resolve"}""");
            break;

        case "inn":
            result = await client.GetInn(ResolveGameId());
            break;

        case "inn-book":
        {
            var service = positional.Count > 1 ? positional[1].ToLowerInvariant() : "bed";
            if (service != "bed" && service != "bath" && service != "full")
            {
                Console.Error.WriteLine($"Unknown inn service '{service}'. Use bed | bath | full.");
                return 1;
            }
            var bookJson = $$"""{"action":"inn_book","innService":"{{service}}"}""";
            result = await client.Action(ResolveGameId(), bookJson);
            break;
        }

        case "rest":
            result = await client.Action(ResolveGameId(), """{"action":"inn_book","innService":"bed"}""");
            break;

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

        case "inflict":
        {
            if (positional.Count < 2) { Console.Error.WriteLine("Usage: inflict <condition>"); return 1; }
            result = await client.DebugAddCondition(ResolveGameId(), positional[1]);
            break;
        }

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
