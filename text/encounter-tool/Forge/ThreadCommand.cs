// Debug: dump a thread's beat spine (the .NET answer to thread.py's __main__).
// Used to verify the engine-driven walk matches forge's reference spine.
namespace Forge;

public static class ThreadCommand
{
    public static int Run(string[] args)
    {
        string? threadName = null;
        var positional = new List<string>();
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] == "--thread" && i + 1 < args.Length) threadName = args[++i];
            else if (!args[i].StartsWith("--")) positional.Add(args[i]);
        }

        if (positional.Count == 0)
        {
            Console.Error.WriteLine("usage: forge thread <arc-dir> [--thread <name>]");
            return 2;
        }

        threadName ??= Threads.Default;
        if (!Threads.All.TryGetValue(threadName, out var plan))
        {
            Console.Error.WriteLine($"unknown thread '{threadName}' (have: {string.Join(", ", Threads.All.Keys)})");
            return 2;
        }

        var (order, tags) = Thread.Walk(positional[0], plan);
        // Ordinal sort to match Python's sorted() (code-point order) for parity.
        var sortedTags = tags.OrderBy(t => t, StringComparer.Ordinal);
        Console.WriteLine($"thread '{threadName}': {order.Count} beat(s); tags=[{string.Join(", ", sortedTags)}]");
        foreach (var b in order)
            Console.WriteLine($"  {b.Source}:{b.Line}");
        return 0;
    }
}
