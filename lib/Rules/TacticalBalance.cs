namespace Dreamlands.Rules;

/// <summary>
/// A card contributed by an item or skill for tactical encounter deckbuilding.
/// Name is the display name. Archetype is the canonical template ID (e.g. "momentum_to_progress").
/// </summary>
public sealed record TacticalCard(string Name, string Archetype);

/// <summary>Canonical archetype: defines cost and effect for a card template.</summary>
public sealed record TacticalArchetype(string Id, string CostKind, int CostAmount, string EffectKind, int EffectAmount);

/// <summary>Flavor name set for an archetype, keyed by encounter subtype.</summary>
public sealed record ArchetypeFlavor(string Fight, string Debate, string Sneak, string Navigate);

/// <summary>Balance data for the tactical encounter deckbuilding system.</summary>
public sealed class TacticalBalance
{
    public static readonly TacticalBalance Default = new();

    public int DeckSize { get; init; } = 15;

    /// <summary>Archetype definitions: template ID → cost/effect.</summary>
    public IReadOnlyDictionary<string, TacticalArchetype> Archetypes { get; init; } = BuildArchetypes();

    /// <summary>Flavor names per archetype per encounter subtype.</summary>
    public IReadOnlyDictionary<string, ArchetypeFlavor> Flavors { get; init; } = BuildFlavors();

    /// <summary>Skill-intrinsic cards. Key = skill, value = cards in level order (index 0 = level 1).</summary>
    public IReadOnlyDictionary<Skill, IReadOnlyList<TacticalCard>> SkillCards { get; init; } = BuildSkillCards();

    /// <summary>Global chaff archetypes used to pad decks.</summary>
    public IReadOnlyList<string> Chaff { get; init; } = BuildChaff();

    static Dictionary<string, TacticalArchetype> BuildArchetypes() => new()
    {
        // Progress cards
        ["free_progress_small"] = new("free_progress_small", "free", 0, "damage", 1),
        ["momentum_to_progress"] = new("momentum_to_progress", "momentum", 1, "damage", 2),
        ["momentum_to_progress_large"] = new("momentum_to_progress_large", "momentum", 2, "damage", 3),
        ["momentum_to_progress_huge"] = new("momentum_to_progress_huge", "momentum", 3, "damage", 5),
        ["spirits_to_progress"] = new("spirits_to_progress", "spirits", 1, "damage", 3),
        ["spirits_to_progress_large"] = new("spirits_to_progress_large", "spirits", 2, "damage", 5),
        ["threat_to_progress"] = new("threat_to_progress", "tick", 0, "damage", 2),
        ["threat_to_progress_large"] = new("threat_to_progress_large", "tick", 0, "damage", 3),

        // Momentum cards
        ["free_momentum_small"] = new("free_momentum_small", "free", 0, "momentum", 1),
        ["free_momentum"] = new("free_momentum", "free", 0, "momentum", 2),
        ["threat_to_momentum"] = new("threat_to_momentum", "tick", 0, "momentum", 2),
        ["spirits_to_momentum"] = new("spirits_to_momentum", "spirits", 1, "momentum", 3),

        // Stop-threat cards
        ["momentum_to_cancel"] = new("momentum_to_cancel", "momentum", 2, "stop_timer", 0),
        ["spirits_to_cancel"] = new("spirits_to_cancel", "spirits", 1, "stop_timer", 0),
        ["free_cancel"] = new("free_cancel", "free", 0, "stop_timer", 0),
    };

    static Dictionary<string, ArchetypeFlavor> BuildFlavors() => new()
    {
        ["free_progress_small"] = new("Jab", "Pointed Remark", "Inch Forward", "Careful Step"),
        ["momentum_to_progress"] = new("Slash", "Sharp Rebuke", "Slip Past", "Scramble Across"),
        ["momentum_to_progress_large"] = new("Cleave", "Damning Evidence", "Sprint Between Cover", "Power Through"),
        ["momentum_to_progress_huge"] = new("Haymaker", "Closing Argument", "Ghost Through", "Leap of Faith"),
        ["spirits_to_progress"] = new("Reckless Lunge", "Bold Claim", "Brazen Dash", "Force the Crossing"),
        ["spirits_to_progress_large"] = new("Death Blow", "Bare Your Soul", "Now or Never", "Last Reserves"),
        ["threat_to_progress"] = new("Press Attack", "Talk Past Them", "Ignore the Noise", "Ignore the Signs"),
        ["threat_to_progress_large"] = new("All-Out Assault", "Inflammatory Accusation", "Run For It", "Charge Ahead"),
        ["free_momentum_small"] = new("Test Guard", "Feel Them Out", "Watch the Pattern", "Read the Terrain"),
        ["free_momentum"] = new("Feint", "Build Your Case", "Time Their Rounds", "Find Your Footing"),
        ["threat_to_momentum"] = new("Overextend", "Give Them Rope", "Tune It Out", "Press On Regardless"),
        ["spirits_to_momentum"] = new("Second Wind", "Swallow Your Pride", "Steady Your Nerves", "Grit Your Teeth"),
        ["momentum_to_cancel"] = new("Parry", "Objection", "Evade", "Brace"),
        ["spirits_to_cancel"] = new("Desperate Parry", "Concede the Point", "Desperate Dodge", "Dig In"),
        ["free_cancel"] = new("Perfect Counter", "Checkmate", "Vanish", "Safe Ground"),
    };

    static Dictionary<Skill, IReadOnlyList<TacticalCard>> BuildSkillCards() => new()
    {
        [Skill.Combat] = new TacticalCard[]
        {
            new("Zornhau", "momentum_to_progress"),
            new("Nachdrängen", "spirits_to_momentum"),
            new("Überlaufen", "threat_to_progress"),
            new("Scheitelhau", "momentum_to_progress_large"),
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
    };

    static IReadOnlyList<string> BuildChaff() =>
    [
        "free_progress_small",
        "free_momentum_small",
        "threat_to_momentum",
        "momentum_to_progress",
        "spirits_to_progress",
        "free_momentum_small",
        "free_progress_small",
        "spirits_to_momentum",
    ];
}
