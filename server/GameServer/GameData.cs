using Dreamlands.Encounter;
using Dreamlands.Map;
using Dreamlands.Rules;

namespace GameServer;

/// <summary>
/// Read-only game data singletons. Loaded once at startup.
/// Bundle can be hot-reloaded via ReloadBundle().
/// </summary>
public class GameData
{
    public Map Map { get; }
    public BalanceData Balance { get; } = BalanceData.Default;
    public string ApiVersion { get; }
    public bool NoEncounters { get; }
    public bool NoCamp { get; }

    private readonly string _bundlePath;
    private EncounterBundle _bundle;
    private readonly object _bundleLock = new();
    public EncounterBundle Bundle { get { lock (_bundleLock) return _bundle; } }

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
        _bundlePath = bundlePath;
        _bundle = EncounterBundle.Load(_bundlePath);
    }

    public void ReloadBundle()
    {
        var fresh = EncounterBundle.Load(_bundlePath);
        lock (_bundleLock) _bundle = fresh;
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
