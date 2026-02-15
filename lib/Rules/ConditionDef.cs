using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Dreamlands.Rules;

/// <summary>Definition of a status condition from conditions.yaml.</summary>
public sealed class ConditionDef
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string? Type { get; init; }
    public string Biome { get; init; } = "none";
    public string Tier { get; init; } = "none";
    public string Drains { get; init; } = "health";
    public Magnitude DrainMagnitude { get; init; }
    public double OvernightChance { get; init; }
    public IReadOnlyList<string> ResistedBy { get; init; } = [];
    public string? CuredBy { get; init; }
    public string? AutoClearOnExit { get; init; }
    public string? Foreshadow { get; init; }

    internal static IReadOnlyDictionary<string, ConditionDef> Load(string balancePath)
    {
        var path = Path.Combine(balancePath, "conditions.yaml");
        if (!File.Exists(path)) return new Dictionary<string, ConditionDef>();

        var yaml = File.ReadAllText(path);
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();

        var doc = deserializer.Deserialize<ConditionsDoc>(yaml);
        if (doc?.Conditions == null) return new Dictionary<string, ConditionDef>();

        var result = new Dictionary<string, ConditionDef>();
        foreach (var (id, c) in doc.Conditions)
        {
            var drainMag = Magnitudes.FromScriptName(c.Drain ?? "small") ?? Magnitude.Small;
            var resistedBy = c.ResistedBy ?? [];

            result[id] = new ConditionDef
            {
                Id = id,
                Name = c.Name ?? id,
                Type = c.Type,
                Biome = c.Biome ?? "none",
                Tier = c.Tier ?? "none",
                Drains = c.Drains ?? "health",
                DrainMagnitude = drainMag,
                OvernightChance = c.OvernightChance,
                ResistedBy = resistedBy,
                CuredBy = c.CuredBy,
                AutoClearOnExit = c.AutoClearOnExit,
                Foreshadow = c.Foreshadow,
            };
        }
        return result;
    }

    // DTOs
    class ConditionsDoc
    {
        public Dictionary<string, ConditionYaml> Conditions { get; set; } = new();
    }
    class ConditionYaml
    {
        public string? Name { get; set; }
        public string? Type { get; set; }
        public string? Biome { get; set; }
        public string? Tier { get; set; }
        public string? Drains { get; set; }
        public string? Drain { get; set; }
        public double OvernightChance { get; set; }
        public List<string>? ResistedBy { get; set; }
        public string? CuredBy { get; set; }
        public string? AutoClearOnExit { get; set; }
        public string? Foreshadow { get; set; }
        public string? Note { get; set; }
    }
}
