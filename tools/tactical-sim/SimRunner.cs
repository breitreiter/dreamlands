using System.Collections.Concurrent;
using Dreamlands.Orchestration;
using Dreamlands.Rules;
using Dreamlands.Tactical;

namespace TacticalSim;

// ── Per-turn vibe snapshot ──────────────────────────────────────

record TurnVibe(
    int Turn,
    double Choice,      // 0 = obvious pick, 1 = genuinely hard decision
    double Tension,      // 0 = timer safe, 1 = about to fire
    double Juice,        // 0 = chaff, 1 = haymaker/cancel
    double Weight,       // rolling frustration (negative = bad streak)
    double Triumph,      // 0 or 1 — cleared a timer this turn
    string CardPlayed,   // archetype of card played
    string Action,       // "play", "press", "force"
    bool TimerFired,
    int SpiritsLost,
    bool Conditioned);

record RunVibes(List<TurnVibe> Turns, bool Won, int SpiritsSpent);

// ── Juice ratings ───────────────────────────────────────────────

static class JuiceRatings
{
    static readonly Dictionary<string, double> Ratings = new()
    {
        ["free_progress_small"] = 0.10,
        ["free_momentum_small"] = 0.10,
        ["free_momentum"] = 0.25,
        ["momentum_to_progress"] = 0.40,
        ["spirits_to_momentum"] = 0.20,
        ["threat_to_progress"] = 0.60,
        ["threat_to_progress_large"] = 0.75,
        ["momentum_to_progress_large"] = 0.70,
        ["spirits_to_progress"] = 0.55,
        ["momentum_to_progress_huge"] = 1.00,
        ["momentum_to_cancel"] = 0.85,
        ["spirits_to_cancel"] = 0.80,
        ["free_cancel"] = 1.00,
    };

    public static double Of(OpeningSnapshot o) => o.EffectKind == EffectKind.StopTimer
        ? 0.90
        : Ratings.GetValueOrDefault(o.Name, 0.3);
}

// ── Runner ──────────────────────────────────────────────────────

static class SimRunner
{
    const int MaxTurns = 30;

    public static List<RunVibes> Run(string[] deckSpec, ApproachKind approach, int runs, int baseSeed = 42)
    {
        var balance = BalanceData.Default;
        var scenario = Scenarios.Platonic;
        var bag = new ConcurrentBag<RunVibes>();

        Parallel.For(0, runs, i =>
        {
            int seed = baseSeed + i;
            var result = RunOne(deckSpec, scenario, approach, balance, seed);
            bag.Add(result);
        });

        return [.. bag];
    }

    static RunVibes RunOne(string[] deckSpec, EncounterScenario scenario, ApproachKind approach,
        BalanceData balance, int seed)
    {
        var session = Scenarios.BuildSession(seed, balance);
        var encounter = Scenarios.BuildEncounter(scenario);
        var state = new TacticalState();
        var tb = balance.Tactical;

        int spiritsStart = session.Player.Spirits;
        var step = TacticalRunner.Begin(session, encounter, state);

        bool deckInjected = false;
        var vibes = new List<TurnVibe>();
        int badStreak = 0;
        int turn = 0;
        int prevActiveTimers = -1;

        while (true)
        {
            if (!deckInjected && step is not TacticalStep.ChooseApproach)
            {
                var deckRng = new Random(seed + 99);
                state.Deck = Scenarios.BuildDeck(deckSpec, balance, deckRng);
                state.DrawIndex = 0;
                deckInjected = true;
            }

            switch (step)
            {
                case TacticalStep.ChooseApproach:
                    step = TacticalRunner.ApplyApproach(session, encounter, state, approach);
                    break;

                case TacticalStep.ShowTurn show:
                    if (++turn > MaxTurns)
                        return new RunVibes(vibes, false, spiritsStart - session.Player.Spirits);

                    var td = show.Data;
                    int preSpirits = session.Player.Spirits;
                    int activeTimers = td.Timers.Count(t => !t.Stopped);

                    // Detect timer fire from previous turn
                    bool timerFired = false;
                    bool conditioned = false;
                    // (Timer fire detection happens via spirits loss after act)

                    // Choose action
                    var (action, idx, label) = ChooseAction(td, approach, tb);

                    // Measure vibe before acting
                    double choice = MeasureChoice(td);
                    double tension = MeasureTension(td);

                    // Act
                    try
                    {
                        step = TacticalRunner.Act(session, encounter, state, action, idx);
                    }
                    catch (InvalidOperationException)
                    {
                        return new RunVibes(vibes, false, spiritsStart - session.Player.Spirits);
                    }

                    // Measure what happened
                    int spiritsLost = preSpirits - session.Player.Spirits;
                    var played = idx >= 0 && idx < td.Openings.Count ? td.Openings[idx] : null;
                    double juice = played != null ? JuiceRatings.Of(played) : 0.1;

                    // Triumph: did we advance to the next timer or finish?
                    double triumph = 0.0;
                    if (step is TacticalStep.Finished)
                    {
                        triumph = 1.0;
                    }
                    else if (step is TacticalStep.ShowTurn next)
                    {
                        int newActive = next.Data.Timers.Count(t => !t.Stopped);
                        if (prevActiveTimers >= 0 && newActive < activeTimers)
                            triumph = 1.0;
                    }
                    prevActiveTimers = activeTimers;

                    // Rolling weight
                    bool isBad = juice < 0.25 || label is "force" or "stuck";
                    badStreak = isBad ? badStreak + 1 : 0;
                    double weight = badStreak == 0 ? 0.0
                        : -0.2 * badStreak - 0.1 * Math.Max(0, badStreak - 1);

                    vibes.Add(new TurnVibe(turn, choice, tension, juice, weight, triumph,
                        played?.Name ?? "none", label, timerFired, spiritsLost, conditioned));
                    break;

                case TacticalStep.Finished fin:
                    bool won = fin.Reason != TacticalFinishReason.SpiritsLoss;
                    return new RunVibes(vibes, won, spiritsStart - session.Player.Spirits);

                default:
                    throw new InvalidOperationException($"Unexpected step: {step.GetType().Name}");
            }
        }
    }

    // ── Bot logic ───────────────────────────────────────────────

    static (TacticalAction Action, int Idx, string Label) ChooseAction(
        TacticalTurnData turn, ApproachKind approach, TacticalBalance tb)
    {
        if (approach == ApproachKind.Scout)
            return CancelBot(turn, tb);
        return AggroBot(turn, tb);
    }

    static (TacticalAction, int, string) CancelBot(TacticalTurnData turn, TacticalBalance tb)
    {
        var cancel = FindCancel(turn);
        if (cancel >= 0) return (TacticalAction.TakeOpening, cancel, "play");

        if (turn.Momentum >= tb.PressAdvantageCost)
            return (TacticalAction.PressAdvantage, 0, "press");

        var best = BestAffordable(turn);
        if (best >= 0) return (TacticalAction.TakeOpening, best, "play");

        if (turn.PlayerSpirits > tb.ForceOpeningCost)
            return (TacticalAction.ForceOpening, 0, "force");

        return (TacticalAction.TakeOpening, 0, "stuck");
    }

    static (TacticalAction, int, string) AggroBot(TacticalTurnData turn, TacticalBalance tb)
    {
        var finisher = FindFinisher(turn);
        if (finisher >= 0) return (TacticalAction.TakeOpening, finisher, "play");

        var damage = BestDamage(turn);
        if (damage >= 0) return (TacticalAction.TakeOpening, damage, "play");

        var best = BestAffordable(turn);
        if (best >= 0) return (TacticalAction.TakeOpening, best, "play");

        if (turn.Momentum >= tb.PressAdvantageCost)
            return (TacticalAction.PressAdvantage, 0, "press");

        if (turn.PlayerSpirits > tb.ForceOpeningCost)
            return (TacticalAction.ForceOpening, 0, "force");

        return (TacticalAction.TakeOpening, 0, "stuck");
    }

    // ── Card finders ────────────────────────────────────────────

    static int FindCancel(TacticalTurnData turn)
    {
        int best = -1, bestCost = int.MaxValue;
        for (int i = 0; i < turn.Openings.Count; i++)
        {
            var o = turn.Openings[i];
            if (o.EffectKind != EffectKind.StopTimer || !CanAfford(o, turn)) continue;
            int cost = o.CostKind == CostKind.Free ? 0 : o.CostAmount;
            if (cost < bestCost) { best = i; bestCost = cost; }
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

    static int BestDamage(TacticalTurnData turn)
    {
        int best = -1, bestDmg = 0;
        for (int i = 0; i < turn.Openings.Count; i++)
        {
            var o = turn.Openings[i];
            if (o.EffectKind != EffectKind.Damage || !CanAfford(o, turn)) continue;
            if (o.EffectAmount > bestDmg) { best = i; bestDmg = o.EffectAmount; }
        }
        return best;
    }

    static int BestAffordable(TacticalTurnData turn)
    {
        int best = -1, bestScore = -1;
        for (int i = 0; i < turn.Openings.Count; i++)
        {
            var o = turn.Openings[i];
            if (!CanAfford(o, turn)) continue;
            int score = o.EffectKind switch
            {
                EffectKind.StopTimer => 100,
                EffectKind.Damage => o.EffectAmount + 10,
                EffectKind.Momentum => o.EffectAmount,
                _ => 0,
            };
            if (score > bestScore) { best = i; bestScore = score; }
        }
        return best;
    }

    static bool CanAfford(OpeningSnapshot o, TacticalTurnData turn) => o.CostKind switch
    {
        CostKind.Free => true,
        CostKind.Momentum => turn.Momentum >= o.CostAmount,
        CostKind.Spirits => turn.PlayerSpirits > o.CostAmount,
        CostKind.Tick => true,
        _ => false,
    };

    // ── Vibe measurement ────────────────────────────────────────

    static double MeasureChoice(TacticalTurnData turn)
    {
        // Two dimensions of choice:
        // 1. Card selection: are visible options close in value?
        // 2. Press decision: is the best card "good enough" or should I dig?

        var values = new List<double>();
        for (int i = 0; i < turn.Openings.Count; i++)
        {
            var o = turn.Openings[i];
            if (!CanAfford(o, turn)) continue;
            double v = o.EffectKind switch
            {
                EffectKind.StopTimer => 0.9,
                EffectKind.Damage => 0.3 + o.EffectAmount * 0.12,
                EffectKind.Momentum => 0.2 + o.EffectAmount * 0.05,
                _ => 0.1,
            };
            values.Add(v);
        }

        double cardChoice = 0.0;
        if (values.Count >= 2)
        {
            values.Sort();
            values.Reverse();
            double gap = values[0] - values[1];
            cardChoice = Math.Clamp(1.0 - gap * 3, 0.0, 1.0);
        }

        // Press decision: mediocre card + momentum to press = real decision
        double bestVal = values.Count > 0 ? values[0] : 0;
        double pressChoice = 0.0;
        if (bestVal > 0.15 && bestVal < 0.55 && turn.Momentum >= 2)
            pressChoice = 1.0 - Math.Abs(bestVal - 0.35) * 4;

        return Math.Clamp(Math.Max(cardChoice, pressChoice), 0.0, 1.0);
    }

    static double MeasureTension(TacticalTurnData turn)
    {
        double worst = 0;
        foreach (var t in turn.Timers)
        {
            if (t.Stopped) continue;
            double tension = t.Countdown switch
            {
                <= 1 => 1.0,
                2 => 0.8,
                3 => 0.6,
                4 => 0.4,
                5 => 0.2,
                _ => 0.1,
            };
            if (tension > worst) worst = tension;
        }
        return worst;
    }
}
