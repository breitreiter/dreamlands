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

        var registry = IdRegistry.Load(path);
        if (registry == null)
            Console.WriteLine("  (no known_ids.txt found — skipping tag/quality validation)");

        var files = exts.SelectMany(ext => Directory.GetFiles(path, "*" + ext, SearchOption.AllDirectories))
            .Distinct().OrderBy(f => f).ToArray();
        if (files.Length == 0)
        {
            Console.WriteLine($"No encounter files ({string.Join(", ", exts)}) found under {path}");
            return 0;
        }

        var failed = 0;
        var warned = 0;
        foreach (var file in files)
        {
            var rel = Path.GetRelativePath(path, file);
            var text = File.ReadAllText(file);
            var result = EncounterParser.Parse(text);
            var vocabErrors = new List<string>();
            var idWarnings = new List<string>();

            if (result.Encounter != null)
            {
                vocabErrors = ValidateVocabulary(result.Encounter);
                ValidateAccessibility(result.Encounter, vocabErrors);
                if (registry != null)
                    idWarnings = ValidateKnownIds(result.Encounter, registry);
            }

            var markerWarnings = CheckForMarkers(text);

            if (result.IsSuccess && vocabErrors.Count == 0 && markerWarnings.Count == 0 && idWarnings.Count == 0)
            {
                Console.WriteLine($"  OK  {rel}");
            }
            else
            {
                var hasErrors = !result.IsSuccess || vocabErrors.Count > 0;
                if (hasErrors)
                    failed++;
                else
                    warned++;
                Console.WriteLine($"  {(hasErrors ? "ERR" : "WARN")} {rel}");
                foreach (var err in result.Errors)
                    Console.WriteLine($"      {err}");
                foreach (var err in vocabErrors)
                    Console.WriteLine($"      {err}");
                foreach (var warn in idWarnings)
                    Console.WriteLine($"      {warn}");
                foreach (var warn in markerWarnings)
                    Console.WriteLine($"      {warn}");
            }
        }

        Console.WriteLine();
        if (failed == 0 && warned == 0)
            Console.WriteLine($"All {files.Length} file(s) valid.");
        else if (failed == 0)
            Console.WriteLine($"All {files.Length} file(s) valid ({warned} with warnings).");
        else
            Console.WriteLine($"{failed} of {files.Length} file(s) had errors{(warned > 0 ? $", {warned} with warnings" : "")}.");
        return failed > 0 ? 1 : 0;
    }

    private static void ValidateAccessibility(Encounter encounter, List<string> errors)
    {
        if (encounter.Choices.Count > 0 && encounter.Choices.All(c => c.Requires != null))
            errors.Add("All choices are gated with [requires] — at least one must be unconditional");
    }

    private static readonly HashSet<string> ItemIdVerbs = new()
    {
        "add_item", "has", "equip", "discard"
    };

    private static List<string> ValidateVocabulary(Encounter encounter)
    {
        var errors = new List<string>();
        foreach (var req in encounter.Requires)
        {
            var err = ActionVerb.Validate(req, VerbUsage.Condition);
            if (err != null)
                errors.Add($"encounter [requires {req}]: {err}");
            ValidateItemId(req, errors);
        }
        foreach (var choice in encounter.Choices)
        {
            if (choice.Requires is { } requires)
            {
                var err = ActionVerb.Validate(requires, VerbUsage.Condition);
                if (err != null)
                    errors.Add($"[requires {requires}]: {err}");
                ValidateItemId(requires, errors);
            }

            if (choice.Conditional is { } conditional)
            {
                foreach (var branch in conditional.Branches)
                {
                    var err = ActionVerb.Validate(branch.Condition, VerbUsage.Condition);
                    if (err != null)
                        errors.Add($"@if {branch.Condition}: {err}");
                    ValidateItemId(branch.Condition, errors);
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

    private static readonly string[] BannedPhrases =
    [
        "trader's guild",
        "merchant guild",
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

            foreach (var phrase in BannedPhrases)
            {
                if (lines[i].Contains(phrase, StringComparison.OrdinalIgnoreCase))
                    warnings.Add($"Line {i + 1}: banned phrase \"{phrase}\"");
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
            ValidateItemId(mechanic, errors);
        }
    }

    private static readonly HashSet<string> TagVerbs = new() { "add_tag", "remove_tag", "tag" };
    private static readonly HashSet<string> QualityVerbs = new() { "quality" };

    private static List<string> ValidateKnownIds(Encounter encounter, IdRegistry registry)
    {
        var warnings = new List<string>();
        CollectIdWarnings(encounter.Requires, registry, warnings);
        foreach (var choice in encounter.Choices)
        {
            if (choice.Requires is { } requires)
                CheckActionId(requires, registry, warnings);
            if (choice.Conditional is { } conditional)
            {
                foreach (var branch in conditional.Branches)
                {
                    CheckActionId(branch.Condition, registry, warnings);
                    CollectIdWarnings(branch.Outcome.Mechanics, registry, warnings);
                }
                if (conditional.Fallback is { } fallback)
                    CollectIdWarnings(fallback.Mechanics, registry, warnings);
            }
            if (choice.Single is { } single)
                CollectIdWarnings(single.Part.Mechanics, registry, warnings);
        }
        return warnings;
    }

    private static void CollectIdWarnings(IReadOnlyList<string> actions, IdRegistry registry, List<string> warnings)
    {
        foreach (var action in actions)
            CheckActionId(action, registry, warnings);
    }

    private static void CheckActionId(string action, IdRegistry registry, List<string> warnings)
    {
        var tokens = ActionVerb.Tokenize(action);
        if (tokens.Count < 2) return;
        var verb = tokens[0];

        if (TagVerbs.Contains(verb))
        {
            var warn = IdRegistry.CheckId(tokens[1], registry.Tags, "tag");
            if (warn != null) warnings.Add(warn);
        }
        else if (QualityVerbs.Contains(verb))
        {
            var warn = IdRegistry.CheckId(tokens[1], registry.Qualities, "quality");
            if (warn != null) warnings.Add(warn);
        }
    }

    private static void ValidateItemId(string action, List<string> errors)
    {
        var tokens = ActionVerb.Tokenize(action);
        if (tokens.Count < 2) return;
        if (!ItemIdVerbs.Contains(tokens[0])) return;
        var itemId = tokens[1];
        if (!ItemDef.IsValidId(itemId))
            errors.Add($"'{tokens[0]} {itemId}': unknown item id. Not found in ItemDef.");
    }
}
