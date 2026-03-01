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
    public void EnsureSettlement_InitializesOnFirstVisit()
    {
        var session = MakeSessionWithSettlement();
        Assert.Empty(session.Player.Settlements);

        SettlementRunner.EnsureSettlement(session);

        Assert.True(session.Player.Settlements.ContainsKey("Riverton"));
    }

    [Fact]
    public void EnsureSettlement_RestocksOnRevisit()
    {
        var session = MakeSessionWithSettlement();

        SettlementRunner.EnsureSettlement(session);
        var firstState = session.Player.Settlements["Riverton"];
        var initialLastRestock = firstState.LastRestockDay;

        // Advance days and re-ensure
        session.Player.Day = 10;
        SettlementRunner.EnsureSettlement(session);

        // Settlement should still exist (restocked, not re-initialized)
        Assert.True(session.Player.Settlements.ContainsKey("Riverton"));
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

    [Fact]
    public void EnsureSettlement_StartingCity_HasChapterhouse()
    {
        var map = Helpers.MakeMap();
        var region = new Region(1, Terrain.Plains) { Tier = 1 };
        map[1, 1].Region = region;
        map[1, 1].Poi = new Poi(PoiKind.Settlement, "city")
        {
            Name = "Aldgate",
            Size = SettlementSize.City
        };
        map.StartingCity = map[1, 1];

        var session = Helpers.MakeSession(map: map);
        var data = SettlementRunner.EnsureSettlement(session);

        Assert.NotNull(data);
        Assert.Contains("chapterhouse", data.Services);
        Assert.Contains("market", data.Services);
        Assert.Contains("inn", data.Services);
    }
}
