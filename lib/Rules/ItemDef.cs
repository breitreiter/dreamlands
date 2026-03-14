namespace Dreamlands.Rules;

/// <summary>Type of equipment item.</summary>
public enum ItemType { Tool, Consumable, Token, Weapon, Armor, Boots, Haul }

/// <summary>Weapon class for weapon-type items.</summary>
public enum WeaponClass { Dagger, Axe, Sword }

/// <summary>Armor class for armor-type items.</summary>
public enum ArmorClass { Light, Medium, Heavy }

/// <summary>Definition of an equipment item.</summary>
public sealed class ItemDef
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string? Description { get; init; }
    public ItemType Type { get; init; }
    public int Slots { get; init; } = 1;
    public int CapacityBonus { get; init; }
    public IReadOnlySet<string> Cures { get; init; } = new HashSet<string>();

    public WeaponClass? WeaponClass { get; init; }
    public ArmorClass? ArmorClass { get; init; }
    public FoodType? FoodType { get; init; }
    public Magnitude? Cost { get; init; }
    public string? Biome { get; init; }
    public int? ShopTier { get; init; }
    public IReadOnlyDictionary<Skill, int> SkillModifiers { get; init; } = new Dictionary<Skill, int>();
    public IReadOnlyDictionary<string, Magnitude> ResistModifiers { get; init; } = new Dictionary<string, Magnitude>();

    /// <summary>Bonus to foraging rolls when this weapon is equipped. Stacks with Bushcraft skill.</summary>
    public int ForagingBonus { get; init; }

    /// <summary>True for items that go in Pack (gear + trade goods). False for consumables that go in Haversack.</summary>
    public bool IsPackItem => Type is ItemType.Weapon or ItemType.Armor or ItemType.Boots or ItemType.Tool or ItemType.Haul;

    public static IReadOnlyDictionary<string, ItemDef> All { get; } = BuildAll();

    public static bool IsValidId(string id) => All.ContainsKey(id);

    static Dictionary<string, ItemDef> BuildAll() => new()
    {
        // ── Weapons: Daggers (Combat +1 cap, Foraging +1 to +5) ──

        ["bodkin"] = new()
        {
            Id = "bodkin", Name = "Bodkin", Type = ItemType.Weapon,
            WeaponClass = Rules.WeaponClass.Dagger,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Combat] = 1 },
            ForagingBonus = 1,
            Biome = "plains", ShopTier = 1, Cost = Magnitude.Small,
        },
        ["skinning_knife"] = new()
        {
            Id = "skinning_knife", Name = "Skinning Knife", Type = ItemType.Weapon,
            WeaponClass = Rules.WeaponClass.Dagger,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Combat] = 1 },
            ForagingBonus = 2,
            Biome = "swamp", ShopTier = 1, Cost = Magnitude.Small,
        },
        ["jambiya"] = new()
        {
            Id = "jambiya", Name = "Jambiya", Type = ItemType.Weapon,
            WeaponClass = Rules.WeaponClass.Dagger,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Combat] = 1 },
            ForagingBonus = 2,
            Biome = "scrub", ShopTier = 1, Cost = Magnitude.Small,
        },
        ["seax"] = new()
        {
            Id = "seax", Name = "Seax", Type = ItemType.Weapon,
            WeaponClass = Rules.WeaponClass.Dagger,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Combat] = 1 },
            ForagingBonus = 3,
            Biome = "mountains", ShopTier = 2, Cost = Magnitude.Medium,
        },
        ["kukri"] = new()
        {
            Id = "kukri", Name = "Kukri", Type = ItemType.Weapon,
            WeaponClass = Rules.WeaponClass.Dagger,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Combat] = 1 },
            ForagingBonus = 3,
            Biome = "scrub", ShopTier = 2, Cost = Magnitude.Medium,
        },
        ["hunting_knife"] = new()
        {
            Id = "hunting_knife", Name = "Hunting Knife", Type = ItemType.Weapon,
            WeaponClass = Rules.WeaponClass.Dagger,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Combat] = 1 },
            ForagingBonus = 4,
            Biome = "mountains", ShopTier = 2, Cost = Magnitude.Large,
        },
        ["kopis"] = new()
        {
            Id = "kopis", Name = "Kopis", Type = ItemType.Weapon,
            WeaponClass = Rules.WeaponClass.Dagger,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Combat] = 1 },
            ForagingBonus = 5,
        },

        // ── Weapons: Axes (Combat +1 to +4, Foraging +1 to +3) ──

        ["hatchet"] = new()
        {
            Id = "hatchet", Name = "Hatchet", Type = ItemType.Weapon,
            WeaponClass = Rules.WeaponClass.Axe,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Combat] = 1 },
            ForagingBonus = 1,
            Biome = "forest", ShopTier = 1, Cost = Magnitude.Small,
        },
        ["tomahawk"] = new()
        {
            Id = "tomahawk", Name = "Tomahawk", Type = ItemType.Weapon,
            WeaponClass = Rules.WeaponClass.Axe,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Combat] = 2 },
            ForagingBonus = 1,
            Biome = "forest", ShopTier = 1, Cost = Magnitude.Small,
        },
        ["war_axe"] = new()
        {
            Id = "war_axe", Name = "War Axe", Type = ItemType.Weapon,
            WeaponClass = Rules.WeaponClass.Axe,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Combat] = 2 },
            ForagingBonus = 2,
            Biome = "forest", ShopTier = 2, Cost = Magnitude.Medium,
        },
        ["broadaxe"] = new()
        {
            Id = "broadaxe", Name = "Broadaxe", Type = ItemType.Weapon,
            WeaponClass = Rules.WeaponClass.Axe,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Combat] = 3 },
            ForagingBonus = 2,
            Biome = "mountains", ShopTier = 2, Cost = Magnitude.Large,
        },
        ["bardiche"] = new()
        {
            Id = "bardiche", Name = "Bardiche", Type = ItemType.Weapon,
            WeaponClass = Rules.WeaponClass.Axe,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Combat] = 3 },
            ForagingBonus = 1,
            Biome = "mountains", ShopTier = 2, Cost = Magnitude.Large,
        },
        ["labrys"] = new()
        {
            Id = "labrys", Name = "Labrys", Type = ItemType.Weapon,
            WeaponClass = Rules.WeaponClass.Axe,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Combat] = 4 },
            ForagingBonus = 3,
        },

        // ── Weapons: Swords (Foraging +0 cap, Combat +1 to +5) ──

        ["falchion"] = new()
        {
            Id = "falchion", Name = "Falchion", Type = ItemType.Weapon,
            WeaponClass = Rules.WeaponClass.Sword,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Combat] = 2 },
            Biome = "plains", ShopTier = 1, Cost = Magnitude.Small,
        },
        ["short_sword"] = new()
        {
            Id = "short_sword", Name = "Short Sword", Type = ItemType.Weapon,
            WeaponClass = Rules.WeaponClass.Sword,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Combat] = 3 },
            Biome = "plains", ShopTier = 2, Cost = Magnitude.Medium,
        },
        ["tulwar"] = new()
        {
            Id = "tulwar", Name = "Tulwar", Type = ItemType.Weapon,
            WeaponClass = Rules.WeaponClass.Sword,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Combat] = 3 },
            Biome = "scrub", ShopTier = 2, Cost = Magnitude.Medium,
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
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Combat] = 4 },
            Biome = "plains", ShopTier = 2, Cost = Magnitude.Large,
        },
        ["zweihander"] = new()
        {
            Id = "zweihander", Name = "Zweihänder", Type = ItemType.Weapon,
            WeaponClass = Rules.WeaponClass.Sword,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Combat] = 5 },
        },

        // ── Armor: Light (Cunning +0 to +5, Injury +0, Freezing +0 to +3) ──

        ["tunic"] = new()
        {
            Id = "tunic", Name = "Tunic", Type = ItemType.Armor,
            ArmorClass = Rules.ArmorClass.Light,
            Biome = "plains", ShopTier = 1,
        },
        ["silks"] = new()
        {
            Id = "silks", Name = "Silks", Type = ItemType.Armor,
            ArmorClass = Rules.ArmorClass.Light,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Cunning] = 1 },
            Biome = "scrub", ShopTier = 1, Cost = Magnitude.Small,
        },
        ["waxed_poncho"] = new()
        {
            Id = "waxed_poncho", Name = "Waxed Poncho", Type = ItemType.Armor,
            ArmorClass = Rules.ArmorClass.Light,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Cunning] = 2 },
            ResistModifiers = new Dictionary<string, Magnitude> { ["freezing"] = Magnitude.Trivial },
            Biome = "swamp", ShopTier = 1, Cost = Magnitude.Small,
        },
        ["traveling_cloak"] = new()
        {
            Id = "traveling_cloak", Name = "Traveling Cloak", Type = ItemType.Armor,
            ArmorClass = Rules.ArmorClass.Light,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Cunning] = 3 },
            ResistModifiers = new Dictionary<string, Magnitude> { ["freezing"] = Magnitude.Small },
            Biome = "mountains", ShopTier = 2, Cost = Magnitude.Medium,
        },
        ["embroidered_kaftan"] = new()
        {
            Id = "embroidered_kaftan", Name = "Embroidered Kaftan", Type = ItemType.Armor,
            ArmorClass = Rules.ArmorClass.Light,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Cunning] = 4 },
            ResistModifiers = new Dictionary<string, Magnitude> { ["freezing"] = Magnitude.Small },
            Biome = "scrub", ShopTier = 2, Cost = Magnitude.Large,
        },
        ["nightveil"] = new()
        {
            Id = "nightveil", Name = "Nightveil", Type = ItemType.Armor,
            ArmorClass = Rules.ArmorClass.Light,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Cunning] = 5 },
            ResistModifiers = new Dictionary<string, Magnitude> { ["freezing"] = Magnitude.Medium },
        },

        // ── Armor: Medium (Cunning +1 to +2, Injury +1 to +3, Freezing +1 to +5) ──

        ["leather"] = new()
        {
            Id = "leather", Name = "Leather", Type = ItemType.Armor,
            ArmorClass = Rules.ArmorClass.Medium,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Cunning] = 1 },
            ResistModifiers = new Dictionary<string, Magnitude> { ["injured"] = Magnitude.Trivial, ["freezing"] = Magnitude.Trivial },
            Biome = "forest", ShopTier = 1, Cost = Magnitude.Small,
        },
        ["hide_armor"] = new()
        {
            Id = "hide_armor", Name = "Hide Armor", Type = ItemType.Armor,
            ArmorClass = Rules.ArmorClass.Medium,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Cunning] = 1 },
            ResistModifiers = new Dictionary<string, Magnitude> { ["injured"] = Magnitude.Trivial, ["freezing"] = Magnitude.Small },
            Biome = "mountains", ShopTier = 1, Cost = Magnitude.Small,
        },
        ["buff_coat"] = new()
        {
            Id = "buff_coat", Name = "Buff Coat", Type = ItemType.Armor,
            ArmorClass = Rules.ArmorClass.Medium,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Cunning] = 1 },
            ResistModifiers = new Dictionary<string, Magnitude> { ["injured"] = Magnitude.Small, ["freezing"] = Magnitude.Medium },
            Biome = "forest", ShopTier = 2, Cost = Magnitude.Medium,
        },
        ["lamellar"] = new()
        {
            Id = "lamellar", Name = "Lamellar", Type = ItemType.Armor,
            ArmorClass = Rules.ArmorClass.Medium,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Cunning] = 2 },
            ResistModifiers = new Dictionary<string, Magnitude> { ["injured"] = Magnitude.Small, ["freezing"] = Magnitude.Medium },
            Biome = "mountains", ShopTier = 2, Cost = Magnitude.Large,
        },
        ["frostward"] = new()
        {
            Id = "frostward", Name = "Frostward Harness", Type = ItemType.Armor,
            ArmorClass = Rules.ArmorClass.Medium,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Cunning] = 2 },
            ResistModifiers = new Dictionary<string, Magnitude> { ["injured"] = Magnitude.Medium, ["freezing"] = Magnitude.Huge },
        },

        // ── Armor: Heavy (Injury +1 to +5, Cunning +0, Freezing +0 to +2) ──

        ["gambeson"] = new()
        {
            Id = "gambeson", Name = "Gambeson", Type = ItemType.Armor,
            ArmorClass = Rules.ArmorClass.Heavy,
            ResistModifiers = new Dictionary<string, Magnitude> { ["injured"] = Magnitude.Trivial, ["freezing"] = Magnitude.Trivial },
            Biome = "mountains", ShopTier = 1, Cost = Magnitude.Small,
        },
        ["chainmail"] = new()
        {
            Id = "chainmail", Name = "Chainmail", Type = ItemType.Armor,
            ArmorClass = Rules.ArmorClass.Heavy,
            ResistModifiers = new Dictionary<string, Magnitude> { ["injured"] = Magnitude.Small },
            Biome = "plains", ShopTier = 1, Cost = Magnitude.Small,
        },
        ["scale_armor"] = new()
        {
            Id = "scale_armor", Name = "Scale Armor", Type = ItemType.Armor,
            ArmorClass = Rules.ArmorClass.Heavy,
            ResistModifiers = new Dictionary<string, Magnitude> { ["injured"] = Magnitude.Medium },
            Biome = "scrub", ShopTier = 2, Cost = Magnitude.Medium,
        },
        ["brigandine"] = new()
        {
            Id = "brigandine", Name = "Brigandine", Type = ItemType.Armor,
            ArmorClass = Rules.ArmorClass.Heavy,
            ResistModifiers = new Dictionary<string, Magnitude> { ["injured"] = Magnitude.Large, ["freezing"] = Magnitude.Trivial },
            Biome = "plains", ShopTier = 2, Cost = Magnitude.Large,
        },
        ["wardens_plate"] = new()
        {
            Id = "wardens_plate", Name = "Warden's Plate", Type = ItemType.Armor,
            ArmorClass = Rules.ArmorClass.Heavy,
            ResistModifiers = new Dictionary<string, Magnitude> { ["injured"] = Magnitude.Huge, ["freezing"] = Magnitude.Small },
        },

        // ── Boots (Exhaustion resist +1 to +5) ──

        ["fine_boots"] = new()
        {
            Id = "fine_boots", Name = "Fine Boots", Type = ItemType.Boots,
            ResistModifiers = new Dictionary<string, Magnitude> { ["exhausted"] = Magnitude.Trivial },
            Biome = "plains", ShopTier = 1, Cost = Magnitude.Small,
        },
        ["heavy_work_boots"] = new()
        {
            Id = "heavy_work_boots", Name = "Heavy Work Boots", Type = ItemType.Boots,
            ResistModifiers = new Dictionary<string, Magnitude> { ["exhausted"] = Magnitude.Small },
            Biome = "mountains", ShopTier = 1, Cost = Magnitude.Small,
        },
        ["riding_boots"] = new()
        {
            Id = "riding_boots", Name = "Riding Boots", Type = ItemType.Boots,
            ResistModifiers = new Dictionary<string, Magnitude> { ["exhausted"] = Magnitude.Medium },
            Biome = "scrub", ShopTier = 2, Cost = Magnitude.Medium,
        },
        ["trail_boots"] = new()
        {
            Id = "trail_boots", Name = "Trail Boots", Type = ItemType.Boots,
            ResistModifiers = new Dictionary<string, Magnitude> { ["exhausted"] = Magnitude.Large },
            Biome = "forest", ShopTier = 2, Cost = Magnitude.Large,
        },
        ["windstriders"] = new()
        {
            Id = "windstriders", Name = "Windstriders", Type = ItemType.Boots,
            ResistModifiers = new Dictionary<string, Magnitude> { ["exhausted"] = Magnitude.Huge },
        },

        // ── Tools: Shopable ──

        ["canteen"] = new()
        {
            Id = "canteen", Name = "Canteen", Type = ItemType.Tool,
            ResistModifiers = new Dictionary<string, Magnitude> { ["thirsty"] = Magnitude.Small },
            Biome = "forest", ShopTier = 1, Cost = Magnitude.Small,
        },
        ["waterskin"] = new()
        {
            Id = "waterskin", Name = "Waterskin", Type = ItemType.Tool,
            ResistModifiers = new Dictionary<string, Magnitude> { ["thirsty"] = Magnitude.Medium },
            Biome = "scrub", ShopTier = 2, Cost = Magnitude.Medium,
        },
        ["letters_of_introduction"] = new()
        {
            Id = "letters_of_introduction", Name = "Letters of Introduction", Type = ItemType.Tool,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Negotiation] = 2 },
            Biome = "scrub", ShopTier = 1, Cost = Magnitude.Medium,
        },
        ["peoples_borderlands"] = new()
        {
            Id = "peoples_borderlands", Name = "Peoples of the Borderlands", Type = ItemType.Tool,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Negotiation] = 3 },
            Biome = "mountains", ShopTier = 2, Cost = Magnitude.Large,
        },
        ["traders_ledger"] = new()
        {
            Id = "traders_ledger", Name = "Trader's Ledger", Type = ItemType.Tool,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Mercantile] = 2 },
            Biome = "plains", ShopTier = 1, Cost = Magnitude.Medium,
        },
        ["assayers_kit"] = new()
        {
            Id = "assayers_kit", Name = "Assayer's Kit", Type = ItemType.Tool,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Mercantile] = 3 },
            Biome = "mountains", ShopTier = 2, Cost = Magnitude.Large,
        },
        ["cartographers_kit"] = new()
        {
            Id = "cartographers_kit", Name = "Cartographer's Kit", Type = ItemType.Tool,
            ResistModifiers = new Dictionary<string, Magnitude> { ["lost"] = Magnitude.Huge },
            Biome = "plains", ShopTier = 1, Cost = Magnitude.Large,
        },
        ["sleeping_kit"] = new()
        {
            Id = "sleeping_kit", Name = "Sleeping Kit", Type = ItemType.Tool,
            ResistModifiers = new Dictionary<string, Magnitude> { ["exhausted"] = Magnitude.Large },
            Biome = "forest", ShopTier = 2, Cost = Magnitude.Large,
        },

        // ── Tools: Dungeon-only ──

        ["lattice_ward"] = new()
        {
            Id = "lattice_ward", Name = "Lattice Ward", Type = ItemType.Tool,
            ResistModifiers = new Dictionary<string, Magnitude> { ["lattice_sickness"] = Magnitude.Huge },
        },
        ["lead_lined_case"] = new()
        {
            Id = "lead_lined_case", Name = "Lead-Lined Case", Type = ItemType.Tool,
            ResistModifiers = new Dictionary<string, Magnitude> { ["irradiated"] = Magnitude.Huge },
        },
        ["antivenom_kit"] = new()
        {
            Id = "antivenom_kit", Name = "Antivenom Kit", Type = ItemType.Tool,
            ResistModifiers = new Dictionary<string, Magnitude> { ["poison"] = Magnitude.Huge },
        },

        // ── Food ──
        // One def per FoodType. Display names come from FlavorText.FoodName() at purchase/forage
        // time and are stored on the ItemInstance. Each food item occupies 1 haversack slot.

        ["food_protein"] = new()
        {
            Id = "food_protein", Name = "Meat & Fish", Type = ItemType.Consumable,
            FoodType = Rules.FoodType.Protein, Cost = Magnitude.Trivial,
        },
        ["food_grain"] = new()
        {
            Id = "food_grain", Name = "Breadstuffs", Type = ItemType.Consumable,
            FoodType = Rules.FoodType.Grain, Cost = Magnitude.Trivial,
        },
        ["food_sweets"] = new()
        {
            Id = "food_sweets", Name = "Sweets", Type = ItemType.Consumable,
            FoodType = Rules.FoodType.Sweets, Cost = Magnitude.Trivial,
        },

        // ── Medicines ──

        ["bandages"] = new()
        {
            Id = "bandages", Name = "Bandages", Type = ItemType.Consumable,
            Description = "Clean linen strips treated with pine resin. Cures injured.",
            Cures = new HashSet<string> { "injured" },
            Cost = Magnitude.Trivial,
        },
        ["siphon_glass"] = new()
        {
            Id = "siphon_glass", Name = "Siphon Glass", Type = ItemType.Consumable,
            Description = "Strange crystalline fragments. It's said they draw out and capture unnatural colors.",
            Cures = new HashSet<string> { "lattice_sickness" },
            Biome = "scrub", ShopTier = 3, Cost = Magnitude.Medium,
        },
        ["pale_knot_berry"] = new()
        {
            Id = "pale_knot_berry", Name = "Pale Knot Berry", Type = ItemType.Consumable,
            Description = "Waxy white berries found on wind-stunted shrubs. A handful restores vigor and cures exhaustion.",
            Cures = new HashSet<string> { "exhausted" },
            Biome = "plains", ShopTier = 2, Cost = Magnitude.Small,
        },
        ["shustov_tonic"] = new()
        {
            Id = "shustov_tonic", Name = "Shustov Tonic", Type = ItemType.Consumable,
            Description = "A smoky distillation of charcoal and salt-marsh herbs. Flushes radiation sickness over several nights.",
            Cures = new HashSet<string> { "irradiated" },
            Biome = "plains", ShopTier = 3, Cost = Magnitude.Medium,
        },
        ["mudcap_fungus"] = new()
        {
            Id = "mudcap_fungus", Name = "Mudcap Fungus", Type = ItemType.Consumable,
            Description = "A squat brown mushroom with a gritty cap. Eaten raw, it draws poison from the blood.",
            Cures = new HashSet<string> { "poisoned" },
            Biome = "swamp", ShopTier = 2, Cost = Magnitude.Small,
        },

        // ── Tokens (dungeon rewards) ──

        ["ivory_comb"] = new()
        {
            Id = "ivory_comb", Name = "Ivory Comb", Type = ItemType.Token,
            Description = "A delicate comb carved from yellowed bone, cold to the touch. Faint scratches on the spine might be letters in a language you don't recognize.",
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Negotiation] = 1 },
        },

        // ── Haul (generic def — per-haul identity comes from HaulDefId on ItemInstance) ──

        ["haul"] = new()
        {
            Id = "haul", Name = "Haul", Type = ItemType.Haul,
            Description = "A delivery bound for a specific settlement.",
        },
    };
}
