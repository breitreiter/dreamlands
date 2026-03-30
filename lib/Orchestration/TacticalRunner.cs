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

    public record Finished(TacticalFinishReason Reason, List<MechanicResult>? FailureResults = null, List<MechanicResult>? ConditionResults = null) : TacticalStep;
}

public enum TacticalFinishReason { ResistanceKill, ControlKill, SpiritsLoss }

public record TacticalTurnData(
    int Turn,
    int Resistance,
    int ResistanceMax,
    int Momentum,
    int PlayerSpirits,
    IReadOnlyList<ActiveTimer> Timers,
    IReadOnlyList<OpeningSnapshot> Openings,
    IReadOnlyList<OpeningSnapshot>? Queue,
    IReadOnlyList<TimerFired> TimersFired,
    IReadOnlyList<string> PendingConditions);

public record TimerFired(string Name, TimerEffect Effect, int Amount, string? ConditionId = null);

public enum TacticalAction { TakeOpening, PressAdvantage, ForceOpening }

// ── Runner ─────────────────────────────────────────────────────────

public static class TacticalRunner
{
    const int PressAdvantageCost = 2;
    const int ForceOpeningCost = 2;
    const int BonusOpeningCount = 3;
    const int MomentumPerTurn = 1;

    public static TacticalStep Begin(GameSession session, TacticalEncounter encounter, TacticalState state)
    {
        state.EncounterId = encounter.Id;
        state.Resistance = encounter.Resistance;
        state.Turn = 0;

        if (encounter.Variant == Variant.Combat)
        {
            if (encounter.Approaches.Count > 0)
                return new TacticalStep.ChooseApproach(encounter, encounter.Approaches);

            // No approaches — start with 0 momentum
            state.Momentum = 0;
            DrawTimers(state, encounter, encounter.TimerDraw, session.Rng);
            BuildDeck(state, session, encounter);
            return StartTurn(state, session, encounter);
        }

        // Traverse: no approach selection
        state.Momentum = 0;
        state.PathIndex = 0;
        DrawTimers(state, encounter, encounter.TimerDraw, session.Rng);
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

    public static TacticalStep ApplyApproach(
        GameSession session, TacticalEncounter encounter, TacticalState state, ApproachKind approach)
    {
        var def = encounter.Approaches.FirstOrDefault(a => a.Kind == approach)
            ?? throw new ArgumentException($"Invalid approach: {approach}");

        state.Momentum = def.Momentum;
        DrawTimers(state, encounter, def.TimerCount, session.Rng);
        BuildDeck(state, session, encounter);

        if (def.BonusOpenings > 0)
            state.BonusNextTurn = true;

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

    // ── Internal ───────────────────────────────────────

    static TacticalStep PressOrForce(GameSession session, TacticalEncounter encounter, TacticalState state, bool isMomentum)
    {
        if (isMomentum)
        {
            if (state.Momentum < PressAdvantageCost)
                throw new InvalidOperationException("Not enough momentum to Press the Advantage.");
            state.Momentum -= PressAdvantageCost;
        }
        else
        {
            if (session.Player.Spirits < ForceOpeningCost)
                throw new InvalidOperationException("Not enough spirits to Force an Opening.");
            session.Player.Spirits -= ForceOpeningCost;
        }
        state.BonusNextTurn = true;
        return AdvanceTurn(state, session, encounter);
    }

    static TacticalStep TakeOpening(
        GameSession session, TacticalEncounter encounter, TacticalState state, int index)
    {
        if (index < 0 || index >= state.Openings.Count)
            throw new ArgumentOutOfRangeException(nameof(index));

        var opening = state.Openings[index];

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
                TickRandomTimer(state, session.Rng);
                break;
        }

        // Apply effect
        switch (opening.EffectKind)
        {
            case EffectKind.Damage:
                state.Resistance = Math.Max(0, state.Resistance - opening.EffectAmount);
                break;
            case EffectKind.StopTimer:
                StopTimer(state, opening.StopsTimerIndex);
                break;
            case EffectKind.Momentum:
                state.Momentum += opening.EffectAmount;
                break;
        }

        // Check win conditions
        if (state.Resistance <= 0)
            return new TacticalStep.Finished(TacticalFinishReason.ResistanceKill,
                ConditionResults: ResolvePendingConditions(state, session));
        if (state.Timers.All(t => t.Stopped))
            return new TacticalStep.Finished(TacticalFinishReason.ControlKill,
                ConditionResults: ResolvePendingConditions(state, session));

        // Traverse: advance path
        if (encounter.Variant == Variant.Traverse && state.Queue != null && state.Queue.Count > 0)
        {
            state.PathIndex++;
            state.Queue.RemoveAt(0);
        }

        return AdvanceTurn(state, session, encounter);
    }

    static TacticalStep AdvanceTurn(TacticalState state, GameSession session, TacticalEncounter encounter)
    {
        state.Turn++;

        // Tick all active timers
        var fired = new List<TimerFired>();
        foreach (var timer in state.Timers.Where(t => !t.Stopped))
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
                        state.Resistance += timer.Amount;
                        fired.Add(new TimerFired(timer.Name, timer.Effect, timer.Amount));
                        break;
                    case TimerEffect.Condition:
                        state.PendingConditions.Add(timer.ConditionId!);
                        fired.Add(new TimerFired(timer.Name, timer.Effect, 0, timer.ConditionId));
                        break;
                }
                timer.Current = timer.Countdown;
            }
        }
        state.LastTimersFired = fired;

        // Spirits loss check
        if (session.Player.Spirits <= 0)
        {
            List<MechanicResult>? failureResults = null;
            if (encounter.Failure != null)
                failureResults = Mechanics.Apply(encounter.Failure.Mechanics, session.Player, session.Balance, session.Rng);
            var conditionResults = ResolvePendingConditions(state, session);
            return new TacticalStep.Finished(TacticalFinishReason.SpiritsLoss, failureResults, conditionResults);
        }

        // Passive momentum gain
        state.Momentum += MomentumPerTurn;

        return StartTurn(state, session, encounter);
    }

    static TacticalStep StartTurn(TacticalState state, GameSession session, TacticalEncounter encounter)
    {
        int count = state.BonusNextTurn ? BonusOpeningCount : 1;
        state.BonusNextTurn = false;

        if (encounter.Variant == Variant.Traverse && state.Queue != null)
        {
            // Traverse: current opening comes from the path
            if (state.Queue.Count > 0)
            {
                state.Openings = [state.Queue[0]];
                // Bonus openings are drawn from deck (detour)
                for (int i = 1; i < count; i++)
                    state.Openings.Add(ReThemeStopTimer(DeckBuilder.Draw(state, session.Rng), state));
            }
            else
            {
                // Path exhausted — draw from deck
                state.Openings = DrawOpenings(state, count, session.Rng);
            }
        }
        else
        {
            // Combat: draw from deck
            state.Openings = DrawOpenings(state, count, session.Rng);
        }

        return new TacticalStep.ShowTurn(new TacticalTurnData(
            state.Turn,
            state.Resistance,
            encounter.Resistance,
            state.Momentum,
            session.Player.Spirits,
            state.Timers,
            state.Openings,
            VisibleQueue(state, session, encounter),
            state.LastTimersFired,
            state.PendingConditions));
    }

    static List<OpeningSnapshot> DrawOpenings(TacticalState state, int count, Random rng)
    {
        var result = new List<OpeningSnapshot>(count);
        for (int i = 0; i < count; i++)
            result.Add(ReThemeStopTimer(DeckBuilder.Draw(state, rng), state));
        return result;
    }

    /// <summary>If a stop_timer card is drawn, assign it to the most urgent active timer.</summary>
    static OpeningSnapshot ReThemeStopTimer(OpeningSnapshot card, TacticalState state)
    {
        if (card.EffectKind != EffectKind.StopTimer) return card;

        var target = state.Timers
            .Select((t, i) => (Timer: t, Index: i))
            .Where(x => !x.Timer.Stopped)
            .OrderBy(x => x.Timer.Current)
            .FirstOrDefault();

        if (target.Timer == null) return card;

        card.StopsTimerIndex = target.Index;
        card.Name = target.Timer.CounterName ?? card.Name;
        return card;
    }

    static void BuildDeck(TacticalState state, GameSession session, TacticalEncounter encounter)
    {
        state.Deck = DeckBuilder.Build(encounter, session.Player, session.Balance, state.Timers, session.Rng);
        state.DrawIndex = 0;
    }

    static void DrawTimers(TacticalState state, TacticalEncounter encounter, int count, Random rng)
    {
        var available = encounter.Timers.ToList();
        state.Timers = [];
        for (int i = 0; i < count && available.Count > 0; i++)
        {
            var idx = rng.Next(available.Count);
            var def = available[idx];
            available.RemoveAt(idx);
            state.Timers.Add(new ActiveTimer
            {
                Name = def.Name,
                CounterName = def.CounterName,
                Effect = def.Effect,
                Amount = def.Amount,
                Countdown = def.Countdown,
                Current = def.Countdown,
                Stopped = false,
                ConditionId = def.ConditionId,
            });
        }
    }

    static void TickRandomTimer(TacticalState state, Random rng)
    {
        var active = state.Timers.Where(t => !t.Stopped).ToList();
        if (active.Count > 0)
            active[rng.Next(active.Count)].Current--;
    }

    static void StopTimer(TacticalState state, int? timerIndex)
    {
        if (timerIndex.HasValue && timerIndex.Value < state.Timers.Count)
            state.Timers[timerIndex.Value].Stopped = true;
        else
        {
            // Fallback: stop most urgent
            var target = state.Timers.Where(t => !t.Stopped).OrderBy(t => t.Current).FirstOrDefault();
            if (target != null) target.Stopped = true;
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
                if (!lastCheck.Passed)
                {
                    anyFailed = true;
                    break;
                }
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
