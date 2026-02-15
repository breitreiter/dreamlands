using Dreamlands.Map;

namespace MapGen;

public static class ContentPopulator
{
    public static void Populate(Map map, ContentLoader content, Random rng)
    {
        Console.Error.WriteLine("Populating content...");
        AssignRegionNames(map, content, rng);
        AssignNodeDescriptions(map, content, rng);
        Console.Error.WriteLine("  Settlements...");
        SettlementPlacer.PlaceSettlements(map, content, rng);
        Console.Error.WriteLine("  Tiers...");
        TierAssigner.Assign(map);
        Console.Error.WriteLine("  Dungeons...");
        var roster = DungeonRoster.Load(content.ContentPath);
        DungeonPlacer.PlaceDungeons(map, roster, rng);
        Console.Error.WriteLine("  Encounters...");
        EncounterPlacer.Place(map);
    }

    private static void AssignRegionNames(Map map, ContentLoader content, Random rng)
    {
        var usedNames = new Dictionary<Terrain, HashSet<string>>();

        foreach (var region in map.Regions)
        {
            if (!content.HasNames(region.Terrain))
                continue;

            if (!usedNames.ContainsKey(region.Terrain))
                usedNames[region.Terrain] = new HashSet<string>();

            string? name = null;
            for (int i = 0; i < 10; i++)
            {
                var candidate = content.GetRandomName(region.Terrain, rng);
                if (candidate != null && !usedNames[region.Terrain].Contains(candidate))
                {
                    name = candidate;
                    break;
                }
            }

            name ??= content.GetRandomName(region.Terrain, rng);

            if (name != null)
            {
                region.Name = name;
                usedNames[region.Terrain].Add(name);
            }
        }
    }

    private static void AssignNodeDescriptions(Map map, ContentLoader content, Random rng)
    {
        foreach (var node in map.AllNodes())
        {
            if (!content.HasDescriptions(node.Terrain))
                continue;

            node.Description = content.GetRandomDescription(node.Terrain, rng);
        }
    }

}
