namespace Dreamlands.Encounter;

/// <summary>
/// Parses encounter files using the token-driven format:
/// <c>* </c> for choices, <c>@check</c>/<c>@else</c> for flow control,
/// <c>+verb</c> for commands, <c>{ }</c> for blocks, bare text for prose.
/// </summary>
public static class EncounterParser
{
    private const string ChoicesMarker = "choices:";

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

    private static IReadOnlyList<Choice> ParseChoices(string[] lines, int start, List<ParseError> errors)
    {
        var choices = new List<Choice>();

        // State
        string? currentOptionText = null;
        int currentOptionLine = 0;
        string? conditionAction = null;
        var successText = new List<string>();
        var successMechanics = new List<string>();
        var failureText = new List<string>();
        var failureMechanics = new List<string>();
        var singleText = new List<string>();
        var singleMechanics = new List<string>();
        bool inBranch = false;
        bool inFailure = false;
        int braceDepth = 0;

        void FinishChoice()
        {
            if (currentOptionText == null) return;
            ParseOptionLinkPreview(currentOptionText, out var link, out var preview);

            if (conditionAction != null)
            {
                choices.Add(new Choice
                {
                    OptionText = currentOptionText,
                    OptionLink = link,
                    OptionPreview = preview,
                    Branched = new BranchedOutcome
                    {
                        ConditionAction = conditionAction,
                        Success = new OutcomePart { Text = JoinProse(successText), Mechanics = successMechanics.ToList() },
                        Failure = new OutcomePart { Text = JoinProse(failureText), Mechanics = failureMechanics.ToList() }
                    }
                });
            }
            else
            {
                choices.Add(new Choice
                {
                    OptionText = currentOptionText,
                    OptionLink = link,
                    OptionPreview = preview,
                    Single = new SingleOutcome
                    {
                        Part = new OutcomePart { Text = JoinProse(singleText), Mechanics = singleMechanics.ToList() }
                    }
                });
            }

            // Reset
            currentOptionText = null;
            conditionAction = null;
            successText.Clear(); successMechanics.Clear();
            failureText.Clear(); failureMechanics.Clear();
            singleText.Clear(); singleMechanics.Clear();
            inBranch = false;
            inFailure = false;
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

            // Handle closing brace: "}" or "} @else {"
            if (trimmed == "}" || trimmed.StartsWith("}", StringComparison.Ordinal))
            {
                if (braceDepth == 0)
                {
                    errors.Add(new ParseError { Line = lineNum, Message = "Unexpected '}' outside a block." });
                    continue;
                }

                // Check for "} @else {"
                var afterBrace = trimmed[1..].Trim();
                if (afterBrace.StartsWith("@else", StringComparison.Ordinal))
                {
                    var rest = afterBrace[5..].Trim();
                    if (rest == "{")
                    {
                        // Switch to failure branch (brace depth stays at 1)
                        inFailure = true;
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
                    inBranch = false;
                continue;
            }

            // Handle "@else {" on its own line (after "}" was on the previous line)
            if (braceDepth == 0 && conditionAction != null && !inFailure &&
                trimmed.StartsWith("@else", StringComparison.Ordinal))
            {
                var rest = trimmed[5..].Trim();
                if (rest == "{")
                {
                    braceDepth++;
                    inBranch = true;
                    inFailure = true;
                    continue;
                }
                else
                {
                    errors.Add(new ParseError { Line = lineNum, Message = "Expected '{' after '@else'." });
                    continue;
                }
            }

            // Handle "@check skill difficulty {"
            if (trimmed.StartsWith("@check ", StringComparison.Ordinal))
            {
                if (conditionAction != null)
                {
                    errors.Add(new ParseError { Line = lineNum, Message = "Multiple @check blocks in one choice." });
                    continue;
                }

                var content = trimmed[1..]; // strip '@', keep "check skill difficulty {"
                if (!content.EndsWith("{"))
                {
                    errors.Add(new ParseError { Line = lineNum, Message = "@check line must end with '{'." });
                    continue;
                }

                conditionAction = content[..^1].Trim(); // "check skill difficulty"
                braceDepth++;
                inBranch = true;
                inFailure = false;
                continue;
            }

            // Handle "+verb args" — game command
            if (trimmed.Length >= 2 && trimmed[0] == '+' && char.IsLetter(trimmed[1]))
            {
                var command = trimmed[1..]; // strip '+'
                if (inBranch)
                {
                    if (inFailure)
                        failureMechanics.Add(command);
                    else
                        successMechanics.Add(command);
                }
                else
                {
                    singleMechanics.Add(command);
                }
                continue;
            }

            // Handle open brace on its own (shouldn't appear outside @check/@else)
            if (trimmed == "{")
            {
                errors.Add(new ParseError { Line = lineNum, Message = "Unexpected '{' without @check or @else." });
                braceDepth++;
                continue;
            }

            // Everything else is prose
            if (inBranch)
            {
                if (inFailure)
                    failureText.Add(raw);
                else
                    successText.Add(raw);
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
