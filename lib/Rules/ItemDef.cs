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

    /// <summary>Cards this item contributes to tactical encounter decks.</summary>
    public IReadOnlyList<TacticalCard> TacticalCards { get; init; } = [];

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
            TacticalCards = [new("Stab desperately", "momentum_to_progress")],
        },
        ["skinning_knife"] = new()
        {
            Id = "skinning_knife", Name = "Skinning Knife", Type = ItemType.Weapon,
            WeaponClass = Rules.WeaponClass.Dagger,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Combat] = 1 },
            ForagingBonus = 2,
            Biome = "swamp", ShopTier = 1, Cost = 15,
            TacticalCards = [new("Slash wildly with your knife", "momentum_to_progress")],
        },
        ["jambiya"] = new()
        {
            Id = "jambiya", Name = "Jambiya", Type = ItemType.Weapon,
            WeaponClass = Rules.WeaponClass.Dagger,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Combat] = 1 },
            ForagingBonus = 2,
            Biome = "scrub", ShopTier = 1, Cost = 15,
            TacticalCards = [new("Strike with your jambiya", "momentum_to_progress")],
        },
        ["seax"] = new()
        {
            Id = "seax", Name = "Seax", Type = ItemType.Weapon,
            WeaponClass = Rules.WeaponClass.Dagger,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Combat] = 1 },
            ForagingBonus = 3,
            Biome = "mountains", ShopTier = 2, Cost = 40,
            TacticalCards = [new("Hack at their defense", "spirits_to_momentum")],
        },
        ["kukri"] = new()
        {
            Id = "kukri", Name = "Kukri", Type = ItemType.Weapon,
            WeaponClass = Rules.WeaponClass.Dagger,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Combat] = 1 },
            ForagingBonus = 3,
            Biome = "scrub", ShopTier = 2, Cost = 40,
            TacticalCards = [new("Stab deep and twist", "momentum_to_cancel")],
        },
        ["hunting_knife"] = new()
        {
            Id = "hunting_knife", Name = "Hunting Knife", Type = ItemType.Weapon,
            WeaponClass = Rules.WeaponClass.Dagger,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Combat] = 1 },
            ForagingBonus = 4,
            Biome = "mountains", ShopTier = 2, Cost = 80,
            TacticalCards = [new("Stab under their guard", "momentum_to_progress")],
        },
        ["the_old_tooth"] = new()
        {
            Id = "the_old_tooth", Name = "The Old Tooth", Type = ItemType.Weapon,
            WeaponClass = Rules.WeaponClass.Dagger,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Combat] = 1 },
            ForagingBonus = 5,
            TacticalCards = [new("Wish upon the Old Tooth", "free_cancel")],
        },

        // ── Weapons: Axes (Combat +1 to +4, Foraging +1 to +3) ──

        ["hatchet"] = new()
        {
            Id = "hatchet", Name = "Hatchet", Type = ItemType.Weapon,
            WeaponClass = Rules.WeaponClass.Axe,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Combat] = 1 },
            ForagingBonus = 1,
            Biome = "forest", ShopTier = 1, Cost = 15,
            TacticalCards = [new("Bury the hatchet in their guard", "momentum_to_progress")],
        },
        ["tomahawk"] = new()
        {
            Id = "tomahawk", Name = "Tomahawk", Type = ItemType.Weapon,
            WeaponClass = Rules.WeaponClass.Axe,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Combat] = 2 },
            ForagingBonus = 1,
            Biome = "forest", ShopTier = 1, Cost = 15,
            TacticalCards =
            [
                new("Push past their defense and chop", "threat_to_progress"),
                new("Exploit a gap in their guard", "momentum_to_progress"),
            ],
        },
        ["war_axe"] = new()
        {
            Id = "war_axe", Name = "War Axe", Type = ItemType.Weapon,
            WeaponClass = Rules.WeaponClass.Axe,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Combat] = 2 },
            ForagingBonus = 2,
            Biome = "forest", ShopTier = 2, Cost = 40,
            TacticalCards =
            [
                new("Bring the axe down with both hands", "momentum_to_progress_large"),
                new("Drive the beard of the axe into their defense", "momentum_to_progress"),
            ],
        },
        ["broadaxe"] = new()
        {
            Id = "broadaxe", Name = "Broadaxe", Type = ItemType.Weapon,
            WeaponClass = Rules.WeaponClass.Axe,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Combat] = 3 },
            ForagingBonus = 2,
            Biome = "mountains", ShopTier = 2, Cost = 80,
            TacticalCards =
            [
                new("Bring the broadaxe down in a terrible arc", "momentum_to_progress_large"),
                new("Wind up and put everything behind the swing", "momentum_to_progress_huge"),
                new("Shove them back with the axe's haft", "free_momentum"),
            ],
        },
        ["bardiche"] = new()
        {
            Id = "bardiche", Name = "Bardiche", Type = ItemType.Weapon,
            WeaponClass = Rules.WeaponClass.Axe,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Combat] = 3 },
            ForagingBonus = 1,
            Biome = "mountains", ShopTier = 2, Cost = 80,
            TacticalCards =
            [
                new("Swing the bardiche in a wide, committed arc", "momentum_to_progress_large"),
                new("Drive the axe's spike into their foot", "free_momentum"),
                new("Brace the shaft and let them come to you", "threat_to_progress_large"),
            ],
        },
        ["revathi_labrys"] = new()
        {
            Id = "revathi_labrys", Name = "Revathi Labrys", Type = ItemType.Weapon,
            WeaponClass = Rules.WeaponClass.Axe,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Combat] = 4 },
            ForagingBonus = 3,
            TacticalCards =
            [
                new("Bring the labrys down like a felled tree", "momentum_to_progress_huge"),
                new("Let the weight carry through in a brutal arc", "momentum_to_progress_large"),
                new("The ground trembles where the labrys strikes", "free_momentum"),
                new("Call upon the Revathi to guide your arm", "spirits_to_progress_large"),
            ],
        },

        // ── Weapons: Swords (Foraging +0 cap, Combat +1 to +5) ──

        ["falchion"] = new()
        {
            Id = "falchion", Name = "Falchion", Type = ItemType.Weapon,
            WeaponClass = Rules.WeaponClass.Sword,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Combat] = 2 },
            Biome = "plains", ShopTier = 1, Cost = 15,
            TacticalCards =
            [
                new("Hack at their defense", "spirits_to_momentum"),
                new("Swing your blade in an arcing chop", "momentum_to_progress"),
            ],
        },
        ["short_sword"] = new()
        {
            Id = "short_sword", Name = "Short Sword", Type = ItemType.Weapon,
            WeaponClass = Rules.WeaponClass.Sword,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Combat] = 3 },
            Biome = "plains", ShopTier = 2, Cost = 40,
            TacticalCards =
            [
                new("Probe their defenses", "free_momentum"),
                new("Thrust your blade through an opening", "momentum_to_progress"),
                new("Exploit their error", "momentum_to_cancel"),
            ],
        },
        ["tulwar"] = new()
        {
            Id = "tulwar", Name = "Tulwar", Type = ItemType.Weapon,
            WeaponClass = Rules.WeaponClass.Sword,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Combat] = 3 },
            Biome = "scrub", ShopTier = 2, Cost = 40,
            TacticalCards =
            [
                new("Hack and slash", "momentum_to_progress"),
                new("Bring your tulwar down in a brutal chop", "momentum_to_progress_large"),
                new("Exploit their error", "momentum_to_cancel"),
            ],
        },
        ["scimitar"] = new()
        {
            Id = "scimitar", Name = "Scimitar", Type = ItemType.Weapon,
            WeaponClass = Rules.WeaponClass.Sword,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Combat] = 4 },
            Biome = "scrub", ShopTier = 2, Cost = 80,
            TacticalCards =
            [
                new("Hammer their defense with quick slashes", "free_momentum"),
                new("Feint high, then kick them off balance", "momentum_to_progress"),
                new("Exploit their error", "momentum_to_cancel"),
                new("Making a daring attack", "threat_to_progress"),
            ],
        },
        ["arming_sword"] = new()
        {
            Id = "arming_sword", Name = "Arming Sword", Type = ItemType.Weapon,
            WeaponClass = Rules.WeaponClass.Sword,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Combat] = 4 },
            Biome = "plains", ShopTier = 2, Cost = 80,
            TacticalCards =
            [
                new("Thrust, then pull back with a draw cut", "momentum_to_progress"),
                new("Grasp your blade and hammer with the pommel", "momentum_to_progress_large"),
                new("Step in close and trip them", "spirits_to_cancel"),
                new("Bind and control their weapon", "free_momentum"),
            ],
        },
        ["shimmering_blade"] = new()
        {
            Id = "shimmering_blade", Name = "Shimmering Blade", Type = ItemType.Weapon,
            WeaponClass = Rules.WeaponClass.Sword,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Combat] = 5 },
            TacticalCards =
            [
                new("Obscure your strikes", "free_momentum"),
                new("Lace the air with uncolor", "momentum_to_progress"),
                new("Burn through iron and sinew", "momentum_to_cancel"),
                new("Let the Lattice guide your hand", "threat_to_progress_large"),
                new("Unleash the Lattice from the blade", "free_cancel"),
            ],
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
            TacticalCards = [new("Tread softly", "free_momentum")],
        },
        ["hunters_gear"] = new()
        {
            Id = "hunters_gear", Name = "Hunter's Gear", Type = ItemType.Armor,
            ArmorClass = Rules.ArmorClass.Light,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Cunning] = 2 },
            ResistModifiers = new Dictionary<string, int> { ["freezing"] = 1 },
            Biome = "swamp", ShopTier = 1, Cost = 15,
            TacticalCards =
            [
                new("Blend with the terrain", "free_momentum"),
                new("Move while they're not looking", "momentum_to_progress"),
            ],
        },
        ["cartographers_cloak"] = new()
        {
            Id = "cartographers_cloak", Name = "Cartographer's Cloak", Type = ItemType.Armor,
            ArmorClass = Rules.ArmorClass.Light,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Cunning] = 3 },
            ResistModifiers = new Dictionary<string, int> { ["freezing"] = 2 },
            Biome = "mountains", ShopTier = 2, Cost = 40,
            TacticalCards =
            [
                new("Move with conviction", "free_momentum"),
                new("Inch forward cautiously", "spirits_to_progress"),
                new("Slip by unnoticed", "momentum_to_cancel"),
            ],
        },
        ["desert_scout_gear"] = new()
        {
            Id = "desert_scout_gear", Name = "Desert Scout Gear", Type = ItemType.Armor,
            ArmorClass = Rules.ArmorClass.Light,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Cunning] = 4 },
            ResistModifiers = new Dictionary<string, int> { ["freezing"] = 2 },
            Biome = "scrub", ShopTier = 2, Cost = 80,
            TacticalCards =
            [
                new("Blend with the terrain", "free_momentum"),
                new("Hurl pocket sand", "momentum_to_progress"),
                new("Slip by unnoticed", "momentum_to_cancel"),
                new("Sprint to cover", "threat_to_progress_large"),
            ],
        },
        ["robe_of_twilight"] = new()
        {
            Id = "robe_of_twilight", Name = "Robe of Twilight", Type = ItemType.Armor,
            ArmorClass = Rules.ArmorClass.Light,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Cunning] = 5 },
            ResistModifiers = new Dictionary<string, int> { ["freezing"] = 3 },
            TacticalCards =
            [
                new("Gather shadows around you", "free_momentum"),
                new("Glide forward silently", "momentum_to_progress"),
                new("Cast terrifying shadows", "momentum_to_cancel"),
                new("Step between shadows", "momentum_to_progress_large"),
                new("Conjure a shadow beast", "free_cancel"),
            ],
        },

        // ── Armor: Medium (Cunning +1 to +2, Injury +1 to +3, Freezing +1 to +5) ──

        ["leather"] = new()
        {
            Id = "leather", Name = "Leather", Type = ItemType.Armor,
            ArmorClass = Rules.ArmorClass.Medium,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Cunning] = 1 },
            ResistModifiers = new Dictionary<string, int> { ["injured"] = 1, ["freezing"] = 1 },
            Biome = "forest", ShopTier = 1, Cost = 15,
            TacticalCards = [new("Tread softly", "free_momentum")],
        },
        ["hide_armor"] = new()
        {
            Id = "hide_armor", Name = "Hide Armor", Type = ItemType.Armor,
            ArmorClass = Rules.ArmorClass.Medium,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Cunning] = 1 },
            ResistModifiers = new Dictionary<string, int> { ["injured"] = 1, ["freezing"] = 2 },
            Biome = "mountains", ShopTier = 1, Cost = 15,
            TacticalCards = [new("Blend with the terrain", "free_momentum")],
        },
        ["buff_coat"] = new()
        {
            Id = "buff_coat", Name = "Buff Coat", Type = ItemType.Armor,
            ArmorClass = Rules.ArmorClass.Medium,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Cunning] = 1 },
            ResistModifiers = new Dictionary<string, int> { ["injured"] = 2, ["freezing"] = 3 },
            Biome = "forest", ShopTier = 2, Cost = 40,
            TacticalCards = [new("Tread softly", "free_momentum")],
        },
        ["lamellar"] = new()
        {
            Id = "lamellar", Name = "Lamellar", Type = ItemType.Armor,
            ArmorClass = Rules.ArmorClass.Medium,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Cunning] = 2 },
            ResistModifiers = new Dictionary<string, int> { ["injured"] = 2, ["freezing"] = 3 },
            Biome = "mountains", ShopTier = 2, Cost = 80,
            TacticalCards =
            [
                new("Move while they're not looking", "momentum_to_progress"),
                new("Blend with the terrain", "free_momentum"),
            ],
        },
        ["mountain_regiment_armor"] = new()
        {
            Id = "mountain_regiment_armor", Name = "17th Mountain Regiment Armor", Type = ItemType.Armor,
            ArmorClass = Rules.ArmorClass.Medium,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Cunning] = 2 },
            ResistModifiers = new Dictionary<string, int> { ["injured"] = 3, ["freezing"] = 5 },
            TacticalCards =
            [
                new("Move with uncanny speed", "spirits_to_momentum"),
                new("Put your faith in the armor", "threat_to_progress_large"),
            ],
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
            TacticalCards =
            [
                new("Drop a name", "free_momentum"),
                new("Mention your patron", "momentum_to_progress"),
            ],
        },
        ["peoples_borderlands"] = new()
        {
            Id = "peoples_borderlands", Name = "A Guide to the Borderlands", Type = ItemType.Tool,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Negotiation] = 3 },
            Biome = "mountains", ShopTier = 2, Cost = 80,
            TacticalCards =
            [
                new("Quote the book", "momentum_to_progress"),
                new("Cite a precedent", "momentum_to_progress_large"),
                new("Show you understand their ways", "free_momentum"),
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
                new("Check the diary", "free_momentum"),
            ],
        },
        ["ornate_spyglass"] = new()
        {
            Id = "ornate_spyglass", Name = "Ornate Spyglass", Type = ItemType.Tool,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Bushcraft] = 3 },
            Biome = "scrub", ShopTier = 2, Cost = 80,
            TacticalCards =
            [
                new("Scout ahead", "free_momentum"),
                new("Spot the path", "momentum_to_progress"),
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
            Biome = "scrub", ShopTier = 2, Cost = 40,
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
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Combat] = 1 },
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
