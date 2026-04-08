using Dreamlands.Rules;

namespace Dreamlands.Game;

/// <summary>
/// Rations refill on settlement entry. Fills every free haversack slot with rations,
/// never displacing existing items. Free, idempotent.
/// Trinkets, keys, medicine, and other haversack items reduce ration capacity by
/// taking slots — packing pressure is real, but food itself is free.
/// </summary>
public static class Rations
{
    public const string RationDefId = "food_ration";

    /// <summary>
    /// Fill all empty haversack slots with rations. Returns the number added.
    /// Does not displace existing items.
    /// </summary>
    /// <param name="displayNameFactory">Called once per ration added so each
    /// item can carry a freshly-rolled meal name. Caller typically supplies
    /// <c>() =&gt; FlavorText.RationName(biome, rng)</c>.</param>
    public static int Refill(PlayerState player, BalanceData balance, Func<string> displayNameFactory)
    {
        var freeSlots = player.HaversackCapacity - player.Haversack.Count;
        if (freeSlots <= 0) return 0;

        for (int i = 0; i < freeSlots; i++)
            player.Haversack.Add(new ItemInstance(RationDefId, displayNameFactory()));

        return freeSlots;
    }
}
