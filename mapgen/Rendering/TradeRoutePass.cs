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
            Color = SKColors.Black,
            StrokeWidth = 3,
            IsAntialias = true,
            Style = SKPaintStyle.Stroke
        };

        foreach (var (from, to) in map.TradeEdges)
        {
            float x1 = from.X * TileSize + TileSize / 2f;
            float y1 = from.Y * TileSize + TileSize / 2f;
            float x2 = to.X * TileSize + TileSize / 2f;
            float y2 = to.Y * TileSize + TileSize / 2f;

            canvas.DrawLine(x1, y1, x2, y2, paint);
        }
    }
}
