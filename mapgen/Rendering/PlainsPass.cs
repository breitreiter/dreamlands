using Dreamlands.Map;
using Dreamlands.Rules;
using SkiaSharp;

namespace MapGen.Rendering;

public static class PlainsPass
{
    private const int TileSize = 128;

    // Grass layer: dense mixed grass
    private const float GrassCellSize = 28f;
    private const float GrassSkipChance = 0.20f;
    private const float GrassScale = 1.0f;
    private const float GrassJitter = 0.70f;

    // Farm layer: sparse farmsteads
    private const float FarmCellSize = 128f;
    private const float FarmSkipChance = 0.65f;
    private const float FarmScale = 1.0f;
    private const float FarmJitter = 0.60f;

    public static void Draw(SKCanvas canvas, Map map, int seed)
    {
        var rng = new Random(seed ^ 0x504C4E53);

        // T1: grass + farms
        DrawTier(canvas, map, rng, 1, "plains/t1/grass", "*.png",
            GrassCellSize, GrassSkipChance, GrassScale, GrassJitter);
        DrawTier(canvas, map, rng, 1, "plains/t1/farms", "*.png",
            FarmCellSize, FarmSkipChance, FarmScale, FarmJitter);

        // T2: sparser scatter
        DrawTier(canvas, map, rng, 2, "plains/t2", "*.png",
            GrassCellSize, 0.90f, GrassScale, GrassJitter);

        // T3: sparser scatter
        DrawTier(canvas, map, rng, 3, "plains/t3", "*.png",
            GrassCellSize, 0.95f, GrassScale, GrassJitter);
    }

    static void DrawTier(SKCanvas canvas, Map map, Random rng, int tier,
        string subDir, string pattern,
        float cellSize, float skipChance, float scale, float jitter)
    {
        string dir = Path.Combine("..", "assets", "map", "decals", subDir);
        var decals = LoadDecals(dir, pattern);
        if (decals.Count == 0) return;

        try
        {
            ScatterLayer(canvas, map, rng, decals, tier, cellSize, skipChance, scale, jitter);
        }
        finally
        {
            foreach (var bmp in decals) bmp.Dispose();
        }
    }

    static void ScatterLayer(SKCanvas canvas, Map map, Random rng,
        List<SKBitmap> decals, int tier, float cellSize, float skipChance, float scale, float jitter)
    {
        int cellsX = (int)MathF.Ceiling(map.Width * TileSize / cellSize);
        int cellsY = (int)MathF.Ceiling(map.Height * TileSize / cellSize);

        for (int gy = 0; gy < cellsY; gy++)
        for (int gx = 0; gx < cellsX; gx++)
        {
            if (rng.NextDouble() < skipChance) continue;

            float cx = (gx + 0.5f) * cellSize;
            float cy = (gy + 0.5f) * cellSize;

            float jitterRange = cellSize * jitter * 0.5f;
            float px = cx + (float)(rng.NextDouble() * 2 - 1) * jitterRange;
            float py = cy + (float)(rng.NextDouble() * 2 - 1) * jitterRange;

            int tileX = (int)(px / TileSize);
            int tileY = (int)(py / TileSize);
            if (!map.InBounds(tileX, tileY)) continue;

            var node = map[tileX, tileY];
            if (node.Terrain != Terrain.Plains) continue;
            if ((node.Region?.Tier ?? 1) != tier) continue;

            var decal = decals[rng.Next(decals.Count)];
            float w = decal.Width * scale;
            float h = decal.Height * scale;

            int leftTileX = (int)((px - w * 0.5f) / TileSize);
            int rightTileX = (int)((px + w * 0.5f) / TileSize);
            if (leftTileX >= 0 && map.InBounds(leftTileX, tileY) && map[leftTileX, tileY].IsWater)
                continue;
            if (rightTileX < map.Width && map.InBounds(rightTileX, tileY) && map[rightTileX, tileY].IsWater)
                continue;

            var dest = new SKRect(px - w * 0.5f, py - h * 0.5f, px + w * 0.5f, py + h * 0.5f);
            canvas.DrawBitmap(decal, dest);
        }
    }

    static List<SKBitmap> LoadDecals(string dir, string pattern)
    {
        var decals = new List<SKBitmap>();
        if (!Directory.Exists(dir)) return decals;

        foreach (var file in Directory.GetFiles(dir, pattern).Order())
        {
            if (file.EndsWith("~")) continue;
            var bmp = SKBitmap.Decode(file);
            if (bmp != null) decals.Add(bmp);
        }

        return decals;
    }
}
