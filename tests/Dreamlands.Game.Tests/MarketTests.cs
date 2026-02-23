using Dreamlands.Game;
using Dreamlands.Rules;

namespace Dreamlands.Game.Tests;

public class MarketTests
{
    static readonly BalanceData Balance = BalanceData.Default;

    static PlayerState Fresh() => PlayerState.NewGame("test", 99, Balance);

    static SettlementState MakeSettlement(string biome = "plains") => new() { Biome = biome };

    [Fact]
    public void GetBasePrice_ReturnsCorrectPriceForKnownItem()
    {
        var item = Balance.Items["bodkin"]; // Cost = Small
        var price = Market.GetBasePrice(item, Balance);
        Assert.Equal(Balance.Character.CostMagnitudes[Magnitude.Small], price);
    }

    [Fact]
    public void InitializeSettlement_Camp_HasFoodAndTradeGoods_NoEquipment()
    {
        var state = Fresh();
        var settlement = Market.InitializeSettlement("Camp", "forest", 1, SettlementSize.Camp, state, Balance, new Random(1));

        // Should have 3 food items
        Assert.True(settlement.Stock.ContainsKey("food_protein"));
        Assert.True(settlement.Stock.ContainsKey("food_grain"));
        Assert.True(settlement.Stock.ContainsKey("food_sweets"));

        // Should have 2 trade goods (from in-biome pool)
        var tradeGoods = settlement.Stock.Keys
            .Where(id => Balance.Items.TryGetValue(id, out var d) && d.Type == ItemType.TradeGood)
            .ToList();
        Assert.Equal(2, tradeGoods.Count);

        // Should have no equipment
        var equipment = settlement.Stock.Keys
            .Where(id => Balance.Items.TryGetValue(id, out var d) && d.Type is ItemType.Weapon or ItemType.Armor or ItemType.Boots)
            .ToList();
        Assert.Empty(equipment);
    }

    [Fact]
    public void InitializeSettlement_Outpost_HasEquipment()
    {
        var state = Fresh();
        var settlement = Market.InitializeSettlement("Outpost", "plains", 1, SettlementSize.Outpost, state, Balance, new Random(1));

        var equipment = settlement.Stock.Keys
            .Where(id => Balance.Items.TryGetValue(id, out var d) && d.Type is ItemType.Weapon or ItemType.Armor or ItemType.Boots)
            .ToList();
        Assert.True(equipment.Count >= 1);
    }

    [Fact]
    public void InitializeSettlement_City_HasMoreStockThanCamp()
    {
        var state = Fresh();
        var rng = new Random(1);
        var camp = Market.InitializeSettlement("Camp", "plains", 2, SettlementSize.Camp, state, Balance, rng);
        var city = Market.InitializeSettlement("City", "plains", 2, SettlementSize.City, state, Balance, new Random(1));

        Assert.True(city.Stock.Count > camp.Stock.Count);
    }

    [Fact]
    public void InitializeSettlement_SetsFeaturedItems()
    {
        var state = Fresh();
        var settlement = Market.InitializeSettlement("Town", "forest", 2, SettlementSize.Town, state, Balance, new Random(1));

        Assert.NotNull(settlement.FeaturedSellItem);
        Assert.NotNull(settlement.FeaturedBuyItem);
        // Featured sell should be a trade good from the catalog
        Assert.Equal(ItemType.TradeGood, Balance.Items[settlement.FeaturedSellItem].Type);
        // Featured buy should be cross-biome
        Assert.NotEqual("forest", Balance.Items[settlement.FeaturedBuyItem].Biome);
    }

    [Fact]
    public void GetBuyFromSettlementPrice_MercantileDiscount()
    {
        var settlement = MakeSettlement();
        settlement.Prices["bodkin"] = 100;
        settlement.Stock["bodkin"] = 1;

        var priceNoMerc = Market.GetBuyFromSettlementPrice("bodkin", settlement, Balance, mercantileSkill: 0);
        var priceWithMerc = Market.GetBuyFromSettlementPrice("bodkin", settlement, Balance, mercantileSkill: 4);

        Assert.True(priceWithMerc < priceNoMerc);
    }

    [Fact]
    public void GetSellToSettlementPrice_StockedItem_ReturnsSettlementPrice()
    {
        var settlement = MakeSettlement("forest");
        settlement.Prices["bodkin"] = 42;
        settlement.Stock["bodkin"] = 1;

        var item = Balance.Items["bodkin"];
        var price = Market.GetSellToSettlementPrice(item, "forest", settlement, Balance, mercantileSkill: 0);

        Assert.Equal(42, price);
    }

    [Fact]
    public void GetSellToSettlementPrice_CrossBiome_GetsBonus()
    {
        // Item is from "plains" biome, selling at a "forest" settlement that doesn't stock it
        var settlement = MakeSettlement("forest");
        var item = Balance.Items["blank_ledger_book"]; // plains trade good, Cost = Small

        var price = Market.GetSellToSettlementPrice(item, "forest", settlement, Balance, mercantileSkill: 0);
        var basePrice = Market.GetBasePrice(item, Balance);

        // Cross-biome flat bonus should be added
        Assert.Equal(basePrice + Balance.Trade.CrossBiomeFlatBonus, price);
    }

    [Fact]
    public void Buy_Success_DeductsGoldAddsItemDecrementsStock()
    {
        var state = Fresh();
        state.Gold = 100;
        var settlement = MakeSettlement();
        settlement.Prices["bodkin"] = 15;
        settlement.Stock["bodkin"] = 2;

        var result = Market.Buy(state, "bodkin", settlement, Balance, new Random(1));

        Assert.True(result.Success);
        Assert.True(state.Gold < 100);
        Assert.Contains(state.Pack, i => i.DefId == "bodkin");
        Assert.Equal(1, settlement.Stock["bodkin"]);
    }

    [Fact]
    public void Buy_OutOfStock_Fails()
    {
        var state = Fresh();
        state.Gold = 100;
        var settlement = MakeSettlement();
        settlement.Prices["bodkin"] = 15;
        settlement.Stock["bodkin"] = 0;

        var result = Market.Buy(state, "bodkin", settlement, Balance, new Random(1));
        Assert.False(result.Success);
    }

    [Fact]
    public void Buy_NotEnoughGold_Fails()
    {
        var state = Fresh();
        state.Gold = 0;
        var settlement = MakeSettlement();
        settlement.Prices["bodkin"] = 15;
        settlement.Stock["bodkin"] = 1;

        var result = Market.Buy(state, "bodkin", settlement, Balance, new Random(1));
        Assert.False(result.Success);
    }

    [Fact]
    public void Buy_PackFull_FailsForEquipment()
    {
        var state = Fresh();
        state.Gold = 100;
        state.PackCapacity = 0; // no room
        var settlement = MakeSettlement();
        settlement.Prices["bodkin"] = 15;
        settlement.Stock["bodkin"] = 1;

        var result = Market.Buy(state, "bodkin", settlement, Balance, new Random(1));
        Assert.False(result.Success);
        Assert.Equal(100, state.Gold); // gold not deducted
    }

    [Fact]
    public void Buy_Food_DoesNotDecrementStock()
    {
        var state = Fresh();
        state.Gold = 100;
        var settlement = MakeSettlement();
        settlement.Prices["food_protein"] = 3;
        settlement.Stock["food_protein"] = 5;

        var result = Market.Buy(state, "food_protein", settlement, Balance, new Random(1));

        Assert.True(result.Success);
        Assert.Equal(5, settlement.Stock["food_protein"]); // unchanged
    }

    [Fact]
    public void Sell_FromPack_RemovesItemAddsGold()
    {
        var state = Fresh();
        state.Gold = 0;
        state.Pack.Add(new ItemInstance("bodkin", "Bodkin"));
        var settlement = MakeSettlement("plains");
        settlement.Prices["bodkin"] = 10;

        var result = Market.Sell(state, "bodkin", "plains", settlement, Balance);

        Assert.True(result.Success);
        Assert.DoesNotContain(state.Pack, i => i.DefId == "bodkin");
        Assert.True(state.Gold > 0);
    }

    [Fact]
    public void Sell_UnequipsIfEquipped()
    {
        var state = Fresh();
        state.Gold = 0;
        var weapon = new ItemInstance("bodkin", "Bodkin");
        state.Pack.Add(weapon);
        state.Equipment.Weapon = weapon;

        var settlement = MakeSettlement("plains");
        settlement.Prices["bodkin"] = 10;

        var result = Market.Sell(state, "bodkin", "plains", settlement, Balance);

        Assert.True(result.Success);
        Assert.Null(state.Equipment.Weapon);
    }

    [Fact]
    public void Sell_NotFound_Fails()
    {
        var state = Fresh();
        var settlement = MakeSettlement();

        var result = Market.Sell(state, "nonexistent", "plains", settlement, Balance);
        Assert.False(result.Success);
    }

    [Fact]
    public void Restock_IncreasesTradeGoodStock()
    {
        var settlement = MakeSettlement();
        settlement.Stock["blank_ledger_book"] = 0;
        settlement.Prices["blank_ledger_book"] = 15;
        settlement.LastRestockDay = 1;

        Market.Restock(settlement, SettlementSize.Town, currentDay: 10, Balance, new Random(1));

        Assert.True(settlement.Stock["blank_ledger_book"] > 0);
    }

    [Fact]
    public void Restock_DoesNotRestockEquipment()
    {
        var settlement = MakeSettlement();
        settlement.Stock["bodkin"] = 0;
        settlement.Stock["blank_ledger_book"] = 0; // need at least one trade good or restock exits early
        settlement.Prices["bodkin"] = 15;
        settlement.Prices["blank_ledger_book"] = 15;
        settlement.LastRestockDay = 1;

        Market.Restock(settlement, SettlementSize.Town, currentDay: 10, Balance, new Random(1));

        Assert.Equal(0, settlement.Stock["bodkin"]);
    }
}
