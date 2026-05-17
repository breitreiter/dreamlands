namespace Dreamlands.Rules;

/// <summary>Starting stats and balance constants.</summary>
public sealed class CharacterBalance
{
    public static readonly CharacterBalance Default = new();

    public int StartingHealth { get; init; } = 4;
    public int StartingSpirits { get; init; } = 20;
    public int StartingGold { get; init; } = 50;
    public int StartingPackSlots { get; init; } = 8;

    // Ambient resist check difficulty
    public Difficulty AmbientResistDifficulty { get; init; } = Difficulty.Medium;

    // Overworld encounter cadence: next consideration scheduled this many moves after each check
    public int EncounterCadenceMin { get; init; } = 7;
    public int EncounterCadenceMax { get; init; } = 11;

    // Inn tiered services (spirits_economy.md)
    public int InnBedCost { get; init; } = 5;
    public int InnBedSpirits { get; init; } = 5;
    public int InnBathCost { get; init; } = 12;
    public int InnBathSpirits { get; init; } = 10;
    public int InnFullCost { get; init; } = 25;

    // Foraging (binary): success skips the day's ration consumption
    public int ForageDC { get; init; } = 20;
}
