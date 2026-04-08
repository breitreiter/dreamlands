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
    /// <param name="displayName">Biome-flavored display name for the ration items.
    /// Caller is responsible for resolving via FlavorText.RationName(biome).</param>
    public static int Refill(PlayerState player, BalanceData balance, string displayName)
    {
        var freeSlots = player.HaversackCapacity - player.Haversack.Count;
        if (freeSlots <= 0) return 0;

        for (int i = 0; i < freeSlots; i++)
            player.Haversack.Add(new ItemInstance(RationDefId, displayName));

        return freeSlots;
    }
}
