using Dreamlands.Tactical;

namespace TacticalSim;

class Program
{
    static int Main(string[] args)
    {
        int runs = 10_000;
        string? deck = null;     // "cancel", "aggro", or null for both
        int? degrade = null;     // replace N best cards with chaff
        bool sweep = false;      // degrade 0..10 for both decks
        bool verbose = false;    // detailed per-turn output
        bool trace = false;     // single encounter play-by-play
        int? traceSeed = null;
        bool ga = false;

        // GA-specific overrides (defaults in GaConfig)
        int? gaPop = null, gaGens = null, gaSims = null;
        string? gaApproach = null;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--runs" when i + 1 < args.Length:
                    runs = int.Parse(args[++i]);
                    break;
                case "--deck" when i + 1 < args.Length:
                    deck = args[++i];
                    break;
                case "--degrade" when i + 1 < args.Length:
                    degrade = int.Parse(args[++i]);
                    break;
                case "--sweep":
                    sweep = true;
                    break;
                case "--verbose":
                    verbose = true;
                    break;
                case "--trace":
                    trace = true;
                    break;
                case "--seed" when i + 1 < args.Length:
                    traceSeed = int.Parse(args[++i]);
                    break;
                case "--ga":
                    ga = true;
                    break;
                case "--ga-pop" when i + 1 < args.Length:
                    gaPop = int.Parse(args[++i]);
                    break;
                case "--ga-gens" when i + 1 < args.Length:
                    gaGens = int.Parse(args[++i]);
                    break;
                case "--ga-sims" when i + 1 < args.Length:
                    gaSims = int.Parse(args[++i]);
                    break;
                case "--ga-approach" when i + 1 < args.Length:
                    gaApproach = args[++i];
                    break;
                default:
                    return PrintUsage();
            }
        }

        if (ga)
        {
            var approach = gaApproach switch
            {
                "cautious" => ApproachKind.Cautious,
                "aggressive" => ApproachKind.Aggressive,
                _ => ApproachKind.Aggressive,
            };
            var config = new GaConfig(
                PopulationSize: gaPop ?? 500,
                Generations: gaGens ?? 200,
                SimsPerDeck: gaSims ?? 30,
                Approach: approach);
            GeneticSearch.Run(config);
            return 0;
        }

        if (trace)
        {
            int seed = traceSeed ?? new Random().Next(10000);
            var deckName = deck ?? "aggro";
            var (spec, approach) = deckName == "cancel"
                ? (PlatonicDecks.Cancel, ApproachKind.Cautious)
                : (PlatonicDecks.Aggro, ApproachKind.Aggressive);
            var deckCards = degrade.HasValue ? PlatonicDecks.Degrade(spec, degrade.Value) : spec;
            var label = degrade.HasValue
                ? $"{deckName} / degrade={degrade} / seed={seed}"
                : $"{deckName} / platonic / seed={seed}";
            var result = SimRunner.RunOneTrace(deckCards, approach, seed);
            SimReport.PrintTrace(result, label);
            return 0;
        }

        if (sweep)
        {
            RunSweep(runs);
            return 0;
        }

        var decks = new List<(string Name, string[] Spec, ApproachKind Approach)>();
        if (deck == null || deck == "cancel")
            decks.Add(("cancel", PlatonicDecks.Cancel, ApproachKind.Cautious));
        if (deck == null || deck == "aggro")
            decks.Add(("aggro", PlatonicDecks.Aggro, ApproachKind.Aggressive));

        foreach (var (name, spec, approach) in decks)
        {
            var deckCards = degrade.HasValue ? PlatonicDecks.Degrade(spec, degrade.Value) : spec;
            var label = degrade.HasValue
                ? $"{name} / degrade={degrade}"
                : $"{name} / platonic";

            var results = SimRunner.Run(deckCards, approach, runs);

            if (verbose)
                SimReport.PrintDetailed(results, label);
            else
                SimReport.PrintCompact(results, label);
        }

        return 0;
    }

    static void RunSweep(int runs)
    {
        foreach (var (name, spec, approach) in new[]
        {
            ("aggro", PlatonicDecks.Aggro, ApproachKind.Aggressive),
            ("cancel", PlatonicDecks.Cancel, ApproachKind.Cautious),
        })
        {
            Console.WriteLine();
            Console.WriteLine(new string('=', 90));
            Console.WriteLine($"  SWEEP: {name} — degrading 0..10 cards");
            Console.WriteLine(new string('=', 90));

            for (int n = 0; n <= 10; n++)
            {
                var deckCards = n > 0 ? PlatonicDecks.Degrade(spec, n) : spec;
                var results = SimRunner.Run(deckCards, approach, runs);
                SimReport.PrintCompact(results, $"degrade={n,2}");
            }
        }
    }

    static int PrintUsage()
    {
        Console.WriteLine("Usage: tactical-sim [options]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --runs N        Simulation runs per scenario (default: 10000)");
        Console.WriteLine("  --deck NAME     cancel, aggro, or omit for both");
        Console.WriteLine("  --degrade N     Replace N best cards with chaff");
        Console.WriteLine("  --sweep         Degrade 0..10 for both decks");
        Console.WriteLine("  --verbose       Detailed per-turn vibe table");
        Console.WriteLine("  --trace         Single encounter play-by-play");
        Console.WriteLine("  --seed N        RNG seed for trace mode");
        Console.WriteLine();
        Console.WriteLine("Genetic algorithm:");
        Console.WriteLine("  --ga            Run GA deck search");
        Console.WriteLine("  --ga-pop N      Population size (default: 500)");
        Console.WriteLine("  --ga-gens N     Generations (default: 200)");
        Console.WriteLine("  --ga-sims N     Simulations per deck (default: 30)");
        Console.WriteLine("  --ga-approach X aggressive or cautious (default: aggressive)");
        return 1;
    }
}
