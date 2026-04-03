using Dreamlands.Game;
using Dreamlands.Rules;
using Dreamlands.Tactical;

namespace Dreamlands.Orchestration;

// ── Step results ───────────────────────────────────────────────────

public abstract record TacticalStep
{
    public record ChooseApproach(
        TacticalEncounter Encounter,
        IReadOnlyList<ApproachDef> Approaches) : TacticalStep;

    public record ShowTurn(TacticalTurnData Data) : TacticalStep;

    public record Finished(
        TacticalFinishReason Reason,
        List<MechanicResult>? FailureResults = null,
        List<MechanicResult>? SuccessResults = null,
        List<MechanicResult>? ConditionResults = null) : TacticalStep;
}

public enum TacticalFinishReason { ResistanceKill, ControlKill, SpiritsLoss }

public record TacticalTurnData(
    int Turn,
    int Momentum,
    int PlayerSpirits,
    /// <summary>Current active timer's remaining resistance.</summary>
    int Resistance,
    /// <summary>Current active timer's max resistance.</summary>
    int ResistanceMax,
    /// <summary>Whether press/force has been used this turn.</summary>
    bool DigUsed,
    IReadOnlyList<ActiveTimer> Timers,
    /// <summary>Index of current active timer in the Timers list.</summary>
    int CurrentTimerIndex,
    IReadOnlyList<OpeningSnapshot> Openings,
    IReadOnlyList<OpeningSnapshot>? Queue,
    IReadOnlyList<TimerFired> TimersFired,
    IReadOnlyList<string> PendingConditions);

public record TimerFired(string Name, TimerEffect Effect, int Amount, string? ConditionId = null);

public enum TacticalAction { TakeOpening, PressAdvantage, ForceOpening }

// ── Runner ─────────────────────────────────────────────────────────
//
// vNext turn flow (from fun_deck.md):
//   1. Active timer ticks down (fires if 0, effect applied, countdown resets)
//   2. Momentum +1 (cautious) or +2 (aggressive)
//   3. Draw 1 card (aggressive) or 2 cards (cautious)
//   4. Either play one card, OR press/force to draw 2 more then pick one
//   5. Progress applied to current timer's resistance
//   6. Timer resistance cleared → next timer activates
//   7. Last timer cleared → victory
//
// Press: costs 2 momentum, draws 2 more cards into hand
// Force: costs 2 spirits, draws 2 more cards into hand
// Press and Force are mutually exclusive per turn.
// Must always play a card.

public static class TacticalRunner
{
    public static TacticalStep Begin(GameSession session, TacticalEncounter encounter, TacticalState state)
    {
        state.EncounterId = encounter.Id;
        state.Turn = 0;
        state.CurrentTimerIndex = 0;

        if (encounter.Variant == Variant.Combat)
        {
            if (encounter.Approaches.Count > 0)
                return new TacticalStep.ChooseApproach(encounter, encounter.Approaches);

            // No approaches defined — default to aggressive
            state.Approach = ApproachKind.Aggressive;
            InitTimers(state, encounter);
            BuildDeck(state, session, encounter);
            return StartTurn(state, session, encounter);
        }

        // Traverse: no approach selection
        state.Approach = ApproachKind.Cautious;
        state.Momentum = 0;
        state.PathIndex = 0;
        InitTimers(state, encounter);
        BuildDeck(state, session, encounter);

        // Populate queue from authored path
        state.Queue = [];
        var archetypes = session.Balance.Tactical.Archetypes;
        for (int i = 0; i < encounter.Path.Count; i++)
        {
            if (archetypes.TryGetValue(encounter.Path[i].Archetype, out var arch))
                state.Queue.Add(SnapshotFromArchetype(arch, encounter.Path[i].Name));
        }

        return StartTurn(state, session, encounter);
    }

    public static TacticalStep Resume(GameSession session, TacticalEncounter encounter, TacticalState state)
    {
        if (encounter.Variant == Variant.Combat && encounter.Approaches.Count > 0 && state.Timers.Count == 0)
            return new TacticalStep.ChooseApproach(encounter, encounter.Approaches);

        return MakeShowTurn(state, session, encounter);
    }

    public static TacticalStep ApplyApproach(
        GameSession session, TacticalEncounter encounter, TacticalState state, ApproachKind approach)
    {
        state.Approach = approach;
        state.Momentum = 0;
        InitTimers(state, encounter);
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
            state.Openings.Add(ReThemeStopTimer(DeckBuilder.Draw(state, session.Rng), state));

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
        var currentTimer = GetCurrentTimer(state);

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
                if (currentTimer != null)
                    currentTimer.Current--;
                break;
        }

        // Apply effect to current timer
        switch (opening.EffectKind)
        {
            case EffectKind.Damage:
                if (currentTimer != null)
                    currentTimer.Resistance = Math.Max(0, currentTimer.Resistance - opening.EffectAmount);
                break;
            case EffectKind.StopTimer:
                // Cancel clears the current timer entirely
                if (currentTimer != null)
                    currentTimer.Resistance = 0;
                break;
            case EffectKind.Momentum:
                state.Momentum += opening.EffectAmount;
                break;
        }

        // Advance past cleared timers
        while (state.CurrentTimerIndex < state.Timers.Count
            && state.Timers[state.CurrentTimerIndex].Resistance <= 0)
        {
            state.Timers[state.CurrentTimerIndex].Stopped = true;
            state.CurrentTimerIndex++;
        }

        // Win: all timers cleared
        if (state.CurrentTimerIndex >= state.Timers.Count)
        {
            var reason = opening.EffectKind == EffectKind.StopTimer
                ? TacticalFinishReason.ControlKill
                : TacticalFinishReason.ResistanceKill;
            var successResults = encounter.Success != null
                ? Mechanics.Apply(encounter.Success.Mechanics, session.Player, session.Balance, session.Rng)
                : null;
            return new TacticalStep.Finished(reason,
                SuccessResults: successResults,
                ConditionResults: ResolvePendingConditions(state, session));
        }

        // Traverse: advance path
        if (encounter.Variant == Variant.Traverse && state.Queue != null && state.Queue.Count > 0)
        {
            state.PathIndex++;
            state.Queue.RemoveAt(0);
        }

        return AdvanceTurn(state, session, encounter);
    }

    // ── Turn advancement ──────────────────────────────────────

    static TacticalStep AdvanceTurn(TacticalState state, GameSession session, TacticalEncounter encounter)
    {
        state.Turn++;
        state.DigUsedThisTurn = false;

        // 1. Tick the current active timer
        var fired = new List<TimerFired>();
        var timer = GetCurrentTimer(state);
        if (timer != null)
        {
            timer.Current--;
            if (timer.Current <= 0)
            {
                switch (timer.Effect)
                {
                    case TimerEffect.Spirits:
                        session.Player.Spirits = Math.Max(0, session.Player.Spirits - timer.Amount);
                        fired.Add(new TimerFired(timer.Name, timer.Effect, timer.Amount));
                        break;
                    case TimerEffect.Resistance:
                        timer.Resistance += timer.Amount;
                        fired.Add(new TimerFired(timer.Name, timer.Effect, timer.Amount));
                        break;
                    case TimerEffect.Condition:
                        state.PendingConditions.Add(timer.ConditionId!);
                        fired.Add(new TimerFired(timer.Name, timer.Effect, 0, timer.ConditionId));
                        break;
                }
                timer.Current = timer.Countdown; // reset countdown
            }
        }
        state.LastTimersFired = fired;

        // Spirits loss check
        if (session.Player.Spirits <= 0)
        {
            List<MechanicResult>? failureResults = null;
            if (encounter.Failure != null)
                failureResults = Mechanics.Apply(encounter.Failure.Mechanics, session.Player, session.Balance, session.Rng);
            return new TacticalStep.Finished(TacticalFinishReason.SpiritsLoss,
                FailureResults: failureResults,
                ConditionResults: ResolvePendingConditions(state, session));
        }

        // 2. Gain momentum (approach-dependent)
        int momentumGain = state.Approach == ApproachKind.Aggressive ? 2 : 1;
        state.Momentum += momentumGain;

        return StartTurn(state, session, encounter);
    }

    static TacticalStep StartTurn(TacticalState state, GameSession session, TacticalEncounter encounter)
    {
        // 3. Draw cards (approach-dependent)
        int drawCount = state.Approach == ApproachKind.Cautious ? 2 : 1;

        if (encounter.Variant == Variant.Traverse && state.Queue != null && state.Queue.Count > 0)
        {
            state.Openings = [state.Queue[0]];
            for (int i = 1; i < drawCount; i++)
                state.Openings.Add(ReThemeStopTimer(DeckBuilder.Draw(state, session.Rng), state));
        }
        else
        {
            state.Openings = DrawOpenings(state, drawCount, session.Rng);
        }

        EnsurePlayable(state, session);
        return MakeShowTurn(state, session, encounter);
    }

    static TacticalStep.ShowTurn MakeShowTurn(TacticalState state, GameSession session, TacticalEncounter encounter)
    {
        var timer = GetCurrentTimer(state);
        return new TacticalStep.ShowTurn(new TacticalTurnData(
            state.Turn,
            state.Momentum,
            session.Player.Spirits,
            timer?.Resistance ?? 0,
            encounter.Timers.Count > state.CurrentTimerIndex
                ? encounter.Timers[state.CurrentTimerIndex].Resistance : 0,
            state.DigUsedThisTurn,
            state.Timers,
            state.CurrentTimerIndex,
            state.Openings,
            VisibleQueue(state, session, encounter),
            state.LastTimersFired,
            state.PendingConditions));
    }

    // ── Helpers ────────────────────────────────────────────────

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
            state.Openings[lastIdx] = ReThemeStopTimer(DeckBuilder.Draw(state, session.Rng), state);
        }
    }

    static ActiveTimer? GetCurrentTimer(TacticalState state) =>
        state.CurrentTimerIndex < state.Timers.Count ? state.Timers[state.CurrentTimerIndex] : null;

    static List<OpeningSnapshot> DrawOpenings(TacticalState state, int count, Random rng)
    {
        var result = new List<OpeningSnapshot>(count);
        for (int i = 0; i < count; i++)
            result.Add(ReThemeStopTimer(DeckBuilder.Draw(state, rng), state));
        return result;
    }

    static OpeningSnapshot ReThemeStopTimer(OpeningSnapshot card, TacticalState state)
    {
        if (card.EffectKind != EffectKind.StopTimer) return card;

        var timer = GetCurrentTimer(state);
        if (timer == null) return card;

        card.StopsTimerIndex = state.CurrentTimerIndex;
        card.Name = timer.CounterName ?? card.Name;
        return card;
    }

    static void BuildDeck(TacticalState state, GameSession session, TacticalEncounter encounter)
    {
        state.Deck = DeckBuilder.Build(encounter, session.Player, session.Balance, state.Timers, session.Rng);
        state.DrawIndex = 0;
    }

    static void InitTimers(TacticalState state, TacticalEncounter encounter)
    {
        state.Timers = [];
        state.CurrentTimerIndex = 0;
        foreach (var def in encounter.Timers)
        {
            state.Timers.Add(new ActiveTimer
            {
                Name = def.Name,
                CounterName = def.CounterName,
                Effect = def.Effect,
                Amount = def.Amount,
                Countdown = def.Countdown,
                Current = def.Countdown,
                Resistance = def.Resistance,
                Stopped = false,
                ConditionId = def.ConditionId,
            });
        }
    }

    static IReadOnlyList<OpeningSnapshot>? VisibleQueue(TacticalState state, GameSession session, TacticalEncounter encounter)
    {
        if (state.Queue == null) return null;
        var depth = GetGoverningSkillLevel(session, encounter);
        return state.Queue.Take(depth).ToList();
    }

    static int GetGoverningSkillLevel(GameSession session, TacticalEncounter encounter)
    {
        var skill = encounter.Stat != null ? Skills.FromScriptName(encounter.Stat) : null;
        if (skill == null) return 0;
        return session.Player.Skills.TryGetValue(skill.Value, out var level) ? level : 0;
    }

    static List<MechanicResult> ResolvePendingConditions(TacticalState state, GameSession session)
    {
        var results = new List<MechanicResult>();
        if (state.PendingConditions.Count == 0) return results;

        var grouped = state.PendingConditions.GroupBy(id => id);
        foreach (var group in grouped)
        {
            var conditionId = group.Key;
            if (session.Player.ActiveConditions.ContainsKey(conditionId)) continue;

            session.Balance.Conditions.TryGetValue(conditionId, out var def);
            var dc = def?.ResistDifficulty ?? session.Balance.Character.AmbientResistDifficulty;

            bool anyFailed = false;
            SkillCheckResult? lastCheck = null;
            foreach (var _ in group)
            {
                lastCheck = SkillChecks.RollResist(conditionId, dc, session.Player, session.Balance, session.Rng);
                if (!lastCheck.Passed) { anyFailed = true; break; }
            }

            if (anyFailed)
            {
                var stacks = def?.Stacks ?? 1;
                session.Player.ActiveConditions[conditionId] = stacks;
                results.Add(new MechanicResult.ConditionAdded(conditionId, stacks, lastCheck));
            }
            else
            {
                results.Add(new MechanicResult.ConditionResisted(conditionId, lastCheck!));
            }
        }

        return results;
    }

    static OpeningSnapshot SnapshotFromArchetype(TacticalArchetype arch, string name) => new()
    {
        Name = name,
        CostKind = Enum.Parse<CostKind>(arch.CostKind, ignoreCase: true),
        CostAmount = arch.CostAmount,
        EffectKind = Enum.Parse<EffectKind>(arch.EffectKind, ignoreCase: true),
        EffectAmount = arch.EffectAmount,
    };
}
