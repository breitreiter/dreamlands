namespace CombatPrototype.Cmb;

public static class DiceParser
{
    /// <summary>
    /// Accepts forms: "1d8", "2d8+2", "1d4-1", "+4", "-2", "5".
    /// </summary>
    public static DiceRoll Parse(string raw)
    {
        var s = raw.Trim();
        if (s.Length == 0) throw new FormatException("empty dice expression");

        int dIdx = s.IndexOf('d');
        if (dIdx < 0)
        {
            return new DiceRoll(0, 0, ParseSignedInt(s));
        }

        int count = int.Parse(s[..dIdx]);
        string rest = s[(dIdx + 1)..];

        int sep = -1;
        for (int i = 0; i < rest.Length; i++)
        {
            if (rest[i] == '+' || rest[i] == '-') { sep = i; break; }
        }

        if (sep < 0)
        {
            return new DiceRoll(count, int.Parse(rest), 0);
        }

        int sides = int.Parse(rest[..sep]);
        int mod = int.Parse(rest[sep..]);
        return new DiceRoll(count, sides, mod);
    }

    private static int ParseSignedInt(string s) =>
        s.StartsWith('+') ? int.Parse(s[1..]) : int.Parse(s);
}
