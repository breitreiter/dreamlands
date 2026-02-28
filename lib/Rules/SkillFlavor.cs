namespace Dreamlands.Rules;

/// <summary>Flavor text for skill levels, keyed by (Skill, level).</summary>
public static class SkillFlavor
{
    /// <summary>Get flavor text for a skill at a given level. Returns empty string for unknown combos.</summary>
    public static string Get(Skill skill, int level)
    {
        var tier = level switch
        {
            <= 0 => 0,
            <= 2 => 1,
            _ => 2,
        };
        return Flavors.TryGetValue((skill, tier), out var text) ? text : "";
    }

    // tier 0 = Unskilled (level ≤ 0), tier 1 = Trained (level 1-2), tier 2 = Expert (level 3+)
    static readonly Dictionary<(Skill, int), string> Flavors = new()
    {
        [(Skill.Combat, 0)] = "Violence is a tool for simpletons and savages",
        [(Skill.Combat, 1)] = "You know drills and forms, but have spilled little blood",
        [(Skill.Combat, 2)] = "You read intent in a shoulder twitch and end fights decisively",

        [(Skill.Negotiation, 0)] = "You speak plainly and without artifice",
        [(Skill.Negotiation, 1)] = "You recognize leverage and know when to press or yield",
        [(Skill.Negotiation, 2)] = "You shape the discussion like an artisan",

        [(Skill.Bushcraft, 0)] = "The outdoors is strange, hostile terrain",
        [(Skill.Bushcraft, 1)] = "You can find water, shelter, and a way through",
        [(Skill.Bushcraft, 2)] = "You read land and weather like scripture",

        [(Skill.Cunning, 0)] = "You prefer a straightforward approach",
        [(Skill.Cunning, 1)] = "You notice angles and opportunities others overlook",
        [(Skill.Cunning, 2)] = "You are always three moves ahead",

        [(Skill.Luck, 0)] = "You've learned to rely on skill, never chance",
        [(Skill.Luck, 1)] = "Fortune sometimes favors you",
        [(Skill.Luck, 2)] = "You live a charmed life",

        [(Skill.Mercantile, 0)] = "You have enthusiasm, but little sense of margins or risk",
        [(Skill.Mercantile, 1)] = "You understand trade and the cost of tying up coin",
        [(Skill.Mercantile, 2)] = "You walk markets like a lion, hungry and calculating",
    };
}
