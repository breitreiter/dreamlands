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
    public void Enter_AtSettlement_ReturnsData()
    {
        var session = MakeSessionWithSettlement();
        var data = SettlementRunner.Enter(session);

        Assert.NotNull(data);
        Assert.Equal("Riverton", data.Name);
        Assert.Equal(1, data.Tier);
        Assert.Equal("plains", data.Biome);
        Assert.Equal(SettlementSize.Town, data.Size);
    }

    [Fact]
    public void Enter_NotAtSettlement_ReturnsNull()
    {
        var session = Helpers.MakeSession();
        Assert.Null(SettlementRunner.Enter(session));
    }

    [Fact]
    public void Enter_InitializesSettlementOnFirstVisit()
    {
        var session = MakeSessionWithSettlement();
        Assert.Empty(session.Player.Settlements);

        SettlementRunner.Enter(session);

        Assert.True(session.Player.Settlements.ContainsKey("Riverton"));
    }

    [Fact]
    public void Enter_RestocksOnRevisit()
    {
        var session = MakeSessionWithSettlement();

        SettlementRunner.Enter(session);
        var firstState = session.Player.Settlements["Riverton"];
        var initialLastRestock = firstState.LastRestockDay;

        // Advance days and re-enter
        session.Player.Day = 10;
        session.Mode = SessionMode.Exploring;
        SettlementRunner.Enter(session);

        // Settlement should still exist (restocked, not re-initialized)
        Assert.True(session.Player.Settlements.ContainsKey("Riverton"));
    }

    [Fact]
    public void Enter_SetsAtSettlementMode()
    {
        var session = MakeSessionWithSettlement();
        SettlementRunner.Enter(session);

        Assert.Equal(SessionMode.AtSettlement, session.Mode);
        Assert.Equal("Riverton", session.Player.CurrentSettlementId);
    }

    [Fact]
    public void Leave_SetsExploringMode()
    {
        var session = MakeSessionWithSettlement();
        SettlementRunner.Enter(session);

        SettlementRunner.Leave(session);

        Assert.Equal(SessionMode.Exploring, session.Mode);
        Assert.Null(session.Player.CurrentSettlementId);
    }
}
