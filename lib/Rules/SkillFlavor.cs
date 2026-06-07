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

    // Per-tier description text from project/design/skills.md "Character Sheet Text".
    // Communicates both the passive benefit and the check-resolution behavior.
    // tier 0 = Untrained, tier 1 = Trained, tier 2 = Expert.
    static readonly Dictionary<(Skill, int), string> Flavors = new()
    {
        [(Skill.Combat, 0)] = "Daggers and light armor only. Encounter checks are punishing.",
        [(Skill.Combat, 1)] = "Adds axes and medium armor. Encounter checks are fair.",
        [(Skill.Combat, 2)] = "Adds swords and heavy armor. Encounter checks are generous.",

        [(Skill.Negotiation, 0)] = "No contract bonus. Encounter checks are punishing.",
        [(Skill.Negotiation, 1)] = "+20% contract payout. Encounter checks are fair.",
        [(Skill.Negotiation, 2)] = "+40% contract payout. Encounter checks are generous.",

        [(Skill.Bushcraft, 0)] = "No passive benefit. Encounter checks are punishing.",
        [(Skill.Bushcraft, 1)] = "Halves travel hazard costs; eat every other night. Encounter checks are fair.",
        [(Skill.Bushcraft, 2)] = "Quarters travel hazard costs. Encounter checks are generous.",

        [(Skill.Cunning, 0)] = "No passive benefit. Encounter checks are punishing.",
        [(Skill.Cunning, 1)] = "40% chance to resist serious conditions. Encounter checks are fair.",
        [(Skill.Cunning, 2)] = "80% chance to resist serious conditions. Encounter checks are generous.",
    };
}
