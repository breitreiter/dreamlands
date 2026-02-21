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

    internal static IReadOnlyDictionary<string, ConditionDef> All { get; } = BuildAll();

    static Dictionary<string, ConditionDef> BuildAll() => new()
    {
        ["freezing"] = new()
        {
            Id = "freezing", Name = "Freezing", Biome = "mountains", Tier = "any",
            Stacks = 1, HealthDrain = Magnitude.Trivial, SpiritsDrain = Magnitude.Small,
            SpecialCure = "Leave the mountain biome or enter a settlement.",
        },
        ["hungry"] = new()
        {
            Id = "hungry", Name = "Hungry", Biome = "none", Tier = "none",
            Stacks = 2, HealthDrain = Magnitude.Trivial, SpiritsDrain = Magnitude.Trivial,
        },
        ["thirsty"] = new()
        {
            Id = "thirsty", Name = "Thirsty", Biome = "scrub", Tier = "any",
            Stacks = 1, HealthDrain = Magnitude.Small, SpiritsDrain = Magnitude.Small,
            SpecialCure = "Enter a settlement.",
        },
        ["swamp_fever"] = new()
        {
            Id = "swamp_fever", Name = "Swamp Fever", Biome = "swamp", Tier = "any",
            Stacks = 4, HealthDrain = Magnitude.Trivial, SpiritsDrain = Magnitude.Trivial,
        },
        ["road_flux"] = new()
        {
            Id = "road_flux", Name = "Road Flux", Biome = "none", Tier = "none",
            Stacks = 2, HealthDrain = Magnitude.Trivial, SpiritsDrain = Magnitude.Trivial,
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
            Stacks = 1,
            SpecialEffect = "Erase a random number of previously-discovered map tile routes.",
        },
        ["injured"] = new()
        {
            Id = "injured", Name = "Injured", Biome = "none", Tier = "none",
            Stacks = 3, HealthDrain = Magnitude.Small, SpiritsDrain = Magnitude.Trivial,
        },
    };
}
