using System.Collections.Concurrent;
using Dreamlands.Rules;
using Dreamlands.Tactical;

namespace TacticalSim;

// ── Configuration ──────────────────────────────────────────────

record GaConfig(
    int PopulationSize = 5000,
    int Generations = 300,
    int TournamentSize = 5,
    double CrossoverRate = 0.5,
    double MutationRate = 0.10,
    double ElitismPct = 0.05,
    int SimsPerDeck = 30,
    ApproachKind Approach = ApproachKind.Aggressive);

// ── GA engine ──────────────────────────────────────────────────

static class GeneticSearch
{
    public static void Run(GaConfig config)
    {
        var balance = BalanceData.Default;
        var pool = balance.Tactical.Archetypes.Keys
            .Where(k => k != "free_cancel")
            .OrderBy(k => k).ToArray();
        int deckSize = balance.Tactical.DeckSize;
        int eliteCount = Math.Max(1, (int)(config.PopulationSize * config.ElitismPct));

        // Identify cancel card indices for the max-3-cancels constraint
        var cancelIndices = new HashSet<int>();
        var nonCancelIndices = new List<int>();
        for (int i = 0; i < pool.Length; i++)
        {
            if (balance.Tactical.Archetypes[pool[i]].EffectKind is "stop_timer")
                cancelIndices.Add(i);
            else
                nonCancelIndices.Add(i);
        }

        Console.WriteLine($"GA search: pop={config.PopulationSize}, gen={config.Generations}, " +
                          $"sims/deck={config.SimsPerDeck}, approach={config.Approach}");
        Console.WriteLine($"Card pool ({pool.Length}): {string.Join(", ", pool)}");
        Console.WriteLine();

        // Seed population
        var population = new int[config.PopulationSize][];
        var rng = new Random(42);
        for (int i = 0; i < config.PopulationSize; i++)
        {
            population[i] = RandomDeck(deckSize, pool.Length, rng);
            RepairDeck(population[i], cancelIndices, nonCancelIndices, pool.Length, rng);
        }

        // Track best-ever for convergence detection
        double bestEver = double.NegativeInfinity;
        int convergenceGen = -1;
        var bestHistory = new List<double>();

        // Generational loop
        double[] fitness = [];
        double[] medianTurns = [];
        for (int gen = 0; gen < config.Generations; gen++)
        {
            (fitness, medianTurns) = EvaluatePopulation(population, pool, config);

            // Stats
            int bestIdx = 0;
            double bestFit = fitness[0], worstFit = fitness[0], sumFit = fitness[0];
            for (int i = 1; i < fitness.Length; i++)
            {
                sumFit += fitness[i];
                if (fitness[i] > bestFit) { bestFit = fitness[i]; bestIdx = i; }
                if (fitness[i] < worstFit) worstFit = fitness[i];
            }
            double avgFit = sumFit / fitness.Length;

            // Convergence tracking
            bestHistory.Add(bestFit);
            if (convergenceGen < 0 && bestHistory.Count >= 2)
            {
                double prev = bestHistory[^2];
                double improvement = prev == 0 ? 0 : Math.Abs(bestFit - prev) / Math.Abs(prev);
                if (bestFit > bestEver && improvement < 0.001)
                    convergenceGen = gen;
            }
            if (bestFit > bestEver) bestEver = bestFit;

            // Diversity: unique decks
            var unique = new HashSet<string>();
            foreach (var deck in population)
                unique.Add(string.Join(",", deck));

            // Log
            Console.WriteLine($"Gen {gen,3}  best={bestFit,7:F3}  avg={avgFit,7:F3}  worst={worstFit,7:F3}  " +
                              $"turns={medianTurns[bestIdx],2:F0}  " +
                              $"diversity={unique.Count}/{config.PopulationSize}  " +
                              $"deck=[{FormatDeck(population[bestIdx], pool)}]");

            // Don't breed after the last generation
            if (gen == config.Generations - 1) break;

            // Build next generation
            var next = new int[config.PopulationSize][];

            // Elitism: keep top N
            var ranked = Enumerable.Range(0, config.PopulationSize)
                .OrderByDescending(i => fitness[i])
                .ToArray();
            for (int i = 0; i < eliteCount; i++)
                next[i] = (int[])population[ranked[i]].Clone();

            // Fill rest via tournament selection + crossover + mutation
            for (int i = eliteCount; i < config.PopulationSize; i++)
            {
                var parentA = TournamentSelect(population, fitness, config.TournamentSize, rng);
                var parentB = TournamentSelect(population, fitness, config.TournamentSize, rng);
                var child = UniformCrossover(parentA, parentB, config.CrossoverRate, rng);
                Mutate(child, pool.Length, config.MutationRate, rng);
                RepairDeck(child, cancelIndices, nonCancelIndices, pool.Length, rng);
                Normalize(child);
                next[i] = child;
            }

            population = next;
        }

        // End-of-run report
        PrintEndOfRun(population, fitness, medianTurns, pool, config, convergenceGen);
    }

    // ── Fitness ─────────────────────────────────────────────────

    static (double[] Fitness, double[] MedianTurns) EvaluatePopulation(int[][] population, string[] pool, GaConfig config)
    {
        var scores = new double[population.Length];
        var medTurns = new double[population.Length];

        Parallel.For(0, population.Length, i =>
        {
            var deckSpec = population[i].Select(id => pool[id]).ToArray();
            var results = SimRunner.Run(deckSpec, config.Approach, config.SimsPerDeck);
            scores[i] = FitnessFunction(results);
            var turnCounts = results.Where(r => r.Won).Select(r => r.Turns.Count).OrderBy(x => x).ToList();
            medTurns[i] = turnCounts.Count > 0 ? turnCounts[turnCounts.Count / 2] : 0;
        });

        return (scores, medTurns);
    }

    /// <summary>
    /// STUB — replace with real fitness function before running.
    /// Takes the raw sim results for a single deck and returns a scalar fitness score.
    /// Higher is better. No normalization or clipping.
    /// </summary>
    static double FitnessFunction(List<RunVibes> results)
    {
        // TODO: define real fitness function.
        // Available signals per run:
        //   r.Won              — did the encounter end in victory?
        //   r.SpiritsSpent     — total spirits lost
        //   r.Turns            — list of TurnVibe, each with:
        //     .Choice           0–1, how hard the decision was (1 = genuinely hard)
        //     .Tension          0–1, how close a timer is to firing (1 = imminent)
        //     .Juice            0–1, how impactful the card played was (1 = haymaker)
        //     .Weight           rolling frustration (negative = bad streak)
        //     .Triumph          0 or 1, cleared a timer this turn
        //     .TimerFired       bool
        //     .Conditioned      bool
        //     .CardPlayed       archetype name
        //     .Action           "play", "press", "force"
        //     .SpiritsLost      int, spirits lost this turn

        double totalVibe = 0.0;
        int goodRuns = 0;

        foreach (var r in results)
        {
            // Fail out losses, pyrrhic victories, or degenerate lengths
            if (!r.Won || r.SpiritsSpent > 4 || r.Turns.Count > 12 || r.Turns.Count < 5)
                continue;

            double runVibe = 0.0;
            string? lastCard = null;
            int repeatStreak = 0;
            foreach (var t in r.Turns)
            {
                double turnScore = t.Choice + t.Tension + t.Juice + t.Weight;

                // Penalize seeing the same card repeatedly — "not this again"
                if (t.CardPlayed == lastCard)
                {
                    repeatStreak++;
                    turnScore -= 0.8 * repeatStreak; // escalating penalty
                }
                else
                {
                    repeatStreak = 0;
                }
                lastCard = t.CardPlayed;

                runVibe += turnScore;
            }

            totalVibe += runVibe / r.Turns.Count;
            goodRuns++;
        }

        if (goodRuns == 0) return 0.0;
        return totalVibe / goodRuns;
    }

    // ── Selection ───────────────────────────────────────────────

    static int[] TournamentSelect(int[][] pop, double[] fitness, int k, Random rng)
    {
        int best = rng.Next(pop.Length);
        for (int i = 1; i < k; i++)
        {
            int candidate = rng.Next(pop.Length);
            if (fitness[candidate] > fitness[best])
                best = candidate;
        }
        return pop[best];
    }

    // ── Crossover & mutation ────────────────────────────────────

    static int[] UniformCrossover(int[] a, int[] b, double rate, Random rng)
    {
        var child = new int[a.Length];
        for (int i = 0; i < a.Length; i++)
            child[i] = rng.NextDouble() < rate ? b[i] : a[i];
        return child;
    }

    static void Mutate(int[] deck, int poolSize, double rate, Random rng)
    {
        for (int i = 0; i < deck.Length; i++)
            if (rng.NextDouble() < rate)
                deck[i] = rng.Next(poolSize);
    }

    // ── Deck helpers ────────────────────────────────────────────

    static int[] RandomDeck(int size, int poolSize, Random rng)
    {
        var deck = new int[size];
        for (int i = 0; i < size; i++)
            deck[i] = rng.Next(poolSize);
        Normalize(deck);
        return deck;
    }

    /// <summary>Sort card IDs so permutation-equivalent decks have identical representation.</summary>
    static void Normalize(int[] deck) => Array.Sort(deck);

    const int MaxCancels = 2;

    const int MaxCopies = 3;

    /// <summary>Enforce cancel cap and per-card copy cap.</summary>
    static void RepairDeck(int[] deck, HashSet<int> cancelIds, List<int> nonCancelIds, int poolSize, Random rng)
    {
        // Cap cancels
        int cancelCount = 0;
        for (int i = 0; i < deck.Length; i++)
        {
            if (!cancelIds.Contains(deck[i])) continue;
            cancelCount++;
            if (cancelCount > MaxCancels)
                deck[i] = nonCancelIds[rng.Next(nonCancelIds.Count)];
        }

        // Cap copies of any single card — loop until clean
        bool dirty = true;
        int safety = 50;
        while (dirty && safety-- > 0)
        {
            dirty = false;
            var counts = new Dictionary<int, int>();
            for (int i = 0; i < deck.Length; i++)
            {
                counts.TryGetValue(deck[i], out int c);
                counts[deck[i]] = c + 1;
                if (c + 1 > MaxCopies)
                {
                    deck[i] = rng.Next(poolSize);
                    dirty = true;
                }
            }
        }
    }

    static string FormatDeck(int[] deck, string[] pool)
    {
        return string.Join(", ", deck
            .GroupBy(id => id)
            .OrderByDescending(g => g.Count())
            .ThenBy(g => g.Key)
            .Select(g => $"{g.Count()}x {pool[g.Key]}"));
    }

    // ── End-of-run report ───────────────────────────────────────

    static void PrintEndOfRun(int[][] population, double[] fitness, double[] medianTurns, string[] pool,
        GaConfig config, int convergenceGen)
    {
        Console.WriteLine();
        Console.WriteLine(new string('═', 90));
        Console.WriteLine("  END-OF-RUN REPORT");
        Console.WriteLine(new string('═', 90));

        // Top 20 deduplicated decks
        var ranked = Enumerable.Range(0, population.Length)
            .OrderByDescending(i => fitness[i])
            .ToArray();

        var seen = new HashSet<string>();
        var topDecks = new List<(int[] Deck, double Fitness, double MedianTurns)>();
        foreach (var i in ranked)
        {
            var key = string.Join(",", population[i]);
            if (seen.Add(key))
                topDecks.Add((population[i], fitness[i], medianTurns[i]));
            if (topDecks.Count >= 20) break;
        }

        Console.WriteLine();
        Console.WriteLine("  Top 20 decks (deduplicated):");
        for (int i = 0; i < topDecks.Count; i++)
        {
            var (deck, fit, mt) = topDecks[i];
            Console.WriteLine($"    {i + 1,2}. {fit,7:F3}  turns={mt,2:F0}  [{FormatDeck(deck, pool)}]");
        }

        // Card frequency in top 10%
        int top10pct = Math.Max(1, population.Length / 10);
        var topIndices = ranked.Take(top10pct).ToArray();
        var freq = new int[pool.Length];
        int totalCards = 0;
        foreach (var i in topIndices)
            foreach (var card in population[i])
            {
                freq[card]++;
                totalCards++;
            }

        Console.WriteLine();
        Console.WriteLine($"  Card frequency in top {top10pct} decks ({totalCards} total card slots):");
        var freqRanked = Enumerable.Range(0, pool.Length)
            .OrderByDescending(i => freq[i])
            .ToArray();
        foreach (var id in freqRanked)
        {
            double pct = (double)freq[id] / totalCards;
            int bar = (int)(pct * 40);
            Console.WriteLine($"    {pool[id],-30} {freq[id],5} ({pct,5:P1}) {new string('█', bar)}");
        }

        // Convergence
        Console.WriteLine();
        if (convergenceGen >= 0)
            Console.WriteLine($"  Convergence: generation {convergenceGen} (best fitness improvement < 0.1%/gen)");
        else
            Console.WriteLine("  Convergence: not reached (best fitness still improving > 0.1%/gen)");
    }
}
