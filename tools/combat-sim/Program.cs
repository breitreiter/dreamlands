using CombatSim;
using Dreamlands.Encounter;

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

Report.RunOne("Sword (locked)",
    new SwordPolicy(), matchups, trials, seed);

Report.RunSweep("Axe (locked: 2d4, +1 dmg/-1 AC per stack, immunity Block)",
    new[]
    {
        new AxeParams(MomentumAcPenaltyPerStack: 0, MomentumCap: 5),
        new AxeParams(MomentumAcPenaltyPerStack: 1, MomentumCap: 5),
        new AxeParams(MomentumAcPenaltyPerStack: 2, MomentumCap: 5),
    },
    p => new AxePolicy(p), matchups, trials, seed);

// Dagger (locked): timing-window attack outcomes (no d20), defense via standard
// AC. Nested bands miss / hit / crit / super-crit; super-crit (T3+ only)
// cancels the next monster turn. See project/design/dagger_reflex_minigame.md.
// Sweep skill profiles from newbie to pinball wizard to see how the niche
// scales vs the sword/axe baselines above.
Report.RunSweep("Dagger timing-window (skill-profile sweep, 1d4 base)",
    new[]
    {
        // (HitRate, CritRate, SuperCritRate)
        new TimingDaggerParams(new DiceRoll(1, 4, 0), HitRate: 0.50, CritRate: 0.05, SuperCritRate: 0.00),  // newbie / new pattern
        new TimingDaggerParams(new DiceRoll(1, 4, 0), HitRate: 0.70, CritRate: 0.20, SuperCritRate: 0.00),  // median, T1/T2 dagger (no super)
        new TimingDaggerParams(new DiceRoll(1, 4, 0), HitRate: 0.70, CritRate: 0.20, SuperCritRate: 0.05),  // median, T3 dagger (super unlocked)
        new TimingDaggerParams(new DiceRoll(1, 4, 0), HitRate: 0.85, CritRate: 0.40, SuperCritRate: 0.10),  // skilled, T3
        new TimingDaggerParams(new DiceRoll(1, 4, 0), HitRate: 1.00, CritRate: 0.80, SuperCritRate: 0.40),  // master, T3
        new TimingDaggerParams(new DiceRoll(1, 4, 0), HitRate: 1.00, CritRate: 1.00, SuperCritRate: 1.00),  // pinball wizard ceiling
    },
    p => new TimingDaggerPolicy(p), matchups, trials, seed);
