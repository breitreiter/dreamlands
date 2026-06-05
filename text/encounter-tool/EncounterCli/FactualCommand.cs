using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;

namespace EncounterCli;

/// <summary>
/// Pipeline stage 2 (factual) of the arc-writer flow (see plans/arc_writer.md).
///
/// For each FIXME beat, GLM writes one block of plain, legible prose that threads the
/// scene's curated COLOR through the load-bearing facts — competent reporting, not
/// literary texture (that is the downstream voice pass). The strategy is ported from
/// the ../factual spike: a few-shot worked example as a genuine prior turn (the proven
/// fix for described-speech-vs-quoted-dialogue flip-flopping), a hard plain-register
/// discipline, "say what is MEANT" for compressed shorthand, quoting-is-not-inventing,
/// and a deterministic em-dash post-process (GLM ignores the instruction).
///
/// Input contract: colorize writes ONE scene-level COLOR pool at the top of the file.
/// The whole pool is offered to every beat as shared material; each beat threads the
/// lines that land on it and leaves the rest. The writer sees all the scene's beats for
/// context but writes only the marked one — ../factual's stated production shape
/// ("scene-aware prose emitted per beat"). Output lands as a `# --- FACTUAL ---` block
/// adjacent to the FIXME, which the voice and critic passes consume.
///
/// Model: GLM-4.5-Air via the LocalLlm provider (same as colorize). Idempotent: a FIXME
/// with an adjacent FACTUAL block is skipped unless --force; a file with no COLOR pool
/// is skipped (colorize is the upstream).
/// </summary>
static class FactualCommand
{
    internal const string SystemPrompt = """
        You are the FACTUAL writer in a prose pipeline for a CRPG's encounters. You receive a
        scene's BEATS (the load-bearing facts of what happens and what is present) and a
        scene-level COLOR pool (curated observable details), plus the surrounding scene and the
        arc brief. You write a plain, clear prose account of ONE marked beat, threading the
        color through the facts.

        You are NOT the final stylist. A later stage adds literary texture. Your job is the
        clean, competent substrate beneath it: accurate, legible, plain.

        HOW TO WRITE IT
        - Second person, present tense. The PC is "you".
        - Write real prose for the marked beat only. The other beats are shown for context so you
          do not restate what an earlier beat already established or pre-empt what a later one
          owns.
        - Thread the COLOR that lands on this beat. The pool is shared across the whole scene, so
          do not use all of it here: take the lines that belong to this beat and leave the rest
          for the beats they belong to. Merge related details into flowing sentences; never walk
          the list or dump it.
        - Color is the only thing you may leave out. Preserve the BEAT in full; it is load-bearing.

        KEEP IT PLAIN — this is the hard part; resist the urge to embellish
        - No similes or metaphors. Write "the caravan idles in the road", never "idles like a
          beast made to kneel". If a COLOR line is itself a simile ("a face like cracked
          leather"), render the plain fact behind it ("a deeply lined face") — keep the
          observation, drop the figure.
        - No voiced or showy verbs. Write "the wind in the banners", not "the wind worrying the
          banners".
        - No wry narrator asides, knowing commentary, or attitude.
        - Do not pile on mood adjectives. State what is observable and let it stand.
        - The BEATS and COLOR are shorthand, sometimes awkward or compressed. Say plainly what is
          MEANT: render the plain fact a compressed phrase points to. Never copy an odd phrase
          verbatim, and never mangle a compressed one into nonsense. (These instructions contain
          example phrases for illustration only — never lift wording out of this prompt into your
          prose.)
        - This world is a pre-industrial secondary world. Use NO modern or anachronistic slang,
          even when a beat does. Never use the verb "clock" to mean notice or recognize; when a
          beat uses it, render the plain perception instead: her eyes catch on the ring, she
          notices it, she marks it. The same goes for any casual modernism: write the
          period-plain fact, not the slang the beat happens to use.
        - No em-dashes. Use commas, colons, or separate sentences.

        EXTERNALLY-VERIFIABLE FICTION ONLY
        - Describe what is seen, heard, said, and done. Never narrate the PC's feelings, thoughts,
          intentions, or memories — the player owns those. Render a PC choice as the action plus a
          flat stance: "You don't step in; the man can sort his own affairs.", NOT "You decline,
          knowing you'll have trouble enough alone in the city."
        - Unpack NPC interiority into observable behavior. A beat may compress state to bare
          interiority ("she is quietly terrified", "he is a true believer"); render it as
          phenomenon — a hand going still, a cup set down unfinished, a pause a beat too long,
          eyes that stop tracking. Never carry the bare claim through ("she was terrified").

        DIALOGUE — render attributed speech, invent none
        - Quote a character ONLY when the beat ATTRIBUTES SPEECH to them: it says they say, tell,
          ask, warn, offer, explain, plead, note, introduce, and so on. Then write REAL QUOTED
          DIALOGUE in their register, not reported summary. "She says three hands left for the
          docks" is WRONG; quote her: "We had three hands hired. They went to Aldgate. Dock work
          pays better." A character's given reasoning belongs in their mouth as dialogue, not
          narrated.
        - A situation described from the outside is NOT attributed speech: a dispute, an argument,
          a quarrel, raised voices, a look that asks for help. Render it as what is seen and heard
          and do NOT invent words for it. "He looks to you for help" is a look, not a line; leave
          it a look.
        - Quoting is not inventing: the words carry ONLY facts the beat already gives. Never invent
          the content of speech — no new demand, number, name, place, or reason. Stage directions
          around the speech (who looks where, how a cup is held) stay third-person external. Only
          fall back to reported speech if the beat explicitly says "she summarizes" or "he gives
          the short version".

        DO NOT INVENT
        - The beat, the color, the scene frame, the brief, and the locale are your only sources.
          Add no new props, characters, events, names, ages, professions, or histories. Scenes you
          cannot see may contradict anything you make up. Do not pre-empt a reveal the beat stops
          short of.
        - When a beat is deliberately spare, render exactly what it states and stop. Do not specify,
          name, weigh, or describe a thing the beat leaves unspecified: "an unfamiliar weight on the
          hand" is rendered as exactly that, NOT as a glimpsed or handled object. A later choice may
          reveal it; that is not your job.

        END WHEN THE BEAT IS ESTABLISHED. Do not extend past it; the next beat handles the next
        moment.

        OUTPUT: the prose for the marked beat only. No headers, no bullets, no labels, no
        commentary, no preamble. One paragraph, or two short ones if the beat naturally splits.
        """;

    // One worked example (a synthetic scene, not a real arc) ridden as a genuine prior turn.
    // It demonstrates the two things imperatives alone don't pin down: a described speech act
    // ("explains") rendered as QUOTED, characterful dialogue; and awkward shorthand color
    // ("knuckles swollen and wrong-angled") rendered as clean prose. It also models per-beat
    // restraint — the marked beat is #2, so it does NOT re-describe the lean-to that beat #1
    // already established; it threads only the color that lands on the speaking.
    internal const string ExampleUser = """
        SCENE: The Toll Bridge
        SCENE FRAME (static body prose at the top of this encounter):
        (none)

        ALL BEATS IN THIS SCENE, in order (context — do NOT write these):
        1. (mundane) The PC reaches a narrow stone bridge over a flooded river; a tollkeeper in a lean-to blocks the way.
        2. (mundane) The tollkeeper explains the bridge is the only crossing for miles, and the ford downstream is under water after the rains.
        3. (strain) He warns that the timbers on the far span are rotten, and to keep to the left.
        4. (dread) The PC notices the toll box is empty and the ledger has not been marked in days.

        WRITE THIS BEAT ONLY (#2, register: mundane), at: encounter body:
        The tollkeeper explains the bridge is the only crossing for miles, and the ford downstream is under water after the rains.

        SCENE COLOR POOL (shared across the whole scene — thread the lines that land on THIS beat; leave the rest):
        - the river running high and brown, dragging branches
        - the lean-to roofed with a single sheet of tin, drumming under the drizzle
        - the keeper's knuckles swollen and wrong-angled, old breaks
        - the ledger open, the last entry's ink gone pale

        Write the prose for the marked beat now. Externally-verifiable fiction only, plain register, real quoted dialogue where the beat has speech, no em-dashes, invent nothing. Prose only.
        """;

    internal const string ExampleAssistant = """
        "Only crossing for miles," the tollkeeper says. "The ford's downstream, and it's been under water since the rains. You want the far side, you pay, or you swim." He counts the toll with swollen, wrong-angled knuckles, old breaks set badly.
        """;

    public static async Task<int> RunAsync(string[] args)
    {
        // Parity-harness entry point: `factual --parity [N]`. Self-contained regression gate
        // that proves the shipping prompt/strategy reproduces ../factual. No arc-dir needed.
        if (args.Length > 0 && args[0] == "--parity")
            return await FactualEval.RunAsync(args.Skip(1).ToArray());

        string? arcDir = null, configPath = null;
        var force = false;
        var promptsOnly = false;
        var only = new List<string>();   // scene stems to limit to (repeatable); empty = whole arc

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--config" && i + 1 < args.Length) configPath = args[++i];
            else if (args[i] == "--only" && i + 1 < args.Length) only.Add(args[++i]);
            else if (args[i] == "--force") force = true;
            else if (args[i] == "--prompts-only") promptsOnly = true;
            else if (!args[i].StartsWith('-')) arcDir = args[i];
        }

        if (arcDir == null)
        {
            Console.Error.WriteLine("factual requires <arc-dir> (or --parity for the regression gate).");
            return 1;
        }

        arcDir = Path.GetFullPath(arcDir);
        if (!Directory.Exists(arcDir))
        {
            Console.Error.WriteLine($"Directory not found: {arcDir}");
            return 1;
        }

        var brief = DraftBlocks.LoadBrief(arcDir);
        if (brief == null)
        {
            Console.Error.WriteLine($"No .md brief found in {arcDir}");
            return 1;
        }

        var biome = DraftBlocks.InferBiome(arcDir);
        var localeGuide = DraftBlocks.LoadLocaleGuide(biome);
        if (localeGuide == null)
        {
            Console.Error.WriteLine($"No locale guide found for biome '{biome}'");
            return 1;
        }

        var encFiles = Directory.GetFiles(arcDir, "*.enc")
            .Where(f => !Path.GetFileName(f).StartsWith('_'))
            .Where(f => only.Count == 0 || only.Any(o =>
                string.Equals(Path.GetFileNameWithoutExtension(f), o, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(f => f).ToList();
        if (encFiles.Count == 0)
        {
            Console.Error.WriteLine($"No matching .enc files in {arcDir}");
            return 1;
        }

        FactualCfg? cfg = null;
        if (!promptsOnly)
        {
            cfg = LoadCfg(configPath);
            if (cfg == null) return 1;
        }

        int totalWritten = 0, totalNoColor = 0, totalDone = 0;
        foreach (var file in encFiles)
        {
            var rel = Path.GetRelativePath(arcDir, file);
            Console.WriteLine($"{rel}:");
            var (written, noColor, done) = await ProcessFileAsync(file, brief, localeGuide, cfg, force, promptsOnly);
            totalWritten += written;
            totalNoColor += noColor;
            totalDone += done;
        }

        Console.WriteLine();
        Console.WriteLine($"Wrote {totalWritten} FACTUAL block(s); skipped {totalNoColor} (no COLOR pool), {totalDone} (already-done).");
        return 0;
    }

    static async Task<(int written, int noColor, int done)> ProcessFileAsync(
        string file, string brief, string localeGuide, FactualCfg? cfg, bool force, bool promptsOnly)
    {
        var content = File.ReadAllText(file).Replace("\r\n", "\n").Replace("\r", "\n");
        var lines = content.Split('\n').ToList();
        var title = lines.Count > 0 ? lines[0].Trim() : Path.GetFileNameWithoutExtension(file);
        var encounterBody = DraftBlocks.ExtractEncounterBody(lines);

        var pool = DraftBlocks.ExtractScenePool(lines);
        if (pool.Count == 0)
        {
            Console.WriteLine("  (no COLOR pool — run colorize first)");
            return (0, 1, 0);
        }

        var fixmes = new List<int>();
        for (int i = 0; i < lines.Count; i++)
            if (DraftBlocks.FixmePattern.IsMatch(lines[i]))
                fixmes.Add(i);

        if (fixmes.Count == 0)
        {
            Console.WriteLine("  (no FIXME beats)");
            return (0, 0, 0);
        }

        // Snapshot every beat's register + text (file order) for the scene-aware context block.
        // Captured before any insertion shifts indices; addressed by FIXME ordinal, not line.
        var beats = fixmes.Select(li =>
        {
            var m = DraftBlocks.FixmePattern.Match(lines[li]);
            return (register: DraftBlocks.ExtractRegister(lines[li]) ?? "mundane", text: m.Groups[2].Value.Trim());
        }).ToList();

        int written = 0, done = 0;
        for (int idx = fixmes.Count - 1; idx >= 0; idx--)
        {
            var li = fixmes[idx];
            var match = DraftBlocks.FixmePattern.Match(lines[li]);
            var indent = match.Groups[1].Value;

            if (DraftBlocks.HasAdjacentBlock(lines, li, "FACTUAL"))
            {
                if (!force) { done++; continue; }
                RemoveAdjacentBlock(lines, li, "FACTUAL");
            }

            var location = DraftBlocks.DescribeBeatContext(lines, li);
            var userPrompt = BuildPrompt(brief, localeGuide, title, encounterBody, beats, idx, location, pool);

            if (promptsOnly)
            {
                Console.WriteLine($"  L{li + 1}: prompt for beat #{idx + 1} '{DraftBlocks.Truncate(beats[idx].text, 50)}'");
                Console.WriteLine("  ---");
                foreach (var ln in userPrompt.Split('\n'))
                    Console.WriteLine("  " + ln);
                Console.WriteLine("  ---");
                continue;
            }

            string raw;
            try
            {
                Console.Write($"  L{li + 1}: beat #{idx + 1} '{DraftBlocks.Truncate(beats[idx].text, 50)}'... ");
                raw = await cfg!.Glm.CompleteAsync(BuildMessages(userPrompt),
                    cfg.Temp, cfg.TopP, cfg.MinP, cfg.MaxTokens, enableThinking: false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
                continue;
            }

            var prose = HouseStyle(ExtractProse(raw) ?? "");
            if (string.IsNullOrEmpty(prose))
            {
                Console.WriteLine("empty");
                continue;
            }
            var proseLines = prose.Split('\n').Select(l => l.TrimEnd()).ToList();
            while (proseLines.Count > 0 && string.IsNullOrWhiteSpace(proseLines[^1])) proseLines.RemoveAt(proseLines.Count - 1);
            Console.WriteLine($"{proseLines.Count(l => !string.IsNullOrWhiteSpace(l))} prose line(s)");

            var insertAt = DraftBlocks.FindEndOfDraftStack(lines, li);
            var block = DraftBlocks.BuildBlock(indent, "FACTUAL", proseLines);
            lines.InsertRange(insertAt, block);
            written++;
        }

        if (written > 0 && !promptsOnly)
        {
            var backup = Path.Combine(Path.GetDirectoryName(file)!, "_" + Path.GetFileName(file));
            File.Copy(file, backup, overwrite: true);
            File.WriteAllText(file, string.Join("\n", lines));
            Console.WriteLine($"  → wrote {written} block(s); backup at {Path.GetFileName(backup)}");
        }

        return (written, 0, done);
    }

    /// <summary>The system + few-shot example + this-beat user message, as an ordered turn list.</summary>
    internal static (string role, string content)[] BuildMessages(string userPrompt) =>
    [
        ("system", SystemPrompt),
        ("user", ExampleUser),
        ("assistant", ExampleAssistant),
        ("user", userPrompt),
    ];

    internal static string BuildPrompt(
        string brief, string localeGuide, string title, string encounterBody,
        List<(string register, string text)> beats, int markedIdx, string location, List<string> pool)
    {
        var sb = new StringBuilder();
        sb.Append("ARC BRIEF (the author's intent for this arc):\n\n").Append(brief).Append("\n\n---\n\n");
        sb.Append("LOCALE GUIDE (biome setting, register, palette):\n\n").Append(localeGuide).Append("\n\n---\n\n");
        sb.Append("SCENE: ").Append(title).Append('\n');
        sb.Append("SCENE FRAME (static body prose at the top of this encounter):\n")
          .Append(string.IsNullOrWhiteSpace(encounterBody) ? "(none)" : encounterBody).Append("\n\n");

        sb.Append("ALL BEATS IN THIS SCENE, in order (context — do NOT write these):\n");
        for (int i = 0; i < beats.Count; i++)
            sb.Append(i + 1).Append(". (").Append(beats[i].register).Append(") ").Append(beats[i].text).Append('\n');
        sb.Append('\n');

        sb.Append("WRITE THIS BEAT ONLY (#").Append(markedIdx + 1).Append(", register: ")
          .Append(beats[markedIdx].register).Append("), at: ").Append(location).Append(":\n")
          .Append(beats[markedIdx].text).Append("\n\n");

        sb.Append("SCENE COLOR POOL (shared across the whole scene — thread the lines that land on THIS beat; leave the rest):\n");
        foreach (var b in pool) sb.Append("- ").Append(b).Append('\n');
        sb.Append('\n');

        sb.Append("Write the prose for the marked beat now. Externally-verifiable fiction only, ")
          .Append("plain register, real quoted dialogue where the beat has speech, no em-dashes, ")
          .Append("invent nothing. Prose only.");
        return sb.ToString();
    }

    // GLM ignores the no-em-dash instruction; enforce it mechanically (a project hard rule —
    // `check` hard-fails em-dashes). Ported from ../factual's _house_style.
    static readonly Regex Dash = new(@"\s*[—–]\s*", RegexOptions.Compiled);
    static readonly Regex DoubledComma = new(@"\s*,\s*,\s*", RegexOptions.Compiled);
    static readonly Regex CommaBeforePunct = new(@"\s*,\s*([.;:])", RegexOptions.Compiled);

    internal static string HouseStyle(string text)
    {
        text = Dash.Replace(text, ", ");            // "paces — fine" -> "paces, fine"
        text = DoubledComma.Replace(text, ", ");
        text = CommaBeforePunct.Replace(text, "$1");
        return text;
    }

    internal static string? ExtractProse(string output)
    {
        if (string.IsNullOrWhiteSpace(output)) return null;
        var text = output.Trim();

        if (text.StartsWith("```"))
        {
            var firstNl = text.IndexOf('\n');
            if (firstNl > 0) text = text[(firstNl + 1)..];
        }
        if (text.EndsWith("```"))
            text = text[..^3];

        // Defensive: if a mis-templated / base model leaks chat-template tokens and keeps
        // generating a synthetic transcript, keep only the first turn (the real beat prose).
        var tok = text.IndexOf("<|", StringComparison.Ordinal);
        if (tok >= 0) text = text[..tok];

        text = text.Trim();

        var firstLine = text.Split('\n', 2)[0];
        string[] preambles = ["Here is", "Here's", "Sure", "I'll", "I'd", "Below is", "The integrated", "The marked"];
        if (preambles.Any(p => firstLine.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
        {
            var nl = text.IndexOf('\n');
            if (nl > 0) text = text[(nl + 1)..].TrimStart();
        }

        return text.Length > 0 ? text : null;
    }

    /// <summary>Remove an existing adjacent `# --- KIND ---` … `# --- end ---` block (force-regen).</summary>
    static void RemoveAdjacentBlock(List<string> lines, int fixmeIndex, string kind)
    {
        var marker = $"# --- {kind}";
        for (int i = fixmeIndex + 1; i < lines.Count; i++)
        {
            var trimmed = lines[i].TrimStart();
            if (string.IsNullOrEmpty(trimmed)) continue;
            if (!trimmed.StartsWith('#')) return;
            if (!trimmed.StartsWith(marker, StringComparison.OrdinalIgnoreCase)) continue;
            int end = i;
            while (end < lines.Count && !lines[end].TrimStart().StartsWith("# --- end", StringComparison.OrdinalIgnoreCase)) end++;
            if (end < lines.Count) end++;   // include the closing marker
            lines.RemoveRange(i, end - i);
            return;
        }
    }

    // ── Config ───────────────────────────────────────────────────────────────────
    internal sealed record FactualCfg(GlmClient Glm, double Temp, double TopP, double MinP, int MaxTokens);

    internal static FactualCfg? LoadCfg(string? configPath)
    {
        var config = LlmClient.LoadConfig(configPath);
        if (config == null) return null;

        var local = config.GetSection("ChatProviders").GetChildren()
            .FirstOrDefault(c => string.Equals(c["Name"]?.Trim(), "LocalLlm", StringComparison.OrdinalIgnoreCase));
        if (local == null)
        {
            Console.Error.WriteLine("No 'LocalLlm' provider in appsettings.json ChatProviders (need Endpoint + Model for GLM).");
            return null;
        }

        var fs = config.GetSection("Factual");
        var glm = new GlmClient(local["Endpoint"]?.Trim() ?? "", local["Model"]?.Trim() ?? "",
            local["ApiKey"]?.Trim() ?? "local", fs.GetValue("TimeoutSeconds", 600));
        return new FactualCfg(glm,
            Temp: fs.GetValue("Temp", 0.4),
            TopP: fs.GetValue("TopP", 1.0),
            MinP: fs.GetValue("MinP", 0.01),
            MaxTokens: fs.GetValue("MaxTokens", 1500));
    }
}
