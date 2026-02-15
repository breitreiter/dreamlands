using Dreamlands.Map;

namespace MapGen;

public static class DungeonPlacer
{
    private const int MinSeparation = 8;

    private static readonly Dictionary<string, Terrain> BiomeToTerrain = new()
    {
        ["forest"] = Terrain.Forest,
        ["mountains"] = Terrain.Mountains,
        ["swamp"] = Terrain.Swamp,
        ["hills"] = Terrain.Hills,
        ["plains"] = Terrain.Plains,
    };

    public static void PlaceDungeons(Map map, List<DungeonEntry> roster, Random rng)
    {
        var traversable = map.AllNodes()
            .Where(n => !n.IsWater && n.Connections != Direction.None && n.DistanceFromCity < int.MaxValue)
            .ToDictionary(n => (n.X, n.Y));

        if (traversable.Count == 0)
            return;

        var placed = new List<Node>();

        // Shuffle roster so tie-breaking isn't biased by file order
        var shuffled = roster.OrderBy(_ => rng.Next()).ToList();

        foreach (var entry in shuffled)
        {
            if (!BiomeToTerrain.TryGetValue(entry.Biome, out var terrain))
                continue;

            var candidates = traversable.Values
                .Where(n => n.Terrain == terrain
                    && n.Poi == null
                    && (n.Region?.Tier ?? 0) >= entry.TierMin
                    && (n.Region?.Tier ?? 0) <= entry.TierMax)
                .ToList();

            if (candidates.Count == 0)
                continue;

            // Score candidates
            var scored = new List<(Node node, int score)>();
            foreach (var node in candidates)
            {
                int connections = CountConnections(node);
                // Strongly prefer dead ends, then low connectivity
                int score = connections switch
                {
                    1 => 100,
                    2 => 60,
                    3 => 30,
                    _ => 10
                };

                // Penalize proximity to already-placed dungeons
                int minDist = MinBfsDistance(node, placed, traversable, MinSeparation);
                if (minDist < MinSeparation)
                    score -= (MinSeparation - minDist) * 15;

                // Randomize tiebreaking
                score += rng.Next(10);
                scored.Add((node, score));
            }

            var best = scored.OrderByDescending(s => s.score).First().node;
            best.Poi = new Poi(PoiKind.Dungeon, entry.Name)
            {
                DungeonId = entry.Id,
                DecalFile = entry.Decal
            };
            placed.Add(best);
        }
    }

    private static int CountConnections(Node node)
    {
        int count = 0;
        foreach (var dir in DirectionExtensions.Each())
            if (node.HasConnection(dir)) count++;
        return count;
    }

    private static int MinBfsDistance(Node start, List<Node> targets, Dictionary<(int, int), Node> traversable, int maxDist)
    {
        if (targets.Count == 0)
            return int.MaxValue;

        var targetSet = new HashSet<Node>(targets);
        var visited = new Dictionary<Node, int> { [start] = 0 };
        var queue = new Queue<Node>();
        queue.Enqueue(start);
        int closest = int.MaxValue;

        while (queue.Count > 0)
        {
            var node = queue.Dequeue();
            int dist = visited[node];

            if (targetSet.Contains(node) && dist > 0)
                closest = Math.Min(closest, dist);

            if (dist >= maxDist || dist >= closest)
                continue;

            foreach (var dir in DirectionExtensions.Each())
            {
                if (!node.HasConnection(dir))
                    continue;

                var neighbor = SettlementPlacer.GetConnectedNeighbor(node, dir, traversable);
                if (neighbor != null && !visited.ContainsKey(neighbor))
                {
                    visited[neighbor] = dist + 1;
                    queue.Enqueue(neighbor);
                }
            }
        }

        return closest;
    }
}
