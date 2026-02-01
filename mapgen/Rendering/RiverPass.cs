using Dreamlands.Map;
using SkiaSharp;

namespace MapGen.Rendering;

public static class RiverPass
{
    private const int TileSize = 128;
    private const float Half = TileSize / 2f;
    private const int NumPoints = 12;
    private const float MaxWander = 18f;
    private const float WanderStep = 8f;
    private const float OutlineWidth = 10f;
    private const float MainWidth = 6f;

    private static readonly SKColor OutlineColor = new(40, 80, 140);
    private static readonly SKColor MainColor = new(70, 130, 200);

    public static void Draw(SKCanvas canvas, Map map, int seed)
    {
        var paths = new List<SKPath>();

        foreach (var node in map.AllNodes())
        {
            if (!node.HasRiver || node.IsWater) continue;

            var dirs = DirectionExtensions.Each().Where(d => node.HasRiverOn(d)).ToList();
            var pairs = PairDirections(dirs);
            var rng = new Random(HashCode.Combine(seed, node.X, node.Y, 0x52495645));
            bool hasPoi = node.Poi != null && node.Poi.Kind != PoiKind.WaterSource;

            foreach (var (a, b) in pairs)
            {
                var exit = b != Direction.None ? b : a.Opposite();
                var start = EdgeAnchor(node, a, seed);
                var end = EdgeAnchor(node, exit, seed);
                bool isBend = a != exit.Opposite();

                var points = BasePath(start, end, a, isBend);
                if (!hasPoi) RandomWalk(points, rng);
                SnipLoops(points);
                paths.Add(Smooth(points));
            }
        }

        using var outlinePaint = new SKPaint
        {
            Color = OutlineColor, StrokeWidth = OutlineWidth,
            Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Round, IsAntialias = true
        };
        using var mainPaint = new SKPaint
        {
            Color = MainColor, StrokeWidth = MainWidth,
            Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Round, IsAntialias = true
        };

        foreach (var p in paths) canvas.DrawPath(p, outlinePaint);
        foreach (var p in paths) canvas.DrawPath(p, mainPaint);
        foreach (var p in paths) p.Dispose();
    }

    // Deterministic anchor on tile edge — neighboring tiles produce the same
    // point on their shared edge because the hash keys are normalized.
    static SKPoint EdgeAnchor(Node node, Direction dir, int seed)
    {
        float baseX, baseY;
        int ek1, ek2, orient;

        switch (dir)
        {
            case Direction.North:
                baseX = node.X * TileSize + Half; baseY = node.Y * TileSize;
                ek1 = node.X; ek2 = node.Y; orient = 0; break;
            case Direction.South:
                baseX = node.X * TileSize + Half; baseY = (node.Y + 1) * TileSize;
                ek1 = node.X; ek2 = node.Y + 1; orient = 0; break;
            case Direction.East:
                baseX = (node.X + 1) * TileSize; baseY = node.Y * TileSize + Half;
                ek1 = node.X + 1; ek2 = node.Y; orient = 1; break;
            case Direction.West:
                baseX = node.X * TileSize; baseY = node.Y * TileSize + Half;
                ek1 = node.X; ek2 = node.Y; orient = 1; break;
            default:
                return new SKPoint(node.X * TileSize + Half, node.Y * TileSize + Half);
        }

        float shift = ((HashCode.Combine(seed, ek1, ek2, orient) & 0xFFFF) / 65536f - 0.5f)
                     * TileSize * 0.25f;
        if (orient == 0) baseX += shift; else baseY += shift;
        return new SKPoint(baseX, baseY);
    }

    // Lay down evenly-spaced points. Straight line for through-flow,
    // quadratic arc for 90-degree bends so noise starts on a smooth base.
    static List<SKPoint> BasePath(SKPoint start, SKPoint end, Direction startDir, bool isBend)
    {
        var pts = new List<SKPoint>(NumPoints + 1);

        if (isBend)
        {
            // Control point at the intersection of inward directions (≈ tile center)
            var cp = (startDir is Direction.North or Direction.South)
                ? new SKPoint(start.X, end.Y)
                : new SKPoint(end.X, start.Y);

            for (int i = 0; i <= NumPoints; i++)
            {
                float t = i / (float)NumPoints;
                float u = 1 - t;
                pts.Add(new SKPoint(
                    u * u * start.X + 2 * u * t * cp.X + t * t * end.X,
                    u * u * start.Y + 2 * u * t * cp.Y + t * t * end.Y));
            }
        }
        else
        {
            for (int i = 0; i <= NumPoints; i++)
            {
                float t = i / (float)NumPoints;
                pts.Add(new SKPoint(
                    start.X + (end.X - start.X) * t,
                    start.Y + (end.Y - start.Y) * t));
            }
        }

        return pts;
    }

    // Correlated perpendicular displacement — each step drifts from the last
    // rather than jumping independently, giving natural-looking meander.
    // Tapers to zero near tile edges so adjacent tiles join seamlessly.
    static void RandomWalk(List<SKPoint> pts, Random rng)
    {
        float drift = 0;
        const int fade = 2;

        for (int i = 1; i < pts.Count - 1; i++)
        {
            drift += ((float)rng.NextDouble() * 2 - 1) * WanderStep;
            drift = Math.Clamp(drift, -MaxWander, MaxWander);

            float taper = Math.Min(Math.Min(i, pts.Count - 1 - i) / (float)fade, 1f);

            // Perpendicular to local path tangent
            float dx = pts[i + 1].X - pts[i - 1].X;
            float dy = pts[i + 1].Y - pts[i - 1].Y;
            float len = MathF.Sqrt(dx * dx + dy * dy);
            if (len < 0.01f) continue;

            float d = drift * taper;
            pts[i] = new SKPoint(
                pts[i].X + (-dy / len) * d,
                pts[i].Y + (dx / len) * d);
        }
    }

    // Remove self-intersecting loops from the point list before smoothing.
    // Scans for segment crossings and snips out the looped points.
    static void SnipLoops(List<SKPoint> pts)
    {
        restart:
        for (int i = 0; i < pts.Count - 2; i++)
        {
            for (int j = i + 2; j < pts.Count - 1; j++)
            {
                if (SegmentsIntersect(pts[i], pts[i + 1], pts[j], pts[j + 1]))
                {
                    pts.RemoveRange(i + 1, j - i - 1);
                    goto restart;
                }
            }
        }
    }

    static bool SegmentsIntersect(SKPoint a, SKPoint b, SKPoint c, SKPoint d)
    {
        float d1 = Cross(c, d, a);
        float d2 = Cross(c, d, b);
        float d3 = Cross(a, b, c);
        float d4 = Cross(a, b, d);
        if (((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0)) &&
            ((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0)))
            return true;
        return false;
    }

    static float Cross(SKPoint a, SKPoint b, SKPoint p) =>
        (b.X - a.X) * (p.Y - a.Y) - (b.Y - a.Y) * (p.X - a.X);

    // Catmull-Rom → cubic bezier conversion for a smooth curve through all points.
    static SKPath Smooth(List<SKPoint> pts)
    {
        var path = new SKPath();
        if (pts.Count < 2) return path;

        path.MoveTo(pts[0]);
        for (int i = 0; i < pts.Count - 1; i++)
        {
            var p0 = pts[Math.Max(0, i - 1)];
            var p1 = pts[i];
            var p2 = pts[i + 1];
            var p3 = pts[Math.Min(pts.Count - 1, i + 2)];

            path.CubicTo(
                new SKPoint(p1.X + (p2.X - p0.X) / 6f, p1.Y + (p2.Y - p0.Y) / 6f),
                new SKPoint(p2.X - (p3.X - p1.X) / 6f, p2.Y - (p3.Y - p1.Y) / 6f),
                p2);
        }
        return path;
    }

    static List<(Direction a, Direction b)> PairDirections(List<Direction> dirs)
    {
        if (dirs.Count <= 2)
            return dirs.Count == 1
                ? [(dirs[0], Direction.None)]
                : [(dirs[0], dirs[1])];

        var remaining = new List<Direction>(dirs);
        var pairs = new List<(Direction, Direction)>();

        // Pair opposites first (straight-through), then remaining as bends
        foreach (var d in new[] { Direction.North, Direction.East })
        {
            var opp = d.Opposite();
            if (remaining.Remove(d) && remaining.Remove(opp))
                pairs.Add((d, opp));
        }
        while (remaining.Count >= 2)
        {
            pairs.Add((remaining[0], remaining[1]));
            remaining.RemoveRange(0, 2);
        }
        if (remaining.Count == 1)
            pairs.Add((remaining[0], Direction.None));

        return pairs;
    }
}
