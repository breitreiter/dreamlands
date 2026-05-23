using System.Globalization;
using System.Text.RegularExpressions;

namespace Dreamlands.Encounter;

/// <summary>
/// Parses .fight monster encounter files. Token-driven; shares sigils with the
/// .enc format so authors don't have to context-switch:
///
///   `[key value]`  front-matter (file-level attributes)
///   `* name ...`   section header (move / intro / win / lose)
///   `+verb args`   mechanic verb inside a prose block (gold, tag, etc.)
///   `#`            comment
///
/// Example:
///
///   [title Some Goblin]
///   [image foo/bar.webp]
///   [blood #7a0a0a]
///   [stats hp=18]
///
///   * move Heavy Telegraphed Slow Attack
///     narration: It winds back, hauling the maul over its head.
///     narration: It snarls and lifts the spike to shoulder height.
///
///   * move Defend
///     narration: It hunches behind its shield.
///
///   * intro
///   A goblin steps from the brush.
///
///   * win
///   The goblin slumps.
///   +gold 8
///   +tag killed_goblin
///
///   * lose
///   Everything goes black.
///
/// `* move` is followed by a Move encoding parsed by <see cref="Move.Parse"/>
/// (last token is the base, preceding tokens are mutators). Multiple narration
/// lines on one move = variants; the runner picks one randomly per use.
/// </summary>
public static partial class CmbParser
{
    [GeneratedRegex(@"^\[(\w+)(?:\s+(.+?))?\]\s*$")]
    private static partial Regex FrontMatterPattern();

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
            string raw = lines[i];
            string trimmed = raw.TrimEnd();
            string stripped = trimmed.TrimStart();

            if (stripped.Length == 0 || stripped.StartsWith('#'))
            {
                i++;
                continue;
            }

            if (stripped.StartsWith('['))
            {
                ParseFrontMatter(stripped, enc, source, i);
                i++;
                continue;
            }

            if (stripped.StartsWith("* "))
            {
                i = ParseSection(lines, i, stripped[2..].TrimStart(), enc, source);
                continue;
            }

            if (stripped.StartsWith('+'))
                throw new FormatException(
                    $"{source}:{i + 1}: '+' is reserved for mechanic verbs inside section bodies. " +
                    $"Front-matter uses '[key value]'. Got: {stripped}");

            if (stripped.StartsWith('>'))
                throw new FormatException(
                    $"{source}:{i + 1}: '>' mechanic lines are obsolete; use '+verb args' instead. Got: {stripped}");

            throw new FormatException($"{source}:{i + 1}: expected '[key value]', '* section', or '#' comment, got: {stripped}");
        }

        return enc;
    }

    static void ParseFrontMatter(string line, CombatEncounter enc, string source, int i)
    {
        var m = FrontMatterPattern().Match(line);
        if (!m.Success)
            throw new FormatException($"{source}:{i + 1}: malformed front-matter, expected '[key value]': {line}");
        string key = m.Groups[1].Value;
        string value = m.Groups[2].Success ? m.Groups[2].Value.Trim() : "";

        switch (key)
        {
            case "title":  enc.Title = value; break;
            case "image":  enc.Image = value; break;
            case "blood":  enc.BloodColor = value; break;
            case "repool": enc.Repool = ParseBool(value, source, i); break;
            case "stats":  enc.Stats = ParseStats(value, source, i); break;
            default:
                throw new FormatException($"{source}:{i + 1}: unknown front-matter key '[{key}]'");
        }
    }

    static int ParseSection(IReadOnlyList<string> lines, int i, string header, CombatEncounter enc, string source)
    {
        if (header.Length == 0)
            throw new FormatException($"{source}:{i + 1}: '* ' needs a section name (move/intro/win/lose)");

        int sp = header.IndexOf(' ');
        string kind = sp < 0 ? header : header[..sp];
        string args = sp < 0 ? "" : header[(sp + 1)..].Trim();

        switch (kind)
        {
            case "move":
                return ParseMoveBlock(lines, i, args, enc, source);
            case "intro":
                i = ParseProseBlock(lines, i + 1, out string intro, out _);
                enc.Intro = intro;
                return i;
            case "win":
                i = ParseProseBlock(lines, i + 1, out string winText, out var winMech);
                enc.WinText = winText;
                enc.WinMechanics = winMech;
                return i;
            case "lose":
                i = ParseProseBlock(lines, i + 1, out string loseText, out var loseMech);
                enc.LoseText = loseText;
                enc.LoseMechanics = loseMech;
                return i;
            default:
                throw new FormatException($"{source}:{i + 1}: unknown section '* {kind}'");
        }
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
            throw new FormatException($"{source}:{line + 1}: [stats] needs hp=<n> with n > 0");
        return new MonsterStats(hp);
    }

    static int ParseMoveBlock(IReadOnlyList<string> lines, int i, string moveEncoding, CombatEncounter enc, string source)
    {
        if (string.IsNullOrEmpty(moveEncoding))
            throw new FormatException($"{source}:{i + 1}: '* move' needs a Move encoding (e.g. 'Big Attack')");

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

        while (i < lines.Count && !IsSectionStart(lines[i]))
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
        while (i < lines.Count && !IsSectionStart(lines[i]))
        {
            string raw = lines[i];
            string trimmed = raw.TrimStart();
            if (trimmed.StartsWith('+'))
            {
                string mech = trimmed[1..].TrimStart();
                if (mech.Length > 0) mechanics.Add(mech);
            }
            else if (trimmed.StartsWith('>'))
            {
                throw new FormatException($"line {i + 1}: '>' mechanic lines are obsolete; use '+verb args' instead. Got: {trimmed}");
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

    static bool IsSectionStart(string line)
    {
        string s = line.TrimStart();
        return s.StartsWith("* ") || s.StartsWith('[');
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
