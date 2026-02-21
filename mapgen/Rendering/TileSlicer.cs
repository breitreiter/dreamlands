using SkiaSharp;

namespace MapGen.Rendering;

public static class TileSlicer
{
    private const int TileSize = 256;
    private const int MaxZoom = 6;

    public static void Slice(SKImage source, string outputDir)
    {
        for (int z = 0; z <= MaxZoom; z++)
        {
            // Each zoom level is exactly half the resolution of the next.
            // At max zoom, use source dimensions; each step down halves them.
            int divisor = 1 << (MaxZoom - z);
            int scaledW = (source.Width + divisor - 1) / divisor;
            int scaledH = (source.Height + divisor - 1) / divisor;

            Console.Error.WriteLine($"  Zoom {z}: {scaledW}x{scaledH} -> tiles...");

            using var scaledBitmap = new SKBitmap(scaledW, scaledH);
            using var scaledCanvas = new SKCanvas(scaledBitmap);
            scaledCanvas.DrawImage(source, new SKRect(0, 0, scaledW, scaledH));

            int tilesX = (scaledW + TileSize - 1) / TileSize;
            int tilesY = (scaledH + TileSize - 1) / TileSize;

            for (int x = 0; x < tilesX; x++)
            {
                var tileDir = Path.Combine(outputDir, z.ToString(), x.ToString());
                Directory.CreateDirectory(tileDir);

                for (int y = 0; y < tilesY; y++)
                {
                    int srcX = x * TileSize;
                    int srcY = y * TileSize;
                    int w = Math.Min(TileSize, scaledW - srcX);
                    int h = Math.Min(TileSize, scaledH - srcY);

                    using var tileBitmap = new SKBitmap(TileSize, TileSize);
                    using var tileCanvas = new SKCanvas(tileBitmap);
                    tileCanvas.Clear(SKColors.Transparent);

                    var srcRect = new SKRect(srcX, srcY, srcX + w, srcY + h);
                    var destRect = new SKRect(0, 0, w, h);
                    tileCanvas.DrawBitmap(scaledBitmap, srcRect, destRect);

                    var tilePath = Path.Combine(tileDir, $"{y}.png");
                    using var image = SKImage.FromBitmap(tileBitmap);
                    using var data = image.Encode(SKEncodedImageFormat.Png, 90);
                    using var stream = File.Create(tilePath);
                    data.SaveTo(stream);
                }
            }
        }
    }
}
