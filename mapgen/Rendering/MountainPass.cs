using Dreamlands.Map;
using SkiaSharp;

namespace MapGen.Rendering;

public static class MountainPass
{
    private const int TileSize = 128;

    // Unified scatter grid — dense enough for overlapping mountain layering
    private const float CellSize = 48f;
    private const float Jitter = 0.55f;
    private const float BaseSkipChance = 0.15f;
    private const float EdgeSkipBoost = 0.35f;

    // Scale ramps with height: small foothills at edges, large peaks at ridgeline
    private const float MinScale = 0.22f;
    private const float MaxScale = 0.45f;

    // BFS distance mapped to [0,1] — tiles at depth >= ReferenceDepth are full height
    private const float ReferenceDepth = 3f;
    private const float NoiseFrequency = 0.4f;
    private const float NoisePerturbation = 0.3f;

    // Height thresholds for sprite category selection
    private const float SmallHillCeiling = 0.25f;
    private const float BigHillCeiling = 0.45f;
    private const float SmallMountainCeiling = 0.65f;
    private const float BigMountainCeiling = 0.85f;

    // POI sprites layered into mountain rendering (baked-in elevation)
    private const float PoiScale = 0.32f;

    private static readonly (int dx, int dy)[] Directions = { (-1, 0), (1, 0), (0, -1), (0, 1) };

    public static void Draw(SKCanvas canvas, Map map, int seed)
    {
        string mountainDir = Path.Combine("..", "assets", "map", "decals", "mountains");
        string poiDir = Path.Combine("..", "assets", "map", "decals", "poi");
        var settlements = LoadDecals(Path.Combine(poiDir, "settlements", "mountain"));
        var dungeons = LoadDecals(Path.Combine(poiDir, "dungeons", "mountains"));
        var smallHills = LoadDecals(Path.Combine(mountainDir, "Small individual hills"));
        var bigHills = LoadDecals(Path.Combine(mountainDir, "Big individual hills"));
        var smallMountains = LoadDecals(Path.Combine(mountainDir, "Small individual mountains"));
        var bigMountains = LoadDecals(Path.Combine(mountainDir, "Big individual mountains"));
        var pinnacles = LoadDecals(Path.Combine(mountainDir, "Individual pinnacles"));
        // Ridgeline pool combines pinnacles + big mountains (references only, not owned)
        var ridgeline = new List<SKBitmap>(pinnacles.Count + bigMountains.Count);
        ridgeline.AddRange(pinnacles);
        ridgeline.AddRange(bigMountains);

        var specificDungeonDecals = new List<SKBitmap>();
        try
        {
            var heightField = ComputeHeightField(map, seed);
            var rng = new Random(seed ^ 0x4D4F4E54); // "MONT"
            var placements = new List<(float x, float y, SKBitmap decal, float scale)>();

            int cellsX = (int)MathF.Ceiling(map.Width * TileSize / CellSize);
            int cellsY = (int)MathF.Ceiling(map.Height * TileSize / CellSize);

            for (int gy = 0; gy < cellsY; gy++)
            for (int gx = 0; gx < cellsX; gx++)
            {
                float cx = (gx + 0.5f) * CellSize;
                float cy = (gy + 0.5f) * CellSize;

                float jitterRange = CellSize * Jitter * 0.5f;
                float px = cx + (float)(rng.NextDouble() * 2 - 1) * jitterRange;
                float py = cy + (float)(rng.NextDouble() * 2 - 1) * jitterRange;

                int tileX = (int)(px / TileSize);
                int tileY = (int)(py / TileSize);
                if (!map.InBounds(tileX, tileY)) continue;
                if (map[tileX, tileY].Terrain != Terrain.Mountains) continue;

                float h = heightField[tileX, tileY];

                // Sparser at edges, denser at ridgeline
                float skipChance = BaseSkipChance + (1f - h) * EdgeSkipBoost;
                if (rng.NextDouble() < skipChance) continue;

                var pool = h switch
                {
                    < SmallHillCeiling => smallHills,
                    < BigHillCeiling => bigHills,
                    < SmallMountainCeiling => smallMountains,
                    < BigMountainCeiling => bigMountains,
                    _ => ridgeline
                };
                if (pool.Count == 0) continue;

                var decal = pool[rng.Next(pool.Count)];
                float scale = MinScale + h * (MaxScale - MinScale);
                float w = decal.Width * scale;

                // Don't bleed onto water
                int leftTileX = (int)((px - w * 0.5f) / TileSize);
                int rightTileX = (int)((px + w * 0.5f) / TileSize);
                if (leftTileX >= 0 && map.InBounds(leftTileX, tileY) && map[leftTileX, tileY].IsWater)
                    continue;
                if (rightTileX < map.Width && map.InBounds(rightTileX, tileY) && map[rightTileX, tileY].IsWater)
                    continue;

                placements.Add((px, py, decal, scale));
            }

            // Mountain POIs — layered into the same Y-sort so baked-in bases get covered
            foreach (var node in map.AllNodes().Where(n => n.Terrain == Terrain.Mountains && n.Poi != null))
            {
                SKBitmap? decal = null;

                if (node.Poi!.Kind == PoiKind.Settlement)
                {
                    if (settlements.Count == 0) continue;
                    decal = settlements[rng.Next(settlements.Count)];
                }
                else if (node.Poi.Kind == PoiKind.Dungeon)
                {
                    if (node.Poi.DecalFile != null)
                    {
                        var path = Path.Combine(poiDir, "dungeons", "mountains", node.Poi.DecalFile);
                        if (File.Exists(path))
                        {
                            decal = SKBitmap.Decode(path);
                            if (decal != null) specificDungeonDecals.Add(decal);
                        }
                    }
                    decal ??= dungeons.Count > 0 ? dungeons[rng.Next(dungeons.Count)] : null;
                }

                if (decal == null) continue;
                float cx = (node.X + 0.5f) * TileSize;
                float cy = (node.Y + 0.5f) * TileSize;
                placements.Add((cx, cy, decal, PoiScale));
            }

            // Painter's algorithm: sort by Y, draw back-to-front
            placements.Sort((a, b) => a.y.CompareTo(b.y));

            foreach (var (px, py, decal, scale) in placements)
            {
                float w = decal.Width * scale;
                float dh = decal.Height * scale;
                var dest = new SKRect(px - w * 0.5f, py - dh, px + w * 0.5f, py);
                canvas.DrawBitmap(decal, dest);
            }
        }
        finally
        {
            foreach (var bmp in settlements) bmp.Dispose();
            foreach (var bmp in dungeons) bmp.Dispose();
            foreach (var bmp in smallHills) bmp.Dispose();
            foreach (var bmp in bigHills) bmp.Dispose();
            foreach (var bmp in smallMountains) bmp.Dispose();
            foreach (var bmp in bigMountains) bmp.Dispose();
            foreach (var bmp in pinnacles) bmp.Dispose();
            foreach (var bmp in specificDungeonDecals) bmp.Dispose();
        }
    }

    static float[,] ComputeHeightField(Map map, int seed)
    {
        var dist = new int[map.Width, map.Height];
        var frontier = new Queue<(int x, int y)>();

        for (int y = 0; y < map.Height; y++)
        for (int x = 0; x < map.Width; x++)
            dist[x, y] = -1;

        // Seed: mountain tiles on the boundary (adjacent to non-mountain or map edge)
        for (int y = 0; y < map.Height; y++)
        for (int x = 0; x < map.Width; x++)
        {
            if (map[x, y].Terrain != Terrain.Mountains) continue;

            bool boundary = x == 0 || x == map.Width - 1 || y == 0 || y == map.Height - 1;
            if (!boundary)
            {
                foreach (var (dx, dy) in Directions)
                {
                    if (map[x + dx, y + dy].Terrain != Terrain.Mountains)
                    {
                        boundary = true;
                        break;
                    }
                }
            }

            if (boundary)
            {
                dist[x, y] = 0;
                frontier.Enqueue((x, y));
            }
        }

        // BFS inward — each ring is one tile deeper into the range
        while (frontier.Count > 0)
        {
            var (cx, cy) = frontier.Dequeue();
            foreach (var (dx, dy) in Directions)
            {
                int nx = cx + dx, ny = cy + dy;
                if (!map.InBounds(nx, ny)) continue;
                if (map[nx, ny].Terrain != Terrain.Mountains) continue;
                if (dist[nx, ny] != -1) continue;

                dist[nx, ny] = dist[cx, cy] + 1;
                frontier.Enqueue((nx, ny));
            }
        }

        // Normalize against ReferenceDepth and perturb with noise to break up rings
        var noise = new Noise(seed ^ 0x4D544E53); // "MTNS"
        var height = new float[map.Width, map.Height];

        for (int y = 0; y < map.Height; y++)
        for (int x = 0; x < map.Width; x++)
        {
            if (dist[x, y] < 0) continue;
            float normalized = Math.Clamp(dist[x, y] / ReferenceDepth, 0f, 1f);
            float perturbation = (noise.Sample(x * NoiseFrequency, y * NoiseFrequency) - 0.5f) * NoisePerturbation;
            height[x, y] = Math.Clamp(normalized + perturbation, 0f, 1f);
        }

        return height;
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
