namespace Dreamlands.Encounter;

/// <summary>
/// Parsed root model for a `.fight` combat encounter. Companion to
/// <see cref="Encounter"/> for narrative encounters.
/// </summary>
public sealed class CombatEncounter
{
    /// <summary>Bundle id; set by the loader, not the parser.</summary>
    public string Id { get; set; } = "";

    /// <summary>Bundle category (e.g. "plains/tier1"); set by the loader.</summary>
    public string Category { get; set; } = "";

    public int? Tier { get; set; }

    public string Title { get; set; } = "";
    public string Image { get; set; } = "";
    public bool Repool { get; set; } = false;

    /// <summary>CSS color for blood-splat hit animations. Default mammalian red;
    /// non-mammals (lattice, golem, etc.) override via <c>+blood &lt;hex&gt;</c>.</summary>
    public string BloodColor { get; set; } = "#7a0a0a";

    public MonsterStats Stats { get; set; } = new(0);
    public List<MonsterMoveDef> Moves { get; set; } = new();

    public string Intro { get; set; } = "";
    public string WinText { get; set; } = "";
    public string LoseText { get; set; } = "";

    /// <summary>Raw mechanic strings (e.g. "gold 8", "tag killed_gorzog") run through
    /// the standard <c>Mechanics.Apply</c> pipeline on victory.</summary>
    public List<string> WinMechanics { get; set; } = new();

    /// <summary>Raw mechanic strings applied on defeat.</summary>
    public List<string> LoseMechanics { get; set; } = new();
}

public sealed record MonsterStats(int Hp);

/// <summary>
/// One entry in a monster's move pool. The mechanical action is the <see cref="Action"/>;
/// <see cref="NarrationVariants"/> is one or more flavour lines, picked at commit time
/// so two `Attack` entries with different narration give the encounter texture.
/// </summary>
public sealed class MonsterMoveDef
{
    public Move Action { get; set; } = new();
    public List<string> NarrationVariants { get; set; } = new();

    public string PickNarration(Random rng) =>
        NarrationVariants.Count == 0
            ? ""
            : NarrationVariants[rng.Next(NarrationVariants.Count)];
}
