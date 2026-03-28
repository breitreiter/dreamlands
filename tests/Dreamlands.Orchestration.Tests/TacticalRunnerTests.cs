using Dreamlands.Game;
using Dreamlands.Orchestration;
using Dreamlands.Rules;
using Dreamlands.Tactical;
using Xunit;

namespace Dreamlands.Orchestration.Tests;

public class TacticalRunnerTests
{
    static readonly BalanceData Balance = BalanceData.Default;

    static TacticalEncounter MakeCombat(
        int resistance = 8, int momentum = 3, int timerDraw = 1,
        List<TimerDef>? timers = null, List<OpeningDef>? openings = null,
        List<ApproachDef>? approaches = null, FailureOutcome? failure = null)
    {
        return new TacticalEncounter
        {
            Id = "test/combat",
            Category = "test",
            Title = "Test Combat",
            Body = "Test.",
            Variant = Variant.Combat,
            Resistance = resistance,
            Momentum = momentum,
            TimerDraw = timerDraw,
            Timers = timers ?? [new TimerDef("Threat", TimerEffect.Spirits, 1, 4)],
            Openings = openings ?? [
                new OpeningDef("Strike", new OpeningCost(CostKind.Momentum, 2), new OpeningEffect(EffectKind.Damage, 3)),
                new OpeningDef("Guard", new OpeningCost(CostKind.Free), new OpeningEffect(EffectKind.Momentum, 2)),
                new OpeningDef("Break", new OpeningCost(CostKind.Tick), new OpeningEffect(EffectKind.StopTimer)),
            ],
            Approaches = approaches ?? [
                new ApproachDef(ApproachKind.Scout, 0, 1, 3),
                new ApproachDef(ApproachKind.Direct, 3, 1),
                new ApproachDef(ApproachKind.Wild, 6, 1),
            ],
            Failure = failure ?? new FailureOutcome("You lose.", ["damage_spirits 2"]),
        };
    }

    static TacticalEncounter MakeTraverse(
        int resistance = 6, int queueDepth = 4, int timerDraw = 1)
    {
        return new TacticalEncounter
        {
            Id = "test/traverse",
            Category = "test",
            Title = "Test Traverse",
            Body = "Test.",
            Variant = Variant.Traverse,
            Resistance = resistance,
            QueueDepth = queueDepth,
            TimerDraw = timerDraw,
            Timers = [new TimerDef("Hazard", TimerEffect.Spirits, 1, 3)],
            Openings = [
                new OpeningDef("Step", new OpeningCost(CostKind.Free), new OpeningEffect(EffectKind.Damage, 1)),
                new OpeningDef("Push", new OpeningCost(CostKind.Momentum, 1), new OpeningEffect(EffectKind.Damage, 2)),
            ],
            Failure = new FailureOutcome("You fall.", ["damage_spirits 1"]),
        };
    }

    static (GameSession Session, TacticalState State) MakeContext()
    {
        var session = Helpers.MakeSession();
        var state = new TacticalState();
        return (session, state);
    }

    // ── Begin ──────────────────────────────────────────────────────

    [Fact]
    public void CombatBeginReturnsChooseApproach()
    {
        var (session, state) = MakeContext();
        var enc = MakeCombat();
        var step = TacticalRunner.Begin(session, enc, state);

        Assert.IsType<TacticalStep.ChooseApproach>(step);
        var ca = (TacticalStep.ChooseApproach)step;
        Assert.Equal(3, ca.Approaches.Count);
    }

    [Fact]
    public void TraverseBeginReturnsShowTurn()
    {
        var (session, state) = MakeContext();
        var enc = MakeTraverse();
        var step = TacticalRunner.Begin(session, enc, state);

        Assert.IsType<TacticalStep.ShowTurn>(step);
        var st = (TacticalStep.ShowTurn)step;
        Assert.Equal(6, st.Data.Resistance);
        Assert.Equal(0, st.Data.Momentum);
        Assert.NotNull(st.Data.Queue);
        Assert.Equal(4, st.Data.Queue.Count);
    }

    // ── Approach ───────────────────────────────────────────────────

    [Fact]
    public void ApplyApproachSetsState()
    {
        var (session, state) = MakeContext();
        var enc = MakeCombat();
        TacticalRunner.Begin(session, enc, state);

        var step = TacticalRunner.ApplyApproach(session, enc, state, ApproachKind.Direct);

        Assert.IsType<TacticalStep.ShowTurn>(step);
        var st = (TacticalStep.ShowTurn)step;
        Assert.Equal(3, st.Data.Momentum);
        Assert.Single(st.Data.Openings); // no bonus for direct
    }

    [Fact]
    public void ScoutApproachGivesBonusOpenings()
    {
        var (session, state) = MakeContext();
        var enc = MakeCombat();
        TacticalRunner.Begin(session, enc, state);

        var step = TacticalRunner.ApplyApproach(session, enc, state, ApproachKind.Scout);

        Assert.IsType<TacticalStep.ShowTurn>(step);
        var st = (TacticalStep.ShowTurn)step;
        Assert.Equal(0, st.Data.Momentum);
        Assert.Equal(3, st.Data.Openings.Count); // scout bonus
    }

    // ── Take Opening ───────────────────────────────────────────────

    [Fact]
    public void TakeOpeningDamagesResistance()
    {
        var (session, state) = MakeContext();
        var enc = MakeCombat(resistance: 10, approaches: []);
        TacticalRunner.Begin(session, enc, state);

        // Find a damage opening
        var dmgIdx = state.Openings.FindIndex(o => o.EffectKind == EffectKind.Damage);
        if (dmgIdx < 0)
        {
            // Force a damage opening into the slot
            state.Openings[0] = state.Pool.First(p => p.EffectKind == EffectKind.Damage);
            dmgIdx = 0;
        }

        var opening = state.Openings[dmgIdx];
        state.Momentum = 10; // ensure we can afford it
        var step = TacticalRunner.Act(session, enc, state, TacticalAction.TakeOpening, dmgIdx);

        // Resistance should have decreased
        Assert.True(state.Resistance < 10);
    }

    [Fact]
    public void ResistanceZeroEndsWithResistanceKill()
    {
        var (session, state) = MakeContext();
        // Very low resistance, big damage opening
        var enc = MakeCombat(
            resistance: 1,
            approaches: [],
            openings: [new OpeningDef("Kill", new OpeningCost(CostKind.Free), new OpeningEffect(EffectKind.Damage, 5))]);
        TacticalRunner.Begin(session, enc, state);

        var step = TacticalRunner.Act(session, enc, state, TacticalAction.TakeOpening, 0);
        Assert.IsType<TacticalStep.Finished>(step);
        Assert.Equal(TacticalFinishReason.ResistanceKill, ((TacticalStep.Finished)step).Reason);
    }

    [Fact]
    public void AllTimersStoppedEndsWithControlKill()
    {
        var (session, state) = MakeContext();
        var enc = MakeCombat(
            resistance: 100,
            timerDraw: 1,
            approaches: [],
            openings: [new OpeningDef("Stop", new OpeningCost(CostKind.Free), new OpeningEffect(EffectKind.StopTimer))]);
        TacticalRunner.Begin(session, enc, state);

        var step = TacticalRunner.Act(session, enc, state, TacticalAction.TakeOpening, 0);
        Assert.IsType<TacticalStep.Finished>(step);
        Assert.Equal(TacticalFinishReason.ControlKill, ((TacticalStep.Finished)step).Reason);
    }

    // ── Press / Force ──────────────────────────────────────────────

    [Fact]
    public void PressAdvantageGrantsBonusOpenings()
    {
        var (session, state) = MakeContext();
        var enc = MakeCombat(approaches: []);
        TacticalRunner.Begin(session, enc, state);
        state.Momentum = 5;

        var step = TacticalRunner.Act(session, enc, state, TacticalAction.PressAdvantage);
        Assert.IsType<TacticalStep.ShowTurn>(step);
        var turn = (TacticalStep.ShowTurn)step;
        Assert.Equal(3, turn.Data.Openings.Count);
        Assert.Equal(4, state.Momentum); // 5 - 2 cost + 1 passive from AdvanceTurn
    }

    [Fact]
    public void ForceOpeningDrainsSpirits()
    {
        var (session, state) = MakeContext();
        var enc = MakeCombat(approaches: []);
        TacticalRunner.Begin(session, enc, state);
        var spiritsBefore = session.Player.Spirits;

        var step = TacticalRunner.Act(session, enc, state, TacticalAction.ForceOpening);
        Assert.IsType<TacticalStep.ShowTurn>(step);
        Assert.Equal(spiritsBefore - 2, session.Player.Spirits);
        Assert.Equal(3, ((TacticalStep.ShowTurn)step).Data.Openings.Count);
    }

    [Fact]
    public void PressAdvantageFailsWithoutMomentum()
    {
        var (session, state) = MakeContext();
        var enc = MakeCombat(approaches: []);
        TacticalRunner.Begin(session, enc, state);
        state.Momentum = 0;

        Assert.Throws<InvalidOperationException>(() =>
            TacticalRunner.Act(session, enc, state, TacticalAction.PressAdvantage));
    }

    // ── Timers ─────────────────────────────────────────────────────

    [Fact]
    public void TimerFiresDrainsSpirits()
    {
        var (session, state) = MakeContext();
        // Timer with countdown 1 fires immediately on first advance
        var enc = MakeCombat(
            timerDraw: 1,
            timers: [new TimerDef("Fast", TimerEffect.Spirits, 3, 1)],
            approaches: []);
        TacticalRunner.Begin(session, enc, state);
        var spiritsBefore = session.Player.Spirits;

        // Take a free opening to advance the turn
        state.Openings[0] = state.Pool.First(p => p.CostKind == CostKind.Free);
        TacticalRunner.Act(session, enc, state, TacticalAction.TakeOpening, 0);

        Assert.Equal(spiritsBefore - 3, session.Player.Spirits);
    }

    [Fact]
    public void SpiritsDrainToZeroTriggersFailure()
    {
        var (session, state) = MakeContext();
        session.Player.Spirits = 1; // barely alive
        var enc = MakeCombat(
            timerDraw: 1,
            timers: [new TimerDef("Lethal", TimerEffect.Spirits, 5, 1)],
            approaches: [],
            failure: new FailureOutcome("Dead.", ["damage_spirits 1"]));
        TacticalRunner.Begin(session, enc, state);

        state.Openings[0] = state.Pool.First(p => p.CostKind == CostKind.Free);
        var step = TacticalRunner.Act(session, enc, state, TacticalAction.TakeOpening, 0);

        Assert.IsType<TacticalStep.Finished>(step);
        var fin = (TacticalStep.Finished)step;
        Assert.Equal(TacticalFinishReason.SpiritsLoss, fin.Reason);
        Assert.NotNull(fin.FailureResults);
    }

    // ── Traverse queue ─────────────────────────────────────────────

    [Fact]
    public void TraverseQueueAdvancesOnTakeOpening()
    {
        var (session, state) = MakeContext();
        var enc = MakeTraverse(resistance: 100, queueDepth: 4);
        TacticalRunner.Begin(session, enc, state);

        Assert.NotNull(state.Queue);
        var firstInQueue = state.Queue[0].Name;
        var queueCount = state.Queue.Count;

        // The current opening should be the front of the queue
        Assert.Equal(firstInQueue, state.Openings[0].Name);

        state.Momentum = 10; // ensure we can afford any opening
        TacticalRunner.Act(session, enc, state, TacticalAction.TakeOpening, 0);

        // Queue should still be at target depth (replenished)
        Assert.Equal(queueCount, state.Queue.Count);
    }

    // ── Opening pool filtering ─────────────────────────────────────

    [Fact]
    public void GatedOpeningExcludedWithoutItem()
    {
        var (session, state) = MakeContext();
        var enc = MakeCombat(
            approaches: [],
            openings: [
                new OpeningDef("Basic", new OpeningCost(CostKind.Free), new OpeningEffect(EffectKind.Damage, 1)),
                new OpeningDef("Gated", new OpeningCost(CostKind.Free), new OpeningEffect(EffectKind.Damage, 5), "has bear_trap"),
            ]);
        TacticalRunner.Begin(session, enc, state);

        // Player doesn't have bear_trap, so pool should only have Basic
        Assert.Single(state.Pool);
        Assert.Equal("Basic", state.Pool[0].Name);
    }

    // ── Momentum passive gain ──────────────────────────────────────

    [Fact]
    public void MomentumIncreasesEachTurn()
    {
        var (session, state) = MakeContext();
        // Only damage openings — no momentum effect to confuse the math
        var enc = MakeCombat(
            resistance: 100,
            approaches: [],
            openings: [new OpeningDef("Hit", new OpeningCost(CostKind.Free), new OpeningEffect(EffectKind.Damage, 1))]);
        TacticalRunner.Begin(session, enc, state);
        var momBefore = state.Momentum;

        TacticalRunner.Act(session, enc, state, TacticalAction.TakeOpening, 0);

        // +1 passive from AdvanceTurn (free cost, damage effect = no other momentum changes)
        Assert.Equal(momBefore + 1, state.Momentum);
    }
}
