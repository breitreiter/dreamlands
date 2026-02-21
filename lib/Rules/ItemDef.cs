namespace Dreamlands.Rules;

/// <summary>Type of equipment item.</summary>
public enum ItemType { Tool, Consumable, Weapon, Armor, Boots }

/// <summary>Weapon class for weapon-type items.</summary>
public enum WeaponClass { Dagger, Axe, Sword }

/// <summary>Definition of an equipment item.</summary>
public sealed class ItemDef
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public ItemType Type { get; init; }
    public int Slots { get; init; } = 1;
    public int StackSize { get; init; } = 1;
    public int CapacityBonus { get; init; }
    public IReadOnlyDictionary<string, Magnitude> Cures { get; init; } = new Dictionary<string, Magnitude>();

    public WeaponClass? WeaponClass { get; init; }
    public Magnitude? Cost { get; init; }
    public string? Biome { get; init; }
    public int? ShopTier { get; init; }
    public IReadOnlyDictionary<Skill, int> SkillModifiers { get; init; } = new Dictionary<Skill, int>();
    public IReadOnlyDictionary<string, Magnitude> ResistModifiers { get; init; } = new Dictionary<string, Magnitude>();

    /// <summary>True for items that go in Pack (equippable gear). False for consumables/tools that go in Haversack.</summary>
    public bool IsPackItem => Type is ItemType.Weapon or ItemType.Armor or ItemType.Boots;

    internal static IReadOnlyDictionary<string, ItemDef> All { get; } = BuildAll();

    static Dictionary<string, ItemDef> BuildAll() => new()
    {
        // ── Weapons ──

        ["bodkin"] = new()
        {
            Id = "bodkin", Name = "Bodkin", Type = ItemType.Weapon,
            WeaponClass = Rules.WeaponClass.Dagger,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Combat] = 1, [Skill.Mercantile] = 1 },
            Biome = "plains", ShopTier = 1, Cost = Magnitude.Small,
        },
        ["jambiya"] = new()
        {
            Id = "jambiya", Name = "Jambiya", Type = ItemType.Weapon,
            WeaponClass = Rules.WeaponClass.Dagger,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Combat] = 2 },
            Biome = "scrub", ShopTier = 1, Cost = Magnitude.Small,
        },
        ["seax"] = new()
        {
            Id = "seax", Name = "Seax", Type = ItemType.Weapon,
            WeaponClass = Rules.WeaponClass.Dagger,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Combat] = 2, [Skill.Bushcraft] = 2 },
            Biome = "mountains", ShopTier = 1, Cost = Magnitude.Small,
        },
        ["hatchet"] = new()
        {
            Id = "hatchet", Name = "Hatchet", Type = ItemType.Weapon,
            WeaponClass = Rules.WeaponClass.Axe,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Combat] = 2, [Skill.Bushcraft] = 2 },
            Biome = "forest", ShopTier = 1, Cost = Magnitude.Small,
        },
        ["war_axe"] = new()
        {
            Id = "war_axe", Name = "War Axe", Type = ItemType.Weapon,
            WeaponClass = Rules.WeaponClass.Axe,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Combat] = 3, [Skill.Bushcraft] = 1 },
            Biome = "plains", ShopTier = 2, Cost = Magnitude.Medium,
        },
        ["bardiche"] = new()
        {
            Id = "bardiche", Name = "Bardiche", Type = ItemType.Weapon,
            WeaponClass = Rules.WeaponClass.Axe,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Combat] = 4 },
            Biome = "plains", ShopTier = 2, Cost = Magnitude.Large,
        },
        ["short_sword"] = new()
        {
            Id = "short_sword", Name = "Short Sword", Type = ItemType.Weapon,
            WeaponClass = Rules.WeaponClass.Sword,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Combat] = 3 },
            Biome = "plains", ShopTier = 2, Cost = Magnitude.Medium,
        },
        ["scimitar"] = new()
        {
            Id = "scimitar", Name = "Scimitar", Type = ItemType.Weapon,
            WeaponClass = Rules.WeaponClass.Sword,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Combat] = 4 },
            Biome = "scrub", ShopTier = 2, Cost = Magnitude.Large,
        },
        ["arming_sword"] = new()
        {
            Id = "arming_sword", Name = "Arming Sword", Type = ItemType.Weapon,
            WeaponClass = Rules.WeaponClass.Sword,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Combat] = 4, [Skill.Negotiation] = 2 },
            Biome = "plains", ShopTier = 1, Cost = Magnitude.Huge,
        },

        // ── Armor ──

        ["tunic"] = new()
        {
            Id = "tunic", Name = "Tunic", Type = ItemType.Armor,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Mercantile] = 1 },
            Biome = "plains", ShopTier = 1,
        },
        ["leather"] = new()
        {
            Id = "leather", Name = "Leather", Type = ItemType.Armor,
            ResistModifiers = new Dictionary<string, Magnitude> { ["injured"] = Magnitude.Small },
            Biome = "forest", ShopTier = 1, Cost = Magnitude.Small,
        },
        ["gambeson"] = new()
        {
            Id = "gambeson", Name = "Gambeson", Type = ItemType.Armor,
            ResistModifiers = new Dictionary<string, Magnitude> { ["injured"] = Magnitude.Small },
            Biome = "mountains", ShopTier = 1, Cost = Magnitude.Small,
        },
        ["chainmail"] = new()
        {
            Id = "chainmail", Name = "Chainmail", Type = ItemType.Armor,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Luck] = 1, [Skill.Stealth] = -3 },
            ResistModifiers = new Dictionary<string, Magnitude> { ["injured"] = Magnitude.Medium },
            Biome = "plains", ShopTier = 2, Cost = Magnitude.Large,
        },
        ["scale_armor"] = new()
        {
            Id = "scale_armor", Name = "Scale Armor", Type = ItemType.Armor,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Stealth] = -3 },
            ResistModifiers = new Dictionary<string, Magnitude> { ["injured"] = Magnitude.Medium },
            Biome = "scrub", ShopTier = 2, Cost = Magnitude.Medium,
        },

        // ── Boots ──

        ["fine_boots"] = new()
        {
            Id = "fine_boots", Name = "Fine Boots", Type = ItemType.Boots,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Negotiation] = 1, [Skill.Mercantile] = 1, [Skill.Stealth] = 1 },
            Biome = "plains", ShopTier = 1, Cost = Magnitude.Large,
        },
        ["riding_boots"] = new()
        {
            Id = "riding_boots", Name = "Riding Boots", Type = ItemType.Boots,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Negotiation] = 1 },
            ResistModifiers = new Dictionary<string, Magnitude> { ["exhausted"] = Magnitude.Small },
            Biome = "scrub", ShopTier = 2, Cost = Magnitude.Medium,
        },
        ["heavy_work_boots"] = new()
        {
            Id = "heavy_work_boots", Name = "Heavy Work Boots", Type = ItemType.Boots,
            ResistModifiers = new Dictionary<string, Magnitude> { ["exhausted"] = Magnitude.Medium },
            Biome = "mountains", ShopTier = 1, Cost = Magnitude.Medium,
        },

        // ── Tools ──

        ["cartographers_kit"] = new()
        {
            Id = "cartographers_kit", Name = "Cartographer's Kit", Type = ItemType.Tool,
            ResistModifiers = new Dictionary<string, Magnitude> { ["lost"] = Magnitude.Large },
            Biome = "plains", ShopTier = 1, Cost = Magnitude.Large,
        },
        ["sleeping_kit"] = new()
        {
            Id = "sleeping_kit", Name = "Sleeping Kit", Type = ItemType.Tool,
            ResistModifiers = new Dictionary<string, Magnitude> { ["exhausted"] = Magnitude.Medium },
            Biome = "any", Cost = Magnitude.Small,
        },
        ["cooking_supplies"] = new()
        {
            Id = "cooking_supplies", Name = "Cooking Supplies", Type = ItemType.Tool,
            ResistModifiers = new Dictionary<string, Magnitude> { ["exhausted"] = Magnitude.Small },
            Biome = "any", Cost = Magnitude.Small,
        },
        ["writing_kit"] = new()
        {
            Id = "writing_kit", Name = "Writing Kit", Type = ItemType.Tool,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Mercantile] = 2, [Skill.Negotiation] = 1 },
            Biome = "mountains", ShopTier = 2, Cost = Magnitude.Medium,
        },
        ["yoriks_guide"] = new()
        {
            Id = "yoriks_guide", Name = "Yorik's Guide to Plant and Beast", Type = ItemType.Tool,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Bushcraft] = 2 },
            Biome = "mountains", ShopTier = 2, Cost = Magnitude.Large,
        },
        ["canteen"] = new()
        {
            Id = "canteen", Name = "Canteen", Type = ItemType.Tool,
            ResistModifiers = new Dictionary<string, Magnitude> { ["thirsty"] = Magnitude.Medium },
            Biome = "any", Cost = Magnitude.Small,
        },
        ["insect_netting"] = new()
        {
            Id = "insect_netting", Name = "Insect Netting", Type = ItemType.Tool,
            ResistModifiers = new Dictionary<string, Magnitude> { ["swamp_fever"] = Magnitude.Medium },
            Biome = "swamp", Cost = Magnitude.Small,
        },
        ["breathing_apparatus"] = new()
        {
            Id = "breathing_apparatus", Name = "Intricate Breathing Apparatus", Type = ItemType.Tool,
            ResistModifiers = new Dictionary<string, Magnitude> { ["irradiated"] = Magnitude.Medium, ["road_flux"] = Magnitude.Medium },
            Biome = "plains", ShopTier = 2, Cost = Magnitude.Large,
        },
        ["heavy_furs"] = new()
        {
            Id = "heavy_furs", Name = "Heavy Furs", Type = ItemType.Tool,
            ResistModifiers = new Dictionary<string, Magnitude> { ["freezing"] = Magnitude.Large },
            Biome = "mountains", Cost = Magnitude.Small,
        },
        ["peoples_borderlands"] = new()
        {
            Id = "peoples_borderlands", Name = "Peoples of the Borderlands", Type = ItemType.Tool,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Negotiation] = 3 },
            Biome = "mountains", ShopTier = 2, Cost = Magnitude.Large,
        },

        // ── Medicines ──

        ["bandages"] = new()
        {
            Id = "bandages", Name = "Bandages", Type = ItemType.Consumable,
            StackSize = 10, Cures = new Dictionary<string, Magnitude> { ["injured"] = Magnitude.Small },
            Cost = Magnitude.Trivial,
        },
        ["jorgo_root"] = new()
        {
            Id = "jorgo_root", Name = "Jorgo Root", Type = ItemType.Consumable,
            StackSize = 5, ResistModifiers = new Dictionary<string, Magnitude> { ["swamp_fever"] = Magnitude.Small },
            Biome = "mountains", ShopTier = 2, Cost = Magnitude.Small,
        },
        ["gravediggers_ear"] = new()
        {
            Id = "gravediggers_ear", Name = "Gravedigger's Ear", Type = ItemType.Consumable,
            StackSize = 5, Cures = new Dictionary<string, Magnitude> { ["swamp_fever"] = Magnitude.Small },
            Biome = "swamp", ShopTier = 2, Cost = Magnitude.Small,
        },
        ["duskwort"] = new()
        {
            Id = "duskwort", Name = "Duskwort", Type = ItemType.Consumable,
            StackSize = 5, ResistModifiers = new Dictionary<string, Magnitude> { ["injured"] = Magnitude.Small },
            Biome = "plains", ShopTier = 2, Cost = Magnitude.Small,
        },
        ["thumbroot"] = new()
        {
            Id = "thumbroot", Name = "Thumbroot", Type = ItemType.Consumable,
            StackSize = 5, Cures = new Dictionary<string, Magnitude> { ["injured"] = Magnitude.Small },
            Biome = "forest", ShopTier = 2, Cost = Magnitude.Small,
        },
        ["wound_sealant"] = new()
        {
            Id = "wound_sealant", Name = "Wound Sealant", Type = ItemType.Consumable,
            StackSize = 3, Cures = new Dictionary<string, Magnitude> { ["injured"] = Magnitude.Large },
            Biome = "plains", ShopTier = 3, Cost = Magnitude.Medium,
        },
        ["creeping_baldric"] = new()
        {
            Id = "creeping_baldric", Name = "Creeping Baldric", Type = ItemType.Consumable,
            StackSize = 5, Cures = new Dictionary<string, Magnitude> { ["road_flux"] = Magnitude.Small },
            Biome = "forest", ShopTier = 2, Cost = Magnitude.Small,
        },
        ["dustseed"] = new()
        {
            Id = "dustseed", Name = "Dustseed", Type = ItemType.Consumable,
            StackSize = 10, ResistModifiers = new Dictionary<string, Magnitude> { ["road_flux"] = Magnitude.Small },
            Biome = "plains", ShopTier = 2, Cost = Magnitude.Trivial,
        },
        ["widows_veil"] = new()
        {
            Id = "widows_veil", Name = "Widow's Veil", Type = ItemType.Consumable,
            StackSize = 5, ResistModifiers = new Dictionary<string, Magnitude> { ["exhausted"] = Magnitude.Small },
            Biome = "swamp", ShopTier = 2, Cost = Magnitude.Small,
        },
        ["pale_knot_berry"] = new()
        {
            Id = "pale_knot_berry", Name = "Pale Knot Berry", Type = ItemType.Consumable,
            StackSize = 5, Cures = new Dictionary<string, Magnitude> { ["exhausted"] = Magnitude.Small },
            Biome = "plains", ShopTier = 2, Cost = Magnitude.Small,
        },
        ["shustov_tonic"] = new()
        {
            Id = "shustov_tonic", Name = "Shustov Tonic", Type = ItemType.Consumable,
            StackSize = 3,
            ResistModifiers = new Dictionary<string, Magnitude> { ["irradiated"] = Magnitude.Small },
            Cures = new Dictionary<string, Magnitude> { ["irradiated"] = Magnitude.Small },
            Biome = "plains", ShopTier = 3, Cost = Magnitude.Medium,
        },
        ["mudcap_fungus"] = new()
        {
            Id = "mudcap_fungus", Name = "Mudcap Fungus", Type = ItemType.Consumable,
            StackSize = 5, Cures = new Dictionary<string, Magnitude> { ["poisoned"] = Magnitude.Medium },
            Biome = "swamp", ShopTier = 2, Cost = Magnitude.Small,
        },
    };
}
