namespace EncounterCli;

/// <summary>
/// Pipeline stage 3 of the arc-writer flow (see plans/arc_writer.md).
/// For each FIXME beat with an adjacent FACTUAL block, qwen produces
/// one voiced variant per (author, scene) pair using the DSPy-optimized
/// programs in voices/. Each variant lands as a separate
/// `# --- VOICED &lt;author&gt; &lt;scene&gt; ---` block stacked after the FACTUAL.
///
/// Defaults to two authors (HPL, REH) per beat, each at the beat's
/// FIXME register (the parenthesized scene-type tag on the FIXME line,
/// or `mundane` if absent). Override with --authors and --scene flags.
///
/// Idempotent per (author, scene) pair: a beat that already has a VOICED
/// block for the same author+scene is skipped unless --force. To
/// regenerate just one variant, delete its block.
/// </summary>
static class VoiceCommand
{
    static readonly HashSet<string> ValidScenes =
        ["mundane", "action", "horror", "dread", "wonder", "revelation"];

    public static async Task<int> RunAsync(string[] args)
    {
        string? arcDir = null;
        var qwenUrl = "http://imp:8080";
        string[] authors = ["HPL", "REH"];
        string? sceneOverride = null;
        var defaultScene = "mundane";
        string? programsDir = null;
        var force = false;
        var promptsOnly = false;
        float temperature = 0.7f;

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--qwen-url" && i + 1 < args.Length) qwenUrl = args[++i];
            else if (args[i] == "--authors" && i + 1 < args.Length)
                authors = args[++i].Split(',', StringSplitOptions.RemoveEmptyEntries).Select(a => a.Trim()).ToArray();
            else if (args[i] == "--scene" && i + 1 < args.Length) sceneOverride = args[++i];
            else if (args[i] == "--default-scene" && i + 1 < args.Length) defaultScene = args[++i];
            else if (args[i] == "--programs" && i + 1 < args.Length) programsDir = args[++i];
            else if (args[i] == "--temperature" && i + 1 < args.Length) temperature = float.Parse(args[++i]);
            else if (args[i] == "--force") force = true;
            else if (args[i] == "--prompts-only") promptsOnly = true;
            else if (!args[i].StartsWith('-')) arcDir = args[i];
        }

        if (arcDir == null)
        {
            Console.Error.WriteLine("voice requires <arc-dir>.");
            return 1;
        }

        arcDir = Path.GetFullPath(arcDir);
        if (!Directory.Exists(arcDir))
        {
            Console.Error.WriteLine($"Directory not found: {arcDir}");
            return 1;
        }

        foreach (var author in authors)
        {
            if (author is not ("HPL" or "REH" or "CAS"))
            {
                Console.Error.WriteLine($"Unknown author '{author}'. Valid: HPL, REH, CAS.");
                return 1;
            }
        }

        if (sceneOverride != null && !ValidScenes.Contains(sceneOverride))
        {
            Console.Error.WriteLine($"Unknown scene '{sceneOverride}'. Valid: {string.Join(", ", ValidScenes)}.");
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

        ExpandClient? client = promptsOnly ? null : new ExpandClient(qwenUrl, programsDir);

        int totalWritten = 0, totalNoFactual = 0, totalDone = 0;
        foreach (var file in encFiles)
        {
            var rel = Path.GetRelativePath(arcDir, file);
            Console.WriteLine($"{rel}:");
            var (written, noFactual, done) = await ProcessFileAsync(
                file, client, authors, sceneOverride, defaultScene,
                temperature, force, promptsOnly);
            totalWritten += written;
            totalNoFactual += noFactual;
            totalDone += done;
        }

        Console.WriteLine();
        Console.WriteLine($"Wrote {totalWritten} VOICED block(s); skipped {totalNoFactual} beats (no adjacent FACTUAL), {totalDone} variants (already-done).");
        return 0;
    }

    static async Task<(int written, int noFactual, int done)> ProcessFileAsync(
        string file, ExpandClient? client,
        string[] authors, string? sceneOverride, string defaultScene,
        float temperature, bool force, bool promptsOnly)
    {
        var content = File.ReadAllText(file).Replace("\r\n", "\n").Replace("\r", "\n");
        var lines = content.Split('\n').ToList();

        var fixmes = new List<int>();
        for (int i = 0; i < lines.Count; i++)
            if (DraftBlocks.FixmePattern.IsMatch(lines[i]))
                fixmes.Add(i);

        if (fixmes.Count == 0)
        {
            Console.WriteLine("  (no FIXME beats)");
            return (0, 0, 0);
        }

        int written = 0, noFactual = 0, done = 0;

        // Process in reverse so earlier indices stay stable
        for (int idx = fixmes.Count - 1; idx >= 0; idx--)
        {
            var li = fixmes[idx];
            var match = DraftBlocks.FixmePattern.Match(lines[li]);
            var indent = match.Groups[1].Value;
            var beatText = match.Groups[2].Value.Trim();

            var factualLines = DraftBlocks.ExtractBlock(lines, li, "FACTUAL");
            if (factualLines == null)
            {
                noFactual++;
                continue;
            }
            // Join non-empty FACTUAL content into a single passage
            var factualProse = string.Join(" ",
                factualLines.Where(l => !string.IsNullOrWhiteSpace(l)).Select(l => l.Trim())).Trim();
            if (string.IsNullOrEmpty(factualProse))
            {
                noFactual++;
                continue;
            }

            var beatRegister = DraftBlocks.ExtractRegister(lines[li]);
            var scene = sceneOverride ?? beatRegister ?? defaultScene;
            if (!ValidScenes.Contains(scene)) scene = defaultScene;

            foreach (var author in authors)
            {
                var kindAttr = $"VOICED {author} {scene}";
                if (!force && DraftBlocks.HasAdjacentBlock(lines, li, kindAttr))
                {
                    done++;
                    continue;
                }

                if (promptsOnly)
                {
                    Console.WriteLine($"  L{li + 1} [{author}/{scene}]: would expand '{DraftBlocks.Truncate(beatText, 40)}'");
                    Console.WriteLine($"    FLAT: {DraftBlocks.Truncate(factualProse, 100)}");
                    continue;
                }

                string? voiced;
                try
                {
                    Console.Write($"  L{li + 1} [{author}/{scene}]: ");
                    voiced = await client!.ExpandAsync(author, scene, factualProse, temperature);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ERROR: {ex.Message}");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(voiced))
                {
                    Console.WriteLine("empty");
                    continue;
                }
                var voicedLines = voiced.Split('\n').Select(l => l.TrimEnd()).ToList();
                while (voicedLines.Count > 0 && string.IsNullOrWhiteSpace(voicedLines[^1]))
                    voicedLines.RemoveAt(voicedLines.Count - 1);
                Console.WriteLine($"{voicedLines.Count(l => !string.IsNullOrEmpty(l))} prose line(s)");

                var insertAt = DraftBlocks.FindEndOfDraftStack(lines, li);
                var block = DraftBlocks.BuildBlock(indent, kindAttr, voicedLines);
                lines.InsertRange(insertAt, block);
                written++;
            }
        }

        if (written > 0 && !promptsOnly)
        {
            var backup = Path.Combine(Path.GetDirectoryName(file)!, "_" + Path.GetFileName(file));
            File.Copy(file, backup, overwrite: true);
            File.WriteAllText(file, string.Join("\n", lines));
            Console.WriteLine($"  → wrote {written} block(s); backup at {Path.GetFileName(backup)}");
        }

        return (written, noFactual, done);
    }
}
