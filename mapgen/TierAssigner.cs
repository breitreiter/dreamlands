using Dreamlands.Map;
using Dreamlands.Rules;

namespace MapGen;

public static class TierAssigner
{
    // T3 regions beyond this fraction of their biome's total tiles get bisected.
    // Inner nodes become a new T2 region; outer nodes stay T3.
    private const double MaxT3Fraction = 0.10;

    public static void Assign(Map map)
    {
        var groups = map.Regions
            .Where(r => r.Terrain != Terrain.Lake)
            .GroupBy(r => r.Terrain);

        foreach (var group in groups)
        {
            var regions = group
                .Select(r => (region: r, dist: MinDistanceFromCity(r)))
                .Where(x => x.dist < int.MaxValue)
                .OrderBy(x => x.dist)
                .ToList();

            if (regions.Count == 0)
                continue;

            if (regions.Count == 1)
            {
                regions[0].region.Tier = 1;
            }
            else if (regions.Count == 2)
            {
                regions[0].region.Tier = 1;
                regions[1].region.Tier = 3;
            }
            else
            {
                regions[0].region.Tier = 1;
                regions[^1].region.Tier = 3;
                for (int i = 1; i < regions.Count - 1; i++)
                    regions[i].region.Tier = 2;
            }

            // Bisect oversized T3 regions
            if (regions.Count >= 2)
            {
                var t3 = regions[^1].region;
                var biomeTiles = regions.Sum(r => r.region.Size);
                var maxT3Size = (int)(biomeTiles * MaxT3Fraction);

                if (t3.Size > maxT3Size && maxT3Size > 0)
                    BisectRegion(map, t3, maxT3Size);
            }
        }
    }

    private static void BisectRegion(Map map, Region t3, int maxT3Size)
    {
        // Sort nodes farthest-first; the outer portion stays T3
        var sorted = t3.Nodes.OrderByDescending(n => n.DistanceFromCity).ToList();

        var nextId = map.Regions.Max(r => r.Id) + 1;
        var spillover = new Region(nextId, t3.Terrain) { Tier = 2 };

        // Farthest nodes stay in T3, the rest spill into new T2
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
