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

var matchups = new SimMatchup[]
{
    new(Baselines.EvenMatch(1),   Baselines.T1),
    new(Baselines.EvenMatch(2),   Baselines.T2),
    new(Baselines.EvenMatch(3),   Baselines.T3),
    new(Baselines.Overmatched(1), Baselines.T1),
    new(Baselines.Overmatched(2), Baselines.T2),
    new(Baselines.Overmatched(3), Baselines.T3),
    new(Baselines.Tourist(),      Baselines.T3),
};

Report.RunOne("Sword (reference — already validated)",
    new SwordPolicy(), matchups, trials, seed);

Report.RunSweep("Axe (locked: 2d4, +1 dmg/-1 AC per stack, immunity Block)",
    new[]
    {
        new AxeParams(MomentumAcPenaltyPerStack: 0, MomentumCap: 5),
        new AxeParams(MomentumAcPenaltyPerStack: 1, MomentumCap: 5),
        new AxeParams(MomentumAcPenaltyPerStack: 2, MomentumCap: 5),
    },
    p => new AxePolicy(p), matchups, trials, seed);
