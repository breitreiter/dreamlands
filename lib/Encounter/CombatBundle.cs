namespace Dreamlands.Encounter;

/// <summary>
/// Holds parsed <see cref="CombatEncounter"/>s from a directory of .cmb files. Phase 1
/// uses direct on-disk loading; later phases may add a JSON bundle step matching the
/// .enc bundling pipeline.
///
/// Encounter id = path relative to the root, with the .cmb extension stripped and
/// directory separators normalized to forward slashes (e.g. "plains/tier1/gorzog").
/// </summary>
public sealed class CombatBundle
{
    public IReadOnlyList<CombatEncounter> Encounters { get; }
    readonly Dictionary<string, CombatEncounter> _byId;
    readonly Dictionary<string, List<CombatEncounter>> _byCategory;

    CombatBundle(List<CombatEncounter> encounters)
    {
        Encounters = encounters;
        _byId = encounters.ToDictionary(e => e.Id, StringComparer.OrdinalIgnoreCase);
        _byCategory = encounters
            .GroupBy(e => e.Category, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);
    }

    public CombatEncounter? GetById(string id) =>
        _byId.TryGetValue(id, out var e) ? e : null;

    public IReadOnlyList<CombatEncounter> GetByCategory(string category) =>
        _byCategory.TryGetValue(category, out var list) ? list : Array.Empty<CombatEncounter>();

    public IReadOnlyList<string> GetCategories() => _byCategory.Keys.ToList();

    public static CombatBundle LoadDirectory(string root)
    {
        if (!Directory.Exists(root))
            return new CombatBundle(new List<CombatEncounter>());

        var files = Directory.EnumerateFiles(root, "*.cmb", SearchOption.AllDirectories)
            .OrderBy(p => p, StringComparer.OrdinalIgnoreCase);

        var list = new List<CombatEncounter>();
        foreach (var path in files)
        {
            var enc = CmbParser.ParseFile(path);
            var rel = Path.GetRelativePath(root, path).Replace(Path.DirectorySeparatorChar, '/');
            var idNoExt = rel.EndsWith(".cmb", StringComparison.OrdinalIgnoreCase)
                ? rel[..^4]
                : rel;
            enc.Id = idNoExt;
            int slash = idNoExt.LastIndexOf('/');
            enc.Category = slash >= 0 ? idNoExt[..slash] : "";
            // Tier inference: ".../tierN/..." in the path
            enc.Tier = InferTier(enc.Category);
            list.Add(enc);
        }
        return new CombatBundle(list);
    }

    static int? InferTier(string category)
    {
        foreach (var seg in category.Split('/'))
        {
            if (seg.StartsWith("tier") && int.TryParse(seg[4..], out var n))
                return n;
        }
        return null;
    }
}
