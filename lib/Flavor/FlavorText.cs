using Dreamlands.Map;

namespace Dreamlands.Flavor;

public static class FlavorText
{
    // --- Map regions (biome, tier) ---

    public static string RegionName(Terrain biome, int tier) =>
        $"The {biome} Region";

    public static string TileDescription(Terrain biome, int tier) =>
        $"A stretch of {biome.ToString().ToLower()}.";

    // --- Settlements (biome, tier, size) ---

    public static string SettlementName(Terrain biome, int tier, SettlementSize size) =>
        $"{biome} Settlement";

    public static string SettlementDescription(Terrain biome, int tier, SettlementSize size) =>
        $"A settlement in the {biome.ToString().ToLower()}.";

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

    public static string HealerDescription(Terrain biome, int tier, SettlementSize size) =>
        "A healer's hut, smelling of herbs.";

    // --- Other elements (biome, tier) ---

    public static string TimeOfDayDescription(Terrain biome, int tier) =>
        "The sky stretches overhead.";

    public static string WeatherDescription(Terrain biome, int tier, string weatherState) =>
        "The weather is unremarkable.";

    // --- Loose elements ---

    public static string ConditionWarning(string condition) =>
        $"You are affected by {condition}.";
}
