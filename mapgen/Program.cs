using Dreamlands.Map;
using MapGen.Rendering;
using SkiaSharp;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace MapGen;

public class Program
{
    static readonly string RepoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
    static readonly string WorldsYamlPath = Path.Combine(RepoRoot, "worlds", "worlds.yaml");

    public static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            PrintUsage();
            return;
        }

        var command = args[0];
        switch (command)
        {
            case "generate":
                HandleGenerate(args[1..]);
                break;
            case "list":
                HandleList();
                break;
            default:
                // Legacy mode: positional args (width height [seed])
                if (int.TryParse(command, out _))
                {
                    Console.Error.WriteLine("Warning: legacy positional mode — output writes to CWD, not a world folder.");
                    HandleLegacy(args);
                }
                else
                {
                    Console.Error.WriteLine($"Unknown command: {command}");
                    PrintUsage();
                }
                break;
        }
    }

    static void PrintUsage()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("  mapgen generate <world-name>   Generate (or regenerate) a named world");
        Console.WriteLine("  mapgen generate --all          Regenerate all worlds in registry");
        Console.WriteLine("  mapgen list                    Show registered worlds and status");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --animate, -a    Show generation animation");
        Console.WriteLine("  --regions, -r    Show region breakdown");
        Console.WriteLine();
        Console.WriteLine("Legacy:");
        Console.WriteLine("  mapgen <width> <height> [seed]   Ad-hoc generation (writes to CWD)");
    }

    static WorldRegistry LoadRegistry()
    {
        if (!File.Exists(WorldsYamlPath))
        {
            Console.Error.WriteLine($"worlds.yaml not found at {WorldsYamlPath}");
            Environment.Exit(1);
        }

        var yaml = File.ReadAllText(WorldsYamlPath);
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();

        return deserializer.Deserialize<WorldRegistry>(yaml);
    }

    static void SaveRegistry(WorldRegistry registry)
    {
        var serializer = new SerializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
            .Build();

        File.WriteAllText(WorldsYamlPath, serializer.Serialize(registry));
    }

    static void HandleGenerate(string[] args)
    {
        bool all = false;
        bool animate = false;
        bool showRegions = false;
        string? worldName = null;

        foreach (var arg in args)
        {
            switch (arg)
            {
                case "--all": all = true; break;
                case "--animate" or "-a": animate = true; break;
                case "--regions" or "-r": showRegions = true; break;
                default: worldName = arg; break;
            }
        }

        if (!all && worldName == null)
        {
            Console.Error.WriteLine("Usage: mapgen generate <world-name> | --all");
            return;
        }

        var registry = LoadRegistry();

        if (all)
        {
            foreach (var name in registry.Worlds.Keys)
                GenerateWorld(name, registry, animate, showRegions);
        }
        else
        {
            GenerateWorld(worldName!, registry, animate, showRegions);
        }
    }

    static void GenerateWorld(string name, WorldRegistry registry, bool animate, bool showRegions)
    {
        if (!registry.Worlds.TryGetValue(name, out var config))
        {
            Console.Error.WriteLine($"Unknown world: '{name}'. Run 'mapgen list' to see registered worlds.");
            return;
        }

        Console.Error.WriteLine($"=== Generating world '{name}' ({config.Width}x{config.Height}) ===");

        Action<Map>? onCycle = animate ? RenderAnimationFrame : null;

        var (map, actualSeed) = MapGenerator.Generate(config.Width, config.Height, config.Seed, onCycle);
        Console.Error.WriteLine($"Seed: {actualSeed}");

        // Persist seed if it was null (first generation)
        if (config.Seed == null)
        {
            config.Seed = actualSeed;
            SaveRegistry(registry);
            Console.Error.WriteLine($"Seed {actualSeed} recorded in worlds.yaml");
        }

        var contentPath = Path.Combine(RepoRoot, "text", "notes", "content");
        var contentRng = new Random(actualSeed);
        ContentPopulator.Populate(map, contentPath, contentRng);

        if (animate)
            Console.Clear();

        // Console preview
        MapRenderer.Render(map);
        Console.WriteLine();
        MapRenderer.RenderLegend();

        // Write output to worlds/<name>/
        var worldDir = Path.Combine(RepoRoot, "worlds", name);
        Directory.CreateDirectory(worldDir);

        // map.json
        var jsonPath = Path.Combine(worldDir, "map.json");
        MapSerializer.Save(map, actualSeed, jsonPath);
        Console.Error.WriteLine($"  map.json -> {jsonPath}");

        // map.png + tiles — render passes use relative paths from the mapgen source dir
        var mapgenDir = Path.Combine(RepoRoot, "mapgen");
        var prevDir = Environment.CurrentDirectory;
        Environment.CurrentDirectory = mapgenDir;
        using var image = ImageRenderer.Render(map, actualSeed);
        Environment.CurrentDirectory = prevDir;

        var pngPath = Path.Combine(worldDir, "map.png");
        Console.Error.WriteLine("  Encoding PNG...");
        using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
        using (var stream = File.Create(pngPath))
            data.SaveTo(stream);
        Console.Error.WriteLine($"  map.png -> {pngPath}");

        var tilesDir = Path.Combine(worldDir, "tiles");
        Console.Error.WriteLine("  Slicing tiles...");
        TileSlicer.Slice(image, tilesDir);
        Console.Error.WriteLine($"  tiles -> {tilesDir}/");

        // Create placeholder directories
        Directory.CreateDirectory(Path.Combine(worldDir, "encounters"));
        Directory.CreateDirectory(Path.Combine(worldDir, "assets"));

        // Summary
        PrintSummary(map, actualSeed, showRegions);
        Console.WriteLine();
    }

    static void HandleList()
    {
        var registry = LoadRegistry();

        Console.WriteLine("Registered worlds:");
        Console.WriteLine();

        foreach (var (name, config) in registry.Worlds)
        {
            var seedStr = config.Seed.HasValue ? config.Seed.Value.ToString() : "(not yet generated)";
            var worldDir = Path.Combine(RepoRoot, "worlds", name);
            var hasMap = File.Exists(Path.Combine(worldDir, "map.json"));
            var status = hasMap ? "generated" : "pending";

            Console.WriteLine($"  {name,-20} {config.Width}x{config.Height}  seed: {seedStr,-16} [{status}]");
        }
    }

    static void HandleLegacy(string[] args)
    {
        int width = 60;
        int height = 20;
        int? seed = null;
        bool animate = false;
        bool showRegions = false;
        var positionalArgs = new List<string>();

        for (int i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (arg is "--animate" or "-a")
                animate = true;
            else if (arg is "--regions" or "-r")
                showRegions = true;
            else
                positionalArgs.Add(arg);
        }

        if (positionalArgs.Count >= 2)
        {
            width = int.Parse(positionalArgs[0]);
            height = int.Parse(positionalArgs[1]);
        }
        if (positionalArgs.Count >= 3)
            seed = int.Parse(positionalArgs[2]);

        Action<Map>? onCycle = animate ? RenderAnimationFrame : null;

        Console.Error.WriteLine($"Generating {width}x{height} map...");
        var (map, actualSeed) = MapGenerator.Generate(width, height, seed, onCycle);
        Console.Error.WriteLine($"Seed: {actualSeed}");

        var contentPath = Path.Combine(RepoRoot, "text", "notes", "content");
        var contentRng = new Random(actualSeed);
        ContentPopulator.Populate(map, contentPath, contentRng);

        if (animate)
            Console.Clear();

        MapRenderer.Render(map);
        Console.WriteLine();
        MapRenderer.RenderLegend();
        PrintSummary(map, actualSeed, showRegions);
    }

    static void PrintSummary(Map map, int seed, bool showRegions)
    {
        var settlements = map.AllNodes().Where(n => n.Poi?.Kind == PoiKind.Settlement).ToList();
        if (settlements.Count > 0)
            Console.WriteLine($"Settlements: {settlements.Count}");

        var dungeonCount = map.AllNodes().Count(n => n.Poi?.Kind == PoiKind.Dungeon);
        if (dungeonCount > 0)
            Console.WriteLine($"Dungeons: {dungeonCount}");

        var encounterCount = map.AllNodes().Count(n => n.Poi?.Kind == PoiKind.Encounter);
        if (encounterCount > 0)
            Console.WriteLine($"Encounters: {encounterCount}");

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
    }

    private static void RenderAnimationFrame(Map map)
    {
        Console.SetCursorPosition(0, 0);
        MapRenderer.Render(map);
        Thread.Sleep(50);
    }
}

public class WorldRegistry
{
    public Dictionary<string, WorldConfig> Worlds { get; set; } = new();
}

public class WorldConfig
{
    public int Width { get; set; }
    public int Height { get; set; }
    public int? Seed { get; set; }
}
