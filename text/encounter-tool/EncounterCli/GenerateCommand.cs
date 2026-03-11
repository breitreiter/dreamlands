using System.Text;
using System.Text.RegularExpressions;

namespace EncounterCli;

static partial class GenerateCommand
{
    const string GenerationDir = "/home/joseph/repos/dreamlands/text/encounters/generation/v2";
    const string LocaleGuideFilename = "locale_guide.txt";
    const string SystemPrompt = "You are a narrative designer for a computer RPG. Follow instructions precisely. Output only the requested content.";

    record Archetype(string Name, string Description, List<ParticipantCombo> Combos);
    record Character(string Name, string Description);

    enum ParticipantCombo
    {
        NeutralVsNeutral,
        NeutralVsVillain,
        VillainVsVillain,
        PcVsNeutral,
        PcVsVillain,
        PcVsEnvironment,
    }

    record Blueprint(string Kind, Archetype Archetype, ParticipantCombo Combo, List<(string Role, string Faction, Character Character)> Cast);

    public static async Task<int> RunAsync(string[] args)
    {
        string? outPath = null;
        string? configPath = null;
        var promptsOnly = false;

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--out" && i + 1 < args.Length) { outPath = args[i + 1]; i++; }
            else if (args[i] == "--config" && i + 1 < args.Length) { configPath = args[i + 1]; i++; }
            else if (args[i] == "--prompts-only") promptsOnly = true;
        }

        // Locale guide: always in cwd
        var localePath = Path.GetFullPath(LocaleGuideFilename);
        if (!File.Exists(localePath))
        {
            Console.Error.WriteLine($"Locale guide not found: {localePath}");
            Console.Error.WriteLine("Run this command from a directory containing locale_guide.txt.");
            return 1;
        }

        if (!Directory.Exists(GenerationDir))
        {
            Console.Error.WriteLine($"Generation directory not found: {GenerationDir}");
            return 1;
        }

        // Load archetype pools
        var conflictPath = Path.Combine(GenerationDir, "conflict_archetypes.md");
        var troublePath = Path.Combine(GenerationDir, "trouble_archetypes.md");
        var neutralPath = Path.Combine(GenerationDir, "neutral_archetypes.md");
        var villainPath = Path.Combine(GenerationDir, "villain_archetypes.md");
        var promptPath = Path.Combine(GenerationDir, "encounter_prompt.md");

        foreach (var (name, path) in new[]
        {
            ("conflict_archetypes.md", conflictPath),
            ("trouble_archetypes.md", troublePath),
            ("neutral_archetypes.md", neutralPath),
            ("villain_archetypes.md", villainPath),
            ("encounter_prompt.md", promptPath),
        })
        {
            if (!File.Exists(path))
            {
                Console.Error.WriteLine($"Required file not found: {name} (expected at {path})");
                return 1;
            }
        }

        var conflicts = ParseBoldArchetypes(File.ReadAllText(conflictPath));
        var troubles = ParseBoldArchetypes(File.ReadAllText(troublePath));
        var neutrals = ParseCharacters(File.ReadAllText(neutralPath), "## ");
        var villains = ParseCharacters(File.ReadAllText(villainPath), "# ");

        if (conflicts.Count == 0 || troubles.Count == 0 || neutrals.Count == 0 || villains.Count == 0)
        {
            Console.Error.WriteLine("One or more archetype files parsed as empty.");
            return 1;
        }

        var random = new Random();
        var localeGuide = File.ReadAllText(localePath);
        var promptTemplate = File.ReadAllText(promptPath);

        // Blueprint selection loop
        Blueprint blueprint;
        while (true)
        {
            blueprint = RollBlueprint(random, conflicts, troubles, neutrals, villains);
            PrintBlueprint(blueprint);
            Console.Write("\nProceed? [Y/n/r] ");

            var key = Console.ReadLine()?.Trim().ToLowerInvariant() ?? "";
            if (key is "" or "y")
                break;
            if (key == "n")
                return 0;

            Console.WriteLine();
        }
        Console.WriteLine();

        // Assemble prompt
        var archetypeText = $"{blueprint.Archetype.Name} — {blueprint.Archetype.Description}";
        var castText = FormatCast(blueprint);
        var prompt = promptTemplate
            .Replace("{{LOCALE_GUIDE}}", localeGuide)
            .Replace("{{ARCHETYPE}}", archetypeText)
            .Replace("{{CAST}}", castText);

        if (promptsOnly)
        {
            Console.WriteLine("--- Assembled Prompt ---");
            Console.WriteLine(prompt);
            return 0;
        }

        var client = LlmClient.TryCreate(configPath);
        if (client == null)
            return 1;

        // Generation + review loop
        while (true)
        {
            Console.WriteLine("Generating encounter...");
            Console.WriteLine();

            string? encounterText;
            try
            {
                encounterText = await client.CompleteAsync(prompt, SystemPrompt);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error generating encounter: {ex.Message}");
                return 1;
            }

            if (string.IsNullOrWhiteSpace(encounterText))
            {
                Console.Error.WriteLine("No response from LLM.");
                return 1;
            }

            encounterText = StripCodeFences(encounterText);
            encounterText = EncounterPostProcessor.Process(encounterText);

            Console.WriteLine(encounterText);
            Console.Write("\nAccept? [y/n/r] ");

            var key = Console.ReadLine()?.Trim().ToLowerInvariant() ?? "";
            if (key == "y")
            {
                if (!string.IsNullOrEmpty(outPath))
                {
                    outPath = Path.GetFullPath(outPath);
                    var outDir = Path.GetDirectoryName(outPath);
                    if (!string.IsNullOrEmpty(outDir))
                        Directory.CreateDirectory(outDir);
                    File.WriteAllText(outPath, encounterText);

                    var finalPath = TryRenameToTitle(outPath, encounterText);
                    Console.WriteLine($"Wrote output to {finalPath}");
                }
                return 0;
            }
            if (key == "n")
                return 0;

            // 'r' or anything else — re-generate with same blueprint
            Console.WriteLine();
        }
    }

    static Blueprint RollBlueprint(
        Random random,
        List<Archetype> conflicts,
        List<Archetype> troubles,
        List<Character> neutrals,
        List<Character> villains)
    {
        var isConflict = random.Next(2) == 0;
        var pool = isConflict ? conflicts : troubles;
        var archetype = pool[random.Next(pool.Count)];
        var combo = archetype.Combos[random.Next(archetype.Combos.Count)];
        var cast = CastCharacters(random, combo, neutrals, villains);
        return new Blueprint(isConflict ? "Conflict" : "Trouble", archetype, combo, cast);
    }

    static List<(string Role, string Faction, Character Character)> CastCharacters(
        Random random,
        ParticipantCombo combo,
        List<Character> neutrals,
        List<Character> villains)
    {
        var cast = new List<(string, string, Character)>();

        switch (combo)
        {
            case ParticipantCombo.NeutralVsNeutral:
            {
                var (a, b) = PickTwo(random, neutrals);
                cast.Add(("Faction A", "neutral", a));
                cast.Add(("Faction B", "neutral", b));
                break;
            }
            case ParticipantCombo.NeutralVsVillain:
            {
                cast.Add(("Faction A", "neutral", Pick(random, neutrals)));
                cast.Add(("Faction B", "villain", Pick(random, villains)));
                break;
            }
            case ParticipantCombo.VillainVsVillain:
            {
                var (a, b) = PickTwo(random, villains);
                cast.Add(("Faction A", "villain", a));
                cast.Add(("Faction B", "villain", b));
                break;
            }
            case ParticipantCombo.PcVsNeutral:
                cast.Add(("Antagonist", "neutral", Pick(random, neutrals)));
                break;
            case ParticipantCombo.PcVsVillain:
                cast.Add(("Antagonist", "villain", Pick(random, villains)));
                break;
            case ParticipantCombo.PcVsEnvironment:
                // No cast
                break;
        }

        return cast;
    }

    static T Pick<T>(Random random, List<T> pool) => pool[random.Next(pool.Count)];

    static (T, T) PickTwo<T>(Random random, List<T> pool)
    {
        var a = random.Next(pool.Count);
        var b = random.Next(pool.Count - 1);
        if (b >= a) b++;
        return (pool[a], pool[b]);
    }

    static void PrintBlueprint(Blueprint bp)
    {
        Console.WriteLine($"Type:      {bp.Kind}");
        Console.WriteLine($"Archetype: {bp.Archetype.Name}");
        Console.WriteLine($"Combo:     {FormatCombo(bp.Combo)}");
        if (bp.Cast.Count > 0)
        {
            Console.WriteLine("Cast:");
            foreach (var (role, faction, character) in bp.Cast)
                Console.WriteLine($"  {role} ({faction}): {character.Name}");
        }
    }

    static string FormatCombo(ParticipantCombo combo) => combo switch
    {
        ParticipantCombo.NeutralVsNeutral => "neutral vs neutral",
        ParticipantCombo.NeutralVsVillain => "neutral vs villain",
        ParticipantCombo.VillainVsVillain => "villain vs villain",
        ParticipantCombo.PcVsNeutral => "pc vs neutral",
        ParticipantCombo.PcVsVillain => "pc vs villain",
        ParticipantCombo.PcVsEnvironment => "pc vs environment",
        _ => combo.ToString(),
    };

    static string FormatCast(Blueprint bp)
    {
        if (bp.Cast.Count == 0)
            return "(No cast — PC vs environment)";

        var sb = new StringBuilder();
        foreach (var (role, faction, character) in bp.Cast)
        {
            if (sb.Length > 0) sb.AppendLine();
            sb.Append($"- {role} ({faction}): {character.Name} — {character.Description}");
        }
        return sb.ToString();
    }

    // --- Archetype parsing ---

    [GeneratedRegex(@"^\*\*(.+?)\*\*")]
    private static partial Regex BoldNamePattern();

    static List<Archetype> ParseBoldArchetypes(string text)
    {
        var archetypes = new List<Archetype>();
        var lines = text.Split('\n');

        for (int i = 0; i < lines.Length; i++)
        {
            var match = BoldNamePattern().Match(lines[i]);
            if (!match.Success) continue;

            var name = match.Groups[1].Value;
            // Description is everything after the bold marker on the same line
            var descStart = lines[i][(match.Index + match.Length)..].TrimStart(' ', '—', '-', ' ');

            // Collect remaining lines until next bold entry or EOF
            var descLines = new List<string>();
            if (!string.IsNullOrWhiteSpace(descStart))
                descLines.Add(descStart);

            var combos = new List<ParticipantCombo>();
            for (int j = i + 1; j < lines.Length; j++)
            {
                if (BoldNamePattern().IsMatch(lines[j]))
                    break;

                var trimmed = lines[j].Trim();

                // Parse participant combo lines
                var parsed = TryParseCombo(trimmed);
                if (parsed != null)
                {
                    combos.Add(parsed.Value);
                    continue;
                }

                // Skip the "Possible conflict/trouble participants:" header
                if (trimmed.StartsWith("Possible ") && trimmed.EndsWith("participants:"))
                    continue;

                if (!string.IsNullOrWhiteSpace(trimmed) && !trimmed.StartsWith("- "))
                    descLines.Add(trimmed);
            }

            var description = string.Join(" ", descLines).Trim();
            archetypes.Add(new Archetype(name, description, combos));
        }

        return archetypes;
    }

    static ParticipantCombo? TryParseCombo(string line)
    {
        if (!line.StartsWith("- ")) return null;
        var combo = line[2..].Trim().ToLowerInvariant();
        return combo switch
        {
            "neutral vs neutral" => ParticipantCombo.NeutralVsNeutral,
            "neutral vs villain" => ParticipantCombo.NeutralVsVillain,
            "villain vs villain" => ParticipantCombo.VillainVsVillain,
            "pc vs neutral" => ParticipantCombo.PcVsNeutral,
            "pc vs villain" => ParticipantCombo.PcVsVillain,
            "pc vs environment" => ParticipantCombo.PcVsEnvironment,
            _ => null,
        };
    }

    static List<Character> ParseCharacters(string text, string headingPrefix)
    {
        var characters = new List<Character>();
        var lines = text.Split('\n');

        for (int i = 0; i < lines.Length; i++)
        {
            if (!lines[i].StartsWith(headingPrefix)) continue;

            var name = lines[i][headingPrefix.Length..].Trim();
            if (string.IsNullOrWhiteSpace(name)) continue;

            // Description is the next non-blank line
            for (int j = i + 1; j < lines.Length; j++)
            {
                var trimmed = lines[j].Trim();
                if (string.IsNullOrWhiteSpace(trimmed)) continue;
                characters.Add(new Character(name, trimmed));
                break;
            }
        }

        return characters;
    }

    // --- Output helpers (shared with old version) ---

    static string TryRenameToTitle(string outPath, string text)
    {
        var title = EncounterPostProcessor.ExtractTitle(text);
        if (string.IsNullOrWhiteSpace(title) || title.Length > 60)
            return FallbackRename(outPath);

        var sanitized = SanitizeFilename(title);
        if (string.IsNullOrWhiteSpace(sanitized))
            return FallbackRename(outPath);

        var dir = Path.GetDirectoryName(outPath) ?? ".";
        var newPath = Path.Combine(dir, sanitized + ".enc");

        if (string.Equals(outPath, newPath, StringComparison.Ordinal))
            return outPath;

        if (File.Exists(newPath))
        {
            Console.Error.WriteLine($"Cannot rename: {Path.GetFileName(newPath)} already exists.");
            return outPath;
        }

        File.Move(outPath, newPath);
        return newPath;
    }

    static string FallbackRename(string outPath)
    {
        var dir = Path.GetDirectoryName(outPath) ?? ".";
        var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var newPath = Path.Combine(dir, $"encounter_{stamp}.enc");

        if (string.Equals(outPath, newPath, StringComparison.Ordinal) || File.Exists(newPath))
            return outPath;

        File.Move(outPath, newPath);
        return newPath;
    }

    static string SanitizeFilename(string title)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sb = new StringBuilder(title.Length);
        foreach (var c in title)
        {
            if (Array.IndexOf(invalid, c) < 0)
                sb.Append(c);
        }
        return sb.ToString().Trim();
    }

    static string StripCodeFences(string text)
    {
        var trimmed = text.Trim();
        if (trimmed.StartsWith("```"))
        {
            var firstNewline = trimmed.IndexOf('\n');
            if (firstNewline > 0)
                trimmed = trimmed[(firstNewline + 1)..];
        }
        if (trimmed.EndsWith("```"))
            trimmed = trimmed[..^3].TrimEnd();
        return trimmed;
    }
}
