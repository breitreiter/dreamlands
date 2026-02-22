using Dreamlands.Map;
using Dreamlands.Rules;

namespace MapGen;

public static class SettlementPlacer
{
    // Settlement spacing distances
    private const int Tier1Max = 15;
    private const int Tier2Max = 25;
    private const int Tier3Max = 40;

    public static void PlaceSettlements(Map map, Random rng)
    {
        var traversable = map.AllNodes()
            .Where(n => !n.IsWater && n.Y > 0)
            .ToDictionary(n => (n.X, n.Y));

        if (traversable.Count == 0)
            return;

        var startingCity = PlaceStartingCity(map, traversable);
        if (startingCity == null)
            return;

        map.StartingCity = startingCity;
        ComputeDistanceField(map, startingCity, traversable);

        var covered = new HashSet<Node>();
        int survivalRadius = GetSurvivalRadius(startingCity.DistanceFromCity);
        MarkCovered(map, startingCity, traversable, covered, survivalRadius);

        while (true)
        {
            var uncovered = traversable.Values.Where(n => !covered.Contains(n)).ToList();
            if (uncovered.Count == 0)
                break;

            var target = FindFurthestUncovered(map, uncovered, covered, traversable);
            if (target == null)
                break;

            survivalRadius = GetSurvivalRadius(target.DistanceFromCity);
            var site = FindBestSiteNear(map, target, traversable, covered, survivalRadius);
            if (site == null)
                break;

            PlaceSettlement(site);
            MarkCovered(map, site, traversable, covered, survivalRadius);
        }
    }

    private static bool IsNearEdge(Node node, Map map, int minDist, int maxDist)
    {
        int edgeDist = Math.Min(Math.Min(node.X, map.Width - 1 - node.X),
                                Math.Min(node.Y, map.Height - 1 - node.Y));
        return edgeDist >= minDist && edgeDist <= maxDist;
    }

    private static Node? PlaceStartingCity(Map map, Dictionary<(int, int), Node> traversable)
    {
        var nearEdge = traversable.Values.Where(n => IsNearEdge(n, map, 1, 2)).ToList();
        if (nearEdge.Count == 0)
            nearEdge = traversable.Values.ToList();

        var preferred = nearEdge.Where(n => n.Terrain == Terrain.Plains).ToList();
        var candidates = preferred.Count > 0 ? preferred : nearEdge;

        var city = candidates.OrderByDescending(n => TraversableNeighborCount(map, n)).First();
        city.Poi = new Poi(PoiKind.Settlement, "City") { Size = SettlementSize.City };
        return city;
    }

    private static void ComputeDistanceField(Map map, Node start, Dictionary<(int, int), Node> traversable)
    {
        start.DistanceFromCity = 0;
        var queue = new Queue<Node>();
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            var node = queue.Dequeue();
            var nextDist = node.DistanceFromCity + 1;

            foreach (var (dir, neighbor) in map.LandNeighbors(node))
            {
                if (traversable.ContainsKey((neighbor.X, neighbor.Y)) && neighbor.DistanceFromCity > nextDist)
                {
                    neighbor.DistanceFromCity = nextDist;
                    queue.Enqueue(neighbor);
                }
            }
        }
    }

    // Settlement spacing by tier (how far apart settlements should be)
    private static int GetSurvivalRadius(int distance) => distance switch
    {
        <= Tier1Max => 5,   // Tier 1: dense (inns every 4-6, towns every 8-12)
        <= Tier2Max => 8,   // Tier 2: moderate (settlements every 6-8)
        <= Tier3Max => 12,  // Tier 3: sparse (settlements every 15-20)
        _ => 18             // Tier 4: very sparse
    };

    private static int TraversableNeighborCount(Map map, Node node) =>
        map.LandNeighbors(node).Count();

    private static void PlaceSettlement(Node node)
    {
        node.Poi = new Poi(PoiKind.Settlement, "Settlement");
    }

    private static void MarkCovered(Map map, Node start, Dictionary<(int, int), Node> traversable, HashSet<Node> covered, int radius)
    {
        var visited = new Dictionary<Node, int> { [start] = 0 };
        var queue = new Queue<Node>();
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            var node = queue.Dequeue();
            var dist = visited[node];
            covered.Add(node);

            if (dist >= radius)
                continue;

            foreach (var (dir, neighbor) in map.LandNeighbors(node))
            {
                if (traversable.ContainsKey((neighbor.X, neighbor.Y)) && !visited.ContainsKey(neighbor))
                {
                    visited[neighbor] = dist + 1;
                    queue.Enqueue(neighbor);
                }
            }
        }
    }

    public static Node? GetTraversableNeighbor(Node node, Direction dir, Dictionary<(int, int), Node> traversable)
    {
        var (dx, dy) = dir.ToOffset();
        return traversable.GetValueOrDefault((node.X + dx, node.Y + dy));
    }

    private static Node? FindFurthestUncovered(Map map, List<Node> uncovered, HashSet<Node> covered, Dictionary<(int, int), Node> traversable)
    {
        Node? furthest = null;
        int maxDist = -1;

        foreach (var node in uncovered)
        {
            int dist = MinDistanceToCovered(map, node, covered, traversable);
            if (dist > maxDist)
            {
                maxDist = dist;
                furthest = node;
            }
        }

        return furthest;
    }

    private static int MinDistanceToCovered(Map map, Node start, HashSet<Node> covered, Dictionary<(int, int), Node> traversable)
    {
        var visited = new Dictionary<Node, int> { [start] = 0 };
        var queue = new Queue<Node>();
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            var node = queue.Dequeue();
            var dist = visited[node];

            if (covered.Contains(node))
                return dist;

            foreach (var (dir, neighbor) in map.LandNeighbors(node))
            {
                if (traversable.ContainsKey((neighbor.X, neighbor.Y)) && !visited.ContainsKey(neighbor))
                {
                    visited[neighbor] = dist + 1;
                    queue.Enqueue(neighbor);
                }
            }
        }

        return int.MaxValue;
    }

    private static Node? FindBestSiteNear(Map map, Node target, Dictionary<(int, int), Node> traversable, HashSet<Node> covered, int radius)
    {
        var visited = new Dictionary<Node, int> { [target] = 0 };
        var queue = new Queue<Node>();
        queue.Enqueue(target);

        Node? bestSite = null;
        int bestScore = -1;

        while (queue.Count > 0)
        {
            var node = queue.Dequeue();
            var dist = visited[node];

            if (node.Poi == null)
            {
                int connectivity = TraversableNeighborCount(map, node);
                int score = connectivity * 10 - (covered.Contains(node) ? 5 : 0);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestSite = node;
                }
            }

            if (dist >= radius / 2)
                continue;

            foreach (var (dir, neighbor) in map.LandNeighbors(node))
            {
                if (traversable.ContainsKey((neighbor.X, neighbor.Y)) && !visited.ContainsKey(neighbor))
                {
                    visited[neighbor] = dist + 1;
                    queue.Enqueue(neighbor);
                }
            }
        }

        return bestSite ?? target;
    }
}
