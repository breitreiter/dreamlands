using Dreamlands.Rules;

namespace Dreamlands.Game;

public record InnQuote(int Nights, int GoldCost, int HealthRecovered, int SpiritsRecovered);

public record InnResult(int NightsStayed, int GoldSpent, int HealthRecovered, int SpiritsRecovered,
    List<string> ConditionsCleared, List<string> MedicinesConsumed);

public static class Inn
{
    /// <summary>
    /// Check whether the player can use an inn. Disqualified if any active condition
    /// has HealthDrain, is NOT ClearedOnSettlement, and the player lacks a matching
    /// Cures item in the Haversack.
    /// </summary>
    public static (bool Allowed, List<string> DisqualifyingConditions) CanUseInn(
        PlayerState state, BalanceData balance)
    {
        var disqualifying = new List<string>();

        foreach (var (conditionId, _) in state.ActiveConditions)
        {
            if (!balance.Conditions.TryGetValue(conditionId, out var def)) continue;
            if (def.HealthDrain == null) continue;
            if (def.ClearedOnSettlement) continue;

            // Check if player has a medicine that cures this condition
            bool hasCure = false;
            foreach (var item in state.Haversack)
            {
                if (balance.Items.TryGetValue(item.DefId, out var itemDef)
                    && itemDef.Cures.Contains(conditionId))
                {
                    hasCure = true;
                    break;
                }
            }

            if (!hasCure)
                disqualifying.Add(conditionId);
        }

        return (disqualifying.Count == 0, disqualifying);
    }

    /// <summary>
    /// Calculate the cost and duration of a full-recovery inn stay.
    /// </summary>
    public static InnQuote GetQuote(PlayerState state, BalanceData balance)
    {
        var healthDeficit = state.MaxHealth - state.Health;
        var spiritsDeficit = state.MaxSpirits - state.Spirits;
        var nights = Math.Max(healthDeficit, spiritsDeficit);
        nights = Math.Max(nights, 1); // at least one night

        // First night free, remaining nights at InnNightlyCost each
        var goldCost = Math.Max(0, nights - 1) * balance.Character.InnNightlyCost;

        return new InnQuote(nights, goldCost, healthDeficit, spiritsDeficit);
    }

    /// <summary>
    /// Stay one night at the inn. Triggers a normal end-of-day with PendingNoBiome
    /// (inn shelters from ambient threats) and clears exhausted.
    /// </summary>
    public static List<EndOfDayEvent> StayOneNight(
        PlayerState state, string biome, int tier,
        List<string> foodDefIds, List<string> medicineDefIds,
        BalanceData balance, Random rng)
    {
        state.PendingNoBiome = true;
        state.PendingEndOfDay = true;

        var events = EndOfDay.Resolve(state, biome, tier, foodDefIds, medicineDefIds, balance, rng);

        // Inn clears exhausted (its SpecialCure is "Rest in an inn.")
        if (state.ActiveConditions.Remove("exhausted"))
            events.Add(new EndOfDayEvent.ConditionCured("exhausted"));

        return events;
    }

    /// <summary>
    /// Full recovery inn stay. Single step: deduct gold, restore health/spirits to max,
    /// clear exhausted, consume medicines for treatable conditions, advance day counter.
    /// No per-night EndOfDay loop. Food is included in the inn price.
    /// </summary>
    public static InnResult StayFullRecovery(PlayerState state, BalanceData balance)
    {
        var quote = GetQuote(state, balance);

        state.Gold -= quote.GoldCost;
        state.Health = state.MaxHealth;
        state.Spirits = state.MaxSpirits;
        state.Day += quote.Nights;

        var conditionsCleared = new List<string>();
        var medicinesConsumed = new List<string>();

        // Clear exhausted
        if (state.ActiveConditions.Remove("exhausted"))
            conditionsCleared.Add("exhausted");

        // Consume medicines for treatable conditions (no failed-resist negation)
        ConsumeMedicines(state, balance, conditionsCleared, medicinesConsumed);

        return new InnResult(quote.Nights, quote.GoldCost,
            quote.HealthRecovered, quote.SpiritsRecovered,
            conditionsCleared, medicinesConsumed);
    }

    /// <summary>
    /// Full recovery chapterhouse stay. Clears ALL conditions, no gold cost,
    /// no medicine consumed. Time cost still applies.
    /// </summary>
    public static InnResult StayChapterhouse(PlayerState state, BalanceData balance)
    {
        var quote = GetQuote(state, balance);

        state.Health = state.MaxHealth;
        state.Spirits = state.MaxSpirits;
        state.Day += quote.Nights;

        var conditionsCleared = state.ActiveConditions.Keys.ToList();
        state.ActiveConditions.Clear();

        return new InnResult(quote.Nights, 0,
            quote.HealthRecovered, quote.SpiritsRecovered,
            conditionsCleared, []);
    }

    static void ConsumeMedicines(PlayerState state, BalanceData balance,
        List<string> conditionsCleared, List<string> medicinesConsumed)
    {
        // Find all active conditions that have a matching cure in haversack
        var curable = new List<(string ConditionId, int ItemIndex, string ItemDefId)>();

        foreach (var (conditionId, _) in state.ActiveConditions)
        {
            if (!balance.Conditions.TryGetValue(conditionId, out var def)) continue;
            if (def.ClearedOnSettlement) continue; // these clear on their own

            for (int i = 0; i < state.Haversack.Count; i++)
            {
                var item = state.Haversack[i];
                if (balance.Items.TryGetValue(item.DefId, out var itemDef)
                    && itemDef.Cures.Contains(conditionId))
                {
                    curable.Add((conditionId, i, item.DefId));
                    break;
                }
            }
        }

        // Process in reverse index order so removals don't shift earlier indices
        foreach (var (conditionId, _, defId) in curable.OrderByDescending(c => c.ItemIndex))
        {
            // Re-find index since earlier removals may have shifted things
            var idx = state.Haversack.FindIndex(i => i.DefId == defId);
            if (idx < 0) continue;

            state.Haversack.RemoveAt(idx);
            medicinesConsumed.Add(defId);

            var stacks = state.ActiveConditions.GetValueOrDefault(conditionId);
            var newStacks = stacks - 1;
            if (newStacks <= 0)
            {
                state.ActiveConditions.Remove(conditionId);
                conditionsCleared.Add(conditionId);
            }
            else
            {
                state.ActiveConditions[conditionId] = newStacks;
            }
        }

        // Clear conditions that are ClearedOnSettlement
        foreach (var (conditionId, _) in state.ActiveConditions.ToList())
        {
            if (balance.Conditions.TryGetValue(conditionId, out var def) && def.ClearedOnSettlement)
            {
                state.ActiveConditions.Remove(conditionId);
                conditionsCleared.Add(conditionId);
            }
        }
    }
}
