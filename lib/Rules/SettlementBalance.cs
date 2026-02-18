namespace Dreamlands.Rules;

/// <summary>Storage tier definition.</summary>
public readonly record struct StorageTier(string Id, int Cost, int Slots);

/// <summary>Settlement service definition.</summary>
public sealed class ServiceDef
{
    public string Id { get; init; } = "";
    public string Availability { get; init; } = "common";
    public bool AlwaysAvailable { get; init; }
    public IReadOnlyList<StorageTier> StorageTiers { get; init; } = [];
}

/// <summary>Regional specialty definition.</summary>
public sealed class RegionalSpecialty
{
    public string Id { get; init; } = "";
    public IReadOnlyList<string> Sells { get; init; } = [];
    public double PriceModifier { get; init; } = 1.0;
}

/// <summary>Settlement balance data.</summary>
public sealed class SettlementBalance
{
    public static readonly SettlementBalance Default = new();

    public IReadOnlyDictionary<string, ServiceDef> Services { get; init; } = BuildServices();
    public IReadOnlyDictionary<string, RegionalSpecialty> RegionalSpecialties { get; init; } = BuildSpecialties();

    static Dictionary<string, ServiceDef> BuildServices() => new()
    {
        ["market"] = new() { Id = "market", Availability = "universal", AlwaysAvailable = true },
        ["water_source"] = new() { Id = "water_source", Availability = "universal", AlwaysAvailable = true },
        ["storage"] = new()
        {
            Id = "storage", Availability = "common",
            StorageTiers = [
                new("basic", 10, 10),
                new("expanded", 50, 25),
                new("large", 150, 50),
                new("warehouse", 400, 100),
            ],
        },
        ["healer"] = new() { Id = "healer", Availability = "rare" },
        ["temple"] = new() { Id = "temple", Availability = "rare" },
        ["entertainment"] = new() { Id = "entertainment", Availability = "tbd" },
    };

    static Dictionary<string, RegionalSpecialty> BuildSpecialties() => new()
    {
        ["swamp_adjacent"] = new()
        {
            Id = "swamp_adjacent", Sells = ["mosquito_netting", "jorgo_root"], PriceModifier = 0.9,
        },
        ["mountain_settlements"] = new()
        {
            Id = "mountain_settlements", Sells = ["warm_clothing", "warming_stew"], PriceModifier = 0.8,
        },
        ["plains_temples"] = new()
        {
            Id = "plains_temples", Sells = ["warding_talisman"], PriceModifier = 1.0,
        },
        ["forest_adjacent"] = new()
        {
            Id = "forest_adjacent", Sells = ["treated_bedroll"], PriceModifier = 0.9,
        },
    };
}
