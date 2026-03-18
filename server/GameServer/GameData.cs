using Dreamlands.Encounter;
using Dreamlands.Map;
using Dreamlands.Rules;

namespace GameServer;

/// <summary>
/// Read-only game data singletons. Loaded once at startup.
/// </summary>
public class GameData
{
    public Map Map { get; }
    public EncounterBundle Bundle { get; }
    public BalanceData Balance { get; } = BalanceData.Default;
    public string ApiVersion { get; }
    public bool NoEncounters { get; }
    public bool NoCamp { get; }

    public GameData()
    {
        var mapPath = Environment.GetEnvironmentVariable("DREAMLANDS_MAP");
        var bundlePath = Environment.GetEnvironmentVariable("DREAMLANDS_BUNDLE");

        // In dev: walk up from assembly location to find repo root
        if (mapPath == null || bundlePath == null)
        {
            var repoRoot = FindRepoRoot();
            mapPath ??= Path.Combine(repoRoot, "worlds/production/map.json");
            bundlePath ??= Path.Combine(repoRoot, "worlds/production/encounters.bundle.json");
            ApiVersion = Environment.GetEnvironmentVariable("DREAMLANDS_API_VERSION")
                ?? File.ReadAllText(Path.Combine(repoRoot, "api-version")).Trim();
        }
        else
        {
            // Deployed: resolve relative paths against assembly directory
            var baseDir = Path.GetDirectoryName(typeof(GameData).Assembly.Location)!;
            if (!Path.IsPathRooted(mapPath))
                mapPath = Path.Combine(baseDir, mapPath);
            if (!Path.IsPathRooted(bundlePath))
                bundlePath = Path.Combine(baseDir, bundlePath);

            ApiVersion = Environment.GetEnvironmentVariable("DREAMLANDS_API_VERSION") ?? "1";

            // Also resolve api-version file if it exists next to the data
            var apiVersionFile = Path.Combine(Path.GetDirectoryName(mapPath)!, "api-version");
            if (ApiVersion == "1" && File.Exists(apiVersionFile))
                ApiVersion = File.ReadAllText(apiVersionFile).Trim();
        }

        NoEncounters = Environment.GetEnvironmentVariable("DREAMLANDS_NO_ENCOUNTERS") == "1";
        NoCamp = Environment.GetEnvironmentVariable("DREAMLANDS_NO_CAMP") == "1";

        Map = MapSerializer.Load(mapPath);
        Bundle = EncounterBundle.Load(bundlePath);
    }

    static string FindRepoRoot()
    {
        var dir = Path.GetDirectoryName(typeof(GameData).Assembly.Location)!;
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir, "Dreamlands.sln"))) return dir;
            dir = Path.GetDirectoryName(dir);
        }
        return Directory.GetCurrentDirectory();
    }
}
