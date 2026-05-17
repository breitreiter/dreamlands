using Dreamlands.Rules;

namespace Dreamlands.Game;

public record RescueResult(List<string> LostItems, int GoldLost);

/// <summary>
/// Rescue system: when health hits 0 during end-of-day, the player is rescued —
/// stripped of replaceable items, reset to starting gold, teleported to the Chapterhouse.
/// Items with no shop cost (dungeon-only tools, top-tier gear) are kept.
/// </summary>
public static class Rescue
{
    public static RescueResult Apply(PlayerState state, int startX, int startY, BalanceData balance)
    {
        var lostItems = new List<string>();
        var goldLost = Math.Max(0, state.Gold - balance.Character.StartingGold);

        // Strip all market-purchasable items from Pack (items with a shop cost, including hauls)
        for (int i = state.Pack.Count - 1; i >= 0; i--)
        {
            var item = state.Pack[i];
            var isHaul = item.DefId == "haul" || (balance.Items.TryGetValue(item.DefId, out var d) && d.Type == ItemType.Haul);
            var isPurchasable = !isHaul && balance.Items.TryGetValue(item.DefId, out var def) && def.Cost != null;

            if (isHaul || isPurchasable)
            {
                item.IsEquipped = false;
                lostItems.Add(item.DisplayName);
                state.Pack.RemoveAt(i);
            }
        }

        // Reset gold
        state.Gold = balance.Character.StartingGold;

        // Full recovery
        state.Health = state.MaxHealth;
        state.Spirits = state.MaxSpirits;

        // Clear all conditions
        state.ActiveConditions.Clear();

        // Teleport to chapterhouse
        state.X = startX;
        state.Y = startY;

        // Clear dungeon state
        state.CurrentDungeonId = null;
        state.CurrentEncounterId = null;

        // Reset encounter cadence
        state.MoveCount = 0;
        state.NextEncounterMove = 0;

        // Reset time
        state.Time = TimePeriod.Morning;
        state.Day++;

        // Clear pending flags
        state.PendingEndOfDay = false;
        state.PendingNoSleep = false;
        state.PendingNoMeal = false;
        state.PendingNoBiome = false;

        return new RescueResult(lostItems, goldLost);
    }
}
