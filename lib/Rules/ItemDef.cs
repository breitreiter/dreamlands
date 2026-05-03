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
    public int? Cost { get; init; }
    public string? Biome { get; init; }
    public int? ShopTier { get; init; }
    public IReadOnlyDictionary<Skill, int> SkillModifiers { get; init; } = new Dictionary<Skill, int>();
    public IReadOnlyDictionary<string, int> ResistModifiers { get; init; } = new Dictionary<string, int>();

    /// <summary>RPS combat moves contributed when equipped, in encoded form (e.g.
    /// "Riposte Attack", "Big Rare Defend"). Parsed lazily by the combat profile
    /// builder. See super_rps.md § Item Movesets.</summary>
    public IReadOnlyList<string> RpsMoves { get; init; } = Array.Empty<string>();

    /// <summary>Minimum Combat skill required to wield/wear this item meaningfully.
    /// 0 = daggers/light, 2 = axes/medium, 4 = swords/heavy. Not enforced today —
    /// players can equip above their level (TODO: gate at equip-time).</summary>
    public int RequiredCombat { get; init; } = 0;

    /// <summary>Cards this item contributes to tactical encounter decks.
    /// Tactical encounters are slated for removal; new items leave this empty.</summary>
    public IReadOnlyList<TacticalCard> TacticalCards { get; init; } = [];

    /// <summary>True for items that go in Pack (gear + trade goods). False for consumables that go in Haversack.</summary>
    public bool IsPackItem => Type is ItemType.Weapon or ItemType.Armor or ItemType.Boots or ItemType.Tool or ItemType.Haul;

    public static IReadOnlyDictionary<string, ItemDef> All { get; } = BuildAll();

    public static bool IsValidId(string id) => All.ContainsKey(id);

    static Dictionary<string, ItemDef> BuildAll() => new()
    {
        // ── Weapons: Daggers (Combat +1 to +5, cancel-focused) ──
        // Pool: momentum_to_progress, momentum_to_cancel, free_momentum, spirits_to_cancel, free_cancel

        ["hunting_knife"] = new()
        {
            Id = "hunting_knife", Name = "Hunting Knife", Type = ItemType.Weapon,
            WeaponClass = Rules.WeaponClass.Dagger,
            RequiredCombat = 0,
            RpsMoves = ["Attack"],
            Biome = "plains", ShopTier = 1, Cost = 15,
        },
        ["kukri"] = new()
        {
            Id = "kukri", Name = "Kukri", Type = ItemType.Weapon,
            WeaponClass = Rules.WeaponClass.Dagger,
            RequiredCombat = 0,
            RpsMoves = ["Attack", "Stunning Power Attack"],
            Biome = "scrub", ShopTier = 2, Cost = 40,
        },
        ["seax"] = new()
        {
            Id = "seax", Name = "Fine Seax", Type = ItemType.Weapon,
            WeaponClass = Rules.WeaponClass.Dagger,
            RequiredCombat = 0,
            RpsMoves = ["Attack", "Riposte Attack"],
            Biome = "mountains", ShopTier = 2, Cost = 80,
        },
        ["the_old_tooth"] = new()
        {
            Id = "the_old_tooth", Name = "The Old Tooth", Type = ItemType.Weapon,
            WeaponClass = Rules.WeaponClass.Dagger,
            RequiredCombat = 0,
            RpsMoves = ["Riposte Attack", "Heavy Provoking Attack"],
        },

        // ── Weapons: Axes (Combat +1 to +5, aggro-focused, zero cancels) ──
        // Pool: momentum_to_progress, free_momentum, momentum_to_progress_large, threat_to_progress_large, momentum_to_progress_huge

        ["hatchet"] = new()
        {
            Id = "hatchet", Name = "Hatchet", Type = ItemType.Weapon,
            WeaponClass = Rules.WeaponClass.Axe,
            RequiredCombat = 2,
            RpsMoves = ["Attack"],
            Biome = "forest", ShopTier = 1, Cost = 15,
        },
        ["war_axe"] = new()
        {
            Id = "war_axe", Name = "War Axe", Type = ItemType.Weapon,
            WeaponClass = Rules.WeaponClass.Axe,
            RequiredCombat = 2,
            RpsMoves = ["Attack", "Heavy Power Attack"],
            Biome = "forest", ShopTier = 2, Cost = 40,
        },
        ["broadaxe"] = new()
        {
            Id = "broadaxe", Name = "Broadaxe", Type = ItemType.Weapon,
            WeaponClass = Rules.WeaponClass.Axe,
            RequiredCombat = 2,
            RpsMoves = ["Attack", "Brutal Heavy Power Attack"],
            Biome = "mountains", ShopTier = 2, Cost = 80,
        },
        ["revathi_labrys"] = new()
        {
            Id = "revathi_labrys", Name = "Revathi Labrys", Type = ItemType.Weapon,
            WeaponClass = Rules.WeaponClass.Axe,
            RequiredCombat = 2,
            RpsMoves = ["Heavy Attack", "Heavy Slow Terrifying Attack"],
        },

        // ── Weapons: Swords (Combat +1 to +5, hybrid) ──
        // Pool: momentum_to_progress, free_momentum, momentum_to_cancel, momentum_to_progress_large, free_cancel

        ["falchion"] = new()
        {
            Id = "falchion", Name = "Falchion", Type = ItemType.Weapon,
            WeaponClass = Rules.WeaponClass.Sword,
            RequiredCombat = 4,
            RpsMoves = ["Attack"],
            Biome = "plains", ShopTier = 1, Cost = 15,
        },
        ["short_sword"] = new()
        {
            Id = "short_sword", Name = "Short Sword", Type = ItemType.Weapon,
            WeaponClass = Rules.WeaponClass.Sword,
            RequiredCombat = 4,
            RpsMoves = ["Riposte Attack"],
            Biome = "plains", ShopTier = 1, Cost = 15,
        },
        ["scimitar"] = new()
        {
            Id = "scimitar", Name = "Scimitar", Type = ItemType.Weapon,
            WeaponClass = Rules.WeaponClass.Sword,
            RequiredCombat = 4,
            RpsMoves = ["Attack", "Big Rare Defend"],
            Biome = "scrub", ShopTier = 2, Cost = 80,
        },
        ["shimmering_blade"] = new()
        {
            Id = "shimmering_blade", Name = "Shimmering Blade", Type = ItemType.Weapon,
            WeaponClass = Rules.WeaponClass.Sword,
            RequiredCombat = 4,
            RpsMoves = ["Riposte Attack", "Big Wary Recover"],
        },

        // ── Armor: Light (Cunning +0 to +5, Injury +0, Freezing +0 to +3) ──

        ["tunic"] = new()
        {
            Id = "tunic", Name = "Tunic", Type = ItemType.Armor,
            ArmorClass = Rules.ArmorClass.Light,
            RequiredCombat = 0,
            RpsMoves = ["Defend"],
            Biome = "plains", ShopTier = 1,
        },
        ["silks"] = new()
        {
            Id = "silks", Name = "Silks", Type = ItemType.Armor,
            ArmorClass = Rules.ArmorClass.Light,
            RequiredCombat = 0,
            RpsMoves = ["Defend", "Wary Read"],
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Cunning] = 1 },
            Biome = "scrub", ShopTier = 1, Cost = 15,
        },
        ["cartographers_cloak"] = new()
        {
            Id = "cartographers_cloak", Name = "Cartographer's Cloak", Type = ItemType.Armor,
            ArmorClass = Rules.ArmorClass.Light,
            RequiredCombat = 0,
            RpsMoves = ["Wary Read", "Rare Wary Recover"],
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Cunning] = 3 },
            ResistModifiers = new Dictionary<string, int> { ["freezing"] = 2 },
            Biome = "mountains", ShopTier = 2, Cost = 40,
        },
        ["robe_of_twilight"] = new()
        {
            Id = "robe_of_twilight", Name = "Robe of Twilight", Type = ItemType.Armor,
            ArmorClass = Rules.ArmorClass.Light,
            RequiredCombat = 0,
            RpsMoves = ["Big Rare Wary Recover", "Mythic Perfect Defend"],
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Cunning] = 5 },
            ResistModifiers = new Dictionary<string, int> { ["freezing"] = 3 },
        },

        // ── Armor: Medium (Cunning +1 to +2, Injury +1 to +3, Freezing +1 to +5) ──

        ["hide_armor"] = new()
        {
            Id = "hide_armor", Name = "Hide Armor", Type = ItemType.Armor,
            ArmorClass = Rules.ArmorClass.Medium,
            RequiredCombat = 2,
            RpsMoves = ["Defend"],
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Cunning] = 1 },
            ResistModifiers = new Dictionary<string, int> { ["injured"] = 1, ["freezing"] = 2 },
            Biome = "mountains", ShopTier = 1, Cost = 15,
        },
        ["lamellar"] = new()
        {
            Id = "lamellar", Name = "Lamellar", Type = ItemType.Armor,
            ArmorClass = Rules.ArmorClass.Medium,
            RequiredCombat = 2,
            RpsMoves = ["Defend", "Big Rare Defend"],
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Cunning] = 2 },
            ResistModifiers = new Dictionary<string, int> { ["injured"] = 2, ["freezing"] = 3 },
            Biome = "mountains", ShopTier = 2, Cost = 80,
        },
        ["mountain_regiment_armor"] = new()
        {
            Id = "mountain_regiment_armor", Name = "17th Mountain Regiment Armor", Type = ItemType.Armor,
            ArmorClass = Rules.ArmorClass.Medium,
            RequiredCombat = 2,
            RpsMoves = ["Big Rare Defend", "Rare Wary Recover"],
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Cunning] = 2 },
            ResistModifiers = new Dictionary<string, int> { ["injured"] = 3, ["freezing"] = 5 },
        },

        // ── Armor: Heavy (Injury +1 to +5, Cunning +0, Freezing +0 to +2) ──

        ["gambeson"] = new()
        {
            Id = "gambeson", Name = "Gambeson", Type = ItemType.Armor,
            ArmorClass = Rules.ArmorClass.Heavy,
            RequiredCombat = 4,
            RpsMoves = ["Defend"],
            ResistModifiers = new Dictionary<string, int> { ["injured"] = 1, ["freezing"] = 1 },
            Biome = "mountains", ShopTier = 1, Cost = 15,
        },
        ["scale_armor"] = new()
        {
            Id = "scale_armor", Name = "Scale Armor", Type = ItemType.Armor,
            ArmorClass = Rules.ArmorClass.Heavy,
            RequiredCombat = 4,
            RpsMoves = ["Big Defend"],
            ResistModifiers = new Dictionary<string, int> { ["injured"] = 3 },
            Biome = "scrub", ShopTier = 2, Cost = 40,
        },
        ["brigandine"] = new()
        {
            Id = "brigandine", Name = "Brigandine", Type = ItemType.Armor,
            ArmorClass = Rules.ArmorClass.Heavy,
            RequiredCombat = 4,
            RpsMoves = ["Defend", "Big Rare Shielding Defend"],
            ResistModifiers = new Dictionary<string, int> { ["injured"] = 4, ["freezing"] = 1 },
            Biome = "plains", ShopTier = 2, Cost = 80,
        },
        ["golem_armor"] = new()
        {
            Id = "golem_armor", Name = "Golem Armor", Type = ItemType.Armor,
            ArmorClass = Rules.ArmorClass.Heavy,
            RequiredCombat = 4,
            RpsMoves = ["Big Defend", "Perfect Rare Defend"],
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
        ["scarecrow_boots"] = new()
        {
            Id = "scarecrow_boots", Name = "Scarecrow Boots", Type = ItemType.Boots,
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
            TacticalCards =
            [
                new("Select an appropriate letter of introduction and present it", "free_momentum"),
                new("Casually mention your guild patron", "momentum_to_progress"),
            ],
        },
        ["peoples_borderlands"] = new()
        {
            Id = "peoples_borderlands", Name = "A Guide to the Borderlands", Type = ItemType.Tool,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Negotiation] = 3 },
            Biome = "mountains", ShopTier = 2, Cost = 80,
            TacticalCards =
            [
                new("Quote A Guide to the Borderlands", "momentum_to_progress"),
                new("Mention a relevant historical fact", "momentum_to_progress_large"),
                new("Cite imperial scholarship on the matter", "free_momentum"),
            ],
        },
        ["cartographers_diary"] = new()
        {
            Id = "cartographers_diary", Name = "Cartographer's Diary", Type = ItemType.Tool,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Bushcraft] = 2 },
            Biome = "mountain", ShopTier = 1, Cost = 40,
            TacticalCards =
            [
                new("Recall a story about this place", "momentum_to_progress"),
                new("Check the cartograph's diary for notes on this place", "free_momentum"),
            ],
        },
        ["ornate_spyglass"] = new()
        {
            Id = "ornate_spyglass", Name = "Ornate Spyglass", Type = ItemType.Tool,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Bushcraft] = 3 },
            Biome = "scrub", ShopTier = 2, Cost = 80,
            TacticalCards =
            [
                new("Use the spyglass to scout ahead", "free_momentum"),
                new("Keep following the path you scouted", "momentum_to_progress"),
                new("Glass the danger", "momentum_to_cancel"),
            ],
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
        // Single ration item; display name set via FlavorText.RationName(biome) at refill time.
        // 1 ration = 1 day of food = 1 haversack slot. Free at any settlement.

        ["food_ration"] = new()
        {
            Id = "food_ration", Name = "Rations", Type = ItemType.Consumable,
            Cost = 3,
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
            Biome = "scrub", ShopTier = 2, Cost = 40,
        },
        // Removed: pale_knot_berry cured exhausted, but exhaustion is now ClearedOnSettlement.
        // Minor conditions no longer have item cures (haversack_refactor.md).
        ["shustov_tonic"] = new()
        {
            Id = "shustov_tonic", Name = "Shustov Tonic", Type = ItemType.Consumable,
            Description = "A smoky distillation of charcoal and salt-marsh herbs. Flushes radiation sickness over several nights.",
            Cures = new HashSet<string> { "irradiated" },
            Biome = "plains", ShopTier = 2, Cost = 40,
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
            TacticalCards = [new("Listen to the ghostly whispers", "spirits_to_momentum")],
        },

        ["lucky_buckle"] = new()
        {
            Id = "lucky_buckle", Name = "Lucky Buckle", Type = ItemType.Token,
            Description = "A legionaire's brass buckle. Not so lucky for the previous owner, but you feel a strange attachment to it.",
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Combat] = 2 },
            TacticalCards = [new("Trust your luck", "spirits_to_cancel")],
        },

        ["knotwork_seed"] = new()
        {
            Id = "knotwork_seed", Name = "Knotwork Seed", Type = ItemType.Token,
            Description = "An intricately braided seed gifted by the Revënakh. It glows faintly in the dark.",
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Bushcraft] = 1 },
            TacticalCards = [new("Trust the seed", "momentum_to_progress_large")],
        },

        ["tarnished_key"] = new()
        {
            Id = "tarnished_key", Name = "Tarnished Key", Type = ItemType.Token,
            Description = "A worn key to a door in the Halfway House. A reminder of the importance of discretion.",
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Cunning] = 1 },
            TacticalCards = [new("Remember the key's lesson", "free_momentum")],
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
        ["control_shaft"] = new()
        {
            Id = "control_shaft", Name = "Control Shaft", Type = ItemType.Tool,
            Description = "A metal rod wrapped in waxed linen and bound with cording. An imperial armory seal holds the bindings in place.",
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
