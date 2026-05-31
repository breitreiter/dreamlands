using System.Text.RegularExpressions;

namespace EncounterCli;

/// <summary>
/// Pipeline stage 1 of the arc-writer flow (see plans/arc_writer.md).
/// Walks the .enc files in an arc directory, finds FIXME beats, and for each
/// one prompts qwen for a handful of texture bullets that the human can curate.
/// Bullets are injected as a `# --- COLOR ---` block immediately below the
/// FIXME. Idempotent: a FIXME that already has an adjacent COLOR block is
/// skipped unless --force.
/// </summary>
static class ColorizeCommand
{
    const string SystemPrompt = """
        You are a fact-elaboration pre-pass for an interactive fiction encounter system.

        You receive a BRIEF (the author's high-level description of an arc), a LOCALE GUIDE (the biome's setting and tone), and a single BEAT (a sentence-level scene direction the author has written as a placeholder for prose). You propose discrete TEXTURE BULLETS that the author can pick from to enrich the beat. The author makes the final decision; you are a librarian, not a fiction writer.

        YOU ARE NOT WRITING PROSE. You extract and rearrange concrete detail from the brief and locale guide into bullets the author can choose from.

        OUTPUT FORMAT: each bullet on its own line, starting with '- ', one sentence each. No preamble, no section headers, no summary.

        HARD RULES:

        1. WIKI VOICE ONLY. Write like a wiki entry, not like fiction. Flat, factual, no stylization, no atmospheric flourishes. NO horror register. NO operatic phrasing. NO sensory crescendos. Good: "The brass cylinder on her desk is a Kesharat-issue cipher seal." Bad: "On the desk sits a brass cylinder, its surface etched with the cold geometries of empire."

        2. STATE, NOT EVENTS. Each bullet describes a STANDING CONDITION, something true while the beat holds. Use present tense and stative verbs. Never narrate the PC doing things. Good: "The pencil-stub on the desk is worn to a nub." Bad: "You notice the pencil-stub worn to a nub."

        3. GROUND IN THE SOURCE. Every concrete detail must trace to the BRIEF, the LOCALE GUIDE, or knowledge made canonical in the BEAT. Do not invent names, dates, numbers, props, or events. If the BRIEF gives a character's age range, you may use it; if it does not, do not invent one.

        4. BULLETS MAY CONTRADICT EACH OTHER. The author picks. You may propose "the cup is empty" and "the cup is full of cold tea" for the same beat. Diversity is good. Cross-consistency is the author's job at pick-time.

        5. ONE SENTENCE PER BULLET. No prose paragraphs. No multi-sentence bullets.

        6. NO EM-DASHES. Use commas, semicolons, or separate sentences.

        7. If the beat already names everything that matters and the source has nothing concrete to add, output zero bullets. Padding is worse than silence.
        """;

    static readonly Regex FixmePattern = new(
        @"^(\s*)FIXME(?:\([^)]*\))?:\s*(.*)$",
        RegexOptions.Compiled);

    public static async Task<int> RunAsync(string[] args)
    {
        string? arcDir = null;
        var qwenUrl = "http://imp:8080";
        var minWords = 40;
        var perBeat = 5;
        var force = false;
        var promptsOnly = false;

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--qwen-url" && i + 1 < args.Length) qwenUrl = args[++i];
            else if (args[i] == "--min-words" && i + 1 < args.Length) minWords = int.Parse(args[++i]);
            else if (args[i] == "--per-beat" && i + 1 < args.Length) perBeat = int.Parse(args[++i]);
            else if (args[i] == "--force") force = true;
            else if (args[i] == "--prompts-only") promptsOnly = true;
            else if (!args[i].StartsWith('-')) arcDir = args[i];
        }

        if (arcDir == null)
        {
            Console.Error.WriteLine("colorize requires <arc-dir>.");
            return 1;
        }

        arcDir = Path.GetFullPath(arcDir);
        if (!Directory.Exists(arcDir))
        {
            Console.Error.WriteLine($"Directory not found: {arcDir}");
            return 1;
        }

        var briefFiles = Directory.GetFiles(arcDir, "*.md").OrderBy(f => f).ToList();
        if (briefFiles.Count == 0)
        {
            Console.Error.WriteLine($"No .md brief found in {arcDir}");
            return 1;
        }
        var brief = string.Join("\n\n---\n\n",
            briefFiles.Select(f => $"### {Path.GetFileName(f)}\n\n{File.ReadAllText(f)}"));

        var biome = InferBiome(arcDir);
        var localeGuide = LoadLocaleGuide(biome);
        if (localeGuide == null)
        {
            Console.Error.WriteLine($"No locale guide found for biome '{biome}' (looked under text/encounters/{biome}/tier*/locale_guide.txt)");
            return 1;
        }

        var encFiles = Directory.GetFiles(arcDir, "*.enc").OrderBy(f => f).ToList();
        if (encFiles.Count == 0)
        {
            Console.Error.WriteLine($"No .enc files in {arcDir}");
            return 1;
        }

        QwenClient? client = promptsOnly ? null : new QwenClient(qwenUrl);

        int totalColorized = 0, totalSkipped = 0, totalSilent = 0;
        foreach (var file in encFiles)
        {
            var rel = Path.GetRelativePath(arcDir, file);
            Console.WriteLine($"{rel}:");
            var (colorized, skipped, silent) = await ProcessFileAsync(
                file, brief, localeGuide, client, minWords, perBeat, force, promptsOnly);
            totalColorized += colorized;
            totalSkipped += skipped;
            totalSilent += silent;
        }

        Console.WriteLine();
        Console.WriteLine($"Colorized {totalColorized} beat(s); skipped {totalSkipped} (already-done or below min-words); {totalSilent} beat(s) returned no bullets.");
        return 0;
    }

    static async Task<(int colorized, int skipped, int silent)> ProcessFileAsync(
        string file, string brief, string localeGuide, QwenClient? client,
        int minWords, int perBeat, bool force, bool promptsOnly)
    {
        var content = File.ReadAllText(file).Replace("\r\n", "\n").Replace("\r", "\n");
        var lines = content.Split('\n').ToList();
        var title = lines.Count > 0 ? lines[0].Trim() : Path.GetFileNameWithoutExtension(file);

        var fixmes = new List<int>();
        for (int i = 0; i < lines.Count; i++)
            if (FixmePattern.IsMatch(lines[i]))
                fixmes.Add(i);

        if (fixmes.Count == 0)
        {
            Console.WriteLine("  (no FIXME beats)");
            return (0, 0, 0);
        }

        int colorized = 0, skipped = 0, silent = 0;

        // Reverse order: later insertions don't shift earlier indices
        for (int idx = fixmes.Count - 1; idx >= 0; idx--)
        {
            var li = fixmes[idx];
            var match = FixmePattern.Match(lines[li]);
            var indent = match.Groups[1].Value;
            var body = match.Groups[2].Value.Trim();
            var words = body.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;

            if (words < minWords)
            {
                skipped++;
                continue;
            }

            if (!force && HasAdjacentColorBlock(lines, li))
            {
                skipped++;
                continue;
            }

            var userPrompt = BuildPrompt(brief, localeGuide, title, lines, li, body, perBeat);

            if (promptsOnly)
            {
                Console.WriteLine($"  L{li + 1}: prompt for '{Truncate(body, 50)}'");
                Console.WriteLine("  ---");
                foreach (var ln in userPrompt.Split('\n'))
                    Console.WriteLine("  " + ln);
                Console.WriteLine("  ---");
                continue;
            }

            string? raw;
            try
            {
                Console.Write($"  L{li + 1}: '{Truncate(body, 50)}'... ");
                raw = await client!.CompleteAsync(SystemPrompt, userPrompt, temperature: 0.6, maxTokens: 1200);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
                continue;
            }

            var bullets = ExtractBullets(raw ?? "");
            if (bullets.Count == 0)
            {
                Console.WriteLine("0 bullets (silent)");
                silent++;
                continue;
            }
            Console.WriteLine($"{bullets.Count} bullets");

            var block = BuildColorBlock(indent, bullets);
            lines.InsertRange(li + 1, block);
            colorized++;
        }

        if (colorized > 0 && !promptsOnly)
        {
            var backup = Path.Combine(Path.GetDirectoryName(file)!, "_" + Path.GetFileName(file));
            File.Copy(file, backup, overwrite: true);
            File.WriteAllText(file, string.Join("\n", lines));
            Console.WriteLine($"  → wrote {colorized} block(s); backup at {Path.GetFileName(backup)}");
        }

        return (colorized, skipped, silent);
    }

    /// <summary>
    /// True if a `# --- COLOR ...` line appears between the FIXME and the next
    /// non-comment, non-blank line.
    /// </summary>
    static bool HasAdjacentColorBlock(List<string> lines, int fixmeIndex)
    {
        for (int i = fixmeIndex + 1; i < lines.Count; i++)
        {
            var trimmed = lines[i].TrimStart();
            if (string.IsNullOrEmpty(trimmed)) continue;
            if (!trimmed.StartsWith('#')) return false;
            if (trimmed.StartsWith("# --- COLOR", StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    static string BuildPrompt(
        string brief, string localeGuide, string title,
        List<string> lines, int fixmeIndex, string body, int perBeat)
    {
        var context = DescribeBeatContext(lines, fixmeIndex);
        var register = ExtractRegister(lines[fixmeIndex]) ?? "(unspecified)";
        var low = Math.Max(2, perBeat - 1);
        var high = perBeat + 1;
        return $"""
            BRIEF (arc-specific source — author's intent and content):

            {brief}

            ---

            LOCALE GUIDE (biome-wide setting, palette, characters, sensory register):

            {localeGuide}

            ---

            ENCOUNTER: {title}
            LOCATION IN FILE: {context}
            BEAT REGISTER: {register}

            BEAT (the FIXME beat to colorize):
            {body}

            Propose {low}-{high} texture bullets now. Ground every concrete detail in the BRIEF or LOCALE GUIDE. Wiki voice, state-not-events, may contradict each other. Output bullets only, one per line, prefixed with '- '.
            """;
    }

    /// <summary>
    /// Describe where in the encounter file this FIXME sits, so the prompt
    /// can give the model useful framing without forcing the model to parse
    /// the whole file.
    /// </summary>
    static string DescribeBeatContext(List<string> lines, int fixmeIndex)
    {
        string? choice = null;
        string? branch = null;
        int depth = 0;
        for (int i = fixmeIndex - 1; i >= 0; i--)
        {
            var trimmed = lines[i].TrimStart();
            if (trimmed.StartsWith('#')) continue;
            if (trimmed.StartsWith('}')) { depth++; continue; }
            if (trimmed.EndsWith('{') && depth > 0) { depth--; continue; }

            if (branch == null)
            {
                if (trimmed.StartsWith("@if ", StringComparison.Ordinal))
                    branch = trimmed[4..].TrimEnd('{', ' ', '\t');
                else if (trimmed.StartsWith("} @elif ", StringComparison.Ordinal))
                    branch = trimmed[8..].TrimEnd('{', ' ', '\t');
                else if (trimmed.StartsWith("@elif ", StringComparison.Ordinal))
                    branch = trimmed[6..].TrimEnd('{', ' ', '\t');
                else if (trimmed.StartsWith("} @else", StringComparison.Ordinal) || trimmed.StartsWith("@else", StringComparison.Ordinal))
                    branch = "else";
            }

            if (trimmed.StartsWith("* ", StringComparison.Ordinal))
            {
                var afterStar = trimmed[2..];
                var eq = afterStar.IndexOf('=');
                choice = (eq > 0 ? afterStar[..eq] : afterStar).Trim();
                break;
            }
            if (trimmed.Equals("choices:", StringComparison.Ordinal)) break;
        }

        if (choice == null) return "encounter body (before the choices block)";
        if (branch == null) return $"outcome of choice \"{choice}\"";
        return $"outcome of choice \"{choice}\", branch: {branch}";
    }

    static string? ExtractRegister(string fixmeLine)
    {
        var m = Regex.Match(fixmeLine, @"FIXME\(([^)]+)\):");
        return m.Success ? m.Groups[1].Value.Trim() : null;
    }

    static List<string> ExtractBullets(string output)
    {
        if (string.IsNullOrWhiteSpace(output)) return [];
        var text = output.Trim();

        if (text.StartsWith("```"))
        {
            var firstNl = text.IndexOf('\n');
            if (firstNl > 0) text = text[(firstNl + 1)..];
        }
        if (text.EndsWith("```"))
            text = text[..^3];

        var bullets = new List<string>();
        foreach (var line in text.Split('\n'))
        {
            var trim = line.TrimStart();
            string? bullet = null;
            if (trim.StartsWith("- ", StringComparison.Ordinal)) bullet = trim[2..].Trim();
            else if (trim.StartsWith("* ", StringComparison.Ordinal)) bullet = trim[2..].Trim();
            else if (trim.StartsWith("• ", StringComparison.Ordinal)) bullet = trim[2..].Trim();
            if (!string.IsNullOrEmpty(bullet)) bullets.Add(bullet);
        }
        return bullets;
    }

    static List<string> BuildColorBlock(string indent, List<string> bullets)
    {
        var result = new List<string> { indent + "# --- COLOR ---" };
        foreach (var b in bullets)
            result.Add(indent + "# " + b);
        result.Add(indent + "# --- end ---");
        return result;
    }

    static string Truncate(string s, int max) =>
        s.Length > max ? s[..max] + "..." : s;

    static string InferBiome(string arcDir)
    {
        var parts = arcDir.Split(Path.DirectorySeparatorChar);
        for (int i = 0; i < parts.Length - 1; i++)
            if (parts[i].Equals("arcs", StringComparison.OrdinalIgnoreCase))
                return parts[i + 1];
        return "";
    }

    /// <summary>
    /// Concatenate every tier's locale_guide.txt for the given biome.
    /// Searches up from cwd for text/encounters/&lt;biome&gt;/.
    /// </summary>
    static string? LoadLocaleGuide(string biome)
    {
        if (string.IsNullOrEmpty(biome)) return null;
        var dir = Directory.GetCurrentDirectory();
        while (dir != null)
        {
            var biomeDir = Path.Combine(dir, "text", "encounters", biome);
            if (Directory.Exists(biomeDir))
            {
                var guides = new List<string>();
                foreach (var sub in Directory.GetDirectories(biomeDir).OrderBy(p => p))
                {
                    var lg = Path.Combine(sub, "locale_guide.txt");
                    if (File.Exists(lg))
                        guides.Add($"### {Path.GetFileName(sub)}\n\n{File.ReadAllText(lg)}");
                }
                if (guides.Count > 0)
                    return string.Join("\n\n---\n\n", guides);
            }
            dir = Path.GetDirectoryName(dir);
        }
        return null;
    }
}
