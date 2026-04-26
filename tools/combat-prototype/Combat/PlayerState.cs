using CombatPrototype.Cmb;

namespace CombatPrototype.Combat;

public sealed class PlayerState
{
    public int MaxSpirits { get; init; }
    public int MaxHealth { get; init; }
    public int Spirits { get; set; }
    public int Health { get; set; }

    public int BaseAc { get; init; }
    public int BaseAttackBonus { get; init; }
    public int BaseDamageBonus { get; init; }
    public int DamageDieSize { get; init; }

    public int Bushcraft { get; init; }
    public int Cunning { get; init; }

    public SwordStance Stance { get; set; } = SwordStance.Balanced;

    public bool IsDead => Health <= 0;

    public int EffectiveAc => BaseAc + StanceModifiers.For(Stance).ac;

    public int AttackToHitBonus => BaseAttackBonus + StanceModifiers.For(Stance).attack;

    public int EffectiveDamageBonus => BaseDamageBonus + StanceModifiers.For(Stance).attack;

    public DiceRoll DamageRoll => new(1, DamageDieSize, EffectiveDamageBonus);

    public static PlayerState FromLoadout(Loadout l) => new()
    {
        MaxSpirits = l.StartingSpirits,
        MaxHealth = l.StartingHealth,
        Spirits = l.StartingSpirits,
        Health = l.StartingHealth,
        BaseAc = l.Ac,
        BaseAttackBonus = l.AttackBonus,
        BaseDamageBonus = l.DamageBonus,
        DamageDieSize = l.DamageDieSize,
        Bushcraft = l.Bushcraft,
        Cunning = l.Cunning
    };

    public static PlayerState Default() => FromLoadout(new Loadout());

    public DamageBreakdown TakeDamage(int amount)
    {
        int spiritsBefore = Spirits;
        int healthBefore = Health;

        int absorbedBySpirits = Math.Min(Spirits, amount);
        Spirits -= absorbedBySpirits;
        int overflow = amount - absorbedBySpirits;
        int absorbedByHealth = Math.Min(Health, overflow);
        Health -= absorbedByHealth;

        return new DamageBreakdown(amount, absorbedBySpirits, absorbedByHealth, spiritsBefore, healthBefore);
    }
}

public sealed record DamageBreakdown(int Total, int OnSpirits, int OnHealth, int SpiritsBefore, int HealthBefore);
