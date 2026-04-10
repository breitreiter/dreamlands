using Dreamlands.Rules;

namespace Dreamlands.Game;

public record RationRefillResult(int Added, int GoldSpent);

/// <summary>
/// Rations refill at settlements via the explicit "Restock Food and Leave" action.
/// Fills every free haversack slot with rations, charging the player the per-ration
/// cost (<see cref="ItemDef.Cost"/> on <c>food_ration</c>). Refill caps at what the
/// player can afford. Never displaces existing items.
/// </summary>
public static class Rations
{
    public const string RationDefId = "food_ration";

    /// <summary>
    /// Fill empty haversack slots with rations, deducting gold per ration. Caps at
    /// the number the player can afford. Returns the count added and gold spent.
    /// Does not displace existing items.
    /// </summary>
    /// <param name="displayNameFactory">Called once per ration added so each
    /// item can carry a freshly-rolled meal name. Caller typically supplies
    /// <c>() =&gt; FlavorText.RationName(biome, rng)</c>.</param>
    public static RationRefillResult Refill(PlayerState player, BalanceData balance, Func<string> displayNameFactory)
    {
        var freeSlots = player.HaversackCapacity - player.Haversack.Count;
        if (freeSlots <= 0) return new RationRefillResult(0, 0);

        var pricePerRation = balance.Items[RationDefId].Cost ?? 0;
        var affordable = pricePerRation > 0 ? player.Gold / pricePerRation : freeSlots;
        var toAdd = Math.Min(freeSlots, affordable);
        if (toAdd <= 0) return new RationRefillResult(0, 0);

        var goldSpent = toAdd * pricePerRation;
        player.Gold -= goldSpent;

        for (int i = 0; i < toAdd; i++)
            player.Haversack.Add(new ItemInstance(RationDefId, displayNameFactory()));

        return new RationRefillResult(toAdd, goldSpent);
    }
}
