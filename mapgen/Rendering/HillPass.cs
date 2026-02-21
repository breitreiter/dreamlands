using Dreamlands.Map;
using Dreamlands.Rules;
using SkiaSharp;

namespace MapGen.Rendering;

public static class HillPass
{
    private const int TileSize = 128;
    private const float CellSize = 80f;
    private const float JitterFraction = 0.60f;
    private const float SkipChance = 0.30f;

    private const float PalmCellSize = 128f;
    private const float PalmSkipChance = 0.70f;
    private const float PalmScale = 0.40f;

    public static void Draw(SKCanvas canvas, Map map, int seed)
    {
        string decalDir = Path.Combine("..", "assets", "map", "decals", "relief_lines");
        var decals = LoadDecals(decalDir);
        if (decals.Count == 0) return;

        try
        {
            float medianWidth = decals.OrderBy(d => d.Width).ElementAt(decals.Count / 2).Width;
            float scale = TileSize / medianWidth;

            var rng = new Random(seed ^ 0x48494C4C); // distinct stream from other passes

            int cellsX = (int)MathF.Ceiling(map.Width * TileSize / CellSize);
            int cellsY = (int)MathF.Ceiling(map.Height * TileSize / CellSize);

            for (int gy = 0; gy < cellsY; gy++)
            for (int gx = 0; gx < cellsX; gx++)
            {
                if (rng.NextDouble() < SkipChance) continue;

                float cx = (gx + 0.5f) * CellSize;
                float cy = (gy + 0.5f) * CellSize;

                float jitterRange = CellSize * JitterFraction * 0.5f;
                float px = cx + (float)(rng.NextDouble() * 2 - 1) * jitterRange;
                float py = cy + (float)(rng.NextDouble() * 2 - 1) * jitterRange;

                int tileX = (int)(px / TileSize);
                int tileY = (int)(py / TileSize);
                if (!map.InBounds(tileX, tileY)) continue;
                if (map[tileX, tileY].Terrain != Terrain.Scrub) continue;

                var decal = decals[rng.Next(decals.Count)];
                float w = decal.Width * scale;
                float h = decal.Height * scale;

                // Check that left and right extents don't bleed onto water
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
        finally
        {
            foreach (var bmp in decals)
                bmp.Dispose();
        }

        DrawPalms(canvas, map, seed);
    }

    static void DrawPalms(SKCanvas canvas, Map map, int seed)
    {
        string palmDir = Path.Combine("..", "assets", "map", "decals", "trees", "Conan date palm normal");
        var palms = LoadDecals(palmDir);
        if (palms.Count == 0) return;

        try
        {
            var placements = new List<(float x, float y, SKBitmap decal)>();
            var rng = new Random(seed ^ 0x50414C4D); // "PALM"

            int cellsX = (int)MathF.Ceiling(map.Width * TileSize / PalmCellSize);
            int cellsY = (int)MathF.Ceiling(map.Height * TileSize / PalmCellSize);

            for (int gy = 0; gy < cellsY; gy++)
            for (int gx = 0; gx < cellsX; gx++)
            {
                if (rng.NextDouble() < PalmSkipChance) continue;

                float cx = (gx + 0.5f) * PalmCellSize;
                float cy = (gy + 0.5f) * PalmCellSize;

                float jitterRange = PalmCellSize * JitterFraction * 0.5f;
                float px = cx + (float)(rng.NextDouble() * 2 - 1) * jitterRange;
                float py = cy + (float)(rng.NextDouble() * 2 - 1) * jitterRange;

                int tileX = (int)(px / TileSize);
                int tileY = (int)(py / TileSize);
                if (!map.InBounds(tileX, tileY)) continue;
                if (map[tileX, tileY].Terrain != Terrain.Scrub) continue;

                var decal = palms[rng.Next(palms.Count)];
                placements.Add((px, py, decal));
            }

            placements.Sort((a, b) => a.y.CompareTo(b.y));

            foreach (var (px, py, decal) in placements)
            {
                float w = decal.Width * PalmScale;
                float h = decal.Height * PalmScale;
                var dest = new SKRect(px - w * 0.5f, py - h * 0.5f, px + w * 0.5f, py + h * 0.5f);
                canvas.DrawBitmap(decal, dest);
            }
        }
        finally
        {
            foreach (var bmp in palms)
                bmp.Dispose();
        }
    }

    static List<SKBitmap> LoadDecals(string dir)
    {
        var decals = new List<SKBitmap>();
        if (!Directory.Exists(dir)) return decals;

        foreach (var file in Directory.GetFiles(dir, "*.png").Order())
        {
            var bmp = SKBitmap.Decode(file);
            if (bmp != null) decals.Add(bmp);
        }

        return decals;
    }
}
