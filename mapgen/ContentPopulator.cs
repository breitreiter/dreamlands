using Dreamlands.Flavor;
using Dreamlands.Map;

namespace MapGen;

public static class ContentPopulator
{
    public static void Populate(Map map, ContentLoader content, Random rng)
    {
        Console.Error.WriteLine("Populating content...");
        Console.Error.WriteLine("  Settlements...");
        SettlementPlacer.PlaceSettlements(map, content, rng);
        Console.Error.WriteLine("  Tiers...");
        TierAssigner.Assign(map);
        NameRegions(map);
        NameSettlements(map);
        AssignNodeDescriptions(map);
        Console.Error.WriteLine("  Dungeons...");
        var roster = DungeonRoster.Load(content.ContentPath);
        DungeonPlacer.PlaceDungeons(map, roster, rng);
        Console.Error.WriteLine("  Encounters...");
        EncounterPlacer.Place(map);
    }

    private static void NameRegions(Map map)
    {
        foreach (var region in map.Regions)
            region.Name = FlavorText.RegionName(region.Terrain, region.Tier);
    }

    private static void NameSettlements(Map map)
    {
        foreach (var node in map.AllNodes())
        {
            if (node.Poi?.Kind != PoiKind.Settlement)
                continue;

            var tier = node.Region?.Tier ?? 1;
            node.Poi.Name = FlavorText.SettlementName(node.Terrain, tier);
        }
    }

    private static void AssignNodeDescriptions(Map map)
    {
        foreach (var node in map.AllNodes())
        {
            var tier = node.Region?.Tier ?? 1;
            node.Description = node.Poi?.Kind == PoiKind.Settlement
                ? FlavorText.SettlementDescription(node.Terrain, tier)
                : FlavorText.TileDescription(node.Terrain, tier);
        }
    }

}
