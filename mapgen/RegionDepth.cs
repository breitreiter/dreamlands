using Dreamlands.Map;

namespace MapGen;

public static class RegionDepth
{
    /// <summary>
    /// Minimum Manhattan distance from this node to any node of a different terrain.
    /// Higher = deeper inside the region. Scans orthogonal neighbors outward.
    /// </summary>
    public static int Compute(Node node, Map map)
    {
        var terrain = node.Terrain;

        for (int r = 1; r <= Math.Max(map.Width, map.Height); r++)
        {
            // Scan the perimeter of the Manhattan diamond at radius r
            for (int dx = -r; dx <= r; dx++)
            {
                int dy = r - Math.Abs(dx);

                // Check both +dy and -dy
                foreach (int sy in dy == 0 ? [0] : new[] { dy, -dy })
                {
                    int nx = node.X + dx;
                    int ny = node.Y + sy;

                    if (!map.InBounds(nx, ny))
                        return r; // Map edge counts as region boundary

                    if (map[nx, ny].Terrain != terrain)
                        return r;
                }
            }
        }

        return int.MaxValue;
    }
}
