namespace Dreamlands.Rules;

public enum ArcRewardKind { Skill, Health, Inventory }

public record ArcRewardSlot(
    string Id,
    string Label,
    ArcRewardKind Kind,
    int Cap,
    Skill? Skill,
    string Tier1Description,
    string Tier2Description);

/// <summary>
/// Static definition of the arc-completion tableau: what can be picked, caps, and magnitudes.
/// </summary>
public static class ArcRewards
{
    public static readonly ArcRewardSlot[] All =
    [
        new("combat",      "Combat",       ArcRewardKind.Skill,     2, Rules.Skill.Combat,
            "You can now equip axes and medium armor",
            "You can now equip swords and heavy armor"),
        new("negotiation", "Negotiation",  ArcRewardKind.Skill,     2, Rules.Skill.Negotiation,
            "Contracts pay 20% more on delivery",
            "Contracts pay 40% more on delivery"),
        new("cunning",     "Cunning",      ArcRewardKind.Skill,     2, Rules.Skill.Cunning,
            "40% chance to resist serious conditions (injured, etc)",
            "80% chance to resist serious conditions (injured, etc)"),
        new("bushcraft",   "Bushcraft",    ArcRewardKind.Skill,     2, Rules.Skill.Bushcraft,
            "Halves travel hazard costs; eat every other night",
            "Quarters travel hazard costs"),
        new("health",      "Constitution", ArcRewardKind.Health,    2, null,
            "Gain +1 max health",
            "Gain +1 max health"),
        new("inventory",   "Packing",      ArcRewardKind.Inventory, 2, null,
            "Gain +1 pack slot",
            "Gain +1 pack slot"),
    ];

    /// <summary>Max health gain per pick. Two picks = +2 total.</summary>
    public const int HealthPerPick = 1;

    /// <summary>Pack capacity gain per pick. Two picks = +2 slots over starting 8.</summary>
    public const int InventoryPerPick = 1;
}
