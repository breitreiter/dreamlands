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
        int clock = 10,
        List<ChallengeDef>? challenges = null,
        List<OpeningDef>? openings = null,
        List<ApproachDef>? approaches = null,
        FailureOutcome? failure = null)
    {
        return new TacticalEncounter
        {
            Id = "test/combat",
            Category = "test",
            Title = "Test Combat",
            Body = "Test.",
            Stat = "combat",
            Clock = clock,
            Challenges = challenges ?? [new ChallengeDef("Threat", "Stop Threat", 8)],
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
    public void DeckLoopsWhenExhausted()
    {
        var (session, state) = MakeContext();
        var enc = MakeCombat(approaches: []);
        TacticalRunner.Begin(session, enc, state);

        // Draw until exhausted + 2 more
        var deckSize = state.Deck.Count;
        for (int i = state.DrawIndex; i < deckSize + 2; i++)
            DeckBuilder.Draw(state);

        // DrawIndex should have wrapped
        Assert.True(state.DrawIndex <= 2);
    }

    // ── Take Opening ───────────────────────────────────

    [Fact]
    public void TakeOpeningDamagesResistance()
    {
        var (session, state) = MakeContext();
        var enc = MakeCombat(challenges: [new ChallengeDef("Test", null, 10)], approaches: []);
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
        Assert.True(state.Challenges[0].Resistance < 10);
    }

    [Fact]
    public void ResistanceZeroEndsWithResistanceKill()
    {
        var (session, state) = MakeContext();
        var enc = MakeCombat(challenges: [new ChallengeDef("Test", null, 1)], approaches: []);
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
    public void AllChallengesClearedEndsWithControlKill()
    {
        var (session, state) = MakeContext();
        var enc = MakeCombat(challenges: [new ChallengeDef("Test", null, 100)], approaches: []);
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

    // ── Clock ─────────────────────────────────────────

    [Fact]
    public void ClockExpiryTriggersFailure()
    {
        var (session, state) = MakeContext();
        session.Player.Spirits = 20;
        var enc = MakeCombat(
            clock: 2,
            challenges: [new ChallengeDef("Test", null, 100)],
            approaches: [],
            failure: new FailureOutcome("Time's up.", ["damage_spirits 3"]));
        TacticalRunner.Begin(session, enc, state);

        var stall = new OpeningSnapshot
        {
            Name = "Stall",
            CostKind = CostKind.Free,
            EffectKind = EffectKind.Momentum,
            EffectAmount = 1,
        };

        // Turn 1: clock 2 → 1
        state.Openings[0] = stall;
        var step = TacticalRunner.Act(session, enc, state, TacticalAction.TakeOpening, 0);
        Assert.IsType<TacticalStep.ShowTurn>(step);

        // Turn 2: clock 1 → 0 → expired
        state.Openings[0] = stall;
        step = TacticalRunner.Act(session, enc, state, TacticalAction.TakeOpening, 0);
        Assert.IsType<TacticalStep.Finished>(step);
        var fin = (TacticalStep.Finished)step;
        Assert.Equal(TacticalFinishReason.ClockExpired, fin.Reason);
        Assert.NotNull(fin.FailureResults);
    }

    [Fact]
    public void SpiritsDrainToZeroTriggersFailure()
    {
        var (session, state) = MakeContext();
        session.Player.Spirits = 1;
        var enc = MakeCombat(
            challenges: [new ChallengeDef("Test", null, 100)],
            approaches: [],
            failure: new FailureOutcome("Dead.", ["damage_spirits 1"]));
        TacticalRunner.Begin(session, enc, state);

        // Spend the last spirit on a spirits-cost card
        state.Openings[0] = new OpeningSnapshot
        {
            Name = "Desperate",
            CostKind = CostKind.Spirits,
            CostAmount = 1,
            EffectKind = EffectKind.Damage,
            EffectAmount = 3,
        };
        var step = TacticalRunner.Act(session, enc, state, TacticalAction.TakeOpening, 0);

        Assert.IsType<TacticalStep.Finished>(step);
        var fin = (TacticalStep.Finished)step;
        Assert.Equal(TacticalFinishReason.SpiritsLoss, fin.Reason);
        Assert.NotNull(fin.FailureResults);
    }

    // ── Multi-challenge progression ───────────────────

    [Fact]
    public void ClearingFirstChallengeAdvancesToSecond()
    {
        var (session, state) = MakeContext();
        var enc = MakeCombat(
            challenges: [
                new ChallengeDef("First", null, 1),
                new ChallengeDef("Second", null, 100),
            ],
            approaches: []);
        TacticalRunner.Begin(session, enc, state);

        state.Openings[0] = new OpeningSnapshot
        {
            Name = "Hit",
            CostKind = CostKind.Free,
            EffectKind = EffectKind.Damage,
            EffectAmount = 5,
        };

        var step = TacticalRunner.Act(session, enc, state, TacticalAction.TakeOpening, 0);
        Assert.IsType<TacticalStep.ShowTurn>(step);

        Assert.True(state.Challenges[0].Cleared);
        Assert.Equal(1, state.CurrentChallengeIndex);
    }

    [Fact]
    public void ClearingAllChallengesWins()
    {
        var (session, state) = MakeContext();
        var enc = MakeCombat(
            challenges: [
                new ChallengeDef("First", null, 1),
                new ChallengeDef("Second", null, 1),
            ],
            approaches: []);
        TacticalRunner.Begin(session, enc, state);

        // Clear first
        state.Openings[0] = new OpeningSnapshot
        {
            Name = "Hit",
            CostKind = CostKind.Free,
            EffectKind = EffectKind.Damage,
            EffectAmount = 5,
        };
        var step = TacticalRunner.Act(session, enc, state, TacticalAction.TakeOpening, 0);
        Assert.IsType<TacticalStep.ShowTurn>(step);

        // Clear second
        state.Openings[0] = new OpeningSnapshot
        {
            Name = "Hit",
            CostKind = CostKind.Free,
            EffectKind = EffectKind.Damage,
            EffectAmount = 5,
        };
        step = TacticalRunner.Act(session, enc, state, TacticalAction.TakeOpening, 0);
        Assert.IsType<TacticalStep.Finished>(step);
        Assert.Equal(TacticalFinishReason.ResistanceKill, ((TacticalStep.Finished)step).Reason);
    }

    // ── Tick cost advances clock ──────────────────────

    [Fact]
    public void TickCostAdvancesClock()
    {
        var (session, state) = MakeContext();
        var enc = MakeCombat(clock: 10, approaches: []);
        TacticalRunner.Begin(session, enc, state);

        var clockBefore = state.Clock;
        state.Openings[0] = new OpeningSnapshot
        {
            Name = "Risky Move",
            CostKind = CostKind.Tick,
            CostAmount = 1,
            EffectKind = EffectKind.Damage,
            EffectAmount = 2,
        };
        TacticalRunner.Act(session, enc, state, TacticalAction.TakeOpening, 0);
        // Clock should have ticked once from the card cost, plus once from turn advancement
        Assert.Equal(clockBefore - 2, state.Clock);
    }

    // ── Momentum ───────────────────────────────────────

    [Fact]
    public void MomentumIncreasesEachTurn()
    {
        var (session, state) = MakeContext();
        var enc = MakeCombat(challenges: [new ChallengeDef("Test", null, 100)], approaches: []);
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

    // ── Clock visible in turn data ────────────────────

    [Fact]
    public void TurnDataIncludesClock()
    {
        var (session, state) = MakeContext();
        var enc = MakeCombat(clock: 15, approaches: []);
        TacticalRunner.Begin(session, enc, state);

        var step = TacticalRunner.Begin(session, enc, state);
        Assert.IsType<TacticalStep.ShowTurn>(step);
        var data = ((TacticalStep.ShowTurn)step).Data;
        Assert.Equal(15, data.Clock);
    }

    [Fact]
    public void TurnDataIncludesChallenges()
    {
        var (session, state) = MakeContext();
        var enc = MakeCombat(
            challenges: [
                new ChallengeDef("Alpha", "Stop Alpha", 5),
                new ChallengeDef("Beta", null, 8),
            ],
            approaches: []);
        TacticalRunner.Begin(session, enc, state);

        var step = TacticalRunner.Begin(session, enc, state);
        Assert.IsType<TacticalStep.ShowTurn>(step);
        var data = ((TacticalStep.ShowTurn)step).Data;
        Assert.Equal(2, data.Challenges.Count);
        Assert.Equal("Alpha", data.Challenges[0].Name);
        Assert.Equal("Stop Alpha", data.Challenges[0].CounterName);
        Assert.Equal(5, data.Challenges[0].Resistance);
    }
}
