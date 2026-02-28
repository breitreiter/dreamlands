using Dreamlands.Map;
using Dreamlands.Rules;

namespace Dreamlands.Flavor;

public static class FlavorText
{
    static int _counter;

    // --- Map regions (biome, tier) ---

    public static string RegionName(Terrain biome, int tier)
    {
        var prefix = TierPrefix(tier);
        var noun = biome switch
        {
            Terrain.Plains => Pick("Meadows", "Steppe", "Grasslands", "Fields"),
            Terrain.Forest => Pick("Woods", "Timberlands", "Thicket", "Greenwood"),
            Terrain.Scrub => Pick("Barrens", "Drylands", "Wastes", "Flats"),
            Terrain.Mountains => Pick("Peaks", "Crags", "Spires", "Heights"),
            Terrain.Swamp => Pick("Mire", "Fens", "Bogs", "Marshes"),
            Terrain.Lake => Pick("Shallows", "Deeps", "Waters", "Mere"),
            _ => "Wastes",
        };
        return $"The {prefix} {noun}";
    }

    public static string TileDescription(Terrain biome, int tier)
    {
        var mood = TierMood(tier);
        return biome switch
        {
            Terrain.Plains => $"{mood} grasslands stretch to the horizon.",
            Terrain.Forest => $"A {mood.ToLower()} forest presses in from all sides.",
            Terrain.Scrub => $"{mood} scrubland stretches to the horizon, dry and sparse.",
            Terrain.Mountains => $"{mood} peaks loom overhead, jagged against the sky.",
            Terrain.Swamp => $"A {mood.ToLower()} swamp spreads underfoot, thick with mist.",
            Terrain.Lake => "Still water reflects the sky.",
            _ => "Barren ground.",
        };
    }

    // --- Settlements (biome, tier, size) ---

    public static string SettlementName(Terrain biome, int tier, SettlementSize size)
    {
        var root = biome switch
        {
            Terrain.Plains => Pick("Wheat", "Wind", "Sun", "Grass"),
            Terrain.Forest => Pick("Oak", "Elm", "Thorn", "Moss"),
            Terrain.Scrub => Pick("Dust", "Thorn", "Sand", "Flint"),
            Terrain.Mountains => Pick("Iron", "Frost", "Crag", "Granite"),
            Terrain.Swamp => Pick("Fog", "Reed", "Mire", "Murk"),
            _ => "Lake",
        };
        var suffix = size switch
        {
            SettlementSize.City => "gate",
            SettlementSize.Town => "ford",
            SettlementSize.Village => "hollow",
            SettlementSize.Outpost => "watch",
            SettlementSize.Camp => "rest",
            _ => "stead",
        };
        return $"{root}{suffix}";
    }

    public static string SettlementDescription(Terrain biome, int tier, SettlementSize size)
    {
        var sizeDesc = size switch
        {
            SettlementSize.City => "A sprawling city",
            SettlementSize.Town => "A busy town",
            SettlementSize.Village => "A modest village",
            SettlementSize.Outpost => "A small outpost",
            SettlementSize.Camp => "A rough camp",
            _ => "A settlement",
        };
        var biomeDesc = biome switch
        {
            Terrain.Plains => "amid open fields",
            Terrain.Forest => "beneath the canopy",
            Terrain.Scrub => "huddled in the scrubland",
            Terrain.Mountains => "nestled in a mountain pass",
            Terrain.Swamp => "built on stilts above the bog",
            _ => "by the water's edge",
        };
        var tierDesc = tier switch
        {
            1 => "Lanterns glow warmly and the streets feel safe.",
            2 => "The locals eye strangers with wary caution.",
            _ => "Shadows cling to every corner and the air feels wrong.",
        };
        return $"{sizeDesc} {biomeDesc}. {tierDesc}";
    }

    public static string GuildOfficeDescription(Terrain biome, int tier, SettlementSize size) =>
        "The guild office is open for business.";

    public static string MarketDescription(Terrain biome, int tier, SettlementSize size) =>
        "A bustling market square.";

    public static string GuildOfficeRumors(Terrain biome, int tier, SettlementSize size, string priceSheet) =>
        "No rumors today.";

    public static string TempleDescription(Terrain biome, int tier, SettlementSize size) =>
        "A quiet temple.";

    public static string InnDescription(Terrain biome, int tier, SettlementSize size) =>
        "A warm inn with a crackling fire.";

    // --- Other elements (biome, tier) ---

    public static string TimeOfDayDescription(Terrain biome, int tier) =>
        "The sky stretches overhead.";

    public static string WeatherDescription(Terrain biome, int tier, string weatherState) =>
        "The weather is unremarkable.";

    // --- Loose elements ---

    public static string ConditionWarning(string condition) =>
        $"You are affected by {condition}.";

    // --- Items ---

    public static string ItemDescription(string itemId) => itemId switch
    {
        // Weapons
        "bodkin" => "More suited to court than the frontier.",
        "jambiya" => "Curved dagger carried by nearly every adult desert clan member.",
        "seax" => "A utility knife common among the hill folk.",
        "hatchet" => "Ubiquitous tool of the forest folk.",
        "war_axe" => "A heavy axe built for war.",
        "bardiche" => "A mighty two-handed axe.",
        "short_sword" => "A well-balanced blade, designed for quick thrusts.",
        "scimitar" => "A true warrior's blade, swift and deadly.",
        "arming_sword" => "The pinnacle of the swordmaker's art. A masterpiece.",
        // Armor
        "tunic" => "A simple tunic, comfortable but offering little protection.",
        "leather" => "Flexible lamellar armor, favored by hunters.",
        "gambeson" => "Heavy padded surcoat.",
        "chainmail" => "Fine mail shirt, remarkably well preserved.",
        "scale_armor" => "Heavy Kesharat infantry armor.",
        // Boots
        "fine_boots" => "Exquisite footwear, more at home in court than the dusty road.",
        "riding_boots" => "Kesharat officer's boots.",
        "heavy_work_boots" => "Simple but comfortable boots.",
        // Tools
        "cartographers_kit" => "A guild cartographer's kit in excellent condition.",
        "sleeping_kit" => "A bedroll and simple waxed canvas tent.",
        "cooking_supplies" => "A small cookpot and utensils.",
        "writing_kit" => "Pen, paper, ink, wax, and seal.",
        "yoriks_guide" => "Beautiful illustrated guide to local flora and fauna.",
        "canteen" => "A stout canteen with a wooden stopper.",
        "insect_netting" => "Fine netting to keep out flying pests.",
        "breathing_apparatus" => "Bulky device that attaches over mouth and nose.",
        "heavy_furs" => "Bulky but warm furs.",
        "peoples_borderlands" => "A remarkably complete guide to the cultures of the frontier.",
        _ => "",
    };

    // --- Food ---

    public static (string Name, string Description) FoodName(
        FoodType type, Terrain biome, bool foraged, Random? rng = null) =>
        FoodNames.Pick(type, biome, foraged, rng);

    // --- Helpers ---

    static string TierPrefix(int tier) => tier switch
    {
        1 => Pick("Gentle", "Quiet", "Sunlit", "Verdant"),
        2 => Pick("Grey", "Windswept", "Lonely", "Weathered"),
        _ => Pick("Dread", "Forsaken", "Blighted", "Accursed"),
    };

    static string TierMood(int tier) => tier switch
    {
        1 => "Peaceful",
        2 => "Desolate",
        _ => "Ominous",
    };

    static string Pick(params string[] options) =>
        options[_counter++ % options.Length];
}
