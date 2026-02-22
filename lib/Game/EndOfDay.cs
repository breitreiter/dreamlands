using Dreamlands.Rules;

namespace Dreamlands.Game;

/// <summary>
/// End-of-day resolution engine. Resolves food, ambient conditions, medicine,
/// condition drain, and rest recovery when the day counter increments.
/// </summary>
public static class EndOfDay
{
    // Conditions that are always checked regardless of biome
    static readonly string[] UniversalAmbientIds = ["lost"];

    // Conditions that only come from encounters, never from ambient resist checks
    static readonly HashSet<string> EncounterOnlyIds = ["poisoned", "injured", "exhausted"];

    /// <summary>
    /// Returns ambient conditions that threaten the player tonight based on camping biome/tier.
    /// </summary>
    public static List<ConditionDef> GetThreats(string biome, int tier, BalanceData balance)
    {
        var threats = new List<ConditionDef>();

        foreach (var def in balance.Conditions.Values)
        {
            if (def.Id == "hungry") continue;
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
    /// Execute the full end-of-day resolution sequence.
    /// Reads and clears PendingNoSleep/PendingNoMeal/PendingNoBiome flags from PlayerState.
    /// </summary>
    public static List<EndOfDayEvent> Resolve(
        PlayerState state, string biome, int tier,
        List<string> foodDefIds, List<string> medicineDefIds,
        BalanceData balance, Random rng)
    {
        var events = new List<EndOfDayEvent>();

        // Read and clear pending flags
        var noSleep = state.PendingNoSleep;
        var noMeal = state.PendingNoMeal;
        var noBiome = state.PendingNoBiome;
        state.PendingEndOfDay = false;
        state.PendingNoSleep = false;
        state.PendingNoMeal = false;
        state.PendingNoBiome = false;

        // 1. Consume food
        bool balanced = false;
        if (!noMeal)
            balanced = ResolveFood(state, foodDefIds, balance, events);

        // 2. Ambient resist checks
        var failedResists = new HashSet<string>();
        ResolveAmbientResists(state, biome, tier, noBiome, balance, rng, events, failedResists);

        // 3. Apply medicines
        ResolveMedicines(state, medicineDefIds, failedResists, balance, events);

        // 4. Condition drain
        string? worstDrainCondition = ResolveConditionDrain(state, balance, events);

        // 5. Rest recovery
        if (!noSleep)
            ResolveRest(state, balanced, balance, events);

        // 6. Death check
        if (state.Health <= 0)
            events.Add(new EndOfDayEvent.PlayerDied(worstDrainCondition));

        return events;
    }

    static bool ResolveFood(PlayerState state, List<string> foodDefIds,
        BalanceData balance, List<EndOfDayEvent> events)
    {
        // Remove selected food items from Haversack (up to 3)
        var eaten = new List<string>();
        bool hasProtein = false, hasGrain = false, hasSweets = false;

        var toEat = foodDefIds.Take(3).ToList();
        foreach (var defId in toEat)
        {
            var idx = state.Haversack.FindIndex(i => i.DefId == defId);
            if (idx < 0) continue;

            var item = state.Haversack[idx];
            state.Haversack.RemoveAt(idx);
            eaten.Add(item.DisplayName);

            // Track food types for balanced meal check
            if (balance.Items.TryGetValue(defId, out var def) && def.FoodType != null)
            {
                switch (def.FoodType.Value)
                {
                    case FoodType.Protein: hasProtein = true; break;
                    case FoodType.Grain: hasGrain = true; break;
                    case FoodType.Sweets: hasSweets = true; break;
                }
            }
        }

        bool balanced = hasProtein && hasGrain && hasSweets;

        if (eaten.Count == 0)
        {
            // Starving — apply Hungry if not already active
            events.Add(new EndOfDayEvent.Starving());
            if (!state.ActiveConditions.ContainsKey("hungry"))
            {
                var stacks = balance.Conditions.TryGetValue("hungry", out var def) ? def.Stacks : 2;
                state.ActiveConditions["hungry"] = stacks;
            }
        }
        else
        {
            events.Add(new EndOfDayEvent.FoodConsumed(eaten, balanced));

            // If Hungry is active, reduce stacks
            if (state.ActiveConditions.TryGetValue("hungry", out var hungerStacks))
            {
                hungerStacks--;
                if (hungerStacks <= 0)
                {
                    state.ActiveConditions.Remove("hungry");
                    events.Add(new EndOfDayEvent.HungerCured());
                }
                else
                {
                    state.ActiveConditions["hungry"] = hungerStacks;
                    events.Add(new EndOfDayEvent.HungerReduced(hungerStacks));
                }
            }
        }

        return balanced;
    }

    static void ResolveAmbientResists(PlayerState state, string biome, int tier,
        bool noBiome, BalanceData balance, Random rng,
        List<EndOfDayEvent> events, HashSet<string> failedResists)
    {
        var threats = GetThreats(biome, tier, balance);

        foreach (var threat in threats)
        {
            // Skip biome-specific checks if noBiome flag is set
            if (noBiome && threat.Biome != "none")
                continue;

            var dc = threat.ResistDifficulty ?? balance.Character.AmbientResistDifficulty;
            var check = SkillChecks.RollResist(
                threat.Id, dc,
                state, balance, rng);

            if (check.Passed)
            {
                events.Add(new EndOfDayEvent.ResistPassed(threat.Id, check));
            }
            else
            {
                failedResists.Add(threat.Id);

                if (!state.ActiveConditions.ContainsKey(threat.Id))
                    state.ActiveConditions[threat.Id] = threat.Stacks;

                events.Add(new EndOfDayEvent.ResistFailed(threat.Id, check,
                    state.ActiveConditions[threat.Id]));
            }
        }
    }

    static void ResolveMedicines(PlayerState state, List<string> medicineDefIds,
        HashSet<string> failedResists, BalanceData balance, List<EndOfDayEvent> events)
    {
        foreach (var defId in medicineDefIds)
        {
            var idx = state.Haversack.FindIndex(i => i.DefId == defId);
            if (idx < 0) continue;

            if (!balance.Items.TryGetValue(defId, out var itemDef)) continue;
            if (itemDef.Cures.Count == 0) continue;

            // Remove medicine from Haversack
            state.Haversack.RemoveAt(idx);

            foreach (var (conditionId, cureMagnitude) in itemDef.Cures)
            {
                // If this condition had a failed resist tonight, cure is negated
                if (failedResists.Contains(conditionId))
                {
                    events.Add(new EndOfDayEvent.CureNegated(defId, conditionId));
                    continue;
                }

                if (!state.ActiveConditions.TryGetValue(conditionId, out var stacks))
                    continue;

                var cureAmount = balance.Character.DamageMagnitudes.GetValueOrDefault(cureMagnitude, 1);
                var newStacks = stacks - cureAmount;

                if (newStacks <= 0)
                {
                    state.ActiveConditions.Remove(conditionId);
                    events.Add(new EndOfDayEvent.CureApplied(defId, conditionId, stacks, 0));
                    events.Add(new EndOfDayEvent.ConditionCured(conditionId));
                }
                else
                {
                    state.ActiveConditions[conditionId] = newStacks;
                    events.Add(new EndOfDayEvent.CureApplied(defId, conditionId, cureAmount, newStacks));
                }
            }
        }
    }

    static string? ResolveConditionDrain(PlayerState state, BalanceData balance,
        List<EndOfDayEvent> events)
    {
        string? worstCondition = null;
        int worstDrain = 0;

        foreach (var (conditionId, _) in state.ActiveConditions)
        {
            if (!balance.Conditions.TryGetValue(conditionId, out var def)) continue;

            int healthLost = 0, spiritsLost = 0;

            if (def.HealthDrain is { } hMag)
                healthLost = balance.Character.DamageMagnitudes.GetValueOrDefault(hMag, 0);
            if (def.SpiritsDrain is { } sMag)
                spiritsLost = balance.Character.DamageMagnitudes.GetValueOrDefault(sMag, 0);

            if (healthLost > 0 || spiritsLost > 0)
            {
                state.Health = Math.Max(0, state.Health - healthLost);
                state.Spirits = Math.Max(0, state.Spirits - spiritsLost);
                events.Add(new EndOfDayEvent.ConditionDrain(conditionId, healthLost, spiritsLost));

                if (healthLost > worstDrain)
                {
                    worstDrain = healthLost;
                    worstCondition = conditionId;
                }
            }

            if (def.SpecialEffect != null)
                events.Add(new EndOfDayEvent.SpecialEffect(conditionId, def.SpecialEffect));
        }

        return worstCondition;
    }

    static void ResolveRest(PlayerState state, bool balanced,
        BalanceData balance, List<EndOfDayEvent> events)
    {
        var healthGain = balance.Character.BaseRestHealth;
        var spiritsGain = balance.Character.BaseRestSpirits;

        if (balanced)
        {
            healthGain += balance.Character.BalancedMealHealthBonus;
            spiritsGain += balance.Character.BalancedMealSpiritsBonus;
        }

        state.Health = Math.Min(state.MaxHealth, state.Health + healthGain);
        state.Spirits = Math.Min(state.MaxSpirits, state.Spirits + spiritsGain);

        events.Add(new EndOfDayEvent.RestRecovery(healthGain, spiritsGain));
    }
}
