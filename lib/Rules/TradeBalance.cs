using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Dreamlands.Rules;

/// <summary>Trade good category from trade.yaml.</summary>
public sealed class TradeCategory
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public int BaseValue { get; init; }
    public IReadOnlyDictionary<string, double> RegionalModifiers { get; init; } = new Dictionary<string, double>();
    public IReadOnlyList<string> FlavorItems { get; init; } = [];
}

/// <summary>Trade balance data from trade.yaml.</summary>
public sealed class TradeBalance
{
    public IReadOnlyDictionary<string, TradeCategory> Categories { get; init; } = new Dictionary<string, TradeCategory>();
    public int PerLevelBonusPercent { get; init; } = 5;

    internal static TradeBalance Load(string balancePath)
    {
        var path = Path.Combine(balancePath, "trade.yaml");
        if (!File.Exists(path)) return new TradeBalance();

        var yaml = File.ReadAllText(path);
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();

        var doc = deserializer.Deserialize<TradeDoc>(yaml);
        if (doc?.Trade == null) return new TradeBalance();
        var t = doc.Trade;

        var categories = new Dictionary<string, TradeCategory>();
        if (t.Categories != null)
        {
            foreach (var (id, cat) in t.Categories)
            {
                categories[id] = new TradeCategory
                {
                    Id = id,
                    Name = cat.Name ?? id,
                    BaseValue = cat.BaseValue,
                    RegionalModifiers = cat.RegionalModifiers ?? new Dictionary<string, double>(),
                    FlavorItems = cat.FlavorItems ?? [],
                };
            }
        }

        return new TradeBalance
        {
            Categories = categories,
            PerLevelBonusPercent = 5,
        };
    }

    // DTOs
    class TradeDoc { public TradeYaml? Trade { get; set; } }
    class TradeYaml
    {
        public Dictionary<string, TradeCatYaml>? Categories { get; set; }
        public PricingYaml? Pricing { get; set; }
    }
    class TradeCatYaml
    {
        public string? Name { get; set; }
        public int BaseValue { get; set; }
        public Dictionary<string, double>? RegionalModifiers { get; set; }
        public List<string>? FlavorItems { get; set; }
    }
    class PricingYaml
    {
        public Dictionary<string, int>? TransactionScale { get; set; }
        public Dictionary<string, object>? TradingSkillBonus { get; set; }
    }
}
