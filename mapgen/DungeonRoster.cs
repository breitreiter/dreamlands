using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace MapGen;

public record DungeonEntry(string Id, string Name, string Biome, string Decal, string Folder, int TierMin, int TierMax);

public static class DungeonRoster
{
    public static List<DungeonEntry> Load(string contentPath)
    {
        var path = Path.Combine(contentPath, "dungeons_roster.yaml");
        if (!File.Exists(path))
            return new List<DungeonEntry>();

        var yaml = File.ReadAllText(path);
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();

        var doc = deserializer.Deserialize<RosterDocument>(yaml);
        if (doc?.Dungeons == null)
            return new List<DungeonEntry>();

        return doc.Dungeons
            .Select(d => new DungeonEntry(
                d.Id, d.Name, d.Biome, d.Decal, d.Folder,
                d.TierRange.Count >= 1 ? d.TierRange[0] : 1,
                d.TierRange.Count >= 2 ? d.TierRange[1] : 4))
            .ToList();
    }

    class RosterDocument
    {
        public List<DungeonYaml> Dungeons { get; set; } = new();
    }

    class DungeonYaml
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Biome { get; set; } = "";
        public string Decal { get; set; } = "";
        public string Folder { get; set; } = "";
        public List<int> TierRange { get; set; } = new();
    }
}
