namespace Dreamlands.Encounter;

/// <summary>
/// Parsed root model for a `.fight` combat encounter (one named monster set piece).
/// Companion to <see cref="Encounter"/> for narrative encounters.
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

    /// <summary>CSS color for blood-splat hit animations. Default mammalian
    /// red; non-mammals (lattice, golem, etc.) override via `+blood &lt;hex&gt;`.</summary>
    public string BloodColor { get; set; } = "#7a0a0a";

    public MonsterStats Stats { get; set; } = new(0, 0, 0, new DiceRoll(0, 0, 0));
    public List<Hitbox> Hitboxes { get; set; } = new();
    public List<MonsterMove> Moves { get; set; } = new();

    public string Intro { get; set; } = "";
    public string WinText { get; set; } = "";
    public string LoseText { get; set; } = "";

    /// <summary>Raw mechanic strings (e.g. "gold 8", "tag killed_gorzog") run through
    /// the standard <c>Mechanics.Apply</c> pipeline on victory.</summary>
    public List<string> WinMechanics { get; set; } = new();

    /// <summary>Raw mechanic strings applied on defeat.</summary>
    public List<string> LoseMechanics { get; set; } = new();

    /// <summary>The default, always-available move (timer 0). Throws if missing.</summary>
    public MonsterMove BasicMove =>
        Moves.FirstOrDefault(m => m.Timer == 0)
            ?? throw new InvalidOperationException($"{Title}: missing basic move (timer=0).");
}

public sealed record MonsterStats(int Hp, int Ac, int ToHit, DiceRoll Damage);

public sealed record DiceRoll(int Count, int Sides, int Modifier)
{
    public int Roll(Random rng)
    {
        int total = Modifier;
        for (int i = 0; i < Count; i++) total += rng.Next(1, Sides + 1);
        return total;
    }

    public int Min => Count + Modifier;
    public int Max => Count * Sides + Modifier;

    public override string ToString() =>
        Count == 0
            ? Modifier.ToString("+0;-0;0")
            : Modifier == 0
                ? $"{Count}d{Sides}"
                : $"{Count}d{Sides}{(Modifier >= 0 ? "+" : "")}{Modifier}";
}

public sealed record Hitbox(string Id, double Left, double Top, double Right, double Bottom);

public enum IntentClass { Attack, HeavyAttack, Defend, Pierce, Condition, Flee }

public sealed class MonsterMove
{
    public string Id { get; set; } = "";
    public IntentClass IntentClass { get; set; }
    public string IntentText { get; set; } = "";

    /// <summary>Cooldown, in turns. 0 = the basic (always-available) move.</summary>
    public int Timer { get; set; }

    public string Sprite { get; set; } = "";
    public string Anchor { get; set; } = "center";
    public string Narration { get; set; } = "";
    public List<MoveMechanic> Mechanics { get; set; } = new();

    public bool IsBasic => Timer == 0;
}

/// <summary>One line in a monster move's mechanics list (e.g. "deal_damage 1d8").</summary>
public sealed record MoveMechanic(string Verb, string Args)
{
    public override string ToString() => string.IsNullOrEmpty(Args) ? Verb : $"{Verb} {Args}";
}
