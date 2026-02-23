namespace Dreamlands.Rules;

/// <summary>Type of equipment item.</summary>
public enum ItemType { Tool, Consumable, Token, Weapon, Armor, Boots, TradeGood }

/// <summary>Weapon class for weapon-type items.</summary>
public enum WeaponClass { Dagger, Axe, Sword }

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
    public FoodType? FoodType { get; init; }
    public Magnitude? Cost { get; init; }
    public string? Biome { get; init; }
    public int? ShopTier { get; init; }
    public IReadOnlyDictionary<Skill, int> SkillModifiers { get; init; } = new Dictionary<Skill, int>();
    public IReadOnlyDictionary<string, Magnitude> ResistModifiers { get; init; } = new Dictionary<string, Magnitude>();

    /// <summary>True for items that go in Pack (gear + trade goods). False for consumables that go in Haversack.</summary>
    public bool IsPackItem => Type is ItemType.Weapon or ItemType.Armor or ItemType.Boots or ItemType.Tool or ItemType.TradeGood;

    internal static IReadOnlyDictionary<string, ItemDef> All { get; } = BuildAll();

    static Dictionary<string, ItemDef> BuildAll() => new()
    {
        // ── Weapons ──

        ["bodkin"] = new()
        {
            Id = "bodkin", Name = "Bodkin", Type = ItemType.Weapon,
            WeaponClass = Rules.WeaponClass.Dagger,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Combat] = 1 },
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
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Combat] = 2 },
            Biome = "mountains", ShopTier = 1, Cost = Magnitude.Small,
        },
        ["hatchet"] = new()
        {
            Id = "hatchet", Name = "Hatchet", Type = ItemType.Weapon,
            WeaponClass = Rules.WeaponClass.Axe,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Combat] = 2 },
            Biome = "forest", ShopTier = 1, Cost = Magnitude.Small,
        },
        ["war_axe"] = new()
        {
            Id = "war_axe", Name = "War Axe", Type = ItemType.Weapon,
            WeaponClass = Rules.WeaponClass.Axe,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Combat] = 3 },
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
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Combat] = 4 },
            Biome = "plains", ShopTier = 1, Cost = Magnitude.Huge,
        },

        // ── Armor ──

        ["tunic"] = new()
        {
            Id = "tunic", Name = "Tunic", Type = ItemType.Armor,
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
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Cunning] = -3 },
            ResistModifiers = new Dictionary<string, Magnitude> { ["injured"] = Magnitude.Medium },
            Biome = "plains", ShopTier = 2, Cost = Magnitude.Large,
        },
        ["scale_armor"] = new()
        {
            Id = "scale_armor", Name = "Scale Armor", Type = ItemType.Armor,
            SkillModifiers = new Dictionary<Skill, int> { [Skill.Cunning] = -3 },
            ResistModifiers = new Dictionary<string, Magnitude> { ["injured"] = Magnitude.Medium },
            Biome = "scrub", ShopTier = 2, Cost = Magnitude.Medium,
        },

        // ── Boots ──

        ["fine_boots"] = new()
        {
            Id = "fine_boots", Name = "Fine Boots", Type = ItemType.Boots,
            Biome = "plains", ShopTier = 1, Cost = Magnitude.Large,
        },
        ["riding_boots"] = new()
        {
            Id = "riding_boots", Name = "Riding Boots", Type = ItemType.Boots,
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
            ResistModifiers = new Dictionary<string, Magnitude> { ["irradiated"] = Magnitude.Medium, ["gut_worms"] = Magnitude.Medium },
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
            Cures = new HashSet<string> { "injured" },
            Cost = Magnitude.Trivial,
        },
        ["jorgo_root"] = new()
        {
            Id = "jorgo_root", Name = "Jorgo Root", Type = ItemType.Consumable,
            ResistModifiers = new Dictionary<string, Magnitude> { ["swamp_fever"] = Magnitude.Small },
            Biome = "mountains", ShopTier = 2, Cost = Magnitude.Small,
        },
        ["gravediggers_ear"] = new()
        {
            Id = "gravediggers_ear", Name = "Gravedigger's Ear", Type = ItemType.Consumable,
            Cures = new HashSet<string> { "swamp_fever" },
            Biome = "swamp", ShopTier = 2, Cost = Magnitude.Small,
        },
        ["duskwort"] = new()
        {
            Id = "duskwort", Name = "Duskwort", Type = ItemType.Consumable,
            ResistModifiers = new Dictionary<string, Magnitude> { ["injured"] = Magnitude.Small },
            Biome = "plains", ShopTier = 2, Cost = Magnitude.Small,
        },
        ["thumbroot"] = new()
        {
            Id = "thumbroot", Name = "Thumbroot", Type = ItemType.Consumable,
            Cures = new HashSet<string> { "injured" },
            Biome = "forest", ShopTier = 2, Cost = Magnitude.Small,
        },
        ["wound_sealant"] = new()
        {
            Id = "wound_sealant", Name = "Wound Sealant", Type = ItemType.Consumable,
            Cures = new HashSet<string> { "injured" },
            Biome = "plains", ShopTier = 3, Cost = Magnitude.Medium,
        },
        ["creeping_baldric"] = new()
        {
            Id = "creeping_baldric", Name = "Creeping Baldric", Type = ItemType.Consumable,
            Cures = new HashSet<string> { "gut_worms" },
            Biome = "forest", ShopTier = 2, Cost = Magnitude.Small,
        },
        ["dustseed"] = new()
        {
            Id = "dustseed", Name = "Dustseed", Type = ItemType.Consumable,
            ResistModifiers = new Dictionary<string, Magnitude> { ["gut_worms"] = Magnitude.Small },
            Biome = "plains", ShopTier = 2, Cost = Magnitude.Trivial,
        },
        ["widows_veil"] = new()
        {
            Id = "widows_veil", Name = "Widow's Veil", Type = ItemType.Consumable,
            ResistModifiers = new Dictionary<string, Magnitude> { ["exhausted"] = Magnitude.Small },
            Biome = "swamp", ShopTier = 2, Cost = Magnitude.Small,
        },
        ["pale_knot_berry"] = new()
        {
            Id = "pale_knot_berry", Name = "Pale Knot Berry", Type = ItemType.Consumable,
            Cures = new HashSet<string> { "exhausted" },
            Biome = "plains", ShopTier = 2, Cost = Magnitude.Small,
        },
        ["shustov_tonic"] = new()
        {
            Id = "shustov_tonic", Name = "Shustov Tonic", Type = ItemType.Consumable,
            ResistModifiers = new Dictionary<string, Magnitude> { ["irradiated"] = Magnitude.Small },
            Cures = new HashSet<string> { "irradiated" },
            Biome = "plains", ShopTier = 3, Cost = Magnitude.Medium,
        },
        ["mudcap_fungus"] = new()
        {
            Id = "mudcap_fungus", Name = "Mudcap Fungus", Type = ItemType.Consumable,
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

        // ── Trade Goods: Plains ──

        ["blank_ledger_book"] = new()
        {
            Id = "blank_ledger_book", Name = "Blank Ledger Book", Type = ItemType.TradeGood,
            Description = "A stiff-backed ledger with crisp lined pages and a faint watermark of Aldgate's scales pressed into every sheet.",
            Biome = "plains", ShopTier = 1, Cost = Magnitude.Small,
        },
        ["bolt_of_undyed_wool_cloth"] = new()
        {
            Id = "bolt_of_undyed_wool_cloth", Name = "Bolt of Undyed Wool Cloth", Type = ItemType.TradeGood,
            Description = "A tightly rolled length of practical wool, coarse but even, bound with twine and sealed with a mill stamp.",
            Biome = "plains", ShopTier = 1, Cost = Magnitude.Small,
        },
        ["stamped_grain_weights"] = new()
        {
            Id = "stamped_grain_weights", Name = "Stamped Grain Weights", Type = ItemType.TradeGood,
            Description = "A nested stack of palm-sized brass weights, each etched with imperial measures and worn smooth at the edges.",
            Biome = "plains", ShopTier = 1, Cost = Magnitude.Small,
        },
        ["silver_trade_bar"] = new()
        {
            Id = "silver_trade_bar", Name = "Silver Trade Bar", Type = ItemType.TradeGood,
            Description = "A heavy silver bar. The intricate Merchant Guild stamp proves the purity of the metal.",
            Biome = "plains", ShopTier = 1, Cost = Magnitude.Medium,
        },
        ["army_surplus_spearheads"] = new()
        {
            Id = "army_surplus_spearheads", Name = "Army Surplus Spearheads", Type = ItemType.TradeGood,
            Description = "Five oil-wrapped iron spearheads tied together with cord, their sockets still marked with faded regimental numbers.",
            Biome = "plains", ShopTier = 2, Cost = Magnitude.Small,
        },
        ["salvaged_signal_lantern"] = new()
        {
            Id = "salvaged_signal_lantern", Name = "Salvaged Signal Lantern", Type = ItemType.TradeGood,
            Description = "A dented brass field lantern with intact shutter mechanism and soot still blackening the chimney glass.",
            Biome = "plains", ShopTier = 2, Cost = Magnitude.Small,
        },
        ["cracked_surveyors_transit"] = new()
        {
            Id = "cracked_surveyors_transit", Name = "Cracked Surveyor's Transit", Type = ItemType.TradeGood,
            Description = "A tripod-mounted sighting instrument in a leather case, one lens fractured but the leveling screws still precise.",
            Biome = "plains", ShopTier = 2, Cost = Magnitude.Medium,
        },
        ["vitrified_stone_shards"] = new()
        {
            Id = "vitrified_stone_shards", Name = "Vitrified Stone Shards", Type = ItemType.TradeGood,
            Description = "A cloth-wrapped parcel of unnaturally smooth, glass-sheened stone fragments fused by impossible heat.",
            Biome = "plains", ShopTier = 2, Cost = Magnitude.Small,
        },
        ["depot_iron_mess_kit"] = new()
        {
            Id = "depot_iron_mess_kit", Name = "Depot Iron Mess Kit", Type = ItemType.TradeGood,
            Description = "A hinged iron cook-tin stamped with imperial inventory codes, scrubbed clean but permanently smoke-darkened.",
            Biome = "plains", ShopTier = 2, Cost = Magnitude.Small,
        },
        ["collapsed_tower_bell_fragment"] = new()
        {
            Id = "collapsed_tower_bell_fragment", Name = "Collapsed Tower Bell Fragment", Type = ItemType.TradeGood,
            Description = "A curved section of bronze bell rim heavy in the hand, its casting inscription cut off mid-prayer.",
            Biome = "plains", ShopTier = 2, Cost = Magnitude.Small,
        },
        ["imperial_road_marker_plaque"] = new()
        {
            Id = "imperial_road_marker_plaque", Name = "Imperial Road Marker Plaque", Type = ItemType.TradeGood,
            Description = "A cast-iron directional plaque pried from its post, bearing distances to forts that no longer exist.",
            Biome = "plains", ShopTier = 2, Cost = Magnitude.Small,
        },
        ["grid_regulator_plate"] = new()
        {
            Id = "grid_regulator_plate", Name = "Grid Regulator Plate", Type = ItemType.TradeGood,
            Description = "A fist-sized brass and ceramic control plate engraved with a serial number and threaded ports for unseen machinery.",
            Biome = "plains", ShopTier = 3, Cost = Magnitude.Medium,
        },
        ["untouched_market_coin_chest"] = new()
        {
            Id = "untouched_market_coin_chest", Name = "Untouched Market Coin Chest", Type = ItemType.TradeGood,
            Description = "A compact iron-bound coffer containing tightly stacked rolls of minted coin, each wrapped in paper bands.",
            Biome = "plains", ShopTier = 3, Cost = Magnitude.Large,
        },
        ["golem_memory_cylinder"] = new()
        {
            Id = "golem_memory_cylinder", Name = "Golem Memory Cylinder", Type = ItemType.TradeGood,
            Description = "A palm-length crystal-and-brass cylinder humming faintly when held, its interior script rotating in slow sequence.",
            Biome = "plains", ShopTier = 3, Cost = Magnitude.Large,
        },

        // ── Trade Goods: Mountains ──

        ["ridge_salt_slab"] = new()
        {
            Id = "ridge_salt_slab", Name = "Ridge Salt Slab", Type = ItemType.TradeGood,
            Description = "A hand-cut brick of crystalline mountain salt wrapped in rough cloth and tied with hemp cord.",
            Biome = "mountains", ShopTier = 1, Cost = Magnitude.Small,
        },
        ["charcoal_bundle"] = new()
        {
            Id = "charcoal_bundle", Name = "Charcoal Bundle", Type = ItemType.TradeGood,
            Description = "A compact sack of dense hardwood charcoal, light but brittle, leaving fine black dust on the fingers.",
            Biome = "mountains", ShopTier = 1, Cost = Magnitude.Small,
        },
        ["company_scrip_stack"] = new()
        {
            Id = "company_scrip_stack", Name = "Company Scrip Stack", Type = ItemType.TradeGood,
            Description = "A leather pouch containing stamped tin tokens and folded promissory slips redeemable only at company stores.",
            Biome = "mountains", ShopTier = 1, Cost = Magnitude.Small,
        },
        ["raw_silver_ore_chunk"] = new()
        {
            Id = "raw_silver_ore_chunk", Name = "Raw Silver Ore Chunk", Type = ItemType.TradeGood,
            Description = "A jagged stone fist-sized and heavy, streaked through with dull veins that glint faintly in good light.",
            Biome = "mountains", ShopTier = 1, Cost = Magnitude.Small,
        },
        ["annotated_legal_codex"] = new()
        {
            Id = "annotated_legal_codex", Name = "Annotated Legal Codex", Type = ItemType.TradeGood,
            Description = "A thick-bound volume crowded with marginalia in at least three disciplined hands.",
            Biome = "mountains", ShopTier = 2, Cost = Magnitude.Medium,
        },
        ["precision_astrolabe"] = new()
        {
            Id = "precision_astrolabe", Name = "Precision Astrolabe", Type = ItemType.TradeGood,
            Description = "A velvet-wrapped brass instrument of nested rotating rings etched with star tables and calibration marks.",
            Biome = "mountains", ShopTier = 2, Cost = Magnitude.Medium,
        },
        ["printed_reform_pamphlets"] = new()
        {
            Id = "printed_reform_pamphlets", Name = "Printed Reform Pamphlets", Type = ItemType.TradeGood,
            Description = "A neatly stitched stack of polemical essays printed on fine rag paper and trimmed square.",
            Biome = "mountains", ShopTier = 2, Cost = Magnitude.Small,
        },
        ["sealed_research_dossier"] = new()
        {
            Id = "sealed_research_dossier", Name = "Sealed Research Dossier", Type = ItemType.TradeGood,
            Description = "A wax-sealed folio of diagrams and correspondence stamped with institutional insignia.",
            Biome = "mountains", ShopTier = 2, Cost = Magnitude.Medium,
        },
        ["engineered_retort_glassware"] = new()
        {
            Id = "engineered_retort_glassware", Name = "Engineered Retort Glassware", Type = ItemType.TradeGood,
            Description = "A padded roll containing precisely blown glass bulbs and tubes fitted with corked joints.",
            Biome = "mountains", ShopTier = 2, Cost = Magnitude.Medium,
        },
        ["corrected_mine_safety_blueprint"] = new()
        {
            Id = "corrected_mine_safety_blueprint", Name = "Corrected Mine Safety Blueprint", Type = ItemType.TradeGood,
            Description = "A carefully inked technical drawing rolled into a leather tube and annotated with cautious revisions.",
            Biome = "mountains", ShopTier = 2, Cost = Magnitude.Small,
        },
        ["observatory_lens_disc"] = new()
        {
            Id = "observatory_lens_disc", Name = "Observatory Lens Disc", Type = ItemType.TradeGood,
            Description = "A thick polished crystal disc wrapped in felt, flawless at its center and cold as mountain water.",
            Biome = "mountains", ShopTier = 2, Cost = Magnitude.Large,
        },
        ["filed_petition_year_73"] = new()
        {
            Id = "filed_petition_year_73", Name = "Filed Petition (Year 73)", Type = ItemType.TradeGood,
            Description = "A ribbon-bound stack of parchment forms bearing stamps, counterstamps, and several adjournment notices.",
            Biome = "mountains", ShopTier = 3, Cost = Magnitude.Small,
        },
        ["binding_judgment_writ"] = new()
        {
            Id = "binding_judgment_writ", Name = "Binding Judgment Writ", Type = ItemType.TradeGood,
            Description = "A heavy vellum decree embossed with a raised seal whose authority is felt before it is read.",
            Biome = "mountains", ShopTier = 3, Cost = Magnitude.Medium,
        },
        ["wegtafel_fragment"] = new()
        {
            Id = "wegtafel_fragment", Name = "Wegtafel Fragment", Type = ItemType.TradeGood,
            Description = "A broken stone directional tablet etched with carved arrows that no longer point anywhere useful.",
            Biome = "mountains", ShopTier = 3, Cost = Magnitude.Small,
        },

        // ── Trade Goods: Forest ──

        ["stacked_firewood_bundle"] = new()
        {
            Id = "stacked_firewood_bundle", Name = "Stacked Firewood Bundle", Type = ItemType.TradeGood,
            Description = "A neatly tied armful of seasoned oak lengths cut to uniform size.",
            Biome = "forest", ShopTier = 1, Cost = Magnitude.Small,
        },
        ["tanned_deerhide"] = new()
        {
            Id = "tanned_deerhide", Name = "Tanned Deerhide", Type = ItemType.TradeGood,
            Description = "A supple, smoke-cured hide folded tight, its grain even and free of rot.",
            Biome = "forest", ShopTier = 1, Cost = Magnitude.Small,
        },
        ["charcoal_sack"] = new()
        {
            Id = "charcoal_sack", Name = "Charcoal Sack", Type = ItemType.TradeGood,
            Description = "A small waxed sack of blackened wood chunks sized for steady heat.",
            Biome = "forest", ShopTier = 1, Cost = Magnitude.Small,
        },
        ["pitch_resin_pot"] = new()
        {
            Id = "pitch_resin_pot", Name = "Pitch Resin Pot", Type = ItemType.TradeGood,
            Description = "A stoppered clay jar of thick pine pitch that softens and sharpens in the warmth of the hand.",
            Biome = "forest", ShopTier = 1, Cost = Magnitude.Small,
        },
        ["ghostcap_extract_vials"] = new()
        {
            Id = "ghostcap_extract_vials", Name = "Ghostcap Extract Vials", Type = ItemType.TradeGood,
            Description = "Corked glass vials of cloudy tincture that smells faintly sweet.",
            Biome = "forest", ShopTier = 2, Cost = Magnitude.Medium,
        },
        ["figured_heartwood_planks"] = new()
        {
            Id = "figured_heartwood_planks", Name = "Figured Heartwood Planks", Type = ItemType.TradeGood,
            Description = "Carefully cut boards no longer than a forearm, their grain rippling in rare patterns.",
            Biome = "forest", ShopTier = 2, Cost = Magnitude.Medium,
        },
        ["luck_charms"] = new()
        {
            Id = "luck_charms", Name = "Luck Charms", Type = ItemType.TradeGood,
            Description = "Palm-sized tokens of carved bone bound in copper wire and hung on a knotted cord.",
            Biome = "forest", ShopTier = 2, Cost = Magnitude.Small,
        },
        ["dryads_knot_fungus"] = new()
        {
            Id = "dryads_knot_fungus", Name = "Dryad's Knot Fungus", Type = ItemType.TradeGood,
            Description = "A dried spiral of woody fungus wrapped in cloth, prized for its alchemical properties.",
            Biome = "forest", ShopTier = 2, Cost = Magnitude.Medium,
        },
        ["trail_kit_roll"] = new()
        {
            Id = "trail_kit_roll", Name = "Trail Kit Roll", Type = ItemType.TradeGood,
            Description = "A compact leather roll containing blaze knives, resin markers, and coils of treated twine.",
            Biome = "forest", ShopTier = 2, Cost = Magnitude.Small,
        },
        ["unusual_local_writings"] = new()
        {
            Id = "unusual_local_writings", Name = "Unusual Local Writings", Type = ItemType.TradeGood,
            Description = "A rough-bound journal of unusual observations from one of the more eccentric woodland exiles.",
            Biome = "forest", ShopTier = 2, Cost = Magnitude.Small,
        },
        ["caged_songbird"] = new()
        {
            Id = "caged_songbird", Name = "Caged Songbird", Type = ItemType.TradeGood,
            Description = "A small, iridescent blue noctournal bird, praised for its haunting song.",
            Biome = "forest", ShopTier = 2, Cost = Magnitude.Medium,
        },
        ["unsettling_wooden_doll"] = new()
        {
            Id = "unsettling_wooden_doll", Name = "Unsettling Wooden Doll", Type = ItemType.TradeGood,
            Description = "A crude, hand-made doll founding hanging from a cord in the deep woods.",
            Biome = "forest", ShopTier = 3, Cost = Magnitude.Small,
        },
        ["imperial_standard"] = new()
        {
            Id = "imperial_standard", Name = "Imperial Standard", Type = ItemType.TradeGood,
            Description = "An antique banner in remarkable condition, sparkling with gold thread.",
            Biome = "forest", ShopTier = 3, Cost = Magnitude.Large,
        },
        ["master_snare_mechanism"] = new()
        {
            Id = "master_snare_mechanism", Name = "Master Snare Mechanism", Type = ItemType.TradeGood,
            Description = "A compact spring-loaded trapping device engineered for silent, repeatable deployment.",
            Biome = "forest", ShopTier = 3, Cost = Magnitude.Small,
        },

        // ── Trade Goods: Scrub ──

        ["caravan_spice_pouch"] = new()
        {
            Id = "caravan_spice_pouch", Name = "Caravan Spice Pouch", Type = ItemType.TradeGood,
            Description = "A hefty drawstring pouch of aromatic dried petals and seeds blended in clan-specific proportions.",
            Biome = "scrub", ShopTier = 1, Cost = Magnitude.Medium,
        },
        ["hammered_copper_armbands"] = new()
        {
            Id = "hammered_copper_armbands", Name = "Hammered Copper Armbands", Type = ItemType.TradeGood,
            Description = "A box of flexible copper bands worked by hand and stamped with a subtle clan sigil.",
            Biome = "scrub", ShopTier = 1, Cost = Magnitude.Small,
        },
        ["perfumed_resin_cakes"] = new()
        {
            Id = "perfumed_resin_cakes", Name = "Perfumed Resin Cakes", Type = ItemType.TradeGood,
            Description = "Thumb-thick discs of hardened aromatic resin that release scent when warmed.",
            Biome = "scrub", ShopTier = 1, Cost = Magnitude.Small,
        },
        ["exquisite_kaftan"] = new()
        {
            Id = "exquisite_kaftan", Name = "Exquisite Kaftan", Type = ItemType.TradeGood,
            Description = "A fine garmet, lightweight yet elegant.",
            Biome = "scrub", ShopTier = 1, Cost = Magnitude.Medium,
        },
        ["rail_line_survey"] = new()
        {
            Id = "rail_line_survey", Name = "Rail Line Survey", Type = ItemType.TradeGood,
            Description = "A hefty book documenting the extensive rail lines across the Kesharat empire.",
            Biome = "scrub", ShopTier = 2, Cost = Magnitude.Small,
        },
        ["jade_slab"] = new()
        {
            Id = "jade_slab", Name = "Jade Slab", Type = ItemType.TradeGood,
            Description = "An unworked slab of jade, wrapped in heavy cloth.",
            Biome = "scrub", ShopTier = 2, Cost = Magnitude.Large,
        },
        ["signal_tower_clockwork_core"] = new()
        {
            Id = "signal_tower_clockwork_core", Name = "Signal Tower Clockwork Core", Type = ItemType.TradeGood,
            Description = "A dense brass gear assembly housed in a padded wooden case.",
            Biome = "scrub", ShopTier = 2, Cost = Magnitude.Medium,
        },
        ["the_lattice_an_introduction"] = new()
        {
            Id = "the_lattice_an_introduction", Name = "The Lattice, An Introduction", Type = ItemType.TradeGood,
            Description = "A heavy volume of collected essays explaining the Lattice in patient, circular prose.",
            Biome = "scrub", ShopTier = 2, Cost = Magnitude.Medium,
        },
        ["stamped_rail_spikes"] = new()
        {
            Id = "stamped_rail_spikes", Name = "Stamped Rail Spikes", Type = ItemType.TradeGood,
            Description = "Four heavy iron spikes bound together, each head marked with foundry stamps.",
            Biome = "scrub", ShopTier = 2, Cost = Magnitude.Small,
        },
        ["kesharat_robe"] = new()
        {
            Id = "kesharat_robe", Name = "Kesharat Robe", Type = ItemType.TradeGood,
            Description = "A heavy woolen robe with unusual geometric patterns.",
            Biome = "scrub", ShopTier = 2, Cost = Magnitude.Medium,
        },
        ["confiscated_clan_banner"] = new()
        {
            Id = "confiscated_clan_banner", Name = "Confiscated Clan Banner", Type = ItemType.TradeGood,
            Description = "A carefully folded length of dyed fabric bearing a clan emblem stitched in bold thread.",
            Biome = "scrub", ShopTier = 2, Cost = Magnitude.Small,
        },
        ["unusual_coins"] = new()
        {
            Id = "unusual_coins", Name = "Unusual Coins", Type = ItemType.TradeGood,
            Description = "A box of strange coins from distant lands. Not accepted as currency here, but possibly of interest to collectors.",
            Biome = "scrub", ShopTier = 2, Cost = Magnitude.Small,
        },
        ["alignment_rod"] = new()
        {
            Id = "alignment_rod", Name = "Alignment Rod", Type = ItemType.TradeGood,
            Description = "A smooth metal rod that hums faintly and vibrates in the hand when held upright.",
            Biome = "scrub", ShopTier = 3, Cost = Magnitude.Medium,
        },
        ["colorless_crystal_node"] = new()
        {
            Id = "colorless_crystal_node", Name = "Colorless Crystal Node", Type = ItemType.TradeGood,
            Description = "A multifaceted crystal that seems to refract light without producing any color.",
            Biome = "scrub", ShopTier = 3, Cost = Magnitude.Medium,
        },
        ["worker_identification_band"] = new()
        {
            Id = "worker_identification_band", Name = "Worker Identification Band", Type = ItemType.TradeGood,
            Description = "A metal wrist band engraved with a serial designation and fitted with an unremovable clasp.",
            Biome = "scrub", ShopTier = 3, Cost = Magnitude.Small,
        },

        // ── Trade Goods: Swamp ──

        ["bundled_river_reeds"] = new()
        {
            Id = "bundled_river_reeds", Name = "Bundled River Reeds", Type = ItemType.TradeGood,
            Description = "A tight coil of dried river reeds trimmed evenly and bound for weaving or roofing.",
            Biome = "swamp", ShopTier = 1, Cost = Magnitude.Small,
        },
        ["peat_fuel_brick"] = new()
        {
            Id = "peat_fuel_brick", Name = "Peat Fuel Brick", Type = ItemType.TradeGood,
            Description = "A dense rectangular brick of dried peat wrapped in oilcloth to keep it intact.",
            Biome = "swamp", ShopTier = 1, Cost = Magnitude.Small,
        },
        ["leech_jar"] = new()
        {
            Id = "leech_jar", Name = "Leech Jar", Type = ItemType.TradeGood,
            Description = "A corked glass jar containing several dark, sluggish leeches in cloudy water.",
            Biome = "swamp", ShopTier = 1, Cost = Magnitude.Small,
        },
        ["resin_sealed_waterproof_satchel"] = new()
        {
            Id = "resin_sealed_waterproof_satchel", Name = "Resin-Sealed Waterproof Satchel", Type = ItemType.TradeGood,
            Description = "A treated leather pouch whose seams are sealed in black pitch against water.",
            Biome = "swamp", ShopTier = 1, Cost = Magnitude.Small,
        },
        ["embalming_salts"] = new()
        {
            Id = "embalming_salts", Name = "Embalming Salts", Type = ItemType.TradeGood,
            Description = "A heavy leather drawstring bag of fine mineral salts blended to slow decay.",
            Biome = "swamp", ShopTier = 2, Cost = Magnitude.Small,
        },
        ["blackwater_dye_vial"] = new()
        {
            Id = "blackwater_dye_vial", Name = "Blackwater Dye Vial", Type = ItemType.TradeGood,
            Description = "A stoppered bottle of deep, inky dye that stains cloth almost permanently.",
            Biome = "swamp", ShopTier = 2, Cost = Magnitude.Medium,
        },
        ["venom_ampoules"] = new()
        {
            Id = "venom_ampoules", Name = "Venom Ampoules", Type = ItemType.TradeGood,
            Description = "A padded case holding two sealed glass ampoules filled with pale yellow venom.",
            Biome = "swamp", ShopTier = 2, Cost = Magnitude.Small,
        },
        ["bog_iron_ingot"] = new()
        {
            Id = "bog_iron_ingot", Name = "Bog-Iron Ingot", Type = ItemType.TradeGood,
            Description = "A compact, rough-cast iron bar smelted from marsh deposits, pitted but solid.",
            Biome = "swamp", ShopTier = 2, Cost = Magnitude.Small,
        },
        ["preserved_lotus_resin"] = new()
        {
            Id = "preserved_lotus_resin", Name = "Preserved Lotus Resin", Type = ItemType.TradeGood,
            Description = "A wax-wrapped box of hardened resin nodes used in ritual and scenting.",
            Biome = "swamp", ShopTier = 2, Cost = Magnitude.Medium,
        },
        ["witchfire_lamp"] = new()
        {
            Id = "witchfire_lamp", Name = "Witchfire Lamp", Type = ItemType.TradeGood,
            Description = "An intricate glass terrarium containing a moss that gives off a pale blue light.",
            Biome = "swamp", ShopTier = 2, Cost = Magnitude.Large,
        },
        ["nightbloom_poison_resin"] = new()
        {
            Id = "nightbloom_poison_resin", Name = "Nightbloom Poison Resin", Type = ItemType.TradeGood,
            Description = "A wax-sealed clay jar containing a tar-dark resin that stains the skin.",
            Biome = "swamp", ShopTier = 2, Cost = Magnitude.Small,
        },
        ["marsh_pearl"] = new()
        {
            Id = "marsh_pearl", Name = "Marsh Pearl", Type = ItemType.TradeGood,
            Description = "A fist-sized irregular pearl with shifting iridescence and a faint organic warmth.",
            Biome = "swamp", ShopTier = 3, Cost = Magnitude.Large,
        },
        ["root_head"] = new()
        {
            Id = "root_head", Name = "Root Head", Type = ItemType.TradeGood,
            Description = "A life-sized replica of your own head, formed from a twisted knot of tiny roots.",
            Biome = "swamp", ShopTier = 3, Cost = Magnitude.Medium,
        },
    };
}
