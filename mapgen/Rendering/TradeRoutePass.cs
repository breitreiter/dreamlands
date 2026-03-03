using Dreamlands.Map;
using SkiaSharp;

namespace MapGen.Rendering;

public static class TradeRoutePass
{
    private const int TileSize = 128;

    public static void Draw(SKCanvas canvas, Map map)
    {
        using var paint = new SKPaint
        {
            Color = SKColors.Black.WithAlpha(38),
            StrokeWidth = 4,
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            PathEffect = SKPathEffect.CreateDash(new float[] { 12, 8 }, 0)
        };

        foreach (var (from, to) in map.TradeEdges)
        {
            float x1 = from.X * TileSize + TileSize / 2f;
            float y1 = from.Y * TileSize + TileSize / 2f;
            float x2 = to.X * TileSize + TileSize / 2f;
            float y2 = to.Y * TileSize + TileSize / 2f;

            // S-curve: control points offset perpendicular to the midpoint
            float mx = (x1 + x2) / 2f;
            float my = (y1 + y2) / 2f;
            float dx = x2 - x1;
            float dy = y2 - y1;
            float len = MathF.Sqrt(dx * dx + dy * dy);
            float nx = -dy / len * len * 0.15f;
            float ny = dx / len * len * 0.15f;

            using var path = new SKPath();
            path.MoveTo(x1, y1);
            path.CubicTo(mx + nx, my + ny, mx - nx, my - ny, x2, y2);
            canvas.DrawPath(path, paint);
        }
    }
}
