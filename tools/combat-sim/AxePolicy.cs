using Dreamlands.Combat;
using Dreamlands.Encounter;

namespace CombatSim;

/// <summary>Tunable axe parameters. Locked design uses defaults below.</summary>
public sealed record AxeParams(
    int MomentumAcPenaltyPerStack = 1,
    int MomentumCap = 5,
    int SpiritsThresholdToBlock = 10);

/// <summary>
/// Axe: stacking +1 damage / -<c>MomentumAcPenaltyPerStack</c> AC per attack action,
/// capped at <see cref="AxeParams.MomentumCap"/>. Block grants full immunity to the
/// next monster attack and resets momentum to 0. Damage die is 2d4 (avg 5, range 2-8
/// — tighter floor than 1d8).
///
/// Smart-block decision (only used when reading intent):
///   block iff   (next intent is Heavy/Pierce)
///         AND  (spirits ≤ threshold — fat buffer means just eat the hit)
///         AND  (EV of this turn's attack &lt; monster HP — no kill swing on the table)
/// </summary>
public sealed class AxePolicy(AxeParams p) : WeaponPolicy
{
    static readonly DiceRoll BaseDie = new(2, 4, 0);
    const int MomentumDamagePerStack = 1;

    readonly AxeParams _p = p;
    int _momentum;
    bool _immuneThisTurn;

    public override string Name => $"Axe (mAcPen={_p.MomentumAcPenaltyPerStack} cap={_p.MomentumCap} spThresh={_p.SpiritsThresholdToBlock})";

    public override int EffectiveAc =>
        Pc.BaseAc - _momentum * _p.MomentumAcPenaltyPerStack;

    public override bool ImmuneToIncomingDamage => _immuneThisTurn;

    public override void Reset(PcProfile pc)
    {
        base.Reset(pc);
        _momentum = 0;
        _immuneThisTurn = false;
    }

    public override int ChooseAndExecute(PolicyTurn turn, Random rng)
    {
        bool considerBlock = Pc.ReadsIntent &&
            turn.NextMonsterIntent is IntentClass.HeavyAttack or IntentClass.Pierce;
        bool block = considerBlock
                     && turn.PlayerSpirits <= _p.SpiritsThresholdToBlock
                     && ExpectedAttackDamage(turn.MonsterAc, Pc.AttackBonus, AttackDice()) < turn.MonsterHp;

        if (block)
        {
            _immuneThisTurn = true;
            _momentum = 0;
            return 0;
        }

        var attack = Resolver.RollAttack(rng, Pc.AttackBonus, turn.MonsterAc);
        int dmg = 0;
        if (attack.Hit)
        {
            var dice = AttackDice();
            dmg = Resolver.RollDamage(rng, dice, attack.Crit).Total;
        }
        // Momentum builds on the action regardless of hit/miss (per design: "missing
        // does not reset momentum, only choosing a non-attack action does").
        _momentum = Math.Min(_momentum + 1, _p.MomentumCap);
        return dmg;
    }

    public override void OnMonsterTurnComplete()
    {
        _immuneThisTurn = false;
    }

    DiceRoll AttackDice() =>
        BaseDie with { Modifier = Pc.DamageBonus + _momentum * MomentumDamagePerStack };
}
