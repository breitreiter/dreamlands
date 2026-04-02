using System.Collections.Concurrent;
using Dreamlands.Rules;
using Dreamlands.Tactical;

namespace TacticalSim;

// ── Configuration ──────────────────────────────────────────────

record GaConfig(
    int PopulationSize = 500,
    int Generations = 200,
    int TournamentSize = 5,
    double CrossoverRate = 0.5,
    double MutationRate = 0.10,
    double ElitismPct = 0.05,
    int SimsPerDeck = 30,
    ApproachKind Approach = ApproachKind.Direct);

// ── GA engine ──────────────────────────────────────────────────

static class GeneticSearch
{
    public static void Run(GaConfig config)
    {
        var balance = BalanceData.Default;
        var pool = balance.Tactical.Archetypes.Keys.OrderBy(k => k).ToArray();
        int deckSize = balance.Tactical.DeckSize;
        int eliteCount = Math.Max(1, (int)(config.PopulationSize * config.ElitismPct));

        Console.WriteLine($"GA search: pop={config.PopulationSize}, gen={config.Generations}, " +
                          $"sims/deck={config.SimsPerDeck}, approach={config.Approach}");
        Console.WriteLine($"Card pool ({pool.Length}): {string.Join(", ", pool)}");
        Console.WriteLine();

        // Seed population
        var population = new int[config.PopulationSize][];
        var rng = new Random(42);
        for (int i = 0; i < config.PopulationSize; i++)
            population[i] = RandomDeck(deckSize, pool.Length, rng);

        // Track best-ever for convergence detection
        double bestEver = double.NegativeInfinity;
        int convergenceGen = -1;
        var bestHistory = new List<double>();

        // Generational loop
        double[] fitness = [];
        for (int gen = 0; gen < config.Generations; gen++)
        {
            fitness = EvaluatePopulation(population, pool, config);

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
                Normalize(child);
                next[i] = child;
            }

            population = next;
        }

        // End-of-run report
        PrintEndOfRun(population, fitness, pool, config, convergenceGen);
    }

    // ── Fitness ─────────────────────────────────────────────────

    static double[] EvaluatePopulation(int[][] population, string[] pool, GaConfig config)
    {
        var scores = new double[population.Length];

        Parallel.For(0, population.Length, i =>
        {
            var deckSpec = population[i].Select(id => pool[id]).ToArray();
            scores[i] = EvaluateDeck(deckSpec, config.Approach, config.SimsPerDeck);
        });

        return scores;
    }

    static double EvaluateDeck(string[] deckSpec, ApproachKind approach, int sims)
    {
        var results = SimRunner.Run(deckSpec, approach, sims);
        return FitnessFunction(results);
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

        var allTurns = results.SelectMany(r => r.Turns).ToList();
        if (allTurns.Count == 0) return 0;
        return allTurns.Average(t => t.Juice);
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

    static string FormatDeck(int[] deck, string[] pool)
    {
        return string.Join(", ", deck
            .GroupBy(id => id)
            .OrderByDescending(g => g.Count())
            .ThenBy(g => g.Key)
            .Select(g => $"{g.Count()}x {pool[g.Key]}"));
    }

    // ── End-of-run report ───────────────────────────────────────

    static void PrintEndOfRun(int[][] population, double[] fitness, string[] pool,
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
        var topDecks = new List<(int[] Deck, double Fitness)>();
        foreach (var i in ranked)
        {
            var key = string.Join(",", population[i]);
            if (seen.Add(key))
                topDecks.Add((population[i], fitness[i]));
            if (topDecks.Count >= 20) break;
        }

        Console.WriteLine();
        Console.WriteLine("  Top 20 decks (deduplicated):");
        for (int i = 0; i < topDecks.Count; i++)
        {
            var (deck, fit) = topDecks[i];
            Console.WriteLine($"    {i + 1,2}. {fit,7:F3}  [{FormatDeck(deck, pool)}]");
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
