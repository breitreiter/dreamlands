using Dreamlands.CombatHarness;
using Dreamlands.Encounter;
using Dreamlands.Rules;

// Sweeps the three core strategy archetypes (plus random as a floor) across
// tier-coherent loadouts against every fight in the corpus.
//
// The numbers are a LOWER BOUND on optimal play, not optimal play. See
// plans/rps_combat_harness.md §8 for why that caveat is load-bearing.

int trials = 1000;
// Entry spirits. 20 is the cap you leave town with, not what you arrive with:
// fatigue alone costs 1/night untrained and the T2/T3 fights are days out.
int[] spiritsSweep = [20, 15, 10, 5];
int seed = 20260816;
string root = Path.Combine(RepoRoot(), "text", "encounters", "combat");

for (int i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--trials" when i + 1 < args.Length: trials = int.Parse(args[++i]); break;
        case "--seed"   when i + 1 < args.Length: seed   = int.Parse(args[++i]); break;
        case "--root"   when i + 1 < args.Length: root   = args[++i]; break;
        case "--spirits" when i + 1 < args.Length:
            spiritsSweep = args[++i].Split(',').Select(int.Parse).ToArray(); break;
        case "-h" or "--help":
            Console.WriteLine("Usage: CombatHarness [--trials N] [--seed N] [--root <dir>] [--spirits 20,15,10,5]");
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

IPolicy[] policies = [new BerserkPolicy(), new AggroPolicy(), new ControlPolicy(), new TurtlePolicy(), new RandomPolicy()];
var bands = Loadouts.All();

Console.WriteLine($"trials={trials}/cell  seed={seed}  {bundle.Encounters.Count()} fights");
foreach (var (name, kit) in bands) Console.WriteLine($"  {name,-28} {kit.Count,2} loadouts: {string.Join(", ", kit.Select(l => l.Label).Take(3))}{(kit.Count > 3 ? ", ..." : "")}");
Console.WriteLine();

int sweepTrials = Math.Max(50, trials / 4);
var agg = new Dictionary<(string Band, string Policy), double>();
var costAgg = new Dictionary<(string Band, string Policy), double>();
var condAgg = new Dictionary<(string Band, string Policy), double>();
var header = $"{"fight",-34}{"band",-25}" + string.Concat(policies.Select(p => $"{p.Name,9}")) + $"{"best",9}{"spread",8}{"bsrk-mk",9}";
Console.WriteLine(header);
Console.WriteLine(new string('-', header.Length));

foreach (var enc in bundle.Encounters.OrderBy(e => e.Tier ?? 9).ThenBy(e => e.Id, StringComparer.Ordinal))
{
    foreach (var (bandName, kit) in bands)
    {
        var winPct = new double[policies.Length];
        var mutualPct = new double[policies.Length];
        var spiritsLeft = new double[policies.Length];
        var condsGained = new double[policies.Length];
        for (int p = 0; p < policies.Length; p++)
        {
            int wins = 0, mutual = 0, n = 0;
            long spiritsOnWin = 0, condsOnWin = 0;
            foreach (var lo in kit)
            {
                for (int t = 0; t < trials; t++)
                {
                    var rng = new Random(Seed(seed, enc.Id, lo.Label, policies[p].Name, t));
                    var r = Runner.Run(enc, lo.Weapon, lo.Armor, policies[p], balance, rng, spiritsSweep[0]);
                    if (r.Outcome == Outcome.Won) { wins++; spiritsOnWin += r.SpiritsLeft; condsOnWin += r.ConditionsGained; }
                    if (r.Outcome == Outcome.MutualKill) mutual++;
                    n++;
                }
            }
            winPct[p] = 100.0 * wins / n;
            mutualPct[p] = 100.0 * mutual / n;
            spiritsLeft[p] = wins == 0 ? 0 : spiritsOnWin / (double)wins;
            condsGained[p] = wins == 0 ? 0 : condsOnWin / (double)wins;
            costAgg[(bandName, policies[p].Name)] = costAgg.GetValueOrDefault((bandName, policies[p].Name)) + spiritsLeft[p] / bundle.Encounters.Count();
            condAgg[(bandName, policies[p].Name)] = condAgg.GetValueOrDefault((bandName, policies[p].Name)) + condsGained[p] / bundle.Encounters.Count();
        }
        foreach (var (p, w) in policies.Zip(winPct))
            agg[(bandName, p.Name)] = agg.GetValueOrDefault((bandName, p.Name)) + w / bundle.Encounters.Count();

        int bestIdx = Array.IndexOf(winPct, winPct.Max());
        double spread = winPct.Max() - winPct.Min();
        Console.WriteLine($"{enc.Id,-34}{bandName,-25}" +
            string.Concat(winPct.Select(w => $"{w,8:F1}%")) +
            $"{policies[bestIdx].Name,9}{spread,7:F1}%{mutualPct[Array.IndexOf(policies, policies.First(x => x.Name == "berserk"))],9:F1}%");
    }
}

Console.WriteLine();
Console.WriteLine("mean win% across all fights");
Console.WriteLine($"{"band",-25}" + string.Concat(policies.Select(p => $"{p.Name,9}")));
foreach (var (bandName, _) in bands)
    Console.WriteLine($"{bandName,-25}" + string.Concat(policies.Select(p => $"{agg.GetValueOrDefault((bandName, p.Name)),8:F1}%")));

Console.WriteLine();
Console.WriteLine("cost of winning: mean spirits left / conditions gained, on wins only (20 spirits in)");
Console.WriteLine($"{"band",-25}" + string.Concat(policies.Select(p => $"{p.Name,16}")));
foreach (var (bandName, _) in bands)
    Console.WriteLine($"{bandName,-25}" + string.Concat(policies.Select(p =>
        $"{costAgg.GetValueOrDefault((bandName, p.Name)),9:F1}sp{condAgg.GetValueOrDefault((bandName, p.Name)),5:F2}c")));

Console.WriteLine();
Console.WriteLine($"mean win% by entry spirits (all fights x all bands, {sweepTrials} trials/cell)");
Console.WriteLine($"{"spirits",-25}" + string.Concat(policies.Select(p => $"{p.Name,9}")));
foreach (var sp in spiritsSweep)
{
    var means = new double[policies.Length];
    for (int p = 0; p < policies.Length; p++)
    {
        int wins = 0, n = 0;
        foreach (var enc in bundle.Encounters)
        foreach (var (bandName, kit) in bands)
        foreach (var lo in kit)
            for (int t = 0; t < sweepTrials; t++)
            {
                var rng = new Random(Seed(seed, enc.Id, lo.Label, policies[p].Name, sp, t));
                if (Runner.Run(enc, lo.Weapon, lo.Armor, policies[p], balance, rng, sp).Outcome == Outcome.Won) wins++;
                n++;
            }
        means[p] = 100.0 * wins / n;
    }
    Console.WriteLine($"{sp + " spirits",-25}" + string.Concat(means.Select(m => $"{m,8:F1}%")));
}

if (Tell.UnknownCount > 0)
    Console.Error.WriteLine(
        $"WARNING: {Tell.UnknownCount} unrecognised tells — lib/Combat/Tells.cs has likely been reworded. " +
        "The policies are reading nothing and every number above is suspect.");

return 0;

static int Seed(int seed, params object[] parts)
{
    unchecked
    {
        uint h = (uint)seed;
        foreach (var p in parts)
        {
            foreach (char c in p.ToString() ?? "") { h ^= c; h *= 16777619; }
            h ^= 0x9e3779b9;
        }
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
