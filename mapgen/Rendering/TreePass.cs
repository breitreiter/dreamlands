using Dreamlands.Map;
using Dreamlands.Rules;
using SkiaSharp;

namespace MapGen.Rendering;

public static class TreePass
{
    private const int TileSize = 128;
    private const int GridCells = 5; // 5x5 grid per tile
    private const float CellSize = TileSize / (float)GridCells;
    private const float JitterFraction = 0.70f;
    private const float SkipChance = 0.20f;
    private const int ZoneSize = 256; // species clustering zone in pixels
    private const float DominantChance = 0.70f;
    private const float DecalScale = 0.40f;
    private const float ThinRadius = 192f; // 1.5 tiles
    private const float MaxThinSkip = 0.85f;

    public static void Draw(SKCanvas canvas, Map map, int seed)
    {
        string decalDir = Path.Combine("..", "assets", "map", "decals", "trees");
        var species = LoadSpecies(decalDir);
        if (species.Count == 0) return;

        try
        {
            var placements = new List<(float x, float y, SKBitmap decal)>();
            var rng = new Random(seed);

            var poiCenters = map.AllNodes()
                .Where(n => n.Poi?.Kind == PoiKind.Settlement)
                .Select(n => ((n.X + 0.5f) * TileSize, (n.Y + 0.5f) * TileSize))
                .ToList();

            foreach (var node in map.AllNodes().Where(n => n.Terrain == Terrain.Forest))
            {
                int tileLeft = node.X * TileSize;
                int tileTop = node.Y * TileSize;

                for (int gy = 0; gy < GridCells; gy++)
                for (int gx = 0; gx < GridCells; gx++)
                {
                    if (rng.NextDouble() < SkipChance) continue;

                    float cx = tileLeft + (gx + 0.5f) * CellSize;
                    float cy = tileTop + (gy + 0.5f) * CellSize;

                    float jitterRange = CellSize * JitterFraction * 0.5f;
                    float px = cx + (float)(rng.NextDouble() * 2 - 1) * jitterRange;
                    float py = cy + (float)(rng.NextDouble() * 2 - 1) * jitterRange;

                    int tileX = (int)(px / TileSize);
                    int tileY = (int)(py / TileSize);
                    if (!map.InBounds(tileX, tileY) || map[tileX, tileY].IsWater) continue;

                    float nearestPoiDist = float.MaxValue;
                    foreach (var (poiX, poiY) in poiCenters)
                    {
                        float dx = px - poiX, dy = py - poiY;
                        float d = MathF.Sqrt(dx * dx + dy * dy);
                        if (d < nearestPoiDist) nearestPoiDist = d;
                    }
                    if (nearestPoiDist < ThinRadius && rng.NextDouble() < MaxThinSkip * (1f - nearestPoiDist / ThinRadius))
                        continue;

                    int zoneX = (int)(px / ZoneSize);
                    int zoneY = (int)(py / ZoneSize);
                    int dominantIdx = Math.Abs(HashCode.Combine(seed, zoneX, zoneY)) % species.Count;

                    int speciesIdx = rng.NextDouble() < DominantChance
                        ? dominantIdx
                        : rng.Next(species.Count);

                    var decals = species[speciesIdx];
                    var decal = decals[rng.Next(decals.Count)];

                    placements.Add((px, py, decal));
                }
            }

            // Painter's algorithm: draw back-to-front
            placements.Sort((a, b) => a.y.CompareTo(b.y));

            foreach (var (px, py, decal) in placements)
            {
                float w = decal.Width * DecalScale;
                float h = decal.Height * DecalScale;
                var dest = new SKRect(px - w * 0.5f, py - h * 0.5f, px + w * 0.5f, py + h * 0.5f);
                canvas.DrawBitmap(decal, dest);
            }
        }
        finally
        {
            foreach (var decals in species)
            foreach (var bmp in decals)
                bmp.Dispose();
        }
    }

    static List<List<SKBitmap>> LoadSpecies(string decalDir)
    {
        var species = new List<List<SKBitmap>>();
        if (!Directory.Exists(decalDir)) return species;

        foreach (var dir in Directory.GetDirectories(decalDir).Order())
        {
            var name = Path.GetFileName(dir);
            if (name.Contains("palm", StringComparison.OrdinalIgnoreCase)
                || name.Contains("beech", StringComparison.OrdinalIgnoreCase))
                continue;

            var decals = new List<SKBitmap>();
            foreach (var file in Directory.GetFiles(dir, "*.png").Order())
            {
                var bmp = SKBitmap.Decode(file);
                if (bmp != null) decals.Add(bmp);
            }

            if (decals.Count > 0) species.Add(decals);
        }

        return species;
    }
}
