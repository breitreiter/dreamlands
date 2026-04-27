using Dreamlands.Game;

namespace Dreamlands.Combat;

/// <summary>
/// Outcome bands for a dagger timing-window attack. The client resolves the
/// timing minigame and posts the band it landed in; the server treats this as
/// authoritative (single-player game; cheating isn't a sport here).
///
/// See project/design/dagger_reflex_minigame.md for the full design.
/// </summary>
public enum TimingBand
{
    Miss,
    Hit,
    Crit,
    SuperCrit,
}

public abstract record PlayerCombatAction
{
    /// <summary>Attack with the equipped weapon. Consumes the turn.</summary>
    public sealed record Attack : PlayerCombatAction;

    /// <summary>Toggle sword stance. Free action — does not consume the turn.</summary>
    public sealed record SetStance(SwordStance Stance) : PlayerCombatAction;

    /// <summary>
    /// Dagger timing-window attack. The client posts the <see cref="TimingBand"/>
    /// it landed in; the server applies the corresponding outcome (miss / hit /
    /// crit / super-crit). Super-crit cancels the next monster turn.
    /// </summary>
    public sealed record DaggerAttack(TimingBand Band) : PlayerCombatAction;

    /// <summary>Attempt to flee. Cunning save; failure costs the turn and a free monster basic-attack.</summary>
    public sealed record Flee : PlayerCombatAction;
}
