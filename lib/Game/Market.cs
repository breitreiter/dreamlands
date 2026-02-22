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
        var state = new SettlementState { Biome = biome };
        var catalog = new List<string>();

        // Food — always stocked at every settlement
        foreach (var foodId in new[] { "food_protein", "food_grain", "food_sweets" })
            catalog.Add(foodId);

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
            state.Stock[itemId] = def switch
            {
                { Type: ItemType.Weapon or ItemType.Armor or ItemType.Boots } => 1,
                { FoodType: not null } => maxStock, // food always well-stocked
                _ => initialQty,
            };
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

        // Trade goods restock; food has unlimited stock, equipment never restocks
        var restockIds = settlement.Stock.Keys
            .Where(id => balance.Items.TryGetValue(id, out var def)
                         && def.Type == ItemType.TradeGood)
            .ToList();

        if (restockIds.Count == 0)
        {
            settlement.LastRestockDay = currentDay;
            return;
        }

        for (int day = 0; day < elapsed; day++)
        {
            for (int i = 0; i < perDay; i++)
            {
                var itemId = restockIds[rng.Next(restockIds.Count)];
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

        // If the settlement stocks this item, sell price = settlement's own price (no arbitrage)
        if (settlement.Prices.ContainsKey(item.Id))
            return settlement.Prices[item.Id];

        double modifier = 1.0;
        int flat = 0;

        // Featured buy premium
        if (item.Id == settlement.FeaturedBuyItem)
            modifier += balance.Trade.FeaturedBuyPremium;

        // Cross-biome flat bonus / same-biome penalty
        if (item.Biome != settlementBiome)
            flat += balance.Trade.CrossBiomeFlatBonus;
        else
            modifier -= balance.Trade.SameBiomeBuyPenalty;

        return Math.Max(1, (int)Math.Round(basePrice * modifier) + flat);
    }

    public static int GetBuyFromSettlementPrice(string itemId, SettlementState settlement, BalanceData balance, int mercantileSkill)
    {
        if (!balance.Items.TryGetValue(itemId, out var def)) return 0;
        var price = settlement.Prices.GetValueOrDefault(itemId, GetBasePrice(def, balance));
        var discount = 1.0 - mercantileSkill * balance.Trade.MercantileDiscountPerPoint;
        return Math.Max(1, (int)Math.Round(price * discount));
    }

    public static MarketResult Buy(PlayerState player, string itemId, SettlementState settlement,
        BalanceData balance, Random rng, Func<FoodType, string, Random, ItemInstance>? createFood = null)
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

        var instance = def.FoodType is FoodType ft && createFood != null
            ? createFood(ft, settlement.Biome, rng)
            : new ItemInstance(def.Id, def.Name) { FoodType = def.FoodType };

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
        if (def.FoodType == null) // food has unlimited stock in settlements
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

    public static MarketOrderResult ApplyOrder(PlayerState player, MarketOrder order,
        string biome, SettlementState settlement, BalanceData balance, Random rng,
        Func<FoodType, string, Random, ItemInstance>? createFood = null)
    {
        var results = new List<MarketLineResult>();

        // Sells first — frees inventory space + adds gold
        foreach (var sell in order.Sells)
        {
            var result = Sell(player, sell.ItemDefId, biome, settlement, balance);
            results.Add(new MarketLineResult("sell", sell.ItemDefId, result.Success, result.Message));
        }

        // Then buys
        foreach (var buy in order.Buys)
        {
            for (int i = 0; i < buy.Quantity; i++)
            {
                var result = Buy(player, buy.ItemId, settlement, balance, rng, createFood);
                results.Add(new MarketLineResult("buy", buy.ItemId, result.Success, result.Message));
                if (!result.Success) break; // stop buying this item on first failure
            }
        }

        return new MarketOrderResult(results.All(r => r.Success), results);
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

public record MarketOrder(List<BuyLine> Buys, List<SellLine> Sells);
public record BuyLine(string ItemId, int Quantity);
public record SellLine(string ItemDefId);
public record MarketOrderResult(bool Success, List<MarketLineResult> Results);
public record MarketLineResult(string Action, string ItemId, bool Success, string Message);
