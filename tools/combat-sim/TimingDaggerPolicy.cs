using Dreamlands.Combat;
using Dreamlands.Encounter;

namespace CombatSim;

/// <summary>
/// Timing-window dagger model. Player attacks resolve via timing precision
/// rather than d20:
///   miss   → 0 damage
///   hit    → base die + damage bonus
///   crit   → 2x dice (regular crit damage)
///   super  → 2x dice + cancels monster's next turn (T3+ unlock only)
///
/// Rates are nested-cumulative: a roll under <see cref="SuperCritRate"/> is a
/// super-crit; a roll under <see cref="CritRate"/> (but not super) is a crit;
/// under <see cref="HitRate"/> (but not crit) is a hit; otherwise miss. So
/// (HitRate=0.7, CritRate=0.2, SuperCritRate=0.05) means 5% super, 15% crit,
/// 50% just-hit, 30% miss.
///
/// Defense is independent (uses Pc.BaseAc unmodified). The minigame only
/// affects damage output; survival is the standard AC story.
///
/// Super-crit cancels the immediately-following monster turn, but the heavy
/// cooldown still ticks per design — so blanking a basic just costs the
/// monster an action; blanking a heavy resets the cooldown without dealing
/// damage. Either way the monster loses one swing.
/// </summary>
public sealed record TimingDaggerParams(
    DiceRoll BaseDie,
    double HitRate,
    double CritRate,
    double SuperCritRate);

public sealed class TimingDaggerPolicy(TimingDaggerParams p) : WeaponPolicy
{
    readonly TimingDaggerParams _p = p;
    int _pendingMonsterTurnSkips;

    public override string Name =>
        $"Dagger ({_p.BaseDie.Count}d{_p.BaseDie.Sides} hit={_p.HitRate:P0} crit={_p.CritRate:P0} super={_p.SuperCritRate:P0})";

    public override void Reset(PcProfile pc)
    {
        base.Reset(pc);
        _pendingMonsterTurnSkips = 0;
    }

    public override bool ConsumeMonsterTurnSkip()
    {
        if (_pendingMonsterTurnSkips <= 0) return false;
        _pendingMonsterTurnSkips--;
        return true;
    }

    public override int ChooseAndExecute(PolicyTurn turn, Random rng)
    {
        var dice = _p.BaseDie with { Modifier = Pc.DamageBonus };
        double roll = rng.NextDouble();

        if (roll < _p.SuperCritRate)
        {
            _pendingMonsterTurnSkips++;
            return Resolver.RollDamage(rng, dice, crit: true).Total;
        }
        if (roll < _p.CritRate)
            return Resolver.RollDamage(rng, dice, crit: true).Total;
        if (roll < _p.HitRate)
            return Resolver.RollDamage(rng, dice, crit: false).Total;
        return 0;
    }
}
