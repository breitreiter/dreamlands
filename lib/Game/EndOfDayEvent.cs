namespace Dreamlands.Game;

/// <summary>Structural result from end-of-day resolution. UI/flavor layer handles display text.</summary>
public abstract record EndOfDayEvent
{
    public record FoodConsumed(List<string> FoodEaten, bool Balanced) : EndOfDayEvent;
    public record Starving : EndOfDayEvent;
    public record HungerReduced(int NewStacks) : EndOfDayEvent;
    public record HungerCured : EndOfDayEvent;
    public record ResistPassed(string ConditionId, SkillCheckResult Check) : EndOfDayEvent;
    public record ResistFailed(string ConditionId, SkillCheckResult Check, int Stacks) : EndOfDayEvent;
    public record CureApplied(string ItemDefId, string ConditionId, int StacksRemoved, int Remaining) : EndOfDayEvent;
    public record CureNegated(string ItemDefId, string ConditionId) : EndOfDayEvent;
    public record ConditionCured(string ConditionId) : EndOfDayEvent;
    public record ConditionDrain(string ConditionId, int HealthLost, int SpiritsLost) : EndOfDayEvent;
    public record SpecialEffect(string ConditionId, string Effect) : EndOfDayEvent;
    public record RestRecovery(int HealthGained, int SpiritsGained) : EndOfDayEvent;
    public record PlayerDied(string? ConditionId) : EndOfDayEvent;
}
