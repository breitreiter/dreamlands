using Dreamlands.Map;

namespace MapGen;

public class ContentLoader
{
    public string ContentPath => _contentPath;
    private readonly string _contentPath;
    private readonly Dictionary<Terrain, List<string>> _names = new();
    private readonly Dictionary<Terrain, List<string>> _descriptions = new();
    private readonly Dictionary<Terrain, List<ContentEntry>> _encounters = new();
    private readonly Dictionary<PoiKind, List<ContentEntry>> _poiTypes = new();

    public ContentLoader(string? contentPath = null)
    {
        _contentPath = contentPath ?? Path.Combine(AppContext.BaseDirectory, "content");
        LoadAllContent();
    }

    private void LoadAllContent()
    {
        foreach (var terrain in Enum.GetValues<Terrain>())
        {
            _names[terrain] = LoadFile(Path.Combine("names", $"{terrain.ToString().ToLower()}.txt"));
            _descriptions[terrain] = LoadFile(Path.Combine("descriptions", $"{terrain.ToString().ToLower()}.txt"));

            if (terrain != Terrain.Lake)
                _encounters[terrain] = LoadContentFile(Path.Combine("encounters", $"{terrain.ToString().ToLower()}.txt"));
        }

        _poiTypes[PoiKind.Settlement] = LoadContentFile("settlements.txt");
        _poiTypes[PoiKind.Dungeon] = LoadContentFile("dungeons.txt");
        _poiTypes[PoiKind.Landmark] = LoadContentFile("landmarks.txt");
        _poiTypes[PoiKind.WaterSource] = LoadContentFile("watersources.txt");
    }

    private List<string> LoadFile(string relativePath)
    {
        var fullPath = Path.Combine(_contentPath, relativePath);
        if (!File.Exists(fullPath))
            return new List<string>();

        return File.ReadAllLines(fullPath)
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrEmpty(line) && !line.StartsWith('#'))
            .ToList();
    }

    private List<ContentEntry> LoadContentFile(string relativePath)
    {
        var fullPath = Path.Combine(_contentPath, relativePath);
        if (!File.Exists(fullPath))
            return new List<ContentEntry>();

        var entries = new List<ContentEntry>();
        foreach (var rawLine in File.ReadAllLines(fullPath))
        {
            var line = rawLine.Trim();
            if (string.IsNullOrEmpty(line) || line.StartsWith('#'))
                continue;

            entries.Add(ParseContentEntry(line));
        }
        return entries;
    }

    private static ContentEntry ParseContentEntry(string line)
    {
        var colonIndex = line.IndexOf(':');
        if (colonIndex > 0)
        {
            var prefix = line[..colonIndex];
            var dashIndex = prefix.IndexOf('-');
            if (dashIndex > 0 &&
                int.TryParse(prefix[..dashIndex], out int min) &&
                int.TryParse(prefix[(dashIndex + 1)..], out int max))
            {
                return new ContentEntry(line[(colonIndex + 1)..].Trim(), min, max);
            }
        }
        return new ContentEntry(line, 0, 999);
    }

    public string? GetRandomName(Terrain terrain, Random rng)
    {
        var list = _names.GetValueOrDefault(terrain);
        return list is { Count: > 0 } ? list[rng.Next(list.Count)] : null;
    }

    public string? GetRandomDescription(Terrain terrain, Random rng)
    {
        var list = _descriptions.GetValueOrDefault(terrain);
        return list is { Count: > 0 } ? list[rng.Next(list.Count)] : null;
    }

    public string? GetRandomPoiType(PoiKind kind, Random rng) =>
        GetPoiTypeAtDistance(kind, 0, rng);

    public string? GetPoiTypeAtDistance(PoiKind kind, int distance, Random rng)
    {
        var list = _poiTypes.GetValueOrDefault(kind);
        if (list == null || list.Count == 0)
            return null;

        var candidates = list
            .Where(e => distance >= e.MinDistance && distance <= e.MaxDistance)
            .ToList();

        return candidates.Count > 0 ? candidates[rng.Next(candidates.Count)].Text : null;
    }

    public Encounter? GetRandomEncounter(Terrain terrain, Random rng) =>
        GetEncounterAtDistance(terrain, 0, rng);

    public Encounter? GetEncounterAtDistance(Terrain terrain, int distance, Random rng)
    {
        var list = _encounters.GetValueOrDefault(terrain);
        if (list == null || list.Count == 0)
            return null;

        var candidates = list
            .Where(e => distance >= e.MinDistance && distance <= e.MaxDistance)
            .ToList();

        return candidates.Count > 0 ? new Encounter(candidates[rng.Next(candidates.Count)].Text, terrain) : null;
    }

    public bool HasNames(Terrain terrain) => _names.GetValueOrDefault(terrain)?.Count > 0;
    public bool HasDescriptions(Terrain terrain) => _descriptions.GetValueOrDefault(terrain)?.Count > 0;
    public bool HasPoiTypes(PoiKind kind) => _poiTypes.GetValueOrDefault(kind)?.Count > 0;
    public bool HasEncounters(Terrain terrain) => _encounters.GetValueOrDefault(terrain)?.Count > 0;
}
