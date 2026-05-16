using System.Text.RegularExpressions;

namespace EncounterCli;

static class FixmeCommand
{
    // System prompt for the Haiku POV-rewrite stage (post-LoRA).
    // Input: author-voice prose in 1st/3rd person. Output: 2nd-person CRPG present tense.
    const string PoVRewriteSystemPrompt =
        "You are converting a passage of pulp-fantasy prose into second-person present tense for a text CRPG.\n\n" +
        "Rules:\n" +
        "- The player character (Conan, Solomon Kane, El Borak, the narrator, etc.) becomes \"you\" in present tense.\n" +
        "- Named supporting NPCs stay as named NPCs.\n" +
        "- Author-specific cosmic entities (Cthulhu, the Old Ones, etc.) become generic descriptors.\n" +
        "- Keep all concrete facts, sensory details, and events intact.\n" +
        "- Preserve the atmospheric register and phrasing; do not flatten the prose.\n" +
        "- Do not add new plot elements.\n" +
        "- No em-dashes; use commas, semicolons, or separate sentences.\n" +
        "Output only the rewritten prose, nothing else.";

    // Matches: [leading]FIXME[(register[,length])]: text
    // Groups: ann (optional annotation), text (summary)
    static readonly Regex FixmePattern = new(
        @"FIXME(?:\((?<ann>[^)]*)\))?:\s*(?<text>.*)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    record FixmeInfo(string Summary, string? Register, string Length);

    public static async Task<int> RunAsync(string[] args)
    {
        string? filePath = null;
        string? configPath = null;
        string? expandUrl = null;
        string? programsDir = null;
        var author = "HPL";
        var defaultScene = "dread";
        var promptsOnly = false;
        var candidates = 1;
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--config" && i + 1 < args.Length) { configPath = args[i + 1]; i++; }
            else if (args[i] == "--expand-url" && i + 1 < args.Length) { expandUrl = args[i + 1]; i++; }
            else if (args[i] == "--programs" && i + 1 < args.Length) { programsDir = args[i + 1]; i++; }
            else if (args[i] == "--author" && i + 1 < args.Length) { author = args[i + 1]; i++; }
            else if (args[i] == "--scene" && i + 1 < args.Length) { defaultScene = args[i + 1]; i++; }
            else if (args[i] == "--candidates" && i + 1 < args.Length) { candidates = int.Parse(args[i + 1]); i++; }
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
            if (ContainsFixme(lines[i]))
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

        var expandClient = expandUrl != null
            ? new ExpandClient(expandUrl, programsDir)
            : null;

        if (promptsOnly)
        {
            for (int i = 0; i < fixmeIndices.Count; i++)
            {
                var lineIndex = fixmeIndices[i];
                var fixme = ExtractFixme(lines[lineIndex]);
                var choiceLine = FindChoiceLine(lines, lineIndex);
                var precedingProse = FindPrecedingProse(lines, lineIndex);
                var mechanics = FindMechanics(lines, lineIndex);
                if (i > 0) Console.WriteLine();
                Console.WriteLine("---");
                Console.WriteLine($"FIXME L{lineIndex + 1}: {Truncate(fixme.Summary, 60)}");
                Console.WriteLine("---");
                if (expandClient != null)
                {
                    var sceneType = ResolveScene(fixme.Register, defaultScene);
                    Console.WriteLine("[Stage 1 — expand prompt]");
                    foreach (var m in expandClient.BuildMessages(author, sceneType, fixme.Summary))
                        Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(m));
                    Console.WriteLine("[Stage 2 — Haiku POV rewrite system prompt]");
                    Console.WriteLine(PoVRewriteSystemPrompt);
                }
                else
                {
                    Console.WriteLine(BuildPrompt(title, bodySnippet, choiceLine, precedingProse, fixme.Summary, mechanics));
                }
            }
            return 0;
        }

        var client = LlmClient.TryCreate(configPath);
        if (client == null)
            return 1;

        string? systemPrompt = null;
        if (expandClient == null)
        {
            var rewritePrompt = FindRewritePrompt(filePath);
            if (rewritePrompt == null)
            {
                Console.Error.WriteLine("Could not find generation/rewrite_prompt.md in any parent directory.");
                return 1;
            }
            systemPrompt = File.ReadAllText(rewritePrompt);
        }

        var replacements = 0;
        for (int idx = fixmeIndices.Count - 1; idx >= 0; idx--)
        {
            var lineIndex = fixmeIndices[idx];
            var line = lines[lineIndex];
            var fixme = ExtractFixme(line);
            var leading = line[..FixmePattern.Match(line).Index];
            var choiceLine = FindChoiceLine(lines, lineIndex);
            var precedingProse = FindPrecedingProse(lines, lineIndex);
            var mechanics = FindMechanics(lines, lineIndex);

            var sceneType = ResolveScene(fixme.Register, defaultScene);
            Console.WriteLine($"FIXME (L{lineIndex + 1}) [{sceneType}]: {Truncate(fixme.Summary, 60)}");

            var allProse = new List<string>();
            try
            {
                if (expandClient != null)
                {
                    for (int c = 0; c < candidates; c++)
                    {
                        var expandedProse = await expandClient.ExpandAsync(author, sceneType, fixme.Summary);
                        LogChunk($"Expand output {c + 1}/{candidates}", expandedProse ?? "(empty)");
                        if (string.IsNullOrEmpty(expandedProse)) continue;
                        var rewritten = await client.CompleteAsync(expandedProse, PoVRewriteSystemPrompt);
                        LogChunk($"Haiku output {c + 1}/{candidates}", rewritten ?? "(empty)");
                        var prose = ExtractProse(rewritten ?? "");
                        if (!string.IsNullOrEmpty(prose)) allProse.Add(prose);
                    }
                }
                else
                {
                    var prompt = BuildPrompt(title, bodySnippet, choiceLine, precedingProse, fixme.Summary, mechanics);
                    LogChunk("Haiku prompt", prompt);
                    var responseText = await client.CompleteAsync(prompt, systemPrompt);
                    LogChunk("Haiku output", responseText ?? "(empty)");
                    var prose = ExtractProse(responseText ?? "");
                    if (!string.IsNullOrEmpty(prose)) allProse.Add(prose);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"  Error: {ex.Message}");
                continue;
            }

            if (allProse.Count == 0)
            {
                Console.Error.WriteLine("  No usable prose in response; skipping.");
                continue;
            }

            var replacement = new List<string>();
            for (int c = 0; c < allProse.Count; c++)
            {
                if (c > 0) replacement.Add("");
                var label = allProse.Count == 1 ? "REVIEW" : $"CANDIDATE {c + 1}";
                var proseLines = allProse[c].Split('\n');
                replacement.Add(leading + label + ": " + proseLines[0].Trim());
                for (int i = 1; i < proseLines.Length; i++)
                {
                    var pl = proseLines[i].Trim();
                    replacement.Add(pl.Length > 0 ? leading + pl : "");
                }
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

    static readonly HashSet<string> ValidScenes =
        ["mundane", "action", "horror", "dread", "wonder", "revelation"];

    static string ResolveScene(string? register, string fallback) =>
        register != null && ValidScenes.Contains(register) ? register : fallback;

    static bool ContainsFixme(string line)
    {
        var trim = line.TrimStart();
        if (trim.StartsWith("* ", StringComparison.Ordinal)) trim = trim[2..].TrimStart();
        return FixmePattern.IsMatch(trim);
    }

    static FixmeInfo ExtractFixme(string line)
    {
        var match = FixmePattern.Match(line);
        if (!match.Success) return new(line.Trim(), null, "medium");

        var summary = match.Groups["text"].Value.Trim();
        var ann = match.Groups["ann"].Value;

        string? register = null;
        string? length = null;

        if (!string.IsNullOrEmpty(ann))
        {
            var parts = ann.Split(',', 2);
            var r = parts[0].Trim();
            if (r.Length > 0) register = r;
            if (parts.Length > 1)
            {
                var l = parts[1].Trim();
                if (l.Length > 0) length = l;
            }
        }

        return new(summary, register, length ?? ComputeLength(summary));
    }

    // Dumb rubric: short beat description → short expansion, etc.
    static string ComputeLength(string text)
    {
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        return words < 25 ? "short" : words < 50 ? "medium" : "long";
    }

    static string Truncate(string s, int max) =>
        s.Length > max ? s[..max] + "..." : s;

    static void LogChunk(string label, string content)
    {
        Console.Error.WriteLine($"\n── {label} ──────────────────────────");
        Console.Error.WriteLine(content);
        Console.Error.WriteLine("─────────────────────────────────────");
    }

    static string? ExtractProse(string output)
    {
        if (output.Contains("invalid_request_error", StringComparison.Ordinal) ||
            output.Contains("\"error\":", StringComparison.Ordinal))
            return null;

        var text = output.Trim();

        if (text.StartsWith("```"))
        {
            var firstNl = text.IndexOf('\n');
            if (firstNl > 0) text = text[(firstNl + 1)..];
        }
        if (text.EndsWith("```"))
            text = text[..^3];

        text = text.Trim();
        if (text.StartsWith("REVIEW:", StringComparison.OrdinalIgnoreCase))
            text = text[7..];

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

    static string BuildLoraPrompt(string author, string? register, string length, string context, string beat)
    {
        var reg = register != null ? $"[register: {register}]\n" : "";
        return $"[author: {author}]\n{reg}[length: {length}]\n[context]\n{context}\n[/context]\n[beat: {beat}]\n[/beat]\n\n";
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
