using Dreamlands.Rules;

namespace Dreamlands.Game;

/// <summary>
/// End-of-day resolution engine. Auto-consumes food and medicine from haversack,
/// resolves ambient conditions, condition drain, and rest recovery.
/// </summary>
public static class EndOfDay
{
    // Conditions that are always checked regardless of biome
    static readonly string[] UniversalAmbientIds = ["exhausted", "lost"];

    // Conditions that only come from encounters, never from ambient resist checks
    static readonly HashSet<string> EncounterOnlyIds = ["poisoned", "injured", "disheartened"];

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
    /// Food and medicine are auto-selected from haversack — no player choices.
    /// </summary>
    public static List<EndOfDayEvent> Resolve(
        PlayerState state, string biome, int tier,
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

        // Snapshot which conditions the player already has entering this rest
        var preExisting = new HashSet<string>(state.ActiveConditions.Keys);

        // 1. Roll resists silently — record pass/fail, do NOT apply new conditions yet
        var resistResults = RollResists(state, biome, tier, noBiome, balance, rng, events);

        // 2. Auto-consume food
        bool balanced = false;
        if (!noMeal)
            balanced = ResolveFood(state, balance, events);

        // 3. Auto-consume medicine (only cures pre-existing conditions)
        ResolveMedicines(state, preExisting, resistResults, balance, events);

        // 4. Apply new conditions from failed resists (for conditions player didn't already have)
        ApplyNewConditions(state, resistResults, balance, events);

        // 5. Condition drain
        string? worstDrainCondition = ResolveConditionDrain(state, balance, events);

        // 6. Rest recovery
        if (!noSleep)
            ResolveRest(state, balanced, balance, events);

        // 7. Evaluate disheartened
        ResolveDisheartened(state, balance, events);

        // 8. Death check
        if (state.Health <= 0)
            events.Add(new EndOfDayEvent.PlayerDied(worstDrainCondition));

        return events;
    }

    /// <summary>
    /// Roll resist checks for all ambient threats. Returns failed condition IDs.
    /// Does NOT mutate state — condition application happens later.
    /// </summary>
    static HashSet<string> RollResists(PlayerState state, string biome, int tier,
        bool noBiome, BalanceData balance, Random rng, List<EndOfDayEvent> events)
    {
        var failed = new HashSet<string>();
        var threats = GetThreats(biome, tier, balance);

        foreach (var threat in threats)
        {
            if (noBiome && threat.Biome != "none")
                continue;

            var dc = threat.ResistDifficulty ?? balance.Character.AmbientResistDifficulty;
            var check = SkillChecks.RollResist(threat.Id, dc, state, balance, rng);

            if (check.Passed)
                events.Add(new EndOfDayEvent.ResistPassed(threat.Id, check));
            else
            {
                failed.Add(threat.Id);
                events.Add(new EndOfDayEvent.ResistFailed(threat.Id, check,
                    state.ActiveConditions.GetValueOrDefault(threat.Id)));
            }
        }

        return failed;
    }

    /// <summary>
    /// Auto-select and consume food from haversack. Tries for balanced meal first
    /// (1 protein + 1 grain + 1 sweets), then fills remaining slots with whatever is available.
    /// </summary>
    static bool ResolveFood(PlayerState state, BalanceData balance, List<EndOfDayEvent> events)
    {
        var eaten = new List<string>();
        bool hasProtein = false, hasGrain = false, hasSweets = false;

        // Pass 1: pick one of each food type for balanced meal
        int? proteinIdx = null, grainIdx = null, sweetsIdx = null;
        for (int i = 0; i < state.Haversack.Count; i++)
        {
            var item = state.Haversack[i];
            if (!balance.Items.TryGetValue(item.DefId, out var def) || def.FoodType == null)
                continue;

            switch (def.FoodType.Value)
            {
                case FoodType.Protein when proteinIdx == null: proteinIdx = i; break;
                case FoodType.Grain when grainIdx == null: grainIdx = i; break;
                case FoodType.Sweets when sweetsIdx == null: sweetsIdx = i; break;
            }
        }

        // Collect indices to consume (balanced first, then fill)
        var toConsume = new List<int>();

        if (proteinIdx != null && grainIdx != null && sweetsIdx != null)
        {
            toConsume.Add(proteinIdx.Value);
            toConsume.Add(grainIdx.Value);
            toConsume.Add(sweetsIdx.Value);
            hasProtein = hasGrain = hasSweets = true;
        }
        else
        {
            // No balanced meal possible — grab up to 3 food items
            for (int i = 0; i < state.Haversack.Count && toConsume.Count < 3; i++)
            {
                var item = state.Haversack[i];
                if (balance.Items.TryGetValue(item.DefId, out var def) && def.FoodType != null)
                {
                    toConsume.Add(i);
                    switch (def.FoodType.Value)
                    {
                        case FoodType.Protein: hasProtein = true; break;
                        case FoodType.Grain: hasGrain = true; break;
                        case FoodType.Sweets: hasSweets = true; break;
                    }
                }
            }
        }

        // Remove consumed items in reverse index order
        foreach (var idx in toConsume.OrderByDescending(i => i))
        {
            eaten.Insert(0, state.Haversack[idx].DisplayName);
            state.Haversack.RemoveAt(idx);
        }

        bool balanced = hasProtein && hasGrain && hasSweets;

        if (eaten.Count > 0)
            events.Add(new EndOfDayEvent.FoodConsumed(eaten, balanced));
        else
            events.Add(new EndOfDayEvent.Starving());

        // Hungry logic: shortage = 3 - foodEaten
        int shortage = 3 - eaten.Count;
        int currentStacks = state.ActiveConditions.GetValueOrDefault("hungry");
        int maxStacks = balance.Conditions.TryGetValue("hungry", out var hungryDef) ? hungryDef.Stacks : 3;

        if (eaten.Count == 3)
        {
            // Full meal: cure 1 stack silently
            if (currentStacks > 0)
            {
                var newStacks = currentStacks - 1;
                if (newStacks <= 0)
                {
                    state.ActiveConditions.Remove("hungry");
                    events.Add(new EndOfDayEvent.HungerCured());
                }
                else
                {
                    state.ActiveConditions["hungry"] = newStacks;
                    events.Add(new EndOfDayEvent.HungerChanged(newStacks));
                }
            }
        }
        else if (shortage > currentStacks)
        {
            // Shortage exceeds current stacks: set stacks to shortage
            var newStacks = Math.Min(shortage, maxStacks);
            state.ActiveConditions["hungry"] = newStacks;
            if (newStacks != currentStacks)
                events.Add(new EndOfDayEvent.HungerChanged(newStacks));
        }
        // Otherwise: no change to hungry stacks

        return balanced;
    }

    /// <summary>
    /// Auto-consume medicine from haversack for pre-existing active conditions.
    /// CureNegated if that condition also failed resist AND player already had it.
    /// </summary>
    static void ResolveMedicines(PlayerState state, HashSet<string> preExisting,
        HashSet<string> failedResists, BalanceData balance, List<EndOfDayEvent> events)
    {
        // Find all haversack items that cure an active pre-existing condition
        var consumed = new List<(int Index, string DefId, string ConditionId)>();

        foreach (var (conditionId, _) in state.ActiveConditions)
        {
            if (!preExisting.Contains(conditionId)) continue;

            for (int i = 0; i < state.Haversack.Count; i++)
            {
                var item = state.Haversack[i];
                if (!balance.Items.TryGetValue(item.DefId, out var itemDef)) continue;
                if (!itemDef.Cures.Contains(conditionId)) continue;

                // Don't double-consume the same haversack slot
                if (consumed.Any(c => c.Index == i)) continue;

                consumed.Add((i, item.DefId, conditionId));
                break; // one medicine per condition per night
            }
        }

        // Remove items in reverse index order, then apply effects
        foreach (var (idx, defId, conditionId) in consumed.OrderByDescending(c => c.Index))
        {
            state.Haversack.RemoveAt(idx);

            // CureNegated: condition failed resist tonight AND player already had it
            if (failedResists.Contains(conditionId) && preExisting.Contains(conditionId))
            {
                events.Add(new EndOfDayEvent.CureNegated(defId, conditionId));
                continue;
            }

            if (!state.ActiveConditions.TryGetValue(conditionId, out var stacks))
                continue;

            var newStacks = stacks - 1;
            if (newStacks <= 0)
            {
                state.ActiveConditions.Remove(conditionId);
                events.Add(new EndOfDayEvent.CureApplied(defId, conditionId, 1, 0));
                events.Add(new EndOfDayEvent.ConditionCured(conditionId));
            }
            else
            {
                state.ActiveConditions[conditionId] = newStacks;
                events.Add(new EndOfDayEvent.CureApplied(defId, conditionId, 1, newStacks));
            }
        }
    }

    /// <summary>
    /// Apply new conditions from failed resists — only for conditions the player didn't already have.
    /// </summary>
    static void ApplyNewConditions(PlayerState state, HashSet<string> failedResists,
        BalanceData balance, List<EndOfDayEvent> events)
    {
        foreach (var conditionId in failedResists)
        {
            if (state.ActiveConditions.ContainsKey(conditionId)) continue;

            if (balance.Conditions.TryGetValue(conditionId, out var def))
            {
                state.ActiveConditions[conditionId] = def.Stacks;
                events.Add(new EndOfDayEvent.ConditionAcquired(conditionId, def.Stacks));
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

    static void ResolveDisheartened(PlayerState state, BalanceData balance, List<EndOfDayEvent> events)
    {
        var threshold = balance.Character.SpiritDisadvantageThreshold;
        var has = state.ActiveConditions.ContainsKey("disheartened");

        if (state.Spirits < threshold && !has)
        {
            state.ActiveConditions["disheartened"] = 1;
            events.Add(new EndOfDayEvent.DisheartendGained());
        }
        else if (state.Spirits >= threshold && has)
        {
            state.ActiveConditions.Remove("disheartened");
            events.Add(new EndOfDayEvent.DisheartendCleared());
        }
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
