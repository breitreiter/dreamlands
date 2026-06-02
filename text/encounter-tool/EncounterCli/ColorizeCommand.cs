using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;

namespace EncounterCli;

/// <summary>
/// Pipeline stage 1 of the arc-writer flow. Generates "color" — scene texture as
/// raw, atomic observables — for each `.enc` in an arc, as a scene-level pool the
/// author curates.
///
/// Strategy ported from the ../colorize spike's drain_loop.py:
///   A. DRAIN — GLM generates in small batches (5/turn, low temp), fed the
///      already-accepted set each turn so it doesn't re-derive lines. GLM front-
///      loads its strongest material, so a hard turn-cap (~4 turns / ~20 lines)
///      clips the pool before quality degrades into rephrasings and bland
///      inventions — GLM will NOT self-terminate, so it's bounded from outside.
///      A cheap BM25 cull drops lexical near-dups within the loop.
///   B. END-PASS — one Haiku call CUTS (load-bearing invention / explanation /
///      generic / ornate — leaning toward keep; it does NOT cut similes or
///      in-world anachronisms) and then RANKS survivors best-first, so the author
///      reads top-down and stops early. The rank pass is the main event.
/// Survivors are written as ONE `# --- COLOR ---` pool at the top of the file,
/// ranked, each line a `# []` curation checkbox the author toggles to `# [x]`.
///
/// Idempotent: a file that already carries a COLOR pool is skipped unless --force.
/// </summary>
static class ColorizeCommand
{
    // Generation system prompt — ../colorize/prompts/v3.md. The per-turn user
    // message overrides its "8-20" count ("ignore any count target"); the drain
    // loop controls volume from outside via the turn-cap.
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

        - One observable per line, plain: no comparison, no explaining clause.
        - **Spread** across register (mostly aliveness/grounding; the off-key register a
          minority, and only where a beat plants it; warmth where supported), anchor
          (environment, objects, the work, the place, a single person — don't pile lines
          on one person), and sense (sound, smell, touch, thermal — not sight alone).

        Output only the fragments, one per line. No tags, no numbering, no preamble,
        no commentary.
        """;

    // End-pass rubric — ported from ../colorize/src/drain_loop.py END_RUBRIC. Cuts
    // sparingly (load-bearing invention / explanation / generic / ornate; leans
    // toward KEEP; does NOT cut similes or in-world anachronisms), then RANKS the
    // survivors best-first. The ranking is the main work; the cull is secondary.
    const string EndRubric = """
        You are the final critic for raw "color" candidates in a horror RPG. Color is
        one observable instant, recorded plainly — what a lens or microphone caught, surface only.
        Lines are raw material a later writer will shape; they are never finished prose.

        You get the scene's beats and a candidate pool. Do TWO things: CUT the bad lines, then RANK
        what survives.

        CUT a line only if it:
        - LOAD-BEARING INVENTION: introduces a new object, agent, or event that a reader would stop
          and ask about — "whose chisel is this?", "why is there a body here?", "what is moving?".
          These drag a causal story a color pop can't carry.
          *** Lean toward KEEPING. *** Inert atmosphere is GOOD even when the beats did not name it —
          a drip of water, a vein of quartz, a cold draught, a distant unseen sound that stays a
          sensation (not a named creature) assert no new fact to explain, and deepen the register.
          The test is not "is it in the beats" but "would a reader stop to ask a question." When in
          any doubt whether a line is load-bearing or merely atmospheric, KEEP it.
        - EXPLANATION / TELLING: "despite", "not X but Y", any because/so-that clause, or a
          parenthetical that explains the meaning — anything that narrates what a thing *means* or a
          person's motive/interior rather than recording the observable surface.
        - GENERIC: atmosphere that could sit in any scene at all (dust motes in a shaft of light, a
          long shadow). Color must be particular to THIS scene.
        - ORNATE: over-written, the observable buried in decoration.

        DO NOT cut for comparison/simile ("like", "as if") or for anachronistic vocabulary on their
        own — a coherent comparison ("hands like roots") and in-world tech words are fine here. (The
        only bad comparison is one that asserts a physically false state, e.g. "steam like spun gold"
        — steam is white; that is rare, leave it for the human curator, do not hunt for it.)

        Then RANK the kept lines best-first: most concretely observable and most particular to THIS
        scene's specific beats at the top; inert ambient texture kept but lower. An author will read
        top-down and stop early, so the strongest grounded lines must come first.

        Return ONLY JSON, no prose:
        {"kept":["<line verbatim>", ... ranked best-first ...],
         "cut":[{"line":"<verbatim>","reason":"loadbearing|explain|generic|ornate"}, ...]}
        """;

    public static async Task<int> RunAsync(string[] args)
    {
        string? arcDir = null, configPath = null;
        bool force = false, promptsOnly = false, noRank = false, audit = false;
        var only = new List<string>();   // scene stems to limit to (repeatable); empty = whole arc

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--config" && i + 1 < args.Length) configPath = args[++i];
            else if (args[i] == "--only" && i + 1 < args.Length) only.Add(args[++i]);
            else if (args[i] == "--force") force = true;
            else if (args[i] == "--prompts-only") promptsOnly = true;
            else if (args[i] == "--no-filter") noRank = true;
            else if (args[i] == "--audit") audit = true;
            else if (!args[i].StartsWith('-')) arcDir = args[i];
        }

        if (arcDir == null) { Console.Error.WriteLine("colorize requires <arc-dir>."); return 1; }
        arcDir = Path.GetFullPath(arcDir);
        if (!Directory.Exists(arcDir)) { Console.Error.WriteLine($"Directory not found: {arcDir}"); return 1; }

        if (string.IsNullOrWhiteSpace(LoadArcBriefCheck(arcDir)))
        {
            Console.Error.WriteLine($"No bibles or brief (.md files) found in {arcDir}");
            return 1;
        }
        var bible = LoadColorBible(arcDir);   // _color.md recurring motifs + authored callbacks (may be empty)

        var encFiles = Directory.GetFiles(arcDir, "*.enc")
            .Where(f => !Path.GetFileName(f).StartsWith('_'))   // skip _Backup.enc copies
            .Where(f => only.Count == 0 || only.Any(o =>
                string.Equals(Path.GetFileNameWithoutExtension(f), o, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(f => f).ToList();
        if (encFiles.Count == 0) { Console.Error.WriteLine($"No matching .enc files in {arcDir}"); return 1; }

        // prompts-only prints the turn-1 message (no model, no config needed).
        if (promptsOnly)
        {
            foreach (var file in encFiles)
            {
                var beats = StripDraftComments(File.ReadAllText(file).Replace("\r\n", "\n"));
                Console.WriteLine($"\n===== {Path.GetFileName(file)} (turn 1) =====\n");
                Console.WriteLine(BuildTurnMessage(FindLens(file), ApplicableColor(bible, file), beats, "(nothing yet)", 5));
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
        bool canRank = !noRank && cfg.Anthropic?.ApiKey is { Length: > 0 } k && !k.Contains("your-");
        if (!noRank && !canRank)
            Console.WriteLine("(no Anthropic key — writing the raw drained pool without the rank/cull pass; set the 'Anthropic' provider key to enable it)\n");

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
            Console.Write($"{name}: draining ");
            var pool = await DrainGenerate(glm, file, recurring, cfg);
            Console.Write($"= {pool.Count} → ");

            List<string> kept = pool;
            List<(string line, string reason)> cut = new();
            if (canRank)
            {
                var beats = StripDraftComments(raw);
                (kept, cut) = await EndPass(anthropicHttp, cfg.Anthropic!, beats, pool);
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

    // ── Stage A: drain GLM in small batches with a hard turn-cap ─────────────────
    static async Task<List<string>> DrainGenerate(GlmClient glm, string encFile, string? recurring, Cfg cfg)
    {
        var lens = FindLens(encFile);
        var beats = StripDraftComments(File.ReadAllText(encFile).Replace("\r\n", "\n"));
        var accepted = new List<string>();

        for (int turn = 1; turn <= cfg.TurnCap; turn++)
        {
            var already = accepted.Count > 0
                ? string.Join("\n", accepted.Select(a => $"- {a}"))
                : "(nothing yet)";
            var user = BuildTurnMessage(lens, recurring, beats, already, cfg.Batch);

            string text;
            try
            {
                text = await glm.CompleteAsync(SystemPrompt, user, cfg.Temp, cfg.TopP, cfg.MinP,
                    cfg.MaxTokensPerTurn, cfg.EnableThinking);
            }
            catch (Exception ex) { Console.Error.WriteLine($"\n  GLM turn {turn} error: {ex.Message}"); break; }

            // Keep a candidate only if it's a BM25-novel line vs the accepted set AND
            // vs the lines already taken this turn (the batch can repeat itself).
            var novel = new List<string>();
            foreach (var c in ParseLines(text))
                if (Bm25Novel(c, accepted, cfg.Bm25Threshold) && Bm25Novel(c, novel, cfg.Bm25Threshold))
                    novel.Add(c);

            accepted.AddRange(novel);
            Console.Write($"t{turn}+{novel.Count} ");
            if (novel.Count <= cfg.BailAt) break;   // the well is dry; stop before GLM invents filler
        }
        return accepted;
    }

    static string BuildTurnMessage(string? lens, string? recurring, string beats, string already, int batch)
    {
        var sb = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(lens))
            sb.Append("## LENS (read first — whose eye, what to notice, in what key)\n").Append(lens).Append("\n\n");
        if (!string.IsNullOrWhiteSpace(recurring))
            sb.Append("## RECURRING ARC COLOR (canonical, available where it fits this scene — render fresh, never forced)\n")
              .Append(recurring).Append("\n\n");
        sb.Append("## SCENE BEATS\n").Append(beats).Append("\n\n");
        sb.Append("## ALREADY RECORDED — do not repeat these or restate them in other words:\n").Append(already).Append("\n\n");
        sb.Append($"## YOUR TASK THIS TURN\nGive up to {batch} NEW color fragments not already recorded above ")
          .Append("and not rewordings of them. Ignore any count target in your instructions. Record ONLY ")
          .Append("observations grounded in this scene. One fragment per line, no numbering, no commentary.");
        return sb.ToString();
    }

    // ── Stage B: end-pass cut + rank with a cached Haiku call ───────────────────
    static async Task<(List<string> kept, List<(string, string)> cut)> EndPass(
        HttpClient http, Provider anthropic, string beats, List<string> pool)
    {
        var user = "## SCENE BEATS\n" + beats + "\n\n## CANDIDATE POOL\n"
                 + string.Join("\n", pool.Select(l => $"- {l}"));
        var body = new
        {
            model = anthropic.Model,
            max_tokens = 2000,
            system = new object[] { new { type = "text", text = EndRubric, cache_control = new { type = "ephemeral" } } },
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
            Console.Error.WriteLine("\n  end-pass returned unparseable JSON — keeping raw pool");
            return (pool, new());
        }
        kept.RemoveAll(string.IsNullOrWhiteSpace);
        return (kept, cut);
    }

    // ── BM25 near-dup cull (ported from drain_loop.py) ──────────────────────────
    static readonly HashSet<string> Stop = new(
        ("the a an of in on to into and or with its it that this as at by from for over under up "
       + "so still where when one two has have been is are was").Split(' '),
        StringComparer.Ordinal);

    static string Stem(string w)
    {
        foreach (var s in new[] { "ing", "ed", "es", "s" })
            if (w.EndsWith(s, StringComparison.Ordinal) && w.Length - s.Length >= 3) return w[..^s.Length];
        return w;
    }

    static List<string> Toks(string s) =>
        Regex.Matches(s.ToLowerInvariant(), "[a-z]+")
            .Select(m => m.Value).Where(w => w.Length > 2 && !Stop.Contains(w)).Select(Stem).ToList();

    /// <summary>True if `line` is lexically novel vs every line in `accepted`
    /// (tf-idf cosine below the threshold). Mirrors drain_loop.py bm25_novel.</summary>
    static bool Bm25Novel(string line, List<string> accepted, double thr)
    {
        if (accepted.Count == 0) return true;
        var corpus = accepted.Append(line).ToList();
        var df = new Dictionary<string, int>();
        foreach (var t in corpus)
            foreach (var w in Toks(t).ToHashSet()) df[w] = df.GetValueOrDefault(w) + 1;
        int n = corpus.Count;
        var idf = df.ToDictionary(kv => kv.Key, kv => Math.Log((n + 1.0) / (kv.Value + 0.5)));

        Dictionary<string, double> Vec(string t)
        {
            var v = new Dictionary<string, double>();
            foreach (var w in Toks(t)) v[w] = v.GetValueOrDefault(w) + 1;
            return v.ToDictionary(kv => kv.Key, kv => kv.Value * idf.GetValueOrDefault(kv.Key));
        }

        var lv = Vec(line);
        double dl = Math.Sqrt(lv.Values.Sum(x => x * x));
        foreach (var acc in accepted)
        {
            var av = Vec(acc);
            var common = lv.Keys.Where(av.ContainsKey).ToList();
            if (common.Count == 0) continue;
            double num = common.Sum(w => lv[w] * av[w]);
            double da = Math.Sqrt(av.Values.Sum(x => x * x));
            if (dl > 0 && da > 0 && num / (dl * da) >= thr) return false;
        }
        return true;
    }

    static List<string> ParseLines(string text)
    {
        var outl = new List<string>();
        if (string.IsNullOrWhiteSpace(text)) return outl;
        text = text.Trim();
        if (text.StartsWith("```"))
        {
            var nl = text.IndexOf('\n');
            if (nl > 0) text = text[(nl + 1)..];
            if (text.EndsWith("```")) text = text[..^3];
        }
        foreach (var raw in text.Split('\n'))
        {
            var l = raw.Trim().TrimStart('-', '*', '•', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '.', ' ').Trim();
            l = Regex.Replace(l, @"\[.*?\]", "").Trim();
            if (l.Length > 8 && !l.StartsWith('#')) outl.Add(l);
        }
        return outl;
    }

    // ── Lens + color bible inputs ────────────────────────────────────────────────

    /// <summary>A scene's lens: a per-scene "&lt;name&gt;.lens.md" sidecar (preferred)
    /// or an arc-wide "_lens.md" peer beside the encounter. Null if neither exists.</summary>
    static string? FindLens(string encFile)
    {
        var sidecar = Path.ChangeExtension(encFile, ".lens.md");
        if (File.Exists(sidecar)) return File.ReadAllText(sidecar).Trim();
        var peer = Path.Combine(Path.GetDirectoryName(encFile)!, "_lens.md");
        if (File.Exists(peer)) return File.ReadAllText(peer).Trim();
        return null;
    }

    /// <summary>Sanity check that the dir is a real arc: any .md that isn't a lens or
    /// the color bible (a bible or brief). The bibles aren't fed to generation — the
    /// lens + color bible + beats carry the context — but their presence gates the run.</summary>
    static string LoadArcBriefCheck(string arcDir) =>
        string.Join("", Directory.GetFiles(arcDir, "*.md")
            .Where(f => !f.EndsWith(".lens.md", StringComparison.OrdinalIgnoreCase))
            .Where(f => !Path.GetFileName(f).Equals("_lens.md", StringComparison.OrdinalIgnoreCase))
            .Where(f => !Path.GetFileName(f).Equals("_color.md", StringComparison.OrdinalIgnoreCase))
            .Select(f => Path.GetFileName(f)));

    // _color.md: arc-wide recurring motifs + hand-authored callbacks. Authored,
    // canonical. Each entry is a `- ` bullet (plus indented continuation, e.g. a
    // `render:` cue). An optional `only-in: FileA, FileB` line scopes the entry to
    // named scenes (chronology safety for callbacks); absent = available to all.
    // `only-in:` is a tool directive, stripped from the text shown to the model.
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

    // ── Writeback ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Insert ONE scene-level COLOR pool at the top of the file body (after the
    /// title and any [attr] / leading comment lines), kept in ranked order. Each
    /// line is a `# []` curation checkbox the author toggles to `# [x]`. On --force,
    /// an existing pool is removed first. Writes a `_&lt;file&gt;` backup (gitignored).
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
        double Temp, double TopP, double MinP, int MaxTokensPerTurn,
        int Batch, int TurnCap, int BailAt, double Bm25Threshold,
        bool EnableThinking, int TimeoutSeconds);

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
            Temp: cs.GetValue("Temp", 0.3),
            TopP: cs.GetValue("TopP", 1.0),
            MinP: cs.GetValue("MinP", 0.01),
            MaxTokensPerTurn: cs.GetValue("MaxTokensPerTurn", 500),
            Batch: cs.GetValue("Batch", 5),
            TurnCap: cs.GetValue("TurnCap", 4),
            BailAt: cs.GetValue("BailAt", 1),
            Bm25Threshold: cs.GetValue("Bm25Threshold", 0.5),
            EnableThinking: cs.GetValue("EnableThinking", false),
            TimeoutSeconds: cs.GetValue("TimeoutSeconds", 600));
    }
}
