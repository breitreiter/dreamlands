using Dreamlands.Map;
using Dreamlands.Orchestration;
using Dreamlands.Rules;

namespace Dreamlands.Orchestration.Tests;

public class MovementTests
{
    [Fact]
    public void GetExits_CenterOfGrid_FourExits()
    {
        var session = Helpers.MakeSession();
        var exits = Movement.GetExits(session);
        Assert.Equal(4, exits.Count);
    }

    [Fact]
    public void GetExits_CornerOfGrid_TwoExits()
    {
        var session = Helpers.MakeSession(playerX: 0, playerY: 0);
        var exits = Movement.GetExits(session);
        Assert.Equal(2, exits.Count);
    }

    [Fact]
    public void GetExits_AdjacentWater_ExcludesWater()
    {
        var map = Helpers.MakeMap();
        map[1, 0].Terrain = Terrain.Lake;
        map[0, 1].Terrain = Terrain.Lake;
        var session = Helpers.MakeSession(map: map);

        var exits = Movement.GetExits(session);

        Assert.Equal(2, exits.Count);
        Assert.Contains(exits, e => e.Dir == Direction.South);
        Assert.Contains(exits, e => e.Dir == Direction.East);
    }

    [Fact]
    public void TryMove_ValidDirection_ReturnsTarget()
    {
        var session = Helpers.MakeSession();
        var target = Movement.TryMove(session, Direction.North);

        Assert.NotNull(target);
        Assert.Equal(1, target.X);
        Assert.Equal(0, target.Y);
    }

    [Fact]
    public void TryMove_IntoWater_ReturnsNull()
    {
        var map = Helpers.MakeMap();
        map[1, 0].Terrain = Terrain.Lake;
        var session = Helpers.MakeSession(map: map);

        Assert.Null(Movement.TryMove(session, Direction.North));
    }

    [Fact]
    public void TryMove_OffEdge_ReturnsNull()
    {
        var session = Helpers.MakeSession(playerX: 0, playerY: 0);
        Assert.Null(Movement.TryMove(session, Direction.North));
        Assert.Null(Movement.TryMove(session, Direction.West));
    }

    [Fact]
    public void Execute_UpdatesPlayerPosition()
    {
        var session = Helpers.MakeSession();
        Movement.Execute(session, Direction.North);

        Assert.Equal(1, session.Player.X);
        Assert.Equal(0, session.Player.Y);
    }

    [Fact]
    public void Execute_MarksNodeVisited()
    {
        var session = Helpers.MakeSession();
        Movement.Execute(session, Direction.North);

        var visited = session.GetVisitedNodeSet();
        Assert.Contains(session.Map[1, 0], visited);
    }

    [Fact]
    public void Execute_InvalidDirection_Throws()
    {
        var map = Helpers.MakeMap();
        map[1, 0].Terrain = Terrain.Lake;
        var session = Helpers.MakeSession(map: map);

        Assert.Throws<InvalidOperationException>(() => Movement.Execute(session, Direction.North));
    }
}
