namespace EncounterCli;

/// <summary>
/// Pipeline stage 4 of the arc-writer flow (see plans/arc_writer.md).
/// Cross-provider critic. Runs in two phases:
///
///   factual: for each FIXME with a FACTUAL block, ask haiku to flag
///     invention, PC interiority, contradiction, and scope leak in the
///     FACTUAL prose against the source (beat + COLOR + brief + locale).
///     Writes `# --- CRITIC factual ---`.
///
///   voice: for each VOICED block (one critic block per author/scene
///     variant), ask haiku to flag additions, interiority, and
///     contradictions vs. the FACTUAL ground truth. Writes
///     `# --- CRITIC voiced &lt;author&gt; &lt;scene&gt; ---`.
///
/// Cross-provider matters: the same model that wrote the prose can't
/// honestly grade it. Haiku (via Anthropic) gives independent signal.
///
/// Idempotent per target. Non-blocking: critic findings attach as draft
/// comments; the human curator decides what to act on. To regenerate a
/// critique, delete the corresponding CRITIC block.
/// </summary>
static class CriticCommand
{
    const string FactualSystemPrompt = """
        You are a fact-checker for an interactive fiction encounter system. The author has produced FACTUAL prose intended to integrate a BEAT (a one-sentence scene direction) and a curated set of TEXTURE BULLETS (specific concrete details the author wants reflected in the prose). Your job is to flag every divergence from the source.

        Flag these:
        - INVENTION: any prop, character, name, age, profession, history, or event added beyond the BEAT, the BULLETS, the BRIEF, or the LOCALE GUIDE.
        - PC INTERIORITY: any narration of the player character's feelings, thoughts, intentions, or memories. The player owns those. The PC is "you" / second person.
        - CONTRADICTION: any claim that conflicts with the source.
        - SCOPE LEAK: any narration of events outside this beat's frame (the next beat handles the next moment).

        Do NOT flag:
        - Synonym substitution (said → spoke, walks → strides).
        - Bridging actions that are observable and not loaded (e.g., a character moving between two actions both bullets describe).
        - NPC interior states the PC could plausibly observe (e.g., "she frowned" from a "she's troubled" bullet).
        - Minor stylistic flourishes that don't add facts.

        OUTPUT FORMAT: one finding per line, prefixed with severity in brackets — [CRIT] for must-address, [LOW] for worth-noting. Quote the offending phrase. One line each, no preamble.

        If there are no findings, output exactly: (no findings)
        """;

    const string VoiceSystemPrompt = """
        You are a fact-checker for an interactive fiction encounter system. The author has produced a VOICED draft (a stylized rewrite in an author's voice) of an UNDERLYING RECORD (the canonical factual prose). Voice preservation is desirable; fact corruption is not. Your job is to flag everything the VOICED draft adds, contradicts, or interiorizes beyond the RECORD.

        Flag these:
        - INVENTION: any concrete fact (prop, character detail, sensory specific, action) added beyond the RECORD.
        - PC INTERIORITY: any narration of the player character's feelings, thoughts, intentions, or memories. The PC is "you" / second person.
        - CONTRADICTION: any claim that conflicts with the RECORD.
        - LOADED EMBELLISHMENT: stylistic intensification that introduces a new claim. "Ink that pulsed faintly" adds a fact. "Voice hushed as the grave" adds an unspecified sound quality.

        Do NOT flag:
        - Voice signatures (Lovecraftian phrasings, REH terseness, CAS preciousness, etc.) — those are the point.
        - Synonym substitution, rhythm changes, sentence reordering.
        - Pure intensifiers that don't add facts ("dark blood" vs. "blood", "the stone, cold and heavy" when the RECORD just says "the stone").
        - Restatement of RECORD facts in different words.

        OUTPUT FORMAT: one finding per line, prefixed with severity in brackets — [CRIT] for must-address, [LOW] for worth-noting. Quote the offending phrase. One line each, no preamble.

        If there are no findings, output exactly: (no findings)
        """;

    public static async Task<int> RunAsync(string[] args)
    {
        string? arcDir = null;
        string? configPath = null;
        var phase = "both"; // factual | voice | both
        var force = false;
        var promptsOnly = false;

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--config" && i + 1 < args.Length) configPath = args[++i];
            else if (args[i] == "--phase" && i + 1 < args.Length) phase = args[++i].ToLowerInvariant();
            else if (args[i] == "--force") force = true;
            else if (args[i] == "--prompts-only") promptsOnly = true;
            else if (!args[i].StartsWith('-')) arcDir = args[i];
        }

        if (arcDir == null)
        {
            Console.Error.WriteLine("critic requires <arc-dir>.");
            return 1;
        }
        if (phase is not ("factual" or "voice" or "both"))
        {
            Console.Error.WriteLine($"--phase must be 'factual', 'voice', or 'both' (got '{phase}').");
            return 1;
        }

        arcDir = Path.GetFullPath(arcDir);
        if (!Directory.Exists(arcDir))
        {
            Console.Error.WriteLine($"Directory not found: {arcDir}");
            return 1;
        }

        var brief = DraftBlocks.LoadBrief(arcDir) ?? "";
        var biome = DraftBlocks.InferBiome(arcDir);
        var localeGuide = DraftBlocks.LoadLocaleGuide(biome) ?? "";

        var encFiles = Directory.GetFiles(arcDir, "*.enc")
            .Where(f => !Path.GetFileName(f).StartsWith("_"))
            .OrderBy(f => f).ToList();
        if (encFiles.Count == 0)
        {
            Console.Error.WriteLine($"No .enc files in {arcDir}");
            return 1;
        }

        LlmClient? client = null;
        if (!promptsOnly)
        {
            client = LlmClient.TryCreate(configPath);
            if (client == null) return 1;
        }

        int totalFactual = 0, totalVoice = 0, totalSkipped = 0;
        foreach (var file in encFiles)
        {
            var rel = Path.GetRelativePath(arcDir, file);
            Console.WriteLine($"{rel}:");
            var (f, v, s) = await ProcessFileAsync(
                file, brief, localeGuide, client, phase, force, promptsOnly);
            totalFactual += f;
            totalVoice += v;
            totalSkipped += s;
        }

        Console.WriteLine();
        Console.WriteLine($"Wrote {totalFactual} factual-critic + {totalVoice} voice-critic block(s); skipped {totalSkipped}.");
        return 0;
    }

    static async Task<(int factual, int voice, int skipped)> ProcessFileAsync(
        string file, string brief, string localeGuide,
        LlmClient? client, string phase, bool force, bool promptsOnly)
    {
        var content = File.ReadAllText(file).Replace("\r\n", "\n").Replace("\r", "\n");
        var lines = content.Split('\n').ToList();
        var title = lines.Count > 0 ? lines[0].Trim() : Path.GetFileNameWithoutExtension(file);

        var fixmes = new List<int>();
        for (int i = 0; i < lines.Count; i++)
            if (DraftBlocks.FixmePattern.IsMatch(lines[i]))
                fixmes.Add(i);

        if (fixmes.Count == 0)
        {
            Console.WriteLine("  (no FIXME beats)");
            return (0, 0, 0);
        }

        int factualCount = 0, voiceCount = 0, skipped = 0;

        for (int idx = fixmes.Count - 1; idx >= 0; idx--)
        {
            var li = fixmes[idx];
            var match = DraftBlocks.FixmePattern.Match(lines[li]);
            var indent = match.Groups[1].Value;
            var beatText = match.Groups[2].Value.Trim();
            var register = DraftBlocks.ExtractRegister(lines[li]) ?? "(unspecified)";
            var location = DraftBlocks.DescribeBeatContext(lines, li);

            var factualLines = DraftBlocks.ExtractBlock(lines, li, "FACTUAL");
            var colorLines = DraftBlocks.ExtractBlock(lines, li, "COLOR");

            // Phase: factual
            if (phase is "factual" or "both" && factualLines != null && colorLines != null)
            {
                if (!force && DraftBlocks.HasAdjacentBlock(lines, li, "CRITIC factual"))
                {
                    skipped++;
                }
                else
                {
                    var prompt = BuildFactualPrompt(
                        brief, localeGuide, title, location, register, beatText,
                        colorLines.Where(l => !string.IsNullOrWhiteSpace(l)).ToList(),
                        JoinProse(factualLines));

                    if (promptsOnly)
                    {
                        Console.WriteLine($"  L{li + 1} [factual]: would critique '{DraftBlocks.Truncate(beatText, 40)}'");
                    }
                    else
                    {
                        Console.Write($"  L{li + 1} [factual]: ");
                        var findings = await CallCritic(client!, FactualSystemPrompt, prompt);
                        if (findings != null)
                        {
                            var insertAt = DraftBlocks.FindEndOfDraftStack(lines, li);
                            var block = DraftBlocks.BuildBlock(indent, "CRITIC factual", findings);
                            lines.InsertRange(insertAt, block);
                            factualCount++;
                            Console.WriteLine($"{findings.Count} finding(s)");
                        }
                    }
                }
            }

            // Phase: voice — one critic per VOICED variant
            if (phase is "voice" or "both" && factualLines != null)
            {
                var factualProse = JoinProse(factualLines);
                var adjacentBlocks = DraftBlocks.ListAdjacentBlocks(lines, li);
                var voicedAttrs = adjacentBlocks.Where(a => a.StartsWith("VOICED ", StringComparison.Ordinal)).ToList();

                foreach (var attr in voicedAttrs)
                {
                    // attr is e.g. "VOICED HPL dread"; critic header is "CRITIC voiced HPL dread"
                    var voicedTag = attr["VOICED ".Length..].Trim(); // "HPL dread"
                    var criticAttr = $"CRITIC voiced {voicedTag}";

                    if (!force && DraftBlocks.HasAdjacentBlock(lines, li, criticAttr))
                    {
                        skipped++;
                        continue;
                    }

                    var voicedLines = DraftBlocks.ExtractBlock(lines, li, attr);
                    if (voicedLines == null || voicedLines.All(string.IsNullOrWhiteSpace))
                    {
                        skipped++;
                        continue;
                    }
                    var voicedProse = JoinProse(voicedLines);
                    var prompt = BuildVoicePrompt(factualProse, voicedProse, voicedTag);

                    if (promptsOnly)
                    {
                        Console.WriteLine($"  L{li + 1} [voice {voicedTag}]: would critique");
                    }
                    else
                    {
                        Console.Write($"  L{li + 1} [voice {voicedTag}]: ");
                        var findings = await CallCritic(client!, VoiceSystemPrompt, prompt);
                        if (findings != null)
                        {
                            var insertAt = DraftBlocks.FindEndOfDraftStack(lines, li);
                            var block = DraftBlocks.BuildBlock(indent, criticAttr, findings);
                            lines.InsertRange(insertAt, block);
                            voiceCount++;
                            Console.WriteLine($"{findings.Count} finding(s)");
                        }
                    }
                }
            }
        }

        if ((factualCount > 0 || voiceCount > 0) && !promptsOnly)
        {
            var backup = Path.Combine(Path.GetDirectoryName(file)!, "_" + Path.GetFileName(file));
            File.Copy(file, backup, overwrite: true);
            File.WriteAllText(file, string.Join("\n", lines));
            Console.WriteLine($"  → wrote {factualCount + voiceCount} block(s); backup at {Path.GetFileName(backup)}");
        }

        return (factualCount, voiceCount, skipped);
    }

    static async Task<List<string>?> CallCritic(LlmClient client, string system, string user)
    {
        string? raw;
        try
        {
            raw = await client.CompleteAsync(user, system);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: {ex.Message}");
            return null;
        }
        if (raw == null) return new List<string> { "(critic returned empty)" };

        var text = raw.Trim();
        if (text.Equals("(no findings)", StringComparison.OrdinalIgnoreCase))
            return new List<string> { "(no findings)" };

        var lines = text.Split('\n')
            .Select(l => l.TrimEnd())
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .ToList();
        return lines.Count > 0 ? lines : new List<string> { "(no findings)" };
    }

    static string JoinProse(List<string> lines) =>
        string.Join(" ", lines.Where(l => !string.IsNullOrWhiteSpace(l)).Select(l => l.Trim())).Trim();

    static string BuildFactualPrompt(
        string brief, string localeGuide, string title, string location, string register,
        string beat, List<string> bullets, string factual)
    {
        var bulletList = bullets.Count == 0
            ? "(none)"
            : string.Join("\n", bullets.Select(b => "- " + b));
        return $"""
            BRIEF:

            {brief}

            ---

            LOCALE GUIDE:

            {localeGuide}

            ---

            ENCOUNTER: {title}
            LOCATION: {location}
            BEAT REGISTER: {register}

            BEAT (one-sentence scene direction the author wrote):
            {beat}

            TEXTURE BULLETS (the curated facts the author wants in the prose):
            {bulletList}

            FACTUAL DRAFT (the candidate prose to fact-check):
            {factual}

            Flag every invention, PC interiority, contradiction, and scope leak in the FACTUAL DRAFT. Output one finding per line, severity-prefixed. Output "(no findings)" if the draft is clean.
            """;
    }

    static string BuildVoicePrompt(string factual, string voiced, string voicedTag)
    {
        return $"""
            UNDERLYING RECORD (the canonical factual prose; every claim here is true):
            {factual}

            ---

            VOICED DRAFT ({voicedTag}):
            {voiced}

            Flag every addition, PC interiority, contradiction, and loaded embellishment in the VOICED DRAFT vs. the RECORD. Voice signatures are the point and should be preserved. Output one finding per line, severity-prefixed. Output "(no findings)" if the draft is clean.
            """;
    }
}
