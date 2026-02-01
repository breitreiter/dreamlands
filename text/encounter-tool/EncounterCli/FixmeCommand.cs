namespace EncounterCli;

static class FixmeCommand
{
    public static async Task<int> RunAsync(string[] args)
    {
        string? filePath = null;
        string? configPath = null;
        var promptsOnly = false;
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--config" && i + 1 < args.Length) { configPath = args[i + 1]; i++; }
            else if (args[i] == "--prompts-only") promptsOnly = true;
            else if (!args[i].StartsWith('-')) filePath = args[i];
        }

        if (string.IsNullOrEmpty(filePath))
        {
            Console.Error.WriteLine("fixme requires <file.enc>.");
            return 1;
        }

        filePath = Path.GetFullPath(filePath);
        if (!File.Exists(filePath))
        {
            Console.Error.WriteLine($"File not found: {filePath}");
            return 1;
        }

        var content = File.ReadAllText(filePath).Replace("\r\n", "\n").Replace("\r", "\n");
        var lines = content.Split('\n').ToList();
        var fixmeIndices = new List<int>();
        for (int i = 0; i < lines.Count; i++)
        {
            if (lines[i].TrimStart().StartsWith("FIXME:", StringComparison.OrdinalIgnoreCase))
                fixmeIndices.Add(i);
        }

        if (fixmeIndices.Count == 0)
        {
            Console.WriteLine("No FIXME: lines found.");
            return 0;
        }

        var title = lines.Count > 0 ? lines[0].Trim() : "";
        var bodyEnd = lines.IndexOf(lines.FirstOrDefault(l => l.Trim() == "choices:") ?? "");
        var bodySnippet = bodyEnd > 1
            ? string.Join("\n", lines.Skip(1).Take(Math.Min(15, bodyEnd - 1)))
            : "";

        if (promptsOnly)
        {
            for (int i = 0; i < fixmeIndices.Count; i++)
            {
                var lineIndex = fixmeIndices[i];
                var summary = ExtractSummary(lines[lineIndex]);
                var choiceLine = FindChoiceLine(lines, lineIndex);
                var precedingProse = FindPrecedingProse(lines, lineIndex);
                var mechanics = FindMechanics(lines, lineIndex);
                var prompt = BuildPrompt(title, bodySnippet, choiceLine, precedingProse, summary, mechanics);
                if (i > 0) Console.WriteLine();
                Console.WriteLine("---");
                Console.WriteLine($"FIXME L{lineIndex + 1}: {Truncate(summary, 60)}");
                Console.WriteLine("---");
                Console.WriteLine(prompt);
            }
            return 0;
        }

        var client = LlmClient.TryCreate(configPath);
        if (client == null)
            return 1;

        var rewritePrompt = FindRewritePrompt(filePath);
        if (rewritePrompt == null)
        {
            Console.Error.WriteLine("Could not find generation/rewrite_prompt.md in any parent directory.");
            return 1;
        }
        var systemPrompt = File.ReadAllText(rewritePrompt);

        var replacements = 0;
        for (int idx = fixmeIndices.Count - 1; idx >= 0; idx--)
        {
            var lineIndex = fixmeIndices[idx];
            var line = lines[lineIndex];
            var trim = line.TrimStart();
            var summary = ExtractSummary(line);
            var leading = line[..(line.Length - trim.Length)];
            var choiceLine = FindChoiceLine(lines, lineIndex);
            var precedingProse = FindPrecedingProse(lines, lineIndex);
            var mechanics = FindMechanics(lines, lineIndex);
            var prompt = BuildPrompt(title, bodySnippet, choiceLine, precedingProse, summary, mechanics);

            Console.WriteLine($"FIXME (L{lineIndex + 1}): {Truncate(summary, 60)}");
            string? responseText;
            try
            {
                responseText = await client.CompleteAsync(prompt, systemPrompt);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"  Error: {ex.Message}");
                continue;
            }

            var prose = ExtractProse(responseText ?? "");
            if (string.IsNullOrEmpty(prose))
            {
                Console.Error.WriteLine("  No usable prose in response; skipping.");
                continue;
            }

            var proseLines = prose.Split('\n');
            // First line gets REVIEW: prefix, subsequent lines get same indent
            var replacement = new List<string>();
            replacement.Add(leading + "REVIEW: " + proseLines[0].Trim());
            for (int i = 1; i < proseLines.Length; i++)
            {
                var pl = proseLines[i].Trim();
                replacement.Add(pl.Length > 0 ? leading + pl : "");
            }
            lines.RemoveAt(lineIndex);
            lines.InsertRange(lineIndex, replacement);
            replacements++;
        }

        if (replacements > 0)
        {
            var dir = Path.GetDirectoryName(filePath)!;
            var backup = Path.Combine(dir, "_" + Path.GetFileName(filePath));
            File.Copy(filePath, backup, overwrite: true);
            Console.WriteLine($"Backup: {backup}");
            File.WriteAllText(filePath, string.Join("\n", lines));
        }
        Console.WriteLine(replacements > 0 ? $"Replaced {replacements} FIXME(s)." : "No replacements made.");
        return 0;
    }

    static string ExtractSummary(string line)
    {
        var trim = line.TrimStart();
        const string prefix = "FIXME:";
        var idx = trim.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
        return idx >= 0 ? trim[(idx + prefix.Length)..].Trim() : trim;
    }

    static string Truncate(string s, int max) =>
        s.Length > max ? s[..max] + "..." : s;

    static string? ExtractProse(string output)
    {
        if (output.Contains("invalid_request_error", StringComparison.Ordinal) ||
            output.Contains("\"error\":", StringComparison.Ordinal))
            return null;

        var text = output.Trim();

        // Strip code fences
        if (text.StartsWith("```"))
        {
            var firstNl = text.IndexOf('\n');
            if (firstNl > 0) text = text[(firstNl + 1)..];
        }
        if (text.EndsWith("```"))
            text = text[..^3];

        // Strip a leading REVIEW: prefix if present
        text = text.Trim();
        if (text.StartsWith("REVIEW:", StringComparison.OrdinalIgnoreCase))
            text = text[7..];

        // Strip LLM preamble lines
        var lines = text.Split('\n').ToList();
        while (lines.Count > 0)
        {
            var t = lines[0].Trim();
            if (t.Length == 0 || t.StartsWith("Here", StringComparison.Ordinal)
                || t.StartsWith("Sure", StringComparison.Ordinal)
                || t.StartsWith("I'll", StringComparison.Ordinal)
                || t.StartsWith("I'd", StringComparison.Ordinal))
                lines.RemoveAt(0);
            else
                break;
        }

        text = string.Join('\n', lines).Trim();
        return text.Length > 0 ? text : null;
    }

    static string FindChoiceLine(List<string> lines, int fromIndex)
    {
        for (int i = fromIndex - 1; i >= 0; i--)
        {
            var t = lines[i].TrimStart();
            if (t.StartsWith("* "))
                return t;
        }
        return "";
    }

    static string FindPrecedingProse(List<string> lines, int fromIndex)
    {
        var proseLines = new List<string>();
        for (int i = fromIndex - 1; i >= 0; i--)
        {
            var line = lines[i];
            var t = line.Trim();
            var spaces = line.Length - line.TrimStart().Length;
            if (spaces == 2 && t.Length > 0 && !t.StartsWith('['))
                break;
            if (t.StartsWith('['))
                continue;
            if (t.Length == 0)
                continue;
            proseLines.Add(t);
        }
        proseLines.Reverse();
        return string.Join(" ", proseLines);
    }

    static List<string> FindMechanics(List<string> lines, int fromIndex)
    {
        var mechanics = new List<string>();
        for (int i = fromIndex + 1; i < lines.Count; i++)
        {
            var line = lines[i];
            var t = line.Trim();
            var indent = line.Length - line.TrimStart().Length;
            if (indent <= 2 && t.Length > 0) break;
            if (t == "[else]") break;
            if (t.StartsWith("[if ", StringComparison.Ordinal)) break;
            if (t.StartsWith('[') && t.EndsWith(']') && t.Length > 2 && !t.StartsWith("[else]") && !t.StartsWith("[if "))
                mechanics.Add(t[1..^1].Trim());
        }
        return mechanics;
    }

    static string BuildPrompt(string title, string bodySnippet, string choiceLine, string precedingProse, string fixmeSummary, List<string> mechanics)
    {
        var mechanicsText = mechanics.Count > 0 ? string.Join(", ", mechanics) : "none";
        var precedingSection = string.IsNullOrEmpty(precedingProse)
            ? ""
            : $"\nPreceding prose (already written, continue from here): {precedingProse}\n";
        return $@"Encounter title: {title}

Setting (excerpt):
{bodySnippet}

Player choice: {choiceLine}
{precedingSection}
Outcome summary to expand: {fixmeSummary}

Mechanics that follow (reference or imply these in the prose): {mechanicsText}

Expand every beat into prose. Output only your prose, nothing else.";
    }

    static string? FindRewritePrompt(string encFilePath)
    {
        var dir = Path.GetDirectoryName(Path.GetFullPath(encFilePath));
        while (dir != null)
        {
            var candidate = Path.Combine(dir, "generation", "rewrite_prompt.md");
            if (File.Exists(candidate)) return candidate;
            dir = Path.GetDirectoryName(dir);
        }
        return null;
    }
}
