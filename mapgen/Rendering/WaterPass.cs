using Dreamlands.Map;
using SkiaSharp;

namespace MapGen.Rendering;

public static class WaterPass
{
    public static void Draw(SKCanvas canvas, int canvasWidth, int canvasHeight, Dictionary<string, SKBitmap> textures)
    {
        using var shader = SKShader.CreateBitmap(
            textures["plains.png"],
            SKShaderTileMode.Repeat,
            SKShaderTileMode.Repeat);
        using var paint = new SKPaint { Shader = shader };
        canvas.DrawRect(0, 0, canvasWidth, canvasHeight, paint);
    }
}
