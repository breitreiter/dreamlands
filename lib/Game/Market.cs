using Dreamlands.Rules;

namespace Dreamlands.Game;

public static class Market
{
    public static int GetBuyPrice(ItemDef item, BalanceData balance)
    {
        if (item.Cost == null) return 0;
        return balance.Character.CostMagnitudes.TryGetValue(item.Cost.Value, out var price) ? price : 0;
    }

    public static int GetSellPrice(ItemDef item, BalanceData balance) =>
        GetBuyPrice(item, balance) / 2;

    public static List<ItemDef> GetStock(int settlementTier, BalanceData balance) =>
        balance.Items.Values
            .Where(i => i.ShopTier != null && i.ShopTier <= settlementTier && i.Cost != null && i.Type != ItemType.TradeGood)
            .OrderBy(i => i.Type)
            .ThenBy(i => GetBuyPrice(i, balance))
            .ToList();

    public static MarketResult Buy(PlayerState player, string itemId, int quantity, BalanceData balance)
    {
        if (!balance.Items.TryGetValue(itemId, out var def))
            return new MarketResult(false, $"Unknown item: {itemId}");

        var price = GetBuyPrice(def, balance) * quantity;
        if (price <= 0)
            return new MarketResult(false, "Item not for sale");

        if (player.Gold < price)
            return new MarketResult(false, "Not enough gold");

        for (int i = 0; i < quantity; i++)
        {
            var instance = new ItemInstance(def.Id, def.Name);

            if (def.IsPackItem)
            {
                if (player.Pack.Count >= player.PackCapacity)
                    return new MarketResult(false, "Pack is full");
                player.Pack.Add(instance);
            }
            else
            {
                if (player.Haversack.Count >= player.HaversackCapacity)
                    return new MarketResult(false, "Haversack is full");
                player.Haversack.Add(instance);
            }
        }

        player.Gold -= price;
        return new MarketResult(true, $"Bought {quantity}x {def.Name} for {price} gold");
    }

    public static MarketResult Sell(PlayerState player, string itemDefId, BalanceData balance)
    {
        // Try pack first, then haversack
        var packItem = player.Pack.FirstOrDefault(i => i.DefId == itemDefId);
        if (packItem != null)
        {
            // Unequip if equipped
            if (player.Equipment.Weapon?.DefId == itemDefId)
                player.Equipment.Weapon = null;
            else if (player.Equipment.Armor?.DefId == itemDefId)
                player.Equipment.Armor = null;
            else if (player.Equipment.Boots?.DefId == itemDefId)
                player.Equipment.Boots = null;

            player.Pack.Remove(packItem);
            var def = balance.Items.GetValueOrDefault(itemDefId);
            var price = def != null ? GetSellPrice(def, balance) : 0;
            player.Gold += price;
            return new MarketResult(true, $"Sold {packItem.DisplayName} for {price} gold");
        }

        var havItem = player.Haversack.FirstOrDefault(i => i.DefId == itemDefId);
        if (havItem != null)
        {
            player.Haversack.Remove(havItem);
            var def = balance.Items.GetValueOrDefault(itemDefId);
            var price = def != null ? GetSellPrice(def, balance) : 0;
            player.Gold += price;
            return new MarketResult(true, $"Sold {havItem.DisplayName} for {price} gold");
        }

        return new MarketResult(false, "Item not found in inventory");
    }
}

public record MarketResult(bool Success, string Message);
