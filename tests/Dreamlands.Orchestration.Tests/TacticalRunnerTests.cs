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
        int resistance = 8,
        List<TimerDef>? timers = null, List<OpeningDef>? openings = null,
        List<ApproachDef>? approaches = null, FailureOutcome? failure = null)
    {
        return new TacticalEncounter
        {
            Id = "test/combat",
            Category = "test",
            Title = "Test Combat",
            Body = "Test.",
            Stat = "combat",
            Timers = timers ?? [new TimerDef("Threat", TimerEffect.Spirits, 1, 4, resistance, "Stop Threat")],
            Openings = openings ?? [
                new OpeningDef("Strike", "momentum_to_progress_large"),
                new OpeningDef("Guard", "free_momentum"),
                new OpeningDef("Jab", "free_progress_small"),
                new OpeningDef("Push", "momentum_to_progress"),
                new OpeningDef("Shove", "free_progress_small"),
                new OpeningDef("Dodge", "free_momentum"),
                new OpeningDef("Rush", "momentum_to_progress"),
                new OpeningDef("Block", "free_progress_small"),
                new OpeningDef("Taunt", "free_momentum"),
                new OpeningDef("Lunge", "momentum_to_progress"),
                new OpeningDef("Parry", "free_progress_small"),
                new OpeningDef("Kick", "free_progress_small"),
                new OpeningDef("Grapple", "momentum_to_progress"),
            ],
            Approaches = approaches ?? [
                new ApproachDef(ApproachKind.Aggressive),
                new ApproachDef(ApproachKind.Cautious),
            ],
            Failure = failure ?? new FailureOutcome("You lose.", ["damage_spirits 2"]),
        };
    }

    static (GameSession Session, TacticalState State) MakeContext()
    {
        var session = Helpers.MakeSession();
        // Give the player a weapon so they have some collection cards
        session.Player.Equipment.Weapon = new ItemInstance("short_sword", "Short Sword");
        // Set bushcraft so traverse queue visibility works (base stat drives visible depth)
        session.Player.Skills[Skill.Bushcraft] = 3;
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
        Assert.Equal(2, ca.Approaches.Count);
    }


    // ── Approach ───────────────────────────────────────────────────

    [Fact]
    public void ApplyApproachSetsState()
    {
        var (session, state) = MakeContext();
        var enc = MakeCombat();
        TacticalRunner.Begin(session, enc, state);

        var step = TacticalRunner.ApplyApproach(session, enc, state, ApproachKind.Aggressive);

        Assert.IsType<TacticalStep.ShowTurn>(step);
        var st = (TacticalStep.ShowTurn)step;
        Assert.Equal(0, st.Data.Momentum);
        Assert.Single(st.Data.Openings);
    }

    [Fact]
    public void CautiousApproachDrawsTwoCards()
    {
        var (session, state) = MakeContext();
        var enc = MakeCombat();
        TacticalRunner.Begin(session, enc, state);

        var step = TacticalRunner.ApplyApproach(session, enc, state, ApproachKind.Cautious);

        Assert.IsType<TacticalStep.ShowTurn>(step);
        var st = (TacticalStep.ShowTurn)step;
        Assert.Equal(0, st.Data.Momentum);
        Assert.Equal(2, st.Data.Openings.Count);
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
        // Short sword contributes collection cards
        var enc = MakeCombat(approaches: []);
        TacticalRunner.Begin(session, enc, state);

        Assert.Contains(state.Deck, c => c.Name == "Test their guard with a quick cut");
        Assert.Contains(state.Deck, c => c.Name == "Feint high and step back to recover");
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
        Assert.True(state.Timers[0].Resistance < 10);
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
        var enc = MakeCombat(resistance: 100, approaches: []);
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
            timers: [new TimerDef("Lethal", TimerEffect.Spirits, 5, 1, 100, "Stop Lethal")],
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

    // ── Condition timers ──────────────────────────────────

    [Fact]
    public void ConditionTimerFireAddsToState()
    {
        var (session, state) = MakeContext();
        session.Player.Spirits = 20;
        var enc = MakeCombat(
            resistance: 100,
            timers: [new TimerDef("Rocks", TimerEffect.Condition, 0, 1, 100, ConditionId: "injured")],
            approaches: []);
        TacticalRunner.Begin(session, enc, state);

        // Timer fires every 1 turn. Take a no-op opening to advance.
        state.Openings[0] = new OpeningSnapshot
        {
            Name = "Stall",
            CostKind = CostKind.Free,
            EffectKind = EffectKind.Momentum,
            EffectAmount = 1,
        };
        TacticalRunner.Act(session, enc, state, TacticalAction.TakeOpening, 0);

        Assert.Single(state.PendingConditions);
        Assert.Equal("injured", state.PendingConditions[0]);
    }

    [Fact]
    public void ConditionTimerStacksMultipleFirings()
    {
        var (session, state) = MakeContext();
        session.Player.Spirits = 20;
        var enc = MakeCombat(
            resistance: 100,
            timers: [new TimerDef("Rocks", TimerEffect.Condition, 0, 1, 100, ConditionId: "injured")],
            approaches: []);
        TacticalRunner.Begin(session, enc, state);

        var stall = new OpeningSnapshot
        {
            Name = "Stall",
            CostKind = CostKind.Free,
            EffectKind = EffectKind.Momentum,
            EffectAmount = 1,
        };

        // Three turns = three firings
        for (int i = 0; i < 3; i++)
        {
            state.Openings[0] = stall;
            TacticalRunner.Act(session, enc, state, TacticalAction.TakeOpening, 0);
        }

        Assert.Equal(3, state.PendingConditions.Count);
        Assert.All(state.PendingConditions, id => Assert.Equal("injured", id));
    }

    [Fact]
    public void PendingConditionsResolvedOnResistanceKill()
    {
        // Seed 42 + default player: RollResist for "injured" should fail with enough stacks
        var (session, state) = MakeContext();
        var enc = MakeCombat(resistance: 1, timers: [], approaches: []);
        TacticalRunner.Begin(session, enc, state);

        // Manually add pending conditions to simulate timer firings
        state.PendingConditions.AddRange(["injured", "injured", "injured"]);

        state.Openings[0] = new OpeningSnapshot
        {
            Name = "Kill Shot",
            CostKind = CostKind.Free,
            EffectKind = EffectKind.Damage,
            EffectAmount = 10,
        };

        var step = TacticalRunner.Act(session, enc, state, TacticalAction.TakeOpening, 0);
        var fin = Assert.IsType<TacticalStep.Finished>(step);
        Assert.Equal(TacticalFinishReason.ResistanceKill, fin.Reason);
        Assert.NotNull(fin.ConditionResults);
        Assert.NotEmpty(fin.ConditionResults);
    }

    [Fact]
    public void PendingConditionsResolvedOnControlKill()
    {
        var (session, state) = MakeContext();
        var enc = MakeCombat(resistance: 100,
            timers: [new TimerDef("Threat", TimerEffect.Spirits, 1, 99, 100, "Stop")],
            approaches: []);
        TacticalRunner.Begin(session, enc, state);

        state.PendingConditions.Add("exhausted");

        state.Openings[0] = new OpeningSnapshot
        {
            Name = "Counter",
            CostKind = CostKind.Free,
            EffectKind = EffectKind.StopTimer,
            StopsTimerIndex = 0,
        };

        var step = TacticalRunner.Act(session, enc, state, TacticalAction.TakeOpening, 0);
        var fin = Assert.IsType<TacticalStep.Finished>(step);
        Assert.Equal(TacticalFinishReason.ControlKill, fin.Reason);
        Assert.NotNull(fin.ConditionResults);
        Assert.NotEmpty(fin.ConditionResults);
    }

    [Fact]
    public void PendingConditionsResolvedOnSpiritsLoss()
    {
        var (session, state) = MakeContext();
        session.Player.Spirits = 1;
        var enc = MakeCombat(
            timers: [new TimerDef("Lethal", TimerEffect.Spirits, 5, 1, 100, "Stop")],
            approaches: [],
            failure: new FailureOutcome("Dead.", ["damage_spirits 1"]));
        TacticalRunner.Begin(session, enc, state);

        state.PendingConditions.Add("injured");

        state.Openings[0] = new OpeningSnapshot
        {
            Name = "Stall",
            CostKind = CostKind.Free,
            EffectKind = EffectKind.Momentum,
            EffectAmount = 1,
        };

        var step = TacticalRunner.Act(session, enc, state, TacticalAction.TakeOpening, 0);
        var fin = Assert.IsType<TacticalStep.Finished>(step);
        Assert.Equal(TacticalFinishReason.SpiritsLoss, fin.Reason);
        Assert.NotNull(fin.ConditionResults);
        Assert.NotEmpty(fin.ConditionResults);
    }

    [Fact]
    public void NoPendingConditionsReturnsEmptyList()
    {
        var (session, state) = MakeContext();
        var enc = MakeCombat(resistance: 1, timers: [], approaches: []);
        TacticalRunner.Begin(session, enc, state);

        state.Openings[0] = new OpeningSnapshot
        {
            Name = "Kill Shot",
            CostKind = CostKind.Free,
            EffectKind = EffectKind.Damage,
            EffectAmount = 10,
        };

        var step = TacticalRunner.Act(session, enc, state, TacticalAction.TakeOpening, 0);
        var fin = Assert.IsType<TacticalStep.Finished>(step);
        Assert.NotNull(fin.ConditionResults);
        Assert.Empty(fin.ConditionResults);
    }

    [Fact]
    public void ConditionTimerAppearsInTimersFired()
    {
        var (session, state) = MakeContext();
        session.Player.Spirits = 20;
        var enc = MakeCombat(
            resistance: 100,
            timers: [new TimerDef("Rocks", TimerEffect.Condition, 0, 1, 100, ConditionId: "injured")],
            approaches: []);
        TacticalRunner.Begin(session, enc, state);

        state.Openings[0] = new OpeningSnapshot
        {
            Name = "Stall",
            CostKind = CostKind.Free,
            EffectKind = EffectKind.Momentum,
            EffectAmount = 1,
        };
        var step = TacticalRunner.Act(session, enc, state, TacticalAction.TakeOpening, 0);
        var turn = Assert.IsType<TacticalStep.ShowTurn>(step);

        Assert.Single(turn.Data.TimersFired);
        Assert.Equal("Rocks", turn.Data.TimersFired[0].Name);
        Assert.Equal(TimerEffect.Condition, turn.Data.TimersFired[0].Effect);
        Assert.Equal("injured", turn.Data.TimersFired[0].ConditionId);
    }

    [Fact]
    public void PendingConditionsVisibleInTurnData()
    {
        var (session, state) = MakeContext();
        session.Player.Spirits = 20;
        var enc = MakeCombat(
            resistance: 100,
            timers: [new TimerDef("Rocks", TimerEffect.Condition, 0, 1, 100, ConditionId: "injured")],
            approaches: []);
        TacticalRunner.Begin(session, enc, state);

        state.Openings[0] = new OpeningSnapshot
        {
            Name = "Stall",
            CostKind = CostKind.Free,
            EffectKind = EffectKind.Momentum,
            EffectAmount = 1,
        };
        var step = TacticalRunner.Act(session, enc, state, TacticalAction.TakeOpening, 0);
        var turn = Assert.IsType<TacticalStep.ShowTurn>(step);

        Assert.Single(turn.Data.PendingConditions);
        Assert.Equal("injured", turn.Data.PendingConditions[0]);
    }

    [Fact]
    public void SkipsConditionAlreadyActive()
    {
        var (session, state) = MakeContext();
        session.Player.ActiveConditions["injured"] = 3;
        var enc = MakeCombat(resistance: 1, timers: [], approaches: []);
        TacticalRunner.Begin(session, enc, state);

        state.PendingConditions.AddRange(["injured", "injured"]);

        state.Openings[0] = new OpeningSnapshot
        {
            Name = "Kill Shot",
            CostKind = CostKind.Free,
            EffectKind = EffectKind.Damage,
            EffectAmount = 10,
        };

        var step = TacticalRunner.Act(session, enc, state, TacticalAction.TakeOpening, 0);
        var fin = Assert.IsType<TacticalStep.Finished>(step);
        Assert.NotNull(fin.ConditionResults);
        Assert.Empty(fin.ConditionResults);
    }

    // ── Ambient timers ─────────────────────────────────

    [Fact]
    public void AmbientTimersTickEveryTurn()
    {
        var (session, state) = MakeContext();
        session.Player.Spirits = 20;
        var enc = MakeCombat(
            resistance: 100,
            timers: [
                new TimerDef("Ambient Drain", TimerEffect.Spirits, 1, 3, 0),  // ambient (resist 0)
                new TimerDef("Sequential", TimerEffect.Spirits, 1, 99, 100),  // sequential
            ],
            approaches: []);
        TacticalRunner.Begin(session, enc, state);

        // Ambient timer should have its own countdown ticking
        var ambient = state.Timers[0];
        Assert.True(ambient.IsAmbient);
        var initialCurrent = ambient.Current;

        state.Openings[0] = new OpeningSnapshot
        {
            Name = "Stall",
            CostKind = CostKind.Free,
            EffectKind = EffectKind.Momentum,
            EffectAmount = 1,
        };
        TacticalRunner.Act(session, enc, state, TacticalAction.TakeOpening, 0);

        Assert.Equal(initialCurrent - 1, ambient.Current);
    }

    [Fact]
    public void AmbientTimersClearWhenSequentialDone()
    {
        var (session, state) = MakeContext();
        var enc = MakeCombat(
            resistance: 1,
            timers: [
                new TimerDef("Ambient", TimerEffect.Spirits, 1, 99, 0),  // ambient
                new TimerDef("Sequential", TimerEffect.Spirits, 1, 99, 1),  // sequential, 1 resist
            ],
            approaches: []);
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
        var fin = (TacticalStep.Finished)step;
        Assert.Equal(TacticalFinishReason.ResistanceKill, fin.Reason);
        Assert.True(state.Timers[0].Stopped); // ambient was auto-cleared
    }

    // ── Fatal timers ──────────────────────────────────

    [Fact]
    public void FatalTimerEndsEncounterOnFire()
    {
        var (session, state) = MakeContext();
        session.Player.Spirits = 20;
        var enc = MakeCombat(
            resistance: 100,
            timers: [
                new TimerDef("Doom", TimerEffect.Fatal, 0, 2, 0),  // ambient fatal, fires in 2 turns
                new TimerDef("Sequential", TimerEffect.Spirits, 1, 99, 100),
            ],
            approaches: [],
            failure: new FailureOutcome("You're caught.", ["damage_spirits 3"]));
        TacticalRunner.Begin(session, enc, state);

        var stall = new OpeningSnapshot
        {
            Name = "Stall",
            CostKind = CostKind.Free,
            EffectKind = EffectKind.Momentum,
            EffectAmount = 1,
        };

        // Turn 1: doom at 2 → 1
        state.Openings[0] = stall;
        var step = TacticalRunner.Act(session, enc, state, TacticalAction.TakeOpening, 0);
        Assert.IsType<TacticalStep.ShowTurn>(step);

        // Turn 2: doom at 1 → 0 → fatal
        state.Openings[0] = stall;
        step = TacticalRunner.Act(session, enc, state, TacticalAction.TakeOpening, 0);
        Assert.IsType<TacticalStep.Finished>(step);
        var fin = (TacticalStep.Finished)step;
        Assert.Equal(TacticalFinishReason.TimerExpired, fin.Reason);
        Assert.NotNull(fin.FailureResults);
    }

    // ── Tick-timer effect ─────────────────────────────

    [Fact]
    public void TickTimerAdvancesTargetCountdown()
    {
        var (session, state) = MakeContext();
        session.Player.Spirits = 20;
        var enc = MakeCombat(
            resistance: 100,
            timers: [
                new TimerDef("Master", TimerEffect.Fatal, 0, 20, 0),  // ambient fatal
                new TimerDef("Waypoint", TimerEffect.TickTimer, 3, 1, 100,
                    TicksTimerName: "Master"),  // sequential, ticks Master by 3 every turn
            ],
            approaches: []);
        TacticalRunner.Begin(session, enc, state);

        var master = state.Timers[0];
        var initialMasterCurrent = master.Current;

        state.Openings[0] = new OpeningSnapshot
        {
            Name = "Stall",
            CostKind = CostKind.Free,
            EffectKind = EffectKind.Momentum,
            EffectAmount = 1,
        };
        TacticalRunner.Act(session, enc, state, TacticalAction.TakeOpening, 0);

        // Master ticked by 1 (ambient natural tick) + 3 (from tick-timer) = 4 less
        Assert.Equal(initialMasterCurrent - 4, master.Current);
    }

    [Fact]
    public void TickTimerCascadesCausingFatalExpiry()
    {
        var (session, state) = MakeContext();
        session.Player.Spirits = 20;
        var enc = MakeCombat(
            resistance: 100,
            timers: [
                new TimerDef("Master", TimerEffect.Fatal, 0, 5, 0),  // ambient fatal, 5 turns
                new TimerDef("Tick Source", TimerEffect.TickTimer, 10, 1, 100,
                    TicksTimerName: "Master"),  // ticks Master by 10 on first fire
            ],
            approaches: [],
            failure: new FailureOutcome("Caught.", []));
        TacticalRunner.Begin(session, enc, state);

        state.Openings[0] = new OpeningSnapshot
        {
            Name = "Stall",
            CostKind = CostKind.Free,
            EffectKind = EffectKind.Momentum,
            EffectAmount = 1,
        };

        // Turn 1: Master 5→4 (ambient tick), Tick Source fires→Master 4→-6. Fatal!
        var step = TacticalRunner.Act(session, enc, state, TacticalAction.TakeOpening, 0);
        Assert.IsType<TacticalStep.Finished>(step);
        Assert.Equal(TacticalFinishReason.TimerExpired, ((TacticalStep.Finished)step).Reason);
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
        // Default approach is Aggressive = +2 momentum per turn
        Assert.Equal(momBefore + 2, state.Momentum);
    }
}
