using Dreamlands.Rules;

namespace Dreamlands.Game;

/// <summary>
/// Persisted state for a suspended picker check. Written to PlayerState when
/// EncounterRunner emits AwaitApproach; cleared when Pick resolves the outcome.
/// Mirrors the ActiveCombat pattern so a closed tab can resume mid-picker.
/// </summary>
public class ActivePickerCheck
{
    public string EncounterId { get; set; } = "";
    public int ChoiceIndex { get; set; }
    public Skill Skill { get; set; }
    public string CorrectId { get; set; } = "";
    public string WrongId { get; set; } = "";
    public string? Preamble { get; set; }

    /// <summary>
    /// Null  = no pre-roll needed (Expert or Trained player).
    /// true  = Untrained; pre-roll passed — player sees picker and resolves at Trained tier.
    /// false = Untrained; pre-roll failed — this path is unreachable (runner short-circuited before writing this record).
    /// </summary>
    public bool? PreRollPassed { get; set; }
}
