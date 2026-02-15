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
        SizeSettlements(map);
        NameRegions(map);
        NameSettlements(map);
        AssignNodeDescriptions(map);
        Console.Error.WriteLine("  Dungeons...");
        var roster = DungeonRoster.Load(content.ContentPath);
        DungeonPlacer.PlaceDungeons(map, roster, rng);
        Console.Error.WriteLine("  Encounters...");
        EncounterPlacer.Place(map);
    }

    private static void SizeSettlements(Map map)
    {
        foreach (var node in map.AllNodes())
        {
            if (node.Poi?.Kind != PoiKind.Settlement || node.Poi.Size != null)
                continue;

            var tier = node.Region?.Tier ?? 1;
            int connections = 0;
            foreach (var dir in DirectionExtensions.Each())
                if (node.HasConnection(dir)) connections++;
            bool high = connections >= 4;

            node.Poi.Size = tier switch
            {
                1 => high ? SettlementSize.Town : SettlementSize.Village,
                2 => high ? SettlementSize.Village : SettlementSize.Outpost,
                _ => high ? SettlementSize.Outpost : SettlementSize.Camp,
            };
        }
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
            var size = node.Poi.Size ?? SettlementSize.Village;
            node.Poi.Name = FlavorText.SettlementName(node.Terrain, tier, size);
        }
    }

    private static void AssignNodeDescriptions(Map map)
    {
        foreach (var node in map.AllNodes())
        {
            var tier = node.Region?.Tier ?? 1;
            node.Description = node.Poi?.Kind == PoiKind.Settlement
                ? FlavorText.SettlementDescription(node.Terrain, tier, node.Poi.Size ?? SettlementSize.Village)
                : FlavorText.TileDescription(node.Terrain, tier);
        }
    }

}
