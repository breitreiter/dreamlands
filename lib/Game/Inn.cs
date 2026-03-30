using Dreamlands.Rules;

namespace Dreamlands.Game;

public record InnQuote(int Nights, int GoldCost, int HealthRecovered, int SpiritsRecovered);

public record InnResult(int NightsStayed, int GoldSpent, int HealthRecovered, int SpiritsRecovered,
    List<string> ConditionsCleared, List<string> MedicinesConsumed);

public static class Inn
{
    /// <summary>
    /// Check whether the player can use the inn for full recovery.
    /// Blocked if any active severe condition has insufficient matching medicines
    /// to cover all stacks.
    /// </summary>
    public static (bool Allowed, List<string> DisqualifyingConditions) CanUseInn(
        PlayerState state, BalanceData balance)
    {
        var disqualifying = new List<string>();

        foreach (var (conditionId, stacks) in state.ActiveConditions)
        {
            if (!balance.Conditions.TryGetValue(conditionId, out var def)) continue;
            if (def.Severity != ConditionSeverity.Severe) continue;

            // Count available cures in haversack for this condition
            int cureCount = 0;
            foreach (var item in state.Haversack)
            {
                if (balance.Items.TryGetValue(item.DefId, out var itemDef)
                    && itemDef.Cures.Contains(conditionId))
                    cureCount++;
            }

            if (cureCount < stacks)
                disqualifying.Add(conditionId);
        }

        return (disqualifying.Count == 0, disqualifying);
    }

    /// <summary>
    /// Calculate the cost of a full-recovery inn stay.
    /// Nights = max(maxHealth - health, ceil((maxSpirits - spirits) / 2)), minimum 1.
    /// Cost = nights × InnNightlyCost.
    /// </summary>
    public static InnQuote GetQuote(PlayerState state, BalanceData balance)
    {
        var healthDeficit = state.MaxHealth - state.Health;
        var spiritsDeficit = state.MaxSpirits - state.Spirits;
        var spiritsNights = (spiritsDeficit + 1) / 2; // ceiling division

        var nights = Math.Max(Math.Max(healthDeficit, spiritsNights), 1);
        var goldCost = nights * balance.Character.InnNightlyCost;

        return new InnQuote(nights, goldCost,
            state.MaxHealth - state.Health,
            state.MaxSpirits - state.Spirits);
    }

    /// <summary>
    /// Full recovery inn stay. Deduct gold, consume all medicine for severe conditions,
    /// clear all other conditions, restore to max, advance days per quote.
    /// </summary>
    public static InnResult StayAtInn(PlayerState state, BalanceData balance)
    {
        var quote = GetQuote(state, balance);

        state.Gold -= quote.GoldCost;
        state.Health = state.MaxHealth;
        state.Spirits = state.MaxSpirits;
        state.Day += quote.Nights;

        var conditionsCleared = new List<string>();
        var medicinesConsumed = new List<string>();

        // Clear exhausted (spirits now at max)
        if (state.ActiveConditions.Remove("exhausted"))
            conditionsCleared.Add("exhausted");

        // Consume medicines for severe conditions (1 per stack)
        ConsumeMedicines(state, balance, conditionsCleared, medicinesConsumed);

        return new InnResult(quote.Nights, quote.GoldCost,
            quote.HealthRecovered, quote.SpiritsRecovered,
            conditionsCleared, medicinesConsumed);
    }

    /// <summary>
    /// Full recovery chapterhouse stay. Clears ALL conditions, no gold cost,
    /// no medicine consumed.
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
        // Consume enough medicine to cure all stacks (1 per stack)
        foreach (var (conditionId, stacks) in state.ActiveConditions.ToList())
        {
            if (!balance.Conditions.TryGetValue(conditionId, out var def)) continue;
            if (def.ClearedOnSettlement) continue;

            var remaining = stacks;
            while (remaining > 0)
            {
                var idx = state.Haversack.FindIndex(i =>
                    balance.Items.TryGetValue(i.DefId, out var itemDef)
                    && itemDef.Cures.Contains(conditionId));

                if (idx < 0) break;

                medicinesConsumed.Add(state.Haversack[idx].DefId);
                state.Haversack.RemoveAt(idx);
                remaining--;
            }

            if (remaining <= 0)
            {
                state.ActiveConditions.Remove(conditionId);
                conditionsCleared.Add(conditionId);
            }
            else
            {
                state.ActiveConditions[conditionId] = remaining;
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
