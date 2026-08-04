// Phase: generate one navigable thread end-to-end, each beat steered by the finished
// prose of the beats before it on the path ("story so far"). PAID. Port of
// forge/weave.py.
//
// Where synthesis generates each beat in isolation, weave walks a single thread
// (Thread.Walk) and generates IN ORDER, feeding each finished paragraph forward as
// the voice-and-fact grounding for the next. This is the fix for factual leakage and
// POV drift. Output lands in stages.synthesis_sofar[<model>].
namespace Forge;

using System.Text.Json.Nodes;

public static class WeaveCommand
{
    private const string Stage = "synthesis_sofar";

    public static async Task<int> RunAsync(string[] args)
    {
        var dry = args.Contains("--dry-run");
        var force = args.Contains("--force");
        string? model = null, threadName = null, configPath = null;
        var positional = new List<string>();
        for (var i = 0; i < args.Length; i++)
        {
            var a = args[i];
            if (a == "--model" && i + 1 < args.Length) model = args[++i];
            else if (a == "--thread" && i + 1 < args.Length) threadName = args[++i];
            else if (a == "--config" && i + 1 < args.Length) configPath = args[++i];
            else if (!a.StartsWith("--")) positional.Add(a);
        }

        if (positional.Count == 0)
        {
            Console.Error.WriteLine("usage: forge weave <arc-dir> [--thread <name>] [--model ID] [--dry-run] [--force] [--config <path>]");
            return 2;
        }
        var arcDir = positional[0];
        var (name, plan) = ThreadPlans.Resolve(arcDir, threadName);
        threadName = name;

        var cfg = ForgeConfig.Load(configPath);
        model ??= cfg.Router.IntegratorModel;

        var (order, tags) = Thread.Walk(arcDir, plan.Path);
        var tagRepr = "[" + string.Join(", ", tags.OrderBy(t => t, StringComparer.Ordinal).Select(t => $"'{t}'")) + "]";
        Console.WriteLine($"thread '{threadName}': {order.Count} beat(s); final tags = {tagRepr}; model = {model}");

        var router = dry ? null : new RouterClient(cfg.Router);
        var docCache = new Dictionary<string, (string Path, PeerDocument Doc)>(StringComparer.Ordinal);

        (string Path, PeerDocument Doc) DocFor(string source)
        {
            if (!docCache.TryGetValue(source, out var d))
            {
                var p = System.IO.Path.Combine(arcDir, source + ".json");
                d = (p, Peer.Load(p));
                docCache[source] = d;
            }
            return d;
        }

        var sofar = new List<string>();
        var rows = new List<(string Source, int Line, PeerBeat Beat, string Text)>();

        foreach (var (source, line) in order.Select(o => (o.Source, o.Line)))
        {
            var (path, doc) = DocFor(source);
            var b = doc.Beats.First(x => x.Line == line);

            if (b.Stages["color"] is not JsonObject color)
            {
                var msg = $"  ! {source}:{line} has no color";
                if (dry) { Console.WriteLine(msg + " (skipping in dry-run)"); continue; }
                Console.Error.WriteLine(msg + " — run color first");
                return 1;
            }

            var existing = "";
            if (b.Stages[Stage] is JsonObject ss && ss[model] is JsonObject ex && ex["text"] is JsonNode tn)
                existing = tn.GetValue<string>();

            // Reuse an existing non-empty cell as story-so-far; (re)generate only the
            // missing/blank ones — so a re-run self-heals exactly the empties.
            if (!string.IsNullOrEmpty(existing.Trim()) && !force && !dry)
            {
                sofar.Add(existing);
                rows.Add((source, line, b, existing));
                Console.WriteLine($"  {source}:{line} [reuse]");
                continue;
            }

            var story = string.Join("\n\n", sofar);
            if (dry)
            {
                Console.WriteLine();
                Console.WriteLine(new string('=', 72));
                Console.WriteLine($"{source}:{line}  tone={b.Tone}  story-so-far={sofar.Count} prior beat(s)");
                Console.WriteLine(new string('=', 72));
                Console.WriteLine(SynthesisCommand.BuildUser(b, color, null, story));
                sofar.Add(!string.IsNullOrEmpty(existing) ? existing : b.Original); // grow context
                continue;
            }

            string text;
            try
            {
                text = await router!.CompleteAsync(cfg.Router.SynthesisUpstream, model,
                    [new ChatMessage("system", SynthesisCommand.SystemPrompt),
                     new ChatMessage("user", SynthesisCommand.BuildUser(b, color, null, story))]);
            }
            catch (EmptyCompletionException e)
            {
                Console.Error.WriteLine($"  ! {source}:{line} [{model.Split('/')[^1]}] EMPTY after retries: {e.Message}");
                continue; // leave the gap; never store a blank cell
            }

            var stageObj = b.Stages[Stage] as JsonObject ?? [];
            stageObj[model] = new JsonObject { ["text"] = text };
            b.Stages[Stage] = stageObj;
            Peer.Save(path, doc);
            sofar.Add(text);
            rows.Add((source, line, b, text));
            Console.WriteLine($"  {source}:{line} [{b.Tone}] {WordCount(b.Original)}w -> {WordCount(text)}w");
        }

        if (!dry && rows.Count > 0) Render(arcDir, model, threadName, rows);
        return 0;
    }

    private static void Render(string arcDir, string model, string threadName,
        List<(string Source, int Line, PeerBeat Beat, string Text)> rows)
    {
        var arc = new DirectoryInfo(arcDir.TrimEnd('/', '\\')).Name;
        var slug = model.Split('/')[^1];
        var outPath = System.IO.Path.Combine("out", "compare", $"{arc}.thread.{threadName}.{slug}.md");
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(outPath)!);

        var L = new List<string>
        {
            $"# Threaded synthesis — {arc} / {threadName}  ({slug})",
            "\nOne navigable path; each beat steered by the finished prose before it.\n",
            "## Continuous read\n",
        };
        L.AddRange(rows.Select(r => r.Text));
        L.Add("\n---\n");
        L.Add("## Per-beat (stub ▸ finished)\n");
        foreach (var (source, line, b, text) in rows)
        {
            L.Add($"### {source}:{line}  ({b.Tone})");
            L.Add($"> {b.Original}");
            L.Add(text);
            L.Add("");
        }
        File.WriteAllText(outPath, string.Join("\n\n", L) + "\n");
        Console.WriteLine($"\nrendered -> {outPath}");
    }

    private static int WordCount(string s) => s.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
}
