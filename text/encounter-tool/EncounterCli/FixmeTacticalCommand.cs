namespace EncounterCli;

static class FixmeTacticalCommand
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
            Console.Error.WriteLine("fixme-tactical requires <file.tac>.");
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

        // Find the openings section and its FIXME lines
        var openingsStart = lines.FindIndex(l => l.Trim() == "openings:");
        if (openingsStart < 0)
        {
            Console.WriteLine("No openings: section found.");
            return 0;
        }

        var fixmes = new List<(int LineIndex, string Archetype)>();
        for (int i = openingsStart + 1; i < lines.Count; i++)
        {
            var trim = lines[i].Trim();
            if (trim.Length > 0 && !trim.StartsWith("*")) break; // next section
            if (trim.StartsWith("* FIXME:", StringComparison.OrdinalIgnoreCase))
            {
                var archetype = trim["* FIXME:".Length..].Trim();
                fixmes.Add((i, archetype));
            }
        }

        if (fixmes.Count == 0)
        {
            Console.WriteLine("No FIXME: lines in openings.");
            return 0;
        }

        // Gather encounter context
        var title = lines.Count > 0 ? lines[0].Trim() : "";
        var stat = ExtractStat(lines);
        var body = ExtractBody(lines);
        var localeGuide = FindLocaleGuide(filePath);
        var existingLabels = ExtractExistingLabels(lines, openingsStart);

        var prompt = BuildPrompt(title, stat, body, localeGuide, existingLabels, fixmes);

        if (promptsOnly)
        {
            Console.WriteLine("---");
            Console.WriteLine($"{fixmes.Count} opening FIXME(s) to fill");
            Console.WriteLine("---");
            Console.WriteLine(prompt);
            return 0;
        }

        var client = LlmClient.TryCreate(configPath);
        if (client == null)
            return 1;

        Console.WriteLine($"Generating {fixmes.Count} opening labels...");
        string? response;
        try
        {
            response = await client.CompleteAsync(prompt);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }

        var labels = ParseLabels(response ?? "");
        if (labels.Count != fixmes.Count)
        {
            Console.Error.WriteLine($"Expected {fixmes.Count} labels, got {labels.Count}. Raw response:");
            Console.Error.WriteLine(response);
            return 1;
        }

        // Replace FIXME lines (reverse order to preserve indices)
        var backup = Path.Combine(Path.GetDirectoryName(filePath)!, "_" + Path.GetFileName(filePath));
        File.Copy(filePath, backup, overwrite: true);
        Console.WriteLine($"Backup: {backup}");

        for (int i = fixmes.Count - 1; i >= 0; i--)
        {
            var (lineIndex, archetype) = fixmes[i];
            lines[lineIndex] = $"  * {labels[i]}: {archetype}";
        }

        File.WriteAllText(filePath, string.Join("\n", lines));
        Console.WriteLine($"Replaced {fixmes.Count} opening(s). Review the REVIEW: labels.");
        return 0;
    }

    static string? ExtractStat(List<string> lines)
    {
        foreach (var line in lines.Take(10))
        {
            var trim = line.Trim();
            if (trim.StartsWith("[stat ", StringComparison.OrdinalIgnoreCase) && trim.EndsWith(']'))
                return trim[6..^1].Trim();
        }
        return null;
    }

    static string ExtractBody(List<string> lines)
    {
        // Body is between the header block and the first section (timers:/openings:/etc.)
        var bodyStart = -1;
        var bodyEnd = -1;
        for (int i = 0; i < lines.Count; i++)
        {
            var trim = lines[i].Trim();
            if (bodyStart < 0)
            {
                // Skip header lines (title, [stat], [tier], blank)
                if (trim.Length == 0 || trim.StartsWith('[')) continue;
                if (i == 0) continue; // title
                bodyStart = i;
            }
            if (bodyStart >= 0 && (trim == "clock:" || trim == "challenges:" || trim == "timers:" || trim == "openings:" || trim == "stats:" || trim == "approaches:"))
            {
                bodyEnd = i;
                break;
            }
        }
        if (bodyStart < 0) return "";
        if (bodyEnd < 0) bodyEnd = lines.Count;
        return string.Join("\n", lines.Skip(bodyStart).Take(bodyEnd - bodyStart)).Trim();
    }

    static string? FindLocaleGuide(string tacFilePath)
    {
        var dir = Path.GetDirectoryName(Path.GetFullPath(tacFilePath));
        while (dir != null)
        {
            var candidate = Path.Combine(dir, "locale_guide.txt");
            if (File.Exists(candidate)) return File.ReadAllText(candidate);
            dir = Path.GetDirectoryName(dir);
        }
        return null;
    }

    static List<string> ExtractExistingLabels(List<string> lines, int openingsStart)
    {
        var labels = new List<string>();
        for (int i = openingsStart + 1; i < lines.Count; i++)
        {
            var trim = lines[i].Trim();
            if (trim.Length > 0 && !trim.StartsWith("*")) break;
            if (trim.StartsWith("* ") && !trim.StartsWith("* FIXME:", StringComparison.OrdinalIgnoreCase))
            {
                var colonIdx = trim.IndexOf(':');
                if (colonIdx > 2)
                    labels.Add(trim[2..colonIdx].Trim());
            }
        }
        return labels;
    }

    static string BuildPrompt(string title, string? stat, string body, string? localeGuide,
        List<string> existingLabels, List<(int LineIndex, string Archetype)> fixmes)
    {
        var archetypeList = string.Join("\n", fixmes.Select((f, i) => $"  {i + 1}. {f.Archetype}"));

        var localeSection = !string.IsNullOrEmpty(localeGuide)
            ? $"\nLocale context:\n{localeGuide}\n"
            : "";

        var existingSection = existingLabels.Count > 0
            ? $"\nExisting labels in this encounter (do not repeat these):\n{string.Join("\n", existingLabels.Select(l => $"  - {l}"))}\n"
            : "";

        var statGuidance = (stat?.ToLowerInvariant()) switch
        {
            "combat" => "This is a Combat encounter. Every action label should involve fighting, striking, dodging, blocking, or physical violence. The player is in a fight.",
            "bushcraft" => "This is a Bushcraft encounter. Actions should involve traversal, navigation, endurance, climbing, foraging, or surviving the environment. The player is overcoming terrain or weather.",
            "cunning" => "This is a Cunning encounter. Actions should involve trickery, stealth, observation, quick thinking, or exploiting the situation. The player is outsmarting a problem.",
            "negotiation" => "This is a Negotiation encounter. Actions should involve persuasion, reading people, leveraging information, standing firm, or finding common ground. The player is talking their way through.",
            _ => ""
        };

        var statSection = statGuidance.Length > 0 ? $"\n{statGuidance}\n" : "";

        return $@"You are naming action cards for a tactical encounter in a low-fantasy RPG.

Each card is a short action label — something the player clicks to take that action.
Labels should be imperative or descriptive, 3-8 words, like:
  ""Shove one aside"", ""Duck a bandit's wild blow"", ""Cut through the brush"",
  ""Keep a steady pace"", ""Kick road dust into them"", ""Vault over the fence""

Rules:
- Each label must fit the encounter's theme and setting
- Labels describe actions the player IS ABOUT TO TAKE, not outcomes
- No two labels should be too similar — vary the verbs and imagery
- Match the intensity to the archetype (free_momentum_small = small/easy actions,
  momentum_to_progress_huge = dramatic/decisive actions, threat_to_progress = risky plays,
  spirits_to_momentum = exhausting/painful efforts)
{statSection}
Encounter: {title}

Setting:
{body}
{localeSection}{existingSection}
Generate one label per line for each archetype below. Output ONLY the labels, one per line,
numbered to match. No archetype names, no explanations.

{archetypeList}";
    }

    static List<string> ParseLabels(string response)
    {
        var labels = new List<string>();
        foreach (var line in response.Split('\n'))
        {
            var trim = line.Trim();
            if (trim.Length == 0) continue;

            // Strip leading number + punctuation: "1. ", "1) ", "1: ", etc.
            var cleaned = trim;
            var dotIdx = trim.IndexOfAny(['.', ')', ':']);
            if (dotIdx > 0 && dotIdx <= 3 && int.TryParse(trim[..dotIdx], out _))
                cleaned = trim[(dotIdx + 1)..].Trim();

            // Strip surrounding quotes
            if (cleaned.Length >= 2 && cleaned[0] == '"' && cleaned[^1] == '"')
                cleaned = cleaned[1..^1];

            // Strip trailing archetype if the LLM included it (after a colon or dash)
            var trailingColon = cleaned.LastIndexOf(':');
            if (trailingColon > 0)
            {
                var after = cleaned[(trailingColon + 1)..].Trim().ToLowerInvariant();
                if (after.Contains("momentum") || after.Contains("progress") || after.Contains("spirits")
                    || after.Contains("threat") || after.Contains("cancel") || after.Contains("free"))
                    cleaned = cleaned[..trailingColon].Trim();
            }

            if (cleaned.Length > 0)
                labels.Add(cleaned);
        }
        return labels;
    }
}
