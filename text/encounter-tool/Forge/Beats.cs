// Port of forge/enc.py — parse FIXME beats out of an .enc file.
//
// A *beat* is a FIXME line — a stub awaiting final prose. Everything else (titles,
// directives, choice lines, already-final narration) is enc structure the pipeline
// preserves verbatim and never rewrites. FIXME markers come in two forms:
//
//     FIXME: bare stub
//     FIXME(tone): toned stub      // tone is an optional lowercase qualifier
namespace Forge;

using System.Text.RegularExpressions;

public sealed record Beat(int Line, string Indent, string? Tone, string Original, string? Choice);

public static partial class Beats
{
    [GeneratedRegex(@"^(\s*)FIXME(?:\(([a-z]+)\))?:\s*(.+)$")]
    private static partial Regex FixmeRe();

    [GeneratedRegex(@"^\s*\*\s+(.+?)(?:\s*=\s*(.+?))?(?:\s*\[requires.*\])?\s*$")]
    private static partial Regex ChoiceRe();

    /// One beat per FIXME line, in file order. `Line` is the 0-based source line
    /// index (stable for re-integration, since the .enc is never rewritten), `Tone`
    /// is null for a bare FIXME, and `Choice` is the label of the enclosing
    /// `* choice` line (null in the scene preamble).
    public static List<Beat> Parse(string content)
    {
        var beats = new List<Beat>();
        string? choice = null;
        var lines = SplitLines(content);
        for (int n = 0; n < lines.Count; n++)
        {
            var raw = lines[n];
            var m = FixmeRe().Match(raw);
            if (m.Success)
            {
                beats.Add(new Beat(
                    Line: n,
                    Indent: m.Groups[1].Value,
                    Tone: m.Groups[2].Success ? m.Groups[2].Value : null,
                    Original: m.Groups[3].Value.Trim(),
                    Choice: choice));
                continue;
            }
            var c = ChoiceRe().Match(raw);
            if (c.Success && !raw.TrimStart().StartsWith("FIXME"))
            {
                var label = c.Groups[2].Success ? c.Groups[2].Value : c.Groups[1].Value;
                choice = label.Trim();
            }
        }
        return beats;
    }

    /// Mirror of Python str.splitlines() for the line boundaries that occur in .enc
    /// files (\n and \r\n): no trailing empty element when the text ends in a
    /// newline. parse and integrate MUST split identically — line indices are the
    /// splice key, and integrate rejoins with "\n".join(...) + "\n".
    public static List<string> SplitLines(string content)
    {
        var result = new List<string>();
        int i = 0, start = 0;
        while (i < content.Length)
        {
            char ch = content[i];
            if (ch == '\n')
            {
                result.Add(content[start..i]);
                i++;
                start = i;
            }
            else if (ch == '\r')
            {
                result.Add(content[start..i]);
                i++;
                if (i < content.Length && content[i] == '\n') i++;
                start = i;
            }
            else
            {
                i++;
            }
        }
        if (start < content.Length) result.Add(content[start..]);
        return result;
    }
}
