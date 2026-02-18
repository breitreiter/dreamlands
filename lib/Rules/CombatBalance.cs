namespace Dreamlands.Rules;

/// <summary>Enemy tier stats.</summary>
public readonly record struct EnemyTier(int TypicalHp, int CombatTarget, int Damage);

/// <summary>Combat balance data.</summary>
public sealed class CombatBalance
{
    public static readonly CombatBalance Default = new();

    public int StartingHealth { get; init; }
    public int MinorInjuryDamage { get; init; }
    public int ModerateInjuryDamage { get; init; }
    public int SevereInjuryDamage { get; init; }
    public int DefeatDamage { get; init; }
    public string DefeatCondition { get; init; } = "injured";
    public IReadOnlyDictionary<int, EnemyTier> EnemyTiers { get; init; } = new Dictionary<int, EnemyTier>();
    public int MinorTerror { get; init; }
    public int ModerateTerror { get; init; }
    public int MajorRevelation { get; init; }
    public int WitnessTheUnspeakable { get; init; }
}
