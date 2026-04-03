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

public enum TacticalFinishReason { ResistanceKill, ControlKill, SpiritsLoss, TimerExpired }

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
    IReadOnlyList<TimerFired> TimersFired,
    IReadOnlyList<string> PendingConditions);

public record TimerFired(string Name, TimerEffect Effect, int Amount, string? ConditionId = null);

public enum TacticalAction { TakeOpening, PressAdvantage, ForceOpening }

// ── Runner ─────────────────────────────────────────────────────────
//
// Turn flow:
//   1. Tick ambient timers (check fatal after each)
//   2. Tick current sequential timer (check fatal cascade from tick-timer)
//   3. Check spirits loss
//   4. Momentum gain (approach-dependent)
//   5. Draw cards
//   6. Player plays one card, OR press/force to draw 2 more then pick one
//   7. Progress applied to current sequential timer's resistance
//   8. Timer resistance cleared → next sequential timer activates
//   9. All sequential timers cleared → ambient auto-cleared → victory

public static class TacticalRunner
{
    public static TacticalStep Begin(GameSession session, TacticalEncounter encounter, TacticalState state)
    {
        state.EncounterId = encounter.Id;
        state.Turn = 0;
        state.CurrentTimerIndex = 0;

        if (encounter.Approaches.Count > 0)
            return new TacticalStep.ChooseApproach(encounter, encounter.Approaches);

        // No approaches defined — default to aggressive
        state.Approach = ApproachKind.Aggressive;
        state.Momentum = 0;
        InitTimers(state, encounter);
        BuildDeck(state, session, encounter);
        return StartTurn(state, session, encounter);
    }

    public static TacticalStep Resume(GameSession session, TacticalEncounter encounter, TacticalState state)
    {
        if (encounter.Approaches.Count > 0 && state.Timers.Count == 0)
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
        var currentTimer = GetCurrentSequentialTimer(state);

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

        // Apply effect to current sequential timer
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

        // Advance past cleared sequential timers
        AdvancePastClearedTimers(state);

        // Win: all sequential timers cleared
        if (AllSequentialCleared(state))
        {
            // Auto-clear ambient timers
            foreach (var t in state.Timers.Where(t => t.IsAmbient))
                t.Stopped = true;

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

        return AdvanceTurn(state, session, encounter);
    }

    // ── Turn advancement ──────────────────────────────────────

    static TacticalStep AdvanceTurn(TacticalState state, GameSession session, TacticalEncounter encounter)
    {
        state.Turn++;
        state.DigUsedThisTurn = false;

        var fired = new List<TimerFired>();

        // 1. Tick all ambient timers
        foreach (var timer in state.Timers.Where(t => t.IsAmbient && !t.Stopped))
        {
            var result = TickTimerOnce(timer, state, session);
            if (result != null) fired.Add(result);
        }

        // Check for fatal expiry after ambient ticks
        var fatalCheck = CheckFatalExpiry(state, encounter, session, fired);
        if (fatalCheck != null) return fatalCheck;

        // 2. Tick current sequential timer
        var seqTimer = GetCurrentSequentialTimer(state);
        if (seqTimer != null)
        {
            var result = TickTimerOnce(seqTimer, state, session);
            if (result != null) fired.Add(result);
        }

        // Check for fatal expiry after sequential tick (tick-timer cascades)
        fatalCheck = CheckFatalExpiry(state, encounter, session, fired);
        if (fatalCheck != null) return fatalCheck;

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

        // 3. Gain momentum (approach-dependent)
        int momentumGain = state.Approach == ApproachKind.Aggressive ? 2 : 1;
        state.Momentum += momentumGain;

        return StartTurn(state, session, encounter);
    }

    /// <summary>Tick a single timer's countdown. If it fires, apply effect and reset.</summary>
    static TimerFired? TickTimerOnce(ActiveTimer timer, TacticalState state, GameSession session)
    {
        timer.Current--;
        if (timer.Current > 0) return null;

        // Timer fired
        switch (timer.Effect)
        {
            case TimerEffect.Spirits:
                session.Player.Spirits = Math.Max(0, session.Player.Spirits - timer.Amount);
                break;
            case TimerEffect.Resistance:
                var seqTimer = GetCurrentSequentialTimer(state);
                if (seqTimer != null)
                    seqTimer.Resistance += timer.Amount;
                break;
            case TimerEffect.Condition:
                state.PendingConditions.Add(timer.ConditionId!);
                break;
            case TimerEffect.TickTimer:
                var target = state.Timers.FirstOrDefault(t => t.Name == timer.TicksTimerName);
                if (target != null)
                    target.Current -= timer.Amount;
                break;
            case TimerEffect.Fatal:
                // Don't reset — fatal fires once. CheckFatalExpiry handles the failure.
                return new TimerFired(timer.Name, timer.Effect, 0);
        }

        // Reset countdown (except fatal — it stays at 0)
        if (timer.Effect != TimerEffect.Fatal)
            timer.Current = timer.Countdown;

        return new TimerFired(timer.Name, timer.Effect, timer.Amount,
            timer.Effect == TimerEffect.Condition ? timer.ConditionId : null);
    }

    /// <summary>Check if any fatal timer has reached 0 (from direct countdown or tick-timer cascade).</summary>
    static TacticalStep.Finished? CheckFatalExpiry(
        TacticalState state, TacticalEncounter encounter, GameSession session, List<TimerFired> fired)
    {
        var expired = state.Timers.FirstOrDefault(t =>
            t.Effect == TimerEffect.Fatal && !t.Stopped && t.Current <= 0);
        if (expired == null) return null;

        state.LastTimersFired = fired;
        List<MechanicResult>? failureResults = null;
        if (encounter.Failure != null)
            failureResults = Mechanics.Apply(encounter.Failure.Mechanics, session.Player, session.Balance, session.Rng);
        return new TacticalStep.Finished(TacticalFinishReason.TimerExpired,
            FailureResults: failureResults,
            ConditionResults: ResolvePendingConditions(state, session));
    }

    static TacticalStep StartTurn(TacticalState state, GameSession session, TacticalEncounter encounter)
    {
        int drawCount = state.Approach == ApproachKind.Cautious ? 2 : 1;
        state.Openings = DrawOpenings(state, drawCount, session.Rng);
        EnsurePlayable(state, session);
        return MakeShowTurn(state, session, encounter);
    }

    static TacticalStep.ShowTurn MakeShowTurn(TacticalState state, GameSession session, TacticalEncounter encounter)
    {
        var timer = GetCurrentSequentialTimer(state);
        return new TacticalStep.ShowTurn(new TacticalTurnData(
            state.Turn,
            state.Momentum,
            session.Player.Spirits,
            timer?.Resistance ?? 0,
            encounter.Timers
                .Where(t => t.Resistance > 0)
                .ElementAtOrDefault(GetSequentialIndex(state))?.Resistance ?? 0,
            state.DigUsedThisTurn,
            state.Timers,
            state.CurrentTimerIndex,
            state.Openings,
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

    /// <summary>Get the current sequential (non-ambient) timer, or null if all are cleared.</summary>
    static ActiveTimer? GetCurrentSequentialTimer(TacticalState state)
    {
        for (int i = state.CurrentTimerIndex; i < state.Timers.Count; i++)
        {
            var t = state.Timers[i];
            if (t.IsAmbient || t.Stopped) continue;
            return t;
        }
        return null;
    }

    /// <summary>Get how many sequential timers have been passed (for ResistanceMax lookup).</summary>
    static int GetSequentialIndex(TacticalState state)
    {
        int idx = 0;
        for (int i = 0; i < state.CurrentTimerIndex && i < state.Timers.Count; i++)
        {
            if (!state.Timers[i].IsAmbient) idx++;
        }
        return idx;
    }

    static bool AllSequentialCleared(TacticalState state) =>
        state.Timers.Where(t => !t.IsAmbient).All(t => t.Stopped || t.Resistance <= 0);

    static void AdvancePastClearedTimers(TacticalState state)
    {
        while (state.CurrentTimerIndex < state.Timers.Count)
        {
            var t = state.Timers[state.CurrentTimerIndex];
            if (t.IsAmbient)
            {
                state.CurrentTimerIndex++;
                continue;
            }
            if (t.Resistance > 0)
                break;
            t.Stopped = true;
            state.CurrentTimerIndex++;
        }
    }

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

        var timer = GetCurrentSequentialTimer(state);
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
                TicksTimerName = def.TicksTimerName,
                IsAmbient = def.Resistance == 0,
            });
        }

        // Advance CurrentTimerIndex to first non-ambient timer
        while (state.CurrentTimerIndex < state.Timers.Count
            && state.Timers[state.CurrentTimerIndex].IsAmbient)
        {
            state.CurrentTimerIndex++;
        }
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
}
