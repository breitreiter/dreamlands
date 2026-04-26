namespace CombatPrototype.Combat;

public abstract record PlayerAction
{
    public sealed record Attack : PlayerAction;
    public sealed record SetStance(SwordStance Stance) : PlayerAction;
    public sealed record Flee : PlayerAction;
    public sealed record Abort : PlayerAction;
}

public interface IPlayerController
{
    PlayerAction GetAction(CombatState state);
}
