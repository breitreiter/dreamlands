using System.Text.RegularExpressions;

namespace Dreamlands.Encounter;

/// <summary>
/// Parses encounter files using the token-driven format:
/// <c>* </c> for choices, <c>@if</c>/<c>@elif</c>/<c>@else</c> for flow control,
/// <c>+verb</c> for commands, <c>{ }</c> for blocks, bare text for prose.
/// </summary>
public static partial class EncounterParser
{
    private const string ChoicesMarker = "choices:";

    [GeneratedRegex(@"\[requires\s+(.+?)\]\s*$")]
    private static partial Regex RequiresPattern();

    /// <summary>Parse encounter source text. Returns a result with either a valid encounter or errors.</summary>
    public static ParseResult Parse(string source)
    {
        var errors = new List<ParseError>();
        source = source.Replace("\r\n", "\n").Replace("\r", "\n");
        var lines = source.Split('\n');
        if (lines.Length == 0)
        {
            errors.Add(new ParseError { Message = "File is empty." });
            return new ParseResult { Errors = errors };
        }

        // Title: first line
        var title = lines[0].Trim();

        // Body: from line 2 until a line that starts with "choices:" at column 0
        int choicesLineIndex = -1;
        for (int i = 1; i < lines.Length; i++)
        {
            if (lines[i].TrimEnd() == ChoicesMarker || lines[i].StartsWith(ChoicesMarker, StringComparison.Ordinal))
            {
                choicesLineIndex = i;
                break;
            }
        }

        if (choicesLineIndex < 0)
        {
            errors.Add(new ParseError { Message = "Missing 'choices:' delimiter." });
            return new ParseResult { Encounter = new Encounter { Title = title, Body = JoinBody(lines, 1, lines.Length) }, Errors = errors };
        }

        var body = JoinBody(lines, 1, choicesLineIndex);

        // Parse choices block using token-driven state machine
        var choices = ParseChoices(lines, choicesLineIndex + 1, errors);

        return new ParseResult
        {
            Encounter = new Encounter { Title = title, Body = body, Choices = choices },
            Errors = errors
        };
    }

    private static string JoinBody(string[] lines, int start, int end)
    {
        var bodyLines = new List<string>();
        for (int i = start; i < end; i++)
            bodyLines.Add(lines[i]);
        return string.Join("\n", bodyLines);
    }

    /// <summary>Strip trailing [requires ...] from option text. Returns cleaned text and condition string (or null).</summary>
    private static (string text, string? requires) StripRequires(string optionText)
    {
        var match = RequiresPattern().Match(optionText);
        if (!match.Success)
            return (optionText, null);
        var cleaned = optionText[..match.Index].TrimEnd();
        return (cleaned, match.Groups[1].Value.Trim());
    }

    private static IReadOnlyList<Choice> ParseChoices(string[] lines, int start, List<ParseError> errors)
    {
        var choices = new List<Choice>();

        // State
        string? currentOptionText = null;
        int currentOptionLine = 0;
        var branches = new List<ConditionalBranch>();
        string? currentCondition = null;
        var branchText = new List<string>();
        var branchMechanics = new List<string>();
        var fallbackText = new List<string>();
        var fallbackMechanics = new List<string>();
        var singleText = new List<string>();
        var singleMechanics = new List<string>();
        bool inConditional = false;
        bool inFallback = false;
        int braceDepth = 0;

        void PushBranch()
        {
            if (currentCondition != null)
            {
                branches.Add(new ConditionalBranch
                {
                    Condition = currentCondition,
                    Outcome = new OutcomePart { Text = JoinProse(branchText), Mechanics = branchMechanics.ToList() }
                });
                currentCondition = null;
                branchText.Clear();
                branchMechanics.Clear();
            }
        }

        void FinishChoice()
        {
            if (currentOptionText == null) return;

            var (cleanedText, requires) = StripRequires(currentOptionText);
            ParseOptionLinkPreview(cleanedText, out var link, out var preview);

            if (branches.Count > 0 || currentCondition != null)
            {
                // Finish any in-progress branch
                PushBranch();

                OutcomePart? fallback = null;
                if (inFallback || fallbackText.Count > 0 || fallbackMechanics.Count > 0)
                    fallback = new OutcomePart { Text = JoinProse(fallbackText), Mechanics = fallbackMechanics.ToList() };

                choices.Add(new Choice
                {
                    OptionText = cleanedText,
                    OptionLink = link,
                    OptionPreview = preview,
                    Requires = requires,
                    Conditional = new ConditionalOutcome
                    {
                        Preamble = JoinProse(singleText),
                        Branches = branches.ToList(),
                        Fallback = fallback
                    }
                });
            }
            else
            {
                choices.Add(new Choice
                {
                    OptionText = cleanedText,
                    OptionLink = link,
                    OptionPreview = preview,
                    Requires = requires,
                    Single = new SingleOutcome
                    {
                        Part = new OutcomePart { Text = JoinProse(singleText), Mechanics = singleMechanics.ToList() }
                    }
                });
            }

            // Reset
            currentOptionText = null;
            currentCondition = null;
            branches.Clear();
            branchText.Clear(); branchMechanics.Clear();
            fallbackText.Clear(); fallbackMechanics.Clear();
            singleText.Clear(); singleMechanics.Clear();
            inConditional = false;
            inFallback = false;
            braceDepth = 0;
        }

        for (int i = start; i < lines.Length; i++)
        {
            var lineNum = i + 1;
            var raw = lines[i];
            var trimmed = raw.TrimStart();

            // Skip blank lines
            if (string.IsNullOrWhiteSpace(trimmed))
                continue;

            // Choice boundary: "* " at start (only outside braces)
            if (braceDepth == 0 && trimmed.StartsWith("* ", StringComparison.Ordinal))
            {
                FinishChoice();
                currentOptionText = trimmed[2..].Trim();
                currentOptionLine = lineNum;
                continue;
            }

            // No current choice — stray content
            if (currentOptionText == null)
            {
                errors.Add(new ParseError { Line = lineNum, Message = "Content before first choice (expected '* ')." });
                continue;
            }

            // Handle closing brace: "}" or "} @else {" or "} @elif condition {"
            if (trimmed == "}" || trimmed.StartsWith("}", StringComparison.Ordinal))
            {
                if (braceDepth == 0)
                {
                    errors.Add(new ParseError { Line = lineNum, Message = "Unexpected '}' outside a block." });
                    continue;
                }

                var afterBrace = trimmed[1..].Trim();

                // "} @elif condition {"
                if (afterBrace.StartsWith("@elif", StringComparison.Ordinal))
                {
                    var rest = afterBrace[5..].Trim();
                    if (!rest.EndsWith("{"))
                    {
                        errors.Add(new ParseError { Line = lineNum, Message = "@elif line must end with '{'." });
                        continue;
                    }

                    // Close the current branch, start a new one
                    PushBranch();
                    currentCondition = rest[..^1].Trim();
                    // braceDepth stays at 1
                    continue;
                }

                // "} @else {"
                if (afterBrace.StartsWith("@else", StringComparison.Ordinal))
                {
                    var rest = afterBrace[5..].Trim();
                    if (rest == "{")
                    {
                        PushBranch();
                        inFallback = true;
                        continue;
                    }
                    else
                    {
                        errors.Add(new ParseError { Line = lineNum, Message = "Expected '{' after '@else'." });
                        continue;
                    }
                }

                // Plain "}" — close block
                braceDepth--;
                if (braceDepth == 0)
                {
                    if (!inFallback)
                        PushBranch();
                    inConditional = false;
                }
                continue;
            }

            // Handle "@else {" on its own line (after "}" was on the previous line)
            if (braceDepth == 0 && branches.Count > 0 && !inFallback &&
                trimmed.StartsWith("@else", StringComparison.Ordinal))
            {
                var rest = trimmed[5..].Trim();
                if (rest == "{")
                {
                    braceDepth++;
                    inConditional = true;
                    inFallback = true;
                    continue;
                }
                else
                {
                    errors.Add(new ParseError { Line = lineNum, Message = "Expected '{' after '@else'." });
                    continue;
                }
            }

            // Handle "@elif condition {" on its own line (after "}" was on the previous line)
            if (braceDepth == 0 && branches.Count > 0 && !inFallback &&
                trimmed.StartsWith("@elif", StringComparison.Ordinal))
            {
                var rest = trimmed[5..].Trim();
                if (!rest.EndsWith("{"))
                {
                    errors.Add(new ParseError { Line = lineNum, Message = "@elif line must end with '{'." });
                    continue;
                }
                currentCondition = rest[..^1].Trim();
                braceDepth++;
                inConditional = true;
                continue;
            }

            // Handle "@if condition {"
            if (trimmed.StartsWith("@if ", StringComparison.Ordinal))
            {
                if (branches.Count > 0 || currentCondition != null)
                {
                    errors.Add(new ParseError { Line = lineNum, Message = "Multiple @if blocks in one choice." });
                    continue;
                }

                var content = trimmed[4..]; // strip "@if "
                if (!content.EndsWith("{"))
                {
                    errors.Add(new ParseError { Line = lineNum, Message = "@if line must end with '{'." });
                    continue;
                }

                currentCondition = content[..^1].Trim();
                braceDepth++;
                inConditional = true;
                inFallback = false;
                continue;
            }

            // Explicit error for old @check syntax
            if (trimmed.StartsWith("@check ", StringComparison.Ordinal))
            {
                errors.Add(new ParseError { Line = lineNum, Message = "@check is no longer supported. Use '@if check <skill> <difficulty> {' instead." });
                continue;
            }

            // Handle "+verb args" — game command
            if (trimmed.Length >= 2 && trimmed[0] == '+' && char.IsLetter(trimmed[1]))
            {
                var command = trimmed[1..]; // strip '+'
                if (inConditional)
                {
                    if (inFallback)
                        fallbackMechanics.Add(command);
                    else
                        branchMechanics.Add(command);
                }
                else
                {
                    singleMechanics.Add(command);
                }
                continue;
            }

            // Handle open brace on its own (shouldn't appear outside @if/@elif/@else)
            if (trimmed == "{")
            {
                errors.Add(new ParseError { Line = lineNum, Message = "Unexpected '{' without @if or @else." });
                braceDepth++;
                continue;
            }

            // Everything else is prose
            if (inConditional)
            {
                if (inFallback)
                    fallbackText.Add(raw);
                else
                    branchText.Add(raw);
            }
            else
            {
                singleText.Add(raw);
            }
        }

        // Finish the last choice
        if (braceDepth > 0)
            errors.Add(new ParseError { Line = lines.Length, Message = "Unclosed '{' — missing '}'." });
        FinishChoice();

        return choices;
    }

    private static void ParseOptionLinkPreview(string optionText, out string? optionLink, out string? optionPreview)
    {
        var eq = optionText.IndexOf('=');
        if (eq < 0)
        {
            optionLink = null;
            optionPreview = null;
            return;
        }
        optionLink = optionText[..eq].Trim();
        optionPreview = optionText[(eq + 1)..].Trim();
        if (string.IsNullOrEmpty(optionLink)) optionLink = null;
        if (string.IsNullOrEmpty(optionPreview)) optionPreview = null;
    }

    private static string JoinProse(List<string> lines)
    {
        // Trim leading/trailing blank lines, preserve internal structure
        while (lines.Count > 0 && string.IsNullOrWhiteSpace(lines[0]))
            lines.RemoveAt(0);
        while (lines.Count > 0 && string.IsNullOrWhiteSpace(lines[^1]))
            lines.RemoveAt(lines.Count - 1);
        return string.Join("\n", lines).Trim();
    }
}
