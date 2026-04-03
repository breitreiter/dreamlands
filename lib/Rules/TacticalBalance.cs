namespace Dreamlands.Rules;

/// <summary>
/// A card contributed by an item or skill for tactical encounter deckbuilding.
/// Name is the display name. Archetype is the canonical template ID (e.g. "momentum_to_progress").
/// </summary>
public sealed record TacticalCard(string Name, string Archetype);

/// <summary>Canonical archetype: defines cost and effect for a card template.</summary>
public sealed record TacticalArchetype(string Id, string CostKind, int CostAmount, string EffectKind, int EffectAmount);

/// <summary>Balance data for the tactical encounter deckbuilding system.</summary>
public sealed class TacticalBalance
{
    public static readonly TacticalBalance Default = new();

    public int DeckSize { get; init; } = 15;
    public int PressAdvantageCost { get; init; } = 2;
    public int ForceOpeningCost { get; init; } = 2;

    /// <summary>Archetype definitions: template ID → cost/effect.</summary>
    public IReadOnlyDictionary<string, TacticalArchetype> Archetypes { get; init; } = BuildArchetypes();

    /// <summary>Skill-intrinsic cards. Key = skill, value = cards in level order (index 0 = level 1).</summary>
    public IReadOnlyDictionary<Skill, IReadOnlyList<TacticalCard>> SkillCards { get; init; } = BuildSkillCards();

    static Dictionary<string, TacticalArchetype> BuildArchetypes() => new()
    {
        // Progress cards
        ["free_progress_small"] = new("free_progress_small", "free", 0, "damage", 1),
        ["momentum_to_progress"] = new("momentum_to_progress", "momentum", 1, "damage", 2),
        ["momentum_to_progress_large"] = new("momentum_to_progress_large", "momentum", 2, "damage", 4),
        ["momentum_to_progress_huge"] = new("momentum_to_progress_huge", "momentum", 3, "damage", 6),
        ["spirits_to_progress"] = new("spirits_to_progress", "spirits", 1, "damage", 3),
        ["spirits_to_progress_large"] = new("spirits_to_progress_large", "spirits", 3, "damage", 6),
        ["threat_to_progress"] = new("threat_to_progress", "tick", 1, "damage", 2),
        ["threat_to_progress_large"] = new("threat_to_progress_large", "tick", 2, "damage", 4),

        // Momentum cards
        ["free_momentum_small"] = new("free_momentum_small", "free", 0, "momentum", 1),
        ["free_momentum"] = new("free_momentum", "free", 0, "momentum", 2),
        ["threat_to_momentum"] = new("threat_to_momentum", "tick", 1, "momentum", 3),
        ["spirits_to_momentum"] = new("spirits_to_momentum", "spirits", 1, "momentum", 3),

        // Stop-threat cards
        ["momentum_to_cancel"] = new("momentum_to_cancel", "momentum", 3, "stop_timer", 0),
        ["spirits_to_cancel"] = new("spirits_to_cancel", "spirits", 4, "stop_timer", 0),
        ["free_cancel"] = new("free_cancel", "free", 0, "stop_timer", 0),
    };

    static Dictionary<Skill, IReadOnlyList<TacticalCard>> BuildSkillCards() => new()
    {
        [Skill.Combat] = new TacticalCard[]
        {
            new("Zornhau - strike with iron wrath", "momentum_to_progress"),
            new("Nachdrängen - seize the initiative", "spirits_to_momentum"),
            new("Überlaufen - strike without fear or hesitation", "threat_to_progress"),
            new("Scheitelhau - the inevitable end", "momentum_to_progress_large"),
        },
        [Skill.Negotiation] = new TacticalCard[]
        {
            new("Push back on that", "momentum_to_progress"),
            new("Change the subject", "free_momentum"),
            new("Appeal to a higher authority", "threat_to_progress"),
            new("Present the inevitable conclusion", "momentum_to_progress_huge"),
        },
        [Skill.Cunning] = new TacticalCard[]
        {
            new("Steady yourself", "free_momentum"),
            new("Exploit a distraction", "momentum_to_progress"),
            new("Find the hidden path", "momentum_to_cancel"),
            new("Dash directly through", "threat_to_progress_large"),
        },
        [Skill.Bushcraft] = new TacticalCard[]
        {
            new("Pick a path", "free_progress_small"),
            new("Read the terrain", "free_momentum"),
            new("Find your footing", "momentum_to_progress_large"),
            new("Push through it", "momentum_to_progress_huge"),
        },
        [Skill.Luck] = new TacticalCard[]
        {
            new("A fortunate stumble", "free_progress_small"),
            new("Something catches your eye", "free_momentum"),
            new("Unlikely timing", "momentum_to_cancel"),
            new("Against all odds", "spirits_to_progress_large"),
        },
        [Skill.Mercantile] = new TacticalCard[]
        {
            new("Name your price", "momentum_to_progress"),
            new("Sweeten the deal", "free_momentum"),
            new("Call in a favour", "threat_to_progress"),
            new("Make an offer they can't refuse", "momentum_to_progress_huge"),
        },
    };
}
