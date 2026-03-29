using System.Text.RegularExpressions;

namespace Dreamlands.Tactical;

public static partial class TacticalParser
{
    enum Section { None, Stats, Timers, Openings, Path, Approaches, Failure, Branches }

    [GeneratedRegex(@"^\[(\w+)\s+(.+?)\]\s*$")]
    private static partial Regex FrontMatterPattern();

    // * Timer Name [counter Stop text]: spirits 2 every 4
    [GeneratedRegex(@"^\*\s+(.+?):\s+(spirits|resistance)\s+(\d+)\s+every\s+(\d+)\s*$")]
    private static partial Regex TimerPattern();

    [GeneratedRegex(@"\[counter\s+(.+?)\]")]
    private static partial Regex CounterTag();

    // * Opening Name: cost_type [amount] -> effect_type [amount] [requires ...]
    [GeneratedRegex(@"^\*\s+(.+?):\s+(.+?)\s*->\s*(.+?)\s*$")]
    private static partial Regex OpeningPattern();

    // * kind: momentum N, timers N[, openings N]
    [GeneratedRegex(@"^\*\s+(scout|direct|wild):\s*(.+)$")]
    private static partial Regex ApproachPattern();

    // * Label [intent tag] -> encounter_ref [requires condition]
    [GeneratedRegex(@"^\*\s+(.+?)\s*->\s*(.+?)\s*$")]
    private static partial Regex BranchPattern();

    // [intent tag] extractor
    [GeneratedRegex(@"\[intent\s+(\w+)\]")]
    private static partial Regex IntentTag();

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
        Variant? variant = null;
        string? intent = null;
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
                    if (variant != null)
                        errors.Add(new ParseError(i + 1, "Duplicate [variant]."));
                    else if (!Enum.TryParse<Variant>(value, ignoreCase: true, out var v))
                        errors.Add(new ParseError(i + 1, $"Invalid variant '{value}'. Must be 'combat' or 'traverse'."));
                    else
                        variant = v;
                    break;
                case "intent":
                    intent = value;
                    break;
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
                    "stats" => Section.Stats,
                    "timers" => Section.Timers,
                    "openings" => Section.Openings,
                    "path" => Section.Path,
                    "approaches" => Section.Approaches,
                    "failure" => Section.Failure,
                    "branches" => Section.Branches,
                    _ => Section.None,
                };

                if (section == Section.None)
                    errors.Add(new ParseError(i + 1, $"Unknown section '{name}'."));
                else if (sections.ContainsKey(section))
                    errors.Add(new ParseError(i + 1, $"Duplicate section '{name}'."));

                currentSection = section == Section.None ? null : section;
                currentSectionStart = i + 1;
            }
        }
        if (currentSection != null)
            sections[currentSection.Value] = (currentSectionStart, lines.Length);

        // Detect encounter vs group
        bool isGroup = sections.ContainsKey(Section.Branches);
        bool isEncounter = sections.ContainsKey(Section.Stats) || sections.ContainsKey(Section.Openings)
                        || sections.ContainsKey(Section.Path) || sections.ContainsKey(Section.Timers) || sections.ContainsKey(Section.Approaches)
                        || sections.ContainsKey(Section.Failure);

        if (isGroup && isEncounter)
        {
            errors.Add(new ParseError(null, "File has both 'branches:' and encounter sections. A .tac file must be either an encounter or a group."));
            return new TacticalParseResult { Errors = errors };
        }

        if (!isGroup && !isEncounter)
        {
            errors.Add(new ParseError(null, "No sections found. Expected stats:/openings:/failure: (encounter) or branches: (group)."));
            return new TacticalParseResult { Errors = errors };
        }

        if (isGroup)
            return ParseGroup(lines, title, body, intent, tier, requires, sections, errors);

        return ParseEncounter(lines, title, body, variant, intent, stat, tier, requires, sections, errors);
    }

    static TacticalParseResult ParseEncounter(
        string[] lines, string title, string body,
        Variant? variant, string? intent, string? stat, int? tier,
        List<string> requires, Dictionary<Section, (int start, int end)> sections,
        List<ParseError> errors)
    {
        if (variant == null)
            errors.Add(new ParseError(null, "Encounter is missing [variant combat|traverse]."));

        if (!sections.ContainsKey(Section.Stats))
            errors.Add(new ParseError(null, "Missing required section 'stats:'."));
        if (!sections.ContainsKey(Section.Openings))
            errors.Add(new ParseError(null, "Missing required section 'openings:'."));
        if (!sections.ContainsKey(Section.Failure))
            errors.Add(new ParseError(null, "Missing required section 'failure:'."));

        // Stats
        int resistance = 0;
        int? momentum = null;
        int? queueDepth = null;

        if (sections.TryGetValue(Section.Stats, out var statsRange))
        {
            for (int i = statsRange.start; i < statsRange.end; i++)
            {
                var trimmed = lines[i].Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;

                var parts = trimmed.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 2 || !int.TryParse(parts[1], out var val))
                {
                    errors.Add(new ParseError(i + 1, $"Invalid stat line '{trimmed}'. Expected 'key value'."));
                    continue;
                }

                switch (parts[0])
                {
                    case "resistance": resistance = val; break;
                    case "momentum": momentum = val; break;
                    case "queue_depth": queueDepth = val; break;
                    default:
                        errors.Add(new ParseError(i + 1, $"Unknown stat '{parts[0]}'."));
                        break;
                }
            }
        }

        if (variant == Variant.Combat && momentum == null)
            errors.Add(new ParseError(null, "Combat encounters must specify 'momentum' in stats."));
        if (variant == Variant.Traverse && queueDepth == null)
            errors.Add(new ParseError(null, "Traverse encounters must specify 'queue_depth' in stats."));

        // Timers
        int timerDraw = 0;
        var timers = new List<TimerDef>();
        if (sections.TryGetValue(Section.Timers, out var timerRange))
            ParseTimers(lines, timerRange.start, timerRange.end, ref timerDraw, timers, errors);

        if (timerDraw > timers.Count && timers.Count > 0)
            errors.Add(new ParseError(null, $"Timer draw ({timerDraw}) exceeds timer pool size ({timers.Count})."));

        // Openings
        var openings = new List<OpeningDef>();
        if (sections.TryGetValue(Section.Openings, out var openingRange))
            ParseOpenings(lines, openingRange.start, openingRange.end, openings, errors);

        // Path (traverse authored path — same syntax as openings)
        var path = new List<OpeningDef>();
        if (sections.TryGetValue(Section.Path, out var pathRange))
            ParseOpenings(lines, pathRange.start, pathRange.end, path, errors);

        // Approaches
        var approaches = new List<ApproachDef>();
        if (sections.TryGetValue(Section.Approaches, out var approachRange))
            ParseApproaches(lines, approachRange.start, approachRange.end, approaches, errors);

        if (variant == Variant.Combat && approaches.Count == 0 && sections.ContainsKey(Section.Approaches))
            errors.Add(new ParseError(null, "Approaches section is empty."));

        // Failure
        FailureOutcome? failure = null;
        if (sections.TryGetValue(Section.Failure, out var failureRange))
            failure = ParseFailure(lines, failureRange.start, failureRange.end, errors);

        if (errors.Count > 0)
            return new TacticalParseResult { Errors = errors };

        return new TacticalParseResult
        {
            Encounter = new TacticalEncounter
            {
                Title = title,
                Body = body,
                Variant = variant!.Value,
                Intent = intent,
                Stat = stat,
                Tier = tier,
                Requires = requires,
                Resistance = resistance,
                Momentum = momentum,
                QueueDepth = queueDepth,
                TimerDraw = timerDraw,
                Timers = timers,
                Openings = openings,
                Path = path,
                Approaches = approaches,
                Failure = failure,
            }
        };
    }

    static TacticalParseResult ParseGroup(
        string[] lines, string title, string body,
        string? intent, int? tier, List<string> requires,
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

    static void ParseTimers(string[] lines, int start, int end, ref int draw,
        List<TimerDef> timers, List<ParseError> errors)
    {
        for (int i = start; i < end; i++)
        {
            var trimmed = lines[i].Trim();
            if (string.IsNullOrEmpty(trimmed)) continue;

            // draw N
            if (trimmed.StartsWith("draw "))
            {
                if (!int.TryParse(trimmed[5..].Trim(), out var d) || d < 1)
                    errors.Add(new ParseError(i + 1, $"Invalid draw count '{trimmed}'."));
                else
                    draw = d;
                continue;
            }

            // * Name: effect amount every countdown
            if (!trimmed.StartsWith("* "))
            {
                errors.Add(new ParseError(i + 1, $"Expected '* Timer Name: ...' or 'draw N', got '{trimmed}'."));
                continue;
            }

            var m = TimerPattern().Match(trimmed);
            if (!m.Success)
            {
                errors.Add(new ParseError(i + 1, $"Invalid timer format. Expected '* Name: spirits|resistance N every N'."));
                continue;
            }

            var name = m.Groups[1].Value.Trim();

            // Extract [counter ...] from name
            string? counterName = null;
            var counterMatch = CounterTag().Match(name);
            if (counterMatch.Success)
            {
                counterName = counterMatch.Groups[1].Value.Trim();
                name = name[..counterMatch.Index].Trim();
            }

            var effect = m.Groups[2].Value == "spirits" ? TimerEffect.Spirits : TimerEffect.Resistance;
            var amount = int.Parse(m.Groups[3].Value);
            var countdown = int.Parse(m.Groups[4].Value);

            timers.Add(new TimerDef(name, effect, amount, countdown, counterName));
        }
    }

    static void ParseOpenings(string[] lines, int start, int end,
        List<OpeningDef> openings, List<ParseError> errors)
    {
        for (int i = start; i < end; i++)
        {
            var trimmed = lines[i].Trim();
            if (string.IsNullOrEmpty(trimmed)) continue;

            if (!trimmed.StartsWith("* "))
            {
                errors.Add(new ParseError(i + 1, $"Expected '* Opening Name: ...' in openings section."));
                continue;
            }

            var m = OpeningPattern().Match(trimmed);
            if (!m.Success)
            {
                errors.Add(new ParseError(i + 1, $"Invalid opening format. Expected '* Name: cost -> effect'."));
                continue;
            }

            var name = m.Groups[1].Value.Trim();
            var costStr = m.Groups[2].Value.Trim();
            var effectAndReqs = m.Groups[3].Value.Trim();

            // Extract [requires ...] from the effect side
            string? req = null;
            var reqMatch = RequiresTag().Match(effectAndReqs);
            if (reqMatch.Success)
            {
                req = reqMatch.Groups[1].Value.Trim();
                effectAndReqs = effectAndReqs[..reqMatch.Index].Trim();
            }

            // Also check name for [requires ...]
            if (req == null)
            {
                reqMatch = RequiresTag().Match(name);
                if (reqMatch.Success)
                {
                    req = reqMatch.Groups[1].Value.Trim();
                    name = name[..reqMatch.Index].Trim();
                }
            }

            var cost = ParseCost(costStr, i + 1, errors);
            var effect = ParseEffect(effectAndReqs, i + 1, errors);

            if (cost != null && effect != null)
                openings.Add(new OpeningDef(name, cost, effect, req));
        }
    }

    static OpeningCost? ParseCost(string s, int line, List<ParseError> errors)
    {
        var parts = s.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            errors.Add(new ParseError(line, "Empty cost."));
            return null;
        }

        return parts[0] switch
        {
            "free" when parts.Length == 1 => new OpeningCost(CostKind.Free),
            "tick" when parts.Length == 1 => new OpeningCost(CostKind.Tick),
            "momentum" when parts.Length == 2 && int.TryParse(parts[1], out var m) => new OpeningCost(CostKind.Momentum, m),
            "spirits" when parts.Length == 2 && int.TryParse(parts[1], out var sp) => new OpeningCost(CostKind.Spirits, sp),
            _ => Error()
        };

        OpeningCost? Error()
        {
            errors.Add(new ParseError(line, $"Invalid cost '{s}'. Expected 'free', 'tick', 'momentum N', or 'spirits N'."));
            return null;
        }
    }

    static OpeningEffect? ParseEffect(string s, int line, List<ParseError> errors)
    {
        var parts = s.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            errors.Add(new ParseError(line, "Empty effect."));
            return null;
        }

        return parts[0] switch
        {
            "stop_timer" when parts.Length == 1 => new OpeningEffect(EffectKind.StopTimer),
            "damage" when parts.Length == 2 && int.TryParse(parts[1], out var d) => new OpeningEffect(EffectKind.Damage, d),
            "momentum" when parts.Length == 2 && int.TryParse(parts[1], out var m) => new OpeningEffect(EffectKind.Momentum, m),
            _ => Error()
        };

        OpeningEffect? Error()
        {
            errors.Add(new ParseError(line, $"Invalid effect '{s}'. Expected 'damage N', 'stop_timer', or 'momentum N'."));
            return null;
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
                errors.Add(new ParseError(i + 1, $"Expected '* scout|direct|wild: ...' in approaches section."));
                continue;
            }

            var m = ApproachPattern().Match(trimmed);
            if (!m.Success)
            {
                errors.Add(new ParseError(i + 1, $"Invalid approach format. Expected '* kind: momentum N, timers N'."));
                continue;
            }

            if (!Enum.TryParse<ApproachKind>(m.Groups[1].Value, ignoreCase: true, out var kind))
            {
                errors.Add(new ParseError(i + 1, $"Invalid approach kind '{m.Groups[1].Value}'."));
                continue;
            }

            int mom = 0, timers = 0, bonusOpenings = 0;
            var pairs = m.Groups[2].Value.Split(',', StringSplitOptions.TrimEntries);
            foreach (var pair in pairs)
            {
                var kv = pair.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                if (kv.Length != 2 || !int.TryParse(kv[1], out var val))
                {
                    errors.Add(new ParseError(i + 1, $"Invalid approach parameter '{pair}'."));
                    continue;
                }
                switch (kv[0])
                {
                    case "momentum": mom = val; break;
                    case "timers": timers = val; break;
                    case "openings": bonusOpenings = val; break;
                    default:
                        errors.Add(new ParseError(i + 1, $"Unknown approach parameter '{kv[0]}'."));
                        break;
                }
            }

            approaches.Add(new ApproachDef(kind, mom, timers, bonusOpenings));
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
                errors.Add(new ParseError(i + 1, $"Invalid branch format. Expected '* Label [intent tag] -> encounter_ref'."));
                continue;
            }

            var labelPart = bm.Groups[1].Value.Trim();
            var refPart = bm.Groups[2].Value.Trim();

            // Extract [intent ...] from label
            string? branchIntent = null;
            var intentMatch = IntentTag().Match(labelPart);
            if (intentMatch.Success)
            {
                branchIntent = intentMatch.Groups[1].Value;
                labelPart = labelPart[..intentMatch.Index].Trim();
            }

            // Extract [requires ...] from ref
            string? req = null;
            var reqMatch = RequiresTag().Match(refPart);
            if (reqMatch.Success)
            {
                req = reqMatch.Groups[1].Value.Trim();
                refPart = refPart[..reqMatch.Index].Trim();
            }

            branches.Add(new BranchDef(labelPart, branchIntent, refPart, req));
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
