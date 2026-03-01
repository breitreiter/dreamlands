using Dreamlands.Rules;

namespace Dreamlands.Game;

public record InnQuote(int Nights, int GoldCost, int HealthRecovered, int SpiritsRecovered);

public record InnResult(int NightsStayed, int GoldSpent, int HealthRecovered, int SpiritsRecovered,
    List<string> ConditionsCleared, List<string> MedicinesConsumed);

public static class Inn
{
    /// <summary>
    /// Check whether the player can use the inn for a full recovery.
    /// Blocked if any active condition has HealthDrain, is NOT ClearedOnSettlement,
    /// and the player lacks enough matching Cures items to cover all stacks.
    /// </summary>
    public static (bool Allowed, List<string> DisqualifyingConditions) CanUseInn(
        PlayerState state, BalanceData balance)
    {
        var disqualifying = new List<string>();

        foreach (var (conditionId, stacks) in state.ActiveConditions)
        {
            if (!balance.Conditions.TryGetValue(conditionId, out var def)) continue;
            if (def.HealthDrain == null) continue;
            if (def.ClearedOnSettlement) continue;

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
    /// Calculate the cost and duration of a full-recovery inn stay.
    /// Accounts for condition drain during multi-night stays: each night the player
    /// gets base rest recovery (+1/+1) but conditions drain health/spirits.
    /// Medicines consumed 1/night as applicable.
    /// </summary>
    public static InnQuote GetQuote(PlayerState state, BalanceData balance)
    {
        // Simulate nights to find how many it takes to reach full
        var simHealth = state.Health;
        var simSpirits = state.Spirits;

        // Build drain-per-night from active conditions
        int healthDrain = 0, spiritsDrain = 0;
        var conditionStacks = new Dictionary<string, int>(state.ActiveConditions);

        foreach (var (conditionId, _) in conditionStacks)
        {
            if (!balance.Conditions.TryGetValue(conditionId, out var def)) continue;
            if (def.HealthDrain is { } hMag)
                healthDrain += balance.Character.DamageMagnitudes.GetValueOrDefault(hMag, 0);
            if (def.SpiritsDrain is { } sMag)
                spiritsDrain += balance.Character.DamageMagnitudes.GetValueOrDefault(sMag, 0);
        }

        // Count available medicines per condition
        var curesAvailable = new Dictionary<string, int>();
        foreach (var item in state.Haversack)
        {
            if (!balance.Items.TryGetValue(item.DefId, out var itemDef)) continue;
            foreach (var condId in itemDef.Cures)
            {
                if (conditionStacks.ContainsKey(condId))
                    curesAvailable[condId] = curesAvailable.GetValueOrDefault(condId) + 1;
            }
        }

        // Inn recovery: base rest only (no balanced meal bonus — food included in price but not a triad)
        var baseHealthGain = balance.Character.BaseRestHealth;
        var baseSpiritsGain = balance.Character.BaseRestSpirits;

        int nights = 0;
        const int maxNights = 100; // safety cap
        while (nights < maxNights &&
               (simHealth < state.MaxHealth || simSpirits < state.MaxSpirits))
        {
            nights++;

            // Consume medicines this night (reduces drain for subsequent nights)
            foreach (var condId in conditionStacks.Keys.ToList())
            {
                if (curesAvailable.GetValueOrDefault(condId) <= 0) continue;
                curesAvailable[condId]--;
                conditionStacks[condId]--;

                if (conditionStacks[condId] <= 0)
                {
                    conditionStacks.Remove(condId);
                    // Recalculate drain
                    healthDrain = 0;
                    spiritsDrain = 0;
                    foreach (var (cid, _) in conditionStacks)
                    {
                        if (!balance.Conditions.TryGetValue(cid, out var d)) continue;
                        if (d.HealthDrain is { } h)
                            healthDrain += balance.Character.DamageMagnitudes.GetValueOrDefault(h, 0);
                        if (d.SpiritsDrain is { } s)
                            spiritsDrain += balance.Character.DamageMagnitudes.GetValueOrDefault(s, 0);
                    }
                }
            }

            // Apply drain then recovery
            simHealth = Math.Max(0, simHealth - healthDrain);
            simSpirits = Math.Max(0, simSpirits - spiritsDrain);
            simHealth = Math.Min(state.MaxHealth, simHealth + baseHealthGain);
            simSpirits = Math.Min(state.MaxSpirits, simSpirits + baseSpiritsGain);
        }

        nights = Math.Max(nights, 1);
        var healthRecovered = state.MaxHealth - state.Health;
        var spiritsRecovered = state.MaxSpirits - state.Spirits;

        // First night free, remaining nights at InnNightlyCost each
        var goldCost = Math.Max(0, nights - 1) * balance.Character.InnNightlyCost;

        return new InnQuote(nights, goldCost, healthRecovered, spiritsRecovered);
    }

    /// <summary>
    /// Stay one night at the inn. Triggers a normal end-of-day with PendingNoBiome
    /// (inn shelters from ambient threats) and clears exhausted.
    /// </summary>
    public static List<EndOfDayEvent> StayOneNight(
        PlayerState state, string biome, int tier,
        BalanceData balance, Random rng)
    {
        state.PendingNoBiome = true;
        state.PendingEndOfDay = true;

        var events = EndOfDay.Resolve(state, biome, tier, balance, rng);

        // Inn clears exhausted
        if (state.ActiveConditions.Remove("exhausted"))
            events.Add(new EndOfDayEvent.ConditionCured("exhausted"));

        return events;
    }

    /// <summary>
    /// Full recovery inn stay. Deduct gold, consume all medicine for treatable conditions,
    /// clear ClearedOnSettlement conditions, restore to max, advance days per quote.
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

        // Consume medicines for treatable conditions (1 per stack)
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
