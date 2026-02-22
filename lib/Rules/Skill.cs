namespace Dreamlands.Rules;

/// <summary>Player skills used in encounter checks, rule resolution, and save games.</summary>
public enum Skill
{
    Combat,
    Negotiation,
    Bushcraft,
    Cunning,
    Luck,
    Mercantile
}

/// <summary>Display and script metadata for a single <see cref="Skill"/>.</summary>
public readonly record struct SkillInfo(Skill Skill, string ScriptName, string DisplayName, string Description);

/// <summary>Lookup and metadata utilities for <see cref="Skill"/> values.</summary>
public static class Skills
{
    /// <summary>All defined skills in canonical order.</summary>
    public static IReadOnlyList<SkillInfo> All { get; } = new SkillInfo[]
    {
        new(Skill.Combat,      "combat",      "Combat",      "Fighting prowess in close encounters"),
        new(Skill.Negotiation, "negotiation", "Negotiation", "Persuasion, deception, and social cunning"),
        new(Skill.Bushcraft,   "bushcraft",   "Bushcraft",   "Wilderness survival and travel know-how"),
        new(Skill.Cunning,     "cunning",     "Cunning",     "Trickery, awareness, and staying one step ahead"),
        new(Skill.Luck,        "luck",        "Luck",        "A slight nudge on the odds"),
        new(Skill.Mercantile,  "mercantile",  "Mercantile",  "An eye for value and a tongue for prices"),
    };

    private static readonly Dictionary<string, Skill> ByScriptName =
        All.ToDictionary(i => i.ScriptName, i => i.Skill);

    private static readonly Dictionary<Skill, SkillInfo> InfoBySkill =
        All.ToDictionary(i => i.Skill);

    /// <summary>Get display metadata for a skill.</summary>
    public static SkillInfo GetInfo(this Skill skill) => InfoBySkill[skill];

    /// <summary>The lowercase script name for use in encounter files.</summary>
    public static string ScriptName(this Skill skill) => InfoBySkill[skill].ScriptName;

    /// <summary>Look up a skill by its encounter-script name. Returns null if not recognised.</summary>
    public static Skill? FromScriptName(string name) =>
        ByScriptName.TryGetValue(name, out var skill) ? skill : null;

    /// <summary>True if <paramref name="name"/> matches a known skill script name.</summary>
    public static bool IsValidScriptName(string name) =>
        ByScriptName.ContainsKey(name);
}
