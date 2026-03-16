using Dreamlands.Rules;

namespace Dreamlands.Game;

/// <summary>
/// End-of-day resolution engine. Auto-consumes food and medicine from haversack,
/// resolves ambient conditions, condition drain, and rest recovery.
/// Health never recovers at camp — only at inn/chapterhouse.
/// </summary>
public static class EndOfDay
{
    // Conditions that are always checked regardless of biome
    static readonly string[] UniversalAmbientIds = ["exhausted", "lost"];

    // Conditions that only come from encounters, never from ambient resist checks
    static readonly HashSet<string> EncounterOnlyIds = ["poisoned", "injured", "disheartened", "irradiated", "lattice_sickness"];

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
    /// Execute the full end-of-day resolution sequence.
    /// Food and medicine are auto-selected from haversack — no player choices.
    /// </summary>
    public static List<EndOfDayEvent> Resolve(
        PlayerState state, string biome, int tier,
        BalanceData balance, Random rng,
        int startX = 0, int startY = 0,
        Func<FoodType, Random, ItemInstance>? createFood = null)
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

        // 2. Forage for food (wilderness only)
        ResolveForaging(state, noBiome, balance, rng, createFood, events);

        // 3. Auto-consume food
        bool ate = false, balanced = false;
        if (!noMeal)
            (ate, balanced) = ResolveFood(state, balance, events);

        // 4. Auto-consume medicine (only cures pre-existing conditions)
        var treatedConditions = ResolveMedicines(state, preExisting, balance, events);

        // 5. Apply new conditions from failed resists (for conditions player didn't already have)
        ApplyNewConditions(state, resistResults, balance, events);

        // 6. Condition drain — spirits from minor conditions, health from untreated severe
        string? worstDrainCondition = ResolveConditionDrain(state, treatedConditions, balance, events);

        // 7. Death check — rescue instead of permadeath
        if (state.Health <= 0)
        {
            events.Add(new EndOfDayEvent.PlayerDied(worstDrainCondition));
            var rescue = Rescue.Apply(state, startX, startY, balance);
            events.Add(new EndOfDayEvent.PlayerRescued(rescue.LostItems, rescue.GoldLost));
            return events;
        }

        // 8. Rest recovery (spirits only — health never recovers at camp)
        if (!noSleep && ate)
            ResolveRest(state, balanced, balance, events);

        // 9. Evaluate disheartened
        ResolveDisheartened(state, balance, events);

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

        if (noBiome) return failed;

        foreach (var threat in threats)
        {
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
    /// Automatic foraging: roll bushcraft + gear bonus against 3 DC thresholds.
    /// Yield 0-3 food items added to haversack. Skipped at settlements (noBiome).
    /// </summary>
    static void ResolveForaging(PlayerState state, bool noBiome,
        BalanceData balance, Random rng,
        Func<FoodType, Random, ItemInstance>? createFood,
        List<EndOfDayEvent> events)
    {
        if (noBiome) return;

        var skillLevel = state.Skills.GetValueOrDefault(Skill.Bushcraft);
        var itemBonus = SkillChecks.GetForagingBonus(state, balance);
        var modifier = skillLevel + itemBonus;

        var rollMode = state.ActiveConditions.ContainsKey("disheartened")
            ? RollMode.Disadvantage : RollMode.Normal;
        var natural = SkillChecks.RollD20(rollMode, rng);
        var total = natural + modifier;

        var dc = balance.Character;
        int yield = total >= dc.ForageDC3 ? 3
                  : total >= dc.ForageDC2 ? 2
                  : total >= dc.ForageDC1 ? 1
                  : 0;

        var itemsFound = new List<string>();
        var foodTypes = new[] { FoodType.Protein, FoodType.Grain, FoodType.Sweets };

        for (int i = 0; i < yield; i++)
        {
            var type = foodTypes[i % foodTypes.Length];
            var item = createFood?.Invoke(type, rng)
                ?? new ItemInstance($"food_{type.ToString().ToLowerInvariant()}", type.ToString())
                    { FoodType = type };
            state.Haversack.Add(item);
            itemsFound.Add(item.DisplayName);
        }

        events.Add(new EndOfDayEvent.Foraged(total, modifier, itemsFound));
    }

    /// <summary>
    /// Auto-select and consume food from haversack. Tries for balanced meal first
    /// (1 protein + 1 grain + 1 sweets), then fills remaining slots with whatever is available.
    /// </summary>
    static (bool Ate, bool Balanced) ResolveFood(PlayerState state, BalanceData balance, List<EndOfDayEvent> events)
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

        return (eaten.Count > 0, balanced);
    }

    /// <summary>
    /// Auto-consume medicine from haversack for pre-existing active conditions.
    /// One medicine per condition per night, reduces stacks by 1.
    /// </summary>
    static HashSet<string> ResolveMedicines(PlayerState state, HashSet<string> preExisting,
        BalanceData balance, List<EndOfDayEvent> events)
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

        var treated = new HashSet<string>();

        // Remove items in reverse index order, then apply effects
        foreach (var (idx, defId, conditionId) in consumed.OrderByDescending(c => c.Index))
        {
            state.Haversack.RemoveAt(idx);
            treated.Add(conditionId);

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

        return treated;
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

    /// <summary>
    /// Resolve condition effects at end of day.
    /// Spirits drain: per-condition SpiritsDrain as before.
    /// Health drain: if ANY severe condition is active (after medicine), lose 1 HP total.
    /// </summary>
    static string? ResolveConditionDrain(PlayerState state, HashSet<string> treatedConditions,
        BalanceData balance, List<EndOfDayEvent> events)
    {
        string? worstCondition = null;
        bool hasUntreatedSevere = false;

        foreach (var (conditionId, _) in state.ActiveConditions)
        {
            if (!balance.Conditions.TryGetValue(conditionId, out var def)) continue;

            // Spirits drain still applies per-condition
            if (def.SpiritsDrain is { } sDrain)
            {
                state.Spirits = Math.Max(0, state.Spirits - sDrain);
                events.Add(new EndOfDayEvent.ConditionDrain(conditionId, 0, sDrain));
            }

            // Track whether any severe condition is active and untreated
            if (def.Severity == ConditionSeverity.Severe && !treatedConditions.Contains(conditionId))
            {
                hasUntreatedSevere = true;
                worstCondition ??= conditionId;
            }

            if (def.SpecialEffect != null)
                events.Add(new EndOfDayEvent.SpecialEffect(conditionId, def.SpecialEffect));
        }

        // Binary health drain: only if a severe condition got NO medicine at all
        if (hasUntreatedSevere)
        {
            state.Health = Math.Max(0, state.Health - 1);
            events.Add(new EndOfDayEvent.ConditionDrain(worstCondition!, 1, 0));
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
        var spiritsGain = balance.Character.BaseRestSpirits;

        if (balanced)
            spiritsGain += balance.Character.BalancedMealSpiritsBonus;

        state.Spirits = Math.Min(state.MaxSpirits, state.Spirits + spiritsGain);

        events.Add(new EndOfDayEvent.RestRecovery(0, spiritsGain));
    }
}
