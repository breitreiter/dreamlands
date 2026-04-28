using System.Globalization;

namespace CombatPrototype.Cmb;

/// <summary>
/// Parses .fight monster encounter files. Token-driven, .enc-style. Lines starting with
/// '+' are directives; '#' is a comment; blank lines are ignored. Block directives
/// (move/intro/win/lose) consume subsequent lines until the next '+' at column 0.
/// </summary>
public static class CmbParser
{
    public static CmbEncounter ParseFile(string path) =>
        ParseLines(File.ReadAllLines(path), path);

    public static CmbEncounter ParseLines(IReadOnlyList<string> lines, string source = "<string>")
    {
        var enc = new CmbEncounter();
        int i = 0;
        while (i < lines.Count)
        {
            string raw = lines[i];
            string trimmed = raw.TrimEnd();

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
                case "repool": enc.Repool = ParseBool(args, source, i); i++; break;
                case "stats":  enc.Stats = ParseStats(args, source, i); i++; break;
                case "hitbox": enc.Hitboxes.Add(ParseHitbox(args, source, i)); i++; break;
                case "move":   i = ParseMoveBlock(lines, i, args, enc, source); break;
                case "intro":  i = ParseProseBlock(lines, i + 1, out string intro, out _); enc.Intro = intro; break;
                case "win":    i = ParseProseBlock(lines, i + 1, out string winText, out var winMech);
                               enc.WinText = winText; enc.WinMechanics = winMech; break;
                case "lose":   i = ParseProseBlock(lines, i + 1, out string loseText, out var loseMech);
                               enc.LoseText = loseText; enc.LoseMechanics = loseMech; break;
                default:
                    throw new FormatException($"{source}:{i + 1}: unknown directive '+{directive}'");
            }
        }

        return enc;
    }

    private static (string directive, string args) SplitDirective(string line)
    {
        string body = line[1..];
        int sp = body.IndexOf(' ');
        return sp < 0 ? (body, "") : (body[..sp], body[(sp + 1)..].Trim());
    }

    private static bool ParseBool(string s, string source, int line) => s.ToLowerInvariant() switch
    {
        "true" or "yes" or "1" => true,
        "false" or "no" or "0" or "" => false,
        _ => throw new FormatException($"{source}:{line + 1}: bad boolean '{s}'")
    };

    private static MonsterStats ParseStats(string args, string source, int line)
    {
        int hp = 0, ac = 0, toHit = 0;
        DiceRoll dmg = new(0, 0, 0);
        foreach (var pair in SplitKv(args))
        {
            switch (pair.Key)
            {
                case "hp":     hp = int.Parse(pair.Value, CultureInfo.InvariantCulture); break;
                case "ac":     ac = int.Parse(pair.Value, CultureInfo.InvariantCulture); break;
                case "to_hit": toHit = ParseSignedInt(pair.Value); break;
                case "damage": dmg = DiceParser.Parse(pair.Value); break;
                default:
                    throw new FormatException($"{source}:{line + 1}: unknown stats key '{pair.Key}'");
            }
        }
        return new MonsterStats(hp, ac, toHit, dmg);
    }

    private static Hitbox ParseHitbox(string args, string source, int line)
    {
        int sp = args.IndexOf(' ');
        if (sp < 0) throw new FormatException($"{source}:{line + 1}: hitbox needs id and bounds");
        string id = args[..sp];
        string rest = args[(sp + 1)..];
        double l = 0, t = 0, r = 1, b = 1;
        foreach (var pair in SplitKv(rest))
        {
            double v = double.Parse(pair.Value, CultureInfo.InvariantCulture);
            switch (pair.Key)
            {
                case "left":   l = v; break;
                case "top":    t = v; break;
                case "right":  r = v; break;
                case "bottom": b = v; break;
                default:
                    throw new FormatException($"{source}:{line + 1}: unknown hitbox key '{pair.Key}'");
            }
        }
        return new Hitbox(id, l, t, r, b);
    }

    private static int ParseMoveBlock(IReadOnlyList<string> lines, int i, string id, CmbEncounter enc, string source)
    {
        if (string.IsNullOrEmpty(id))
            throw new FormatException($"{source}:{i + 1}: '+move' needs an id");

        var move = new MonsterMove { Id = id };
        i++;

        while (i < lines.Count && !lines[i].TrimStart().StartsWith('+'))
        {
            string line = lines[i].TrimEnd();
            string trimmed = line.TrimStart();
            if (trimmed.Length == 0 || trimmed.StartsWith('#')) { i++; continue; }

            if (trimmed.StartsWith('>'))
            {
                string mech = trimmed[1..].Trim();
                move.Mechanics.Add(ParseMechanic(mech));
            }
            else
            {
                int colon = trimmed.IndexOf(':');
                if (colon < 0) throw new FormatException($"{source}:{i + 1}: expected 'key: value' in move block, got: {trimmed}");
                string key = trimmed[..colon].Trim();
                string value = StripQuotes(trimmed[(colon + 1)..].Trim());

                switch (key)
                {
                    case "intent":    move.IntentClass = ParseIntentClass(value, source, i); break;
                    case "preview":   move.IntentText = value; break;
                    case "timer":     move.Timer = int.Parse(value, CultureInfo.InvariantCulture); break;
                    case "sprite":    move.Sprite = value; break;
                    case "anchor":    move.Anchor = value; break;
                    case "narration": move.Narration = value; break;
                    default:
                        throw new FormatException($"{source}:{i + 1}: unknown move key '{key}'");
                }
            }
            i++;
        }

        if (move.Mechanics.Count == 0)
            throw new FormatException($"{source}: move '{id}' has no mechanics");

        enc.Moves.Add(move);
        return i;
    }

    private static int ParseProseBlock(IReadOnlyList<string> lines, int i, out string text, out List<MechanicLine> mechanics)
    {
        var prose = new List<string>();
        mechanics = new List<MechanicLine>();
        while (i < lines.Count && !lines[i].TrimStart().StartsWith('+'))
        {
            string line = lines[i];
            string trimmed = line.TrimStart();
            if (trimmed.StartsWith('>'))
            {
                mechanics.Add(ParseMechanic(trimmed[1..].Trim()));
            }
            else if (trimmed.StartsWith('#'))
            {
                // comment, skip
            }
            else
            {
                prose.Add(line);
            }
            i++;
        }
        text = string.Join("\n", prose).Trim();
        return i;
    }

    private static MechanicLine ParseMechanic(string s)
    {
        int sp = s.IndexOf(' ');
        return sp < 0 ? new MechanicLine(s, "") : new MechanicLine(s[..sp], s[(sp + 1)..].Trim());
    }

    private static IntentClass ParseIntentClass(string s, string source, int line) => s switch
    {
        "attack"        => IntentClass.Attack,
        "heavy_attack"  => IntentClass.HeavyAttack,
        "defend"        => IntentClass.Defend,
        "pierce"        => IntentClass.Pierce,
        "condition"     => IntentClass.Condition,
        "flee"          => IntentClass.Flee,
        _ => throw new FormatException($"{source}:{line + 1}: unknown intent class '{s}'")
    };

    private static int ParseSignedInt(string s) =>
        s.StartsWith('+') ? int.Parse(s[1..], CultureInfo.InvariantCulture) : int.Parse(s, CultureInfo.InvariantCulture);

    private static string StripQuotes(string s) =>
        s.Length >= 2 && s[0] == '"' && s[^1] == '"' ? s[1..^1] : s;

    private static IEnumerable<KeyValuePair<string, string>> SplitKv(string args)
    {
        foreach (var part in args.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            int eq = part.IndexOf('=');
            if (eq < 0) throw new FormatException($"expected key=value, got '{part}'");
            yield return new KeyValuePair<string, string>(part[..eq], part[(eq + 1)..]);
        }
    }
}
