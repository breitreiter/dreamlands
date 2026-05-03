using System.Globalization;

namespace Dreamlands.Encounter;

/// <summary>
/// Parses .fight monster encounter files. Token-driven, .enc-adjacent. Lines
/// starting with '+' at column 0 are top-level directives; '#' is a comment;
/// blank lines are ignored. Block directives (move/intro/win/lose) consume
/// subsequent lines until the next column-0 '+'.
///
/// New format (RPS-shaped):
///
///   +title Some Goblin
///   +image foo/bar.webp
///   +blood #7a0a0a
///   +stats hp=18
///
///   +move Big Telegraphed Rare Attack
///     narration: It winds back, hauling the maul over its head.
///     narration: It snarls and lifts the spike to shoulder height.
///
///   +move Defend
///     narration: It hunches behind its shield.
///
///   +intro
///     A goblin steps from the brush.
///
///   +win
///     The goblin slumps.
///     > gold 8
///     > tag killed_goblin
///
///   +lose
///     Everything goes black.
///
/// `+move` is a Move encoding parsed by <see cref="Move.Parse"/> (last token is
/// the base, preceding tokens are mutators). Multiple narration lines on one
/// move = variants; the runner picks one randomly per use.
/// </summary>
public static class CmbParser
{
    public static CombatEncounter ParseFile(string path) =>
        ParseLines(File.ReadAllLines(path), path);

    public static CombatEncounter ParseString(string source, string sourceName = "<string>")
    {
        source = source.Replace("\r\n", "\n").Replace("\r", "\n");
        return ParseLines(source.Split('\n'), sourceName);
    }

    public static CombatEncounter ParseLines(IReadOnlyList<string> lines, string source = "<string>")
    {
        var enc = new CombatEncounter();
        int i = 0;
        while (i < lines.Count)
        {
            string trimmed = lines[i].TrimEnd();

            if (trimmed.Length == 0 || trimmed.TrimStart().StartsWith('#'))
            {
                i++;
                continue;
            }

            if (!trimmed.StartsWith('+'))
                throw new FormatException($"{source}:{i + 1}: expected directive starting with '+', got: {trimmed}");

            (string directive, string args) = SplitDirective(trimmed);

            switch (directive)
            {
                case "title":  enc.Title = args; i++; break;
                case "image":  enc.Image = args; i++; break;
                case "blood":  enc.BloodColor = args; i++; break;
                case "repool": enc.Repool = ParseBool(args, source, i); i++; break;
                case "stats":  enc.Stats = ParseStats(args, source, i); i++; break;
                case "move":   i = ParseMoveBlock(lines, i, args, enc, source); break;
                case "intro":
                    i = ParseProseBlock(lines, i + 1, out string intro, out _);
                    enc.Intro = intro;
                    break;
                case "win":
                    i = ParseProseBlock(lines, i + 1, out string winText, out var winMech);
                    enc.WinText = winText;
                    enc.WinMechanics = winMech;
                    break;
                case "lose":
                    i = ParseProseBlock(lines, i + 1, out string loseText, out var loseMech);
                    enc.LoseText = loseText;
                    enc.LoseMechanics = loseMech;
                    break;
                default:
                    throw new FormatException($"{source}:{i + 1}: unknown directive '+{directive}'");
            }
        }

        return enc;
    }

    static (string directive, string args) SplitDirective(string line)
    {
        string body = line[1..];
        int sp = body.IndexOf(' ');
        return sp < 0 ? (body, "") : (body[..sp], body[(sp + 1)..].Trim());
    }

    static bool ParseBool(string s, string source, int line) => s.ToLowerInvariant() switch
    {
        "true" or "yes" or "1" => true,
        "false" or "no" or "0" or "" => false,
        _ => throw new FormatException($"{source}:{line + 1}: bad boolean '{s}'")
    };

    static MonsterStats ParseStats(string args, string source, int line)
    {
        int hp = 0;
        foreach (var pair in SplitKv(args))
        {
            switch (pair.Key)
            {
                case "hp": hp = int.Parse(pair.Value, CultureInfo.InvariantCulture); break;
                default:
                    throw new FormatException($"{source}:{line + 1}: unknown stats key '{pair.Key}' (only 'hp' is recognized)");
            }
        }
        if (hp <= 0)
            throw new FormatException($"{source}:{line + 1}: +stats needs hp=<n> with n > 0");
        return new MonsterStats(hp);
    }

    static int ParseMoveBlock(IReadOnlyList<string> lines, int i, string moveEncoding, CombatEncounter enc, string source)
    {
        if (string.IsNullOrEmpty(moveEncoding))
            throw new FormatException($"{source}:{i + 1}: '+move' needs a Move encoding (e.g. 'Big Attack')");

        Move action;
        try
        {
            action = Move.Parse(moveEncoding);
        }
        catch (Exception ex)
        {
            throw new FormatException($"{source}:{i + 1}: {ex.Message}");
        }

        var def = new MonsterMoveDef { Action = action };
        i++;

        while (i < lines.Count && !lines[i].TrimStart().StartsWith('+'))
        {
            string trimmed = lines[i].TrimEnd().TrimStart();
            if (trimmed.Length == 0 || trimmed.StartsWith('#')) { i++; continue; }

            int colon = trimmed.IndexOf(':');
            if (colon < 0)
                throw new FormatException($"{source}:{i + 1}: expected 'narration: <text>' in move block, got: {trimmed}");
            string key = trimmed[..colon].Trim();
            string value = StripQuotes(trimmed[(colon + 1)..].Trim());

            switch (key)
            {
                case "narration": def.NarrationVariants.Add(value); break;
                default:
                    throw new FormatException($"{source}:{i + 1}: unknown move key '{key}' (only 'narration' is recognized)");
            }
            i++;
        }

        if (def.NarrationVariants.Count == 0)
            throw new FormatException($"{source}: move '{moveEncoding}' has no narration lines");

        enc.Moves.Add(def);
        return i;
    }

    static int ParseProseBlock(IReadOnlyList<string> lines, int i, out string text, out List<string> mechanics)
    {
        var prose = new List<string>();
        mechanics = new List<string>();
        while (i < lines.Count && !lines[i].TrimStart().StartsWith('+'))
        {
            string raw = lines[i];
            string trimmed = raw.TrimStart();
            if (trimmed.StartsWith('>'))
            {
                // > +gold 8 → "gold 8"; > tag killed_gorzog → "tag killed_gorzog"
                string mech = trimmed[1..].Trim();
                if (mech.StartsWith('+')) mech = mech[1..].TrimStart();
                if (mech.Length > 0) mechanics.Add(mech);
            }
            else if (trimmed.StartsWith('#'))
            {
                // comment, skip
            }
            else
            {
                prose.Add(raw);
            }
            i++;
        }
        text = string.Join("\n", prose).Trim();
        return i;
    }

    static string StripQuotes(string s) =>
        s.Length >= 2 && s[0] == '"' && s[^1] == '"' ? s[1..^1] : s;

    static IEnumerable<KeyValuePair<string, string>> SplitKv(string args)
    {
        foreach (var part in args.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            int eq = part.IndexOf('=');
            if (eq < 0) throw new FormatException($"expected key=value, got '{part}'");
            yield return new KeyValuePair<string, string>(part[..eq], part[(eq + 1)..]);
        }
    }
}
