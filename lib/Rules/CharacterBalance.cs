using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Dreamlands.Rules;

/// <summary>Magnitude-to-integer tables and starting stats from character.yaml.</summary>
public sealed class CharacterBalance
{
    public IReadOnlyDictionary<Magnitude, int> DamageMagnitudes { get; init; } = new Dictionary<Magnitude, int>();
    public IReadOnlyDictionary<Magnitude, int> SkillBumpMagnitudes { get; init; } = new Dictionary<Magnitude, int>();
    public int StartingHealth { get; init; }
    public int StartingSpirits { get; init; }
    public int StartingGold { get; init; }
    public int StartingInventorySlots { get; init; }
    public IReadOnlyList<SpiritsThreshold> SpiritsThresholds { get; init; } = [];
    public int UsesPerLevel { get; init; }
    public int MaxSkillLevel { get; init; }

    internal static CharacterBalance Load(string balancePath)
    {
        var path = Path.Combine(balancePath, "character.yaml");
        if (!File.Exists(path)) return new CharacterBalance();

        var yaml = File.ReadAllText(path);
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();

        var doc = deserializer.Deserialize<CharacterDoc>(yaml);
        var c = doc.Character;
        var s = doc.Skills;

        return new CharacterBalance
        {
            DamageMagnitudes = ParseMagnitudeTable(c.Damage),
            SkillBumpMagnitudes = ParseMagnitudeTable(s.BumpSkill),
            StartingHealth = c.StartingStats.Health,
            StartingSpirits = c.StartingStats.Spirits,
            StartingGold = c.StartingStats.Gold,
            StartingInventorySlots = c.StartingStats.InventorySlots,
            SpiritsThresholds = c.SpiritsPenalties.Thresholds
                .Select(t => new SpiritsThreshold(t.Spirits, t.Penalty))
                .OrderByDescending(t => t.AtOrBelow)
                .ToList(),
            UsesPerLevel = s.Improvement.UsesPerLevel,
            MaxSkillLevel = s.Improvement.MaxSkillLevel,
        };
    }

    static Dictionary<Magnitude, int> ParseMagnitudeTable(Dictionary<string, int> raw)
    {
        var result = new Dictionary<Magnitude, int>();
        foreach (var (key, value) in raw)
        {
            var mag = Magnitudes.FromScriptName(key);
            if (mag != null) result[mag.Value] = value;
        }
        return result;
    }

    // DTOs
    class CharacterDoc
    {
        public CharacterYaml Character { get; set; } = new();
        public SkillsYaml Skills { get; set; } = new();
    }
    class CharacterYaml
    {
        public Dictionary<string, int> Damage { get; set; } = new();
        public StartingStatsYaml StartingStats { get; set; } = new();
        public SpiritsPenaltiesYaml SpiritsPenalties { get; set; } = new();
    }
    class StartingStatsYaml
    {
        public int Health { get; set; }
        public int Spirits { get; set; }
        public int Gold { get; set; }
        public int InventorySlots { get; set; }
    }
    class SpiritsPenaltiesYaml
    {
        public List<ThresholdYaml> Thresholds { get; set; } = new();
    }
    class ThresholdYaml
    {
        public int Spirits { get; set; }
        public int Penalty { get; set; }
    }
    class SkillsYaml
    {
        public ImprovementYaml Improvement { get; set; } = new();
        public Dictionary<string, int> BumpSkill { get; set; } = new();
    }
    class ImprovementYaml
    {
        public int UsesPerLevel { get; set; }
        public int MaxSkillLevel { get; set; }
    }
}

/// <summary>Spirits penalty threshold: if spirits &lt;= AtOrBelow, apply Penalty to checks.</summary>
public readonly record struct SpiritsThreshold(int AtOrBelow, int Penalty);
