// Phase: beat + gemma color -> finished prose, via a reasoning integrator. PAID.
// Port of forge/synthesis.py.
//
// The load-bearing stage. A strong reasoning model takes a beat (events fixed, tone
// given) and the gemma color bank (vivid but produced WITHOUT reasoning, so
// factually unreliable) and writes the final in-world prose. The model's real job is
// a logic filter: mine the color for texture, ground it in the beat's facts and POV,
// reject any image stated as literal fact or breaking continuity. No ban lists — the
// policing is reasoning. Output is stored per model under stages.synthesis[<model>].
namespace Forge;

using System.Text.Json.Nodes;

public static class SynthesisCommand
{
    // Tone -> the register intent the integrator should aim for.
    private static readonly Dictionary<string, string> ToneIntent = new()
    {
        ["dread"] = "slow, creeping dread surfacing beneath a calm surface",
        ["horror"] = "visceral, physical horror — the body, the wrongness made flesh",
        ["wonder"] = "eerie, vertiginous wonder; awe at the strange or vast",
        ["mundane"] = "grounded and matter-of-fact, letting unease sit beneath the ordinary",
        ["action"] = "kinetic physical immediacy; movement and consequence",
        ["revelation"] = "the vertiginous shock of sudden understanding",
    };

    public const string SystemPrompt =
        """
        You are a prose finisher for a weird-fiction CRPG in the lineage of Clark Ashton Smith, H.P. Lovecraft, and Robert E. Howard. You turn one beat — its events fixed, its tone given — into finished in-world prose, drawing imagery from a provided color bank.

        REGISTER
        - Terse, declarative, sensory. Concrete nouns and verbs. The world is strange; the prose is plain.
        - No interiority or editorializing unless the beat itself demands it.
        - Any dialogue is rendered verbatim in the characters' own words, never summarized.

        FIDELITY (binding)
        - Preserve every event and fact in the beat. Invent nothing; drop nothing.
        - Keep the beat's point of view and tense exactly as written.
        - The color bank is IMAGERY ONLY. Its details are NOT trustworthy — it invents names, events, and claims and drifts from the facts. Mine it for texture and phrasing; never import its invented specifics.

        LOGIC FILTER (this is the judgment we need from you)
        The color bank was produced without reasoning, so it commits two errors you must catch and either repair or discard:
        1. Figure stated as literal fact — a metaphor or simile asserted as event ("his hands became pistons", "the page was yellowed skin"). Either mark it clearly as figurative (like / as if / seemed) or cut it. Never assert it as a literal event.
        2. Continuity break — an image that violates the scene's timeline, physical scale, or causality ("many seasons passed in the cellar"). Cut it.
        Keep an image only if it is both on-tone and ontologically coherent in this scene. When a striking image breaks the scene, the scene wins.

        Output ONLY the finished prose for the beat — no preamble, no notes, no markdown.
        """;

    /// Assemble the user message. Shared with weave (story_so_far path). When a
    /// story-so-far is present it supersedes the lens (the prior prose IS the target
    /// voice and the established facts).
    public static string BuildUser(PeerBeat b, JsonObject color, string? lens = null, string storySoFar = "")
    {
        var intent = b.Tone is not null && ToneIntent.TryGetValue(b.Tone, out var v) ? v : "unease";
        var parts = new List<string> { $"TONE: {b.Tone} — {intent}" };

        if (!string.IsNullOrEmpty(b.Choice))
            parts.Add($"CHOICE CONTEXT: {b.Choice}");

        if (!string.IsNullOrEmpty(storySoFar))
            parts.Add(
                "STORY SO FAR — the finished prose the player has just read on the way to "
                + "this beat. Match its voice, grammatical person, and tense exactly, and "
                + "treat everything in it as established fact: do not restate, summarise, or "
                + "re-narrate it. Write this beat as the paragraph that comes next:\n\n"
                + storySoFar);
        else if (!string.IsNullOrEmpty(lens))
            parts.Add($"SCENE LENS (whose eye, what to notice):\n{lens.Trim()}");

        parts.Add("BEAT (events fixed; preserve its POV, tense, and every fact):\n" + b.Original);
        parts.Add(
            "COLOR BANK (imagery only — non-binding and may be incoherent. It is narrated "
            + "in a different grammatical person, usually first-person 'I/me'; recast any "
            + "borrowed image into the beat's person and never carry its 'I/me' across. Mine "
            + "for texture, apply the logic filter):\n" + (color["enriched"]?.GetValue<string>() ?? ""));
        parts.Add(!string.IsNullOrEmpty(storySoFar)
            ? "Write the finished prose for this beat, committed to this path — do not hedge "
              + "about choices the player may or may not have made on other routes."
            : "Write the finished prose for this beat.");

        return string.Join("\n\n", parts);
    }

    /// Sibling <SceneName>.lens.md, if present.
    private static string? LensFor(string peerPath, string source)
    {
        var dir = Path.GetDirectoryName(Path.GetFullPath(peerPath))!;
        var lp = Path.Combine(dir, Path.GetFileNameWithoutExtension(source) + ".lens.md");
        return File.Exists(lp) ? File.ReadAllText(lp) : null;
    }

    public static async Task<int> RunAsync(string[] args)
    {
        var dry = args.Contains("--dry-run");
        var force = args.Contains("--force");
        var useLens = !args.Contains("--no-lens");
        string? model = null, configPath = null;
        var limit = 0;
        HashSet<string>? beatIds = null;
        var positional = new List<string>();
        for (var i = 0; i < args.Length; i++)
        {
            var a = args[i];
            if (a == "--model" && i + 1 < args.Length) model = args[++i];
            else if (a == "--limit" && i + 1 < args.Length) limit = int.Parse(args[++i]);
            else if (a == "--config" && i + 1 < args.Length) configPath = args[++i];
            else if (a == "--beats" && i + 1 < args.Length) beatIds = [.. args[++i].Split(',')];
            else if (!a.StartsWith("--")) positional.Add(a);
        }

        var paths = Cli.ExpandArgs(positional, ".enc.json");
        if (paths.Count == 0)
        {
            Console.Error.WriteLine("usage: forge synthesis <file.enc.json> [...] [--model ID] [--limit N] "
                + "[--beats id,id] [--dry-run] [--no-lens] [--force] [--config <path>]");
            return 2;
        }

        var cfg = ForgeConfig.Load(configPath);
        model ??= cfg.Router.IntegratorModel;
        // dry-run assembles prompts only — no token needed, no router constructed.
        var router = dry ? null : new RouterClient(cfg.Router);

        var done = 0;
        try
        {
            foreach (var pp in paths)
            {
                var doc = Peer.Load(pp);
                var lens = useLens ? LensFor(pp, doc.Source) : null;
                foreach (var b in doc.Beats)
                {
                    var id = $"{doc.Source}:{b.Line}";
                    if (beatIds is not null && !beatIds.Contains(id)) continue;
                    if (b.Stages["color"] is not JsonObject color) continue;

                    var syn = b.Stages["synthesis"] as JsonObject;
                    if (!force && syn?[model] is JsonObject prev && prev["text"] is not null) continue;

                    var user = BuildUser(b, color, lens);
                    if (dry)
                    {
                        Console.WriteLine();
                        Console.WriteLine(new string('=', 72));
                        Console.WriteLine($"{doc.Source}:{b.Line}  tone={b.Tone}  model={model}");
                        Console.WriteLine(new string('=', 72));
                        Console.WriteLine("--- SYSTEM ---");
                        Console.WriteLine(SystemPrompt);
                        Console.WriteLine();
                        Console.WriteLine("--- USER ---");
                        Console.WriteLine(user);
                    }
                    else
                    {
                        string text;
                        try
                        {
                            text = RouterClient.StripThink(await router!.ChatAsync(cfg.Router.SynthesisUpstream, model,
                                [new ChatMessage("system", SystemPrompt), new ChatMessage("user", user)]));
                        }
                        catch (QuotaExhaustedException) { throw; }
                        catch (Exception e)
                        {
                            Console.Error.WriteLine($"  ! {id} [{model}] FAILED: {e.Message}");
                            continue;
                        }
                        syn ??= [];
                        syn[model] = new JsonObject { ["text"] = text };
                        b.Stages["synthesis"] = syn;
                        Peer.Save(pp, doc); // persist each paid result immediately (crash-safe)
                        Console.WriteLine($"  {doc.Source}:{b.Line} [{model}] "
                            + $"{WordCount(b.Original)}w -> {WordCount(text)}w");
                    }
                    done++;
                    if (limit > 0 && done >= limit) return 0;
                }
            }
        }
        catch (QuotaExhaustedException e)
        {
            Console.Error.WriteLine($"\n! router daily cap exhausted — stopping.\n  {e.Message}");
            return 3; // agreed "stop the whole comparison" signal
        }

        if (dry) Console.WriteLine($"\n[dry-run] assembled {done} prompt(s); no API calls made");
        return 0;
    }

    private static int WordCount(string s) => s.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
}
