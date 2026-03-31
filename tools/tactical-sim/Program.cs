namespace TacticalSim;

class Program
{
    static int Main(string[] args)
    {
        int runs = 1000;
        bool verbose = false;
        bool custom = false;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--runs" when i + 1 < args.Length:
                    runs = int.Parse(args[++i]);
                    break;
                case "--verbose":
                    verbose = true;
                    break;
                case "--custom":
                    custom = true;
                    break;
                default:
                    return PrintUsage();
            }
        }

        Console.WriteLine($"Running tactical balance simulation ({runs} runs per scenario)...\n");
        if (custom)
            SimRunner.RunCustomDecks(runs, verbose);
        else
            SimRunner.RunAll(runs, verbose);
        return 0;
    }

    static int PrintUsage()
    {
        Console.WriteLine("Usage: tactical-sim [--runs N] [--verbose]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --runs N      Number of simulation runs per scenario (default: 1000)");
        Console.WriteLine("  --verbose     Show per-scenario card usage breakdown");
        return 1;
    }
}
