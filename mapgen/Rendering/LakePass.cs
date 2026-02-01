using Dreamlands.Map;
using SkiaSharp;

namespace MapGen.Rendering;

public static class LakePass
{
    private const int TileSize = 128;
    private const int Padding = 16;

    public static void Draw(SKCanvas canvas, Map map)
    {
        string decalPath = Path.Combine("..", "assets", "map", "decals", "lake_1.png");
        using var decal = SKBitmap.Decode(decalPath);
        if (decal == null) return;

        var visited = new bool[map.Width, map.Height];

        for (int y = 0; y < map.Height; y++)
        for (int x = 0; x < map.Width; x++)
        {
            if (visited[x, y] || map[x, y].Terrain != Terrain.Lake) continue;

            var blob = FloodFill(map, x, y, visited);
            DrawBlob(canvas, decal, blob);
        }
    }

    static List<(int x, int y)> FloodFill(Map map, int startX, int startY, bool[,] visited)
    {
        var blob = new List<(int, int)>();
        var stack = new Stack<(int, int)>();
        stack.Push((startX, startY));
        visited[startX, startY] = true;

        while (stack.Count > 0)
        {
            var (cx, cy) = stack.Pop();
            blob.Add((cx, cy));

            foreach (var (dx, dy) in new[] { (0, -1), (0, 1), (-1, 0), (1, 0) })
            {
                int nx = cx + dx, ny = cy + dy;
                if (!map.InBounds(nx, ny) || visited[nx, ny]) continue;
                if (map[nx, ny].Terrain != Terrain.Lake) continue;
                visited[nx, ny] = true;
                stack.Push((nx, ny));
            }
        }

        return blob;
    }

    static void DrawBlob(SKCanvas canvas, SKBitmap decal, List<(int x, int y)> blob)
    {
        int minX = int.MaxValue, minY = int.MaxValue;
        int maxX = int.MinValue, maxY = int.MinValue;

        foreach (var (x, y) in blob)
        {
            if (x < minX) minX = x;
            if (y < minY) minY = y;
            if (x > maxX) maxX = x;
            if (y > maxY) maxY = y;
        }

        float left = minX * TileSize - Padding;
        float top = minY * TileSize - Padding;
        float right = (maxX + 1) * TileSize + Padding;
        float bottom = (maxY + 1) * TileSize + Padding;

        canvas.DrawBitmap(decal, new SKRect(left, top, right, bottom));
    }
}
