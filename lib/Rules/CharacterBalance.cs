namespace Dreamlands.Rules;

/// <summary>Starting stats and balance constants.</summary>
public sealed class CharacterBalance
{
    public static readonly CharacterBalance Default = new();

    public int StartingHealth { get; init; } = 4;
    public int StartingSpirits { get; init; } = 20;
    public int StartingGold { get; init; } = 50;
    public int StartingPackSlots { get; init; } = 3;
    public int StartingHaversackSlots { get; init; } = 10;

    /// <summary>Luck reroll trigger chance by skill level (index = level, 0% for ≤0).</summary>
    public IReadOnlyList<int> LuckRerollChance { get; init; } = [0, 5, 10, 15, 20];

    public int MinSkillLevel { get; init; } = -2;
    public int MaxSkillLevel { get; init; } = 4;

    // Ambient resist check difficulty (single DC for all conditions except exhaustion,
    // which uses ExhaustionBaseDC + ExhaustionDCPerNight*ConsecutiveWildernessNights)
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

    // Exhaustion DC scales with consecutive wilderness nights since last settlement
    public int ExhaustionBaseDC { get; init; } = 12;
    public int ExhaustionDCPerNight { get; init; } = 1;

    // Foraging (binary): success skips the day's ration consumption
    public int ForageDC { get; init; } = 20;
}
