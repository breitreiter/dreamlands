using Dreamlands.Rules;

namespace Dreamlands.Game;

/// <summary>
/// End-of-day resolution engine. Auto-consumes a single ration, applies medicine cures,
/// resolves condition drains, and ticks HP regen. Wilderness only — settlement nights
/// bypass this entirely. Travel hazards (thirst/cold/fatigue) are NOT handled here:
/// they accrue deterministically per step/night via <see cref="Travails"/>.
///
/// Tier-based regime (skill_tier_rework.md):
///   - Bushcraft tier controls food cadence (Untrained: every night; Trained/Expert: every other
///     night based on state.Day parity — consume when Day is odd).
///   - medical_kit cures one serious condition per night without being consumed.
///   - Severe conditions drain HP (-1/night); HP +1/day when none are active.
///   - No daily passive spirits regen on the road.
/// </summary>
public static class EndOfDay
{
    /// <summary>Execute the full end-of-day resolution sequence.</summary>
    public static List<EndOfDayEvent> Resolve(
        PlayerState state, BalanceData balance,
        int startX = 0, int startY = 0)
    {
        var events = new List<EndOfDayEvent>();

        // Read and clear pending flags
        var noMeal = state.PendingNoMeal;
        state.PendingEndOfDay = false;
        state.PendingNoSleep = false;
        state.PendingNoMeal = false;

        // 1. Determine if player eats tonight based on Bushcraft tier
        var eatsTonight = ShouldEatTonight(state);

        // 2. Auto-consume one ration (unless no food cadence tonight, or noMeal flag)
        if (!noMeal && eatsTonight)
            ResolveFood(state, events);

        // 3. Auto-apply medical_kit for serious conditions (cure without consuming)
        var treatedConditions = ResolveMedicines(state, balance, events);

        // 4. Spirits drain from missed meals and any spirit-draining conditions
        ResolveSpiritsDrain(state, balance, events);

        // 5. HP regen / drain based on serious conditions
        var hpDrainCondition = ResolveHealthTick(state, treatedConditions, balance, events);

        // 6. Death check — rescue instead of permadeath
        if (state.Health <= 0)
        {
            events.Add(new EndOfDayEvent.PlayerDied(hpDrainCondition));
            var rescue = Rescue.Apply(state, startX, startY, balance);
            events.Add(new EndOfDayEvent.PlayerRescued(rescue.LostItems, rescue.GoldLost));
        }

        return events;
    }

    /// <summary>
    /// Determine if the player should consume a ration tonight.
    /// Untrained Bushcraft: eat every night.
    /// Trained/Expert Bushcraft: eat every other night — consume on odd days.
    /// No new state field needed; parity of Day drives the cadence.
    /// </summary>
    static bool ShouldEatTonight(PlayerState state)
    {
        var tier = state.Skills.GetValueOrDefault(Skill.Bushcraft);
        if (tier == SkillTier.Untrained)
            return true;

        // Trained/Expert: consume on odd days (1, 3, 5…)
        return state.Day % 2 != 0;
    }

    /// <summary>
    /// Eat one ration from the pack. Emits FoodConsumed or Starving.
    /// </summary>
    static void ResolveFood(PlayerState state, List<EndOfDayEvent> events)
    {
        var idx = state.Pack.FindIndex(i => i.DefId == Rations.RationDefId);
        if (idx >= 0)
        {
            var item = state.Pack[idx];
            state.Pack.RemoveAt(idx);
            events.Add(new EndOfDayEvent.FoodConsumed([item.DisplayName]));
        }
        else
        {
            // No food: spirits penalty applied in ResolveSpiritsDrain via the Starving event
            events.Add(new EndOfDayEvent.Starving());
        }
    }

    /// <summary>
    /// Apply pack-carried medicines (Tool items with a Cures set) to serious conditions.
    /// Medicines are reusable — never consumed. Each condition is cured by the first
    /// matching tool in the pack; if the player carries the right kit for several
    /// conditions, all of them clear in one night.
    /// </summary>
    static HashSet<string> ResolveMedicines(PlayerState state,
        BalanceData balance, List<EndOfDayEvent> events)
    {
        var treated = new HashSet<string>();

        var seriousConditions = state.ActiveConditions
            .Where(id => balance.Conditions.TryGetValue(id, out var def)
                && def.Severity == ConditionSeverity.Severe)
            .OrderBy(id => id)
            .ToList();

        if (seriousConditions.Count == 0) return treated;

        foreach (var conditionId in seriousConditions)
        {
            var kit = state.Pack.FirstOrDefault(i =>
                ItemDef.All.TryGetValue(i.DefId, out var def)
                && def.Cures.Contains(conditionId));
            if (kit == null) continue;

            state.ActiveConditions.Remove(conditionId);
            treated.Add(conditionId);
            events.Add(new EndOfDayEvent.CureApplied(kit.DefId, conditionId));
            events.Add(new EndOfDayEvent.ConditionCured(conditionId));
        }

        return treated;
    }

    /// <summary>Apply the spirits penalty for a missed meal.</summary>
    static void ResolveSpiritsDrain(PlayerState state, BalanceData balance, List<EndOfDayEvent> events)
    {
        var noFoodToday = events.Any(e => e is EndOfDayEvent.Starving);
        if (noFoodToday)
        {
            state.Spirits = Math.Max(0, state.Spirits - 1);
            events.Add(new EndOfDayEvent.ConditionDrain("starving", 0, 1));
        }
    }

    /// <summary>
    /// HP regen or drain based on serious conditions:
    ///   - Any active untreated serious condition → HP -1
    ///   - Otherwise → HP +1 (capped at MaxHealth)
    /// Returns the worst untreated serious condition id, if any (used for death messaging).
    /// </summary>
    static string? ResolveHealthTick(PlayerState state, HashSet<string> treatedConditions,
        BalanceData balance, List<EndOfDayEvent> events)
    {
        string? worstCondition = null;
        bool hasUntreatedSerious = false;

        foreach (var conditionId in state.ActiveConditions)
        {
            if (!balance.Conditions.TryGetValue(conditionId, out var def)) continue;

            if (def.Severity == ConditionSeverity.Severe && !treatedConditions.Contains(conditionId))
            {
                hasUntreatedSerious = true;
                worstCondition ??= conditionId;
            }

            if (def.SpecialEffect != null)
                events.Add(new EndOfDayEvent.SpecialEffect(conditionId, def.SpecialEffect));
        }

        if (hasUntreatedSerious)
        {
            state.Health = Math.Max(0, state.Health - 1);
            events.Add(new EndOfDayEvent.ConditionDrain(worstCondition!, 1, 0));
        }
        else if (state.Health < state.MaxHealth)
        {
            state.Health++;
            events.Add(new EndOfDayEvent.HealthRegen(1));
        }

        return worstCondition;
    }
}
