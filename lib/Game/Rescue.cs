using Dreamlands.Rules;

namespace Dreamlands.Game;

public record RescueResult(List<string> LostItems, int GoldLost);

/// <summary>
/// Rescue system: when health hits 0 during end-of-day, the player is rescued —
/// stripped of replaceable items, reset to starting gold, teleported to the Chapterhouse.
/// Items with no shop cost (dungeon-only tools, tokens, top-tier gear) are kept.
/// </summary>
public static class Rescue
{
    public static RescueResult Apply(PlayerState state, int startX, int startY, BalanceData balance)
    {
        var lostItems = new List<string>();
        var goldLost = Math.Max(0, state.Gold - balance.Character.StartingGold);

        // Strip hauls from Pack
        for (int i = state.Pack.Count - 1; i >= 0; i--)
        {
            if (state.Pack[i].DefId == "haul")
            {
                lostItems.Add(state.Pack[i].DisplayName);
                state.Pack.RemoveAt(i);
            }
        }

        // Strip market-purchasable gear from Pack (items with a shop cost)
        for (int i = state.Pack.Count - 1; i >= 0; i--)
        {
            var item = state.Pack[i];
            if (balance.Items.TryGetValue(item.DefId, out var def) && def.Cost != null)
            {
                lostItems.Add(item.DisplayName);
                state.Pack.RemoveAt(i);
            }
        }

        // Strip market-purchasable equipment
        if (ShouldStrip(state.Equipment.Weapon, balance, lostItems))
            state.Equipment.Weapon = null;
        if (ShouldStrip(state.Equipment.Armor, balance, lostItems))
            state.Equipment.Armor = null;
        if (ShouldStrip(state.Equipment.Boots, balance, lostItems))
            state.Equipment.Boots = null;

        // Clear haversack (all food/medicine is re-buyable)
        foreach (var item in state.Haversack)
            lostItems.Add(item.DisplayName);
        state.Haversack.Clear();

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

    static bool ShouldStrip(ItemInstance? item, BalanceData balance, List<string> lostItems)
    {
        if (item == null) return false;
        if (balance.Items.TryGetValue(item.DefId, out var def) && def.Cost != null)
        {
            lostItems.Add(item.DisplayName);
            return true;
        }
        return false;
    }
}
