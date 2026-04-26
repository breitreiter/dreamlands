namespace CombatPrototype.Combat;

public enum SwordStance { Aggressive, Balanced, Defensive }

public static class StanceModifiers
{
    /// <summary>
    /// Stance modifier: <c>attack</c> applies to both to-hit and damage (D&D-style:
    /// attack bonus is one number); <c>ac</c> is the matched defensive trade.
    /// </summary>
    public static (int attack, int ac) For(SwordStance stance) => stance switch
    {
        SwordStance.Aggressive => (+2, -2),
        SwordStance.Balanced   => ( 0,  0),
        SwordStance.Defensive  => (-2, +2),
        _ => (0, 0)
    };
}
