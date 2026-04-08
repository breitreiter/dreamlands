using Dreamlands.Rules;

namespace Dreamlands.Game;

public static class Market
{
    public static int GetBasePrice(ItemDef item, BalanceData balance) =>
        item.Cost ?? 0;

    public static SettlementState InitializeSettlement(
        string settlementId, string biome, int tier, SettlementSize size,
        PlayerState player, BalanceData balance, Random rng)
    {
        var state = new SettlementState { Biome = biome };
        var catalog = new List<string>();

        // Food is not sold in the market UI — players use the "Restock food and leave"
        // button to refill the haversack for free. Rations dropped from encounters can
        // still be sold at the flat ration sell price (see GetSellPrice).

        // Bandages — always stocked everywhere
        catalog.Add("bandages");

        // Specialty medicines — rare, rolled like equipment
        var medicines = balance.Items.Values
            .Where(i => i.Type == ItemType.Consumable && i.Cures.Count > 0
                        && i.Id != "bandages"
                        && (i.Biome == null || i.Biome == biome)
                        && (i.ShopTier == null || i.ShopTier <= tier))
            .ToList();

        var equipment = balance.Items.Values
            .Where(i => i.Type is ItemType.Weapon or ItemType.Armor or ItemType.Boots
                        && i.ShopTier != null && i.ShopTier <= tier && i.Cost != null)
            .ToList();

        var tools = balance.Items.Values
            .Where(i => i.Type == ItemType.Tool && i.Cost != null
                        && (i.Biome == null || i.Biome == biome)
                        && (i.ShopTier == null || i.ShopTier <= tier))
            .ToList();

        // Outpost+: equipment + tool
        if (size >= SettlementSize.Outpost)
        {
            AddRandomItems(catalog, equipment, 1, rng);
            AddRandomItems(catalog, tools, 1, rng);
        }

        // Outpost: specialty medicine (reward for trekking to remote leaf nodes)
        if (size <= SettlementSize.Outpost)
        {
            AddRandomItems(catalog, medicines, 1, rng, catalog);
        }

        // Town+: additional equipment + tool
        if (size >= SettlementSize.Town)
        {
            AddRandomItems(catalog, equipment, 1, rng, catalog);
            AddRandomItems(catalog, tools, 1, rng, catalog);
        }

        // City: additional equipment
        if (size >= SettlementSize.City)
        {
            AddRandomItems(catalog, equipment, 1, rng, catalog);
        }

        // Calculate prices
        foreach (var itemId in catalog)
        {
            var def = balance.Items[itemId];
            var basePrice = GetBasePrice(def, balance);
            var jitter = 1.0 + (rng.NextDouble() * 2 - 1) * balance.Trade.PriceJitter;
            state.Prices[itemId] = Math.Max(1, (int)Math.Round(basePrice * jitter));
        }

        // Set initial stock
        int maxStock = balance.Trade.MaxStock[size];
        foreach (var itemId in catalog)
        {
            var def = balance.Items[itemId];
            state.Stock[itemId] = def switch
            {
                { Type: ItemType.Weapon or ItemType.Armor or ItemType.Boots } => 1,
                { Type: ItemType.Tool } => 1,
                { Id: "bandages" } => maxStock, // bandages always plentiful
                { Cures.Count: > 0 } => 1, // specialty medicines are scarce
                _ => maxStock,
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

        // Medicines restock; food has unlimited stock, equipment never restocks
        var restockIds = settlement.Stock.Keys
            .Where(id => balance.Items.TryGetValue(id, out var def) && def.Cures.Count > 0)
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
            entries.Add(new StockEntry(def, price, qty));
        }
        return entries.OrderBy(e => e.Item.Type).ThenBy(e => e.Price).ToList();
    }

    public static int GetBuyFromSettlementPrice(string itemId, SettlementState settlement, BalanceData balance)
    {
        if (!balance.Items.TryGetValue(itemId, out var def)) return 0;
        return settlement.Prices.GetValueOrDefault(itemId, GetBasePrice(def, balance));
    }

    public static MarketResult Buy(PlayerState player, string itemId, SettlementState settlement,
        BalanceData balance, Random rng)
    {
        if (!balance.Items.TryGetValue(itemId, out var def))
            return new MarketResult(false, $"Unknown item: {itemId}");

        var stock = settlement.Stock.GetValueOrDefault(itemId);
        if (stock <= 0)
            return new MarketResult(false, "Out of stock");

        var price = GetBuyFromSettlementPrice(itemId, settlement, balance);
        if (price <= 0)
            return new MarketResult(false, "Item not for sale");

        if (player.Gold < price)
            return new MarketResult(false, "Not enough gold");

        var instance = new ItemInstance(def.Id, def.Name);

        // Auto-equip weapon/armor/boots if the slot is empty (bypasses pack capacity)
        var autoEquipped = false;
        if (def.Type is ItemType.Weapon && player.Equipment.Weapon == null)
            { player.Equipment.Weapon = instance; autoEquipped = true; }
        else if (def.Type is ItemType.Armor && player.Equipment.Armor == null)
            { player.Equipment.Armor = instance; autoEquipped = true; }
        else if (def.Type is ItemType.Boots && player.Equipment.Boots == null)
            { player.Equipment.Boots = instance; autoEquipped = true; }
        else if (def.IsPackItem)
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
        var verb = autoEquipped ? "Bought and equipped" : "Bought";
        return new MarketResult(true, $"{verb} {def.Name} for {price} gold");
    }

    public static MarketResult ClaimHaul(PlayerState player, string offerId, SettlementState settlement)
    {
        var idx = settlement.HaulOffers.FindIndex(h => h.HaulOfferId == offerId);
        if (idx < 0)
            return new MarketResult(false, "Haul offer not found");

        if (player.Pack.Count >= player.PackCapacity)
            return new MarketResult(false, "Pack is full");

        var haul = settlement.HaulOffers[idx];
        settlement.HaulOffers.RemoveAt(idx);
        player.Pack.Add(haul);

        return new MarketResult(true, $"Claimed {haul.DisplayName} — deliver to {haul.DestinationHint}");
    }

    public static int GetSellPrice(ItemDef item, BalanceData balance)
    {
        // Rations sell at flat Cost (no ratio) — they're a player gold source.
        if (item.Id == Rations.RationDefId)
            return GetBasePrice(item, balance);

        var basePrice = GetBasePrice(item, balance);
        if (basePrice <= 0) return 0;
        return Math.Max(1, (int)Math.Round(basePrice * balance.Trade.SellRatio));
    }

    public static MarketResult Sell(PlayerState player, string itemDefId, BalanceData balance)
    {
        if (!balance.Items.TryGetValue(itemDefId, out var def))
            return new MarketResult(false, $"Unknown item: {itemDefId}");

        if (def.Type == ItemType.Haul)
            return new MarketResult(false, "Cannot sell hauls");

        var price = GetSellPrice(def, balance);
        if (price <= 0)
            return new MarketResult(false, "Item has no sell value");

        // Search pack first, then haversack, then equipment
        var packIdx = player.Pack.FindIndex(i => i.DefId == itemDefId);
        if (packIdx >= 0)
        {
            player.Pack.RemoveAt(packIdx);
            player.Gold += price;
            return new MarketResult(true, $"Sold {def.Name} for {price} gold");
        }

        var havIdx = player.Haversack.FindIndex(i => i.DefId == itemDefId);
        if (havIdx >= 0)
        {
            player.Haversack.RemoveAt(havIdx);
            player.Gold += price;
            return new MarketResult(true, $"Sold {def.Name} for {price} gold");
        }

        // Check equipment slots
        if (player.Equipment.Weapon?.DefId == itemDefId)
        {
            player.Equipment.Weapon = null;
            player.Gold += price;
            return new MarketResult(true, $"Unequipped and sold {def.Name} for {price} gold");
        }
        if (player.Equipment.Armor?.DefId == itemDefId)
        {
            player.Equipment.Armor = null;
            player.Gold += price;
            return new MarketResult(true, $"Unequipped and sold {def.Name} for {price} gold");
        }
        if (player.Equipment.Boots?.DefId == itemDefId)
        {
            player.Equipment.Boots = null;
            player.Gold += price;
            return new MarketResult(true, $"Unequipped and sold {def.Name} for {price} gold");
        }

        return new MarketResult(false, $"You don't have {def.Name}");
    }

    public static MarketOrderResult ApplyOrder(PlayerState player, MarketOrder order,
        SettlementState settlement, BalanceData balance, Random rng)
    {
        var results = new List<MarketLineResult>();

        // Process sells first (frees space + adds gold)
        foreach (var sell in order.Sells)
        {
            var result = Sell(player, sell.ItemDefId, balance);
            results.Add(new MarketLineResult("sell", sell.ItemDefId, result.Success, result.Message));
        }

        var buyFailed = false;
        foreach (var buy in order.Buys)
        {
            if (buyFailed) break;
            for (int i = 0; i < buy.Quantity; i++)
            {
                var result = Buy(player, buy.ItemId, settlement, balance, rng);
                results.Add(new MarketLineResult("buy", buy.ItemId, result.Success, result.Message));
                if (!result.Success) { buyFailed = true; break; }
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

public record StockEntry(ItemDef Item, int Price, int Quantity);

public record MarketResult(bool Success, string Message);

public record MarketOrder(List<BuyLine> Buys, List<SellLine> Sells);
public record BuyLine(string ItemId, int Quantity);
public record SellLine(string ItemDefId);
public record MarketOrderResult(bool Success, List<MarketLineResult> Results);
public record MarketLineResult(string Action, string ItemId, bool Success, string Message);
