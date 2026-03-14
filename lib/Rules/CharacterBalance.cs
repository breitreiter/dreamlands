namespace Dreamlands.Rules;

/// <summary>Magnitude-to-integer tables and starting stats.</summary>
public sealed class CharacterBalance
{
    public static readonly CharacterBalance Default = new();

    public IReadOnlyDictionary<Magnitude, int> DamageMagnitudes { get; init; } = new Dictionary<Magnitude, int>
    {
        [Magnitude.Trivial] = 1,
        [Magnitude.Small] = 2,
        [Magnitude.Medium] = 3,
        [Magnitude.Large] = 4,
        [Magnitude.Huge] = 5,
    };

    public int StartingHealth { get; init; } = 20;
    public int StartingSpirits { get; init; } = 20;
    public int StartingGold { get; init; } = 50;
    public int StartingPackSlots { get; init; } = 3;
    public int StartingHaversackSlots { get; init; } = 20;

    public int SpiritDisadvantageThreshold { get; init; } = 10;

    /// <summary>Luck reroll trigger chance by skill level (index = level, 0% for ≤0).</summary>
    public IReadOnlyList<int> LuckRerollChance { get; init; } = [0, 5, 10, 15, 15];

    public int MinSkillLevel { get; init; } = -2;
    public int MaxSkillLevel { get; init; } = 4;

    // Ambient resist check difficulty (single DC for all conditions)
    public Difficulty AmbientResistDifficulty { get; init; } = Difficulty.Medium;

    // Foraging DC thresholds (multi-tier: beat DC1 = 1 food, DC2 = 2, DC3 = 3)
    public int ForageDC1 { get; init; } = 16;
    public int ForageDC2 { get; init; } = 18;
    public int ForageDC3 { get; init; } = 20;

    // End-of-day rest recovery (before meal bonuses)
    public int BaseRestHealth { get; init; } = 1;
    public int BaseRestSpirits { get; init; } = 1;

    // Balanced meal bonus (1 protein + 1 grain + 1 sweet)
    public int BalancedMealHealthBonus { get; init; } = 1;
    public int BalancedMealSpiritsBonus { get; init; } = 1;

    // Overworld encounter trigger chance (0.0–1.0) per eligible tile (~3 moves/day × 0.10 ≈ 30%/day)
    public double EncounterChance { get; init; } = 0.10;

    // Inn pricing: cost per night for multi-night stays (3x trivial food cost)
    public int InnNightlyCost { get; init; } = 9;

    /// <summary>
    /// Condition resist bonus from gear ResistModifiers. Code enforces per-slot caps:
    /// big gear (weapon/armor/boots/consumable) up to +5, small gear (tools) up to +3.
    /// </summary>
    public IReadOnlyDictionary<Magnitude, int> ResistBonusMagnitudes { get; init; } = new Dictionary<Magnitude, int>
    {
        [Magnitude.Trivial] = 1,
        [Magnitude.Small] = 2,
        [Magnitude.Medium] = 3,
        [Magnitude.Large] = 4,
        [Magnitude.Huge] = 5,
    };

    public IReadOnlyDictionary<Magnitude, int> CostMagnitudes { get; init; } = new Dictionary<Magnitude, int>
    {
        [Magnitude.Trivial] = 3,
        [Magnitude.Small] = 15,
        [Magnitude.Medium] = 40,
        [Magnitude.Large] = 80,
        [Magnitude.Huge] = 200,
    };
}
