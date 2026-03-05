using Dreamlands.Flavor;
using Dreamlands.Map;
using Dreamlands.Rules;

namespace MapGen;

public static class ContentPopulator
{
    public static void Populate(Map map, string contentPath, Random rng)
    {
        Console.Error.WriteLine("Populating content...");
        Console.Error.WriteLine("  Settlements...");
        SettlementPlacer.PlaceSettlements(map, rng);
        Console.Error.WriteLine("  Tiers...");
        TierAssigner.Assign(map);
        Console.Error.WriteLine("  Trade routes...");
        TradeRouteBuilder.Build(map);
        SizeSettlements(map);
        AssignSettlementIds(map);
        NameRegions(map);
        NameSettlements(map);
        AssignNodeDescriptions(map);
        Console.Error.WriteLine("  Dungeons...");
        var roster = DungeonRoster.Load(contentPath);
        DungeonPlacer.PlaceDungeons(map, roster, rng);
        Console.Error.WriteLine("  Encounters...");
        EncounterPlacer.Place(map);
    }

    private static void AssignSettlementIds(Map map)
    {
        foreach (var node in map.AllNodes())
        {
            if (node.Poi?.Kind == PoiKind.Settlement)
                node.Poi.SettlementId = $"s{node.X}_{node.Y}";
        }
    }

    private static void SizeSettlements(Map map)
    {
        // Count children per settlement in the trade tree
        var childCount = new Dictionary<Node, int>();
        foreach (var (from, to) in map.TradeEdges)
        {
            childCount.TryGetValue(to, out var count);
            childCount[to] = count + 1;
        }

        foreach (var node in map.AllNodes())
        {
            if (node.Poi?.Kind != PoiKind.Settlement || node.Poi.Size != null)
                continue;

            var children = childCount.GetValueOrDefault(node, 0);
            node.Poi.Size = children switch
            {
                >= 3 => SettlementSize.Town,
                >= 1 => SettlementSize.Village,
                _ => SettlementSize.Outpost,
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

            if (node == map.StartingCity)
            {
                node.Poi.Name = "Aldgate";
                continue;
            }

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
