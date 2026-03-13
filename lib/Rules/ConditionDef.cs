namespace Dreamlands.Rules;

public enum ConditionSeverity { Minor, Severe }

/// <summary>Definition of a status condition.</summary>
public sealed class ConditionDef
{
    public ConditionSeverity Severity { get; init; } = ConditionSeverity.Minor;
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string Biome { get; init; } = "none";
    public string Tier { get; init; } = "none";
    public int Stacks { get; init; } = 1;
    public Magnitude? HealthDrain { get; init; }
    public Magnitude? SpiritsDrain { get; init; }
    public string? SpecialCure { get; init; }
    public string? SpecialEffect { get; init; }
    /// <summary>If true, this condition is automatically cleared when entering a settlement.</summary>
    public bool ClearedOnSettlement { get; init; }
    /// <summary>Per-condition resist DC override. Null = use AmbientResistDifficulty.</summary>
    public Difficulty? ResistDifficulty { get; init; }

    internal static IReadOnlyDictionary<string, ConditionDef> All { get; } = BuildAll();

    static Dictionary<string, ConditionDef> BuildAll() => new()
    {
        ["freezing"] = new()
        {
            Id = "freezing", Name = "Freezing", Biome = "mountains", Tier = "any",
            Stacks = 1, SpiritsDrain = Magnitude.Medium,
            ClearedOnSettlement = true,
            SpecialCure = "Leave the mountain biome or enter a settlement.",
        },
        ["thirsty"] = new()
        {
            Id = "thirsty", Name = "Thirsty", Biome = "scrub", Tier = "any",
            Stacks = 1, SpiritsDrain = Magnitude.Medium,
            ClearedOnSettlement = true,
            SpecialCure = "Enter a settlement.",
        },
        ["irradiated"] = new()
        {
            Id = "irradiated", Name = "Irradiated", Biome = "plains", Tier = "3",
            Stacks = 3, HealthDrain = Magnitude.Huge, Severity = ConditionSeverity.Severe,
        },
        ["lattice_sickness"] = new()
        {
            Id = "lattice_sickness", Name = "Lattice Sickness", Biome = "swamp", Tier = "3",
            Stacks = 3, HealthDrain = Magnitude.Huge, Severity = ConditionSeverity.Severe,
        },
        ["exhausted"] = new()
        {
            Id = "exhausted", Name = "Exhausted", Biome = "none", Tier = "none",
            Stacks = 1, SpiritsDrain = Magnitude.Medium,
            SpecialCure = "Rest in an inn.",
        },
        ["poisoned"] = new()
        {
            Id = "poisoned", Name = "Poisoned", Biome = "none", Tier = "none",
            Stacks = 3, HealthDrain = Magnitude.Huge, Severity = ConditionSeverity.Severe,
        },
        ["lost"] = new()
        {
            Id = "lost", Name = "Lost", Biome = "none", Tier = "none",
            Stacks = 1, ResistDifficulty = Difficulty.Easy,
            ClearedOnSettlement = true,
            SpecialEffect = "Erase a random number of previously-discovered map tile routes.",
        },
        ["injured"] = new()
        {
            Id = "injured", Name = "Injured", Biome = "none", Tier = "none",
            Stacks = 3, HealthDrain = Magnitude.Huge, Severity = ConditionSeverity.Severe,
        },
        ["disheartened"] = new()
        {
            Id = "disheartened", Name = "Disheartened", Biome = "none", Tier = "none",
            Stacks = 1,
            SpecialCure = "Raise spirits above 9.",
            SpecialEffect = "Disadvantage on all rolls.",
        },
    };
}
