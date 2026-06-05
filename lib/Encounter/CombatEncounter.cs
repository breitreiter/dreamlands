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

    /// <summary>Where this fight fires: "road" (random travel pool) or "none"
    /// (arc-launched only, the default).</summary>
    public string Trigger { get; set; } = "none";

    /// <summary>Optional combat backdrop override (relative to assets/vignettes/,
    /// no extension — same space as .enc [vignette]). Empty = biome/tier default.</summary>
    public string Background { get; set; } = "";

    /// <summary>Never retired by winning — keeps spawning until a [requires]
    /// gate disqualifies it. Validation demands a gate on persistent fights.</summary>
    public bool Persistent { get; set; } = false;

    /// <summary>Raw condition strings gating spawn eligibility (AND together),
    /// same vocabulary as .enc [requires].</summary>
    public List<string> Requires { get; set; } = new();

    /// <summary>CSS color for blood-splat hit animations. Default mammalian red;
    /// non-mammals (lattice, golem, etc.) override via <c>+blood &lt;hex&gt;</c>.</summary>
    public string BloodColor { get; set; } = "#7a0a0a";

    public MonsterStats Stats { get; set; } = new(0);
    public List<MonsterMoveDef> Moves { get; set; } = new();

    public string Intro { get; set; } = "";
    public string WinText { get; set; } = "";
    public string LoseText { get; set; } = "";
    public string FleeText { get; set; } = "";

    /// <summary>Raw mechanic strings (e.g. "gold 8", "tag killed_gorzog") run through
    /// the standard <c>Mechanics.Apply</c> pipeline on victory.</summary>
    public List<string> WinMechanics { get; set; } = new();

    /// <summary>Raw mechanic strings applied on defeat.</summary>
    public List<string> LoseMechanics { get; set; } = new();

    /// <summary>Raw mechanic strings applied when the player flees.</summary>
    public List<string> FleeMechanics { get; set; } = new();
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
