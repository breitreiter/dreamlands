namespace EncounterCli;

/// <summary>
/// Pipeline stage 2 of the arc-writer flow (see plans/arc_writer.md).
/// For each FIXME beat with an adjacent (curated) COLOR block, qwen
/// integrates the beat + the kept color bullets into a single passage of
/// factual prose. Output is written as a `# --- FACTUAL ---` block
/// immediately after the COLOR block.
///
/// "Factual" here means: externally-verifiable fiction. No PC interiority,
/// no invention beyond the supplied beat + color + brief + locale. Voice
/// is left flat; the voice pass handles stylistic transformation.
///
/// Idempotent: a FIXME with an adjacent FACTUAL block is skipped unless
/// --force. A FIXME without an adjacent COLOR block is skipped (colorize
/// is the upstream).
/// </summary>
static class FactualCommand
{
    const string SystemPrompt = """
        You are a factual writer for an interactive fiction encounter system.

        You receive a BEAT (a sentence-level scene direction the author wrote), TEXTURE BULLETS (a curated list of state-facts the author wants reflected in the prose), and CONTEXT (the surrounding encounter scene and the arc brief). Your job is to integrate the beat and the texture bullets into a single passage of prose that fits the structural slot.

        HARD RULES:

        1. EXTERNALLY-VERIFIABLE FICTION ONLY. Describe what is seen, heard, said, and done. Never narrate the PC's feelings, thoughts, intentions, or memories — the player owns those. Good: "She sets the cup down without finishing it." Bad: "She sets the cup down, troubled by the silence."

        2. INTEGRATE EVERY TEXTURE BULLET. Each bullet is a fact the author has chosen to keep. Weave them in as concrete detail. Do not list them, do not skip them, do not summarize them out.

        3. DO NOT INVENT BEYOND CONTEXT. The beat, the bullets, the encounter body, the brief, and the locale guide are your sources. Do not add new props, characters, events, or facts. Do not name unnamed characters. Do not assign ages, professions, or histories the sources do not supply.

        4. MATCH THE BEAT'S REGISTER. FIXME(mundane) means grounded and procedural, no flourish. FIXME(dread) means weight allowed but stay externally observable, no operatic phrasing. FIXME(action) means clean motion, no interiority. FIXME(horror) means visceral but stay external. The voice pass handles stylistic transformation; you write factual.

        5. END WHEN THE BEAT IS ESTABLISHED. Do not extend past what the beat is about. The next beat handles the next moment.

        6. NO EM-DASHES. Use commas, semicolons, or separate sentences.

        7. OUTPUT PROSE ONLY. No headers, no bullets, no commentary, no quoting of the input. One paragraph (or two short ones if the beat naturally splits).
        """;

    public static async Task<int> RunAsync(string[] args)
    {
        string? arcDir = null;
        var qwenUrl = "http://imp:8080";
        var force = false;
        var promptsOnly = false;

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--qwen-url" && i + 1 < args.Length) qwenUrl = args[++i];
            else if (args[i] == "--force") force = true;
            else if (args[i] == "--prompts-only") promptsOnly = true;
            else if (!args[i].StartsWith('-')) arcDir = args[i];
        }

        if (arcDir == null)
        {
            Console.Error.WriteLine("factual requires <arc-dir>.");
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
            .Where(f => !Path.GetFileName(f).StartsWith("_"))
            .OrderBy(f => f).ToList();
        if (encFiles.Count == 0)
        {
            Console.Error.WriteLine($"No .enc files in {arcDir}");
            return 1;
        }

        QwenClient? client = promptsOnly ? null : new QwenClient(qwenUrl);

        int totalWritten = 0, totalNoColor = 0, totalDone = 0;
        foreach (var file in encFiles)
        {
            var rel = Path.GetRelativePath(arcDir, file);
            Console.WriteLine($"{rel}:");
            var (written, noColor, done) = await ProcessFileAsync(file, brief, localeGuide, client, force, promptsOnly);
            totalWritten += written;
            totalNoColor += noColor;
            totalDone += done;
        }

        Console.WriteLine();
        Console.WriteLine($"Wrote {totalWritten} FACTUAL block(s); skipped {totalNoColor} (no adjacent COLOR), {totalDone} (already-done).");
        return 0;
    }

    static async Task<(int written, int noColor, int done)> ProcessFileAsync(
        string file, string brief, string localeGuide, QwenClient? client,
        bool force, bool promptsOnly)
    {
        var content = File.ReadAllText(file).Replace("\r\n", "\n").Replace("\r", "\n");
        var lines = content.Split('\n').ToList();
        var title = lines.Count > 0 ? lines[0].Trim() : Path.GetFileNameWithoutExtension(file);
        var encounterBody = DraftBlocks.ExtractEncounterBody(lines);

        var fixmes = new List<int>();
        for (int i = 0; i < lines.Count; i++)
            if (DraftBlocks.FixmePattern.IsMatch(lines[i]))
                fixmes.Add(i);

        if (fixmes.Count == 0)
        {
            Console.WriteLine("  (no FIXME beats)");
            return (0, 0, 0);
        }

        int written = 0, noColor = 0, done = 0;
        for (int idx = fixmes.Count - 1; idx >= 0; idx--)
        {
            var li = fixmes[idx];
            var match = DraftBlocks.FixmePattern.Match(lines[li]);
            var indent = match.Groups[1].Value;
            var beatText = match.Groups[2].Value.Trim();

            if (!force && DraftBlocks.HasAdjacentBlock(lines, li, "FACTUAL"))
            {
                done++;
                continue;
            }

            var colorBullets = DraftBlocks.ExtractBlock(lines, li, "COLOR");
            if (colorBullets == null)
            {
                noColor++;
                continue;
            }
            // Filter empty lines (curation may leave blanks behind)
            colorBullets = colorBullets.Where(b => !string.IsNullOrWhiteSpace(b)).ToList();
            if (colorBullets.Count == 0)
            {
                noColor++;
                continue;
            }

            var register = DraftBlocks.ExtractRegister(lines[li]) ?? "(unspecified)";
            var location = DraftBlocks.DescribeBeatContext(lines, li);

            var userPrompt = BuildPrompt(brief, localeGuide, title, encounterBody, location, register, beatText, colorBullets);

            if (promptsOnly)
            {
                Console.WriteLine($"  L{li + 1}: prompt for '{DraftBlocks.Truncate(beatText, 50)}' ({colorBullets.Count} bullets)");
                Console.WriteLine("  ---");
                foreach (var ln in userPrompt.Split('\n'))
                    Console.WriteLine("  " + ln);
                Console.WriteLine("  ---");
                continue;
            }

            string? raw;
            try
            {
                Console.Write($"  L{li + 1}: '{DraftBlocks.Truncate(beatText, 50)}' ({colorBullets.Count} color)... ");
                raw = await client!.CompleteAsync(SystemPrompt, userPrompt, temperature: 0.5, maxTokens: 1500);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
                continue;
            }

            var prose = ExtractProse(raw ?? "");
            if (string.IsNullOrEmpty(prose))
            {
                Console.WriteLine("empty");
                continue;
            }
            var proseLines = prose.Split('\n').Select(l => l.TrimEnd()).ToList();
            // Trim trailing blanks
            while (proseLines.Count > 0 && string.IsNullOrWhiteSpace(proseLines[^1])) proseLines.RemoveAt(proseLines.Count - 1);
            var paragraphCount = proseLines.Count(l => !string.IsNullOrWhiteSpace(l));
            Console.WriteLine($"{paragraphCount} prose line(s)");

            // Insert after the COLOR block's `# --- end ---`
            var insertAt = DraftBlocks.FindInsertionAfter(lines, li, "COLOR");
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

        return (written, noColor, done);
    }

    static string BuildPrompt(
        string brief, string localeGuide, string title, string encounterBody,
        string location, string register, string beat, List<string> bullets)
    {
        var bulletList = string.Join("\n", bullets.Select(b => "- " + b));
        return $"""
            BRIEF (arc-specific source — the author's intent for this arc):

            {brief}

            ---

            LOCALE GUIDE (biome-wide setting, register, palette):

            {localeGuide}

            ---

            ENCOUNTER: {title}

            ENCOUNTER BODY (scene-setting prose at the top of this encounter — the static frame around all choices):
            {encounterBody}

            LOCATION OF THIS BEAT IN THE FILE: {location}
            BEAT REGISTER: {register}

            BEAT (the scene direction to factualize):
            {beat}

            TEXTURE BULLETS (curated by the author — integrate every one):
            {bulletList}

            Write the integrated passage now. Externally-verifiable fiction only. No PC interiority. Integrate every bullet. Match the register. End when the beat is established. Prose only.
            """;
    }

    static string? ExtractProse(string output)
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

        text = text.Trim();

        // Strip common LLM preambles
        var firstLine = text.Split('\n', 2)[0];
        string[] preambles = ["Here is", "Here's", "Sure", "I'll", "I'd", "Below is", "The integrated"];
        if (preambles.Any(p => firstLine.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
        {
            var nl = text.IndexOf('\n');
            if (nl > 0) text = text[(nl + 1)..].TrimStart();
        }

        return text.Length > 0 ? text : null;
    }
}
