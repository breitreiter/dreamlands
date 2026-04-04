using Dreamlands.Game;
using Dreamlands.Rules;
using Dreamlands.Tactical;

namespace Dreamlands.Orchestration;

// ── Step results ───────��───────────────────────────────────────────

public abstract record TacticalStep
{
    public record ChooseApproach(
        TacticalEncounter Encounter,
        IReadOnlyList<ApproachDef> Approaches) : TacticalStep;

    public record ShowTurn(TacticalTurnData Data) : TacticalStep;

    public record Finished(
        TacticalFinishReason Reason,
        List<MechanicResult>? FailureResults = null,
        List<MechanicResult>? SuccessResults = null) : TacticalStep;
}

public enum TacticalFinishReason { ResistanceKill, ControlKill, SpiritsLoss, ClockExpired }

public record TacticalTurnData(
    int Turn,
    int Momentum,
    int PlayerSpirits,
    int Clock,
    IReadOnlyList<ActiveChallenge> Challenges,
    int CurrentChallengeIndex,
    bool DigUsed,
    IReadOnlyList<OpeningSnapshot> Openings);

public enum TacticalAction { TakeOpening, PressAdvantage, ForceOpening }

// ── Runner ──────��──────────────────────────────────────────────────
//
// Turn flow:
//   1. Decrement clock (check clock expiry)
//   2. Check spirits loss
//   3. Momentum gain (approach-dependent)
//   4. Draw cards
//   5. Player plays one card, OR press/force to draw 2 more then pick one
//   6. Progress applied to current challenge's resistance
//   7. Challenge resistance cleared → next challenge activates
//   8. All challenges cleared → victory

public static class TacticalRunner
{
    public static TacticalStep Begin(GameSession session, TacticalEncounter encounter, TacticalState state)
    {
        state.EncounterId = encounter.Id;
        state.Turn = 0;
        state.CurrentChallengeIndex = 0;

        if (encounter.Approaches.Count > 0)
            return new TacticalStep.ChooseApproach(encounter, encounter.Approaches);

        // No approaches defined — default to aggressive
        state.Approach = ApproachKind.Aggressive;
        state.Momentum = 0;
        InitChallenges(state, encounter);
        BuildDeck(state, session, encounter);
        return StartTurn(state, session, encounter);
    }

    public static TacticalStep Resume(GameSession session, TacticalEncounter encounter, TacticalState state)
    {
        if (encounter.Approaches.Count > 0 && state.Challenges.Count == 0)
            return new TacticalStep.ChooseApproach(encounter, encounter.Approaches);

        return MakeShowTurn(state, session, encounter);
    }

    public static TacticalStep ApplyApproach(
        GameSession session, TacticalEncounter encounter, TacticalState state, ApproachKind approach)
    {
        state.Approach = approach;
        state.Momentum = 0;
        InitChallenges(state, encounter);
        BuildDeck(state, session, encounter);
        return StartTurn(state, session, encounter);
    }

    public static TacticalStep Act(
        GameSession session, TacticalEncounter encounter, TacticalState state,
        TacticalAction action, int openingIndex = 0)
    {
        return action switch
        {
            TacticalAction.TakeOpening => TakeOpening(session, encounter, state, openingIndex),
            TacticalAction.PressAdvantage => PressOrForce(session, encounter, state, isMomentum: true),
            TacticalAction.ForceOpening => PressOrForce(session, encounter, state, isMomentum: false),
            _ => throw new ArgumentOutOfRangeException(nameof(action)),
        };
    }

    // ── Press / Force: instant draw-2 into current hand ───────

    static TacticalStep PressOrForce(GameSession session, TacticalEncounter encounter, TacticalState state, bool isMomentum)
    {
        if (state.DigUsedThisTurn)
            throw new InvalidOperationException("Already used press/force this turn.");

        var tb = session.Balance.Tactical;
        if (isMomentum)
        {
            if (state.Momentum < tb.PressAdvantageCost)
                throw new InvalidOperationException("Not enough momentum to Press.");
            state.Momentum -= tb.PressAdvantageCost;
        }
        else
        {
            if (session.Player.Spirits < tb.ForceOpeningCost)
                throw new InvalidOperationException("Not enough spirits to Force.");
            session.Player.Spirits -= tb.ForceOpeningCost;
        }

        state.DigUsedThisTurn = true;

        // Draw 2 more cards into the current hand
        for (int i = 0; i < 2; i++)
            state.Openings.Add(ReThemeStopTimer(DeckBuilder.Draw(state), state));

        EnsurePlayable(state, session);
        return MakeShowTurn(state, session, encounter);
    }

    // ── Take Opening: play a card, apply effects ──────────────

    static TacticalStep TakeOpening(
        GameSession session, TacticalEncounter encounter, TacticalState state, int index)
    {
        if (index < 0 || index >= state.Openings.Count)
            throw new ArgumentOutOfRangeException(nameof(index));

        var opening = state.Openings[index];
        var currentChallenge = GetCurrentChallenge(state);

        // Pay cost
        switch (opening.CostKind)
        {
            case CostKind.Momentum:
                if (state.Momentum < opening.CostAmount)
                    throw new InvalidOperationException("Not enough momentum.");
                state.Momentum -= opening.CostAmount;
                break;
            case CostKind.Spirits:
                if (session.Player.Spirits < opening.CostAmount)
                    throw new InvalidOperationException("Not enough spirits.");
                session.Player.Spirits -= opening.CostAmount;
                break;
            case CostKind.Tick:
                // Tick costs advance the clock by 1
                state.Clock--;
                break;
        }

        // Apply effect to current challenge
        switch (opening.EffectKind)
        {
            case EffectKind.Damage:
                if (currentChallenge != null)
                    currentChallenge.Resistance = Math.Max(0, currentChallenge.Resistance - opening.EffectAmount);
                break;
            case EffectKind.StopTimer:
                // Cancel clears the current challenge entirely
                if (currentChallenge != null)
                    currentChallenge.Resistance = 0;
                break;
            case EffectKind.Momentum:
                state.Momentum += opening.EffectAmount;
                break;
        }

        // Advance past cleared challenges
        AdvancePastClearedChallenges(state);

        // Win: all challenges cleared
        if (AllChallengesCleared(state))
        {
            var reason = opening.EffectKind == EffectKind.StopTimer
                ? TacticalFinishReason.ControlKill
                : TacticalFinishReason.ResistanceKill;
            var successResults = encounter.Success != null
                ? Mechanics.Apply(encounter.Success.Mechanics, session.Player, session.Balance, session.Rng)
                : null;
            return new TacticalStep.Finished(reason, SuccessResults: successResults);
        }

        return AdvanceTurn(state, session, encounter);
    }

    // ── Turn advancement ───���──────────────────────────────────

    static TacticalStep AdvanceTurn(TacticalState state, GameSession session, TacticalEncounter encounter)
    {
        state.Turn++;
        state.DigUsedThisTurn = false;

        // 1. Decrement clock
        state.Clock--;

        // 2. Check clock expiry
        if (state.Clock <= 0)
        {
            List<MechanicResult>? failureResults = null;
            if (encounter.Failure != null)
                failureResults = Mechanics.Apply(encounter.Failure.Mechanics, session.Player, session.Balance, session.Rng);
            return new TacticalStep.Finished(TacticalFinishReason.ClockExpired, FailureResults: failureResults);
        }

        // 3. Check spirits loss
        if (session.Player.Spirits <= 0)
        {
            List<MechanicResult>? failureResults = null;
            if (encounter.Failure != null)
                failureResults = Mechanics.Apply(encounter.Failure.Mechanics, session.Player, session.Balance, session.Rng);
            return new TacticalStep.Finished(TacticalFinishReason.SpiritsLoss, FailureResults: failureResults);
        }

        // 4. Gain momentum (approach-dependent)
        int momentumGain = state.Approach == ApproachKind.Aggressive ? 2 : 1;
        state.Momentum += momentumGain;

        return StartTurn(state, session, encounter);
    }

    static TacticalStep StartTurn(TacticalState state, GameSession session, TacticalEncounter encounter)
    {
        int drawCount = state.Approach == ApproachKind.Cautious ? 2 : 1;
        state.Openings = DrawOpenings(state, drawCount);
        EnsurePlayable(state, session);
        return MakeShowTurn(state, session, encounter);
    }

    static TacticalStep.ShowTurn MakeShowTurn(TacticalState state, GameSession session, TacticalEncounter encounter)
    {
        return new TacticalStep.ShowTurn(new TacticalTurnData(
            state.Turn,
            state.Momentum,
            session.Player.Spirits,
            state.Clock,
            state.Challenges,
            state.CurrentChallengeIndex,
            state.DigUsedThisTurn,
            state.Openings));
    }

    // ── Helpers ─────────────��──────────────────────────────────

    /// <summary>
    /// If no card in the hand is playable, silently cycle the last slot
    /// through the deck until at least one card is affordable.
    /// </summary>
    static void EnsurePlayable(TacticalState state, GameSession session)
    {
        if (state.Openings.Count == 0) return;

        bool HasPlayable() => state.Openings.Any(o =>
            o.CostKind == CostKind.Free
            || (o.CostKind == CostKind.Momentum && state.Momentum >= o.CostAmount)
            || (o.CostKind == CostKind.Spirits && session.Player.Spirits >= o.CostAmount)
            || o.CostKind == CostKind.Tick);

        int safety = state.Deck.Count;
        int lastIdx = state.Openings.Count - 1;
        while (!HasPlayable() && safety-- > 0)
        {
            state.Openings[lastIdx] = ReThemeStopTimer(DeckBuilder.Draw(state), state);
        }
    }

    /// <summary>Get the current challenge, or null if all are cleared.</summary>
    static ActiveChallenge? GetCurrentChallenge(TacticalState state)
    {
        for (int i = state.CurrentChallengeIndex; i < state.Challenges.Count; i++)
        {
            var c = state.Challenges[i];
            if (!c.Cleared) return c;
        }
        return null;
    }

    static bool AllChallengesCleared(TacticalState state) =>
        state.Challenges.All(c => c.Cleared || c.Resistance <= 0);

    static void AdvancePastClearedChallenges(TacticalState state)
    {
        while (state.CurrentChallengeIndex < state.Challenges.Count)
        {
            var c = state.Challenges[state.CurrentChallengeIndex];
            if (c.Resistance > 0)
                break;
            c.Cleared = true;
            state.CurrentChallengeIndex++;
        }
    }

    static List<OpeningSnapshot> DrawOpenings(TacticalState state, int count)
    {
        var result = new List<OpeningSnapshot>(count);
        for (int i = 0; i < count; i++)
            result.Add(ReThemeStopTimer(DeckBuilder.Draw(state), state));
        return result;
    }

    static OpeningSnapshot ReThemeStopTimer(OpeningSnapshot card, TacticalState state)
    {
        if (card.EffectKind != EffectKind.StopTimer) return card;

        var challenge = GetCurrentChallenge(state);
        if (challenge == null) return card;

        card.StopsTimerIndex = state.CurrentChallengeIndex;
        card.Name = challenge.CounterName ?? card.Name;
        return card;
    }

    static void BuildDeck(TacticalState state, GameSession session, TacticalEncounter encounter)
    {
        state.Deck = DeckBuilder.Build(encounter, session.Player, session.Balance, session.Rng);
        state.DrawIndex = 0;
    }

    static void InitChallenges(TacticalState state, TacticalEncounter encounter)
    {
        state.Challenges = [];
        state.CurrentChallengeIndex = 0;
        state.Clock = encounter.Clock;

        foreach (var def in encounter.Challenges)
        {
            state.Challenges.Add(new ActiveChallenge
            {
                Name = def.Name,
                CounterName = def.CounterName,
                Resistance = def.Resistance,
                MaxResistance = def.Resistance,
                Cleared = false,
            });
        }
    }
}
