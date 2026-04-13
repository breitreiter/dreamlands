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

        // Build a lookup of directory -> set of short IDs (filename stems) for +open validation
        // Include both .enc and .tac files so +open can resolve to tactical encounters
        var shortIdsByDir = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        var allOpenTargets = files
            .Concat(Directory.GetFiles(path, "*.tac", SearchOption.AllDirectories));
        foreach (var file in allOpenTargets)
        {
            var dir = Path.GetDirectoryName(file) ?? "";
            if (!shortIdsByDir.TryGetValue(dir, out var ids))
            {
                ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                shortIdsByDir[dir] = ids;
            }
            ids.Add(Path.GetFileNameWithoutExtension(file));
        }

        var failed = 0;
        var warned = 0;
        var autoFixed = 0;
        foreach (var file in files)
        {
            var rel = Path.GetRelativePath(path, file);
            var text = File.ReadAllText(file);

            if (AutoFixBadChars(file, text, rel))
            {
                autoFixed++;
                text = File.ReadAllText(file);
            }

            var result = EncounterParser.Parse(text);
            var vocabErrors = new List<string>();
            var idWarnings = new List<string>();

            if (result.Encounter != null)
            {
                vocabErrors = ValidateVocabulary(result.Encounter);
                ValidateAccessibility(result.Encounter, vocabErrors);
                if (registry != null)
                    idWarnings = ValidateKnownIds(result.Encounter, registry);

                var dir = Path.GetDirectoryName(file) ?? "";
                var navErrors = ValidateOpenTargets(result.Encounter, shortIdsByDir.GetValueOrDefault(dir));
                vocabErrors.AddRange(navErrors);
            }

            var markerWarnings = CheckForMarkers(text);
            var dashErrors = CheckForDashAffectations(text);
            vocabErrors.AddRange(dashErrors);

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
        var fixNote = autoFixed > 0 ? $" ({autoFixed} auto-fixed)" : "";
        if (failed == 0 && warned == 0)
            Console.WriteLine($"All {files.Length} file(s) valid.{fixNote}");
        else if (failed == 0)
            Console.WriteLine($"All {files.Length} file(s) valid ({warned} with warnings).{fixNote}");
        else
            Console.WriteLine($"{failed} of {files.Length} file(s) had errors{(warned > 0 ? $", {warned} with warnings" : "")}.{fixNote}");
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

    private static readonly (string Pattern, string Label)[] DashAffectations =
    [
        ("\u2014", "em-dash (\u2014)"),
        ("--", "double-dash (--)"),
    ];

    internal static List<string> CheckForDashAffectations(string text)
    {
        var errors = new List<string>();
        var lines = text.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            foreach (var (pattern, label) in DashAffectations)
            {
                if (lines[i].Contains(pattern))
                    errors.Add($"Line {i + 1}: {label} — rewrite the sentence instead of using a dash for a dramatic pause or aside");
            }
        }
        return errors;
    }

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

            foreach (var phrase in BannedPhrases)
            {
                if (lines[i].Contains(phrase, StringComparison.OrdinalIgnoreCase))
                    warnings.Add($"Line {i + 1}: banned phrase \"{phrase}\"");
            }
        }
        return warnings;
    }

    private static bool AutoFixBadChars(string filePath, string text, string relPath)
    {
        var original = text;
        foreach (var (ch, _, replacement) in BadChars)
            text = text.Replace(ch.ToString(), replacement);

        if (text == original) return false;

        File.WriteAllText(filePath, text);
        Console.WriteLine($"      auto-fixed bad characters");
        return true;
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

    private static List<string> ValidateOpenTargets(Encounter encounter, HashSet<string>? siblingIds)
    {
        var errors = new List<string>();
        var targets = new List<string>();

        foreach (var choice in encounter.Choices)
        {
            if (choice.Conditional is { } conditional)
            {
                foreach (var branch in conditional.Branches)
                    CollectOpenTargets(branch.Outcome.Mechanics, targets);
                if (conditional.Fallback is { } fallback)
                    CollectOpenTargets(fallback.Mechanics, targets);
            }
            if (choice.Single is { } single)
                CollectOpenTargets(single.Part.Mechanics, targets);
        }

        foreach (var target in targets)
        {
            if (siblingIds == null || !siblingIds.Contains(target))
                errors.Add($"+open {target}: no encounter file '{target}' (.enc or .tac) found in the same directory");
        }
        return errors;
    }

    private static void CollectOpenTargets(IReadOnlyList<string> mechanics, List<string> targets)
    {
        foreach (var mechanic in mechanics)
        {
            var tokens = ActionVerb.Tokenize(mechanic);
            if (tokens.Count >= 2 && tokens[0] == "open")
                targets.Add(tokens[1]);
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
