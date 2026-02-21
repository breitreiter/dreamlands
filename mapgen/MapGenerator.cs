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

        Console.Error.WriteLine("  Lake neighbors...");
        ComputeLakeNeighbors(map);

        Console.Error.WriteLine("  Regions...");
        ComputeRegions(map);

        Console.Error.WriteLine("  Connections...");
        GenerateConnections(map, rng);

        Console.Error.WriteLine("  Bridging...");
        BridgeDisconnectedComponents(map);

        Console.Error.WriteLine("  Rivers...");
        GenerateRivers(map, rng);

        Console.Error.WriteLine("  Bridging...");
        BridgeDisconnectedComponents(map);

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

    private static void ComputeLakeNeighbors(Map map)
    {
        foreach (var node in map.AllNodes())
        {
            int count = 0;
            foreach (var dir in DirectionExtensions.Each())
            {
                var neighbor = map.GetNeighbor(node, dir);
                if (neighbor?.Terrain == Terrain.Lake)
                    count++;
            }
            node.LakeNeighbors = count;
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

    private static void GenerateConnections(Map map, Random rng)
    {
        for (int y = 0; y < map.Height; y++)
        {
            for (int x = 0; x < map.Width; x++)
            {
                var node = map[x, y];

                if (node.IsWater)
                    continue;

                if (x < map.Width - 1)
                {
                    var east = map[x + 1, y];
                    if (!east.IsWater)
                    {
                        float prob = GetEdgeProbability(node.Terrain, east.Terrain);
                        if (rng.NextDouble() < prob)
                            map.Connect(node, east);
                    }
                }

                if (y < map.Height - 1)
                {
                    var south = map[x, y + 1];
                    if (!south.IsWater)
                    {
                        float prob = GetEdgeProbability(node.Terrain, south.Terrain);
                        if (rng.NextDouble() < prob)
                            map.Connect(node, south);
                    }
                }
            }
        }
    }

    private static float GetEdgeProbability(Terrain a, Terrain b)
    {
        return MathF.Min(GetTerrainConnectivity(a), GetTerrainConnectivity(b));
    }

    private static float GetTerrainConnectivity(Terrain terrain) => terrain switch
    {
        Terrain.Plains => 0.85f,
        Terrain.Forest => 0.60f,
        Terrain.Scrub => 0.60f,
        Terrain.Swamp => 0.35f,
        Terrain.Mountains => 0.25f,
        _ => 0.5f
    };

    private static void BridgeDisconnectedComponents(Map map)
    {
        while (true)
        {
            var landNodes = map.AllNodes().Where(n => !n.IsWater).ToList();
            if (landNodes.Count == 0) return;

            var componentId = new Dictionary<Node, int>();
            var components = new List<List<Node>>();

            foreach (var start in landNodes)
            {
                if (componentId.ContainsKey(start)) continue;

                var component = new List<Node>();
                var queue = new Queue<Node>();
                queue.Enqueue(start);
                componentId[start] = components.Count;

                while (queue.Count > 0)
                {
                    var node = queue.Dequeue();
                    component.Add(node);

                    foreach (var dir in DirectionExtensions.Each())
                    {
                        if (!node.HasConnection(dir)) continue;

                        var neighbor = map.GetNeighbor(node, dir);
                        if (neighbor == null || componentId.ContainsKey(neighbor)) continue;

                        componentId[neighbor] = components.Count;
                        queue.Enqueue(neighbor);
                    }
                }

                components.Add(component);
            }

            var multiNodeComponents = components.Where(c => c.Count > 1).ToList();
            if (multiNodeComponents.Count <= 1) return;

            var largest = multiNodeComponents.OrderByDescending(c => c.Count).First();
            var largestSet = new HashSet<Node>(largest);

            bool bridged = false;
            foreach (var component in multiNodeComponents)
            {
                if (component == largest) continue;

                foreach (var node in component)
                {
                    foreach (var dir in DirectionExtensions.Each())
                    {
                        var neighbor = map.GetNeighbor(node, dir);
                        if (neighbor != null && largestSet.Contains(neighbor))
                        {
                            map.Connect(node, neighbor);
                            bridged = true;
                            break;
                        }
                    }
                    if (bridged) break;
                }
                if (bridged) break;
            }

            if (!bridged)
            {
                var smallest = multiNodeComponents.Where(c => c != largest)
                    .OrderBy(c => c.Count).First();
                foreach (var node in smallest)
                    node.Connections = Direction.None;
            }
        }
    }

    private static bool IsMapEdge(Node node, Map map) =>
        node.X == 0 || node.X == map.Width - 1 || node.Y == 0 || node.Y == map.Height - 1;

    private static void GenerateRivers(Map map, Random rng)
    {
        // Build distance field: BFS from map-edge tiles
        var distance = new Dictionary<Node, int>();
        var queue = new Queue<Node>();

        foreach (var node in map.AllNodes())
        {
            if (IsMapEdge(node, map))
            {
                distance[node] = 0;
                queue.Enqueue(node);
            }
        }

        while (queue.Count > 0)
        {
            var node = queue.Dequeue();
            int dist = distance[node];

            foreach (var dir in DirectionExtensions.Each())
            {
                var neighbor = map.GetNeighbor(node, dir);
                if (neighbor == null || distance.ContainsKey(neighbor)) continue;
                if (neighbor.Terrain is Terrain.Lake or Terrain.Mountains) continue;

                distance[neighbor] = dist + 1;
                queue.Enqueue(neighbor);
            }
        }

        // Flow rivers from each lake
        var lakes = map.AllNodes().Where(n => n.Terrain == Terrain.Lake).ToList();

        foreach (var lake in lakes)
        {
            Node? bestStart = null;
            Direction bestDir = Direction.None;
            int bestDist = int.MaxValue;

            foreach (var dir in DirectionExtensions.Each())
            {
                var neighbor = map.GetNeighbor(lake, dir);
                if (neighbor == null || !distance.ContainsKey(neighbor)) continue;
                if (neighbor.IsWater) continue;

                if (distance[neighbor] < bestDist)
                {
                    bestDist = distance[neighbor];
                    bestStart = neighbor;
                    bestDir = dir;
                }
            }

            if (bestStart == null) continue;

            lake.AddRiver(bestDir);
            bestStart.AddRiver(bestDir.Opposite());

            FlowRiver(map, rng, bestStart, distance);
        }

        // Mark crossings
        foreach (var node in map.AllNodes())
        {
            if (!node.HasRiver) continue;

            foreach (var dir in DirectionExtensions.Each())
            {
                if (!node.HasRiverOn(dir)) continue;

                var neighbor = map.GetNeighbor(node, dir);
                if (rng.NextDouble() < 0.4)
                {
                    node.AddCrossing(dir);
                    neighbor?.AddCrossing(dir.Opposite());
                }
            }
        }

        // Remove connections blocked by non-crossable rivers
        foreach (var node in map.AllNodes())
        {
            foreach (var dir in DirectionExtensions.Each())
            {
                if (node.HasRiverOn(dir) && !node.IsCrossableOn(dir))
                    node.RemoveConnection(dir);
            }
        }
    }

    private static void FlowRiver(Map map, Random rng, Node current, Dictionary<Node, int> distance)
    {
        var visited = new HashSet<Node> { current };

        while (!IsMapEdge(current, map))
        {
            int currentDist = distance.GetValueOrDefault(current, int.MaxValue);

            var candidates = new List<(Node neighbor, Direction dir, int dist)>();

            foreach (var dir in DirectionExtensions.Each())
            {
                var neighbor = map.GetNeighbor(current, dir);
                if (neighbor == null || visited.Contains(neighbor)) continue;
                if (neighbor.Terrain is Terrain.Lake or Terrain.Mountains) continue;

                int neighborDist = distance.GetValueOrDefault(neighbor, int.MaxValue);
                if (neighborDist <= currentDist)
                    candidates.Add((neighbor, dir, neighborDist));
            }

            if (candidates.Count == 0) break;

            var choice = candidates
                .Select(c => (c.neighbor, c.dir, score: c.dist + rng.Next(5)))
                .OrderBy(c => c.score)
                .First();

            var (nextNode, nextDir, _) = choice;

            current.AddRiver(nextDir);
            nextNode.AddRiver(nextDir.Opposite());

            visited.Add(nextNode);
            current = nextNode;
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
