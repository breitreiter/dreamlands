using Dreamlands.Rules;

namespace Dreamlands.Map;

public static class MapRenderer
{
    // 24-bit RGB colors per terrain
    private static readonly Dictionary<Terrain, (int r, int g, int b)> TerrainRgb = new()
    {
        [Terrain.Lake]      = (0x33, 0x66, 0xCC),
        [Terrain.Plains]    = (0x66, 0xCC, 0x66),
        [Terrain.Forest]    = (0x22, 0x88, 0x22),
        [Terrain.Scrub]     = (0xCC, 0xAA, 0x44),
        [Terrain.Mountains] = (0x88, 0x88, 0x88),
        [Terrain.Swamp]     = (0x66, 0x44, 0x88),
    };

    private const string Reset = "\x1b[0m";

    private static string Fg(int r, int g, int b) => $"\x1b[38;2;{r};{g};{b}m";
    private static string Bg(int r, int g, int b) => $"\x1b[48;2;{r};{g};{b}m";

    private static (int r, int g, int b) GetNodeColor(Map map, Node node)
    {
        if (map.StartingCity == node)
            return (0xFF, 0xFF, 0x00); // Aldgate: yellow

        if (node.Poi != null)
        {
            return node.Poi.Kind switch
            {
                PoiKind.Settlement => (0xFF, 0xFF, 0xFF), // white
                PoiKind.Dungeon    => (0x00, 0x00, 0x00), // black
                _                  => TerrainRgb[node.Terrain],
            };
        }

        return TerrainRgb[node.Terrain];
    }

    public static void Render(Map map, TextWriter? output = null, Node? playerLocation = null, HashSet<Node>? visitedNodes = null)
    {
        output ??= Console.Out;

        // Half-block rendering: each character covers two rows (top ▀ / bottom via bg)
        for (int y = 0; y < map.Height; y += 2)
        {
            for (int x = 0; x < map.Width; x++)
            {
                var topNode = map[x, y];
                var botNode = y + 1 < map.Height ? map[x, y + 1] : null;

                var isVisitedTop = visitedNodes == null || visitedNodes.Contains(topNode);
                var isVisitedBot = botNode != null && (visitedNodes == null || visitedNodes.Contains(botNode));

                // Player marker overrides
                if (playerLocation == topNode)
                {
                    var botColor = botNode != null ? GetNodeColor(map, botNode) : (r: 0, g: 0, b: 0);
                    output.Write($"{Fg(0xFF, 0xFF, 0x00)}{Bg(botColor.r, botColor.g, botColor.b)}☺{Reset}");
                    continue;
                }
                if (playerLocation != null && playerLocation == botNode)
                {
                    var topColor = GetNodeColor(map, topNode);
                    output.Write($"{Fg(topColor.r, topColor.g, topColor.b)}{Bg(0, 0, 0)}▀{Reset}");
                    continue;
                }

                var top = isVisitedTop ? GetNodeColor(map, topNode) : TerrainRgb[topNode.Terrain];
                var bot = botNode != null
                    ? (isVisitedBot ? GetNodeColor(map, botNode) : TerrainRgb[botNode.Terrain])
                    : (r: 0, g: 0, b: 0);

                // ▀ = foreground is top half, background is bottom half
                output.Write($"{Fg(top.r, top.g, top.b)}{Bg(bot.r, bot.g, bot.b)}▀{Reset}");
            }
            output.WriteLine();
        }
    }

    public static void RenderLegend(TextWriter? output = null)
    {
        output ??= Console.Out;
        output.WriteLine("Legend:");

        foreach (var terrain in Enum.GetValues<Terrain>())
        {
            var (r, g, b) = TerrainRgb[terrain];
            output.WriteLine($"  {Bg(r, g, b)}  {Reset} {terrain}");
        }

        output.WriteLine($"  {Bg(0xFF, 0xFF, 0xFF)}  {Reset} Settlement");
        output.WriteLine($"  {Bg(0x00, 0x00, 0x00)}  {Reset} Dungeon");
        output.WriteLine($"  {Bg(0xFF, 0xFF, 0x00)}  {Reset} Aldgate");
    }
}
