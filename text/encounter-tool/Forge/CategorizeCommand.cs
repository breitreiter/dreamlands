// Phase: assign each beat one of the six gemma tones. Free (GLM on imp).
// Port of forge/categorize.py.
//
// The gemma color steerer picks its instruction and trained register from a beat's
// tone. Hand-authored arcs use bare `FIXME:` with no tone, so this phase classifies
// each beat into dread / horror / wonder / mundane / action / revelation and writes
// it to the peer JSON's beats[].tone. Only untoned beats are classified by default
// (idempotent, incremental); --force re-tags every beat.
namespace Forge;

using System.Text.RegularExpressions;

public static partial class CategorizeCommand
{
    public static readonly string[] Tones =
        ["dread", "horror", "wonder", "mundane", "action", "revelation"];

    private static readonly (string Tone, string Desc)[] ToneDesc =
    [
        ("dread", "slow, creeping dread; something is wrong beneath a calm surface"),
        ("horror", "visceral, physical horror; the body, blood, violation"),
        ("wonder", "eerie, vertiginous wonder; awe at something strange or vast"),
        ("mundane", "grounded and matter-of-fact; ordinary action or description, unease only implied"),
        ("action", "kinetic physical action; movement, doing, immediacy"),
        ("revelation", "the shock of sudden understanding; a fact lands and reframes the scene"),
    ];

    private static readonly string SystemPrompt =
        "You classify one beat of interactive weird-fiction by its dominant emotional "
        + "register. Choose exactly one tone from this set:\n"
        + string.Join("\n", ToneDesc.Select(t => $"- {t.Tone}: {t.Desc}"))
        + "\n\nRespond with ONLY a JSON object: {\"tone\": \"<one of the six>\"}.";

    [GeneratedRegex(@"""tone""\s*:\s*""(\w+)""")]
    private static partial Regex ToneJsonRe();

    [GeneratedRegex("[a-z]+")]
    private static partial Regex WordRe();

    public static async Task<int> RunAsync(string[] args)
    {
        var force = args.Contains("--force");
        var dry = args.Contains("--dry-run");
        string? configPath = null;
        var positional = new List<string>();
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--config" && i + 1 < args.Length) configPath = args[++i];
            else if (!args[i].StartsWith("--")) positional.Add(args[i]);
        }

        var paths = Cli.ExpandArgs(positional, ".enc.json");
        if (paths.Count == 0)
        {
            Console.Error.WriteLine("usage: forge categorize <file.enc.json> [...] [--force] [--dry-run] [--config <path>]");
            return 2;
        }

        var glm = new GlmClient(ForgeConfig.Load(configPath).Glm);

        foreach (var pp in paths)
        {
            var doc = Peer.Load(pp);
            var tagged = 0;
            foreach (var b in doc.Beats)
            {
                if (!string.IsNullOrEmpty(b.Tone) && !force) continue;

                if (dry)
                {
                    Console.WriteLine($"\n=== {doc.Source}:{b.Line} ===");
                    Console.WriteLine(UserPrompt(b));
                    continue;
                }

                var tone = await ClassifyAsync(glm, b);
                if (tone is null)
                {
                    tone = "mundane";
                    Console.Error.WriteLine($"  ! {doc.Source}:{b.Line} unparseable -> mundane");
                }
                b.Tone = tone;
                tagged++;
                Console.WriteLine($"  {doc.Source}:{b.Line} -> {tone}");
            }

            if (!dry)
            {
                Peer.Save(pp, doc);
                Console.WriteLine($"  {doc.Source}: {tagged} beat(s) tagged");
            }
        }
        return 0;
    }

    private static string UserPrompt(PeerBeat b)
    {
        var ctx = string.IsNullOrEmpty(b.Choice) ? "" : $"Choice context: {b.Choice}\n";
        return $"{ctx}Beat: {b.Original}";
    }

    private static async Task<string?> ClassifyAsync(GlmClient glm, PeerBeat b)
    {
        var raw = GlmClient.StripThink(await glm.ChatAsync(
            [new ChatMessage("system", SystemPrompt), new ChatMessage("user", UserPrompt(b))],
            temperature: 0.3, maxTokens: 1500));
        return ExtractTone(raw);
    }

    /// Prefer an explicit {"tone":"x"}; otherwise the last tone word mentioned (a
    /// reasoning reply concludes with its pick). Null if no valid tone appears.
    private static string? ExtractTone(string raw)
    {
        var m = ToneJsonRe().Match(raw);
        if (m.Success && Tones.Contains(m.Groups[1].Value.ToLowerInvariant()))
            return m.Groups[1].Value.ToLowerInvariant();

        var hits = WordRe().Matches(raw.ToLowerInvariant())
            .Select(x => x.Value).Where(Tones.Contains).ToList();
        return hits.Count > 0 ? hits[^1] : null;
    }
}
