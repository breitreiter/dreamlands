using Dreamlands.Combat;
using Dreamlands.CombatHarness;
using Dreamlands.Encounter;
using Dreamlands.Rules;

// Smoke-level sweep: a random-strategy PC in random gear against every fight in
// the corpus. This exists to prove the harness drives the live engine end to end;
// the numbers are a floor, not a balance verdict. See plans/rps_combat_harness.md.

int trials = 2000;
int seed = 20260816;
string root = Path.Combine(RepoRoot(), "text", "encounters", "combat");

for (int i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--trials" when i + 1 < args.Length: trials = int.Parse(args[++i]); break;
        case "--seed"   when i + 1 < args.Length: seed   = int.Parse(args[++i]); break;
        case "--root"   when i + 1 < args.Length: root   = args[++i]; break;
        case "-h" or "--help":
            Console.WriteLine("Usage: CombatHarness [--trials N] [--seed N] [--root <combat dir>]");
            return 0;
    }
}

if (!Directory.Exists(root))
{
    Console.Error.WriteLine($"combat directory not found: {root}");
    return 1;
}

var bundle = CombatBundle.LoadDirectory(root);
var balance = BalanceData.Default;
var policy = new RandomPolicy();

// Random gear means "any legal loadout", including the empty ones — no weapon
// leaves the pool with no Attack at all, which is a real (if losing) choice.
var weapons = ItemDef.All.Values.Where(d => d.Type == ItemType.Weapon).Cast<ItemDef?>().Append(null).ToArray();
var armors  = ItemDef.All.Values.Where(d => d.Type == ItemType.Armor).Cast<ItemDef?>().Append(null).ToArray();

Console.WriteLine($"policy={policy.Name}  gear=random  trials={trials}/fight  seed={seed}");
Console.WriteLine($"{weapons.Length} weapon options x {armors.Length} armor options, {bundle.Encounters.Count()} fights\n");
Console.WriteLine($"{"fight",-40}{"hp",4}  {"win",7}{"loss",7}{"stall",7}  {"turns",6}");

foreach (var enc in bundle.Encounters.OrderBy(e => e.Tier ?? 9).ThenBy(e => e.Id, StringComparer.Ordinal))
{
    var tally = new Dictionary<Outcome, int>();
    long turnsTotal = 0;
    int fightSeed = StableHash(enc.Id);

    for (int t = 0; t < trials; t++)
    {
        // One RNG per trial drives gear choice and the fight, so a (seed, trial)
        // pair reproduces exactly. NOT HashCode.Combine: .NET randomises string
        // hashing per process, so that seeds differently on every run.
        var rng = new Random(unchecked(seed * 31 + fightSeed) * 31 + t);
        var weapon = weapons[rng.Next(weapons.Length)];
        var armor  = armors[rng.Next(armors.Length)];

        var r = Runner.Run(enc, weapon, armor, policy, balance, rng);
        tally[r.Outcome] = tally.GetValueOrDefault(r.Outcome) + 1;
        turnsTotal += r.Turns;
    }

    double pct(Outcome o) => 100.0 * tally.GetValueOrDefault(o) / trials;
    Console.WriteLine($"{enc.Id,-40}{enc.Stats.Hp,4}  {pct(Outcome.Won),6:F1}%{pct(Outcome.Lost),6:F1}%" +
                      $"{pct(Outcome.Stalled),6:F1}%  {turnsTotal / (double)trials,6:F1}");
}

return 0;

/// <summary>FNV-1a. Stable across processes and runtimes, unlike string.GetHashCode.</summary>
static int StableHash(string s)
{
    unchecked
    {
        uint h = 2166136261;
        foreach (char c in s) { h ^= c; h *= 16777619; }
        return (int)h;
    }
}

static string RepoRoot()
{
    var dir = AppContext.BaseDirectory;
    while (dir != null && !Directory.Exists(Path.Combine(dir, ".git")))
        dir = Directory.GetParent(dir)?.FullName;
    return dir ?? Directory.GetCurrentDirectory();
}
