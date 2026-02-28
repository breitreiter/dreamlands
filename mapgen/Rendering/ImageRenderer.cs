using Dreamlands.Map;
using SkiaSharp;

namespace MapGen.Rendering;

public static class ImageRenderer
{
    private const int TileSize = 128;

    /// <summary>
    /// Renders the map and returns the final SKImage. Caller owns the returned image and must dispose it.
    /// </summary>
    public static SKImage Render(Map map, int seed)
    {
        int canvasWidth = map.Width * TileSize;
        int canvasHeight = map.Height * TileSize;
        string textureDir = Path.Combine("..", "assets", "map", "textures");

        var textures = new Dictionary<string, SKBitmap>();
        try
        {
            foreach (var file in TerrainPass.RequiredTextureFiles)
            {
                var path = Path.Combine(textureDir, file);
                var bitmap = SKBitmap.Decode(path)
                    ?? throw new FileNotFoundException($"Could not load texture: {path}");
                textures[file] = bitmap;
            }

            Console.Error.WriteLine($"Rendering PNG ({canvasWidth}x{canvasHeight})...");
            using var surface = SKSurface.Create(new SKImageInfo(canvasWidth, canvasHeight));
            var canvas = surface.Canvas;

            Console.Error.WriteLine("  Water...");
            WaterPass.Draw(canvas, canvasWidth, canvasHeight, textures);
            Console.Error.WriteLine("  Lakes...");
            LakePass.Draw(canvas, map);
            Console.Error.WriteLine("  Plains...");
            PlainsPass.Draw(canvas, map, seed);
            Console.Error.WriteLine("  Hills...");
            HillPass.Draw(canvas, map, seed);
            Console.Error.WriteLine("  Swamp...");
            SwampPass.Draw(canvas, map, seed);
            Console.Error.WriteLine("  Mountains...");
            MountainPass.Draw(canvas, map, seed);
            Console.Error.WriteLine("  Trees...");
            TreePass.Draw(canvas, map, seed);
            using var terrainSnapshot = surface.Snapshot();
            Console.Error.WriteLine("  POIs...");
            PoiPass.Draw(canvas, map, terrainSnapshot, seed);

            return surface.Snapshot();
        }
        finally
        {
            foreach (var bitmap in textures.Values)
                bitmap.Dispose();
        }
    }
}
