using Dreamlands.Encounter;
using Dreamlands.Map;
using Dreamlands.Rules;

namespace GameServer;

/// <summary>
/// Game data singletons. Loaded once at startup; bundle can be hot-reloaded in dev.
/// </summary>
public class GameData
{
    public Map Map { get; }
    public EncounterBundle Bundle { get { lock (_bundleLock) return _bundle; } }
    public CombatBundle? CombatBundle { get; private set; }
    public BalanceData Balance { get; } = BalanceData.Default;
    public string ApiVersion { get; }
    public bool NoEncounters { get; }
    public bool NoCamp { get; }
    public bool IsDev { get; }

    private readonly string _bundlePath;
    private EncounterBundle _bundle;
    private readonly object _bundleLock = new();

    public GameData()
    {
        var mapPath = Environment.GetEnvironmentVariable("DREAMLANDS_MAP");
        var bundlePath = Environment.GetEnvironmentVariable("DREAMLANDS_BUNDLE");

        // In dev: walk up from assembly location to find repo root
        if (mapPath == null || bundlePath == null)
        {
            IsDev = true;
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

        _bundlePath = bundlePath;
        Map = MapSerializer.Load(mapPath);
        _bundle = EncounterBundle.Load(bundlePath);

        // Combat bundle: directory of .fight files. Worlds carry a "combat" dir
        // (mirrored from text/encounters by update-encounters.sh); in dev, fall back
        // to the live text/encounters tree so fight edits only need a reload.
        var combatDir = Environment.GetEnvironmentVariable("DREAMLANDS_COMBAT_DIR");
        if (combatDir == null)
        {
            var bundleDir = Path.GetDirectoryName(bundlePath)!;
            var inWorld = Path.Combine(bundleDir, "combat");
            if (Directory.Exists(inWorld))
                combatDir = inWorld;
            else if (IsDev)
                combatDir = Path.Combine(FindRepoRoot(), "text/encounters");
        }
        _combatDir = combatDir;
        if (combatDir != null && Directory.Exists(combatDir))
            CombatBundle = Dreamlands.Encounter.CombatBundle.LoadDirectory(combatDir);
    }

    private string? _combatDir;

    public void ReloadBundle()
    {
        var fresh = EncounterBundle.Load(_bundlePath);
        lock (_bundleLock) _bundle = fresh;
        if (_combatDir != null && Directory.Exists(_combatDir))
            CombatBundle = Dreamlands.Encounter.CombatBundle.LoadDirectory(_combatDir);
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
