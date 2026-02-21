using Dreamlands.Encounter;
using Dreamlands.Rules;

namespace EncounterCli;

static class CheckCommand
{
    public static int Run(string[] args)
    {
        var path = "encounters";
        var exts = new[] { ".enc" };
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--ext" && i + 1 < args.Length)
            {
                exts = args[i + 1].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (exts.Length == 0) exts = new[] { ".enc" };
                i++;
            }
            else if (!args[i].StartsWith('-')) path = args[i];
        }

        path = Path.GetFullPath(path);
        if (!Directory.Exists(path))
        {
            Console.Error.WriteLine($"Path not found: {path}");
            return 1;
        }

        var files = exts.SelectMany(ext => Directory.GetFiles(path, "*" + ext, SearchOption.AllDirectories))
            .Distinct().OrderBy(f => f).ToArray();
        if (files.Length == 0)
        {
            Console.WriteLine($"No encounter files ({string.Join(", ", exts)}) found under {path}");
            return 0;
        }

        var failed = 0;
        foreach (var file in files)
        {
            var rel = Path.GetRelativePath(path, file);
            var text = File.ReadAllText(file);
            var result = EncounterParser.Parse(text);
            var vocabErrors = new List<string>();

            if (result.Encounter != null)
                vocabErrors = ValidateVocabulary(result.Encounter);

            var markerWarnings = CheckForMarkers(text);

            if (result.IsSuccess && vocabErrors.Count == 0 && markerWarnings.Count == 0)
            {
                Console.WriteLine($"  OK  {rel}");
            }
            else
            {
                failed++;
                Console.WriteLine($"  ERR {rel}");
                foreach (var err in result.Errors)
                    Console.WriteLine($"      {err}");
                foreach (var err in vocabErrors)
                    Console.WriteLine($"      {err}");
                foreach (var warn in markerWarnings)
                    Console.WriteLine($"      {warn}");
            }
        }

        Console.WriteLine();
        Console.WriteLine(failed == 0 ? $"All {files.Length} file(s) valid." : $"{failed} of {files.Length} file(s) had errors.");
        return failed > 0 ? 1 : 0;
    }

    private static List<string> ValidateVocabulary(Encounter encounter)
    {
        var errors = new List<string>();
        foreach (var choice in encounter.Choices)
        {
            if (choice.Requires is { } requires)
            {
                var err = ActionVerb.Validate(requires, VerbUsage.Condition);
                if (err != null)
                    errors.Add($"[requires {requires}]: {err}");
            }

            if (choice.Conditional is { } conditional)
            {
                foreach (var branch in conditional.Branches)
                {
                    var err = ActionVerb.Validate(branch.Condition, VerbUsage.Condition);
                    if (err != null)
                        errors.Add($"@if {branch.Condition}: {err}");
                    ValidateMechanics(branch.Outcome.Mechanics, errors);
                }

                if (conditional.Fallback is { } fallback)
                    ValidateMechanics(fallback.Mechanics, errors);
            }

            if (choice.Single is { } single)
            {
                ValidateMechanics(single.Part.Mechanics, errors);
            }
        }
        return errors;
    }

    private static readonly (char Char, string Name, string Replacement)[] BadChars =
    [
        ('\u2014', "em-dash", "--"),
        ('\u2013', "en-dash", "-"),
        ('\u201C', "left double quote", "\""),
        ('\u201D', "right double quote", "\""),
        ('\u2018', "left single quote", "'"),
        ('\u2019', "right single quote/apostrophe", "'"),
    ];

    private static List<string> CheckForMarkers(string text)
    {
        var warnings = new List<string>();
        var lines = text.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            var trimmed = lines[i].TrimStart();
            if (trimmed.StartsWith("FIXME:"))
                warnings.Add($"Line {i + 1}: FIXME marker (must be expanded before publishing)");
            else if (trimmed.StartsWith("REVIEW:"))
                warnings.Add($"Line {i + 1}: REVIEW marker (must be reviewed before publishing)");

            foreach (var (ch, name, replacement) in BadChars)
            {
                if (lines[i].Contains(ch))
                    warnings.Add($"Line {i + 1}: {name} ({ch}) — use {replacement} instead");
            }
        }
        return warnings;
    }

    private static void ValidateMechanics(IReadOnlyList<string> mechanics, List<string> errors)
    {
        foreach (var mechanic in mechanics)
        {
            var err = ActionVerb.Validate(mechanic, VerbUsage.Mechanic);
            if (err != null)
                errors.Add($"+{mechanic}: {err}");
        }
    }
}
