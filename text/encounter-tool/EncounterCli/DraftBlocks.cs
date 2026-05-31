using System.Text.RegularExpressions;

namespace EncounterCli;

/// <summary>
/// Shared utilities for the arc-writer pipeline passes (colorize, factual, voice, critic).
/// Each pass walks FIXME beats and attaches a `# --- KIND ---` block immediately below.
/// Re-runs work by deletion: a pass that finds no downstream block of its kind generates one;
/// finding one means done.
/// </summary>
static class DraftBlocks
{
    public static readonly Regex FixmePattern = new(
        @"^(\s*)FIXME(?:\([^)]*\))?:\s*(.*)$",
        RegexOptions.Compiled);

    static readonly Regex RegisterPattern = new(@"FIXME\(([^)]+)\):", RegexOptions.Compiled);

    /// <summary>
    /// True if a `# --- KIND` opener appears between the FIXME and the next
    /// non-comment, non-blank line. Other comment blocks (other KINDs) in
    /// between are allowed and ignored.
    /// </summary>
    public static bool HasAdjacentBlock(List<string> lines, int fixmeIndex, string kind)
    {
        var marker = $"# --- {kind}";
        for (int i = fixmeIndex + 1; i < lines.Count; i++)
        {
            var trimmed = lines[i].TrimStart();
            if (string.IsNullOrEmpty(trimmed)) continue;
            if (!trimmed.StartsWith('#')) return false;
            if (trimmed.StartsWith(marker, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Extract the body lines from an adjacent block of the given KIND.
    /// Body lines have their `# ` (or `#`) prefix stripped. Returns null
    /// if no such block exists adjacent to this FIXME.
    /// Block closes at `# --- end ---` or any other `# ---` header.
    /// </summary>
    public static List<string>? ExtractBlock(List<string> lines, int fixmeIndex, string kind)
    {
        var openMarker = $"# --- {kind}";
        for (int i = fixmeIndex + 1; i < lines.Count; i++)
        {
            var trimmed = lines[i].TrimStart();
            if (string.IsNullOrEmpty(trimmed)) continue;
            if (!trimmed.StartsWith('#')) return null;
            if (!trimmed.StartsWith(openMarker, StringComparison.OrdinalIgnoreCase))
                continue;

            var body = new List<string>();
            for (int j = i + 1; j < lines.Count; j++)
            {
                var t = lines[j].TrimStart();
                if (!t.StartsWith('#')) break;
                if (t.StartsWith("# ---", StringComparison.Ordinal)) break;
                if (t.StartsWith("# ", StringComparison.Ordinal)) body.Add(t[2..]);
                else if (t == "#") body.Add("");
                else body.Add(t[1..]);
            }
            return body;
        }
        return null;
    }

    /// <summary>
    /// Build the lines for a `# --- KIND_HEADER ---` block with the given body
    /// content, all prefixed with the supplied indent.
    /// </summary>
    public static List<string> BuildBlock(string indent, string kindHeader, IEnumerable<string> bodyLines)
    {
        var result = new List<string> { $"{indent}# --- {kindHeader} ---" };
        foreach (var line in bodyLines)
        {
            if (string.IsNullOrEmpty(line))
                result.Add($"{indent}#");
            else
                result.Add($"{indent}# {line}");
        }
        result.Add($"{indent}# --- end ---");
        return result;
    }

    /// <summary>
    /// Find the line index where a new block of kind `afterKind` should be
    /// inserted: just after the closing `# --- end ---` of the adjacent
    /// block of the given KIND. Falls back to fixmeIndex + 1 if no such
    /// block is present.
    /// </summary>
    public static int FindInsertionAfter(List<string> lines, int fixmeIndex, string afterKind)
    {
        var openMarker = $"# --- {afterKind}";
        bool inBlock = false;
        for (int i = fixmeIndex + 1; i < lines.Count; i++)
        {
            var trimmed = lines[i].TrimStart();
            if (string.IsNullOrEmpty(trimmed))
            {
                if (!inBlock) continue;
                continue;
            }
            if (!trimmed.StartsWith('#')) return i;
            if (!inBlock && trimmed.StartsWith(openMarker, StringComparison.OrdinalIgnoreCase))
            {
                inBlock = true;
                continue;
            }
            if (inBlock && trimmed.StartsWith("# --- end", StringComparison.OrdinalIgnoreCase))
                return i + 1;
            if (!inBlock && trimmed.StartsWith("# ---", StringComparison.Ordinal))
                return i;
        }
        return lines.Count;
    }

    public static string? ExtractRegister(string fixmeLine)
    {
        var m = RegisterPattern.Match(fixmeLine);
        return m.Success ? m.Groups[1].Value.Trim() : null;
    }

    /// <summary>
    /// Walk backward from the FIXME to describe its location in the encounter:
    /// "encounter body", "outcome of choice X", or "outcome of choice X / branch Y".
    /// </summary>
    public static string DescribeBeatContext(List<string> lines, int fixmeIndex)
    {
        string? choice = null;
        string? branch = null;
        int depth = 0;
        for (int i = fixmeIndex - 1; i >= 0; i--)
        {
            var trimmed = lines[i].TrimStart();
            if (trimmed.StartsWith('#')) continue;
            if (trimmed.StartsWith('}')) { depth++; continue; }
            if (trimmed.EndsWith('{') && depth > 0) { depth--; continue; }

            if (branch == null)
            {
                if (trimmed.StartsWith("@if ", StringComparison.Ordinal))
                    branch = trimmed[4..].TrimEnd('{', ' ', '\t');
                else if (trimmed.StartsWith("} @elif ", StringComparison.Ordinal))
                    branch = trimmed[8..].TrimEnd('{', ' ', '\t');
                else if (trimmed.StartsWith("@elif ", StringComparison.Ordinal))
                    branch = trimmed[6..].TrimEnd('{', ' ', '\t');
                else if (trimmed.StartsWith("} @else", StringComparison.Ordinal) || trimmed.StartsWith("@else", StringComparison.Ordinal))
                    branch = "else";
            }

            if (trimmed.StartsWith("* ", StringComparison.Ordinal))
            {
                var afterStar = trimmed[2..];
                var eq = afterStar.IndexOf('=');
                choice = (eq > 0 ? afterStar[..eq] : afterStar).Trim();
                break;
            }
            if (trimmed.Equals("choices:", StringComparison.Ordinal)) break;
        }

        if (choice == null) return "encounter body (before the choices block)";
        if (branch == null) return $"outcome of choice \"{choice}\"";
        return $"outcome of choice \"{choice}\", branch: {branch}";
    }

    public static string InferBiome(string arcDir)
    {
        var parts = arcDir.Split(Path.DirectorySeparatorChar);
        for (int i = 0; i < parts.Length - 1; i++)
            if (parts[i].Equals("arcs", StringComparison.OrdinalIgnoreCase))
                return parts[i + 1];
        return "";
    }

    /// <summary>
    /// Concatenate every tier's locale_guide.txt for the given biome.
    /// Searches up from cwd for text/encounters/&lt;biome&gt;/.
    /// </summary>
    public static string? LoadLocaleGuide(string biome)
    {
        if (string.IsNullOrEmpty(biome)) return null;
        var dir = Directory.GetCurrentDirectory();
        while (dir != null)
        {
            var biomeDir = Path.Combine(dir, "text", "encounters", biome);
            if (Directory.Exists(biomeDir))
            {
                var guides = new List<string>();
                foreach (var sub in Directory.GetDirectories(biomeDir).OrderBy(p => p))
                {
                    var lg = Path.Combine(sub, "locale_guide.txt");
                    if (File.Exists(lg))
                        guides.Add($"### {Path.GetFileName(sub)}\n\n{File.ReadAllText(lg)}");
                }
                if (guides.Count > 0)
                    return string.Join("\n\n---\n\n", guides);
            }
            dir = Path.GetDirectoryName(dir);
        }
        return null;
    }

    /// <summary>
    /// Concatenate every .md file in the arc directory as the brief.
    /// </summary>
    public static string? LoadBrief(string arcDir)
    {
        var briefFiles = Directory.GetFiles(arcDir, "*.md").OrderBy(f => f).ToList();
        if (briefFiles.Count == 0) return null;
        return string.Join("\n\n---\n\n",
            briefFiles.Select(f => $"### {Path.GetFileName(f)}\n\n{File.ReadAllText(f)}"));
    }

    /// <summary>
    /// Extract the body of the encounter (between front-matter and `choices:`),
    /// stripped of pipeline draft comments.
    /// </summary>
    public static string ExtractEncounterBody(List<string> lines)
    {
        int start = 1;
        for (int i = 1; i < lines.Count; i++)
        {
            var t = lines[i].Trim();
            if (string.IsNullOrEmpty(t)) continue;
            if (t.StartsWith('[') && t.EndsWith(']')) { start = i + 1; continue; }
            if (t.StartsWith('#')) { start = i + 1; continue; }
            break;
        }
        int end = lines.Count;
        for (int i = start; i < lines.Count; i++)
        {
            if (lines[i].TrimEnd() == "choices:") { end = i; break; }
        }
        return string.Join("\n", lines.Skip(start).Take(end - start)
            .Where(l => !l.TrimStart().StartsWith('#')));
    }

    public static string Truncate(string s, int max) => s.Length > max ? s[..max] + "..." : s;
}
