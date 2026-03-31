using System.Collections.Concurrent;
using Dreamlands.Orchestration;
using Dreamlands.Rules;
using Dreamlands.Tactical;

namespace TacticalSim;

record RunResult(
    TacticalFinishReason Outcome,
    int Turns,
    int SpiritsStart,
    int SpiritsEnd,
    int MomentumEnd);

static class SimRunner
{
    const int MaxTurns = 100;

    public static void RunAll(int runs, bool verbose)
    {
        var results = new List<(string Bot, string Profile, string Encounter, List<RunResult> Results)>();

        foreach (var encounter in Scenarios.Encounters)
        foreach (var profile in Scenarios.Profiles)
        foreach (var bot in Bots.All)
        {
            var runResults = RunScenario(bot, encounter, profile, runs);
            results.Add((bot.Name, profile.Name, encounter.Name, runResults));
        }

        SimReport.Print(results, verbose);
    }

    public static void RunCustomDecks(int runs, bool verbose)
    {
        var results = new List<(string Bot, string Profile, string Encounter, List<RunResult> Results)>();
        var competent = Bots.All.First(b => b.Name == "Competent");

        foreach (var encounter in Scenarios.Encounters)
        foreach (var profile in Scenarios.CustomProfiles)
        {
            var runResults = RunScenario(competent, encounter, profile, runs);
            results.Add((competent.Name, profile.Name, encounter.Name, runResults));
        }

        SimReport.Print(results, verbose);
    }

    static List<RunResult> RunScenario(Bot bot, EncounterScenario scenario, PlayerProfile profile, int runCount)
    {
        var bag = new ConcurrentBag<RunResult>();
        var balance = BalanceData.Default;

        Parallel.For(0, runCount, seed =>
        {
            var result = RunOne(bot, scenario, profile, balance, seed);
            bag.Add(result);
        });

        return [.. bag];
    }

    static RunResult RunOne(Bot bot, EncounterScenario scenario, PlayerProfile profile, BalanceData balance, int seed)
    {
        var session = Scenarios.BuildSession(profile, seed, balance);
        var encounter = Scenarios.BuildEncounter(scenario);
        var state = new TacticalState();
        var tb = balance.Tactical;

        int spiritsStart = session.Player.Spirits;
        var step = TacticalRunner.Begin(session, encounter, state);
        bool deckInjected = false;

        int turns = 0;
        while (true)
        {
            // Inject custom deck after approach selection (or after Begin for no-approach encounters)
            if (!deckInjected && profile.CustomDeck != null && step is not TacticalStep.ChooseApproach)
            {
                var deckRng = new Random(seed + 99);
                state.Deck = Scenarios.BuildCustomDeck(profile.CustomDeck, balance, deckRng);
                state.DrawIndex = 0;
                deckInjected = true;
            }

            switch (step)
            {
                case TacticalStep.ChooseApproach ca:
                    var approach = bot.ApproachSelect(ca.Approaches);
                    step = TacticalRunner.ApplyApproach(session, encounter, state, approach);
                    break;

                case TacticalStep.ShowTurn show:
                    if (++turns > MaxTurns)
                        return new RunResult(TacticalFinishReason.SpiritsLoss, turns, spiritsStart, session.Player.Spirits, state.Momentum);

                    var action = bot.Strategy(show.Data, encounter, tb);
                    try
                    {
                        step = TacticalRunner.Act(session, encounter, state, action.Action, action.OpeningIndex);
                    }
                    catch (InvalidOperationException)
                    {
                        return new RunResult(TacticalFinishReason.SpiritsLoss, turns, spiritsStart, session.Player.Spirits, state.Momentum);
                    }
                    break;

                case TacticalStep.Finished fin:
                    return new RunResult(fin.Reason, turns, spiritsStart, session.Player.Spirits, state.Momentum);

                default:
                    throw new InvalidOperationException($"Unexpected step type: {step.GetType().Name}");
            }
        }
    }
}
