using Dreamlands.Game;

namespace Dreamlands.Combat;

public static class StanceModifiers
{
    /// <summary>
    /// D&D-style: <c>attack</c> applies to both to-hit and damage; <c>ac</c> is the
    /// matched defensive trade. See project/design/weapon_classes.md.
    /// </summary>
    public static (int attack, int ac) For(SwordStance stance) => stance switch
    {
        SwordStance.Aggressive => (+2, -2),
        SwordStance.Balanced   => ( 0,  0),
        SwordStance.Defensive  => (-2, +2),
        _ => (0, 0)
    };

    public static int PlayerEffectiveAc(CombatState state) =>
        state.Profile.BaseAc + For(state.Stance).ac;

    public static int PlayerAttackBonus(CombatState state) =>
        state.Profile.AttackBonus + For(state.Stance).attack;

    public static int PlayerDamageBonus(CombatState state) =>
        state.Profile.DamageBonus + For(state.Stance).attack;
}
