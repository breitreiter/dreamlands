using Dreamlands.Combat;
using Dreamlands.Encounter;

namespace CombatSim;

/// <summary>
/// Sword: three persistent stances (Aggressive +2/-2, Balanced 0/0, Defensive -2/+2),
/// switched freely each player turn. Damage die is 1d8 (longsword baseline). Tier
/// scaling lands as a flat damage bonus from <see cref="PcProfile.DamageBonus"/>.
/// Already-validated reference policy — used as the parity yardstick for axe/dagger.
/// </summary>
public sealed class SwordPolicy : WeaponPolicy
{
    static readonly DiceRoll BaseDie = new(1, 8, 0);

    SwordStance _stance = SwordStance.Balanced;

    public override string Name => "Sword (stances)";

    public override int EffectiveAc => Pc.BaseAc + StanceMod(_stance).ac;

    public override void Reset(PcProfile pc)
    {
        base.Reset(pc);
        _stance = SwordStance.Balanced;
    }

    public override int ChooseAndExecute(PolicyTurn turn, Random rng)
    {
        if (Pc.ReadsIntent)
        {
            // Defensive only when a Big-Attack-class swing is about to land. Aggressive
            // otherwise — basic attacks are small, the +2/-2 trade favors tempo.
            _stance = turn.NextMonsterIntent switch
            {
                IntentClass.HeavyAttack => SwordStance.Defensive,
                IntentClass.Pierce      => SwordStance.Defensive,
                _                       => SwordStance.Aggressive,
            };
        }
        else
        {
            _stance = SwordStance.Balanced;  // tourist mode — no read
        }

        var (atkMod, _) = StanceMod(_stance);
        var attack = Resolver.RollAttack(rng, Pc.AttackBonus + atkMod, turn.MonsterAc);
        if (!attack.Hit) return 0;
        var dmg = Resolver.RollDamage(rng, BaseDie with { Modifier = Pc.DamageBonus + atkMod }, attack.Crit);
        return dmg.Total;
    }

    static (int attack, int ac) StanceMod(SwordStance s) => s switch
    {
        SwordStance.Aggressive => (+2, -2),
        SwordStance.Balanced   => ( 0,  0),
        SwordStance.Defensive  => (-2, +2),
        _ => (0, 0)
    };
}
