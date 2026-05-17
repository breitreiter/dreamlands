using Dreamlands.Rules;

namespace Dreamlands.Game;

/// <summary>
/// End-of-day resolution engine. Auto-consumes a single ration and applies medical_kit cure,
/// resolves ambient condition resists, condition drain, and HP regen.
///
/// Tier-based regime (skill_tier_rework.md):
///   - Bushcraft tier controls food cadence (Untrained: every night; Trained/Expert: every other
///     night based on state.Day parity — consume when Day is odd).
///   - Travel condition resists: Bushcraft tier → 0% / 40% / 80% passive resist.
///   - Serious condition resists: Cunning tier → 0% / 40% / 80% passive resist.
///   - medical_kit cures one serious condition per night without being consumed.
///   - Minor conditions drain spirits (-2/night); severe conditions drain HP (-1/night).
///   - HP +1/day when no serious conditions, HP -1/day when any serious is active.
///   - No daily passive spirits regen on the road.
/// </summary>
public static class EndOfDay
{
    // Conditions that are always checked regardless of biome
    static readonly string[] UniversalAmbientIds = ["exhausted", "lost"];

    // Conditions that only come from encounters, never from ambient resist checks
    static readonly HashSet<string> EncounterOnlyIds = ["poisoned", "injured", "irradiated", "lattice_sickness"];

    // Travel conditions resist via Bushcraft; serious conditions resist via Cunning
    static readonly HashSet<string> TravelConditionIds = ["exhausted", "freezing", "thirsty", "lost"];

    /// <summary>
    /// Returns ambient conditions that threaten the player tonight based on camping biome/tier.
    /// </summary>
    public static List<ConditionDef> GetThreats(string biome, int tier, BalanceData balance)
    {
        var threats = new List<ConditionDef>();

        foreach (var def in balance.Conditions.Values)
        {
            if (EncounterOnlyIds.Contains(def.Id)) continue;

            // Biome-specific conditions
            if (def.Biome != "none" && def.Biome == biome
                && (def.Tier == "any" || def.Tier == tier.ToString()))
            {
                threats.Add(def);
                continue;
            }

            // Universal ambient conditions
            if (UniversalAmbientIds.Contains(def.Id))
                threats.Add(def);
        }

        return threats;
    }

    /// <summary>
    /// Execute the full end-of-day resolution sequence. Wilderness only —
    /// settlement nights bypass this entirely.
    /// </summary>
    public static List<EndOfDayEvent> Resolve(
        PlayerState state, string biome, int tier,
        BalanceData balance, Random rng,
        int startX = 0, int startY = 0)
    {
        var events = new List<EndOfDayEvent>();

        // Read and clear pending flags
        var noMeal = state.PendingNoMeal;
        var noBiome = state.PendingNoBiome;
        state.PendingEndOfDay = false;
        state.PendingNoSleep = false;
        state.PendingNoMeal = false;
        state.PendingNoBiome = false;

        // 0. Clear biome-specific conditions when the player is no longer in that biome
        if (!noBiome)
            ClearOutOfBiomeConditions(state, biome, balance, events);

        // Snapshot which conditions the player already has entering this rest
        var preExisting = new HashSet<string>(state.ActiveConditions);

        // 1. Roll passive resists — record pass/fail, do NOT apply new conditions yet
        var resistResults = RollResists(state, biome, tier, noBiome, balance, rng, events);

        // 2. Determine if player eats tonight based on Bushcraft tier
        var eatsTonight = ShouldEatTonight(state);

        // 3. Auto-consume one ration (unless no food cadence tonight, or noMeal flag)
        if (!noMeal && eatsTonight)
            ResolveFood(state, events);

        // 4. Auto-apply medical_kit for serious conditions (cure without consuming)
        var treatedConditions = ResolveMedicines(state, preExisting, balance, events);

        // 5. Apply new conditions from failed resists
        ApplyNewConditions(state, resistResults, balance, events);

        // 6. Spirits drain from active minor conditions
        ResolveSpiritsDrain(state, balance, events);

        // 7. HP regen / drain based on serious conditions
        var hpDrainCondition = ResolveHealthTick(state, treatedConditions, balance, events);

        // 8. Death check — rescue instead of permadeath
        if (state.Health <= 0)
        {
            events.Add(new EndOfDayEvent.PlayerDied(hpDrainCondition));
            var rescue = Rescue.Apply(state, startX, startY, balance);
            events.Add(new EndOfDayEvent.PlayerRescued(rescue.LostItems, rescue.GoldLost));
            return events;
        }

        // 9. Increment consecutive wilderness nights counter (feeds exhaustion scaling logic)
        if (!noBiome)
            state.ConsecutiveWildernessNights++;

        // 10. Clear the per-turn condition-immunity set now that the day has resolved
        state.ConditionsClearedThisTurn.Clear();

        return events;
    }

    static void ClearOutOfBiomeConditions(PlayerState state, string biome,
        BalanceData balance, List<EndOfDayEvent> events)
    {
        var toClear = new List<string>();
        foreach (var conditionId in state.ActiveConditions)
        {
            if (!balance.Conditions.TryGetValue(conditionId, out var def)) continue;
            if (def.Biome != "none" && def.Biome != biome)
                toClear.Add(conditionId);
        }

        foreach (var conditionId in toClear)
        {
            state.ActiveConditions.Remove(conditionId);
            events.Add(new EndOfDayEvent.ConditionCured(conditionId));
        }
    }

    /// <summary>
    /// Roll passive resists for all ambient threats using the tier model.
    /// Travel conditions resist via Bushcraft; others use the default (0%).
    /// Returns condition IDs that failed the resist check.
    /// </summary>
    static HashSet<string> RollResists(PlayerState state, string biome, int tier,
        bool noBiome, BalanceData balance, Random rng, List<EndOfDayEvent> events)
    {
        var failed = new HashSet<string>();
        if (noBiome) return failed;

        var threats = GetThreats(biome, tier, balance);
        var bushcraftTier = state.Skills.GetValueOrDefault(Skill.Bushcraft);

        foreach (var threat in threats)
        {
            // Skip rolls for conditions the player already has — adding is a no-op
            if (state.ActiveConditions.Contains(threat.Id)) continue;

            // Skip threats the player just cleared this turn
            if (state.ConditionsClearedThisTurn.Contains(threat.Id)) continue;

            // Tier-based passive resist: travel conditions use Bushcraft
            var tier_ = TravelConditionIds.Contains(threat.Id) ? bushcraftTier : SkillTier.Untrained;
            var resisted = SkillResolution.RollPassiveResist(tier_, rng);

            if (resisted)
                events.Add(new EndOfDayEvent.ResistPassed(threat.Id, null));
            else
            {
                failed.Add(threat.Id);
                events.Add(new EndOfDayEvent.ResistFailed(threat.Id, null));
            }
        }

        return failed;
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
    /// Apply medical_kit cure for pre-existing serious conditions.
    /// The kit is NOT consumed — it persists in the pack for future nights.
    /// Cures one condition per night (alphabetic order if multiple serious conditions active).
    /// </summary>
    static HashSet<string> ResolveMedicines(PlayerState state, HashSet<string> preExisting,
        BalanceData balance, List<EndOfDayEvent> events)
    {
        var treated = new HashSet<string>();

        // Find a medical_kit in pack
        var kit = state.Pack.FirstOrDefault(i => i.DefId == "medical_kit");
        if (kit == null) return treated;

        // Find the first pre-existing serious condition (alphabetic for determinism)
        var seriousConditions = state.ActiveConditions
            .Where(id => preExisting.Contains(id)
                && balance.Conditions.TryGetValue(id, out var def)
                && def.Severity == ConditionSeverity.Severe)
            .OrderBy(id => id)
            .ToList();

        if (seriousConditions.Count == 0) return treated;

        var conditionId = seriousConditions[0];
        // Cure without consuming the kit
        state.ActiveConditions.Remove(conditionId);
        treated.Add(conditionId);
        events.Add(new EndOfDayEvent.CureApplied(kit.DefId, conditionId));
        events.Add(new EndOfDayEvent.ConditionCured(conditionId));

        return treated;
    }

    static void ApplyNewConditions(PlayerState state, HashSet<string> failedResists,
        BalanceData balance, List<EndOfDayEvent> events)
    {
        foreach (var conditionId in failedResists)
        {
            if (!state.ActiveConditions.Add(conditionId)) continue;
            events.Add(new EndOfDayEvent.ConditionAcquired(conditionId));
        }
    }

    /// <summary>
    /// Apply spirits drain from active minor conditions and from missed meals.
    /// Drains stack — exhausted + thirsty in the desert costs 4 spirits/night.
    /// </summary>
    static void ResolveSpiritsDrain(PlayerState state, BalanceData balance, List<EndOfDayEvent> events)
    {
        // Missed meal drains 1 spirit
        var noFoodToday = events.Any(e => e is EndOfDayEvent.Starving);
        if (noFoodToday)
        {
            state.Spirits = Math.Max(0, state.Spirits - 1);
            events.Add(new EndOfDayEvent.ConditionDrain("starving", 0, 1));
        }

        // Per-condition drains
        foreach (var conditionId in state.ActiveConditions)
        {
            if (!balance.Conditions.TryGetValue(conditionId, out var def)) continue;
            if (def.SpiritsDrain is not { } drain || drain <= 0) continue;

            state.Spirits = Math.Max(0, state.Spirits - drain);
            events.Add(new EndOfDayEvent.ConditionDrain(conditionId, 0, drain));
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
