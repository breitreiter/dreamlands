namespace Dreamlands.Rules;

/// <summary>Static definition of a haul (destination-specific trade delivery).</summary>
public sealed partial class HaulDef
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string OriginBiome { get; init; } = "";
    public string DestBiome { get; init; } = "";
    public string OriginFlavor { get; init; } = "";
    public string DeliveryFlavor { get; init; } = "";

    internal static IReadOnlyDictionary<string, HaulDef> All { get; } = BuildAll();

    static Dictionary<string, HaulDef> BuildAll() =>
        Plains().Concat(Mountains()).Concat(Forest()).Concat(Scrub()).Concat(Swamp())
            .ToDictionary(h => h.Id);
}
