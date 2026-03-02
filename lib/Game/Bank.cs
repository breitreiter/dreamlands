using Dreamlands.Rules;

namespace Dreamlands.Game;

public static class Bank
{
    public static string? Deposit(PlayerState player, string defId, string source, SettlementState settlement, BalanceData balance)
    {
        var capacity = balance.Settlements.BankCapacity;
        if (settlement.Bank.Count >= capacity)
            return "Bank is full";

        ItemInstance? item;
        switch (source)
        {
            case "pack":
                item = player.Pack.FirstOrDefault(i => i.DefId == defId);
                if (item == null) return "Item not found in pack";
                player.Pack.Remove(item);
                break;

            case "haversack":
                item = player.Haversack.FirstOrDefault(i => i.DefId == defId);
                if (item == null) return "Item not found in haversack";
                player.Haversack.Remove(item);
                break;

            case "weapon":
                item = player.Equipment.Weapon;
                if (item == null || item.DefId != defId) return "Item not equipped in weapon slot";
                player.Equipment.Weapon = null;
                break;

            case "armor":
                item = player.Equipment.Armor;
                if (item == null || item.DefId != defId) return "Item not equipped in armor slot";
                player.Equipment.Armor = null;
                break;

            case "boots":
                item = player.Equipment.Boots;
                if (item == null || item.DefId != defId) return "Item not equipped in boots slot";
                player.Equipment.Boots = null;
                break;

            default:
                return $"Invalid source: {source}";
        }

        settlement.Bank.Add(item);
        return null;
    }

    public static string? Withdraw(PlayerState player, int bankIndex, SettlementState settlement, BalanceData balance)
    {
        if (bankIndex < 0 || bankIndex >= settlement.Bank.Count)
            return "Invalid bank slot";

        var item = settlement.Bank[bankIndex];
        var def = balance.Items.GetValueOrDefault(item.DefId);
        var isPackItem = def?.IsPackItem ?? true;

        if (isPackItem)
        {
            if (player.Pack.Count >= player.PackCapacity)
                return "Pack is full";
            settlement.Bank.RemoveAt(bankIndex);
            player.Pack.Add(item);
        }
        else
        {
            if (player.Haversack.Count >= player.HaversackCapacity)
                return "Haversack is full";
            settlement.Bank.RemoveAt(bankIndex);
            player.Haversack.Add(item);
        }

        return null;
    }
}
