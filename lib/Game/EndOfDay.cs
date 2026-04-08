using Dreamlands.Rules;

namespace Dreamlands.Game;

/// <summary>
/// End-of-day resolution engine. Auto-consumes a single ration and any matching medicine,
/// resolves ambient resists, condition drain, and HP regen.
///
/// New regime (haversack_refactor.md + spirits_economy.md):
///   - Single food_ration item, 1 per day. No balanced-meal bonus.
///   - Conditions are binary; minor conditions drain spirits, serious conditions tick HP.
///   - HP +1/day when no serious conditions, HP -1/day when any serious is active.
///   - No daily passive spirits regen on the road.
///   - Exhaustion DC scales with ConsecutiveWildernessNights.
///   - Foraging is binary (success skips the day's ration consumption).
/// </summary>
public static class EndOfDay
{
    // Conditions that are always checked regardless of biome
    static readonly string[] UniversalAmbientIds = ["exhausted", "lost"];

    // Conditions that only come from encounters, never from ambient resist checks
    static readonly HashSet<string> EncounterOnlyIds = ["poisoned", "injured", "irradiated", "lattice_sickness"];

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

        // Snapshot which conditions the player already has entering this rest
        var preExisting = new HashSet<string>(state.ActiveConditions);

        // 1. Roll resists silently — record pass/fail, do NOT apply new conditions yet
        var resistResults = RollResists(state, biome, tier, noBiome, balance, rng, events);

        // 2. Forage check — success skips the day's ration consumption
        var foragedToday = ResolveForaging(state, noBiome, balance, rng, events);

        // 3. Auto-consume one ration (unless foraged or noMeal)
        if (!noMeal)
            ResolveFood(state, foragedToday, events);

        // 4. Auto-consume medicine for serious conditions
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

        // 9. Increment consecutive wilderness nights counter (feeds exhaustion DC)
        if (!noBiome)
            state.ConsecutiveWildernessNights++;

        return events;
    }

    /// <summary>
    /// Roll resist checks for all ambient threats. Returns failed condition IDs.
    /// Exhaustion uses a scaling DC tied to consecutive wilderness nights;
    /// other conditions use their static ResistDifficulty.
    /// </summary>
    static HashSet<string> RollResists(PlayerState state, string biome, int tier,
        bool noBiome, BalanceData balance, Random rng, List<EndOfDayEvent> events)
    {
        var failed = new HashSet<string>();
        if (noBiome) return failed;

        var threats = GetThreats(biome, tier, balance);

        foreach (var threat in threats)
        {
            // Skip rolls for conditions the player already has — adding is a no-op
            if (state.ActiveConditions.Contains(threat.Id)) continue;

            SkillCheckResult check;
            if (threat.Id == "exhausted")
            {
                var dc = balance.Character.ExhaustionBaseDC
                       + balance.Character.ExhaustionDCPerNight * state.ConsecutiveWildernessNights;
                check = SkillChecks.RollResist(threat.Id, dc, state, balance, rng);
            }
            else
            {
                var dc = threat.ResistDifficulty ?? balance.Character.AmbientResistDifficulty;
                check = SkillChecks.RollResist(threat.Id, dc, state, balance, rng);
            }

            if (check.Passed)
                events.Add(new EndOfDayEvent.ResistPassed(threat.Id, check));
            else
            {
                failed.Add(threat.Id);
                events.Add(new EndOfDayEvent.ResistFailed(threat.Id, check));
            }
        }

        return failed;
    }

    /// <summary>
    /// Binary foraging: d20 + bushcraft + bushcraft gear vs ForageDC. Success means
    /// the player skips the day's ration consumption (eats from the land). Failure
    /// is silent — they fall back on their pack rations. No items added either way.
    /// </summary>
    static bool ResolveForaging(PlayerState state, bool noBiome,
        BalanceData balance, Random rng, List<EndOfDayEvent> events)
    {
        if (noBiome) return false;

        var skillLevel = state.Skills.GetValueOrDefault(Skill.Bushcraft);
        var itemBonus = SkillChecks.GetItemBonus(Skill.Bushcraft, state, balance);
        var modifier = skillLevel + itemBonus;

        var natural = SkillChecks.RollD20(RollMode.Normal, rng);
        var total = natural + modifier;
        var fed = total >= balance.Character.ForageDC;

        events.Add(new EndOfDayEvent.Foraged(total, modifier, fed));
        return fed;
    }

    /// <summary>
    /// Eat one ration from the haversack. If foraging fed the player, skip consumption.
    /// If no ration is available, the day is hungry — caller checks the Starving event
    /// and applies a spirits penalty.
    /// </summary>
    static void ResolveFood(PlayerState state, bool foragedToday, List<EndOfDayEvent> events)
    {
        if (foragedToday) return;

        var idx = state.Haversack.FindIndex(i => i.DefId == Rations.RationDefId);
        if (idx >= 0)
        {
            var item = state.Haversack[idx];
            state.Haversack.RemoveAt(idx);
            events.Add(new EndOfDayEvent.FoodConsumed([item.DisplayName]));
        }
        else
        {
            // No food: spirits penalty applied in ResolveSpiritsDrain via the Starving event
            events.Add(new EndOfDayEvent.Starving());
        }
    }

    /// <summary>
    /// Auto-consume medicine for pre-existing serious conditions. Conditions are binary —
    /// one matching medicine clears one condition.
    /// </summary>
    static HashSet<string> ResolveMedicines(PlayerState state, HashSet<string> preExisting,
        BalanceData balance, List<EndOfDayEvent> events)
    {
        var consumed = new List<(int Index, string DefId, string ConditionId)>();

        foreach (var conditionId in state.ActiveConditions)
        {
            if (!preExisting.Contains(conditionId)) continue;

            for (int i = 0; i < state.Haversack.Count; i++)
            {
                var item = state.Haversack[i];
                if (!balance.Items.TryGetValue(item.DefId, out var itemDef)) continue;
                if (!itemDef.Cures.Contains(conditionId)) continue;
                if (consumed.Any(c => c.Index == i)) continue;

                consumed.Add((i, item.DefId, conditionId));
                break;
            }
        }

        var treated = new HashSet<string>();

        foreach (var (idx, defId, conditionId) in consumed.OrderByDescending(c => c.Index))
        {
            state.Haversack.RemoveAt(idx);
            treated.Add(conditionId);

            if (state.ActiveConditions.Remove(conditionId))
            {
                events.Add(new EndOfDayEvent.CureApplied(defId, conditionId));
                events.Add(new EndOfDayEvent.ConditionCured(conditionId));
            }
        }

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
    /// Drains stack — exhausted + thirsty in the desert costs 2 spirits/day.
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
