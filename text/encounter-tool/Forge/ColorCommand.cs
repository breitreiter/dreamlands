// Phase: colour each beat via a provider (default: the imp loom triplet). Port of
// forge/color.py. Collects beats that still lack stages.color, ships them as one
// beats-job, and merges the enriched color back into each peer JSON. Resumable —
// re-running ships only the uncoloured beats; --force re-colours everything.
//
// Beats must be tone-tagged first (run categorize); an untoned beat is a hard error.
namespace Forge;

using System.Text.Json;
using System.Text.Json.Nodes;

public static class ColorCommand
{
    public static async Task<int> RunAsync(string[] args)
    {
        var force = args.Contains("--force");
        var dry = args.Contains("--dry-run");
        string? configPath = null;
        var providerName = "imp";
        var limit = 0;
        var positional = new List<string>();
        for (var i = 0; i < args.Length; i++)
        {
            var a = args[i];
            if (a == "--config" && i + 1 < args.Length) configPath = args[++i];
            else if (a == "--provider" && i + 1 < args.Length) providerName = args[++i];
            else if (a == "--limit" && i + 1 < args.Length) limit = int.Parse(args[++i]);
            else if (!a.StartsWith("--")) positional.Add(a);
        }

        var paths = Cli.ExpandArgs(positional, ".enc.json");
        if (paths.Count == 0)
        {
            Console.Error.WriteLine("usage: forge color <file.enc.json> [...] [--force] [--limit N] [--dry-run] [--provider imp|glm-hi-temp] [--config <path>]");
            return 2;
        }

        var (jobs, owner) = Collect(paths, force, limit);
        if (jobs.Count == 0)
        {
            Console.WriteLine("nothing to colour (all beats have stages.color; --force to redo)");
            return 0;
        }

        var untoned = jobs.Where(j => string.IsNullOrEmpty(j.Tone)).Select(j => j.Id).ToList();
        if (untoned.Count > 0)
        {
            Console.Error.WriteLine($"  ! {untoned.Count} beat(s) untoned — run categorize first "
                + $"(e.g. {string.Join(", ", untoned.Take(3))})");
            return 1;
        }

        var arc = new DirectoryInfo(Path.GetDirectoryName(Path.GetFullPath(paths[0]))!).Name;
        var jobPath = Path.Combine(Path.GetTempPath(), $"{arc}.beats.json");
        File.WriteAllText(jobPath, JsonSerializer.Serialize(jobs, ColorJson.Opts));
        Console.WriteLine($"prepared {jobs.Count} beat(s) -> {jobPath}");
        if (dry)
        {
            Console.WriteLine("dry-run: not shipping to imp");
            return 0;
        }

        IColorProvider provider = providerName switch
        {
            "imp" => new ImpLoomColorProvider(ForgeConfig.Load(configPath).Imp),
            "glm-hi-temp" => new GlmHighTempColorProvider(),
            _ => throw new ArgumentException($"unknown provider '{providerName}' (imp | glm-hi-temp)"),
        };

        var records = await provider.EnrichAsync(arc, jobs);
        var n = Merge(records, owner);
        Console.WriteLine($"merged {records.Count} colour record(s) into {n} peer JSON file(s)");
        return 0;
    }

    private static (List<BeatJob> Jobs, Dictionary<string, string> Owner) Collect(
        IReadOnlyList<string> paths, bool force, int limit)
    {
        var jobs = new List<BeatJob>();
        var owner = new Dictionary<string, string>();
        foreach (var pp in paths)
        {
            var doc = Peer.Load(pp);
            foreach (var b in doc.Beats)
            {
                if (b.Stages["color"] is JsonObject && !force) continue;
                var id = $"{doc.Source}:{b.Line}";
                jobs.Add(new BeatJob(id, b.Tone, b.Original));
                owner[id] = pp;
                if (limit > 0 && jobs.Count >= limit) return (jobs, owner);
            }
        }
        return (jobs, owner);
    }

    private static int Merge(IReadOnlyList<ColorRecord> records, Dictionary<string, string> owner)
    {
        var docs = new Dictionary<string, PeerDocument>(StringComparer.Ordinal);
        foreach (var rec in records)
        {
            if (!owner.TryGetValue(rec.Id, out var pp)) continue;
            if (!docs.TryGetValue(pp, out var doc)) docs[pp] = doc = Peer.Load(pp);
            var line = int.Parse(rec.Id[(rec.Id.LastIndexOf(':') + 1)..]);
            var beat = doc.Beats.FirstOrDefault(b => b.Line == line);
            if (beat is null) continue;
            beat.Stages["color"] = new JsonObject
            {
                ["enriched"] = rec.Enriched,
                ["facts"] = new JsonArray([.. rec.Facts.Select(f => (JsonNode?)f)]),
                ["register"] = rec.Register,
                ["length"] = rec.Length,
            };
        }
        foreach (var (pp, doc) in docs) Peer.Save(pp, doc);
        return docs.Count;
    }
}
