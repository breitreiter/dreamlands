namespace Dreamlands.Rules;

public enum ConditionSeverity { Minor, Severe }

/// <summary>
/// Definition of a status condition. All remaining conditions are encounter-acquired;
/// ambient travel hazards were replaced by the deterministic travails system
/// (<see cref="HazardDef"/>, plans/travel_travails.md).
/// </summary>
public sealed class ConditionDef
{
    public ConditionSeverity Severity { get; init; } = ConditionSeverity.Minor;
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public int Stacks { get; init; } = 1;
    public string? SpecialEffect { get; init; }

    internal static IReadOnlyDictionary<string, ConditionDef> All { get; } = BuildAll();

    static Dictionary<string, ConditionDef> BuildAll() => new()
    {
        ["irradiated"] = new()
        {
            Id = "irradiated", Name = "Irradiated",
            Stacks = 3, Severity = ConditionSeverity.Severe,
        },
        ["lattice_sickness"] = new()
        {
            Id = "lattice_sickness", Name = "Lattice Sickness",
            Stacks = 3, Severity = ConditionSeverity.Severe,
        },
        ["poisoned"] = new()
        {
            Id = "poisoned", Name = "Poisoned",
            Stacks = 3, Severity = ConditionSeverity.Severe,
        },
        ["injured"] = new()
        {
            Id = "injured", Name = "Injured",
            Stacks = 3, Severity = ConditionSeverity.Severe,
        },
    };
}
