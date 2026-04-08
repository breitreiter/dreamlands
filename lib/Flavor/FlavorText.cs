using Dreamlands.Map;
using Dreamlands.Rules;

namespace Dreamlands.Flavor;

public static class FlavorText
{
    static int _counter;

    // --- Map regions (biome, tier) ---

    public static string RegionName(Terrain biome, int tier, Random rng, HashSet<string> used)
    {
        if (RegionNames.Draw(biome, tier, rng, used) is { } name)
            return name;

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

    public static string SettlementName(Terrain biome, int tier, Random rng, HashSet<string> used)
    {
        // Draw from curated pool; fall back to combinatorial generator if exhausted
        if (SettlementNames.Draw(biome, tier, rng, used) is { } name)
            return name;

        var root = biome switch
        {
            Terrain.Plains => Pick("Wheat", "Wind", "Sun", "Grass"),
            Terrain.Forest => Pick("Oak", "Elm", "Thorn", "Moss"),
            Terrain.Scrub => Pick("Dust", "Thorn", "Sand", "Flint"),
            Terrain.Mountains => Pick("Iron", "Frost", "Crag", "Granite"),
            Terrain.Swamp => Pick("Fog", "Reed", "Mire", "Murk"),
            _ => "Lake",
        };
        var suffix = Pick("ford", "hollow", "watch", "rest", "stead");
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
        "kukri" => "A heavy recurved blade, wickedly efficient.",
        "hunting_knife" => "A long knife of mountain steel, meant for more than game.",
        "hatchet" => "Ubiquitous tool of the forest folk.",
        "tomahawk" => "Light enough to throw, heavy enough to mean it.",
        "war_axe" => "A heavy axe built for war.",
        "broadaxe" => "A mighty two-handed axe.",
        "falchion" => "A crude cleaver of a sword. It will do.",
        "short_sword" => "A well-balanced blade, designed for quick thrusts.",
        "tulwar" => "A curved sword favored by the desert clans.",
        "scimitar" => "A true warrior's blade, swift and deadly.",
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
        "canteen" => "A stout canteen with a wooden stopper.",
        "waterskin" => "A large waterskin, well-stitched and treated against leaks.",
        "letters_of_introduction" => "Sealed letters vouching for the bearer's good character.",
        "peoples_borderlands" => "A remarkably complete guide to the cultures of the frontier.",
        "traders_ledger" => "A ledger of prices, weights, and measures across the borderlands.",
        "assayers_kit" => "Precision scales, acids, and touchstones for testing ore and coin.",
        "cartographers_kit" => "A guild cartographer's kit in excellent condition.",
        "sleeping_kit" => "A bedroll and sturdy waxed canvas tent.",
        "fever_ward" => "A poultice bag of rare swamp herbs that ward off fever.",
        "bilestone" => "A smooth dark stone said to draw poison from the gut.",
        "lead_lined_case" => "A heavy case lined with lead sheeting.",
        "antivenom_kit" => "Vials of antivenom and a scarification lancet.",
        _ => "",
    };

    // --- Food ---

    /// <summary>
    /// Biome-flavored display name for a single ration. Each biome returns a
    /// meal kit description; players see "you have 5 dried goat, dates, and flatbread"
    /// rather than "you have 5 rations".
    /// </summary>
    public static string RationName(string biome) => biome.ToLowerInvariant() switch
    {
        "plains"    => "smoked sausage, hard cheese, oat cakes",
        "mountains" => "dried goat, dates, flatbread",
        "forest"    => "mushroom jerky, hazelnuts, honey biscuit",
        "scrub"     => "dried lizard, cactus pulp, pemmican",
        "swamp"     => "smoked eel, river rice, swamp berries",
        _           => "trail rations",
    };

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
