using System.Text;
using Microsoft.Extensions.AI;

namespace EncounterCli;

static class GenerateCommand
{
    const string GenerationDir = "/home/joseph/repos/dreamlands/text/encounters/generation";
    const string LocaleGuideFilename = "locale_guide.txt";
    const string SystemPrompt = "You are a narrative designer for a computer RPG. Follow instructions precisely. Output only the requested content.";

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

        // Verify all templates exist
        var structurePath = Path.Combine(GenerationDir, "encounter_structure.md");
        var bodyPath = Path.Combine(GenerationDir, "encounter_prompt.md");
        var outcomesPath = Path.Combine(GenerationDir, "outcomes_prompt.md");

        foreach (var (name, path) in new[] { ("encounter_structure.md", structurePath), ("encounter_prompt.md", bodyPath), ("outcomes_prompt.md", outcomesPath) })
        {
            if (!File.Exists(path))
            {
                Console.Error.WriteLine($"Template not found: {name} (expected at {path})");
                return 1;
            }
        }

        // Load oracle files
        var situationFile = Path.Combine(GenerationDir, "Situation.txt");
        var forcingFile = Path.Combine(GenerationDir, "Forcing.txt");
        var twistFile = Path.Combine(GenerationDir, "Twist.txt");

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

        // Build Turn 1 prompt (structure scaffold) with oracle/locale substituted
        var localeGuide = File.ReadAllText(localePath);
        var structureTemplate = File.ReadAllText(structurePath);
        var structurePrompt = structureTemplate
            .Replace("{{LOCALE_GUIDE}}", localeGuide)
            .Replace("{{ORACLE_SITUATION}}", situation)
            .Replace("{{ORACLE_FORCING}}", forcing)
            .Replace("{{ORACLE_TWIST}}", twist);

        var bodyPrompt = File.ReadAllText(bodyPath);
        var outcomesPrompt = File.ReadAllText(outcomesPath);

        if (promptsOnly)
        {
            Console.WriteLine("--- Pass 1: Structure Scaffold ---");
            Console.WriteLine(structurePrompt);
            Console.WriteLine();
            Console.WriteLine("--- Pass 2: Encounter Body ---");
            Console.WriteLine(bodyPrompt);
            Console.WriteLine();
            Console.WriteLine("--- Pass 3: Choice Outcomes ---");
            Console.WriteLine(outcomesPrompt);
            return 0;
        }

        var client = LlmClient.TryCreate(configPath);
        if (client == null)
            return 1;

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, SystemPrompt),
            new(ChatRole.User, structurePrompt)
        };

        // Pass 1: Structure scaffold
        Console.WriteLine("Generating structure scaffold...");
        Console.WriteLine();
        string? scaffoldText;
        try
        {
            scaffoldText = await client.CompleteAsync(messages);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error generating scaffold: {ex.Message}");
            return 1;
        }

        if (string.IsNullOrWhiteSpace(scaffoldText))
        {
            Console.Error.WriteLine("No response from LLM.");
            return 1;
        }

        Console.WriteLine(scaffoldText);
        Console.WriteLine();

        // Optional steering notes before body generation
        Console.Write("Steering notes (enter to skip, 'n' to quit): ");
        var steering = Console.ReadLine()?.Trim() ?? "";
        if (steering.Equals("n", StringComparison.OrdinalIgnoreCase))
            return 0;
        Console.WriteLine();

        // Pass 2: Encounter body — prepend steering notes if provided
        var bodyWithSteering = string.IsNullOrEmpty(steering)
            ? bodyPrompt
            : $"The user has the following notes on the scaffold:\n{steering}\n\nTake these into account.\n\n{bodyPrompt}";
        messages.Add(new ChatMessage(ChatRole.User, bodyWithSteering));

        Console.WriteLine("Generating encounter body...");
        Console.WriteLine();
        string? encounterText;
        try
        {
            encounterText = await client.CompleteAsync(messages);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error generating encounter body: {ex.Message}");
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
        Console.Write("Continue to epilogues? [Y/n] ");
        if ((Console.ReadLine()?.Trim().ToLowerInvariant() ?? "") == "n")
            return 0;
        Console.WriteLine();

        // Pass 3: Choice outcomes
        messages.Add(new ChatMessage(ChatRole.User, outcomesPrompt));

        Console.WriteLine("Generating outcomes...");
        Console.WriteLine();
        string? outcomesText;
        try
        {
            outcomesText = await client.CompleteAsync(messages);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error generating outcomes: {ex.Message}");
            return 1;
        }

        if (string.IsNullOrWhiteSpace(outcomesText))
        {
            Console.Error.WriteLine("No response from LLM.");
            return 1;
        }

        outcomesText = StripCodeFences(outcomesText);
        outcomesText = EncounterPostProcessor.Process(outcomesText);
        Console.WriteLine(outcomesText);

        // Write output file
        if (!string.IsNullOrEmpty(outPath))
        {
            outPath = Path.GetFullPath(outPath);
            var sb = new StringBuilder();
            sb.Append(encounterText);
            sb.AppendLine();
            sb.AppendLine();
            sb.Append(outcomesText);

            var outDir = Path.GetDirectoryName(outPath);
            if (!string.IsNullOrEmpty(outDir))
                Directory.CreateDirectory(outDir);
            File.WriteAllText(outPath, sb.ToString());

            var finalPath = TryRenameToTitle(outPath, sb.ToString());
            Console.WriteLine();
            Console.WriteLine($"Wrote output to {finalPath}");
        }

        return 0;
    }

    static string TryRenameToTitle(string outPath, string text)
    {
        var title = EncounterPostProcessor.ExtractTitle(text);
        if (string.IsNullOrWhiteSpace(title))
            return FallbackRename(outPath);

        if (title.Length > 60)
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

        if (string.Equals(outPath, newPath, StringComparison.Ordinal))
            return outPath;

        if (File.Exists(newPath))
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
