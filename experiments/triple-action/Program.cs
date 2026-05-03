using System;
using System.Collections.Generic;
using System.Linq;
using TripleAction;

string templatesPath = Path.Combine(AppContext.BaseDirectory, "templates.txt");
var templates = Templates.Load(templatesPath);

string templateName = args.Length > 0 ? args[0] : "default";
if (templateName is "--list" or "-l")
{
    Console.WriteLine($"Templates ({templatesPath}):");
    foreach (var kv in templates) Console.WriteLine($"  {kv.Key,-12} {kv.Value.Name}");
    return;
}
if (!templates.TryGetValue(templateName, out var template))
{
    Console.WriteLine($"unknown template '{templateName}'. Use --list to see options.");
    return;
}

Console.WriteLine($"Encounter: {template.Name}");
Console.WriteLine($"  {template.Monster.Name} ({template.Monster.Hp} HP) — {template.Monster.Moves.Count} moves");
Console.WriteLine($"  {template.Player.Name}    ({template.Player.Hp} HP) — {template.Player.Moves.Count} moves");

var rng = new Random();
int playerHp = template.Player.Hp;
int aiHp = template.Monster.Hp;

var playerCarryStun = new bool[3];
var aiCarryStun = new bool[3];
bool readActiveThisTurn = false;

// move-index -> turn last used
var playerLastUsed = new Dictionary<int, int>();
var aiLastUsed = new Dictionary<int, int>();

int turn = 1;

while (playerHp > 0 && aiHp > 0)
{
    Console.WriteLine();
    Console.WriteLine($"=== Turn {turn} ===  You: {playerHp}/{template.Player.Hp}    {template.Monster.Name}: {aiHp}/{template.Monster.Hp}");

    var aiMoves = new Move[3];
    var aiUsedThisTurn = new HashSet<int>();
    for (int i = 0; i < 3; i++)
    {
        if (aiCarryStun[i]) { aiMoves[i] = new Move("skipped", new HashSet<string>()); continue; }
        var available = AvailableIndices(template.Monster.Moves, aiLastUsed, aiUsedThisTurn, turn);
        int idx = available[rng.Next(available.Count)];
        aiMoves[i] = template.Monster.Moves[idx];
        aiUsedThisTurn.Add(idx);
        aiLastUsed[idx] = turn;
    }

    Console.WriteLine($"  [tell] {Tells.For(aiMoves, template.Monster.Name)}");
    if (readActiveThisTurn)
        Console.WriteLine($"  [read] plan: {string.Join("  /  ", aiMoves.Select(m => m.Encoded))}");

    var playerMoves = new Move[3];
    var playerUsedThisTurn = new HashSet<int>();
    for (int i = 0; i < 3; i++)
    {
        if (playerCarryStun[i])
        {
            playerMoves[i] = new Move("skipped", new HashSet<string>());
            Console.WriteLine($"  Slot {i + 1}: Skipped (stunned)");
            continue;
        }
        var (idx, picked) = PromptMove(i + 1, template.Player.Moves, playerLastUsed, playerUsedThisTurn, turn);
        playerMoves[i] = picked;
        playerUsedThisTurn.Add(idx);
        playerLastUsed[idx] = turn;
    }

    Console.WriteLine();
    Console.WriteLine("--- Reveal ---");
    var nextPlayerCarry = new bool[3];
    var nextAiCarry = new bool[3];

    for (int i = 0; i < 3; i++)
    {
        var p = playerMoves[i];
        var a = aiMoves[i];
        Console.WriteLine($"  Slot {i + 1}: you {p.Encoded,-32} | {template.Monster.Name} {a.Encoded}");

        var r = Resolver.Resolve(p, a, rng);
        playerHp = Clamp(playerHp + r.PlayerDelta, template.Player.Hp);
        aiHp = Clamp(aiHp + r.AiDelta, template.Monster.Hp);

        if (r.StunPlayerNext) ApplyStun(i, playerMoves, nextPlayerCarry);
        if (r.StunAiNext) ApplyStun(i, aiMoves, nextAiCarry);

        Console.WriteLine($"           you: {Sign(r.PlayerDelta)} ({playerHp})    {template.Monster.Name}: {Sign(r.AiDelta)} ({aiHp})");
        if (playerHp <= 0 || aiHp <= 0) break;
    }

    readActiveThisTurn = playerMoves.Any(m => m.Base == "read");
    playerCarryStun = nextPlayerCarry;
    aiCarryStun = nextAiCarry;
    turn++;
}

Console.WriteLine();
if (playerHp <= 0 && aiHp <= 0) Console.WriteLine("Draw — both fall.");
else if (aiHp <= 0) Console.WriteLine($"You defeat the {template.Monster.Name}.");
else Console.WriteLine($"The {template.Monster.Name} defeats you.");

static List<int> AvailableIndices(IReadOnlyList<Move> pool, Dictionary<int, int> lastUsed, HashSet<int> usedThisTurn, int currentTurn)
{
    var result = new List<int>();
    for (int i = 0; i < pool.Count; i++)
    {
        var m = pool[i];
        if (m.Mutators.Contains("rare") && usedThisTurn.Contains(i)) continue;
        if (m.Mutators.Contains("mythic") && lastUsed.TryGetValue(i, out int t) && currentTurn - t < 2) continue;
        result.Add(i);
    }
    return result.Count > 0 ? result : Enumerable.Range(0, pool.Count).ToList(); // fallback if everything's on cooldown
}

static (int idx, Move move) PromptMove(int slot, IReadOnlyList<Move> pool, Dictionary<int, int> lastUsed, HashSet<int> usedThisTurn, int currentTurn)
{
    var available = AvailableIndices(pool, lastUsed, usedThisTurn, currentTurn);
    Console.WriteLine($"  Slot {slot}:");
    for (int j = 0; j < available.Count; j++)
        Console.WriteLine($"    {j + 1}) {pool[available[j]].Encoded}");

    while (true)
    {
        Console.Write("    pick: ");
        var key = Console.ReadKey(intercept: true);
        if (char.IsDigit(key.KeyChar))
        {
            int n = key.KeyChar - '0' - 1;
            if (n >= 0 && n < available.Count)
            {
                Console.WriteLine(pool[available[n]].Encoded);
                return (available[n], pool[available[n]]);
            }
        }
        Console.WriteLine();
    }
}

static int Clamp(int hp, int max) => Math.Max(0, Math.Min(max, hp));

static string Sign(int n) => n == 0 ? "  0" : (n > 0 ? $"+{n}" : n.ToString());

static void ApplyStun(int slotJustResolved, Move[] actionsThisTurn, bool[] carryToNext)
{
    int next = slotJustResolved + 1;
    if (next < 3) actionsThisTurn[next] = new Move("skipped", new HashSet<string>());
    else carryToNext[0] = true;
}
