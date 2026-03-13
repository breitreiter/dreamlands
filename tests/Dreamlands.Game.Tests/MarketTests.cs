using Dreamlands.Game;
using Dreamlands.Rules;

namespace Dreamlands.Game.Tests;

public class MarketTests
{
    static readonly BalanceData Balance = BalanceData.Default;

    static PlayerState Fresh() => PlayerState.NewGame("test", 99, Balance);

    static SettlementState MakeSettlement(string biome = "plains") => new() { Biome = biome };

    [Fact]
    public void InitializeSettlement_StocksFood()
    {
        var state = Fresh();
        var settlement = Market.InitializeSettlement("Camp", "forest", 1, SettlementSize.Camp, state, Balance, new Random(1));

        Assert.True(settlement.Stock.ContainsKey("food_protein"));
        Assert.True(settlement.Stock.ContainsKey("food_grain"));
        Assert.True(settlement.Stock.ContainsKey("food_sweets"));
    }

    [Fact]
    public void InitializeSettlement_StocksMedicine_WhenBiomeTierMatch()
    {
        var state = Fresh();
        // Bandages have no biome/tier restriction — should always be stocked
        var settlement = Market.InitializeSettlement("Camp", "forest", 2, SettlementSize.Camp, state, Balance, new Random(1));

        Assert.True(settlement.Stock.ContainsKey("bandages"));
    }

    [Fact]
    public void InitializeSettlement_StocksEquipment_WhenOutpostOrLarger()
    {
        var state = Fresh();
        var camp = Market.InitializeSettlement("Camp", "plains", 1, SettlementSize.Camp, state, Balance, new Random(1));
        var outpost = Market.InitializeSettlement("Outpost", "plains", 1, SettlementSize.Outpost, state, Balance, new Random(1));

        var campEquip = camp.Stock.Keys
            .Where(id => Balance.Items.TryGetValue(id, out var d) && d.Type is ItemType.Weapon or ItemType.Armor or ItemType.Boots)
            .ToList();
        var outpostEquip = outpost.Stock.Keys
            .Where(id => Balance.Items.TryGetValue(id, out var d) && d.Type is ItemType.Weapon or ItemType.Armor or ItemType.Boots)
            .ToList();

        Assert.Empty(campEquip);
        Assert.True(outpostEquip.Count >= 1);
    }

    [Fact]
    public void InitializeSettlement_StocksTools_WhenOutpostOrLarger()
    {
        var state = Fresh();
        var camp = Market.InitializeSettlement("Camp", "plains", 1, SettlementSize.Camp, state, Balance, new Random(1));
        var outpost = Market.InitializeSettlement("Outpost", "plains", 1, SettlementSize.Outpost, state, Balance, new Random(1));

        var campTools = camp.Stock.Keys
            .Where(id => Balance.Items.TryGetValue(id, out var d) && d.Type == ItemType.Tool)
            .ToList();
        var outpostTools = outpost.Stock.Keys
            .Where(id => Balance.Items.TryGetValue(id, out var d) && d.Type == ItemType.Tool)
            .ToList();

        Assert.Empty(campTools);
        Assert.True(outpostTools.Count >= 1);
    }

    [Fact]
    public void InitializeSettlement_Tools_RespectBiome()
    {
        var state = Fresh();
        // plains tier 1 tools: traders_ledger, cartographers_kit
        // scrub-only tools should not appear in plains
        var settlement = Market.InitializeSettlement("Outpost", "plains", 1, SettlementSize.City, state, Balance, new Random(1));

        var toolIds = settlement.Stock.Keys
            .Where(id => Balance.Items.TryGetValue(id, out var d) && d.Type == ItemType.Tool)
            .ToList();

        foreach (var id in toolIds)
        {
            var def = Balance.Items[id];
            Assert.True(def.Biome == null || def.Biome == "plains",
                $"Tool {id} has biome {def.Biome}, expected plains or universal");
        }
    }

    [Fact]
    public void InitializeSettlement_Tools_ExcludesDungeonOnly()
    {
        var state = Fresh();
        // lattice_ward, lead_lined_case, antivenom_kit have no Cost — should never appear
        var settlement = Market.InitializeSettlement("City", "forest", 3, SettlementSize.City, state, Balance, new Random(1));

        Assert.False(settlement.Stock.ContainsKey("lattice_ward"));
        Assert.False(settlement.Stock.ContainsKey("lead_lined_case"));
        Assert.False(settlement.Stock.ContainsKey("antivenom_kit"));
    }

    [Fact]
    public void InitializeSettlement_NoTradeGoods()
    {
        var state = Fresh();
        var settlement = Market.InitializeSettlement("City", "forest", 3, SettlementSize.City, state, Balance, new Random(1));

        // No trade goods should be in stock (they've been removed from the catalog)
        var tradeGoodCount = settlement.Stock.Keys
            .Count(id => Balance.Items.TryGetValue(id, out var d) && d.Type == ItemType.Haul);
        Assert.Equal(0, tradeGoodCount);
    }

    [Fact]
    public void Buy_Food_Success()
    {
        var state = Fresh();
        state.Gold = 100;
        var settlement = MakeSettlement();
        settlement.Prices["food_protein"] = 3;
        settlement.Stock["food_protein"] = 5;

        var result = Market.Buy(state, "food_protein", settlement, Balance, new Random(1));

        Assert.True(result.Success);
        Assert.Equal(5, settlement.Stock["food_protein"]); // food stock doesn't decrease
        Assert.Equal(97, state.Gold);
    }

    [Fact]
    public void Buy_Equipment_AutoEquips()
    {
        var state = Fresh();
        state.Gold = 100;
        var settlement = MakeSettlement();
        settlement.Prices["bodkin"] = 15;
        settlement.Stock["bodkin"] = 1;

        var result = Market.Buy(state, "bodkin", settlement, Balance, new Random(1));

        Assert.True(result.Success);
        Assert.NotNull(state.Equipment.Weapon);
        Assert.Equal("bodkin", state.Equipment.Weapon.DefId);
    }

    [Fact]
    public void Buy_FailsWhenPackFull()
    {
        var state = Fresh();
        state.Gold = 100;
        state.PackCapacity = 0;
        state.Equipment.Weapon = new ItemInstance("dagger", "Dagger");
        var settlement = MakeSettlement();
        settlement.Prices["bodkin"] = 15;
        settlement.Stock["bodkin"] = 1;

        var result = Market.Buy(state, "bodkin", settlement, Balance, new Random(1));
        Assert.False(result.Success);
        Assert.Equal(100, state.Gold);
    }

    [Fact]
    public void Buy_FailsWhenNotEnoughGold()
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
    public void ClaimHaul_Success()
    {
        var state = Fresh();
        var settlement = MakeSettlement();
        var haul = new ItemInstance("haul", "Spice Delivery")
        {
            HaulOfferId = "offer_1",
            HaulDefId = "test_haul",
            DestinationSettlementId = "dest_1",
            DestinationHint = "A plains settlement in the north",
            Payout = 30,
        };
        settlement.HaulOffers.Add(haul);

        var result = Market.ClaimHaul(state, "offer_1", settlement);

        Assert.True(result.Success);
        Assert.Contains(state.Pack, i => i.DefId == "haul");
        Assert.Contains("Spice Delivery", result.Message);
    }

    [Fact]
    public void ClaimHaul_FailsWhenPackFull()
    {
        var state = Fresh();
        state.PackCapacity = 0;
        var settlement = MakeSettlement();
        settlement.HaulOffers.Add(new ItemInstance("haul", "Test Haul")
        {
            HaulOfferId = "offer_1",
            HaulDefId = "test", DestinationSettlementId = "x",
            DestinationHint = "somewhere", Payout = 10,
        });

        var result = Market.ClaimHaul(state, "offer_1", settlement);
        Assert.False(result.Success);
    }

    [Fact]
    public void ClaimHaul_FailsWhenNotFound()
    {
        var state = Fresh();
        var settlement = MakeSettlement();

        var result = Market.ClaimHaul(state, "nonexistent", settlement);
        Assert.False(result.Success);
    }

    [Fact]
    public void ClaimHaul_RemovesFromOffers()
    {
        var state = Fresh();
        var settlement = MakeSettlement();
        settlement.HaulOffers.Add(new ItemInstance("haul", "Haul A")
        {
            HaulOfferId = "offer_a",
            HaulDefId = "a", DestinationSettlementId = "x",
            DestinationHint = "somewhere", Payout = 10,
        });
        settlement.HaulOffers.Add(new ItemInstance("haul", "Haul B")
        {
            HaulOfferId = "offer_b",
            HaulDefId = "b", DestinationSettlementId = "y",
            DestinationHint = "elsewhere", Payout = 20,
        });

        Market.ClaimHaul(state, "offer_a", settlement);

        Assert.Single(settlement.HaulOffers);
        Assert.Equal("Haul B", settlement.HaulOffers[0].DisplayName);
    }

    [Fact]
    public void Restock_ReplenishesMedicine()
    {
        var settlement = MakeSettlement();
        settlement.Stock["bandages"] = 0;
        settlement.Prices["bandages"] = 5;
        settlement.LastRestockDay = 1;

        Market.Restock(settlement, SettlementSize.Town, currentDay: 10, Balance, new Random(1));

        Assert.True(settlement.Stock["bandages"] > 0);
    }
}
