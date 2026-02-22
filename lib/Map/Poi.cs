using Dreamlands.Rules;

namespace Dreamlands.Map;

public class Poi
{
    public PoiKind Kind { get; }
    public string Type { get; }
    public string? Name { get; set; }
    public SettlementSize? Size { get; set; }
    public string? DungeonId { get; set; }
    public string? DecalFile { get; set; }

    public Poi(PoiKind kind, string type)
    {
        Kind = kind;
        Type = type;
    }
}
