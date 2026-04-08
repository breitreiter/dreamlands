using Dreamlands.Map;
using Dreamlands.Orchestration;
using Dreamlands.Rules;

namespace Dreamlands.Orchestration.Tests;

public class SettlementRunnerTests
{
    static GameSession MakeSessionWithSettlement(SettlementSize size = SettlementSize.Town)
    {
        var map = Helpers.MakeMap();
        var region = new Region(1, Terrain.Plains) { Tier = 1 };
        map[1, 1].Region = region;
        map[1, 1].Poi = new Poi(PoiKind.Settlement, "town")
        {
            Name = "Riverton",
            SettlementId = "s1_1",
            Size = size
        };

        return Helpers.MakeSession(map: map);
    }

    [Fact]
    public void EnsureSettlement_AtSettlement_ReturnsData()
    {
        var session = MakeSessionWithSettlement();
        var data = SettlementRunner.EnsureSettlement(session);

        Assert.NotNull(data);
        Assert.Equal("Riverton", data.Name);
        Assert.Equal(1, data.Tier);
        Assert.Equal("plains", data.Biome);
        Assert.Equal(SettlementSize.Town, data.Size);
    }

    [Fact]
    public void EnsureSettlement_NotAtSettlement_ReturnsNull()
    {
        var session = Helpers.MakeSession();
        Assert.Null(SettlementRunner.EnsureSettlement(session));
    }

    [Fact]
    public void EnsureSettlement_ClearsClearedOnSettlementConditions()
    {
        var session = MakeSessionWithSettlement();
        session.Player.ActiveConditions.Add("freezing");
        session.Player.ActiveConditions.Add("thirsty");
        session.Player.ActiveConditions.Add("exhausted");
        session.Player.ActiveConditions.Add("lost");
        session.Player.ActiveConditions.Add("injured"); // serious — should NOT clear

        SettlementRunner.EnsureSettlement(session);

        Assert.DoesNotContain("freezing", session.Player.ActiveConditions);
        Assert.DoesNotContain("thirsty", session.Player.ActiveConditions);
        Assert.DoesNotContain("exhausted", session.Player.ActiveConditions);
        Assert.DoesNotContain("lost", session.Player.ActiveConditions);
        Assert.Contains("injured", session.Player.ActiveConditions);
    }

    [Fact]
    public void EnsureSettlement_ResetsConsecutiveWildernessNights()
    {
        var session = MakeSessionWithSettlement();
        session.Player.ConsecutiveWildernessNights = 7;

        SettlementRunner.EnsureSettlement(session);

        Assert.Equal(0, session.Player.ConsecutiveWildernessNights);
    }

    [Fact]
    public void EnsureSettlement_DoesNotAutoRefillRations()
    {
        // Players restock rations explicitly via the market's "Restock food and leave"
        // button, not as a side effect of settlement entry.
        var session = MakeSessionWithSettlement();
        Assert.Empty(session.Player.Haversack);

        SettlementRunner.EnsureSettlement(session);

        Assert.Empty(session.Player.Haversack);
    }

    [Fact]
    public void EnsureSettlement_InitializesOnFirstVisit()
    {
        var session = MakeSessionWithSettlement();
        Assert.Empty(session.Player.Settlements);

        SettlementRunner.EnsureSettlement(session);

        Assert.True(session.Player.Settlements.ContainsKey("s1_1"));
    }

    [Fact]
    public void EnsureSettlement_RestocksOnRevisit()
    {
        var session = MakeSessionWithSettlement();

        SettlementRunner.EnsureSettlement(session);
        var firstState = session.Player.Settlements["s1_1"];
        var initialLastRestock = firstState.LastRestockDay;

        // Advance days and re-ensure
        session.Player.Day = 10;
        SettlementRunner.EnsureSettlement(session);

        // Settlement should still exist (restocked, not re-initialized)
        Assert.True(session.Player.Settlements.ContainsKey("s1_1"));
    }

    [Fact]
    public void EnsureSettlement_DoesNotChangeMode()
    {
        var session = MakeSessionWithSettlement();
        Assert.Equal(SessionMode.Exploring, session.Mode);

        SettlementRunner.EnsureSettlement(session);

        Assert.Equal(SessionMode.Exploring, session.Mode);
    }

    [Fact]
    public void EnsureSettlement_IncludesMarketAndInn()
    {
        var session = MakeSessionWithSettlement();
        var data = SettlementRunner.EnsureSettlement(session);

        Assert.NotNull(data);
        Assert.Contains("market", data.Services);
        Assert.Contains("inn", data.Services);
    }

    [Fact]
    public void EnsureSettlement_NormalSettlement_NoChapterhouse()
    {
        var session = MakeSessionWithSettlement();
        var data = SettlementRunner.EnsureSettlement(session);

        Assert.NotNull(data);
        Assert.DoesNotContain("chapterhouse", data.Services);
    }

    // ── Haul Generation ──

    static GameSession MakeSessionWithTradeGraph(int playerX = 2, int playerY = 0)
    {
        var map = Helpers.MakeMap(size: 5);

        var plainsRegion = new Region(1, Terrain.Plains) { Tier = 1 };
        var forestRegion = new Region(2, Terrain.Forest) { Tier = 2 };

        // Root: Plains city at (2,2)
        map[2, 2].Region = plainsRegion;
        map[2, 2].Poi = new Poi(PoiKind.Settlement, "city")
        {
            Name = "Aldgate", SettlementId = "root",
            Size = SettlementSize.City,
            TradeChildIds = ["mid"]
        };
        map.StartingCity = map[2, 2];

        // Mid: Plains town at (2,0)
        map[2, 0].Region = plainsRegion;
        map[2, 0].Poi = new Poi(PoiKind.Settlement, "town")
        {
            Name = "Riverton", SettlementId = "mid",
            Size = SettlementSize.Town,
            TradeParentId = "root",
            TradeChildIds = ["leaf"]
        };

        // Leaf: Forest village at (4,0)
        map[4, 0].Region = forestRegion;
        map[4, 0].Terrain = Terrain.Forest;
        map[4, 0].Poi = new Poi(PoiKind.Settlement, "village")
        {
            Name = "Woodhaven", SettlementId = "leaf",
            Size = SettlementSize.Village,
            TradeParentId = "mid"
        };

        return Helpers.MakeSession(map: map, playerX: playerX, playerY: playerY);
    }

    [Fact]
    public void EnsureSettlement_GeneratesHauls_PrefersDownward()
    {
        // Player at mid (depth 1): hop-2 candidates are root (depth 0) and leaf (depth 2)
        // Downward = [leaf], upward = [root]. First Generate call uses downward.
        var session = MakeSessionWithTradeGraph(playerX: 2, playerY: 0);

        SettlementRunner.EnsureSettlement(session);

        var settlement = session.Player.Settlements["mid"];
        Assert.NotEmpty(settlement.HaulOffers);
        // First offer should target the downward destination
        Assert.Contains(settlement.HaulOffers, o => o.DestinationSettlementId == "leaf");
    }

    [Fact]
    public void EnsureSettlement_GeneratesHauls_FillsWithUpward()
    {
        // Mid is non-leaf, cap=2. Downward has 1 candidate (leaf), upward has 1 (root).
        // Should fill both slots.
        var session = MakeSessionWithTradeGraph(playerX: 2, playerY: 0);

        SettlementRunner.EnsureSettlement(session);

        var settlement = session.Player.Settlements["mid"];
        Assert.Equal(2, settlement.HaulOffers.Count);
        Assert.Contains(settlement.HaulOffers, o => o.DestinationSettlementId == "leaf");
        Assert.Contains(settlement.HaulOffers, o => o.DestinationSettlementId == "root");
    }

    [Fact]
    public void EnsureSettlement_GeneratesHauls_LeafFallsBackToUpward()
    {
        // Player at leaf (depth 2): hop-2 = [root (depth 0)], all upward. No downward candidates.
        var session = MakeSessionWithTradeGraph(playerX: 4, playerY: 0);

        SettlementRunner.EnsureSettlement(session);

        var settlement = session.Player.Settlements["leaf"];
        Assert.NotEmpty(settlement.HaulOffers);
        // Only reachable candidate at hop-2 is root (upward)
        Assert.All(settlement.HaulOffers, o => Assert.Equal("root", o.DestinationSettlementId));
    }

    // ── Haul Restock Pacing ──

    [Fact]
    public void HaulRestock_NoRefillBeforeCooldown()
    {
        var session = MakeSessionWithTradeGraph(playerX: 2, playerY: 0);
        SettlementRunner.EnsureSettlement(session);
        var state = session.Player.Settlements["mid"];

        // Player takes one haul, leaving one slot empty
        state.HaulOffers.RemoveAt(0);
        Assert.Single(state.HaulOffers);

        // Revisit 1 day later — below hub cooldown (4 days)
        session.Player.Day = 2;
        SettlementRunner.EnsureSettlement(session);

        // Slot should NOT refill yet
        Assert.Single(state.HaulOffers);
    }

    [Fact]
    public void HaulRestock_HubRefillsAfterCooldown()
    {
        var session = MakeSessionWithTradeGraph(playerX: 2, playerY: 0);
        SettlementRunner.EnsureSettlement(session);
        var state = session.Player.Settlements["mid"];
        var originalOffers = state.HaulOffers.Select(o => o.HaulOfferId).ToList();

        // Advance past hub cooldown (4 days)
        session.Player.Day = 5;
        SettlementRunner.EnsureSettlement(session);

        // Old offers cleared, new ones generated — 1 tick = 1 slot
        Assert.Single(state.HaulOffers);
        Assert.DoesNotContain(state.HaulOffers[0].HaulOfferId, originalOffers);
    }

    [Fact]
    public void HaulRestock_TwoTicksRefillsBothSlots()
    {
        var session = MakeSessionWithTradeGraph(playerX: 2, playerY: 0);
        SettlementRunner.EnsureSettlement(session);
        var state = session.Player.Settlements["mid"];

        // Advance 2 hub ticks (8 days)
        session.Player.Day = 9;
        SettlementRunner.EnsureSettlement(session);

        Assert.Equal(2, state.HaulOffers.Count);
    }

    [Fact]
    public void HaulRestock_LeafSlowerThanHub()
    {
        var session = MakeSessionWithTradeGraph(playerX: 4, playerY: 0);
        SettlementRunner.EnsureSettlement(session);
        var state = session.Player.Settlements["leaf"];

        // Advance 5 days — past hub cooldown but below leaf cooldown (8 days)
        session.Player.Day = 6;
        SettlementRunner.EnsureSettlement(session);

        // Leaf should not have restocked yet — still has original offer
        Assert.Single(state.HaulOffers);
    }

    [Fact]
    public void HaulRestock_LeafRefillsAfterLeafCooldown()
    {
        var session = MakeSessionWithTradeGraph(playerX: 4, playerY: 0);
        SettlementRunner.EnsureSettlement(session);
        var state = session.Player.Settlements["leaf"];
        var originalId = state.HaulOffers[0].HaulOfferId;

        // Advance past leaf cooldown (8 days)
        session.Player.Day = 9;
        SettlementRunner.EnsureSettlement(session);

        // Old offer cleared, new one generated
        Assert.Single(state.HaulOffers);
        Assert.NotEqual(originalId, state.HaulOffers[0].HaulOfferId);
    }

    [Fact]
    public void EnsureSettlement_StartingCity_HasChapterhouse()
    {
        var map = Helpers.MakeMap();
        var region = new Region(1, Terrain.Plains) { Tier = 1 };
        map[1, 1].Region = region;
        map[1, 1].Poi = new Poi(PoiKind.Settlement, "city")
        {
            Name = "Aldgate",
            SettlementId = "s1_1",
            Size = SettlementSize.City
        };
        map.StartingCity = map[1, 1];

        var session = Helpers.MakeSession(map: map);
        var data = SettlementRunner.EnsureSettlement(session);

        Assert.NotNull(data);
        Assert.Contains("chapterhouse", data.Services);
        Assert.Contains("market", data.Services);
        // Starting city has chapterhouse instead of inn, not both
        Assert.DoesNotContain("inn", data.Services);
    }
}
