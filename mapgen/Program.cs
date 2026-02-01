using Dreamlands.Map;
using MapGen.Rendering;

namespace MapGen;

public class Program
{
    public static void Main(string[] args)
    {
        int width = 60;
        int height = 20;
        int? seed = null;
        bool animate = false;
        bool showRegions = false;
        bool save = false;
        bool png = false;
        var positionalArgs = new List<string>();
        foreach (var arg in args)
        {
            if (arg == "--animate" || arg == "-a")
                animate = true;
            else if (arg == "--regions" || arg == "-r")
                showRegions = true;
            else if (arg == "--save" || arg == "-s")
                save = true;
            else if (arg == "--png")
                png = true;
            else
                positionalArgs.Add(arg);
        }

        // Parse optional args: width height [seed]
        if (positionalArgs.Count >= 2)
        {
            width = int.Parse(positionalArgs[0]);
            height = int.Parse(positionalArgs[1]);
        }
        if (positionalArgs.Count >= 3)
        {
            seed = int.Parse(positionalArgs[2]);
        }

        Action<Map>? onCycle = animate ? RenderAnimationFrame : null;

        Console.Error.WriteLine($"Generating {width}x{height} map...");
        var (map, actualSeed) = MapGenerator.Generate(width, height, seed, onCycle);
        Console.Error.WriteLine($"Seed: {actualSeed}");

        // Populate content (region names, descriptions, settlements)
        var content = new ContentLoader();
        var contentRng = new Random(actualSeed);
        ContentPopulator.Populate(map, content, contentRng);

        if (animate)
            Console.Clear();

        MapRenderer.Render(map);
        Console.WriteLine();
        MapRenderer.RenderLegend();

        // Show POI counts
        var settlements = map.AllNodes().Where(n => n.Poi?.Kind == PoiKind.Settlement).ToList();
        if (settlements.Count > 0)
            Console.WriteLine($"Settlements: {settlements.Count}");
        var dungeonCount = map.AllNodes().Count(n => n.Poi?.Kind == PoiKind.Dungeon);
        if (dungeonCount > 0)
            Console.WriteLine($"Dungeons: {dungeonCount}");

        if (showRegions)
        {
            Console.WriteLine();
            Console.WriteLine($"Regions: {map.Regions.Count} total");
            foreach (var group in map.Regions.GroupBy(r => r.Terrain).OrderByDescending(g => g.Sum(r => r.Size)))
            {
                var regions = group.OrderByDescending(r => r.Size).ToList();
                var regionNames = regions.Select(r => r.Name != null ? $"{r.Name} ({r.Size})" : r.Size.ToString());
                Console.WriteLine($"  {group.Key}: {regions.Count} region(s) - {string.Join(", ", regionNames)}");
            }
        }

        if (save)
        {
            var filename = $"map_{actualSeed}.json";
            MapSerializer.Save(map, actualSeed, filename);
            Console.WriteLine($"Saved: {filename}");
        }

        if (png)
        {
            if (!save)
            {
                var jsonFile = $"map_{actualSeed}.json";
                MapSerializer.Save(map, actualSeed, jsonFile);
                Console.WriteLine($"Saved: {jsonFile}");
            }

            var filename = $"map_{actualSeed}.png";
            ImageRenderer.Render(map, filename, actualSeed);
            Console.WriteLine($"PNG saved: {filename}");
        }
    }

    private static void RenderAnimationFrame(Map map)
    {
        Console.SetCursorPosition(0, 0);
        MapRenderer.Render(map);
        Thread.Sleep(50);
    }
}
