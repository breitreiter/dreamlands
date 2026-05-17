using Dreamlands.Rules;

namespace Dreamlands.Game;

public static class Bank
{
    public static string? Deposit(PlayerState player, string defId, string source, SettlementState settlement, BalanceData balance)
    {
        if (defId == Rations.RationDefId)
            return "Rations restock for free on every visit — no need to store them.";

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

            case "weapon":
                item = player.Pack.FirstOrDefault(i => i.IsEquipped && i.DefId == defId);
                if (item == null) return "Item not equipped in weapon slot";
                item.IsEquipped = false;
                player.Pack.Remove(item);
                break;

            case "armor":
                item = player.Pack.FirstOrDefault(i => i.IsEquipped && i.DefId == defId);
                if (item == null) return "Item not equipped in armor slot";
                item.IsEquipped = false;
                player.Pack.Remove(item);
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

        if (player.Pack.Count >= player.PackCapacity)
            return "Pack is full";

        var item = settlement.Bank[bankIndex];
        settlement.Bank.RemoveAt(bankIndex);
        player.Pack.Add(item);

        return null;
    }
}
