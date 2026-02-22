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
            int divisor = 1 << (MaxZoom - z);
            int scaledW = (source.Width + divisor - 1) / divisor;
            int scaledH = (source.Height + divisor - 1) / divisor;

            int tilesX = (scaledW + TileSize - 1) / TileSize;
            int tilesY = (scaledH + TileSize - 1) / TileSize;

            Console.Error.WriteLine($"  Zoom {z}: {scaledW}x{scaledH} -> {tilesX * tilesY} tiles...");

            using var scaledBitmap = new SKBitmap(scaledW, scaledH);
            using var scaledCanvas = new SKCanvas(scaledBitmap);
            scaledCanvas.DrawImage(source, new SKRect(0, 0, scaledW, scaledH));

            // Pre-create tile column directories
            for (int x = 0; x < tilesX; x++)
                Directory.CreateDirectory(Path.Combine(outputDir, z.ToString(), x.ToString()));

            // Each tile reads from the shared scaledBitmap (thread-safe for pixel reads)
            // and encodes to PNG independently
            var tiles = new List<(int x, int y)>();
            for (int x = 0; x < tilesX; x++)
            for (int y = 0; y < tilesY; y++)
                tiles.Add((x, y));

            Parallel.ForEach(tiles, tile =>
            {
                int srcX = tile.x * TileSize;
                int srcY = tile.y * TileSize;
                int w = Math.Min(TileSize, scaledW - srcX);
                int h = Math.Min(TileSize, scaledH - srcY);

                using var tileBitmap = new SKBitmap(TileSize, TileSize);
                using var tileCanvas = new SKCanvas(tileBitmap);
                tileCanvas.Clear(SKColors.Transparent);

                var srcRect = new SKRect(srcX, srcY, srcX + w, srcY + h);
                var destRect = new SKRect(0, 0, w, h);

                // SKBitmap.ReadPixels is thread-safe; DrawBitmap reads pixels then draws
                tileCanvas.DrawBitmap(scaledBitmap, srcRect, destRect);

                var tilePath = Path.Combine(outputDir, z.ToString(), tile.x.ToString(), $"{tile.y}.png");
                using var image = SKImage.FromBitmap(tileBitmap);
                using var data = image.Encode(SKEncodedImageFormat.Png, 90);
                using var stream = File.Create(tilePath);
                data.SaveTo(stream);
            });
        }
    }
}
