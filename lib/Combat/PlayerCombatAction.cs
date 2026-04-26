using Dreamlands.Game;

namespace Dreamlands.Combat;

public abstract record PlayerCombatAction
{
    /// <summary>Attack with the equipped weapon. Consumes the turn.</summary>
    public sealed record Attack : PlayerCombatAction;

    /// <summary>Toggle sword stance. Free action — does not consume the turn.</summary>
    public sealed record SetStance(SwordStance Stance) : PlayerCombatAction;

    /// <summary>Attempt to flee. Cunning save; failure costs the turn and a free monster basic-attack.</summary>
    public sealed record Flee : PlayerCombatAction;
}
