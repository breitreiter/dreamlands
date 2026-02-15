using Dreamlands.Rules;

namespace Dreamlands.Game;

/// <summary>Structural result from applying a mechanic. UI/flavor layer handles display text.</summary>
public abstract record MechanicResult
{
    public record HealthChanged(int Delta, int NewValue) : MechanicResult;
    public record SpiritsChanged(int Delta, int NewValue) : MechanicResult;
    public record GoldChanged(int Delta, int NewValue) : MechanicResult;
    public record SkillChanged(Skill Skill, int Delta, int NewValue) : MechanicResult;
    public record ItemGained(string DefId, string DisplayName) : MechanicResult;
    public record ItemLost(string DefId, string DisplayName) : MechanicResult;
    public record TagAdded(string TagId) : MechanicResult;
    public record TagRemoved(string TagId) : MechanicResult;
    public record ConditionAdded(string ConditionId) : MechanicResult;
    public record TimeAdvanced(TimePeriod NewPeriod, int NewDay) : MechanicResult;
    public record Navigation(string EncounterId) : MechanicResult;
    public record DungeonFinished : MechanicResult;
    public record DungeonFled : MechanicResult;
}
