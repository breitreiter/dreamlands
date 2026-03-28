using Dreamlands.Game;
using Dreamlands.Tactical;

namespace Dreamlands.Orchestration;

// ── Step results ───────────────────────────────────────────────────

public abstract record TacticalStep
{
    /// <summary>Combat only: player must choose an approach before the encounter starts.</summary>
    public record ChooseApproach(
        TacticalEncounter Encounter,
        IReadOnlyList<ApproachDef> Approaches) : TacticalStep;

    /// <summary>Present the current turn state. Player must act.</summary>
    public record ShowTurn(TacticalTurnData Data) : TacticalStep;

    /// <summary>Encounter ended. Includes failure mechanics if spirits hit 0.</summary>
    public record Finished(TacticalFinishReason Reason, List<MechanicResult>? FailureResults = null) : TacticalStep;
}

public enum TacticalFinishReason { ResistanceKill, ControlKill, SpiritsLoss }

/// <summary>All data the UI needs to render a tactical turn.</summary>
public record TacticalTurnData(
    int Turn,
    int Resistance,
    int Momentum,
    int PlayerSpirits,
    IReadOnlyList<ActiveTimer> Timers,
    IReadOnlyList<OpeningSnapshot> Openings,
    IReadOnlyList<OpeningSnapshot>? Queue,
    IReadOnlyList<TimerFired> TimersFired);

public record TimerFired(string Name, TimerEffect Effect, int Amount);

// ── Player actions ─────────────────────────────────────────────────

public enum TacticalAction { TakeOpening, PressAdvantage, ForceOpening }

// ── Runner ─────────────────────────────────────────────────────────

public static class TacticalRunner
{
    const int PressAdvantageCost = 2;
    const int ForceOpeningCost = 2;
    const int BonusOpeningCount = 3;
    const int MomentumPerTurn = 1;

    /// <summary>
    /// Start a tactical encounter. For combat, returns ChooseApproach.
    /// For traverse, initializes state and returns the first turn.
    /// </summary>
    public static TacticalStep Begin(GameSession session, TacticalEncounter encounter, TacticalState state)
    {
        state.EncounterId = encounter.Id;
        state.Resistance = encounter.Resistance;
        state.Turn = 0;

        // Build the opening pool, filtering by player gear
        state.Pool = BuildPool(encounter.Openings, session.Player, session.Balance);

        if (encounter.Variant == Variant.Combat)
        {
            if (encounter.Approaches.Count > 0)
                return new TacticalStep.ChooseApproach(encounter, encounter.Approaches);

            // No approaches defined — use defaults
            state.Momentum = encounter.Momentum ?? 0;
            DrawTimers(state, encounter, encounter.TimerDraw, session.Rng);
            return StartTurn(state, session, encounter);
        }

        // Traverse: no approach selection
        state.Momentum = 0;
        DrawTimers(state, encounter, encounter.TimerDraw, session.Rng);

        // Build the visible queue
        var queueDepth = encounter.QueueDepth ?? 5;
        state.Queue = [];
        for (int i = 0; i < queueDepth; i++)
            state.Queue.Add(RandomOpening(state.Pool, session.Rng));

        return StartTurn(state, session, encounter);
    }

    /// <summary>Apply the chosen approach (combat only), then start turn 1.</summary>
    public static TacticalStep ApplyApproach(
        GameSession session, TacticalEncounter encounter, TacticalState state, ApproachKind approach)
    {
        var def = encounter.Approaches.FirstOrDefault(a => a.Kind == approach);
        if (def == null)
            throw new ArgumentException($"Invalid approach: {approach}");

        state.Momentum = def.Momentum;
        DrawTimers(state, encounter, def.TimerCount, session.Rng);

        if (def.BonusOpenings > 0)
            state.BonusNextTurn = true;

        return StartTurn(state, session, encounter);
    }

    /// <summary>Process a player action and advance the encounter.</summary>
    public static TacticalStep Act(
        GameSession session, TacticalEncounter encounter, TacticalState state,
        TacticalAction action, int openingIndex = 0)
    {
        switch (action)
        {
            case TacticalAction.TakeOpening:
                return TakeOpening(session, encounter, state, openingIndex);

            case TacticalAction.PressAdvantage:
                if (state.Momentum < PressAdvantageCost)
                    throw new InvalidOperationException("Not enough momentum to Press the Advantage.");
                state.Momentum -= PressAdvantageCost;
                state.BonusNextTurn = true;
                return AdvanceTurn(state, session, encounter);

            case TacticalAction.ForceOpening:
                if (session.Player.Spirits < ForceOpeningCost)
                    throw new InvalidOperationException("Not enough spirits to Force an Opening.");
                session.Player.Spirits -= ForceOpeningCost;
                state.BonusNextTurn = true;
                return AdvanceTurn(state, session, encounter);

            default:
                throw new ArgumentOutOfRangeException(nameof(action));
        }
    }

    // ── Internal ───────────────────────────────────────────────────

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
            case CostKind.Free:
                break;
        }

        // Apply effect
        switch (opening.EffectKind)
        {
            case EffectKind.Damage:
                state.Resistance = Math.Max(0, state.Resistance - opening.EffectAmount);
                break;
            case EffectKind.StopTimer:
                StopMostUrgentTimer(state);
                break;
            case EffectKind.Momentum:
                state.Momentum += opening.EffectAmount;
                break;
        }

        // Check win conditions
        if (state.Resistance <= 0)
            return new TacticalStep.Finished(TacticalFinishReason.ResistanceKill);

        if (state.Timers.All(t => t.Stopped))
            return new TacticalStep.Finished(TacticalFinishReason.ControlKill);

        // For traverse, advance the queue
        if (state.Queue != null && state.Queue.Count > 0)
            state.Queue.RemoveAt(0);

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
                // Timer fires
                fired.Add(new TimerFired(timer.Name, timer.Effect, timer.Amount));
                switch (timer.Effect)
                {
                    case TimerEffect.Spirits:
                        session.Player.Spirits = Math.Max(0, session.Player.Spirits - timer.Amount);
                        break;
                    case TimerEffect.Resistance:
                        state.Resistance += timer.Amount;
                        break;
                }
                // Reset
                timer.Current = timer.Countdown;
            }
        }

        // Check spirits loss
        if (session.Player.Spirits <= 0)
        {
            List<MechanicResult>? failureResults = null;
            if (encounter.Failure != null)
                failureResults = Mechanics.Apply(encounter.Failure.Mechanics, session.Player, session.Balance, session.Rng);
            return new TacticalStep.Finished(TacticalFinishReason.SpiritsLoss, failureResults);
        }

        // Passive momentum gain
        state.Momentum += MomentumPerTurn;

        return StartTurn(state, session, encounter);
    }

    static TacticalStep StartTurn(TacticalState state, GameSession session, TacticalEncounter encounter)
    {
        // Generate openings for this turn
        int count = state.BonusNextTurn ? BonusOpeningCount : 1;
        state.BonusNextTurn = false;

        if (encounter.Variant == Variant.Traverse && state.Queue != null)
        {
            // Traverse: front of queue is the opening, replenish the back
            if (state.Queue.Count > 0)
            {
                state.Openings = [state.Queue[0]];
                // If bonus turn, also draw extra ephemeral openings
                for (int i = 1; i < count; i++)
                    state.Openings.Add(RandomOpening(state.Pool, session.Rng));
            }
            else
            {
                state.Openings = GenerateOpenings(state.Pool, count, session.Rng);
            }

            // Maintain queue depth
            var targetDepth = encounter.QueueDepth ?? 5;
            while (state.Queue.Count < targetDepth)
                state.Queue.Add(RandomOpening(state.Pool, session.Rng));
        }
        else
        {
            // Combat: random openings each turn
            state.Openings = GenerateOpenings(state.Pool, count, session.Rng);
        }

        return new TacticalStep.ShowTurn(new TacticalTurnData(
            state.Turn,
            state.Resistance,
            state.Momentum,
            session.Player.Spirits,
            state.Timers,
            state.Openings,
            state.Queue,
            []));
    }

    static List<OpeningSnapshot> BuildPool(
        IReadOnlyList<OpeningDef> openings, PlayerState player, Rules.BalanceData balance)
    {
        var pool = new List<OpeningSnapshot>();
        for (int i = 0; i < openings.Count; i++)
        {
            var o = openings[i];
            // Filter by requires (gear gate)
            if (o.Requires != null && !Conditions.Evaluate(o.Requires, player, balance, new Random(0)))
                continue;

            pool.Add(new OpeningSnapshot
            {
                PoolIndex = i,
                Name = o.Name,
                CostKind = o.Cost.Kind,
                CostAmount = o.Cost.Amount,
                EffectKind = o.Effect.Kind,
                EffectAmount = o.Effect.Amount,
            });
        }
        return pool;
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
                Effect = def.Effect,
                Amount = def.Amount,
                Countdown = def.Countdown,
                Current = def.Countdown,
                Stopped = false,
            });
        }
    }

    static List<OpeningSnapshot> GenerateOpenings(List<OpeningSnapshot> pool, int count, Random rng)
    {
        if (pool.Count == 0) return [];
        var result = new List<OpeningSnapshot>(count);
        for (int i = 0; i < count; i++)
            result.Add(pool[rng.Next(pool.Count)]);
        return result;
    }

    static OpeningSnapshot RandomOpening(List<OpeningSnapshot> pool, Random rng) =>
        pool[rng.Next(pool.Count)];

    static void TickRandomTimer(TacticalState state, Random rng)
    {
        var active = state.Timers.Where(t => !t.Stopped).ToList();
        if (active.Count > 0)
            active[rng.Next(active.Count)].Current--;
    }

    static void StopMostUrgentTimer(TacticalState state)
    {
        var target = state.Timers
            .Where(t => !t.Stopped)
            .OrderBy(t => t.Current)
            .FirstOrDefault();
        if (target != null)
            target.Stopped = true;
    }
}
