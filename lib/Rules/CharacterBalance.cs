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

    public IReadOnlyDictionary<Magnitude, int> SkillBumpMagnitudes { get; init; } = new Dictionary<Magnitude, int>
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

    public IReadOnlyList<SpiritsThreshold> SpiritsThresholds { get; init; } =
    [
        new(15, -1),
        new(10, -2),
        new(5, -4),
        new(0, -10),
    ];

    public int UsesPerLevel { get; init; } = 5;
    public int MaxSkillLevel { get; init; } = 10;

    public IReadOnlyDictionary<Magnitude, int> CostMagnitudes { get; init; } = new Dictionary<Magnitude, int>
    {
        [Magnitude.Trivial] = 5,
        [Magnitude.Small] = 15,
        [Magnitude.Medium] = 40,
        [Magnitude.Large] = 80,
        [Magnitude.Huge] = 200,
    };
}

/// <summary>Spirits penalty threshold: if spirits &lt;= AtOrBelow, apply Penalty to checks.</summary>
public readonly record struct SpiritsThreshold(int AtOrBelow, int Penalty);
