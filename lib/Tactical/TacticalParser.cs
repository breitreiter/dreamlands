using System.Text.RegularExpressions;

namespace Dreamlands.Tactical;

public static partial class TacticalParser
{
    enum Section { None, Timers, Openings, Approaches, Failure, Success, Branches }

    [GeneratedRegex(@"^\[(\w+)\s+(.+?)\]\s*$")]
    private static partial Regex FrontMatterPattern();

    // * Timer Name: spirits|resistance N every N [resist N]
    [GeneratedRegex(@"^\*\s+(.+?):\s+(spirits|resistance)\s+(\d+)\s+every\s+(\d+)(?:\s+resist\s+(\d+))?\s*$")]
    private static partial Regex TimerPattern();

    // * Timer Name: condition <id> every N [resist N]
    [GeneratedRegex(@"^\*\s+(.+?):\s+condition\s+(\w+)\s+every\s+(\d+)(?:\s+resist\s+(\d+))?\s*$")]
    private static partial Regex ConditionTimerPattern();

    // * Timer Name: fatal every N [resist N]
    [GeneratedRegex(@"^\*\s+(.+?):\s+fatal\s+every\s+(\d+)(?:\s+resist\s+(\d+))?\s*$")]
    private static partial Regex FatalTimerPattern();

    // * Timer Name: tick "Target Name" N every N [resist N]
    [GeneratedRegex(@"^\*\s+(.+?):\s+tick\s+""(.+?)""\s+(\d+)\s+every\s+(\d+)(?:\s+resist\s+(\d+))?\s*$")]
    private static partial Regex TickTimerPattern();

    [GeneratedRegex(@"\[counter\s+(.+?)\]")]
    private static partial Regex CounterTag();

    // * Opening Name: archetype_id [requires ...]
    [GeneratedRegex(@"^\*\s+(.+?):\s+(\w+)\s*(.*)$")]
    private static partial Regex OpeningPattern();

    // * aggressive or * cautious
    [GeneratedRegex(@"^\*\s+(aggressive|cautious)\s*$")]
    private static partial Regex ApproachPattern();

    // * Label -> encounter_ref [requires condition]
    [GeneratedRegex(@"^\*\s+(.+?)\s*->\s*(.+?)\s*$")]
    private static partial Regex BranchPattern();

    // [requires condition] extractor
    [GeneratedRegex(@"\[requires\s+(.+?)\]")]
    private static partial Regex RequiresTag();

    // section marker: word followed by colon at column 0
    [GeneratedRegex(@"^(\w+):\s*$")]
    private static partial Regex SectionMarker();

    public static TacticalParseResult Parse(string source)
    {
        var errors = new List<ParseError>();
        source = source.Replace("\r\n", "\n").Replace("\r", "\n");
        var lines = source.Split('\n');

        if (lines.Length == 0 || string.IsNullOrWhiteSpace(lines[0]))
        {
            errors.Add(new ParseError(null, "File is empty."));
            return new TacticalParseResult { Errors = errors };
        }

        // Title: line 0
        var title = lines[0].Trim();

        // Front-matter
        var requires = new List<string>();
        string? stat = null;
        int? tier = null;
        int bodyStart = 1;

        for (int i = 1; i < lines.Length; i++)
        {
            var trimmed = lines[i].Trim();
            if (string.IsNullOrEmpty(trimmed)) continue;

            var fm = FrontMatterPattern().Match(trimmed);
            if (!fm.Success) { bodyStart = i; break; }

            var key = fm.Groups[1].Value;
            var value = fm.Groups[2].Value.Trim();
            switch (key)
            {
                case "variant":
                    break; // Ignored — kept for backwards compatibility with existing .tac files
                case "stat":
                    stat = value;
                    break;
                case "tier":
                    if (!int.TryParse(value, out var t) || t < 1 || t > 3)
                        errors.Add(new ParseError(i + 1, $"Invalid tier '{value}'. Must be 1, 2, or 3."));
                    else
                        tier = t;
                    break;
                case "requires":
                    requires.Add(value);
                    break;
                default:
                    errors.Add(new ParseError(i + 1, $"Unknown front-matter key '{key}'."));
                    break;
            }
            bodyStart = i + 1;
        }

        // Body: prose until first section marker
        var bodyLines = new List<string>();
        int sectionStart = lines.Length;
        for (int i = bodyStart; i < lines.Length; i++)
        {
            if (SectionMarker().IsMatch(lines[i].Trim()))
            {
                sectionStart = i;
                break;
            }
            bodyLines.Add(lines[i]);
        }
        var body = JoinProse(bodyLines);

        // Parse sections
        var sections = new Dictionary<Section, (int start, int end)>();
        Section? currentSection = null;
        int currentSectionStart = 0;

        for (int i = sectionStart; i < lines.Length; i++)
        {
            var sm = SectionMarker().Match(lines[i].Trim());
            if (sm.Success)
            {
                if (currentSection != null)
                    sections[currentSection.Value] = (currentSectionStart, i);

                var name = sm.Groups[1].Value;
                var section = name switch
                {
                    "timers" => Section.Timers,
                    "openings" => Section.Openings,
                    "path" => Section.None, // Ignored — removed feature, kept for backwards compat
                    "approaches" => Section.Approaches,
                    "failure" => Section.Failure,
                    "success" => Section.Success,
                    "branches" => Section.Branches,
                    _ => Section.None,
                };

                if (section == Section.None && name != "path")
                    errors.Add(new ParseError(i + 1, $"Unknown section '{name}'."));
                else if (section != Section.None && sections.ContainsKey(section))
                    errors.Add(new ParseError(i + 1, $"Duplicate section '{name}'."));

                currentSection = section == Section.None ? null : section;
                currentSectionStart = i + 1;
            }
        }
        if (currentSection != null)
            sections[currentSection.Value] = (currentSectionStart, lines.Length);

        // Detect encounter vs group
        bool isGroup = sections.ContainsKey(Section.Branches);
        bool isEncounter = sections.ContainsKey(Section.Openings)
                        || sections.ContainsKey(Section.Timers) || sections.ContainsKey(Section.Approaches)
                        || sections.ContainsKey(Section.Failure) || sections.ContainsKey(Section.Success);

        if (isGroup && isEncounter)
        {
            errors.Add(new ParseError(null, "File has both 'branches:' and encounter sections. A .tac file must be either an encounter or a group."));
            return new TacticalParseResult { Errors = errors };
        }

        if (!isGroup && !isEncounter)
        {
            errors.Add(new ParseError(null, "No sections found. Expected openings:/failure: (encounter) or branches: (group)."));
            return new TacticalParseResult { Errors = errors };
        }

        if (isGroup)
            return ParseGroup(lines, title, body, tier, requires, sections, errors);

        return ParseEncounter(lines, title, body, stat, tier, requires, sections, errors);
    }

    static TacticalParseResult ParseEncounter(
        string[] lines, string title, string body,
        string? stat, int? tier,
        List<string> requires, Dictionary<Section, (int start, int end)> sections,
        List<ParseError> errors)
    {
        if (!sections.ContainsKey(Section.Openings))
            errors.Add(new ParseError(null, "Missing required section 'openings:'."));
        if (!sections.ContainsKey(Section.Failure))
            errors.Add(new ParseError(null, "Missing required section 'failure:'."));

        // Timers
        var timers = new List<TimerDef>();
        if (sections.TryGetValue(Section.Timers, out var timerRange))
            ParseTimers(lines, timerRange.start, timerRange.end, timers, errors);

        // Openings
        var openings = new List<OpeningDef>();
        if (sections.TryGetValue(Section.Openings, out var openingRange))
            ParseOpenings(lines, openingRange.start, openingRange.end, openings, errors);

        // Approaches
        var approaches = new List<ApproachDef>();
        if (sections.TryGetValue(Section.Approaches, out var approachRange))
            ParseApproaches(lines, approachRange.start, approachRange.end, approaches, errors);

        if (approaches.Count == 0 && sections.ContainsKey(Section.Approaches))
            errors.Add(new ParseError(null, "Approaches section is empty."));

        // Failure
        FailureOutcome? failure = null;
        if (sections.TryGetValue(Section.Failure, out var failureRange))
            failure = ParseFailure(lines, failureRange.start, failureRange.end, errors);

        // Success (optional epilogue + mechanics on victory)
        SuccessOutcome? success = null;
        if (sections.TryGetValue(Section.Success, out var successRange))
            success = ParseSuccess(lines, successRange.start, successRange.end, errors);

        if (errors.Count > 0)
            return new TacticalParseResult { Errors = errors };

        return new TacticalParseResult
        {
            Encounter = new TacticalEncounter
            {
                Title = title,
                Body = body,
                Stat = stat,
                Tier = tier,
                Requires = requires,
                Timers = timers,
                Openings = openings,
                Approaches = approaches,
                Failure = failure,
                Success = success,
            }
        };
    }

    static TacticalParseResult ParseGroup(
        string[] lines, string title, string body,
        int? tier, List<string> requires,
        Dictionary<Section, (int start, int end)> sections,
        List<ParseError> errors)
    {
        var branches = new List<BranchDef>();
        if (sections.TryGetValue(Section.Branches, out var branchRange))
            ParseBranches(lines, branchRange.start, branchRange.end, branches, errors);

        if (branches.Count == 0)
            errors.Add(new ParseError(null, "Branches section is empty."));

        if (errors.Count > 0)
            return new TacticalParseResult { Errors = errors };

        return new TacticalParseResult
        {
            Group = new TacticalGroup
            {
                Title = title,
                Body = body,
                Tier = tier,
                Requires = requires,
                Branches = branches,
            }
        };
    }

    static void ParseTimers(string[] lines, int start, int end,
        List<TimerDef> timers, List<ParseError> errors)
    {
        for (int i = start; i < end; i++)
        {
            var trimmed = lines[i].Trim();
            if (string.IsNullOrEmpty(trimmed)) continue;

            if (!trimmed.StartsWith("* "))
            {
                errors.Add(new ParseError(i + 1, $"Expected '* Timer Name: ...', got '{trimmed}'."));
                continue;
            }

            // Try each timer pattern in order

            // Fatal timer: * Name: fatal every N [resist N]
            var fm = FatalTimerPattern().Match(trimmed);
            if (fm.Success)
            {
                var name = ExtractNameAndCounter(fm.Groups[1].Value, out var counter);
                var countdown = int.Parse(fm.Groups[2].Value);
                var resistance = fm.Groups[3].Success ? int.Parse(fm.Groups[3].Value) : 0;
                timers.Add(new TimerDef(name, TimerEffect.Fatal, 0, countdown, resistance, counter));
                continue;
            }

            // Tick timer: * Name: tick "Target" N every N [resist N]
            var tm = TickTimerPattern().Match(trimmed);
            if (tm.Success)
            {
                var name = ExtractNameAndCounter(tm.Groups[1].Value, out var counter);
                var target = tm.Groups[2].Value;
                var amount = int.Parse(tm.Groups[3].Value);
                var countdown = int.Parse(tm.Groups[4].Value);
                var resistance = tm.Groups[5].Success ? int.Parse(tm.Groups[5].Value) : 0;
                timers.Add(new TimerDef(name, TimerEffect.TickTimer, amount, countdown, resistance, counter, TicksTimerName: target));
                continue;
            }

            // Condition timer: * Name: condition <id> every N [resist N]
            var cm = ConditionTimerPattern().Match(trimmed);
            if (cm.Success)
            {
                var name = ExtractNameAndCounter(cm.Groups[1].Value, out var counter);
                var conditionId = cm.Groups[2].Value;
                var countdown = int.Parse(cm.Groups[3].Value);
                var resistance = cm.Groups[4].Success ? int.Parse(cm.Groups[4].Value) : 0;

                if (!KnownConditions.Contains(conditionId))
                    errors.Add(new ParseError(i + 1, $"Unknown condition '{conditionId}'."));
                else
                    timers.Add(new TimerDef(name, TimerEffect.Condition, 0, countdown, resistance, counter, conditionId));
                continue;
            }

            // Standard timer: * Name: spirits|resistance N every N [resist N]
            var m = TimerPattern().Match(trimmed);
            if (m.Success)
            {
                var name = ExtractNameAndCounter(m.Groups[1].Value, out var counter);
                var effect = m.Groups[2].Value == "spirits" ? TimerEffect.Spirits : TimerEffect.Resistance;
                var amount = int.Parse(m.Groups[3].Value);
                var countdown = int.Parse(m.Groups[4].Value);
                var resistance = m.Groups[5].Success ? int.Parse(m.Groups[5].Value) : 0;
                timers.Add(new TimerDef(name, effect, amount, countdown, resistance, counter));
                continue;
            }

            errors.Add(new ParseError(i + 1, "Invalid timer format. Expected '* Name: spirits|resistance N every N [resist N]', '* Name: condition <id> every N [resist N]', '* Name: fatal every N [resist N]', or '* Name: tick \"Target\" N every N [resist N]'."));
        }
    }

    static string ExtractNameAndCounter(string raw, out string? counterName)
    {
        var name = raw.Trim();
        counterName = null;
        var match = CounterTag().Match(name);
        if (match.Success)
        {
            counterName = match.Groups[1].Value.Trim();
            name = name[..match.Index].Trim();
        }
        return name;
    }

    /// <summary>Known condition IDs for validation. Must match ConditionDef.All keys.</summary>
    static readonly HashSet<string> KnownConditions =
    [
        "freezing", "thirsty", "irradiated", "lattice_sickness",
        "exhausted", "poisoned", "lost", "injured",
    ];

    /// <summary>Known archetype IDs for validation. Must match TacticalBalance.Archetypes keys.</summary>
    static readonly HashSet<string> KnownArchetypes =
    [
        "free_progress_small", "momentum_to_progress", "momentum_to_progress_large",
        "momentum_to_progress_huge", "spirits_to_progress", "spirits_to_progress_large",
        "threat_to_progress", "threat_to_progress_large",
        "free_momentum_small", "free_momentum", "threat_to_momentum", "spirits_to_momentum",
        "momentum_to_cancel", "spirits_to_cancel", "free_cancel",
    ];

    static void ParseOpenings(string[] lines, int start, int end,
        List<OpeningDef> openings, List<ParseError> errors)
    {
        for (int i = start; i < end; i++)
        {
            var trimmed = lines[i].Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith('#')) continue;

            if (!trimmed.StartsWith("* "))
            {
                errors.Add(new ParseError(i + 1, $"Expected '* Opening Name: ...' in openings section."));
                continue;
            }

            var m = OpeningPattern().Match(trimmed);
            if (!m.Success)
            {
                errors.Add(new ParseError(i + 1, $"Invalid opening format. Expected '* Name: archetype_id'."));
                continue;
            }

            var name = m.Groups[1].Value.Trim();
            var archetype = m.Groups[2].Value.Trim();
            var trailing = m.Groups[3].Value.Trim();

            // Extract [requires ...] from trailing text
            string? req = null;
            if (!string.IsNullOrEmpty(trailing))
            {
                var reqMatch = RequiresTag().Match(trailing);
                if (reqMatch.Success)
                    req = reqMatch.Groups[1].Value.Trim();
                else
                    errors.Add(new ParseError(i + 1, $"Unexpected text after archetype: '{trailing}'."));
            }

            if (!KnownArchetypes.Contains(archetype))
                errors.Add(new ParseError(i + 1, $"Unknown archetype '{archetype}'."));
            else
                openings.Add(new OpeningDef(name, archetype, req));
        }
    }

    static void ParseApproaches(string[] lines, int start, int end,
        List<ApproachDef> approaches, List<ParseError> errors)
    {
        for (int i = start; i < end; i++)
        {
            var trimmed = lines[i].Trim();
            if (string.IsNullOrEmpty(trimmed)) continue;

            if (!trimmed.StartsWith("* "))
            {
                errors.Add(new ParseError(i + 1, $"Expected '* aggressive|cautious' in approaches section."));
                continue;
            }

            var m = ApproachPattern().Match(trimmed);
            if (!m.Success)
            {
                errors.Add(new ParseError(i + 1, $"Invalid approach format. Expected '* aggressive' or '* cautious'."));
                continue;
            }

            if (!Enum.TryParse<ApproachKind>(m.Groups[1].Value, ignoreCase: true, out var kind))
            {
                errors.Add(new ParseError(i + 1, $"Invalid approach kind '{m.Groups[1].Value}'."));
                continue;
            }

            approaches.Add(new ApproachDef(kind));
        }
    }

    static FailureOutcome ParseFailure(string[] lines, int start, int end, List<ParseError> errors)
    {
        var proseLines = new List<string>();
        var mechanics = new List<string>();

        for (int i = start; i < end; i++)
        {
            var trimmed = lines[i].Trim();
            if (trimmed.Length > 1 && trimmed[0] == '+' && char.IsLetter(trimmed[1]))
                mechanics.Add(trimmed[1..]);
            else
                proseLines.Add(lines[i]);
        }

        return new FailureOutcome(JoinProse(proseLines), mechanics);
    }

    static SuccessOutcome ParseSuccess(string[] lines, int start, int end, List<ParseError> errors)
    {
        var proseLines = new List<string>();
        var mechanics = new List<string>();

        for (int i = start; i < end; i++)
        {
            var trimmed = lines[i].Trim();
            if (trimmed.Length > 1 && trimmed[0] == '+' && char.IsLetter(trimmed[1]))
                mechanics.Add(trimmed[1..]);
            else
                proseLines.Add(lines[i]);
        }

        return new SuccessOutcome(JoinProse(proseLines), mechanics);
    }

    static void ParseBranches(string[] lines, int start, int end,
        List<BranchDef> branches, List<ParseError> errors)
    {
        for (int i = start; i < end; i++)
        {
            var trimmed = lines[i].Trim();
            if (string.IsNullOrEmpty(trimmed)) continue;

            if (!trimmed.StartsWith("* "))
            {
                errors.Add(new ParseError(i + 1, $"Expected '* Label -> encounter_ref' in branches section."));
                continue;
            }

            var bm = BranchPattern().Match(trimmed);
            if (!bm.Success)
            {
                errors.Add(new ParseError(i + 1, $"Invalid branch format. Expected '* Label -> encounter_ref'."));
                continue;
            }

            var labelPart = bm.Groups[1].Value.Trim();
            var refPart = bm.Groups[2].Value.Trim();

            // Extract [requires ...] from ref
            string? req = null;
            var reqMatch = RequiresTag().Match(refPart);
            if (reqMatch.Success)
            {
                req = reqMatch.Groups[1].Value.Trim();
                refPart = refPart[..reqMatch.Index].Trim();
            }

            branches.Add(new BranchDef(labelPart, refPart, req));
        }
    }

    static string JoinProse(List<string> lines)
    {
        // Strip leading/trailing blank lines
        int first = 0, last = lines.Count - 1;
        while (first <= last && string.IsNullOrWhiteSpace(lines[first])) first++;
        while (last >= first && string.IsNullOrWhiteSpace(lines[last])) last--;
        if (first > last) return "";

        // Find minimum leading whitespace (common indent)
        int minIndent = int.MaxValue;
        for (int i = first; i <= last; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;
            int indent = 0;
            while (indent < lines[i].Length && lines[i][indent] == ' ') indent++;
            minIndent = Math.Min(minIndent, indent);
        }
        if (minIndent == int.MaxValue) minIndent = 0;

        return string.Join('\n', lines.Skip(first).Take(last - first + 1)
            .Select(l => string.IsNullOrWhiteSpace(l) ? "" : l[minIndent..].TrimEnd()));
    }
}
