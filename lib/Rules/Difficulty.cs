namespace Dreamlands.Rules;

/// <summary>Skill check difficulty tiers used in encounter branching.</summary>
public enum Difficulty
{
    Trivial,
    Easy,
    Medium,
    Hard,
    VeryHard,
    Heroic
}

/// <summary>Script and display metadata for a <see cref="Difficulty"/> tier.</summary>
public readonly record struct DifficultyInfo(Difficulty Difficulty, string ScriptName, string DisplayName, int Target);

/// <summary>Lookup and metadata utilities for <see cref="Difficulty"/> values.</summary>
public static class Difficulties
{
    /// <summary>All difficulty tiers in ascending order.</summary>
    public static IReadOnlyList<DifficultyInfo> All { get; } = new DifficultyInfo[]
    {
        new(Difficulty.Trivial,  "trivial",   "Trivial",   5),
        new(Difficulty.Easy,     "easy",      "Easy",      10),
        new(Difficulty.Medium,   "medium",    "Medium",    15),
        new(Difficulty.Hard,     "hard",      "Hard",      20),
        new(Difficulty.VeryHard, "very_hard", "Very Hard", 25),
        new(Difficulty.Heroic,   "heroic",    "Heroic",    30),
    };

    private static readonly Dictionary<string, Difficulty> ByScriptName =
        All.ToDictionary(i => i.ScriptName, i => i.Difficulty);

    private static readonly Dictionary<Difficulty, DifficultyInfo> InfoByDifficulty =
        All.ToDictionary(i => i.Difficulty);

    /// <summary>Get metadata for a difficulty tier.</summary>
    public static DifficultyInfo GetInfo(this Difficulty difficulty) => InfoByDifficulty[difficulty];

    /// <summary>The lowercase script name for use in encounter files.</summary>
    public static string ScriptName(this Difficulty difficulty) => InfoByDifficulty[difficulty].ScriptName;

    /// <summary>The numeric target DC for this difficulty tier.</summary>
    public static int Target(this Difficulty difficulty) => InfoByDifficulty[difficulty].Target;

    /// <summary>Look up a difficulty by its encounter-script name. Returns null if not recognised.</summary>
    public static Difficulty? FromScriptName(string name) =>
        ByScriptName.TryGetValue(name, out var difficulty) ? difficulty : null;

    /// <summary>True if <paramref name="name"/> matches a known difficulty script name.</summary>
    public static bool IsValidScriptName(string name) =>
        ByScriptName.ContainsKey(name);
}
