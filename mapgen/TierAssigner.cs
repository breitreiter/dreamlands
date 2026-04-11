using Dreamlands.Map;
using Dreamlands.Rules;

namespace MapGen;

public static class TierAssigner
{
    // T3 regions beyond this fraction of their biome's total tiles get bisected.
    private const double MaxT3Fraction = 0.10;

    public static void Assign(Map map)
    {
        var oppositeEdge = GetOppositeEdge(map);

        var groups = map.Regions
            .Where(r => r.Terrain != Terrain.Lake)
            .GroupBy(r => r.Terrain);

        foreach (var group in groups)
        {
            var regions = group.ToList();

            if (regions.Count == 0)
                continue;

            // Pick T3: the region whose closest node to the opposite edge is smallest
            var t3 = regions
                .OrderBy(r => MinDistanceToEdge(r, oppositeEdge, map))
                .First();

            // Pick T1: the region closest to the city
            var t1 = regions
                .OrderBy(r => MinDistanceFromCity(r))
                .First();

            // If T1 and T3 collide (only 1 region), T1 wins
            if (t1 == t3 && regions.Count > 1)
                t3 = regions
                    .Where(r => r != t1)
                    .OrderBy(r => MinDistanceToEdge(r, oppositeEdge, map))
                    .First();

            foreach (var region in regions)
            {
                if (region == t3 && region != t1)
                    region.Tier = 3;
                else if (region == t1)
                    region.Tier = 1;
                else
                    region.Tier = 2;
            }

            // Bisect oversized T3
            if (t3.Tier == 3)
            {
                var biomeTiles = regions.Sum(r => r.Size);
                var maxT3Size = (int)(biomeTiles * MaxT3Fraction);

                if (t3.Size > maxT3Size && maxT3Size > 0)
                    BisectRegion(map, t3, maxT3Size, oppositeEdge);
            }
        }
    }

    private enum Edge { North, South, East, West }

    private static Edge GetOppositeEdge(Map map)
    {
        var city = map.StartingCity;
        if (city == null)
            return Edge.North;

        // Which edge is the city closest to?
        int dN = city.Y;
        int dS = map.Height - 1 - city.Y;
        int dW = city.X;
        int dE = map.Width - 1 - city.X;

        int min = Math.Min(Math.Min(dN, dS), Math.Min(dW, dE));

        // Return the opposite edge
        if (min == dN) return Edge.South;
        if (min == dS) return Edge.North;
        if (min == dW) return Edge.East;
        return Edge.West;
    }

    private static int DistanceToEdge(Node n, Edge edge, Map map) => edge switch
    {
        Edge.North => n.Y,
        Edge.South => map.Height - 1 - n.Y,
        Edge.West  => n.X,
        Edge.East  => map.Width - 1 - n.X,
        _ => int.MaxValue
    };

    private static int MinDistanceToEdge(Region region, Edge edge, Map map) =>
        region.Nodes.Count > 0
            ? region.Nodes.Min(n => DistanceToEdge(n, edge, map))
            : int.MaxValue;

    private static void BisectRegion(Map map, Region t3, int maxT3Size, Edge oppositeEdge)
    {
        // Sort nodes closest-to-opposite-edge first; those stay T3
        var sorted = t3.Nodes
            .OrderBy(n => DistanceToEdge(n, oppositeEdge, map))
            .ToList();

        var nextId = map.Regions.Max(r => r.Id) + 1;
        var spillover = new Region(nextId, t3.Terrain) { Tier = 2 };

        t3.Nodes.Clear();
        for (int i = 0; i < sorted.Count; i++)
        {
            if (i < maxT3Size)
            {
                t3.Nodes.Add(sorted[i]);
            }
            else
            {
                sorted[i].Region = spillover;
                spillover.Nodes.Add(sorted[i]);
            }
        }

        map.Regions.Add(spillover);

        Console.Error.WriteLine(
            $"    Bisected {t3.Terrain} T3: {t3.Size} tiles stay T3, " +
            $"{spillover.Size} tiles become T2");
    }

    private static int MinDistanceFromCity(Region region) =>
        region.Nodes.Count > 0
            ? region.Nodes.Min(n => n.DistanceFromCity)
            : int.MaxValue;
}
