using Dreamlands.Map;

namespace MapGen;

public static class EncounterPlacer
{
    private const int MinSpacing = 3;

    public static void Place(Map map)
    {
        var traversable = map.AllNodes()
            .Where(n => !n.IsWater && n.DistanceFromCity < int.MaxValue && n.Y > 0)
            .ToDictionary(n => (n.X, n.Y));

        if (traversable.Count == 0)
            return;

        var regions = map.Regions.Where(r => r.Tier > 0).ToList();
        int placed = 0;

        foreach (var region in regions)
        {
            var nodes = region.Nodes
                .Where(n => n.Poi == null && traversable.ContainsKey((n.X, n.Y)))
                .ToList();

            if (nodes.Count == 0)
                continue;

            var slots = new List<Node>();
            var distance = new Dictionary<Node, int>();

            // Initialize: all nodes start at max distance from any slot
            foreach (var n in nodes)
                distance[n] = int.MaxValue;

            while (true)
            {
                // Find the node farthest from any existing encounter slot
                Node? farthest = null;
                int maxDist = -1;

                foreach (var n in nodes)
                {
                    if (n.Poi != null)
                        continue;

                    int d = distance[n];
                    if (d > maxDist)
                    {
                        maxDist = d;
                        farthest = n;
                    }
                }

                if (farthest == null || maxDist <= MinSpacing)
                    break;

                farthest.Poi = new Poi(PoiKind.Encounter, "Encounter");
                slots.Add(farthest);
                placed++;

                // Update distances from the new slot
                foreach (var n in nodes)
                {
                    int d = Math.Abs(n.X - farthest.X) + Math.Abs(n.Y - farthest.Y);
                    if (d < distance[n])
                        distance[n] = d;
                }
            }
        }

        Console.Error.WriteLine($"    Placed {placed} encounter slots");
    }
}
