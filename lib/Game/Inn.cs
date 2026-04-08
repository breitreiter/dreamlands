using Dreamlands.Rules;

namespace Dreamlands.Game;

public record InnService(string Id, string Name, int Cost, int Spirits, bool RestoresFull);

public record InnBookingResult(
    bool Success,
    string? Reason,
    string ServiceId,
    int GoldSpent,
    int SpiritsRestored,
    List<string> MedicinesConsumed);

public static class Inn
{
    public const string BedServiceId = "bed";
    public const string BathServiceId = "bath";
    public const string FullServiceId = "full";

    /// <summary>
    /// Returns the three inn service tiers from balance data. The bed/bath tiers
    /// add a fixed amount of spirits; the full tier restores spirits to max.
    /// All tiers consume serious-condition medicine if carried (minor conditions
    /// were already cleared by SettlementRunner.EnsureSettlement on entry).
    /// </summary>
    public static IReadOnlyList<InnService> GetServiceOptions(BalanceData balance) =>
    [
        new(BedServiceId,  "A bed for the night",        balance.Character.InnBedCost,  balance.Character.InnBedSpirits,  RestoresFull: false),
        new(BathServiceId, "Bed + hot bath",             balance.Character.InnBathCost, balance.Character.InnBathSpirits, RestoresFull: false),
        new(FullServiceId, "Bed, bath, evening drinks",  balance.Character.InnFullCost, 0,                                RestoresFull: true),
    ];

    /// <summary>
    /// Book a single inn service. Validates affordability, deducts gold, restores
    /// spirits per the tier, advances time by one night, and consumes any matching
    /// serious-condition medicines.
    /// </summary>
    public static InnBookingResult BookService(
        PlayerState state, BalanceData balance, string serviceId)
    {
        var service = GetServiceOptions(balance).FirstOrDefault(s => s.Id == serviceId);
        if (service == null)
            return new InnBookingResult(false, $"Unknown service '{serviceId}'", serviceId, 0, 0, []);

        if (state.Gold < service.Cost)
            return new InnBookingResult(false, "Not enough gold", serviceId, 0, 0, []);

        state.Gold -= service.Cost;

        var spiritsBefore = state.Spirits;
        if (service.RestoresFull)
            state.Spirits = state.MaxSpirits;
        else
            state.Spirits = Math.Min(state.MaxSpirits, state.Spirits + service.Spirits);
        var spiritsRestored = state.Spirits - spiritsBefore;

        // Advance one night (does not trigger EndOfDay — settlement nights bypass it)
        state.Day += 1;

        var medicinesConsumed = new List<string>();
        ConsumeMedicines(state, balance, medicinesConsumed);

        return new InnBookingResult(true, null, serviceId, service.Cost, spiritsRestored, medicinesConsumed);
    }

    /// <summary>
    /// Consume one matching medicine per active serious condition. Conditions are
    /// binary, so one dose clears one condition.
    /// </summary>
    static void ConsumeMedicines(PlayerState state, BalanceData balance, List<string> medicinesConsumed)
    {
        foreach (var conditionId in state.ActiveConditions.ToList())
        {
            if (!balance.Conditions.TryGetValue(conditionId, out var def)) continue;
            if (def.Severity != ConditionSeverity.Severe) continue;

            var idx = state.Haversack.FindIndex(i =>
                balance.Items.TryGetValue(i.DefId, out var itemDef)
                && itemDef.Cures.Contains(conditionId));

            if (idx < 0) continue;

            medicinesConsumed.Add(state.Haversack[idx].DefId);
            state.Haversack.RemoveAt(idx);
            state.ActiveConditions.Remove(conditionId);
        }
    }
}
