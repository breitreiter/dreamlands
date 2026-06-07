namespace Dreamlands.Rules;

public enum HazardUnit { Steps, Nights }

/// <summary>
/// One deterministic travel-hazard channel (travails system — replaced travel conditions).
/// Exposure accrues per unit walked or camped; spirits charge at threshold crossings.
/// </summary>
public sealed class HazardDef
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    /// <summary>Biome whose steps accrue this hazard. Null = universal per-night channel.</summary>
    public string? Biome { get; init; }
    public HazardUnit Unit { get; init; }
    /// <summary>Carrying this item zeroes the channel ("spared" line in the summary).</summary>
    public string MitigatingItemId { get; init; } = "";
    /// <summary>Units per 1 spirit lost, indexed by Bushcraft <see cref="SkillTier"/>.</summary>
    public IReadOnlyList<int> UnitsPerSpirit { get; init; } = [];

    // Summary flavor fragments
    /// <summary>"lost 2 spirits to {CauseNoun}".</summary>
    public string CauseNoun { get; init; } = "";
    /// <summary>Step channels only: "{ExposurePhrase} for half a day, ...". Capitalized.</summary>
    public string ExposurePhrase { get; init; } = "";
    /// <summary>Full sentence shown when gear zeroed the channel.</summary>
    public string SparedPhrase { get; init; } = "";

    internal static IReadOnlyDictionary<string, HazardDef> All { get; } = BuildAll();

    static Dictionary<string, HazardDef> BuildAll() => new()
    {
        ["thirst"] = new()
        {
            Id = "thirst", Name = "Thirst", Biome = "scrub", Unit = HazardUnit.Steps,
            MitigatingItemId = "waterskin",
            UnitsPerSpirit = [2, 4, 8],
            CauseNoun = "thirst",
            ExposurePhrase = "Walked the dry scrubland",
            SparedPhrase = "Your waterskin kept the thirst at bay.",
        },
        ["cold"] = new()
        {
            Id = "cold", Name = "Cold", Biome = "mountains", Unit = HazardUnit.Steps,
            MitigatingItemId = "sleeping_kit",
            UnitsPerSpirit = [2, 4, 8],
            CauseNoun = "cold",
            ExposurePhrase = "Crossed the freezing heights",
            SparedPhrase = "Your bedroll kept the cold out.",
        },
        ["fatigue"] = new()
        {
            Id = "fatigue", Name = "Fatigue", Biome = null, Unit = HazardUnit.Nights,
            MitigatingItemId = "scarecrow_boots",
            UnitsPerSpirit = [1, 2, 4],
            CauseNoun = "fatigue",
            SparedPhrase = "Your scarecrow boots carried you tireless.",
        },
    };
}
