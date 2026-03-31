using Dreamlands.Orchestration;
using Dreamlands.Rules;
using Dreamlands.Tactical;

namespace TacticalSim;

record BotAction(TacticalAction Action, int OpeningIndex = 0);

record Bot(string Name, Func<TacticalTurnData, TacticalEncounter, TacticalBalance, BotAction> Strategy, Func<IReadOnlyList<ApproachDef>, ApproachKind> ApproachSelect);

static class Bots
{
    public static readonly Bot[] All =
    [
        new("Conservative", Conservative, _ => ApproachKind.Scout),
        new("Competent", Competent, _ => ApproachKind.Direct),
        new("Aggressive", Aggressive, approaches => approaches.Any(a => a.Kind == ApproachKind.Wild) ? ApproachKind.Wild : ApproachKind.Direct),
        new("CancelFirst", CancelFirst, _ => ApproachKind.Scout),
    ];

    // ── Conservative ──────────────────────────────────────────────
    // Never Force. Press when affordable. Take best affordable opening.

    static BotAction Conservative(TacticalTurnData turn, TacticalEncounter enc, TacticalBalance tb)
    {
        // Take best affordable opening (prefer damage, then stop_timer, then momentum)
        var best = BestAffordable(turn, preferCancel: false);
        if (best >= 0)
            return new BotAction(TacticalAction.TakeOpening, best);

        // Press if we can
        if (turn.Momentum >= tb.PressAdvantageCost)
            return new BotAction(TacticalAction.PressAdvantage);

        // Take any affordable opening
        return TakeAnyAffordableOrForce(turn, tb);
    }

    // ── Competent ─────────────────────────────────────────────────
    // Cancel > finish kill > highest progress > Press > Force (when safe) > free card

    static BotAction Competent(TacticalTurnData turn, TacticalEncounter enc, TacticalBalance tb)
    {
        bool hasUnstoppedTimers = turn.Timers.Any(t => !t.Stopped);

        // 1. Cancel if available and there are unstopped timers
        if (hasUnstoppedTimers)
        {
            var cancel = FindCancel(turn);
            if (cancel >= 0)
                return new BotAction(TacticalAction.TakeOpening, cancel);
        }

        // 2. If a progress card can finish the encounter, take it
        var finisher = FindFinisher(turn);
        if (finisher >= 0)
            return new BotAction(TacticalAction.TakeOpening, finisher);

        // 3. Take highest affordable damage card
        var damage = BestAffordableDamage(turn);
        if (damage >= 0)
            return new BotAction(TacticalAction.TakeOpening, damage);

        // 4. Press if momentum >= 3
        if (turn.Momentum >= 3 && turn.Momentum >= tb.PressAdvantageCost)
            return new BotAction(TacticalAction.PressAdvantage);

        // 5. Force if spirits healthy and momentum low
        if (turn.PlayerSpirits > 6 && turn.Momentum < 2)
            return new BotAction(TacticalAction.ForceOpening);

        // 6. Take best free/cheap card (momentum builder)
        var free = BestFreeCard(turn);
        if (free >= 0)
            return new BotAction(TacticalAction.TakeOpening, free);

        // 7. Press if affordable
        if (turn.Momentum >= tb.PressAdvantageCost)
            return new BotAction(TacticalAction.PressAdvantage);

        // Fallback
        return TakeAnyAffordableOrForce(turn, tb);
    }

    // ── Aggressive ────────────────────────────────────────────────
    // Race timers. Force liberally. Prioritize spirits-cost damage cards.

    static BotAction Aggressive(TacticalTurnData turn, TacticalEncounter enc, TacticalBalance tb)
    {
        // 1. If a progress card can finish, take it
        var finisher = FindFinisher(turn);
        if (finisher >= 0)
            return new BotAction(TacticalAction.TakeOpening, finisher);

        // 2. Take highest damage card (including spirits-cost)
        var damage = BestAffordableDamage(turn);
        if (damage >= 0)
            return new BotAction(TacticalAction.TakeOpening, damage);

        // 3. Force if momentum low (aggressive spirits spend)
        if (turn.Momentum < 2 && turn.PlayerSpirits > tb.ForceOpeningCost)
            return new BotAction(TacticalAction.ForceOpening);

        // 4. Press if affordable
        if (turn.Momentum >= tb.PressAdvantageCost)
            return new BotAction(TacticalAction.PressAdvantage);

        // 5. Take whatever
        return TakeAnyAffordableOrForce(turn, tb);
    }

    // ── CancelFirst ───────────────────────────────────────────────
    // Stop all timers before any progress. Conservative with spirits.

    static BotAction CancelFirst(TacticalTurnData turn, TacticalEncounter enc, TacticalBalance tb)
    {
        bool hasUnstoppedTimers = turn.Timers.Any(t => !t.Stopped);

        // 1. Always take cancel if available and timers remain
        if (hasUnstoppedTimers)
        {
            var cancel = FindCancel(turn);
            if (cancel >= 0)
                return new BotAction(TacticalAction.TakeOpening, cancel);
        }

        // 2. If all timers stopped (or none), take best damage
        if (!hasUnstoppedTimers)
        {
            var damage = BestAffordableDamage(turn);
            if (damage >= 0)
                return new BotAction(TacticalAction.TakeOpening, damage);
        }

        // 3. Press to find cancel cards
        if (hasUnstoppedTimers && turn.Momentum >= tb.PressAdvantageCost)
            return new BotAction(TacticalAction.PressAdvantage);

        // 4. Take momentum builder
        var free = BestFreeCard(turn);
        if (free >= 0)
            return new BotAction(TacticalAction.TakeOpening, free);

        // 5. Press/take whatever
        if (turn.Momentum >= tb.PressAdvantageCost)
            return new BotAction(TacticalAction.PressAdvantage);

        return TakeAnyAffordableOrForce(turn, tb);
    }

    // ── Helpers ───────────────────────────────────────────────────

    /// <summary>Last-resort: take any affordable opening, or Force/Press to get new cards.</summary>
    static BotAction TakeAnyAffordableOrForce(TacticalTurnData turn, TacticalBalance tb)
    {
        for (int i = 0; i < turn.Openings.Count; i++)
        {
            if (CanAfford(turn.Openings[i], turn))
                return new BotAction(TacticalAction.TakeOpening, i);
        }
        // Nothing affordable — must press or force to get new cards
        if (turn.Momentum >= tb.PressAdvantageCost)
            return new BotAction(TacticalAction.PressAdvantage);
        if (turn.PlayerSpirits > tb.ForceOpeningCost)
            return new BotAction(TacticalAction.ForceOpening);
        // Truly stuck — take opening 0 and hope (will likely lose)
        return new BotAction(TacticalAction.TakeOpening, 0);
    }

    static int FindCancel(TacticalTurnData turn)
    {
        int best = -1;
        for (int i = 0; i < turn.Openings.Count; i++)
        {
            var o = turn.Openings[i];
            if (o.EffectKind != EffectKind.StopTimer) continue;
            if (!CanAfford(o, turn)) continue;
            // Prefer cheapest cancel
            if (best < 0 || CostValue(o) < CostValue(turn.Openings[best]))
                best = i;
        }
        return best;
    }

    static int FindFinisher(TacticalTurnData turn)
    {
        for (int i = 0; i < turn.Openings.Count; i++)
        {
            var o = turn.Openings[i];
            if (o.EffectKind == EffectKind.Damage && o.EffectAmount >= turn.Resistance && CanAfford(o, turn))
                return i;
        }
        return -1;
    }

    static int BestAffordableDamage(TacticalTurnData turn)
    {
        int best = -1;
        int bestDmg = 0;
        for (int i = 0; i < turn.Openings.Count; i++)
        {
            var o = turn.Openings[i];
            if (o.EffectKind != EffectKind.Damage) continue;
            if (!CanAfford(o, turn)) continue;
            if (o.EffectAmount > bestDmg)
            {
                best = i;
                bestDmg = o.EffectAmount;
            }
        }
        return best;
    }

    static int BestAffordable(TacticalTurnData turn, bool preferCancel)
    {
        int best = -1;
        int bestScore = -1;
        for (int i = 0; i < turn.Openings.Count; i++)
        {
            var o = turn.Openings[i];
            if (!CanAfford(o, turn)) continue;
            int score = o.EffectKind switch
            {
                EffectKind.StopTimer => preferCancel ? 100 : 50,
                EffectKind.Damage => o.EffectAmount + 10,
                EffectKind.Momentum => o.EffectAmount,
                _ => 0,
            };
            if (score > bestScore)
            {
                best = i;
                bestScore = score;
            }
        }
        return best;
    }

    static int BestFreeCard(TacticalTurnData turn)
    {
        int best = -1;
        int bestEffect = -1;
        for (int i = 0; i < turn.Openings.Count; i++)
        {
            var o = turn.Openings[i];
            if (o.CostKind != CostKind.Free) continue;
            if (o.EffectAmount > bestEffect)
            {
                best = i;
                bestEffect = o.EffectAmount;
            }
        }
        return best;
    }

    static bool CanAfford(OpeningSnapshot o, TacticalTurnData turn) => o.CostKind switch
    {
        CostKind.Free => true,
        CostKind.Momentum => turn.Momentum >= o.CostAmount,
        CostKind.Spirits => turn.PlayerSpirits > o.CostAmount, // strict > to avoid killing ourselves
        CostKind.Tick => true,
        _ => false,
    };

    static int CostValue(OpeningSnapshot o) => o.CostKind switch
    {
        CostKind.Free => 0,
        CostKind.Momentum => o.CostAmount,
        CostKind.Spirits => o.CostAmount * 3, // spirits worth more than momentum
        CostKind.Tick => 2,
        _ => 99,
    };
}
