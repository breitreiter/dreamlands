using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Dreamlands.Rules;

/// <summary>Type of equipment item.</summary>
public enum ItemType { Tool, Consumable, Weapon, Armor }

/// <summary>Definition of an equipment item from equipment.yaml.</summary>
public sealed class ItemDef
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public ItemType Type { get; init; }
    public int BasePrice { get; init; }
    public int Slots { get; init; }
    public int StackSize { get; init; } = 1;
    public string? Quality { get; init; }
    public int CombatBonus { get; init; }
    public int CombatDefense { get; init; }
    public int CapacityBonus { get; init; }
    public IReadOnlyList<string> Resists { get; init; } = [];
    public IReadOnlyList<string> Cures { get; init; } = [];

    internal static IReadOnlyDictionary<string, ItemDef> Load(string balancePath)
    {
        var path = Path.Combine(balancePath, "equipment.yaml");
        if (!File.Exists(path)) return new Dictionary<string, ItemDef>();

        var yaml = File.ReadAllText(path);
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();

        var doc = deserializer.Deserialize<EquipmentDoc>(yaml);
        if (doc?.Equipment == null) return new Dictionary<string, ItemDef>();

        var result = new Dictionary<string, ItemDef>();
        foreach (var (id, item) in doc.Equipment)
        {
            var type = ParseItemType(item.Type);
            var resists = new List<string>();
            var cures = new List<string>();
            int combatBonus = 0, combatDefense = 0, capacityBonus = 0;

            if (item.Effects != null)
            {
                foreach (var effect in item.Effects)
                {
                    if (effect.Resists != null) resists.AddRange(effect.Resists);
                    if (effect.Cures != null) cures.AddRange(effect.Cures);
                    if (effect.CombatBonus > 0) combatBonus = effect.CombatBonus;
                    if (effect.CombatDefense > 0) combatDefense = effect.CombatDefense;
                    if (effect.CapacityBonus > 0) capacityBonus = effect.CapacityBonus;
                }
            }

            result[id] = new ItemDef
            {
                Id = id,
                Name = item.Name ?? id,
                Type = type,
                BasePrice = item.BasePrice,
                Slots = item.Slots,
                StackSize = item.StackSize > 0 ? item.StackSize : 1,
                Quality = item.Quality,
                CombatBonus = combatBonus,
                CombatDefense = combatDefense,
                CapacityBonus = capacityBonus,
                Resists = resists,
                Cures = cures,
            };
        }
        return result;
    }

    static ItemType ParseItemType(string? type) => type switch
    {
        "tool" => ItemType.Tool,
        "consumable" => ItemType.Consumable,
        "weapon" => ItemType.Weapon,
        "armor" => ItemType.Armor,
        _ => ItemType.Tool,
    };

    // DTOs
    class EquipmentDoc
    {
        public Dictionary<string, ItemYaml> Equipment { get; set; } = new();
    }
    class ItemYaml
    {
        public string? Name { get; set; }
        public string? Type { get; set; }
        public string? Quality { get; set; }
        public int BasePrice { get; set; }
        public int Slots { get; set; }
        public int StackSize { get; set; }
        public string? Availability { get; set; }
        public List<EffectYaml>? Effects { get; set; }
    }
    class EffectYaml
    {
        public List<string>? Resists { get; set; }
        public List<string>? Cures { get; set; }
        public int CombatBonus { get; set; }
        public int CombatDefense { get; set; }
        public int CapacityBonus { get; set; }
    }
}
