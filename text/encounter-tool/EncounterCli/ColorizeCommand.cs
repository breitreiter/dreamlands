using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;

namespace EncounterCli;

/// <summary>
/// Pipeline stage 1 of the arc-writer flow. Generates "color" — scene texture as
/// raw, atomic observables — for each `.enc` in an arc, as a single scene-level
/// grab bag the author curates.
///
/// Two stages (the method validated in the ../colorize spike; see that repo's
/// FINDINGS.md and the project_colorize_method memory):
///   A. over-generate with the local GLM-4.5-Air (v3 prompt, thinking off, a
///      small temperature sweep), steered FIRST by a per-scene lens;
///   B. cull the pool with a cheap Haiku critic (cuts simile / argue / invention
///      / anachronism / generic / ornate; light dedup only).
/// The survivors are written as ONE `# --- COLOR ---` pool at the top of the
/// file body, each line a `# []` curation checkbox the author toggles to `# [x]`.
///
/// Idempotent: a file that already carries a COLOR pool is skipped unless --force.
/// </summary>
static class ColorizeCommand
{
    // Generation system prompt — ported from ../colorize/prompts/v3.md. Built
    // around one positive frame ("transcribe the recording") rather than a
    // "don't"-list; the per-scene lens does the heavy steering at call time.
    const string SystemPrompt = """
        You generate **color** for an encounter in a horror RPG: short, raw fragments of
        what a present, attentive character would notice while the scene happens. Your
        lines are raw material for a later writer — never shown to the player as-is.
        Plain and spare is correct, not lazy.

        # What color is

        Color is the camera, not the plot. A beat says *a worker tightens a cable*;
        color says *the metallic twang of a guy line as he torques it into place*.

        - Each fragment is **one observable instant** — something a lens or a microphone
          could catch, held still in frame.
        - You are **transcribing a recording, not describing it.** Write down the exact
          thing that was caught, and stop. The moment you reach for what it's *like*, or
          what it *means*, you have left the recording and started decorating it.
        - **Surface only.** Show the outside of things; let the reader infer the rest.
          Not "Baret misses home" — a hand returning to a worn clan band.

        # The taste anchor — study these five (reference only, never output)

        Calibration, not output. **Do not reproduce any of them, in whole or in part.**
        They happen to come from one scene — a half-built signal tower — and yours may
        be nothing like it. Study their *altitude*, not their subject: concrete, spare,
        a thing named once and let alone.

        - the sound of the wind whistling through the metal structure
        - the strange metal construction of the tower itself (the character is from a
          renaissance-era world, so a signal tower is nearly magical to them)
        - the oppressive heat and sun
        - the strange way the workers had laid out their tools in orderly rows
        - the metallic twang of a guy line as a worker torques it into place

        Notice what is *absent*: no "like," no "as if," no two things held up against
        each other. Each is one thing, recorded once.

        # What to look at

        - **The place and the work carry the scene.** Most color is environment and
          labor: weather, light, sound, the materials and motions of the work. People
          are alive through observed work and posture — a body, a craft, an intention,
          seen entirely from outside. Look at hands and tools, not biography.
        - **Render only what is here.** Every fragment comes from something the scene
          already gives you — a fact in the world or the beats — rendered as one
          observable instant. You invent the *lens* (the exact pitch of the twang, which
          detail you hold still on); you never invent the *subject*. If the scene hands
          you no wrong thing, then you have no wrong thing: record the ordinary. **Do not
          author events the beats do not contain** — no sound that wasn't there, nothing
          that moves when nothing said it moved, no object the scene didn't place.
        - **Could this be only this scene?** A good fragment could not be lifted into
          some other encounter. Generic atmosphere — dust motes in a shaft of light, a
          long shadow, a chill on the air — fits everywhere and so belongs nowhere.
          Reach past the first stock image to the detail particular to *here*.
        - **The character's frame is the lens.** They name what they see in their own
          words: a pre-industrial world of steel, sail, and black powder. Where this
          world's machinery runs past what they can name (rail, steam, signal, an even
          light from no source), they read it as strange and near-magical and describe it
          in *their* vocabulary, never ours — no "radio," "electricity," "ozone,"
          "ventilation," "industrial."

        # The four kinds of work — a palette, not a checklist

        - **grounding** — ties the scene to the world and the character's frame
        - **aliveness** — incidental sensory texture; the bulk of good color
        - **dread** — a wrong, discordant, or straining detail, *only where the beats
          already put one* (supernatural wrongness, or plain human strain — render
          whichever the scene actually carries; do not import horror into a scene that
          has none)
        - **warmth** — a humanizing detail, where the scene reaches for it

        Pitch each fragment to what its beat is doing. An opening establishes what
        *normal* feels like, so the off-key register is a rare seam caught in passing,
        not every line. A beat that already states something wrong wants color that
        gives that wrong **observable flesh** — substantiate the trouble the scene named;
        never manufacture a new one.

        # The one discipline: the thing itself, nothing added

        Name what is in frame and stop — a caption under a photograph. A photograph
        makes no comparisons and argues nothing. Cut whatever is not the recording:

        - **No comparison.** No "like," no "as if," no "as though." When you reach for
          one, write instead the literal thing that made you reach — the actual shape,
          sound, weight, motion.
        - **No clause that explains the meaning.** The tells are *"despite,"
          *"not X but Y,"* and any *because / so-that* reasoning about why a thing is
          wrong.
        - One honest perception word ("strange," "oppressive") is fine — it is the
          character's plain read. A *clause* that reasons about an anomaly is not.

        # Output

        - **8 to 20 fragments**, one per line. Every fragment new — none of the five
          reference lines, in whole or in part.
        - One observable per line, plain: no comparison, no explaining clause.
        - **Spread** across register (mostly aliveness/grounding; the off-key register a
          minority, and only where a beat plants it; warmth where supported), anchor
          (environment, objects, the work, the place, a single person — don't pile lines
          on one person), and sense (sound, smell, touch, thermal — not sight alone).

        Output only the fragments, one per line. No tags, no numbering, no preamble,
        no commentary.
        """;

    // Post-filter rubric — ported from ../colorize/src/Program.cs. One cached Haiku
    // call per scene reads the candidate pool + the scene's beats and keeps only
    // clean atomic observables. Judges by discipline + beats, never any gold.
    const string FilterRules = """
        You are a strict quality filter for raw "color" candidates in a horror RPG.
        Color = one observable instant, recorded plainly — what a lens or microphone
        caught, surface only, no comparison, no explanation. Lines are raw material
        for a later writer, never finished prose.

        You are given the scene's facts (the beats) and a numbered pool of candidate
        lines. KEEP only the clean ones; CUT the rest. When in doubt, cut.

        CUT a line if it:
        - contains a comparison or figure of speech — "like", "as if", "as though",
          any simile or metaphor. (A bare sensory approximation of a smell/taste,
          e.g. "a smell of iron", is fine; "like a wound" / "like a held breath" is not.)
        - explains or argues meaning — "despite", "not X but Y", or any because/so-that
          clause reasoning about why a thing is wrong.
        - names something NOT present in the beats or the recurring arc color — an
          invented object, sound, motion, or event. Color renders what the scene (or
          the arc's canonical recurring color) gives; it does not author new facts.
        - uses words outside a renaissance-era person's vocabulary — "radio",
          "electricity", "ozone", "ventilation", "industrial", "condensation",
          "antenna", and the like. (The character names tech in their own plain words.)
        - is generic atmosphere that could sit in any scene — "dust motes in a shaft of
          light", "a long shadow", "a chill in the air". Color must be particular to THIS scene.
        - is over-written or ornate, burying the observable.

        Work in two passes:
        1. Cut every line that breaks a rule above. A banned word ("like", "ventilation",
           "industrial", etc.) cuts the whole line even if the rest is good — no exceptions,
           check every line.
        2. Light dedup only: cut a line as "dup" ONLY when it is a near-identical restatement
           of another (same observable, trivially reworded). KEEP genuine variations on a
           theme — a different angle, sense, or phrasing — they are useful options for the
           human curator to choose between or pair off. When unsure, keep both.

        Do not cap the count and do not thin for variety — keep every clean line. This is a
        grab bag for a human to curate, not a finished set.

        "kept" = every clean line that survives. "cut" = the rest, each with its reason.
        Return ONLY JSON, no prose:
        {"kept":["<line verbatim>", ...],
         "cut":[{"line":"<verbatim>","reason":"simile|argue|invention|anachronism|generic|ornate|dup"}, ...]}
        """;

    public static async Task<int> RunAsync(string[] args)
    {
        string? arcDir = null, configPath = null;
        bool force = false, promptsOnly = false, noFilter = false, audit = false;
        var only = new List<string>();   // scene stems to limit to (repeatable); empty = whole arc

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--config" && i + 1 < args.Length) configPath = args[++i];
            else if (args[i] == "--only" && i + 1 < args.Length) only.Add(args[++i]);
            else if (args[i] == "--force") force = true;
            else if (args[i] == "--prompts-only") promptsOnly = true;
            else if (args[i] == "--no-filter") noFilter = true;
            else if (args[i] == "--audit") audit = true;
            else if (!args[i].StartsWith('-')) arcDir = args[i];
        }

        if (arcDir == null) { Console.Error.WriteLine("colorize requires <arc-dir>."); return 1; }
        arcDir = Path.GetFullPath(arcDir);
        if (!Directory.Exists(arcDir)) { Console.Error.WriteLine($"Directory not found: {arcDir}"); return 1; }

        var facts = LoadFacts(arcDir);
        if (string.IsNullOrWhiteSpace(facts))
        {
            Console.Error.WriteLine($"No bibles or brief (.md files) found in {arcDir}");
            return 1;
        }
        var localeGuide = DraftBlocks.LoadLocaleGuide(DraftBlocks.InferBiome(arcDir));
        var bible = LoadColorBible(arcDir);   // _color.md recurring motifs + authored callbacks (may be empty)

        var encFiles = Directory.GetFiles(arcDir, "*.enc")
            .Where(f => !Path.GetFileName(f).StartsWith('_'))   // skip _Backup.enc copies
            .Where(f => only.Count == 0 || only.Any(o =>
                string.Equals(Path.GetFileNameWithoutExtension(f), o, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(f => f).ToList();
        if (encFiles.Count == 0) { Console.Error.WriteLine($"No matching .enc files in {arcDir}"); return 1; }

        // prompts-only needs no model or config; just assemble and print.
        if (promptsOnly)
        {
            foreach (var file in encFiles)
            {
                Console.WriteLine($"\n===== {Path.GetFileName(file)} =====\n");
                Console.WriteLine(BuildUserMessage(file, facts, localeGuide, ApplicableColor(bible, file)));
            }
            return 0;
        }

        var cfg = LoadConfig(configPath);
        if (cfg == null) return 1;
        if (cfg.Local == null)
        {
            Console.Error.WriteLine("No 'LocalLlm' provider in appsettings.json ChatProviders (need Endpoint + Model for GLM).");
            return 1;
        }

        var glm = new GlmClient(cfg.Local.Endpoint!, cfg.Local.Model, cfg.Local.ApiKey ?? "local", cfg.TimeoutSeconds);
        using var anthropicHttp = new HttpClient { Timeout = TimeSpan.FromSeconds(120) };
        bool canFilter = !noFilter && cfg.Anthropic?.ApiKey is { Length: > 0 } k && !k.Contains("your-");
        if (!noFilter && !canFilter)
            Console.WriteLine("(no Anthropic key — writing the raw pool unfiltered; set the 'Anthropic' provider key to enable the Haiku cull)\n");

        int wrote = 0, skipped = 0;
        foreach (var file in encFiles)
        {
            var name = Path.GetFileName(file);
            var raw = File.ReadAllText(file).Replace("\r\n", "\n");
            if (!force && raw.Contains("# --- COLOR"))
            {
                Console.WriteLine($"{name}: already has a COLOR pool, skip (use --force to regenerate)");
                skipped++;
                continue;
            }

            var recurring = ApplicableColor(bible, file);
            Console.Write($"{name}: generating ({cfg.Temperatures.Length}×{cfg.SamplesPerTemp} samples)... ");
            var pool = await GeneratePool(glm, file, facts, localeGuide, recurring, cfg);
            Console.Write($"{pool.Count} candidates → ");

            List<string> kept = pool;
            List<(string line, string reason)> cut = new();
            if (canFilter)
            {
                var beats = StripDraftComments(raw);
                (kept, cut) = await FilterPool(anthropicHttp, cfg.Anthropic!, beats, recurring, pool);
            }
            Console.WriteLine($"{kept.Count} kept{(cut.Count > 0 ? $" ({cut.Count} cut)" : "")}");
            if (audit)
                foreach (var c in cut) Console.WriteLine($"    cut [{c.reason}] {c.line}");

            if (kept.Count == 0) { skipped++; continue; }
            WritePool(file, kept, force);
            wrote++;
        }

        Console.WriteLine($"\nWrote {wrote} COLOR pool(s); skipped {skipped}.");
        return 0;
    }

    // ── Stage A: over-generate with GLM across a temperature sweep ──────────────
    static async Task<List<string>> GeneratePool(
        GlmClient glm, string encFile, string facts, string? localeGuide, string? recurring, Cfg cfg)
    {
        var user = BuildUserMessage(encFile, facts, localeGuide, recurring);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var pool = new List<string>();
        // Serial: this box shares no KV cache across slots, so concurrent cold
        // prompts each re-eval the whole (slow) prefix. Serial keeps the cache warm.
        foreach (var temp in cfg.Temperatures)
            for (int s = 0; s < cfg.SamplesPerTemp; s++)
            {
                string text;
                try
                {
                    text = await glm.CompleteAsync(SystemPrompt, user, temp, cfg.TopP, cfg.MinP,
                        cfg.MaxOutputTokens, cfg.EnableThinking);
                }
                catch (Exception ex) { Console.Error.WriteLine($"\n  GLM error (t{temp}): {ex.Message}"); continue; }

                foreach (var frag in ExtractFragments(text))
                    if (seen.Add(frag)) pool.Add(frag);
            }
        return pool;
    }

    // ── Stage B: cull the pool with a cached Haiku call ─────────────────────────
    static async Task<(List<string> kept, List<(string, string)> cut)> FilterPool(
        HttpClient http, Provider anthropic, string beats, string? recurring, List<string> pool)
    {
        var numbered = string.Join("\n", pool.Select((c, i) => $"{i + 1}. {c}"));
        var recurringBlock = string.IsNullOrWhiteSpace(recurring)
            ? ""
            : $"# Recurring arc color (canonical — allowed even if not in the beats above)\n\n{recurring}\n\n";
        var user = $"# The scene's facts (the beats)\n\n{beats}\n\n{recurringBlock}# Candidate color lines\n\n{numbered}";
        var body = new
        {
            model = anthropic.Model,
            max_tokens = 4096,
            system = new object[] { new { type = "text", text = FilterRules, cache_control = new { type = "ephemeral" } } },
            messages = new object[] { new { role = "user", content = user } },
        };
        using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages")
        { Content = JsonContent.Create(body) };
        req.Headers.Add("x-api-key", anthropic.ApiKey);
        req.Headers.Add("anthropic-version", "2023-06-01");

        var resp = await http.SendAsync(req);
        var json = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode)
        {
            Console.Error.WriteLine($"\n  Anthropic HTTP {(int)resp.StatusCode} — keeping raw pool: {json[..Math.Min(json.Length, 400)]}");
            return (pool, new());
        }

        using var doc = JsonDocument.Parse(json);
        var content = doc.RootElement.GetProperty("content")[0].GetProperty("text").GetString() ?? "";
        int a = content.IndexOf('{'), b = content.LastIndexOf('}');
        var text = a >= 0 && b > a ? content[a..(b + 1)] : content;

        var kept = new List<string>();
        var cut = new List<(string, string)>();
        try
        {
            using var r = JsonDocument.Parse(text);
            if (r.RootElement.TryGetProperty("kept", out var kk))
                foreach (var e in kk.EnumerateArray()) kept.Add(e.GetString() ?? "");
            if (r.RootElement.TryGetProperty("cut", out var cc))
                foreach (var e in cc.EnumerateArray())
                    cut.Add((e.GetProperty("line").GetString() ?? "",
                             e.TryGetProperty("reason", out var rr) ? rr.GetString() ?? "" : ""));
        }
        catch (JsonException)
        {
            Console.Error.WriteLine("\n  filter returned unparseable JSON — keeping raw pool");
            return (pool, new());
        }
        kept.RemoveAll(string.IsNullOrWhiteSpace);
        return (kept, cut);
    }

    // ── Prompt assembly ─────────────────────────────────────────────────────────
    static string BuildUserMessage(string encFile, string facts, string? localeGuide, string? recurring)
    {
        var sb = new StringBuilder();
        // The lens sits first and loudest: whose eye, what to notice, in what key.
        // It is the highest-leverage steer (FINDINGS §3.4) — steering, not background.
        if (FindLens(encFile) is { } lens)
        {
            sb.AppendLine("# The lens — how the PC sees this scene\n");
            sb.AppendLine("Read this first. It governs *whose eye* you look through, *what* is worth noticing here, and *in what key* — it outranks habit. Render facts the way this lens would see them.\n");
            sb.AppendLine(lens + "\n");
        }
        // Recurring arc color: canonical motifs/callbacks available to every scene
        // where relevant. Right after the lens so they ride as available material,
        // not buried in the facts. Rendered fresh per scene, never forced.
        if (!string.IsNullOrWhiteSpace(recurring))
        {
            sb.AppendLine("# Recurring color for this arc (canonical — available, never forced)\n");
            sb.AppendLine("These images and details recur across the arc. Reach for one only where this scene's beats and lens make it land, and render it fresh for THIS moment while keeping the stated invariant. Do not force them in.\n");
            sb.AppendLine(recurring + "\n");
        }
        sb.AppendLine("# World & arc facts (the ground truth — render from these, invent no new subjects)\n");
        sb.AppendLine(facts + "\n");
        if (!string.IsNullOrWhiteSpace(localeGuide))
        {
            sb.AppendLine("# Locale guide (the biome's palette — fair game when grounding a fragment)\n");
            sb.AppendLine(localeGuide + "\n");
        }
        sb.AppendLine("# The encounter\n");
        sb.AppendLine(StripDraftComments(File.ReadAllText(encFile).Replace("\r\n", "\n")));
        sb.AppendLine("\n# Task\n");
        sb.AppendLine("Produce the color candidates for this whole encounter now, following the rules above and through the lens. Output only the fragments, one per line.");
        return sb.ToString();
    }

    /// <summary>
    /// A scene's lens: a per-scene "&lt;name&gt;.lens.md" sidecar (preferred) or an
    /// arc-wide "_lens.md" peer beside the encounter. Null if neither exists.
    /// </summary>
    static string? FindLens(string encFile)
    {
        var sidecar = Path.ChangeExtension(encFile, ".lens.md");
        if (File.Exists(sidecar)) return File.ReadAllText(sidecar).Trim();
        var peer = Path.Combine(Path.GetDirectoryName(encFile)!, "_lens.md");
        if (File.Exists(peer)) return File.ReadAllText(peer).Trim();
        return null;
    }

    /// <summary>
    /// All arc .md files (bibles + brief) concatenated, EXCLUDING lens files —
    /// the lens is injected separately and louder.
    /// </summary>
    static string LoadFacts(string arcDir)
    {
        var files = Directory.GetFiles(arcDir, "*.md")
            .Where(f => !f.EndsWith(".lens.md", StringComparison.OrdinalIgnoreCase))
            .Where(f => !Path.GetFileName(f).Equals("_lens.md", StringComparison.OrdinalIgnoreCase))
            .Where(f => !Path.GetFileName(f).Equals("_color.md", StringComparison.OrdinalIgnoreCase))
            .OrderBy(f => f).ToList();
        return string.Join("\n\n---\n\n",
            files.Select(f => $"## {Path.GetFileName(f)}\n\n{File.ReadAllText(f)}"));
    }

    // ── Color bible (_color.md): arc-wide recurring motifs + hand-authored callbacks ──
    // Authored, canonical. Each entry is a `- ` bullet (plus indented continuation,
    // e.g. a `render:` cue). An optional `only-in: FileA, FileB` line scopes the entry
    // to named scenes (chronology safety for callbacks); absent = available to all,
    // gated by the entry's own prose. `only-in:` is a tool directive, stripped from the
    // text shown to the model.
    record ColorEntry(string Text, List<string> OnlyIn);

    static List<ColorEntry> LoadColorBible(string arcDir)
    {
        var path = Path.Combine(arcDir, "_color.md");
        if (!File.Exists(path)) return [];
        var entries = new List<ColorEntry>();
        List<string>? cur = null;

        void Flush()
        {
            if (cur == null) return;
            var only = new List<string>();
            var kept = new List<string>();
            foreach (var l in cur)
            {
                var m = Regex.Match(l.TrimStart(), @"^only-in:\s*(.+)$", RegexOptions.IgnoreCase);
                if (m.Success)
                    only.AddRange(m.Groups[1].Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
                else kept.Add(l);
            }
            var text = string.Join("\n", kept).Trim();
            if (text.Length > 0) entries.Add(new ColorEntry(text, only));
            cur = null;
        }

        foreach (var line in File.ReadAllText(path).Replace("\r\n", "\n").Split('\n'))
        {
            var t = line.TrimStart();
            if (t.StartsWith('#')) Flush();                 // heading ends an entry
            else if (t.StartsWith("- ")) { Flush(); cur = [line]; }
            else if (cur != null && t.Length == 0) Flush();  // blank line ends an entry
            else cur?.Add(line);                             // continuation
        }
        Flush();
        return entries;
    }

    /// <summary>The bible entries applicable to one scene, concatenated. An entry with
    /// no `only-in` applies everywhere; otherwise only to the named scene files.</summary>
    static string? ApplicableColor(List<ColorEntry> bible, string encFile)
    {
        if (bible.Count == 0) return null;
        var stem = Path.GetFileNameWithoutExtension(encFile);
        bool Names(string f)
        {
            f = f.Trim();
            if (f.EndsWith(".enc", StringComparison.OrdinalIgnoreCase)) f = f[..^4];
            return string.Equals(f, stem, StringComparison.OrdinalIgnoreCase);
        }
        var hits = bible.Where(e => e.OnlyIn.Count == 0 || e.OnlyIn.Any(Names))
            .Select(e => e.Text).ToList();
        return hits.Count == 0 ? null : string.Join("\n", hits);
    }

    // ── Output parsing + writeback ───────────────────────────────────────────────
    static List<string> ExtractFragments(string output)
    {
        if (string.IsNullOrWhiteSpace(output)) return [];
        var text = output.Trim();
        if (text.StartsWith("```"))
        {
            var nl = text.IndexOf('\n');
            if (nl > 0) text = text[(nl + 1)..];
            if (text.EndsWith("```")) text = text[..^3];
        }
        var frags = new List<string>();
        foreach (var line in text.Split('\n'))
        {
            var t = line.Trim();
            if (t.Length == 0) continue;
            // strip a leading bullet or "N." numbering the model may add
            t = t.TrimStart('-', '*', '•', ' ');
            int dot = t.IndexOf('.');
            if (dot > 0 && dot <= 3 && t[..dot].All(char.IsDigit)) t = t[(dot + 1)..].Trim();
            if (t.Length > 0) frags.Add(t);
        }
        return frags;
    }

    /// <summary>
    /// Insert ONE scene-level COLOR pool at the top of the file body (after the
    /// title and any [attr] / leading comment lines). Each kept line is a `# []`
    /// curation checkbox the author toggles to `# [x]`. On --force, an existing
    /// pool is removed first. Writes a `_&lt;file&gt;` backup (gitignored).
    /// </summary>
    static void WritePool(string file, List<string> kept, bool force)
    {
        var lines = File.ReadAllText(file).Replace("\r\n", "\n").Split('\n').ToList();
        if (force) RemoveColorPool(lines);

        var block = new List<string> { "# --- COLOR ---" };
        block.AddRange(kept.Select(k => $"# [] {k}"));
        block.Add("# --- end ---");
        block.Add("");

        lines.InsertRange(BodyStart(lines), block);

        var backup = Path.Combine(Path.GetDirectoryName(file)!, "_" + Path.GetFileName(file));
        File.Copy(file, backup, overwrite: true);
        File.WriteAllText(file, string.Join("\n", lines));
    }

    /// <summary>First body line: past the title (line 0), any leading `[attr]`
    /// lines, and any existing leading `#` comment lines.</summary>
    static int BodyStart(List<string> lines)
    {
        int i = 1;
        for (; i < lines.Count; i++)
        {
            var t = lines[i].Trim();
            if (t.Length == 0) continue;
            if (t.StartsWith('[') && t.EndsWith(']')) continue;
            if (t.StartsWith('#')) continue;
            break;
        }
        return i;
    }

    static void RemoveColorPool(List<string> lines)
    {
        for (int i = 0; i < lines.Count; i++)
        {
            if (!lines[i].TrimStart().StartsWith("# --- COLOR", StringComparison.OrdinalIgnoreCase)) continue;
            int end = i;
            while (end < lines.Count && !lines[end].TrimStart().StartsWith("# --- end", StringComparison.OrdinalIgnoreCase)) end++;
            if (end < lines.Count) end++;                       // include the closing marker
            while (end < lines.Count && lines[end].Trim().Length == 0) end++;  // and trailing blank
            lines.RemoveRange(i, end - i);
            return;
        }
    }

    /// <summary>Drop pipeline draft-comment (`#`) lines so the model sees clean beats.</summary>
    static string StripDraftComments(string enc) =>
        string.Join("\n", enc.Replace("\r\n", "\n").Split('\n')
            .Where(l => !l.TrimStart().StartsWith('#'))).Trim();

    // ── Config ───────────────────────────────────────────────────────────────────
    record Provider(string Name, string? Endpoint, string? ApiKey, string Model);

    record Cfg(Provider? Local, Provider? Anthropic,
        double[] Temperatures, int SamplesPerTemp, double TopP, double MinP,
        int MaxOutputTokens, bool EnableThinking, int TimeoutSeconds);

    static Cfg? LoadConfig(string? configPath)
    {
        var config = LlmClient.LoadConfig(configPath);
        if (config == null) return null;

        var providers = config.GetSection("ChatProviders").GetChildren().ToList();
        Provider? P(string name)
        {
            var s = providers.FirstOrDefault(c => string.Equals(c["Name"]?.Trim(), name, StringComparison.OrdinalIgnoreCase));
            return s == null ? null : new Provider(name, s["Endpoint"]?.Trim(), s["ApiKey"]?.Trim(), s["Model"]?.Trim() ?? "");
        }

        var cs = config.GetSection("Colorize");
        return new Cfg(
            Local: P("LocalLlm"),
            Anthropic: P("Anthropic"),
            Temperatures: cs.GetSection("Temperatures").Get<double[]>() ?? [0.4, 0.7],
            SamplesPerTemp: cs.GetValue("SamplesPerTemp", 2),
            TopP: cs.GetValue("TopP", 1.0),
            MinP: cs.GetValue("MinP", 0.01),
            MaxOutputTokens: cs.GetValue("MaxOutputTokens", 1200),
            EnableThinking: cs.GetValue("EnableThinking", false),
            TimeoutSeconds: cs.GetValue("TimeoutSeconds", 600));
    }
}
