namespace Dreamlands.Game;

/// <summary>Per-hazard accumulator in <see cref="PlayerState.TravailLedger"/>. JSON-serialized to Cosmos.</summary>
public class TravailTally
{
    /// <summary>Exposure units accrued (steps or nights, per the hazard's unit).</summary>
    public int Units { get; set; }
    /// <summary>Exposure units zeroed by mitigating gear.</summary>
    public int Spared { get; set; }
    /// <summary>Spirits charged so far at threshold crossings.</summary>
    public int SpiritsCharged { get; set; }
}

/// <summary>One hazard channel's line in the end-of-journey summary.</summary>
public record TravailLine(
    string HazardId,
    string Name,
    int Units,
    int Spared,
    int SpiritsLost,
    bool SparedByGear,
    bool EasedBySkill,
    string Text);

/// <summary>End-of-journey travails summary, built and flushed by <see cref="Travails.Summarize"/>.</summary>
public record TravailSummary(List<TravailLine> Lines, int TotalSpiritsLost);
