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
            Stat = "combat",
            Resistance = resistance,
            Momentum = momentum,
            TimerDraw = timerDraw,
            Timers = timers ?? [new TimerDef("Threat", TimerEffect.Spirits, 1, 4, "Stop Threat")],
            Openings = openings ?? [
                new OpeningDef("Strike", new OpeningCost(CostKind.Momentum, 2), new OpeningEffect(EffectKind.Damage, 3)),
                new OpeningDef("Guard", new OpeningCost(CostKind.Free), new OpeningEffect(EffectKind.Momentum, 2)),
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
        int resistance = 6, int timerDraw = 1)
    {
        return new TacticalEncounter
        {
            Id = "test/traverse",
            Category = "test",
            Title = "Test Traverse",
            Body = "Test.",
            Variant = Variant.Traverse,
            Stat = "bushcraft",
            Resistance = resistance,
            QueueDepth = 4,
            TimerDraw = timerDraw,
            Timers = [new TimerDef("Hazard", TimerEffect.Spirits, 1, 3, "Avoid Hazard")],
            Openings = [
                new OpeningDef("Step", new OpeningCost(CostKind.Free), new OpeningEffect(EffectKind.Damage, 1)),
                new OpeningDef("Push", new OpeningCost(CostKind.Momentum, 1), new OpeningEffect(EffectKind.Damage, 2)),
            ],
            Path = [
                new OpeningDef("Step", new OpeningCost(CostKind.Free), new OpeningEffect(EffectKind.Damage, 1)),
                new OpeningDef("Push", new OpeningCost(CostKind.Momentum, 1), new OpeningEffect(EffectKind.Damage, 2)),
                new OpeningDef("Step", new OpeningCost(CostKind.Free), new OpeningEffect(EffectKind.Damage, 1)),
                new OpeningDef("Step", new OpeningCost(CostKind.Free), new OpeningEffect(EffectKind.Damage, 1)),
            ],
            Failure = new FailureOutcome("You fall.", ["damage_spirits 1"]),
        };
    }

    static (GameSession Session, TacticalState State) MakeContext()
    {
        var session = Helpers.MakeSession();
        // Give the player a weapon so they have some collection cards
        session.Player.Equipment.Weapon = new ItemInstance("falchion", "Falchion");
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
    }

    [Fact]
    public void TraverseQueuePopulatedFromPath()
    {
        var (session, state) = MakeContext();
        var enc = MakeTraverse();
        TacticalRunner.Begin(session, enc, state);

        Assert.NotNull(state.Queue);
        Assert.Equal(4, state.Queue.Count);
        Assert.Equal("Step", state.Queue[0].Name);
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
        Assert.Single(st.Data.Openings);
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
        Assert.Equal(3, st.Data.Openings.Count);
    }

    // ── Deck ───────────────────────────────────────────────────────

    [Fact]
    public void DeckIsBuiltWithCorrectSize()
    {
        var (session, state) = MakeContext();
        var enc = MakeCombat(approaches: []);
        TacticalRunner.Begin(session, enc, state);

        Assert.Equal(Balance.Tactical.DeckSize, state.Deck.Count);
    }

    [Fact]
    public void DeckContainsCollectionCards()
    {
        var (session, state) = MakeContext();
        // Falchion contributes collection cards
        var enc = MakeCombat(approaches: []);
        TacticalRunner.Begin(session, enc, state);

        Assert.Contains(state.Deck, c => c.Name == "Hack at their defense");
        Assert.Contains(state.Deck, c => c.Name == "Swing your blade in an arcing chop");
    }

    [Fact]
    public void DeckDrawAdvancesIndex()
    {
        var (session, state) = MakeContext();
        var enc = MakeCombat(approaches: []);
        TacticalRunner.Begin(session, enc, state);

        var idx0 = state.DrawIndex;
        Assert.Equal(1, idx0); // StartTurn drew 1 card
    }

    [Fact]
    public void DeckReshufflesWhenExhausted()
    {
        var (session, state) = MakeContext();
        var enc = MakeCombat(approaches: []);
        TacticalRunner.Begin(session, enc, state);

        // Draw until exhausted
        var deckSize = state.Deck.Count;
        for (int i = state.DrawIndex; i < deckSize + 2; i++)
            DeckBuilder.Draw(state, session.Rng);

        // DrawIndex should have reset (reshuffled)
        Assert.True(state.DrawIndex <= 2);
    }

    // ── Take Opening ───────────────────────────────────

    [Fact]
    public void TakeOpeningDamagesResistance()
    {
        var (session, state) = MakeContext();
        var enc = MakeCombat(resistance: 10, approaches: []);
        TacticalRunner.Begin(session, enc, state);

        // Force a known damage opening
        state.Openings[0] = new OpeningSnapshot
        {
            Name = "Test Hit",
            CostKind = CostKind.Free,
            EffectKind = EffectKind.Damage,
            EffectAmount = 3,
        };

        TacticalRunner.Act(session, enc, state, TacticalAction.TakeOpening, 0);
        Assert.True(state.Resistance < 10);
    }

    [Fact]
    public void ResistanceZeroEndsWithResistanceKill()
    {
        var (session, state) = MakeContext();
        var enc = MakeCombat(resistance: 1, approaches: []);
        TacticalRunner.Begin(session, enc, state);

        state.Openings[0] = new OpeningSnapshot
        {
            Name = "Kill Shot",
            CostKind = CostKind.Free,
            EffectKind = EffectKind.Damage,
            EffectAmount = 5,
        };

        var step = TacticalRunner.Act(session, enc, state, TacticalAction.TakeOpening, 0);
        Assert.IsType<TacticalStep.Finished>(step);
        Assert.Equal(TacticalFinishReason.ResistanceKill, ((TacticalStep.Finished)step).Reason);
    }

    [Fact]
    public void AllTimersStoppedEndsWithControlKill()
    {
        var (session, state) = MakeContext();
        var enc = MakeCombat(resistance: 100, timerDraw: 1, approaches: []);
        TacticalRunner.Begin(session, enc, state);

        state.Openings[0] = new OpeningSnapshot
        {
            Name = "Counter",
            CostKind = CostKind.Free,
            EffectKind = EffectKind.StopTimer,
            StopsTimerIndex = 0,
        };

        var step = TacticalRunner.Act(session, enc, state, TacticalAction.TakeOpening, 0);
        Assert.IsType<TacticalStep.Finished>(step);
        Assert.Equal(TacticalFinishReason.ControlKill, ((TacticalStep.Finished)step).Reason);
    }

    // ── Press / Force ──────────────────────────────────

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

    // ── Timers ─────────────────────────────────────────

    [Fact]
    public void SpiritsDrainToZeroTriggersFailure()
    {
        var (session, state) = MakeContext();
        session.Player.Spirits = 1;
        var enc = MakeCombat(
            timerDraw: 1,
            timers: [new TimerDef("Lethal", TimerEffect.Spirits, 5, 1, "Stop Lethal")],
            approaches: [],
            failure: new FailureOutcome("Dead.", ["damage_spirits 1"]));
        TacticalRunner.Begin(session, enc, state);

        state.Openings[0] = new OpeningSnapshot
        {
            Name = "Stall",
            CostKind = CostKind.Free,
            EffectKind = EffectKind.Momentum,
            EffectAmount = 1,
        };
        var step = TacticalRunner.Act(session, enc, state, TacticalAction.TakeOpening, 0);

        Assert.IsType<TacticalStep.Finished>(step);
        var fin = (TacticalStep.Finished)step;
        Assert.Equal(TacticalFinishReason.SpiritsLoss, fin.Reason);
        Assert.NotNull(fin.FailureResults);
    }

    // ── Traverse ───────────────────────────────────────

    [Fact]
    public void TraversePathAdvancesOnTakeOpening()
    {
        var (session, state) = MakeContext();
        var enc = MakeTraverse(resistance: 100);
        TacticalRunner.Begin(session, enc, state);

        Assert.NotNull(state.Queue);
        var queueBefore = state.Queue.Count;
        state.Momentum = 10;

        TacticalRunner.Act(session, enc, state, TacticalAction.TakeOpening, 0);
        Assert.Equal(queueBefore - 1, state.Queue.Count);
        Assert.Equal(1, state.PathIndex);
    }

    // ── Momentum ───────────────────────────────────────

    [Fact]
    public void MomentumIncreasesEachTurn()
    {
        var (session, state) = MakeContext();
        var enc = MakeCombat(resistance: 100, approaches: []);
        TacticalRunner.Begin(session, enc, state);

        state.Openings[0] = new OpeningSnapshot
        {
            Name = "Poke",
            CostKind = CostKind.Free,
            EffectKind = EffectKind.Damage,
            EffectAmount = 1,
        };
        var momBefore = state.Momentum;

        TacticalRunner.Act(session, enc, state, TacticalAction.TakeOpening, 0);
        Assert.Equal(momBefore + 1, state.Momentum);
    }
}
