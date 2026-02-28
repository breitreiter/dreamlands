using Dreamlands.Map;
using Dreamlands.Rules;

namespace MapGen;

public static class MapGenerator
{
    public static (Map map, int seed) Generate(int width, int height, int? seed = null, Action<Map>? onExpansionCycle = null)
    {
        int actualSeed = seed ?? Random.Shared.Next();
        var rng = new Random(actualSeed);

        var map = new Map(width, height);

        Console.Error.WriteLine("  Terrain...");
        GenerateTerrain(map, rng, width, height, onExpansionCycle);

        Console.Error.WriteLine("  Regions...");
        ComputeRegions(map);

        return (map, actualSeed);
    }

    private static void GenerateTerrain(Map map, Random rng, int width, int height, Action<Map>? onExpansionCycle = null)
    {
        // Nodes default to Plains via constructor — no init loop needed

        var landTerrains = new (Terrain terrain, int weight)[]
        {
            (Terrain.Plains, 30),
            (Terrain.Forest, 20),
            (Terrain.Scrub, 35),
            (Terrain.Mountains, 8),
            (Terrain.Swamp, 5),
        };
        int totalWeight = landTerrains.Sum(t => t.weight);

        // Place seeds away from edges using Poisson-disc rejection sampling
        int margin = Math.Clamp(Math.Min(width, height) / 6, 3, 6);
        int totalSeeds = Math.Max(landTerrains.Length, (width * height) / 80);
        double minDist = Math.Sqrt((double)(width * height) / totalSeeds) * 0.7;
        var seedPositions = new List<(int x, int y)>();

        (int x, int y) PlaceSeed()
        {
            for (int attempt = 0; attempt < 30; attempt++)
            {
                int x = rng.Next(margin, width - margin);
                int y = rng.Next(margin, height - margin);
                bool tooClose = false;
                foreach (var (sx, sy) in seedPositions)
                {
                    double dx = x - sx;
                    double dy = y - sy;
                    if (dx * dx + dy * dy < minDist * minDist) { tooClose = true; break; }
                }
                if (!tooClose || attempt == 29)
                {
                    seedPositions.Add((x, y));
                    return (x, y);
                }
            }
            return default; // unreachable
        }

        var queue = new PriorityQueue<(int x, int y), float>();
        var claimed = new bool[width, height];
        var processed = new bool[width, height];
        const float jitter = 0.8f;
        const float stealChance = 0.04f;

        // Guarantee one seed of each terrain type
        foreach (var (terrain, _) in landTerrains)
        {
            var (x, y) = PlaceSeed();
            map[x, y].Terrain = terrain;
            queue.Enqueue((x, y), 0f);
            claimed[x, y] = true;
        }

        // Fill remaining seeds randomly by weight
        int extraSeeds = Math.Max(0, totalSeeds - landTerrains.Length);
        for (int i = 0; i < extraSeeds; i++)
        {
            var (x, y) = PlaceSeed();

            int roll = rng.Next(totalWeight);
            Terrain terrain = Terrain.Plains;
            foreach (var (t, w) in landTerrains)
            {
                roll -= w;
                if (roll < 0) { terrain = t; break; }
            }

            map[x, y].Terrain = terrain;
            queue.Enqueue((x, y), 0f);
            claimed[x, y] = true;
        }

        // Priority-queue expansion with jittered cost — produces irregular boundaries
        int[] ddx = { -1, 1, 0, 0 };
        int[] ddy = { 0, 0, -1, 1 };

        onExpansionCycle?.Invoke(map);
        int dequeueCount = 0;

        while (queue.Count > 0)
        {
            var (cx, cy) = queue.Dequeue();
            processed[cx, cy] = true;
            var currentTerrain = map[cx, cy].Terrain;

            for (int i = 0; i < 4; i++)
            {
                int nx = cx + ddx[i];
                int ny = cy + ddy[i];
                if (nx < 0 || nx >= width || ny < 0 || ny >= height) continue;

                float cost = 1.0f + (float)rng.NextDouble() * jitter;

                if (!claimed[nx, ny])
                {
                    claimed[nx, ny] = true;
                    map[nx, ny].Terrain = currentTerrain;
                    queue.Enqueue((nx, ny), cost);
                }
                else if (!processed[nx, ny] && map[nx, ny].Terrain != currentTerrain
                         && rng.NextDouble() < stealChance)
                {
                    // Frontier stealing: reassign unprocessed neighbor to break up straight walls
                    map[nx, ny].Terrain = currentTerrain;
                    queue.Enqueue((nx, ny), cost);
                }
            }

            if (++dequeueCount % width == 0)
                onExpansionCycle?.Invoke(map);
        }

        // Seed lakes in the interior, biased toward map center
        float centerX = width / 2f;
        float centerY = height / 2f;
        float maxDist = MathF.Sqrt(centerX * centerX + centerY * centerY);

        var interiorLand = map.AllNodes()
            .Where(n => n.X >= 2 && n.X < width - 2 && n.Y >= 2 && n.Y < height - 2)
            .ToList();

        int lakeCount = 1 + (width * height) / 2000;
        for (int i = 0; i < lakeCount && interiorLand.Count > 0; i++)
        {
            var weights = interiorLand.Select(n =>
            {
                float ddx = n.X - centerX;
                float ddy = n.Y - centerY;
                float dist = MathF.Sqrt(ddx * ddx + ddy * ddy);
                return 1f - (dist / maxDist) * 0.8f;
            }).ToList();

            float weightSum = weights.Sum();
            float roll = (float)rng.NextDouble() * weightSum;

            int idx = 0;
            for (int j = 0; j < weights.Count; j++)
            {
                roll -= weights[j];
                if (roll <= 0)
                {
                    idx = j;
                    break;
                }
            }

            interiorLand[idx].Terrain = Terrain.Lake;
            interiorLand.RemoveAt(idx);
        }
    }

    private static void ComputeRegions(Map map)
    {
        int regionId = 0;

        foreach (var start in map.AllNodes())
        {
            if (start.Region != null) continue;

            var region = new Region(regionId++, start.Terrain);
            var queue = new Queue<Node>();
            queue.Enqueue(start);
            start.Region = region;

            while (queue.Count > 0)
            {
                var node = queue.Dequeue();
                region.Nodes.Add(node);

                foreach (var dir in DirectionExtensions.Each())
                {
                    var neighbor = map.GetNeighbor(node, dir);
                    if (neighbor == null || neighbor.Region != null) continue;
                    if (neighbor.Terrain != region.Terrain) continue;

                    neighbor.Region = region;
                    queue.Enqueue(neighbor);
                }
            }

            map.Regions.Add(region);
        }
    }

    // TODO: use this to auto-name regions for game UI ("You have entered The Cursed Bog")
    private static List<List<Node>> FindRegions(Map map, Func<Node, bool> predicate)
    {
        var visited = new HashSet<Node>();
        var regions = new List<List<Node>>();

        foreach (var start in map.AllNodes().Where(predicate))
        {
            if (visited.Contains(start)) continue;

            var region = new List<Node>();
            var queue = new Queue<Node>();
            queue.Enqueue(start);
            visited.Add(start);

            while (queue.Count > 0)
            {
                var node = queue.Dequeue();
                region.Add(node);

                foreach (var dir in DirectionExtensions.Each())
                {
                    var neighbor = map.GetNeighbor(node, dir);
                    if (neighbor == null || visited.Contains(neighbor)) continue;
                    if (!predicate(neighbor)) continue;

                    visited.Add(neighbor);
                    queue.Enqueue(neighbor);
                }
            }

            regions.Add(region);
        }

        return regions;
    }
}
