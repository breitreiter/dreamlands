// Debug: dump a thread's beat spine (the .NET answer to thread.py's __main__), and
// report thread coverage across an arc.
//
// Coverage is the load-bearing check before a paid weave: weave only generates prose
// for beats some thread actually reaches, so an uncovered beat silently falls back to
// isolated synthesis (the lower-quality path). At the_villa's size that was checkable
// by eye; at 10 files and 67 beats it is not.
namespace Forge;

public static class ThreadCommand
{
    public static int Run(string[] args)
    {
        string? threadName = null;
        var coverage = args.Contains("--coverage");
        var positional = new List<string>();
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] == "--thread" && i + 1 < args.Length) threadName = args[++i];
            else if (!args[i].StartsWith("--")) positional.Add(args[i]);
        }

        if (positional.Count == 0)
        {
            Console.Error.WriteLine("usage: forge thread <arc-dir> [--thread <name>] [--coverage]");
            return 2;
        }
        var arcDir = positional[0];
        return coverage ? Coverage(arcDir) : Spine(arcDir, threadName);
    }

    private static int Spine(string arcDir, string? threadName)
    {
        var (name, plan) = ThreadPlans.Resolve(arcDir, threadName);
        var (order, tags) = Thread.Walk(arcDir, plan.Path);
        // Ordinal sort to match Python's sorted() (code-point order) for parity.
        var sortedTags = tags.OrderBy(t => t, StringComparer.Ordinal);
        Console.WriteLine($"thread '{name}': {order.Count} beat(s); tags=[{string.Join(", ", sortedTags)}]");
        foreach (var b in order)
            Console.WriteLine($"  {b.Source}:{b.Line}");
        return 0;
    }

    private static int Coverage(string arcDir)
    {
        var file = ThreadPlans.Load(arcDir);

        var all = new List<(string Source, int Line, string Original)>();
        foreach (var p in Directory.GetFiles(arcDir, "*.enc.json").OrderBy(x => x, StringComparer.Ordinal))
        {
            var doc = Peer.Load(p);
            all.AddRange(doc.Beats.Select(b => (doc.Source, b.Line, b.Original)));
        }
        if (all.Count == 0)
        {
            Console.Error.WriteLine($"no peer JSON in '{arcDir}' — run `forge parse` first");
            return 1;
        }

        Console.WriteLine($"coverage — {new DirectoryInfo(arcDir.TrimEnd('/', '\\')).Name}\n");

        var covered = new HashSet<(string, int)>();
        var failed = new List<(string Name, string Message)>();
        foreach (var (name, plan) in file.Threads)
        {
            try
            {
                var (order, _) = Thread.Walk(arcDir, plan.Path);
                foreach (var b in order) covered.Add((b.Source, b.Line));
                Console.WriteLine($"  {name,-14} {order.Count,3} beat(s)");
            }
            catch (Exception e)
            {
                failed.Add((name, e.Message));
                Console.WriteLine($"  {name,-14}   ! does not walk");
            }
        }

        var uncovered = all.Where(b => !covered.Contains((b.Source, b.Line))).ToList();
        Console.WriteLine($"\n  {all.Count} beat(s) total, {all.Count - uncovered.Count} covered, {uncovered.Count} uncovered");

        if (failed.Count > 0)
        {
            Console.WriteLine("\nbroken threads:");
            foreach (var (name, message) in failed) Console.WriteLine($"  {name}: {message}");
        }

        if (uncovered.Count > 0)
        {
            Console.WriteLine("\nuncovered beats (these would fall back to isolated synthesis):");
            foreach (var g in uncovered.GroupBy(b => b.Source).OrderBy(g => g.Key, StringComparer.Ordinal))
            {
                Console.WriteLine($"  {g.Key}");
                foreach (var b in g.OrderBy(b => b.Line))
                    Console.WriteLine($"    :{b.Line,-4} {Excerpt(b.Original)}");
            }
        }

        return failed.Count > 0 || uncovered.Count > 0 ? 1 : 0;
    }

    private static string Excerpt(string s, int max = 72)
    {
        var one = string.Join(' ', s.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return one.Length <= max ? one : one[..(max - 1)] + "…";
    }
}
