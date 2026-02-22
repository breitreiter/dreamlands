using Dreamlands.Rules;

namespace Dreamlands.Game;

public static class Market
{
    public static int GetBasePrice(ItemDef item, BalanceData balance) =>
        item.Cost != null && balance.Character.CostMagnitudes.TryGetValue(item.Cost.Value, out var price) ? price : 0;

    public static SettlementState InitializeSettlement(
        string name, string biome, int tier, SettlementSize size,
        PlayerState player, BalanceData balance, Random rng)
    {
        var state = new SettlementState();
        var catalog = new List<string>();

        var inBiomeTierGoods = balance.Items.Values
            .Where(i => i.Type == ItemType.TradeGood && i.Biome == biome && i.ShopTier != null && i.ShopTier <= tier)
            .ToList();

        var equipment = balance.Items.Values
            .Where(i => i.Type is ItemType.Weapon or ItemType.Armor or ItemType.Boots
                        && i.ShopTier != null && i.ShopTier <= tier && i.Cost != null)
            .ToList();

        // Camp: 2 in-biome/in-tier trade goods
        AddRandomItems(catalog, inBiomeTierGoods, 2, rng);

        // Outpost: +1 equipment
        if (size >= SettlementSize.Outpost)
            AddRandomItems(catalog, equipment, 1, rng);

        // Village: +1 in-biome/in-tier trade good
        if (size >= SettlementSize.Village)
            AddRandomItems(catalog, inBiomeTierGoods, 1, rng, catalog);

        // Town: +1 out-of-biome tier 1-2 trade good, +1 equipment
        if (size >= SettlementSize.Town)
        {
            var outBiomeGoods = balance.Items.Values
                .Where(i => i.Type == ItemType.TradeGood && i.Biome != biome && i.ShopTier is >= 1 and <= 2)
                .ToList();
            AddRandomItems(catalog, outBiomeGoods, 1, rng, catalog);
            AddRandomItems(catalog, equipment, 1, rng, catalog);
        }

        // City: +1 in-biome/in-tier trade good, +1 equipment
        if (size >= SettlementSize.City)
        {
            AddRandomItems(catalog, inBiomeTierGoods, 1, rng, catalog);
            AddRandomItems(catalog, equipment, 1, rng, catalog);
        }

        // Featured sell item: random from catalog, gets 15% discount
        var tradeGoodsInCatalog = catalog.Where(id => balance.Items[id].Type == ItemType.TradeGood).ToList();
        if (tradeGoodsInCatalog.Count > 0)
            state.FeaturedSellItem = tradeGoodsInCatalog[rng.Next(tradeGoodsInCatalog.Count)];

        // Featured buy item: cross-biome trade good, unique across settlements if possible
        var crossBiomeGoods = balance.Items.Values
            .Where(i => i.Type == ItemType.TradeGood && i.Biome != biome)
            .Select(i => i.Id)
            .ToList();
        var unclaimed = crossBiomeGoods.Where(id => !player.ClaimedFeaturedBuys.Contains(id)).ToList();
        var buyPool = unclaimed.Count > 0 ? unclaimed : crossBiomeGoods;
        if (buyPool.Count > 0)
        {
            state.FeaturedBuyItem = buyPool[rng.Next(buyPool.Count)];
            player.ClaimedFeaturedBuys.Add(state.FeaturedBuyItem);
        }

        // Calculate sell prices (what the settlement charges when player buys)
        foreach (var itemId in catalog)
        {
            var def = balance.Items[itemId];
            var basePrice = GetBasePrice(def, balance);
            var jitter = 1.0 + (rng.NextDouble() * 2 - 1) * balance.Trade.PriceJitter;
            var modifier = itemId == state.FeaturedSellItem ? (1.0 - balance.Trade.FeaturedSellDiscount) : 1.0;
            state.Prices[itemId] = Math.Max(1, (int)Math.Round(basePrice * jitter * modifier));
        }

        // Set initial stock
        int maxStock = balance.Trade.MaxStock[size];
        int initialQty = tier == 1 ? maxStock : 1;
        foreach (var itemId in catalog)
        {
            var def = balance.Items[itemId];
            // Equipment gets exactly 1, never more
            state.Stock[itemId] = def.Type is ItemType.Weapon or ItemType.Armor or ItemType.Boots ? 1 : initialQty;
        }

        state.LastRestockDay = player.Day;
        return state;
    }

    public static void Restock(SettlementState settlement, SettlementSize size, int currentDay, BalanceData balance, Random rng)
    {
        int elapsed = currentDay - settlement.LastRestockDay;
        if (elapsed <= 0) return;

        int maxStock = balance.Trade.MaxStock[size];
        int perDay = balance.Trade.RestockPerDay[size];

        // Only trade goods restock, never equipment
        var tradeGoodIds = settlement.Stock.Keys
            .Where(id => balance.Items.TryGetValue(id, out var def) && def.Type == ItemType.TradeGood)
            .ToList();

        if (tradeGoodIds.Count == 0)
        {
            settlement.LastRestockDay = currentDay;
            return;
        }

        for (int day = 0; day < elapsed; day++)
        {
            for (int i = 0; i < perDay; i++)
            {
                var itemId = tradeGoodIds[rng.Next(tradeGoodIds.Count)];
                if (settlement.Stock[itemId] < maxStock)
                    settlement.Stock[itemId]++;
            }
        }

        settlement.LastRestockDay = currentDay;
    }

    public static List<StockEntry> GetStock(SettlementState settlement, BalanceData balance)
    {
        var entries = new List<StockEntry>();
        foreach (var (itemId, qty) in settlement.Stock)
        {
            if (qty <= 0) continue;
            if (!balance.Items.TryGetValue(itemId, out var def)) continue;
            var price = settlement.Prices.GetValueOrDefault(itemId, GetBasePrice(def, balance));
            entries.Add(new StockEntry(def, price, qty, itemId == settlement.FeaturedSellItem));
        }
        return entries.OrderBy(e => e.Item.Type).ThenBy(e => e.Price).ToList();
    }

    public static int GetSellToSettlementPrice(ItemDef item, string settlementBiome, SettlementState settlement, BalanceData balance, int mercantileSkill)
    {
        var basePrice = GetBasePrice(item, balance);
        if (basePrice <= 0) return 0;

        double modifier = 1.0;

        // Featured buy premium
        if (item.Id == settlement.FeaturedBuyItem)
            modifier += balance.Trade.FeaturedBuyPremium;

        // Same-biome penalty
        if (item.Biome == settlementBiome)
            modifier -= balance.Trade.SameBiomeBuyPenalty;

        return Math.Max(1, (int)Math.Round(basePrice * modifier));
    }

    public static int GetBuyFromSettlementPrice(string itemId, SettlementState settlement, BalanceData balance, int mercantileSkill)
    {
        if (!balance.Items.TryGetValue(itemId, out var def)) return 0;
        var price = settlement.Prices.GetValueOrDefault(itemId, GetBasePrice(def, balance));
        var discount = 1.0 - mercantileSkill * balance.Trade.MercantileDiscountPerPoint;
        return Math.Max(1, (int)Math.Round(price * discount));
    }

    public static MarketResult Buy(PlayerState player, string itemId, SettlementState settlement, BalanceData balance)
    {
        if (!balance.Items.TryGetValue(itemId, out var def))
            return new MarketResult(false, $"Unknown item: {itemId}");

        var stock = settlement.Stock.GetValueOrDefault(itemId);
        if (stock <= 0)
            return new MarketResult(false, "Out of stock");

        int mercantile = player.Skills.GetValueOrDefault(Skill.Mercantile);
        var price = GetBuyFromSettlementPrice(itemId, settlement, balance, mercantile);
        if (price <= 0)
            return new MarketResult(false, "Item not for sale");

        if (player.Gold < price)
            return new MarketResult(false, "Not enough gold");

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

        player.Gold -= price;
        settlement.Stock[itemId] = stock - 1;
        return new MarketResult(true, $"Bought {def.Name} for {price} gold");
    }

    public static MarketResult Sell(PlayerState player, string itemDefId, string settlementBiome,
        SettlementState settlement, BalanceData balance)
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
            int price = 0;
            if (def != null)
            {
                int mercantile = player.Skills.GetValueOrDefault(Skill.Mercantile);
                price = GetSellToSettlementPrice(def, settlementBiome, settlement, balance, mercantile);
            }
            player.Gold += price;
            return new MarketResult(true, $"Sold {packItem.DisplayName} for {price} gold");
        }

        var havItem = player.Haversack.FirstOrDefault(i => i.DefId == itemDefId);
        if (havItem != null)
        {
            player.Haversack.Remove(havItem);
            var def = balance.Items.GetValueOrDefault(itemDefId);
            int price = 0;
            if (def != null)
            {
                int mercantile = player.Skills.GetValueOrDefault(Skill.Mercantile);
                price = GetSellToSettlementPrice(def, settlementBiome, settlement, balance, mercantile);
            }
            player.Gold += price;
            return new MarketResult(true, $"Sold {havItem.DisplayName} for {price} gold");
        }

        return new MarketResult(false, "Item not found in inventory");
    }

    static void AddRandomItems(List<string> catalog, List<ItemDef> pool, int count, Random rng, List<string>? exclude = null)
    {
        var available = exclude != null ? pool.Where(i => !exclude.Contains(i.Id)).ToList() : pool.ToList();
        for (int i = 0; i < count && available.Count > 0; i++)
        {
            var idx = rng.Next(available.Count);
            catalog.Add(available[idx].Id);
            available.RemoveAt(idx);
        }
    }
}

public record StockEntry(ItemDef Item, int Price, int Quantity, bool IsFeaturedSell);

public record MarketResult(bool Success, string Message);
