namespace Dreamlands.Rules;

/// <summary>Definition of a status condition.</summary>
public sealed class ConditionDef
{
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
            Stacks = 1, HealthDrain = Magnitude.Trivial, SpiritsDrain = Magnitude.Small,
            ClearedOnSettlement = true,
            SpecialCure = "Leave the mountain biome or enter a settlement.",
        },
        ["hungry"] = new()
        {
            Id = "hungry", Name = "Hungry", Biome = "none", Tier = "none",
            Stacks = 3, HealthDrain = Magnitude.Trivial, SpiritsDrain = Magnitude.Trivial,
        },
        ["thirsty"] = new()
        {
            Id = "thirsty", Name = "Thirsty", Biome = "scrub", Tier = "any",
            Stacks = 1, HealthDrain = Magnitude.Small, SpiritsDrain = Magnitude.Small,
            ClearedOnSettlement = true,
            SpecialCure = "Enter a settlement.",
        },
        ["swamp_fever"] = new()
        {
            Id = "swamp_fever", Name = "Swamp Fever", Biome = "swamp", Tier = "any",
            Stacks = 4, HealthDrain = Magnitude.Trivial, SpiritsDrain = Magnitude.Trivial,
        },
        ["gut_worms"] = new()
        {
            Id = "gut_worms", Name = "Gut Worms", Biome = "forest", Tier = "2",
            Stacks = 2, HealthDrain = Magnitude.Trivial, SpiritsDrain = Magnitude.Trivial,
            ResistDifficulty = Difficulty.Easy,
        },
        ["irradiated"] = new()
        {
            Id = "irradiated", Name = "Irradiated", Biome = "plains", Tier = "3",
            HealthDrain = Magnitude.Medium, SpiritsDrain = Magnitude.Small,
        },
        ["exhausted"] = new()
        {
            Id = "exhausted", Name = "Exhausted", Biome = "none", Tier = "none",
            Stacks = 1, SpiritsDrain = Magnitude.Small,
            SpecialCure = "Rest in an inn.",
        },
        ["poisoned"] = new()
        {
            Id = "poisoned", Name = "Poisoned", Biome = "none", Tier = "none",
            Stacks = 3, HealthDrain = Magnitude.Small,
        },
        ["lost"] = new()
        {
            Id = "lost", Name = "Lost", Biome = "none", Tier = "none",
            Stacks = 1, ResistDifficulty = Difficulty.Easy,
            SpecialEffect = "Erase a random number of previously-discovered map tile routes.",
        },
        ["injured"] = new()
        {
            Id = "injured", Name = "Injured", Biome = "none", Tier = "none",
            Stacks = 3, HealthDrain = Magnitude.Small, SpiritsDrain = Magnitude.Trivial,
        },
        ["disheartened"] = new()
        {
            Id = "disheartened", Name = "Disheartened", Biome = "none", Tier = "none",
            Stacks = 1,
            SpecialEffect = "Disadvantage on all rolls.",
        },
    };
}
