using System.Text;
using System.Text.RegularExpressions;

namespace EncounterCli;

/// <summary>
/// Regression gate for the factual stage: `factual --parity [N] [--slug s]...`.
///
/// Runs the SHIPPING factual path (FactualCommand.SystemPrompt + the Toll-Bridge few-shot
/// + BuildPrompt + HouseStyle + GLM via LocalLlm) over the six ../factual prototype scenes
/// kept self-contained under eval/factual-parity/, then asserts the hard-won invariants the
/// prototype's stability report established. A green table means the pipeline reproduces the
/// ../factual strategy; the prototype's own outputs sit in eval/factual-parity/known-good/
/// for eyeball comparison.
///
/// Each scene is generated per beat (the production shape) and concatenated into a scene
/// report; invariants are scene-level. This is GLM-call-heavy (~33 beats across 6 scenes,
/// minutes apiece on imp), so --slug narrows the run for a quick check.
/// </summary>
static class FactualEval
{
    public static async Task<int> RunAsync(string[] args)
    {
        string? configPath = null, dirOverride = null;
        int n = 1;
        var slugFilter = new List<string>();

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--config" && i + 1 < args.Length) configPath = args[++i];
            else if (args[i] == "--dir" && i + 1 < args.Length) dirOverride = args[++i];
            else if (args[i] == "--slug" && i + 1 < args.Length) slugFilter.Add(args[++i]);
            else if (int.TryParse(args[i], out var parsed)) n = Math.Max(1, parsed);
        }

        var dir = dirOverride != null ? Path.GetFullPath(dirOverride) : FindFixtureDir();
        if (dir == null || !Directory.Exists(dir))
        {
            Console.Error.WriteLine("Could not locate eval/factual-parity fixtures (use --dir <path>).");
            return 1;
        }

        var slugs = Directory.GetFiles(dir, "*.enc").Select(f => Path.GetFileNameWithoutExtension(f))
            .Where(s => slugFilter.Count == 0 || slugFilter.Any(sf => string.Equals(sf, s, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(s => s).ToList();
        if (slugs.Count == 0) { Console.Error.WriteLine($"No matching fixtures in {dir}"); return 1; }

        var cfg = FactualCommand.LoadCfg(configPath);
        if (cfg == null) return 1;

        Console.WriteLine($"Parity gate: {slugs.Count} scene(s) × {n} run(s) over {dir}");
        Console.WriteLine("(per-beat generation against GLM — this is slow.)\n");

        var rows = new List<(string slug, int run, int words, int quotes, int emdash, bool ok, List<string> fails)>();
        foreach (var slug in slugs)
        {
            var (title, beats, observables) = LoadScene(Path.Combine(dir, slug + ".enc"), Path.Combine(dir, slug + ".color.md"));
            for (int run = 0; run < n; run++)
            {
                Console.Write($"  {slug} #{run}: ");
                string report;
                try
                {
                    report = await GenerateReport(cfg, title, beats, observables);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ERROR: {ex.Message}");
                    rows.Add((slug, run, 0, 0, 0, false, ["generation error"]));
                    continue;
                }

                var runsDir = Path.Combine(dir, "runs");
                Directory.CreateDirectory(runsDir);
                File.WriteAllText(Path.Combine(runsDir, $"{slug}.{run:00}.md"), report + "\n");

                int words = report.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
                int quotes = report.Count(c => c == '"');
                int emdash = report.Count(c => c is '—' or '–');
                var fails = CheckInvariants(slug, report);
                bool ok = fails.Count == 0;
                rows.Add((slug, run, words, quotes, emdash, ok, fails));
                Console.WriteLine($"{(ok ? "PASS" : "FAIL")}  words={words} quotes={quotes} emdash={emdash}"
                    + (ok ? "" : "  [" + string.Join("; ", fails) + "]"));
            }
        }

        Console.WriteLine("\n" + new string('=', 64));
        Console.WriteLine($"{"scene",-22} {"run",3} {"words",6} {"quotes",7} {"emdash",7}  result");
        foreach (var r in rows)
            Console.WriteLine($"{r.slug,-22} {r.run,3} {r.words,6} {r.quotes,7} {r.emdash,7}  {(r.ok ? "PASS" : "FAIL: " + string.Join("; ", r.fails))}");

        int failed = rows.Count(r => !r.ok);
        Console.WriteLine(new string('=', 64));
        Console.WriteLine(failed == 0
            ? $"\nALL PASS ({rows.Count} generation(s)). The pipeline reproduces the ../factual invariants."
            : $"\n{failed}/{rows.Count} generation(s) FAILED an invariant.");
        return failed == 0 ? 0 : 1;
    }

    /// <summary>Generate the scene per beat (production shape) and concatenate into one report.</summary>
    static async Task<string> GenerateReport(
        FactualCommand.FactualCfg cfg, string title,
        List<(string register, string text)> beats, List<string> observables)
    {
        const string brief = "(standalone parity scene — no arc brief; rely on the beats and color only)";
        const string locale = "(standalone parity scene — no locale guide)";
        var blocks = new List<string>();
        for (int i = 0; i < beats.Count; i++)
        {
            var userPrompt = FactualCommand.BuildPrompt(
                brief, locale, title, "(none)", beats, i, "encounter body (parity scene)", observables);
            var raw = await cfg.Glm.CompleteAsync(FactualCommand.BuildMessages(userPrompt),
                cfg.Temp, cfg.TopP, cfg.MinP, cfg.MaxTokens, enableThinking: false);
            var prose = FactualCommand.HouseStyle(FactualCommand.ExtractProse(raw) ?? "");
            if (!string.IsNullOrWhiteSpace(prose)) blocks.Add(prose.Trim());
        }
        return string.Join("\n\n", blocks);
    }

    // ── Invariants (the ../factual stability-report lessons) ─────────────────────
    static List<string> CheckInvariants(string slug, string report)
    {
        var fails = new List<string>();
        var lc = report.ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(report)) { fails.Add("empty report"); return fails; }

        // Universal: the deterministic em-dash guarantee (validates HouseStyle).
        if (report.Contains('—') || report.Contains('–')) fails.Add("em-dash leaked");

        // Universal: "clock" as a verb (= notice/recognize) is modern slang the beats carry
        // (e.g. harvest-gift's "she clocks the PC's ring"). Factual must paraphrase the fact,
        // never preserve the word. See project_voicer_preserves_slang.
        if (Regex.IsMatch(lc, @"\bclock(s|ed|ing)\b")) fails.Add("'clock' (slang for notice) preserved, not paraphrased");

        switch (slug)
        {
            case "harvest-gift":          // the girl speaks — quoted dialogue must appear
            case "hermit-sallow-fen":     // the hermit offers food / warns — speech
            case "the-plaza":             // Torben speaks the dome-light line
                if (!report.Contains('"')) fails.Add("no quoted dialogue (speech scene)");
                break;

            case "intro-aldgate":         // the ring is a script-gated reveal — never name it
                if (lc.Contains("signet")) fails.Add("named the signet (script-gated reveal leaked)");
                if (Regex.IsMatch(lc, @"\bring\b")) fails.Add("named the ring (script-gated reveal leaked)");
                // the dread beat must land as a weight on the hand (any phrasing), not be dropped
                if (!(Regex.IsMatch(lc, @"weigh") && (lc.Contains("hand") || lc.Contains("palm") || lc.Contains("finger"))))
                    fails.Add("dread beat lost (the unfamiliar weight on the hand)");
                // no beat attributes speech (a dispute + a look) — quoted dialogue here is invented
                if (report.Contains('"')) fails.Add("invented dialogue (no beat attributes speech)");
                break;

            case "record-in-stone":       // both oath-fragments reproduced, not summarized
                if (!(lc.Contains("alain") && lc.Contains("harvest moon"))) fails.Add("Alain oath not reproduced");
                if (!(lc.Contains("matthis") && lc.Contains("vigil"))) fails.Add("Matthis oath not reproduced");
                break;

            case "wrenbury-market":       // canteen->casket misparse; battlefield-relic misparse
                if (lc.Contains("casket")) fails.Add("'canteen' misparsed as 'casket'");
                if (!(lc.Contains("relic") || lc.Contains("market"))) fails.Add("'battlefield relic market' misparsed away");
                // the argument is observed and the woman "doesn't call out yet" — no quoted speech
                if (report.Contains('"')) fails.Add("invented dialogue (the scene is observed, no one speaks)");
                break;
        }
        return fails;
    }

    // ── ../factual paired-file parser (mirrors src/write.py load_scene) ──────────
    static (string title, List<(string register, string text)> beats, List<string> observables)
        LoadScene(string encPath, string colorPath)
    {
        var enc = File.ReadAllText(encPath).Replace("\r\n", "\n").Split('\n');
        var title = enc.Length > 0 ? enc[0].Trim() : Path.GetFileNameWithoutExtension(encPath);
        var beats = new List<(string, string)>();
        foreach (var ln in enc)
        {
            var m = Regex.Match(ln, @"^\s*FIXME\((\w+)\):\s*(.*)");
            if (m.Success) beats.Add((m.Groups[1].Value, m.Groups[2].Value.Trim()));
        }

        // A color line is an observable iff it carries an [axis · subject · sense] tag (the
        // `·`); untagged curation notes are skipped. Strip the [..] tag and backticks.
        var observables = new List<string>();
        foreach (var ln in File.ReadAllText(colorPath).Replace("\r\n", "\n").Split('\n'))
        {
            var s = ln.Trim();
            if (s.StartsWith("- ") && s.Contains('·'))
            {
                var text = Regex.Replace(s[2..], @"\[[^\]]*\]", "").Replace("`", "").Trim();
                if (text.Length > 0) observables.Add(text);
            }
        }
        return (title, beats, observables);
    }

    static string? FindFixtureDir()
    {
        foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            var d = start;
            while (d != null)
            {
                var candidate = Path.Combine(d, "eval", "factual-parity");
                if (Directory.Exists(candidate)) return candidate;
                candidate = Path.Combine(d, "text", "encounter-tool", "eval", "factual-parity");
                if (Directory.Exists(candidate)) return candidate;
                d = Path.GetDirectoryName(d);
            }
        }
        return null;
    }
}
