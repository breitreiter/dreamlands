namespace Dreamlands.Rules;

/// <summary>Trade good category.</summary>
public sealed class TradeCategory
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public int BaseValue { get; init; }
    public IReadOnlyDictionary<string, double> RegionalModifiers { get; init; } = new Dictionary<string, double>();
}

/// <summary>Trade balance data.</summary>
public sealed class TradeBalance
{
    public static readonly TradeBalance Default = new();

    public IReadOnlyDictionary<string, TradeCategory> Categories { get; init; } = BuildCategories();
    public int PerLevelBonusPercent { get; init; } = 5;

    static Dictionary<string, TradeCategory> BuildCategories() => new()
    {
        ["textiles"] = new()
        {
            Id = "textiles", Name = "Textiles", BaseValue = 10,
            RegionalModifiers = new Dictionary<string, double> { ["plains"] = 0.8, ["mountains"] = 1.2 },
        },
        ["metals"] = new()
        {
            Id = "metals", Name = "Metals", BaseValue = 15,
            RegionalModifiers = new Dictionary<string, double> { ["mountains"] = 0.7, ["plains"] = 1.3 },
        },
        ["wood"] = new()
        {
            Id = "wood", Name = "Wood", BaseValue = 8,
            RegionalModifiers = new Dictionary<string, double> { ["forest"] = 0.6, ["scrub"] = 1.5 },
        },
        ["stone"] = new()
        {
            Id = "stone", Name = "Stone", BaseValue = 12,
            RegionalModifiers = new Dictionary<string, double> { ["mountains"] = 0.7, ["swamp"] = 1.4 },
        },
        ["gems"] = new()
        {
            Id = "gems", Name = "Gems", BaseValue = 50,
            RegionalModifiers = new Dictionary<string, double> { ["mountains"] = 0.8, ["plains"] = 1.1 },
        },
        ["spices"] = new()
        {
            Id = "spices", Name = "Spices", BaseValue = 20,
            RegionalModifiers = new Dictionary<string, double> { ["swamp"] = 0.7, ["mountains"] = 1.4 },
        },
        ["tools"] = new()
        {
            Id = "tools", Name = "Tools", BaseValue = 25,
            RegionalModifiers = new Dictionary<string, double> { ["mountains"] = 0.9, ["forest"] = 1.2 },
        },
        ["weapons"] = new()
        {
            Id = "weapons", Name = "Weapons", BaseValue = 40,
            RegionalModifiers = new Dictionary<string, double> { ["mountains"] = 0.85, ["plains"] = 1.15 },
        },
        ["books"] = new()
        {
            Id = "books", Name = "Books", BaseValue = 30,
            RegionalModifiers = new Dictionary<string, double> { ["settlements_with_libraries"] = 0.9, ["frontier"] = 1.3 },
        },
    };
}
