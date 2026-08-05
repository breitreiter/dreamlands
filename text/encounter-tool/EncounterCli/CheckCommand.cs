using Dreamlands.Encounter;
using Dreamlands.Rules;

namespace EncounterCli;

static class CheckCommand
{
    public static int Run(string[] args)
    {
        var path = "encounters";
        var exts = new[] { ".enc", ".fight" };
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
        var shortIdsByDir = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in files)
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

            if (file.EndsWith(".fight", StringComparison.OrdinalIgnoreCase))
            {
                var fightDir = Path.GetDirectoryName(file) ?? "";
                if (CheckFight(text, rel, shortIdsByDir.GetValueOrDefault(fightDir))) failed++;
                continue;
            }

            var result = EncounterParser.Parse(text);
            var vocabErrors = new List<string>();
            var idWarnings = new List<string>();

            if (result.Encounter != null)
            {
                vocabErrors = ValidateVocabulary(result.Encounter);
                ValidateAccessibility(result.Encounter, vocabErrors);
                ValidateBodyIsProse(result.Encounter.Body, vocabErrors);
                if (registry != null)
                    idWarnings = ValidateKnownIds(result.Encounter, registry);

                var dir = Path.GetDirectoryName(file) ?? "";
                var navErrors = ValidateOpenTargets(result.Encounter, shortIdsByDir.GetValueOrDefault(dir));
                vocabErrors.AddRange(navErrors);

                ValidateNoMechanicsDropped(text, result.Encounter, vocabErrors);
            }

            var markerWarnings = CheckForMarkers(text);
            var dashErrors = CheckForDashAffectations(text);
            vocabErrors.AddRange(dashErrors);

            var parseWarnings = result.Errors.Where(e => e.IsWarning).ToList();
            var parseErrors = result.Errors.Where(e => !e.IsWarning).ToList();
            var allWarnings = parseWarnings.Select(w => w.ToString()).Concat(markerWarnings).Concat(idWarnings).ToList();

            if (result.IsSuccess && vocabErrors.Count == 0 && allWarnings.Count == 0)
            {
                Console.WriteLine($"  OK  {rel}");
            }
            else
            {
                var hasErrors = parseErrors.Count > 0 || vocabErrors.Count > 0;
                if (hasErrors)
                    failed++;
                else
                    warned++;
                Console.WriteLine($"  {(hasErrors ? "ERR" : "WARN")} {rel}");
                foreach (var err in parseErrors)
                    Console.WriteLine($"      {err}");
                foreach (var err in vocabErrors)
                    Console.WriteLine($"      {err}");
                foreach (var warn in allWarnings)
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

    /// <summary>Validate a .fight file. Returns true if it had errors.</summary>
    private static bool CheckFight(string text, string rel, HashSet<string>? siblingIds)
    {
        var errors = new List<string>();
        CombatEncounter? fight = null;
        try
        {
            fight = CmbParser.ParseString(text, rel);
        }
        catch (FormatException ex)
        {
            errors.Add(ex.Message);
        }

        if (fight != null)
        {
            if (fight.Title.Length == 0)
                errors.Add("missing [title]");
            if (fight.Stats.Hp <= 0)
                errors.Add("missing [stats hp=<n>] with n > 0");
            if (fight.Moves.Count == 0)
                errors.Add("no '* move' sections — a fight needs a move pool");
            if (fight.Persistent && fight.Requires.Count == 0)
                errors.Add("[persistent] fight has no [requires] gate — it would spawn forever; gate it on a tag/quality");

            foreach (var req in fight.Requires)
            {
                var err = ActionVerb.Validate(req, VerbUsage.Condition);
                if (err != null)
                    errors.Add($"[requires {req}]: {err}");
                ValidateItemId(req, errors);
            }
            ValidateMechanics(fight.WinMechanics, errors, isFight: true);
            ValidateMechanics(fight.LoseMechanics, errors, isFight: true);
            ValidateMechanics(fight.FleeMechanics, errors, isFight: true);

            foreach (var target in CollectVerbTargets(fight.WinMechanics.Concat(fight.FleeMechanics), "chain"))
            {
                if (!target.Contains('/') && (siblingIds == null || !siblingIds.Contains(target)))
                    errors.Add($"+chain {target}: no encounter file '{target}' in the same directory (qualified ids allowed for cross-directory chains)");
            }
            foreach (var target in CollectVerbTargets(fight.LoseMechanics, "chain"))
                errors.Add($"+chain {target}: chaining from a lose outro is not supported — defeat goes to rescue");
        }

        errors.AddRange(CheckForDashAffectations(text));

        if (errors.Count == 0)
        {
            Console.WriteLine($"  OK  {rel}");
            return false;
        }
        Console.WriteLine($"  ERR {rel}");
        foreach (var err in errors)
            Console.WriteLine($"      {err}");
        return true;
    }

    private static void ValidateAccessibility(Encounter encounter, List<string> errors)
    {
        if (encounter.Choices.Count > 0 && encounter.Choices.All(c => c.Requires != null))
            errors.Add("All choices are gated with [requires] — at least one must be unconditional");
    }

    /// <summary>
    /// Body is rendered as raw prose at runtime; flow-control tokens (@if/@else/@elif,
    /// `+verb`, choice markers) only have meaning inside the choices block. If they
    /// appear in the body the player sees them literally.
    /// </summary>
    private static void ValidateBodyIsProse(string body, List<string> errors)
    {
        var bodyLines = body.Split('\n');
        for (int i = 0; i < bodyLines.Length; i++)
        {
            var trimmed = bodyLines[i].TrimStart();
            if (trimmed.StartsWith("@if ", StringComparison.Ordinal) ||
                trimmed.StartsWith("@elif ", StringComparison.Ordinal) ||
                trimmed.StartsWith("@else", StringComparison.Ordinal) ||
                trimmed == "}" ||
                trimmed.StartsWith("} @", StringComparison.Ordinal))
            {
                errors.Add($"Body line {i + 1}: flow-control token '{trimmed.Split(' ')[0]}' is only valid inside the choices block — body renders as raw prose");
            }
            else if (trimmed.Length >= 2 && trimmed[0] == '+' && char.IsLetter(trimmed[1]))
            {
                errors.Add($"Body line {i + 1}: mechanic verb '+{trimmed[1..].Split(' ')[0]}' is only valid inside the choices block");
            }
        }
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

                ValidateMechanics(conditional.Mechanics, errors);
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
            if (lines[i].TrimStart().StartsWith('#')) continue; // skip pipeline draft comments
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
            if (trimmed.StartsWith('#')) continue; // skip pipeline draft comments

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

    /// <summary>
    /// Every `+verb` in the source must survive into the parsed model. A mechanic the
    /// parser discards is invisible: the file reads correctly, `check` passes, and the
    /// game quietly loses a navigation or a tag. That is exactly how `+open` hub returns
    /// died in five arcs (bugs/choice_mechanics_after_conditional_dropped.md), so this
    /// reconciles the two sides by count rather than trusting any single code path.
    /// </summary>
    private static void ValidateNoMechanicsDropped(string text, Encounter encounter, List<string> errors)
    {
        // Mirror the parser's own rule for what counts as a mechanic line: '+' then a letter.
        var inSource = text.Split('\n')
            .Select(l => l.Trim())
            .Count(l => l.Length >= 2 && l[0] == '+' && char.IsLetter(l[1]));

        var inModel = 0;
        foreach (var choice in encounter.Choices)
        {
            if (choice.Single is { } single) inModel += single.Part.Mechanics.Count;
            if (choice.Conditional is { } cond)
            {
                foreach (var branch in cond.Branches) inModel += branch.Outcome.Mechanics.Count;
                if (cond.Fallback is { } fb) inModel += fb.Mechanics.Count;
                inModel += cond.Mechanics.Count;
            }
        }

        if (inModel < inSource)
            errors.Add($"{inSource - inModel} mechanic line(s) were dropped during parsing "
                       + $"({inSource} '+verb' lines in the file, {inModel} in the parsed model). "
                       + "A discarded mechanic silently loses navigation or tags.");
    }

    private static void ValidateMechanics(IReadOnlyList<string> mechanics, List<string> errors, bool isFight = false)
    {
        foreach (var mechanic in mechanics)
        {
            var err = ActionVerb.Validate(mechanic, VerbUsage.Mechanic);
            if (err != null)
                errors.Add($"+{mechanic}: {err}");
            ValidateItemId(mechanic, errors);

            // +open/+combat/+chain are context-specific: encounters navigate with
            // +open and hand off with +combat; fight outros chain with +chain.
            var verb = ActionVerb.Tokenize(mechanic).FirstOrDefault();
            if (isFight && verb is "open" or "combat")
                errors.Add($"+{mechanic}: '{verb}' is not valid in fight outros — use +chain to launch a .enc after the coda");
            else if (!isFight && verb == "chain")
                errors.Add($"+{mechanic}: 'chain' is fight-outro only — use +open to navigate between encounters");
        }
    }

    private static IEnumerable<string> CollectVerbTargets(IEnumerable<string> mechanics, string verb)
    {
        foreach (var mechanic in mechanics)
        {
            var tokens = ActionVerb.Tokenize(mechanic);
            if (tokens.Count >= 2 && tokens[0] == verb)
                yield return tokens[1];
        }
    }

    private static List<string> ValidateOpenTargets(Encounter encounter, HashSet<string>? siblingIds)
    {
        var errors = new List<string>();
        var mechanics = new List<string>();

        foreach (var choice in encounter.Choices)
        {
            if (choice.Conditional is { } conditional)
            {
                foreach (var branch in conditional.Branches)
                    mechanics.AddRange(branch.Outcome.Mechanics);
                if (conditional.Fallback is { } fallback)
                    mechanics.AddRange(fallback.Mechanics);
            }
            if (choice.Single is { } single)
                mechanics.AddRange(single.Part.Mechanics);
        }

        foreach (var target in CollectVerbTargets(mechanics, "open"))
        {
            if (siblingIds == null || !siblingIds.Contains(target))
                errors.Add($"+open {target}: no encounter file '{target}' (.enc) found in the same directory");
        }
        foreach (var target in CollectVerbTargets(mechanics, "combat"))
        {
            if (!target.Contains('/') && (siblingIds == null || !siblingIds.Contains(target)))
                errors.Add($"+combat {target}: no fight file '{target}' (.fight) found in the same directory (qualified ids allowed)");
        }
        return errors;
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
        int pos = 0;
        while (pos < tokens.Count)
        {
            var rawVerb = tokens[pos];
            if (rawVerb is "&&" or "||") { pos++; continue; }

            var verb = rawVerb.TrimStart('!');
            var verbDef = ActionVerb.FromName(verb);
            pos++;

            if (pos < tokens.Count && TagVerbs.Contains(verb))
            {
                var warn = IdRegistry.CheckId(tokens[pos], registry.Tags, "tag");
                if (warn != null) warnings.Add(warn);
            }
            else if (pos < tokens.Count && QualityVerbs.Contains(verb))
            {
                var warn = IdRegistry.CheckId(tokens[pos], registry.Qualities, "quality");
                if (warn != null) warnings.Add(warn);
            }

            pos += verbDef?.Args.Count ?? 0;
        }
    }

    private static void ValidateItemId(string action, List<string> errors)
    {
        var tokens = ActionVerb.Tokenize(action);
        int pos = 0;
        while (pos < tokens.Count)
        {
            var rawVerb = tokens[pos];
            if (rawVerb is "&&" or "||") { pos++; continue; }

            var verb = rawVerb.TrimStart('!');
            var verbDef = ActionVerb.FromName(verb);
            pos++;

            if (pos < tokens.Count && ItemIdVerbs.Contains(verb))
            {
                var itemId = tokens[pos];
                if (!ItemDef.IsValidId(itemId))
                    errors.Add($"'{verb} {itemId}': unknown item id. Not found in ItemDef.");
            }

            pos += verbDef?.Args.Count ?? 0;
        }
    }
}
