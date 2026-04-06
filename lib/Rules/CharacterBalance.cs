namespace Dreamlands.Rules;

/// <summary>Starting stats and balance constants.</summary>
public sealed class CharacterBalance
{
    public static readonly CharacterBalance Default = new();

    public int StartingHealth { get; init; } = 4;
    public int StartingSpirits { get; init; } = 20;
    public int StartingGold { get; init; } = 50;
    public int StartingPackSlots { get; init; } = 3;
    public int StartingHaversackSlots { get; init; } = 20;

    public int SpiritDisadvantageThreshold { get; init; } = 10;

    /// <summary>Luck reroll trigger chance by skill level (index = level, 0% for ≤0).</summary>
    public IReadOnlyList<int> LuckRerollChance { get; init; } = [0, 5, 10, 15, 20];

    public int MinSkillLevel { get; init; } = -2;
    public int MaxSkillLevel { get; init; } = 4;

    // Ambient resist check difficulty (single DC for all conditions)
    public Difficulty AmbientResistDifficulty { get; init; } = Difficulty.Medium;

    // Foraging DC thresholds (multi-tier: beat DC1 = 1 food, DC2 = 2, DC3 = 3)
    public int ForageDC1 { get; init; } = 16;
    public int ForageDC2 { get; init; } = 18;
    public int ForageDC3 { get; init; } = 20;

    // End-of-day rest recovery (spirits only — health only recovers at inn/chapterhouse)
    public int BaseRestSpirits { get; init; } = 1;

    // Balanced meal bonus (1 protein + 1 grain + 1 sweet)
    public int BalancedMealSpiritsBonus { get; init; } = 1;

    // Overworld encounter cadence: next consideration scheduled this many moves after each check
    public int EncounterCadenceMin { get; init; } = 7;
    public int EncounterCadenceMax { get; init; } = 11;

    // Inn pricing: cost per night for multi-night stays (3x trivial food cost)
    public int InnNightlyCost { get; init; } = 9;
}
