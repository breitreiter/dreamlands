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
        Console.Error.WriteLine("  Water sources...");
        PlaceWaterSources(map, content, rng);
        Console.Error.WriteLine("  Dungeons...");
        var roster = DungeonRoster.Load(content.ContentPath);
        DungeonPlacer.PlaceDungeons(map, roster, rng);
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

    private static void PlaceWaterSources(Map map, ContentLoader content, Random rng)
    {
        if (!content.HasPoiTypes(PoiKind.WaterSource))
            return;

        var traversable = map.AllNodes()
            .Where(n => !n.IsWater && n.Connections != Direction.None && n.DistanceFromCity < int.MaxValue)
            .ToDictionary(n => (n.X, n.Y));

        if (traversable.Count == 0)
            return;

        // Settlements provide water, as do nodes with natural water access (lake/river)
        var waterProviders = traversable.Values
            .Where(n => n.Poi?.Kind == PoiKind.Settlement || n.IsLakeAdjacent || n.HasRiver)
            .ToHashSet();

        // Find eligible water source locations (for placing POIs where needed)
        var waterEligible = traversable.Values
            .Where(n => n.Poi == null && IsWaterEligible(map, n))
            .ToHashSet();

        // Compute initial water coverage using multi-source BFS
        var waterDistance = ComputeWaterDistances(waterProviders, traversable);

        // Keep placing water sources until all nodes have coverage
        while (true)
        {
            // Find the node that most needs water (furthest from any water source relative to its requirement)
            Node? worstNode = null;
            int worstDeficit = 0;

            foreach (var node in traversable.Values)
            {
                int required = SettlementPlacer.GetWaterRadius(node.DistanceFromCity);
                int actual = waterDistance.GetValueOrDefault(node, int.MaxValue);
                int deficit = actual - required;

                if (deficit > worstDeficit)
                {
                    worstDeficit = deficit;
                    worstNode = node;
                }
            }

            if (worstNode == null)
                break; // All nodes covered

            // Find nearest eligible water site to this node
            var site = FindNearestEligible(worstNode, waterEligible, traversable);
            if (site == null)
                break; // No eligible sites can help

            // Place water source
            var type = content.GetPoiTypeAtDistance(PoiKind.WaterSource, site.DistanceFromCity, rng) ?? "Spring";
            site.Poi = new Poi(PoiKind.WaterSource, type);
            waterProviders.Add(site);
            waterEligible.Remove(site);

            // Update distances from this new water source
            UpdateWaterDistances(site, waterDistance, traversable);
        }
    }

    private static bool IsWaterEligible(Map map, Node node)
    {
        // Adjacent to lake
        foreach (var dir in DirectionExtensions.Each())
        {
            var neighbor = map.GetNeighbor(node, dir);
            if (neighbor?.Terrain == Terrain.Lake)
                return true;
        }

        // Has river
        if (node.HasRiver)
            return true;

        // Mountains/hills can have springs (~20% of nodes)
        if (node.Terrain == Terrain.Mountains || node.Terrain == Terrain.Hills)
        {
            int hash = node.X * 31 + node.Y * 17;
            return hash % 5 == 0;
        }

        return false;
    }

    private static Dictionary<Node, int> ComputeWaterDistances(HashSet<Node> sources, Dictionary<(int, int), Node> traversable)
    {
        var distances = new Dictionary<Node, int>();
        var queue = new Queue<Node>();

        foreach (var source in sources)
        {
            distances[source] = 0;
            queue.Enqueue(source);
        }

        while (queue.Count > 0)
        {
            var node = queue.Dequeue();
            var dist = distances[node];

            foreach (var dir in DirectionExtensions.Each())
            {
                if (!node.HasConnection(dir))
                    continue;

                var neighbor = SettlementPlacer.GetConnectedNeighbor(node, dir, traversable);
                if (neighbor != null && !distances.ContainsKey(neighbor))
                {
                    distances[neighbor] = dist + 1;
                    queue.Enqueue(neighbor);
                }
            }
        }

        return distances;
    }

    private static void UpdateWaterDistances(Node newSource, Dictionary<Node, int> distances, Dictionary<(int, int), Node> traversable)
    {
        var queue = new Queue<Node>();
        distances[newSource] = 0;
        queue.Enqueue(newSource);

        while (queue.Count > 0)
        {
            var node = queue.Dequeue();
            var dist = distances[node];

            foreach (var dir in DirectionExtensions.Each())
            {
                if (!node.HasConnection(dir))
                    continue;

                var neighbor = SettlementPlacer.GetConnectedNeighbor(node, dir, traversable);
                if (neighbor == null)
                    continue;

                var newDist = dist + 1;
                if (!distances.ContainsKey(neighbor) || newDist < distances[neighbor])
                {
                    distances[neighbor] = newDist;
                    queue.Enqueue(neighbor);
                }
            }
        }
    }

    private static Node? FindNearestEligible(Node start, HashSet<Node> eligible, Dictionary<(int, int), Node> traversable)
    {
        if (eligible.Contains(start))
            return start;

        var visited = new HashSet<Node> { start };
        var queue = new Queue<Node>();
        queue.Enqueue(start);

        // Limit search to reasonable radius
        var distances = new Dictionary<Node, int> { [start] = 0 };
        int maxSearch = 15;

        while (queue.Count > 0)
        {
            var node = queue.Dequeue();
            var dist = distances[node];

            if (dist >= maxSearch)
                continue;

            foreach (var dir in DirectionExtensions.Each())
            {
                if (!node.HasConnection(dir))
                    continue;

                var neighbor = SettlementPlacer.GetConnectedNeighbor(node, dir, traversable);
                if (neighbor == null || visited.Contains(neighbor))
                    continue;

                if (eligible.Contains(neighbor))
                    return neighbor;

                visited.Add(neighbor);
                distances[neighbor] = dist + 1;
                queue.Enqueue(neighbor);
            }
        }

        return null;
    }
}
