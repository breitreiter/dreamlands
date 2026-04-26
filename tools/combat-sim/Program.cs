using CombatSim;

int trials = 5000;
int seed = 42;

for (int i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--trials" when i + 1 < args.Length: trials = int.Parse(args[++i]); break;
        case "--seed"   when i + 1 < args.Length: seed   = int.Parse(args[++i]); break;
    }
}

Console.WriteLine($"Combat sim — trials={trials}, seed={seed}");
Console.WriteLine($"Locked targets per project/design/combat_pivot.md:");
Console.WriteLine($"  Even-match fatality < 10%");
Console.WriteLine($"  T1/T2 overmatched fatality 40-70%");
Console.WriteLine($"  T3 tourist fatality ~91% (acceptable)");

var profiles = new[]
{
    (Pc: Baselines.EvenMatch(1),   Monster: Baselines.T1),
    (Pc: Baselines.EvenMatch(2),   Monster: Baselines.T2),
    (Pc: Baselines.EvenMatch(3),   Monster: Baselines.T3),
    (Pc: Baselines.Overmatched(1), Monster: Baselines.T1),
    (Pc: Baselines.Overmatched(2), Monster: Baselines.T2),
    (Pc: Baselines.Overmatched(3), Monster: Baselines.T3),
    (Pc: Baselines.Tourist(),      Monster: Baselines.T3),
};

// ── Reference: Sword stances ──
var swordCells = profiles
    .Select(p => Sim.Run(new SwordPolicy(), p.Pc, p.Monster, trials, seed))
    .ToList();
Report.PrintTable("Sword (reference — already validated)", swordCells);

// ── Axe sweep: Block now grants immunity. Vary momentum AC penalty + cap. ──
var axeVariants = new[]
{
    new AxeParams(MomentumDamagePerStack: 1, MomentumAcPenaltyPerStack: 0, MomentumCap: 5),
    new AxeParams(MomentumDamagePerStack: 1, MomentumAcPenaltyPerStack: 1, MomentumCap: 5),
    new AxeParams(MomentumDamagePerStack: 1, MomentumAcPenaltyPerStack: 2, MomentumCap: 5),
    new AxeParams(MomentumDamagePerStack: 1, MomentumAcPenaltyPerStack: 1, MomentumCap: 3),
    new AxeParams(MomentumDamagePerStack: 1, MomentumAcPenaltyPerStack: 1, MomentumCap: 7),
};

foreach (var v in axeVariants)
{
    var cells = profiles
        .Select(p => Sim.Run(new AxePolicy(v), p.Pc, p.Monster, trials, seed))
        .ToList();
    Report.PrintTable($"Axe variant: {v}", cells);
}
