using Dreamlands.Map;
using SkiaSharp;

namespace MapGen.Rendering;

public static class PoiPass
{
    private const int TileSize = 128;
    private const int DilateRadius = 3;
    private const float BlurSigma = 4f;

    static readonly Dictionary<Terrain, string> TerrainToFolder = new()
    {
        [Terrain.Plains] = "plains_hills",
        [Terrain.Hills] = "scrub",
        [Terrain.Forest] = "forest",
        [Terrain.Mountains] = "mountain",
        [Terrain.Swamp] = "swamp",
    };

    static readonly Dictionary<string, string> BiomeToDungeonFolder = new()
    {
        ["forest"] = "forest",
        ["swamp"] = "swamp",
        ["hills"] = "hills",
        ["plains"] = "plains",
    };

    public static void Draw(SKCanvas canvas, Map map, SKImage terrainBackground, int seed)
    {
        string decalDir = Path.Combine("..", "assets", "map", "decals", "poi", "settlements");
        string dungeonBaseDir = Path.Combine("..", "assets", "map", "decals", "poi", "dungeons");
        var pools = LoadDecalPools(decalDir);
        if (pools.Count == 0) return;

        var cityDecal = SKBitmap.Decode(Path.Combine(decalDir, "aldgate.png"));

        // Scale: fit 75th-percentile decal width into tile (allows wider decals to overflow slightly)
        var allDecals = pools.Values.SelectMany(p => p).ToList();
        var allWidths = allDecals.Select(d => (float)d.Width).OrderBy(w => w).ToList();
        float referenceWidth = allWidths[allWidths.Count * 3 / 4];
        float scale = TileSize / referenceWidth;

        // Anchor: center a median-height decal in tile; tall decals extend upward
        var allHeights = allDecals.Select(d => (float)d.Height).OrderBy(h => h).ToList();
        float medianScaledH = allHeights[allHeights.Count / 2] * scale;

        var rng = new Random(seed);
        var placements = new List<(float x, float y, SKBitmap decal, bool owned)>();
        var dungeonDecalCache = new Dictionary<string, SKBitmap>();

        // Mountain POIs are handled by MountainPass (layered into mountain Y-sort)
        foreach (var node in map.AllNodes().Where(n => n.Terrain != Terrain.Mountains && n.Poi != null))
        {
            if (node.Poi!.Kind == PoiKind.Settlement)
            {
                float cx = (node.X + 0.5f) * TileSize;
                float cy = (node.Y + 0.5f) * TileSize;

                SKBitmap decal;
                if (node.Poi.Type == "City" && cityDecal != null)
                    decal = cityDecal;
                else
                {
                    var pool = TerrainToFolder.TryGetValue(node.Terrain, out var folder) && pools.TryGetValue(folder, out var p)
                        ? p : pools.Values.First();
                    decal = pool[rng.Next(pool.Count)];
                }
                placements.Add((cx, cy, decal, false));
            }
            else if (node.Poi!.Kind == PoiKind.Dungeon && node.Poi.DecalFile != null)
            {
                var biomeKey = node.Terrain switch
                {
                    Terrain.Forest => "forest",
                    Terrain.Swamp => "swamp",
                    Terrain.Hills => "hills",
                    Terrain.Plains => "plains",
                    _ => null
                };
                if (biomeKey == null) continue;

                var decalPath = Path.Combine(dungeonBaseDir, biomeKey, node.Poi.DecalFile);
                if (!dungeonDecalCache.TryGetValue(decalPath, out var decal))
                {
                    decal = File.Exists(decalPath) ? SKBitmap.Decode(decalPath) : null;
                    if (decal != null)
                        dungeonDecalCache[decalPath] = decal;
                }
                if (decal == null) continue;

                float cx = (node.X + 0.5f) * TileSize;
                float cy = (node.Y + 0.5f) * TileSize;
                placements.Add((cx, cy, decal, false));
            }
        }

        // Painter's algorithm: back-to-front by ground position
        placements.Sort((a, b) => a.y.CompareTo(b.y));

        try
        {
            foreach (var (cx, cy, decal, _) in placements)
            {
                float w = decal.Width * scale;
                float h = decal.Height * scale;
                float anchorY = cy + medianScaledH / 2;
                var dest = new SKRect(cx - w / 2, anchorY - h, cx + w / 2, anchorY);

                DrawHalo(canvas, terrainBackground, decal, dest);
                canvas.DrawBitmap(decal, dest);
            }
        }
        finally
        {
            foreach (var pool in pools.Values)
            foreach (var bmp in pool)
                bmp.Dispose();
            foreach (var bmp in dungeonDecalCache.Values)
                bmp.Dispose();
            cityDecal?.Dispose();
        }
    }

    static void DrawHalo(SKCanvas canvas, SKImage background, SKBitmap decal, SKRect dest)
    {
        float expand = DilateRadius + BlurSigma * 3;
        int left = (int)Math.Floor(dest.Left - expand);
        int top = (int)Math.Floor(dest.Top - expand);
        int right = (int)Math.Ceiling(dest.Right + expand);
        int bottom = (int)Math.Ceiling(dest.Bottom + expand);
        int haloW = right - left;
        int haloH = bottom - top;
        if (haloW <= 0 || haloH <= 0) return;

        // 1. Render decal with dilate+blur to get halo alpha shape
        using var maskSurface = SKSurface.Create(new SKImageInfo(haloW, haloH));
        maskSurface.Canvas.Clear(SKColors.Transparent);

        using var filterPaint = new SKPaint
        {
            ImageFilter = SKImageFilter.CreateBlur(BlurSigma, BlurSigma,
                SKImageFilter.CreateDilate(DilateRadius, DilateRadius))
        };

        var decalRect = new SKRect(
            dest.Left - left, dest.Top - top,
            dest.Right - left, dest.Bottom - top);
        maskSurface.Canvas.DrawBitmap(decal, decalRect, filterPaint);
        using var maskImage = maskSurface.Snapshot();

        // 2. Sample terrain background through halo alpha (DstIn)
        using var haloSurface = SKSurface.Create(new SKImageInfo(haloW, haloH));
        haloSurface.Canvas.Clear(SKColors.Transparent);
        haloSurface.Canvas.DrawImage(background,
            new SKRect(left, top, right, bottom),
            new SKRect(0, 0, haloW, haloH));

        using var dstInPaint = new SKPaint { BlendMode = SKBlendMode.DstIn };
        haloSurface.Canvas.DrawImage(maskImage, 0, 0, dstInPaint);
        using var haloImage = haloSurface.Snapshot();

        // 3. Stamp halo onto main canvas (covers trees, shows clean background)
        canvas.DrawImage(haloImage, left, top);
    }

    static Dictionary<string, List<SKBitmap>> LoadDecalPools(string dir)
    {
        var pools = new Dictionary<string, List<SKBitmap>>();
        if (!Directory.Exists(dir)) return pools;

        foreach (var subdir in Directory.GetDirectories(dir).Order())
        {
            var folder = Path.GetFileName(subdir);
            var decals = new List<SKBitmap>();
            foreach (var file in Directory.GetFiles(subdir, "*.png").Order())
            {
                var bmp = SKBitmap.Decode(file);
                if (bmp != null) decals.Add(bmp);
            }
            if (decals.Count > 0) pools[folder] = decals;
        }

        return pools;
    }
}
