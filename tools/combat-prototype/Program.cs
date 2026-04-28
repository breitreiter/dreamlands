using CombatPrototype.Cli;
using CombatPrototype.Cmb;
using CombatPrototype.Combat;

int seed = Random.Shared.Next();
string? monsterPath = null;
string? playerPath = null;

for (int i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--seed" when i + 1 < args.Length:
            seed = int.Parse(args[++i]);
            break;
        case "--monster" when i + 1 < args.Length:
            monsterPath = args[++i];
            break;
        case "--player" when i + 1 < args.Length:
            playerPath = args[++i];
            break;
        case "-h" or "--help":
            PrintHelp();
            return 0;
        default:
            if (monsterPath is null && !args[i].StartsWith("--"))
                monsterPath = args[i];
            break;
    }
}

monsterPath ??= Path.Combine(AppContext.BaseDirectory, "Monsters", "gorzog.fight");
playerPath ??= Path.Combine(AppContext.BaseDirectory, "Player.json");

if (!File.Exists(monsterPath))
{
    Console.Error.WriteLine($"Monster file not found: {monsterPath}");
    return 1;
}

var loadout = Loadout.LoadOrDefault(playerPath);
Console.WriteLine($"Combat prototype — seed {seed}, monster {Path.GetFileName(monsterPath)}, player {Path.GetFileName(playerPath)}");

var rng = new Random(seed);
var encounter = CmbParser.ParseFile(monsterPath);
var player = PlayerState.FromLoadout(loadout);
var renderer = new Renderer();
var controller = new CliController();
var runner = new CombatRunner(rng, renderer);

runner.Run(encounter, player, controller);
return 0;

static void PrintHelp()
{
    Console.WriteLine("Usage: combat-prototype [--seed <n>] [--monster <path>] [--player <path>]");
    Console.WriteLine();
    Console.WriteLine("Runs an interactive combat against the given .fight file (default: Monsters/gorzog.fight).");
    Console.WriteLine("Player stats come from Player.json (default: bundled file alongside the binary).");
}
