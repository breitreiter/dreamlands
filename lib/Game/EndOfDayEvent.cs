namespace Dreamlands.Game;

/// <summary>Structural result from end-of-day resolution. UI/flavor layer handles display text.</summary>
public abstract record EndOfDayEvent
{
    public record FoodConsumed(List<string> FoodEaten) : EndOfDayEvent;
    public record Starving : EndOfDayEvent;
    public record ResistPassed(string ConditionId, SkillCheckResult Check) : EndOfDayEvent;
    public record ResistFailed(string ConditionId, SkillCheckResult Check) : EndOfDayEvent;
    public record CureApplied(string ItemDefId, string ConditionId) : EndOfDayEvent;
    public record ConditionAcquired(string ConditionId) : EndOfDayEvent;
    public record ConditionCured(string ConditionId) : EndOfDayEvent;
    public record ConditionDrain(string ConditionId, int HealthLost, int SpiritsLost) : EndOfDayEvent;
    public record SpecialEffect(string ConditionId, string Effect) : EndOfDayEvent;
    public record HealthRegen(int HealthGained) : EndOfDayEvent;
    public record Foraged(int Rolled, int Modifier, bool Fed) : EndOfDayEvent;
    public record PlayerDied(string? ConditionId) : EndOfDayEvent;
    public record PlayerRescued(List<string> LostItems, int GoldLost) : EndOfDayEvent;
}
