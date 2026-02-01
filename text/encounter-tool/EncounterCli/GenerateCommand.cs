using System.Text;

namespace EncounterCli;

static class GenerateCommand
{
    const string OraclesDir = "/home/joseph/repos/dreamlands/text/encounters/generation";
    const string LocaleGuideFilename = "locale_guide.txt";

    public static async Task<int> RunAsync(string[] args)
    {
        string? templatePath = null;
        string? outPath = null;
        string? configPath = null;
        var promptsOnly = false;
        var twoPass = false;

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--template" && i + 1 < args.Length) { templatePath = args[i + 1]; i++; }
            else if (args[i] == "--out" && i + 1 < args.Length) { outPath = args[i + 1]; i++; }
            else if (args[i] == "--config" && i + 1 < args.Length) { configPath = args[i + 1]; i++; }
            else if (args[i] == "--prompts-only") promptsOnly = true;
            else if (args[i] == "--two-pass") twoPass = true;
        }

        // Locale guide: always in cwd
        var localePath = Path.GetFullPath(LocaleGuideFilename);
        if (!File.Exists(localePath))
        {
            Console.Error.WriteLine($"Locale guide not found: {localePath}");
            Console.Error.WriteLine("Run this command from a directory containing locale_guide.txt.");
            return 1;
        }

        if (!Directory.Exists(OraclesDir))
        {
            Console.Error.WriteLine($"Oracles directory not found: {OraclesDir}");
            return 1;
        }

        // Default template depends on mode:
        //   single-pass (default) -> encounter_prompt_v2.md (body + choices + outcomes)
        //   two-pass              -> encounter_prompt.md (body only, original)
        if (string.IsNullOrEmpty(templatePath))
        {
            templatePath = twoPass
                ? Path.Combine(OraclesDir, "encounter_prompt.md")
                : Path.Combine(OraclesDir, "encounter_prompt_v2.md");
        }
        else
        {
            templatePath = Path.GetFullPath(templatePath);
        }
        if (!File.Exists(templatePath))
        {
            Console.Error.WriteLine($"Template file not found: {templatePath}");
            return 1;
        }

        // Outcomes template (for two-pass mode)
        var outcomesTemplatePath = Path.Combine(OraclesDir, "outcomes_prompt.md");
        if (twoPass && !File.Exists(outcomesTemplatePath))
        {
            Console.Error.WriteLine($"Outcomes template not found: {outcomesTemplatePath}");
            return 1;
        }

        // Load oracle files
        var situationFile = Path.Combine(OraclesDir, "Situation.txt");
        var forcingFile = Path.Combine(OraclesDir, "Forcing.txt");
        var twistFile = Path.Combine(OraclesDir, "Twist.txt");

        foreach (var (name, path) in new[] { ("Situation.txt", situationFile), ("Forcing.txt", forcingFile), ("Twist.txt", twistFile) })
        {
            if (!File.Exists(path))
            {
                Console.Error.WriteLine($"Oracle file not found: {name} (expected at {path})");
                return 1;
            }
        }

        var situations = File.ReadAllLines(situationFile).Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();
        var forcings = File.ReadAllLines(forcingFile).Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();
        var twists = File.ReadAllLines(twistFile).Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();

        if (situations.Length == 0 || forcings.Length == 0 || twists.Length == 0)
        {
            Console.Error.WriteLine("One or more oracle files are empty.");
            return 1;
        }

        var random = new Random();
        string situation, forcing, twist;
        while (true)
        {
            situation = situations[random.Next(situations.Length)];
            forcing = forcings[random.Next(forcings.Length)];
            twist = twists[random.Next(twists.Length)];

            Console.WriteLine($"Situation: {situation}");
            Console.WriteLine($"Forcing:   {forcing}");
            Console.WriteLine($"Twist:     {twist}");
            Console.Write("\nProceed? [Y/n/r] ");

            var key = Console.ReadLine()?.Trim().ToLowerInvariant() ?? "";
            if (key is "" or "y")
                break;
            if (key == "n")
                return 0;

            Console.WriteLine();
        }
        Console.WriteLine();

        // Build prompt from template
        var template = File.ReadAllText(templatePath);
        var localeGuide = File.ReadAllText(localePath);
        var prompt = template
            .Replace("{{LOCALE_GUIDE}}", localeGuide)
            .Replace("{{ORACLE_SITUATION}}", situation)
            .Replace("{{ORACLE_FORCING}}", forcing)
            .Replace("{{ORACLE_TWIST}}", twist);

        if (promptsOnly)
        {
            Console.WriteLine($"--- Generation Prompt ({(twoPass ? "two-pass: body only" : "single-pass: full encounter")}) ---");
            Console.WriteLine(prompt);
            if (twoPass)
            {
                Console.WriteLine();
                Console.WriteLine("--- Outcomes Prompt (pass 2) ---");
                var outcomesTemplate = File.ReadAllText(outcomesTemplatePath);
                var outcomesPreview = outcomesTemplate
                    .Replace("{{LOCALE_GUIDE}}", localeGuide)
                    .Replace("{{ENCOUNTER_TEXT}}", "(encounter text from pass 1)");
                Console.WriteLine(outcomesPreview);
            }
            return 0;
        }

        var client = LlmClient.TryCreate(configPath);
        if (client == null)
            return 1;

        // Pass 1: Generate encounter
        Console.WriteLine(twoPass ? "Generating encounter body..." : "Generating encounter...");
        Console.WriteLine();
        string? encounterText;
        try
        {
            encounterText = await client.CompleteAsync(prompt);
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
        Console.WriteLine();

        // Pass 2 (two-pass mode only): Generate outcomes with rich context
        string? outcomesText = null;
        if (twoPass)
        {
            Console.Write("Continue to epilogues? [Y/n] ");
            var answer = Console.ReadLine()?.Trim().ToLowerInvariant() ?? "";
            if (answer == "n")
                return 0;

            Console.WriteLine();
            Console.WriteLine("Generating outcomes...");
            Console.WriteLine();
            var outcomesTemplate = File.ReadAllText(outcomesTemplatePath);
            var outcomesPrompt = outcomesTemplate
                .Replace("{{ENCOUNTER_TEXT}}", encounterText)
                .Replace("{{LOCALE_GUIDE}}", localeGuide);

            try
            {
                outcomesText = await client.CompleteAsync(outcomesPrompt);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error generating outcomes: {ex.Message}");
                return 1;
            }

            if (!string.IsNullOrWhiteSpace(outcomesText))
            {
                outcomesText = StripCodeFences(outcomesText);
                outcomesText = EncounterPostProcessor.Process(outcomesText);
                Console.WriteLine(outcomesText);
            }
        }

        // Write output file
        if (!string.IsNullOrEmpty(outPath))
        {
            outPath = Path.GetFullPath(outPath);
            var sb = new StringBuilder();
            sb.Append(encounterText);
            if (twoPass && !string.IsNullOrWhiteSpace(outcomesText))
            {
                sb.AppendLine();
                sb.AppendLine();
                sb.Append(outcomesText);
            }
            var outDir = Path.GetDirectoryName(outPath);
            if (!string.IsNullOrEmpty(outDir))
                Directory.CreateDirectory(outDir);
            File.WriteAllText(outPath, sb.ToString());

            // Rename the file to match the encounter title
            var finalPath = TryRenameToTitle(outPath, sb.ToString());
            Console.WriteLine();
            Console.WriteLine($"Wrote output to {finalPath}");
        }

        return 0;
    }

    /// <summary>
    /// Rename the output file to match the encounter title extracted from the text.
    /// Returns the final path (renamed or original if rename wasn't possible).
    /// </summary>
    static string TryRenameToTitle(string outPath, string text)
    {
        var title = EncounterPostProcessor.ExtractTitle(text);
        if (string.IsNullOrWhiteSpace(title))
            return FallbackRename(outPath);

        // If the "title" is too long, it's probably a paragraph — use a random name instead
        if (title.Length > 60)
            return FallbackRename(outPath);

        var sanitized = SanitizeFilename(title);
        if (string.IsNullOrWhiteSpace(sanitized))
            return FallbackRename(outPath);

        var dir = Path.GetDirectoryName(outPath) ?? ".";
        var newPath = Path.Combine(dir, sanitized + ".enc");

        if (string.Equals(outPath, newPath, StringComparison.Ordinal))
            return outPath; // already named correctly

        if (File.Exists(newPath))
        {
            Console.Error.WriteLine($"Cannot rename: {Path.GetFileName(newPath)} already exists.");
            return outPath;
        }

        File.Move(outPath, newPath);
        return newPath;
    }

    /// <summary>
    /// Rename output file to a random timestamped name when no valid title is available.
    /// </summary>
    static string FallbackRename(string outPath)
    {
        var dir = Path.GetDirectoryName(outPath) ?? ".";
        var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var newPath = Path.Combine(dir, $"encounter_{stamp}.enc");

        if (string.Equals(outPath, newPath, StringComparison.Ordinal))
            return outPath;

        if (File.Exists(newPath))
            return outPath;

        File.Move(outPath, newPath);
        return newPath;
    }

    /// <summary>Strip characters that are invalid in filenames.</summary>
    static string SanitizeFilename(string title)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sb = new System.Text.StringBuilder(title.Length);
        foreach (var c in title)
        {
            if (Array.IndexOf(invalid, c) < 0)
                sb.Append(c);
        }
        return sb.ToString().Trim();
    }

    /// <summary>Strip leading/trailing markdown code fences that LLMs sometimes wrap output in.</summary>
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
