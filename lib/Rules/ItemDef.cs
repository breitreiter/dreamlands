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
    public int? Cost { get; init; }
    public string? Biome { get; init; }
    public int? ShopTier { get; init; }
    public IReadOnlyDictionary<Skill, int> SkillModifiers { get; init; } = new Dictionary<Skill, int>();
    public IReadOnlyDictionary<string, int> ResistModifiers { get; init; } = new Dictionary<string, int>();

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
            Biome = "plains", ShopTier = 1, Cost = 15,
        },
        ["skinning_knife"] = new()
        {
            Id = "skinning_knife", Name = "Skinning Knife", Type = ItemType.Weapon,
            WeaponClass = Rules.WeaponClass.Dagger,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Combat] = 1 },
            ForagingBonus = 2,
            Biome = "swamp", ShopTier = 1, Cost = 15,
        },
        ["jambiya"] = new()
        {
            Id = "jambiya", Name = "Jambiya", Type = ItemType.Weapon,
            WeaponClass = Rules.WeaponClass.Dagger,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Combat] = 1 },
            ForagingBonus = 2,
            Biome = "scrub", ShopTier = 1, Cost = 15,
        },
        ["seax"] = new()
        {
            Id = "seax", Name = "Seax", Type = ItemType.Weapon,
            WeaponClass = Rules.WeaponClass.Dagger,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Combat] = 1 },
            ForagingBonus = 3,
            Biome = "mountains", ShopTier = 2, Cost = 40,
        },
        ["kukri"] = new()
        {
            Id = "kukri", Name = "Kukri", Type = ItemType.Weapon,
            WeaponClass = Rules.WeaponClass.Dagger,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Combat] = 1 },
            ForagingBonus = 3,
            Biome = "scrub", ShopTier = 2, Cost = 40,
        },
        ["hunting_knife"] = new()
        {
            Id = "hunting_knife", Name = "Hunting Knife", Type = ItemType.Weapon,
            WeaponClass = Rules.WeaponClass.Dagger,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Combat] = 1 },
            ForagingBonus = 4,
            Biome = "mountains", ShopTier = 2, Cost = 80,
        },
        ["the_old_tooth"] = new()
        {
            Id = "the_old_tooth", Name = "The Old Tooth", Type = ItemType.Weapon,
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
            Biome = "forest", ShopTier = 1, Cost = 15,
        },
        ["tomahawk"] = new()
        {
            Id = "tomahawk", Name = "Tomahawk", Type = ItemType.Weapon,
            WeaponClass = Rules.WeaponClass.Axe,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Combat] = 2 },
            ForagingBonus = 1,
            Biome = "forest", ShopTier = 1, Cost = 15,
        },
        ["war_axe"] = new()
        {
            Id = "war_axe", Name = "War Axe", Type = ItemType.Weapon,
            WeaponClass = Rules.WeaponClass.Axe,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Combat] = 2 },
            ForagingBonus = 2,
            Biome = "forest", ShopTier = 2, Cost = 40,
        },
        ["broadaxe"] = new()
        {
            Id = "broadaxe", Name = "Broadaxe", Type = ItemType.Weapon,
            WeaponClass = Rules.WeaponClass.Axe,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Combat] = 3 },
            ForagingBonus = 2,
            Biome = "mountains", ShopTier = 2, Cost = 80,
        },
        ["bardiche"] = new()
        {
            Id = "bardiche", Name = "Bardiche", Type = ItemType.Weapon,
            WeaponClass = Rules.WeaponClass.Axe,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Combat] = 3 },
            ForagingBonus = 1,
            Biome = "mountains", ShopTier = 2, Cost = 80,
        },
        ["revathi_labrys"] = new()
        {
            Id = "revathi_labrys", Name = "Revathi Labrys", Type = ItemType.Weapon,
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
            Biome = "plains", ShopTier = 1, Cost = 15,
        },
        ["short_sword"] = new()
        {
            Id = "short_sword", Name = "Short Sword", Type = ItemType.Weapon,
            WeaponClass = Rules.WeaponClass.Sword,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Combat] = 3 },
            Biome = "plains", ShopTier = 2, Cost = 40,
        },
        ["tulwar"] = new()
        {
            Id = "tulwar", Name = "Tulwar", Type = ItemType.Weapon,
            WeaponClass = Rules.WeaponClass.Sword,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Combat] = 3 },
            Biome = "scrub", ShopTier = 2, Cost = 40,
        },
        ["scimitar"] = new()
        {
            Id = "scimitar", Name = "Scimitar", Type = ItemType.Weapon,
            WeaponClass = Rules.WeaponClass.Sword,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Combat] = 4 },
            Biome = "scrub", ShopTier = 2, Cost = 80,
        },
        ["arming_sword"] = new()
        {
            Id = "arming_sword", Name = "Arming Sword", Type = ItemType.Weapon,
            WeaponClass = Rules.WeaponClass.Sword,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Combat] = 4 },
            Biome = "plains", ShopTier = 2, Cost = 80,
        },
        ["shimmering_blade"] = new()
        {
            Id = "shimmering_blade", Name = "Shimmering Blade", Type = ItemType.Weapon,
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
            Biome = "scrub", ShopTier = 1, Cost = 15,
        },
        ["waxed_poncho"] = new()
        {
            Id = "waxed_poncho", Name = "Waxed Poncho", Type = ItemType.Armor,
            ArmorClass = Rules.ArmorClass.Light,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Cunning] = 2 },
            ResistModifiers = new Dictionary<string, int> { ["freezing"] = 1 },
            Biome = "swamp", ShopTier = 1, Cost = 15,
        },
        ["traveling_cloak"] = new()
        {
            Id = "traveling_cloak", Name = "Traveling Cloak", Type = ItemType.Armor,
            ArmorClass = Rules.ArmorClass.Light,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Cunning] = 3 },
            ResistModifiers = new Dictionary<string, int> { ["freezing"] = 2 },
            Biome = "mountains", ShopTier = 2, Cost = 40,
        },
        ["embroidered_kaftan"] = new()
        {
            Id = "embroidered_kaftan", Name = "Embroidered Kaftan", Type = ItemType.Armor,
            ArmorClass = Rules.ArmorClass.Light,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Cunning] = 4 },
            ResistModifiers = new Dictionary<string, int> { ["freezing"] = 2 },
            Biome = "scrub", ShopTier = 2, Cost = 80,
        },
        ["magisterial_robe"] = new()
        {
            Id = "magisterial_robe", Name = "Magisterial Robe", Type = ItemType.Armor,
            ArmorClass = Rules.ArmorClass.Light,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Cunning] = 5 },
            ResistModifiers = new Dictionary<string, int> { ["freezing"] = 3 },
        },

        // ── Armor: Medium (Cunning +1 to +2, Injury +1 to +3, Freezing +1 to +5) ──

        ["leather"] = new()
        {
            Id = "leather", Name = "Leather", Type = ItemType.Armor,
            ArmorClass = Rules.ArmorClass.Medium,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Cunning] = 1 },
            ResistModifiers = new Dictionary<string, int> { ["injured"] = 1, ["freezing"] = 1 },
            Biome = "forest", ShopTier = 1, Cost = 15,
        },
        ["hide_armor"] = new()
        {
            Id = "hide_armor", Name = "Hide Armor", Type = ItemType.Armor,
            ArmorClass = Rules.ArmorClass.Medium,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Cunning] = 1 },
            ResistModifiers = new Dictionary<string, int> { ["injured"] = 1, ["freezing"] = 2 },
            Biome = "mountains", ShopTier = 1, Cost = 15,
        },
        ["buff_coat"] = new()
        {
            Id = "buff_coat", Name = "Buff Coat", Type = ItemType.Armor,
            ArmorClass = Rules.ArmorClass.Medium,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Cunning] = 1 },
            ResistModifiers = new Dictionary<string, int> { ["injured"] = 2, ["freezing"] = 3 },
            Biome = "forest", ShopTier = 2, Cost = 40,
        },
        ["lamellar"] = new()
        {
            Id = "lamellar", Name = "Lamellar", Type = ItemType.Armor,
            ArmorClass = Rules.ArmorClass.Medium,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Cunning] = 2 },
            ResistModifiers = new Dictionary<string, int> { ["injured"] = 2, ["freezing"] = 3 },
            Biome = "mountains", ShopTier = 2, Cost = 80,
        },
        ["mountain_regiment_armor"] = new()
        {
            Id = "mountain_regiment_armor", Name = "17th Mountain Regiment Armor", Type = ItemType.Armor,
            ArmorClass = Rules.ArmorClass.Medium,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Cunning] = 2 },
            ResistModifiers = new Dictionary<string, int> { ["injured"] = 3, ["freezing"] = 5 },
        },

        // ── Armor: Heavy (Injury +1 to +5, Cunning +0, Freezing +0 to +2) ──

        ["gambeson"] = new()
        {
            Id = "gambeson", Name = "Gambeson", Type = ItemType.Armor,
            ArmorClass = Rules.ArmorClass.Heavy,
            ResistModifiers = new Dictionary<string, int> { ["injured"] = 1, ["freezing"] = 1 },
            Biome = "mountains", ShopTier = 1, Cost = 15,
        },
        ["chainmail"] = new()
        {
            Id = "chainmail", Name = "Chainmail", Type = ItemType.Armor,
            ArmorClass = Rules.ArmorClass.Heavy,
            ResistModifiers = new Dictionary<string, int> { ["injured"] = 2 },
            Biome = "plains", ShopTier = 1, Cost = 15,
        },
        ["scale_armor"] = new()
        {
            Id = "scale_armor", Name = "Scale Armor", Type = ItemType.Armor,
            ArmorClass = Rules.ArmorClass.Heavy,
            ResistModifiers = new Dictionary<string, int> { ["injured"] = 3 },
            Biome = "scrub", ShopTier = 2, Cost = 40,
        },
        ["brigandine"] = new()
        {
            Id = "brigandine", Name = "Brigandine", Type = ItemType.Armor,
            ArmorClass = Rules.ArmorClass.Heavy,
            ResistModifiers = new Dictionary<string, int> { ["injured"] = 4, ["freezing"] = 1 },
            Biome = "plains", ShopTier = 2, Cost = 80,
        },
        ["golem_armor"] = new()
        {
            Id = "golem_armor", Name = "Golem Armor", Type = ItemType.Armor,
            ArmorClass = Rules.ArmorClass.Heavy,
            ResistModifiers = new Dictionary<string, int> { ["injured"] = 5, ["freezing"] = 2 },
        },

        // ── Boots (Exhaustion resist +1 to +5) ──

        ["fine_boots"] = new()
        {
            Id = "fine_boots", Name = "Fine Boots", Type = ItemType.Boots,
            ResistModifiers = new Dictionary<string, int> { ["exhausted"] = 1 },
            Biome = "plains", ShopTier = 1, Cost = 15,
        },
        ["heavy_work_boots"] = new()
        {
            Id = "heavy_work_boots", Name = "Heavy Work Boots", Type = ItemType.Boots,
            ResistModifiers = new Dictionary<string, int> { ["exhausted"] = 2 },
            Biome = "mountains", ShopTier = 1, Cost = 15,
        },
        ["riding_boots"] = new()
        {
            Id = "riding_boots", Name = "Riding Boots", Type = ItemType.Boots,
            ResistModifiers = new Dictionary<string, int> { ["exhausted"] = 3 },
            Biome = "scrub", ShopTier = 2, Cost = 40,
        },
        ["trail_boots"] = new()
        {
            Id = "trail_boots", Name = "Trail Boots", Type = ItemType.Boots,
            ResistModifiers = new Dictionary<string, int> { ["exhausted"] = 4 },
            Biome = "forest", ShopTier = 2, Cost = 80,
        },
        ["windstriders"] = new()
        {
            Id = "windstriders", Name = "Windstriders", Type = ItemType.Boots,
            ResistModifiers = new Dictionary<string, int> { ["exhausted"] = 5 },
        },

        // ── Tools: Shopable ──

        ["canteen"] = new()
        {
            Id = "canteen", Name = "Canteen", Type = ItemType.Tool,
            ResistModifiers = new Dictionary<string, int> { ["thirsty"] = 2 },
            Biome = "forest", ShopTier = 1, Cost = 15,
        },
        ["waterskin"] = new()
        {
            Id = "waterskin", Name = "Waterskin", Type = ItemType.Tool,
            ResistModifiers = new Dictionary<string, int> { ["thirsty"] = 3 },
            Biome = "scrub", ShopTier = 2, Cost = 40,
        },
        ["letters_of_introduction"] = new()
        {
            Id = "letters_of_introduction", Name = "Letters of Introduction", Type = ItemType.Tool,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Negotiation] = 2 },
            Biome = "scrub", ShopTier = 1, Cost = 40,
        },
        ["peoples_borderlands"] = new()
        {
            Id = "peoples_borderlands", Name = "Peoples of the Borderlands", Type = ItemType.Tool,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Negotiation] = 3 },
            Biome = "mountains", ShopTier = 2, Cost = 80,
        },
        ["cartographers_kit"] = new()
        {
            Id = "cartographers_kit", Name = "Cartographer's Kit", Type = ItemType.Tool,
            ResistModifiers = new Dictionary<string, int> { ["lost"] = 5 },
            Biome = "plains", ShopTier = 1, Cost = 80,
        },
        ["sleeping_kit"] = new()
        {
            Id = "sleeping_kit", Name = "Sleeping Kit", Type = ItemType.Tool,
            ResistModifiers = new Dictionary<string, int> { ["exhausted"] = 4 },
            Biome = "forest", ShopTier = 2, Cost = 80,
        },

        // ── Tools: Dungeon-only ──

        ["lattice_ward"] = new()
        {
            Id = "lattice_ward", Name = "Lattice Ward", Type = ItemType.Tool,
            ResistModifiers = new Dictionary<string, int> { ["lattice_sickness"] = 5 },
        },
        ["sakharov_mask"] = new()
        {
            Id = "sakharov_mask", Name = "Sakharov's Mask", Type = ItemType.Tool,
            ResistModifiers = new Dictionary<string, int> { ["irradiated"] = 5 },
        },
        ["antivenom_kit"] = new()
        {
            Id = "antivenom_kit", Name = "Antivenom Kit", Type = ItemType.Tool,
            ResistModifiers = new Dictionary<string, int> { ["poison"] = 5 },
        },

        // ── Food ──
        // One def per FoodType. Display names come from FlavorText.FoodName() at purchase/forage
        // time and are stored on the ItemInstance. Each food item occupies 1 haversack slot.

        ["food_protein"] = new()
        {
            Id = "food_protein", Name = "Meat & Fish", Type = ItemType.Consumable,
            FoodType = Rules.FoodType.Protein, Cost = 3,
        },
        ["food_grain"] = new()
        {
            Id = "food_grain", Name = "Breadstuffs", Type = ItemType.Consumable,
            FoodType = Rules.FoodType.Grain, Cost = 3,
        },
        ["food_sweets"] = new()
        {
            Id = "food_sweets", Name = "Sweets", Type = ItemType.Consumable,
            FoodType = Rules.FoodType.Sweets, Cost = 3,
        },

        // ── Medicines ──

        ["bandages"] = new()
        {
            Id = "bandages", Name = "Bandages", Type = ItemType.Consumable,
            Description = "Clean linen strips treated with pine resin. Cures injured.",
            Cures = new HashSet<string> { "injured" },
            Cost = 3,
        },
        ["siphon_glass"] = new()
        {
            Id = "siphon_glass", Name = "Siphon Glass", Type = ItemType.Consumable,
            Description = "Strange crystalline fragments. It's said they draw out and capture unnatural colors.",
            Cures = new HashSet<string> { "lattice_sickness" },
            Biome = "scrub", ShopTier = 3, Cost = 40,
        },
        ["pale_knot_berry"] = new()
        {
            Id = "pale_knot_berry", Name = "Pale Knot Berry", Type = ItemType.Consumable,
            Description = "Waxy white berries found on wind-stunted shrubs. A handful restores vigor and cures exhaustion.",
            Cures = new HashSet<string> { "exhausted" },
            Biome = "plains", ShopTier = 2, Cost = 15,
        },
        ["shustov_tonic"] = new()
        {
            Id = "shustov_tonic", Name = "Shustov Tonic", Type = ItemType.Consumable,
            Description = "A smoky distillation of charcoal and salt-marsh herbs. Flushes radiation sickness over several nights.",
            Cures = new HashSet<string> { "irradiated" },
            Biome = "plains", ShopTier = 3, Cost = 40,
        },
        ["mudcap_fungus"] = new()
        {
            Id = "mudcap_fungus", Name = "Mudcap Fungus", Type = ItemType.Consumable,
            Description = "A squat brown mushroom with a gritty cap. Eaten raw, it draws poison from the blood.",
            Cures = new HashSet<string> { "poisoned" },
            Biome = "swamp", ShopTier = 2, Cost = 15,
        },

        ["ivory_comb"] = new()
        {
            Id = "ivory_comb", Name = "Ivory Comb", Type = ItemType.Token,
            Description = "A delicate comb carved from yellowed bone, cold to the touch. Faint scratches on the spine might be letters in a language you don't recognize.",
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Negotiation] = 1 },
        },

        ["lucky_buckle"] = new()
        {
            Id = "lucky_buckle", Name = "Lucky Buckle", Type = ItemType.Token,
            Description = "A legionaire's brass buckle. Not so lucky for the previous owner, but you feel a strange attachment to it.",
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Combat] = 1 },
        },

        ["knotwork_seed"] = new()
        {
            Id = "knotwork_seed", Name = "Knotwork Seed", Type = ItemType.Token,
            Description = "An intricately braided seed gifted by the Revënakh. It glows faintly in the dark.",
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Bushcraft] = 1 },
        },

        ["tarnished_key"] = new()
        {
            Id = "tarnished_key", Name = "Tarnished Key", Type = ItemType.Token,
            Description = "A worn key to a door in the Halfway House. A reminder of the importance of discretion.",
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Cunning] = 1 },
        },

        // ── Tokens (capstone arc keys) ──

        ["hunters_journal"] = new()
        {
            Id = "hunters_journal", Name = "Hunter's Journal", Type = ItemType.Token,
            Description = "A small leather-bound book of field observations — animal tracks, edible plants, trail markings in a hand that is meticulous and warm.",
        },
        ["grid_cipher"] = new()
        {
            Id = "grid_cipher", Name = "Grid Cipher", Type = ItemType.Token,
            Description = "A corroded imperial device fitted with rotating discs of etched glass. When held to Grid markings, the symbols resolve into legible warnings.",
        },
        ["color_lens"] = new()
        {
            Id = "color_lens", Name = "Color Lens", Type = ItemType.Token,
            Description = "A disc of treated glass in a brass frame. Looking through it, the Lattice's Colors separate into distinct bands the eye can tolerate.",
        },
        ["revathi_tile"] = new()
        {
            Id = "revathi_tile", Name = "Revathi Tile", Type = ItemType.Token,
            Description = "A fragment of ancient tilework, faintly warm. The geometric pattern on its face shifts when you look away.",
        },
        ["brass_lantern"] = new()
        {
            Id = "brass_lantern", Name = "Old Brass Lantern", Type = ItemType.Tool,
            Description = "A dented lantern of tarnished brass. The glass is cracked but it still holds a flame. Provides light in dark places.",
            Biome = "plains", ShopTier = 1, Cost = 15,
        },

        // ── Haul (generic def — per-haul identity comes from HaulDefId on ItemInstance) ──

        ["haul"] = new()
        {
            Id = "haul", Name = "Haul", Type = ItemType.Haul,
            Description = "A delivery bound for a specific settlement.",
        },
    };
}
