using System.Text;
using System.Text.RegularExpressions;

namespace EncounterCli;

/// <summary>
/// Transforms raw LLM encounter output into valid .enc format.
/// Converts SUCCESS:/FAILURE: labels to @check/@else blocks and humanizes
/// SCREAMING_SNAKE_CASE action labels on choice lines.
///
/// Does not touch prose content. Does not add game commands (+lines) — those
/// are authoring decisions made during review.
/// </summary>
static partial class EncounterPostProcessor
{
    /// <summary>Maps prompt action labels to encounter skill script names.</summary>
    static readonly Dictionary<string, string> ActionToSkill = new(StringComparer.OrdinalIgnoreCase)
    {
        ["PERSUADE"]         = "negotiation",
        ["INTIMIDATE"]       = "negotiation",
        ["FIGHT_ENEMY"]      = "combat",
        ["SNEAK"]            = "stealth",
        ["STUDY"]            = "perception",
        ["USE_BUSHCRAFT"]    = "bushcraft",
        ["PAY"]              = "mercantile",
        ["USE_STRENGTH"]     = "combat",
        ["CREATE_DIVERSION"] = "stealth",
        ["TAKE_OBJECT"]      = "stealth",
        ["DESTROY_OBJECT"]   = "combat",
        ["USE_TOOL"]         = "perception",
        ["ACCEPT"]           = "negotiation",
        ["REJECT"]           = "negotiation",
        ["IGNORE"]           = "luck",
    };

    [GeneratedRegex(@"^[A-Z][A-Z_ ]+$")]
    private static partial Regex AllCapsLabelPattern();

    /// <summary>
    /// Post-process LLM encounter output into valid .enc format.
    /// Scrubs preamble clutter, then converts SUCCESS/FAILURE to @check/@else.
    /// </summary>
    /// <param name="text">Raw LLM output (after code fence stripping).</param>
    /// <param name="difficulty">Default difficulty for generated @check blocks.</param>
    /// <returns>Transformed encounter text.</returns>
    public static string Process(string text, string difficulty = "medium")
    {
        // Phase 1: scrub LLM preamble clutter from the body
        text = ScrubPreamble(text);

        var lines = text.Split('\n');

        // Find the choices: line
        int choicesIdx = -1;
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].TrimEnd() == "choices:")
            {
                choicesIdx = i;
                break;
            }
        }

        if (choicesIdx < 0)
            return text; // No choices block — pass through unchanged

        // Everything up to and including choices: passes through
        var sb = new StringBuilder();
        for (int i = 0; i <= choicesIdx; i++)
        {
            sb.Append(lines[i]);
            sb.Append('\n');
        }

        // Parse the choices section into per-choice line groups
        var choiceBlocks = SplitIntoChoices(lines, choicesIdx + 1);

        foreach (var block in choiceBlocks)
        {
            sb.Append('\n');
            TransformChoice(block, difficulty, sb);
        }

        return sb.ToString().TrimEnd() + '\n';
    }

    /// <summary>
    /// Extract the encounter title from the first non-blank line of processed text.
    /// Call after <see cref="Process"/> so preamble clutter is already stripped.
    /// </summary>
    public static string? ExtractTitle(string processedText)
    {
        foreach (var line in processedText.Split('\n'))
        {
            if (!string.IsNullOrWhiteSpace(line))
                return line.Trim();
        }
        return null;
    }

    /// <summary>
    /// Remove LLM preamble clutter that doesn't belong in a .enc file:
    /// <c># Encounter:</c> prefix on the title, <c>## Selected Actions</c> /
    /// <c>**Selected Actions:**</c> blocks, <c>---</c> rules, and
    /// <c>## The Encounter</c> headers.
    /// </summary>
    static string ScrubPreamble(string text)
    {
        var lines = text.Split('\n');
        var result = new List<string>();
        bool titleSeen = false;
        bool inSelectedActions = false;

        for (int i = 0; i < lines.Length; i++)
        {
            var trimmed = lines[i].Trim();

            // Clean the title line (first non-blank line)
            if (!titleSeen && !string.IsNullOrWhiteSpace(trimmed))
            {
                titleSeen = true;
                if (trimmed.StartsWith("# Encounter:"))
                {
                    result.Add(trimmed["# Encounter:".Length..].TrimStart());
                    continue;
                }
                // Any other leading # (e.g. "# The Title") — strip the #
                if (trimmed.StartsWith("# "))
                {
                    result.Add(trimmed[2..].TrimStart());
                    continue;
                }
            }

            // Skip --- horizontal rules
            if (trimmed == "---")
                continue;

            // Skip "## Selected Actions" header (list form)
            if (trimmed == "## Selected Actions")
            {
                inSelectedActions = true;
                continue;
            }

            // Skip "**Selected Actions:**..." (inline form)
            if (trimmed.StartsWith("**Selected Actions:**"))
                continue;

            // While inside a Selected Actions list, skip list items and blanks
            if (inSelectedActions)
            {
                if (trimmed.StartsWith("- ") || string.IsNullOrWhiteSpace(trimmed))
                    continue;
                inSelectedActions = false;
            }

            // Skip "## The Encounter" section header
            if (trimmed == "## The Encounter")
                continue;

            result.Add(lines[i]);
        }

        // Collapse consecutive blank lines and trim leading/trailing blanks
        var collapsed = new List<string>();
        bool prevBlank = false;
        foreach (var line in result)
        {
            bool isBlank = string.IsNullOrWhiteSpace(line);
            if (isBlank && prevBlank) continue;
            collapsed.Add(line);
            prevBlank = isBlank;
        }

        while (collapsed.Count > 0 && string.IsNullOrWhiteSpace(collapsed[0]))
            collapsed.RemoveAt(0);
        while (collapsed.Count > 0 && string.IsNullOrWhiteSpace(collapsed[^1]))
            collapsed.RemoveAt(collapsed.Count - 1);

        return string.Join('\n', collapsed) + '\n';
    }

    /// <summary>Split lines after <c>choices:</c> into per-choice groups, each starting with <c>* </c>.</summary>
    static List<List<string>> SplitIntoChoices(string[] lines, int startIdx)
    {
        var choices = new List<List<string>>();
        List<string>? current = null;

        for (int i = startIdx; i < lines.Length; i++)
        {
            var trimmed = lines[i].TrimStart();
            if (trimmed.StartsWith("* "))
            {
                current = new List<string> { lines[i] };
                choices.Add(current);
            }
            else if (current != null)
            {
                current.Add(lines[i]);
            }
            // blank lines before the first choice are dropped
        }

        return choices;
    }

    /// <summary>Transform a single choice block and append to <paramref name="sb"/>.</summary>
    static void TransformChoice(List<string> block, string difficulty, StringBuilder sb)
    {
        if (block.Count == 0) return;

        // Parse the choice header: * LABEL = description  or  * Full text
        var headerLine = block[0];
        var (label, description) = ParseChoiceHeader(headerLine);

        // If the label is ALL CAPS (with spaces or underscores), humanize it and resolve skill
        string? skill = null;
        if (label != null && AllCapsLabelPattern().IsMatch(label))
        {
            skill = ResolveSkill(label);
            var humanized = HumanizeLabel(label);
            sb.Append(description != null ? $"* {humanized} = {description}\n" : $"* {humanized}\n");
        }
        else
        {
            sb.Append(headerLine.TrimStart());
            sb.Append('\n');
        }

        // Collect outcome lines (everything after the header)
        var outcomeLines = block.Skip(1).ToList();

        // Look for SUCCESS: and FAILURE: markers
        int successIdx = -1, failureIdx = -1;
        for (int i = 0; i < outcomeLines.Count; i++)
        {
            var t = outcomeLines[i].Trim();
            if (t == "SUCCESS:" && successIdx < 0) successIdx = i;
            else if (t == "FAILURE:" && failureIdx < 0) failureIdx = i;
        }

        if (successIdx >= 0 && failureIdx > successIdx)
        {
            // Branched outcome: pre-split prose + SUCCESS/FAILURE branches
            var preSplit = outcomeLines.Take(successIdx).ToList();
            var successLines = outcomeLines.Skip(successIdx + 1).Take(failureIdx - successIdx - 1).ToList();
            var failureLines = outcomeLines.Skip(failureIdx + 1).ToList();

            // Pre-split prose (shared text shown before the branch)
            foreach (var line in TrimBlankEnds(preSplit))
                sb.Append($"  {line}\n");

            // @check block
            var skillName = !string.IsNullOrEmpty(skill) ? skill : "luck";
            sb.Append($"  @check {skillName} {difficulty} {{\n");

            foreach (var line in TrimBlankEnds(successLines))
                sb.Append(string.IsNullOrWhiteSpace(line) ? "\n" : $"    {line}\n");

            sb.Append($"  }} @else {{\n");

            foreach (var line in TrimBlankEnds(failureLines))
                sb.Append(string.IsNullOrWhiteSpace(line) ? "\n" : $"    {line}\n");

            sb.Append("  }\n");
        }
        else
        {
            // Single outcome — re-indent prose at 2 spaces
            foreach (var line in TrimBlankEnds(outcomeLines))
                sb.Append(string.IsNullOrWhiteSpace(line) ? "\n" : $"  {line}\n");
        }
    }

    /// <summary>Parse <c>* LABEL = description</c> or <c>* Full text</c>.</summary>
    static (string? label, string? description) ParseChoiceHeader(string line)
    {
        var trimmed = line.TrimStart();
        if (!trimmed.StartsWith("* "))
            return (null, null);

        var content = trimmed[2..];
        var eqIdx = content.IndexOf(" = ");
        if (eqIdx > 0)
        {
            return (content[..eqIdx].Trim(), content[(eqIdx + 3)..].Trim());
        }
        return (content.Trim(), null);
    }

    /// <summary>
    /// Resolve the encounter skill from an ALL-CAPS action label.
    /// Handles exact matches (<c>PERSUADE</c>), underscore variants (<c>FIGHT_ENEMY</c>),
    /// space variants (<c>FIGHT ENEMY</c>), and labels with extra words
    /// (<c>PERSUADE THE GROUP</c> matches via the <c>PERSUADE</c> prefix).
    /// </summary>
    static string? ResolveSkill(string label)
    {
        // Normalize spaces to underscores for dictionary lookup
        var normalized = label.Replace(' ', '_');

        // Exact match
        if (ActionToSkill.TryGetValue(normalized, out var skill))
            return skill;

        // Prefix match: try first two words, then first word
        var words = normalized.Split('_', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length >= 2)
        {
            var twoWord = words[0] + "_" + words[1];
            if (ActionToSkill.TryGetValue(twoWord, out skill))
                return skill;
        }
        if (words.Length >= 1 && ActionToSkill.TryGetValue(words[0], out skill))
            return skill;

        return null;
    }

    /// <summary>Convert <c>FIGHT_ENEMY</c> or <c>FIGHT ENEMY</c> to <c>Fight enemy</c>.</summary>
    static string HumanizeLabel(string label)
    {
        var words = label.Split(new[] { '_', ' ' }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < words.Length; i++)
        {
            if (words[i].Length == 0) continue;
            words[i] = i == 0
                ? char.ToUpper(words[i][0]) + words[i][1..].ToLower()
                : words[i].ToLower();
        }
        return string.Join(' ', words);
    }

    /// <summary>
    /// Strip existing indentation from each line, then trim leading and trailing blank lines.
    /// Internal blank lines (paragraph breaks) are preserved.
    /// </summary>
    static List<string> TrimBlankEnds(List<string> lines)
    {
        var stripped = lines.Select(l => l.TrimStart()).ToList();

        while (stripped.Count > 0 && string.IsNullOrWhiteSpace(stripped[0]))
            stripped.RemoveAt(0);

        while (stripped.Count > 0 && string.IsNullOrWhiteSpace(stripped[^1]))
            stripped.RemoveAt(stripped.Count - 1);

        return stripped;
    }
}
