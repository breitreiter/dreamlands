using Dreamlands.Rules;

namespace Dreamlands.Map;

public static class MapRenderer
{
    private static readonly Dictionary<Terrain, string> TerrainColors = new()
    {
        [Terrain.Lake] = "\x1b[44m",       // Blue background
        [Terrain.Plains] = "\x1b[102m",    // Bright green background
        [Terrain.Forest] = "\x1b[42m",     // Green background
        [Terrain.Scrub] = "\x1b[43m",      // Yellow background
        [Terrain.Mountains] = "\x1b[100m", // Bright black (gray) background
        [Terrain.Swamp] = "\x1b[45m"       // Magenta background
    };

    private static readonly Dictionary<PoiKind, char> PoiGlyphs = new()
    {
        [PoiKind.Settlement] = 'S',
        [PoiKind.Dungeon] = 'D',
        [PoiKind.Landmark] = 'L',
        [PoiKind.Encounter] = 'E',
    };

    private const string Reset = "\x1b[0m";
    private const string BlackFg = "\x1b[30m";
    private const string BlackBg = "\x1b[40m";
    private const string WhiteFg = "\x1b[97m";
    private const string YellowFg = "\x1b[33;1m";

    public static void Render(Map map, TextWriter? output = null, Node? playerLocation = null, HashSet<Node>? visitedNodes = null)
    {
        output ??= Console.Out;

        for (int y = 0; y < map.Height; y++)
        {
            for (int x = 0; x < map.Width; x++)
            {
                var node = map[x, y];
                var isStartingCity = map.StartingCity == node;
                var isPlayer = playerLocation == node;
                var isVisited = visitedNodes == null || visitedNodes.Contains(node);

                if (isPlayer)
                {
                    output.Write($"{BlackBg}{YellowFg}☺{Reset}");
                    continue;
                }

                var bg = TerrainColors[node.Terrain];

                // Unvisited: just terrain color, no details
                if (!isVisited)
                {
                    output.Write($"{bg} {Reset}");
                    continue;
                }

                var fg = node.HasRiver ? WhiteFg : BlackFg;
                if (isStartingCity)
                {
                    fg = YellowFg;
                    bg = BlackBg;
                }

                var ch = node.Poi != null && PoiGlyphs.TryGetValue(node.Poi.Kind, out var glyph) ? glyph : ' ';

                output.Write($"{bg}{fg}{ch}{Reset}");
            }
            output.WriteLine();
        }
    }

    public static void RenderLegend(TextWriter? output = null)
    {
        output ??= Console.Out;
        output.WriteLine("Terrain Legend:");

        foreach (var terrain in Enum.GetValues<Terrain>())
        {
            var bg = TerrainColors[terrain];
            output.WriteLine($"  {bg}{BlackFg} {terrain,-10} {Reset}");
        }
    }
}
