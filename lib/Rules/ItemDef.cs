namespace Dreamlands.Rules;

/// <summary>Type of equipment item.</summary>
public enum ItemType { Tool, Consumable, Weapon, Armor }

/// <summary>Definition of an equipment item.</summary>
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

    internal static IReadOnlyDictionary<string, ItemDef> All { get; } = BuildAll();

    static Dictionary<string, ItemDef> BuildAll() => new()
    {
        // Tools & Gear

        ["camping_gear"] = new()
        {
            Id = "camping_gear", Type = ItemType.Tool, BasePrice = 15, Slots = 2,
            Resists = ["cold", "exhausted"],
        },
        ["base_boots"] = new()
        {
            Id = "base_boots", Type = ItemType.Tool, Quality = "basic", BasePrice = 5, Slots = 1,
        },
        ["sturdy_boots"] = new()
        {
            Id = "sturdy_boots", Type = ItemType.Tool, Quality = "crude", BasePrice = 20, Slots = 1,
            Resists = ["exhausted"],
        },
        ["fine_boots"] = new()
        {
            Id = "fine_boots", Type = ItemType.Tool, Quality = "good", BasePrice = 60, Slots = 1,
            Resists = ["exhausted"],
        },
        ["master_boots"] = new()
        {
            Id = "master_boots", Type = ItemType.Tool, Quality = "fine", BasePrice = 150, Slots = 1,
            Resists = ["exhausted"],
        },
        ["map_kit"] = new()
        {
            Id = "map_kit", Type = ItemType.Tool, BasePrice = 20, Slots = 1,
            Resists = ["lost"],
        },
        ["warm_clothing"] = new()
        {
            Id = "warm_clothing", Type = ItemType.Tool, BasePrice = 15, Slots = 1,
            Resists = ["cold"],
        },
        ["canteen"] = new()
        {
            Id = "canteen", Type = ItemType.Tool, BasePrice = 10, Slots = 1,
            Resists = ["thirsty"],
        },
        ["mosquito_netting"] = new()
        {
            Id = "mosquito_netting", Type = ItemType.Tool, BasePrice = 12, Slots = 1,
            Resists = ["swamp_fever"],
        },
        ["warding_talisman"] = new()
        {
            Id = "warding_talisman", Type = ItemType.Tool, BasePrice = 25, Slots = 1,
            Resists = ["haunted"],
        },
        ["treated_bedroll"] = new()
        {
            Id = "treated_bedroll", Type = ItemType.Tool, BasePrice = 18, Slots = 2,
            Resists = ["infested"],
        },
        ["dust_mask"] = new()
        {
            Id = "dust_mask", Type = ItemType.Tool, BasePrice = 8, Slots = 1,
            Resists = ["rot_lung"],
        },
        ["backpack"] = new()
        {
            Id = "backpack", Type = ItemType.Tool, BasePrice = 30, Slots = 0,
            CapacityBonus = 5,
        },

        // Consumables

        ["bandages"] = new()
        {
            Id = "bandages", Type = ItemType.Consumable, BasePrice = 5, Slots = 1, StackSize = 10,
            Cures = ["injured"],
        },
        ["fever_tonic"] = new()
        {
            Id = "fever_tonic", Type = ItemType.Consumable, BasePrice = 12, Slots = 1, StackSize = 5,
            Cures = ["swamp_fever"],
        },
        ["purgative"] = new()
        {
            Id = "purgative", Type = ItemType.Consumable, BasePrice = 12, Slots = 1, StackSize = 5,
            Cures = ["infested"],
        },
        ["expectorant"] = new()
        {
            Id = "expectorant", Type = ItemType.Consumable, BasePrice = 12, Slots = 1, StackSize = 5,
            Cures = ["rot_lung"],
        },
        ["gut_remedy"] = new()
        {
            Id = "gut_remedy", Type = ItemType.Consumable, BasePrice = 10, Slots = 1, StackSize = 5,
            Cures = ["river_flux"],
        },
        ["purifying_tablets"] = new()
        {
            Id = "purifying_tablets", Type = ItemType.Consumable, BasePrice = 8, Slots = 1, StackSize = 10,
            Resists = ["river_flux"],
        },

        // Weapons

        ["base_weapon"] = new()
        {
            Id = "base_weapon", Type = ItemType.Weapon, Quality = "basic", BasePrice = 5, Slots = 2,
        },
        ["crude_weapon"] = new()
        {
            Id = "crude_weapon", Type = ItemType.Weapon, Quality = "crude", BasePrice = 15, Slots = 2,
            CombatBonus = 1,
        },
        ["good_weapon"] = new()
        {
            Id = "good_weapon", Type = ItemType.Weapon, Quality = "good", BasePrice = 50, Slots = 2,
            CombatBonus = 3,
        },
        ["fine_weapon"] = new()
        {
            Id = "fine_weapon", Type = ItemType.Weapon, Quality = "fine", BasePrice = 150, Slots = 2,
            CombatBonus = 5,
        },

        // Armor

        ["base_armor"] = new()
        {
            Id = "base_armor", Type = ItemType.Armor, Quality = "basic", BasePrice = 10, Slots = 3,
        },
        ["light_armor"] = new()
        {
            Id = "light_armor", Type = ItemType.Armor, Quality = "crude", BasePrice = 30, Slots = 3,
            CombatDefense = 2,
        },
        ["medium_armor"] = new()
        {
            Id = "medium_armor", Type = ItemType.Armor, Quality = "good", BasePrice = 100, Slots = 4,
            CombatDefense = 4,
        },
        ["heavy_armor"] = new()
        {
            Id = "heavy_armor", Type = ItemType.Armor, Quality = "fine", BasePrice = 300, Slots = 5,
            CombatDefense = 6,
        },
    };
}
