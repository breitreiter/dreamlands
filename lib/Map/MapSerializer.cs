using System.Text.Json;
using System.Text.Json.Serialization;
using Dreamlands.Rules;

namespace Dreamlands.Map;

public static class MapSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static void Save(Map map, int seed, string path)
    {
        var dto = ToDto(map, seed);
        var json = JsonSerializer.Serialize(dto, Options);
        File.WriteAllText(path, json);
    }

    public static string ToJson(Map map, int seed)
    {
        var dto = ToDto(map, seed);
        return JsonSerializer.Serialize(dto, Options);
    }

    public static Map Load(string path)
    {
        var json = File.ReadAllText(path);
        return FromJson(json);
    }

    public static Map FromJson(string json)
    {
        var dto = JsonSerializer.Deserialize<MapDto>(json, Options)
            ?? throw new InvalidOperationException("Failed to deserialize map JSON");

        var map = new Map(dto.Width, dto.Height);

        // Build region lookup
        var regions = new Dictionary<int, Region>();
        foreach (var r in dto.Regions)
        {
            var terrain = Enum.Parse<Terrain>(r.Terrain);
            var region = new Region(r.Id, terrain) { Name = r.Name, Tier = r.Tier };
            regions[r.Id] = region;
            map.Regions.Add(region);
        }

        // Populate nodes
        foreach (var n in dto.Nodes)
        {
            var node = map[n.X, n.Y];
            node.Terrain = Enum.Parse<Terrain>(n.Terrain);
            node.Description = n.Description;
            node.DistanceFromCity = n.DistanceFromCity ?? int.MaxValue;

            if (n.RegionId.HasValue && regions.TryGetValue(n.RegionId.Value, out var region))
            {
                node.Region = region;
                region.Nodes.Add(node);
            }

            if (n.Poi != null)
            {
                var kind = Enum.Parse<PoiKind>(n.Poi.Kind);
                node.Poi = new Poi(kind, n.Poi.Type)
                {
                    Name = n.Poi.Name,
                    Size = n.Poi.Size != null ? Enum.Parse<SettlementSize>(n.Poi.Size) : null,
                    DungeonId = n.Poi.DungeonId,
                    DecalFile = n.Poi.DecalFile
                };
            }
        }

        // Restore starting city
        if (dto.StartingCity is { Length: 2 })
            map.StartingCity = map[dto.StartingCity[0], dto.StartingCity[1]];

        return map;
    }

    // --- Serialization ---

    private static MapDto ToDto(Map map, int seed)
    {
        return new MapDto
        {
            Seed = seed,
            Width = map.Width,
            Height = map.Height,
            StartingCity = map.StartingCity != null ? new[] { map.StartingCity.X, map.StartingCity.Y } : null,
            Regions = map.Regions.Select(ToDto).ToList(),
            Nodes = map.AllNodes().Select(ToDto).ToList()
        };
    }

    private static RegionDto ToDto(Region region)
    {
        return new RegionDto
        {
            Id = region.Id,
            Terrain = region.Terrain.ToString(),
            Name = region.Name,
            Tier = region.Tier,
            Size = region.Size
        };
    }

    private static NodeDto ToDto(Node node)
    {
        return new NodeDto
        {
            X = node.X,
            Y = node.Y,
            Terrain = node.Terrain.ToString(),
            RegionId = node.Region?.Id,
            Description = node.Description,
            Poi = node.Poi != null ? ToDto(node.Poi) : null,
            DistanceFromCity = node.DistanceFromCity < int.MaxValue ? node.DistanceFromCity : null
        };
    }

    private static PoiDto ToDto(Poi poi)
    {
        return new PoiDto
        {
            Kind = poi.Kind.ToString(),
            Type = poi.Type,
            Name = poi.Name,
            Size = poi.Size?.ToString(),
            DungeonId = poi.DungeonId,
            DecalFile = poi.DecalFile
        };
    }

    // --- DTOs ---

    internal record MapDto
    {
        public int Seed { get; init; }
        public int Width { get; init; }
        public int Height { get; init; }
        public int[]? StartingCity { get; init; }
        public List<RegionDto> Regions { get; init; } = new();
        public List<NodeDto> Nodes { get; init; } = new();
    }

    internal record RegionDto
    {
        public int Id { get; init; }
        public string Terrain { get; init; } = "";
        public string? Name { get; init; }
        public int Tier { get; init; }
        public int Size { get; init; }
    }

    internal record NodeDto
    {
        public int X { get; init; }
        public int Y { get; init; }
        public string Terrain { get; init; } = "";
        public int? RegionId { get; init; }
        public string? Description { get; init; }
        public PoiDto? Poi { get; init; }
        public int? DistanceFromCity { get; init; }
    }

    internal record PoiDto
    {
        public string Kind { get; init; } = "";
        public string Type { get; init; } = "";
        public string? Name { get; init; }
        public string? Size { get; init; }
        public string? DungeonId { get; init; }
        public string? DecalFile { get; init; }
    }
}
