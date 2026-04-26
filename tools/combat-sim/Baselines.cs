using Dreamlands.Encounter;

namespace CombatSim;

/// <summary>Locked monster baselines per project/design/combat_pivot.md "Default monster per tier".</summary>
public sealed record MonsterBaseline(
    int Tier, int Hp, int Ac, int ToHit,
    DiceRoll BasicDamage, DiceRoll HeavyDamage, int HeavyTimer);

/// <summary>Locked PC profile per the same doc, parameterized by tier and match condition.
/// Damage dice are owned by each <see cref="WeaponPolicy"/>, not by the PC profile.</summary>
public sealed record PcProfile(
    string Label,
    int AttackBonus, int DamageBonus,
    int Bushcraft, int Cunning, int BaseAc,
    bool ReadsIntent);

public static class Baselines
{
    // Heavy timer 3 = the cooldown counts 3→2→1→0; the heavy actually fires on round 4
    // of the combat (matching CombatRunner cooldown semantics).
    public static readonly MonsterBaseline T1 = new(1, 24, 11, 4, new(1,4,0), new(2,8,0),  3);
    public static readonly MonsterBaseline T2 = new(2, 30, 11, 5, new(1,4,0), new(2,8,0),  3);
    public static readonly MonsterBaseline T3 = new(3, 40, 12, 5, new(1,4,0), new(2,10,0), 3);

    public static MonsterBaseline ForTier(int tier) => tier switch
    {
        1 => T1, 2 => T2, 3 => T3,
        _ => throw new ArgumentOutOfRangeException(nameof(tier))
    };

    /// <summary>"Even-match PC" per the doc: Combat +(2+T), tier-N gear, plays stance to intent.</summary>
    public static PcProfile EvenMatch(int tier) => new(
        Label: $"T{tier} even-match",
        AttackBonus: 2 + tier,
        DamageBonus: tier,
        Bushcraft: 2,
        Cunning: 2,
        BaseAc: ExpectedArmorAc(tier),
        ReadsIntent: true);

    /// <summary>"Overmatched PC" per the doc: Combat +0 (sim uses +1 to match table), tier-1 gear.</summary>
    public static PcProfile Overmatched(int tier) => new(
        Label: $"T{tier} overmatched",
        AttackBonus: 1,
        DamageBonus: 1,
        Bushcraft: 0,
        Cunning: 0,
        BaseAc: ExpectedArmorAc(1),    // tier-1 gear regardless of monster tier
        ReadsIntent: false);

    /// <summary>"Tourist" per the doc: Combat +0, tier-1 gear, AC 11. T3 only — should die ~91%.</summary>
    public static PcProfile Tourist() => new(
        Label: "T3 tourist",
        AttackBonus: 0,
        DamageBonus: 0,
        Bushcraft: 0,
        Cunning: 0,
        BaseAc: 11,
        ReadsIntent: false);

    /// <summary>Expected armor AC for each tier per the doc's "Armor scales with tier" table.</summary>
    static int ExpectedArmorAc(int tier) => tier switch
    {
        1 => 12,   // light/medium 12-13
        2 => 14,   // medium 13-14 (use top of band)
        3 => 18,   // heavy plate
        _ => 12,
    };

    public const int StartingSpirits = 20;
    public const int StartingHealth  = 4;
    public const int SurpriseDc      = 12;
}
