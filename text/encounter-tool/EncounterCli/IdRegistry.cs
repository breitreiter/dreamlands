namespace EncounterCli;

/// <summary>
/// Loads known tag/quality IDs from a registry file and provides fuzzy matching
/// for typo detection using Levenshtein distance.
/// </summary>
class IdRegistry
{
    public HashSet<string> Tags { get; } = new();
    public HashSet<string> Qualities { get; } = new();

    IdRegistry() { }

    /// <summary>
    /// Load known_ids.txt from the given encounters directory.
    /// Returns null if the file doesn't exist.
    /// </summary>
    public static IdRegistry? Load(string encountersPath)
    {
        var file = Path.Combine(encountersPath, "known_ids.txt");
        if (!File.Exists(file)) return null;

        var registry = new IdRegistry();
        var target = (HashSet<string>?)null;

        foreach (var raw in File.ReadLines(file))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line[0] == '#')
            {
                // Section headers are comment lines containing "Tags" or "Qualities"
                if (line.Contains("Tags", StringComparison.OrdinalIgnoreCase))
                    target = registry.Tags;
                else if (line.Contains("Qualities", StringComparison.OrdinalIgnoreCase))
                    target = registry.Qualities;
                continue;
            }
            target?.Add(line);
        }

        return registry;
    }

    /// <summary>
    /// Check an ID against a known set. Returns null if valid,
    /// or a warning message if unknown (with a "did you mean?" suggestion when close).
    /// </summary>
    public static string? CheckId(string id, HashSet<string> known, string kind)
    {
        if (known.Contains(id)) return null;

        var suggestion = FindClosest(id, known, maxDistance: 2);
        var hint = suggestion != null ? $" Did you mean '{suggestion}'?" : "";
        return $"unknown {kind} '{id}' — not in known_ids.txt.{hint}";
    }

    static string? FindClosest(string input, HashSet<string> candidates, int maxDistance)
    {
        string? best = null;
        var bestDist = maxDistance + 1;

        foreach (var candidate in candidates)
        {
            // Skip if length difference alone exceeds max distance
            if (Math.Abs(input.Length - candidate.Length) > maxDistance) continue;

            var dist = LevenshteinDistance(input, candidate);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = candidate;
            }
        }

        return best;
    }

    static int LevenshteinDistance(string a, string b)
    {
        var m = a.Length;
        var n = b.Length;
        var prev = new int[n + 1];
        var curr = new int[n + 1];

        for (var j = 0; j <= n; j++) prev[j] = j;

        for (var i = 1; i <= m; i++)
        {
            curr[0] = i;
            for (var j = 1; j <= n; j++)
            {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                curr[j] = Math.Min(
                    Math.Min(curr[j - 1] + 1, prev[j] + 1),
                    prev[j - 1] + cost);
            }
            (prev, curr) = (curr, prev);
        }

        return prev[n];
    }
}
