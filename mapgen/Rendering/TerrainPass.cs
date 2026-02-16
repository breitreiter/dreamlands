using Dreamlands.Map;
using SkiaSharp;

namespace MapGen.Rendering;

public static class TerrainPass
{
    private static readonly Dictionary<Terrain, string> TextureFiles = new()
    {
        [Terrain.Lake] = "water.png",
        [Terrain.Plains] = "plains.png",
        [Terrain.Forest] = "forest.png",
        [Terrain.Scrub] = "scrub.png",
        [Terrain.Mountains] = "mountains.png",
        [Terrain.Swamp] = "swamp.png"
    };

    public static IEnumerable<string> RequiredTextureFiles =>
        TextureFiles.Values.Distinct();

    public static void Draw(SKCanvas canvas, Map map, int tileSize, Dictionary<string, SKBitmap> textures)
    {
        var terrainGroups = map.AllNodes()
            .Where(n => !n.IsWater)
            .GroupBy(n => TextureFiles[n.Terrain]);

        foreach (var group in terrainGroups)
        {
            using var shader = SKShader.CreateBitmap(
                textures[group.Key],
                SKShaderTileMode.Repeat,
                SKShaderTileMode.Repeat);
            using var paint = new SKPaint { Shader = shader };

            var path = new SKPath();
            foreach (var node in group)
                path.AddRect(new SKRect(
                    node.X * tileSize,
                    node.Y * tileSize,
                    (node.X + 1) * tileSize,
                    (node.Y + 1) * tileSize));

            canvas.DrawPath(path, paint);
            path.Dispose();
        }
    }
}
