using Dreamlands.Map;
using Dreamlands.Rules;

namespace MapGen;

public static class TierAssigner
{
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
        }
    }

    private static int MinDistanceFromCity(Region region) =>
        region.Nodes.Count > 0
            ? region.Nodes.Min(n => n.DistanceFromCity)
            : int.MaxValue;
}
