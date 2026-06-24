// Phase gate (free, local): render each beat's stub + tone + color bank to readable
// markdown so the prep work can be vetted BEFORE the paid weave/synthesis call.
//
// kimi assembles well only from clean input, so the load-bearing review happens here
// — on the last free artifact (the gemma color bank) — not after spending the
// gateway. This command produces nothing the pipeline consumes; it is an eyeball
// surface. Re-color a bad beat (delete its stages.color, re-run `forge color`) before
// weaving.
//
// Two modes:
//   forge review <dir|file.enc.json>          every beat, per-file in line order (full coverage)
//   forge review <arc-dir> --thread <name>    one thread's beats, in reading order (what kimi sees)
namespace Forge;

using System.Text;
using System.Text.Json.Nodes;

public static class ReviewCommand
{
    public static int Run(string[] args)
    {
        string? outDir = null, threadName = null;
        var positional = new List<string>();
        for (var i = 0; i < args.Length; i++)
        {
            var a = args[i];
            if (a == "--out" && i + 1 < args.Length) outDir = args[++i];
            else if (a == "--thread" && i + 1 < args.Length) threadName = args[++i];
            else if (!a.StartsWith("--")) positional.Add(a);
        }

        if (positional.Count == 0)
        {
            Console.Error.WriteLine("usage: forge review <dir|file.enc.json> [--thread <name>] [--out <dir>]");
            return 2;
        }

        return threadName is null
            ? RunFiles(positional, outDir)
            : RunThread(positional[0], threadName, outDir);
    }

    // Every beat across the given peer JSONs, per-file in source-line order.
    private static int RunFiles(List<string> positional, string? outDir)
    {
        var paths = Cli.ExpandArgs(positional, ".enc.json");
        if (paths.Count == 0)
        {
            Console.Error.WriteLine("review: no peer JSON found (run forge parse first)");
            return 1;
        }

        var arc = new DirectoryInfo(Path.GetDirectoryName(Path.GetFullPath(paths[0]))!).Name;
        var sb = new StringBuilder();
        sb.Append($"# Pre-weave review — {arc}\n\n");
        sb.Append("Vet the prep BEFORE spending kimi. Each beat: stub, tone, color bank. "
            + "Re-color any bad beat (delete its `stages.color`, re-run `forge color`) before weaving.\n\n");

        int total = 0, missing = 0;
        foreach (var pp in paths)
        {
            var doc = Peer.Load(pp);
            sb.Append($"## {doc.Source}\n\n");
            foreach (var b in doc.Beats.OrderBy(b => b.Line))
            {
                AppendBeat(sb, doc.Source, b);
                total++;
                if (b.Stages["color"] is not JsonObject) missing++;
            }
        }

        return Write(arc, "review", sb, total, missing, outDir);
    }

    // One thread's beats in reading order — the exact sequence/content kimi will weave.
    private static int RunThread(string arcDir, string threadName, string? outDir)
    {
        if (!Threads.All.TryGetValue(threadName, out var plan))
        {
            Console.Error.WriteLine($"unknown thread '{threadName}' (have: {string.Join(", ", Threads.All.Keys)})");
            return 2;
        }

        var (order, tags) = Thread.Walk(arcDir, plan);
        var docs = new Dictionary<string, PeerDocument>(StringComparer.Ordinal);
        PeerBeat? Beat(string source, int line)
        {
            if (!docs.TryGetValue(source, out var doc))
                docs[source] = doc = Peer.Load(Path.Combine(arcDir, source + ".json"));
            return doc.Beats.FirstOrDefault(b => b.Line == line);
        }

        var arc = new DirectoryInfo(arcDir.TrimEnd('/', '\\')).Name;
        var sb = new StringBuilder();
        sb.Append($"# Pre-weave review — {arc} / thread `{threadName}`\n\n");
        sb.Append($"{order.Count} beat(s) in reading order; final tags = "
            + $"[{string.Join(", ", tags.OrderBy(t => t, StringComparer.Ordinal))}]. "
            + "This is the exact sequence kimi weaves — vet it before spending.\n\n");

        int total = 0, missing = 0;
        foreach (var (source, line) in order.Select(o => (o.Source, o.Line)))
        {
            var b = Beat(source, line);
            if (b is null) continue;
            AppendBeat(sb, source, b);
            total++;
            if (b.Stages["color"] is not JsonObject) missing++;
        }

        return Write(arc, $"review.{threadName}", sb, total, missing, outDir);
    }

    private static void AppendBeat(StringBuilder sb, string source, PeerBeat b)
    {
        var color = b.Stages["color"] as JsonObject;
        sb.Append($"### {source}:{b.Line}  ·  tone=`{b.Tone ?? "—"}`");
        if (color is not null)
            sb.Append($"  ·  register `{Str(color, "register")}` / length `{Str(color, "length")}`");
        sb.Append("\n\n");

        if (!string.IsNullOrEmpty(b.Choice))
            sb.Append($"_choice: {b.Choice}_\n\n");

        sb.Append($"**Stub.** {b.Original}\n\n");

        if (color is null)
        {
            sb.Append("> ⚠ no color yet — run `forge color` before weaving.\n\n");
            return;
        }

        var enriched = Str(color, "enriched");
        sb.Append("**Color.**\n\n");
        foreach (var line in enriched.Split('\n'))
            sb.Append($"> {line}\n");
        sb.Append('\n');

        if (color["facts"] is JsonArray facts && facts.Count > 0)
        {
            sb.Append("**Facts the color asserts** (any wrong → repair upstream or expect kimi to filter):\n\n");
            foreach (var f in facts)
                sb.Append($"- {f?.GetValue<string>()}\n");
            sb.Append('\n');
        }
    }

    private static string Str(JsonObject o, string key) => o[key]?.GetValue<string>() ?? "";

    private static int Write(string arc, string suffix, StringBuilder sb, int total, int missing, string? outDir)
    {
        var dir = outDir ?? Path.Combine("out", "review");
        Directory.CreateDirectory(dir);
        var outPath = Path.Combine(dir, $"{arc}.{suffix}.md");
        File.WriteAllText(outPath, sb.ToString());

        Console.WriteLine($"reviewed {total} beat(s) -> {outPath}");
        if (missing > 0)
            Console.WriteLine($"  ⚠ {missing} beat(s) lack color — run `forge color` before weaving");
        return 0;
    }
}
