using Dreamlands.Rules;

namespace Dreamlands.Game;

public record InnService(string Id, string Name, int Cost, int Spirits, bool RestoresFull);

public record InnBookingResult(
    bool Success,
    string? Reason,
    string ServiceId,
    int GoldSpent,
    int SpiritsRestored,
    List<string> MedicinesApplied,
    List<string> ConditionsCleared);

public static class Inn
{
    public const string BedServiceId = "bed";
    public const string BathServiceId = "bath";
    public const string FullServiceId = "full";

    /// <summary>
    /// Returns the three inn service tiers from balance data. The bed/bath tiers
    /// add a fixed amount of spirits; the full tier restores spirits to max.
    /// All tiers clear severe conditions covered by carried medicine kits;
    /// kits are reusable and never consumed (minor conditions were already
    /// cleared by SettlementRunner.EnsureSettlement on entry).
    /// </summary>
    public static IReadOnlyList<InnService> GetServiceOptions(BalanceData balance) =>
    [
        new(BedServiceId,  "A bed for the night",        balance.Character.InnBedCost,  balance.Character.InnBedSpirits,  RestoresFull: false),
        new(BathServiceId, "Bed + hot bath",             balance.Character.InnBathCost, balance.Character.InnBathSpirits, RestoresFull: false),
        new(FullServiceId, "Bed, bath, evening drinks",  balance.Character.InnFullCost, 0,                                RestoresFull: true),
    ];

    /// <summary>
    /// Book a single inn service. Validates affordability, deducts gold, restores
    /// spirits per the tier, advances time by one night, and applies any matching
    /// serious-condition medicines from the pack (reusable; not consumed). At the
    /// chapterhouse the stay is free and the resident physician clears all severe
    /// conditions even without matching kits.
    /// </summary>
    public static InnBookingResult BookService(
        PlayerState state, BalanceData balance, string serviceId, bool chapterhouse = false)
    {
        var service = GetServiceOptions(balance).FirstOrDefault(s => s.Id == serviceId);
        if (service == null)
            return new InnBookingResult(false, $"Unknown service '{serviceId}'", serviceId, 0, 0, [], []);

        var cost = chapterhouse ? 0 : service.Cost;

        if (state.Gold < cost)
            return new InnBookingResult(false, "Not enough gold", serviceId, 0, 0, [], []);

        state.Gold -= cost;

        var spiritsBefore = state.Spirits;
        if (service.RestoresFull)
            state.Spirits = state.MaxSpirits;
        else
            state.Spirits = Math.Min(state.MaxSpirits, state.Spirits + service.Spirits);
        var spiritsRestored = state.Spirits - spiritsBefore;

        // Advance one night (does not trigger EndOfDay — settlement nights bypass it)
        state.Day += 1;

        var medicinesApplied = new List<string>();
        var conditionsCleared = new List<string>();
        ClearSevereConditions(state, balance, medicinesApplied, conditionsCleared, chapterhouse);

        return new InnBookingResult(true, null, serviceId, cost, spiritsRestored, medicinesApplied, conditionsCleared);
    }

    /// <summary>
    /// Clear severe conditions. At the chapterhouse the physician handles every
    /// severe condition for free. At a regular inn, each condition clears if the
    /// player carries a matching medicine kit in the pack; kits are reusable and
    /// stay in the pack. Conditions without a matching kit are left active.
    /// </summary>
    static void ClearSevereConditions(
        PlayerState state,
        BalanceData balance,
        List<string> medicinesApplied,
        List<string> conditionsCleared,
        bool chapterhouse)
    {
        foreach (var conditionId in state.ActiveConditions.ToList())
        {
            if (!balance.Conditions.TryGetValue(conditionId, out var def)) continue;
            if (def.Severity != ConditionSeverity.Severe) continue;

            if (chapterhouse)
            {
                state.ActiveConditions.Remove(conditionId);
                conditionsCleared.Add(conditionId);
                continue;
            }

            var idx = state.Pack.FindIndex(i =>
                balance.Items.TryGetValue(i.DefId, out var itemDef)
                && itemDef.Cures.Contains(conditionId));

            if (idx < 0) continue;

            medicinesApplied.Add(state.Pack[idx].DefId);
            state.ActiveConditions.Remove(conditionId);
            conditionsCleared.Add(conditionId);
        }
    }
}
