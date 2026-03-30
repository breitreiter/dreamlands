using Dreamlands.Tactical;

namespace EncounterCli;

static class TacticalCheckCommand
{
    public static int Run(string[] args)
    {
        var path = "tactical";
        for (int i = 0; i < args.Length; i++)
        {
            if (!args[i].StartsWith('-')) path = args[i];
        }

        path = Path.GetFullPath(path);
        if (!Directory.Exists(path))
        {
            Console.Error.WriteLine($"Path not found: {path}");
            return 1;
        }

        var files = Directory.GetFiles(path, "*.tac", SearchOption.AllDirectories)
            .OrderBy(f => f).ToArray();
        if (files.Length == 0)
        {
            Console.WriteLine($"No .tac files found under {path}");
            return 0;
        }

        var failed = 0;
        var warned = 0;
        foreach (var file in files)
        {
            var rel = Path.GetRelativePath(path, file);
            var text = File.ReadAllText(file);
            var result = TacticalParser.Parse(text);

            var warnings = CheckForMarkers(text);

            if (result.IsSuccess && result.Encounter is { } enc)
                warnings.AddRange(CheckOpeningCount(enc));

            if (result.IsSuccess && warnings.Count == 0)
            {
                Console.WriteLine($"  OK  {rel}");
            }
            else
            {
                var hasErrors = !result.IsSuccess;
                if (hasErrors) failed++;
                else warned++;
                Console.WriteLine($"  {(hasErrors ? "ERR" : "WARN")} {rel}");
                foreach (var err in result.Errors)
                    Console.WriteLine($"      {err}");
                foreach (var warn in warnings)
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

    const int MinDistinctOpenings = 14;

    static List<string> CheckOpeningCount(TacticalEncounter enc)
    {
        var warnings = new List<string>();
        var distinct = enc.Openings.Concat(enc.Path)
            .Select(o => o.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();
        if (distinct < MinDistinctOpenings)
            warnings.Add($"Only {distinct} distinct opening(s) across openings + path (minimum {MinDistinctOpenings})");
        return warnings;
    }

    static List<string> CheckForMarkers(string text)
    {
        var warnings = new List<string>();
        var lines = text.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            var trimmed = lines[i].TrimStart();
            if (trimmed.StartsWith("FIXME:"))
                warnings.Add($"Line {i + 1}: FIXME marker");
            else if (trimmed.StartsWith("REVIEW:"))
                warnings.Add($"Line {i + 1}: REVIEW marker");
        }
        return warnings;
    }
}
