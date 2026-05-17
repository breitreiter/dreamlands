namespace Dreamlands.Rules;

public enum ArcRewardKind { Skill, Health, Inventory }

public record ArcRewardSlot(string Id, string Label, ArcRewardKind Kind, int Cap, Skill? Skill);

/// <summary>
/// Static definition of the arc-completion tableau: what can be picked, caps, and magnitudes.
/// </summary>
public static class ArcRewards
{
    public static readonly ArcRewardSlot[] All =
    [
        new("combat",      "Combat",      ArcRewardKind.Skill,     2, Rules.Skill.Combat),
        new("negotiation", "Negotiation", ArcRewardKind.Skill,     2, Rules.Skill.Negotiation),
        new("cunning",     "Cunning",     ArcRewardKind.Skill,     2, Rules.Skill.Cunning),
        new("bushcraft",   "Bushcraft",   ArcRewardKind.Skill,     2, Rules.Skill.Bushcraft),
        new("health",      "Max Health",  ArcRewardKind.Health,    2, null),
        new("inventory",   "Pack Slots",  ArcRewardKind.Inventory, 2, null),
    ];

    /// <summary>Max health gain per pick. Two picks = +2 total.</summary>
    public const int HealthPerPick = 1;

    /// <summary>Pack capacity gain per pick. Two picks = +2 slots over starting 8.</summary>
    public const int InventoryPerPick = 1;

    /// <summary>Display string for the pick effect shown in the tableau UI.</summary>
    public static string PickEffect(ArcRewardSlot slot) => slot.Kind switch
    {
        ArcRewardKind.Skill     => "+1 tier",
        ArcRewardKind.Health    => $"+{HealthPerPick} max health",
        ArcRewardKind.Inventory => $"+{InventoryPerPick} pack slot",
        _                       => "",
    };
}
