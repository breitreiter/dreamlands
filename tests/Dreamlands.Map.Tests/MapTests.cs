using Dreamlands.Map;
using Dreamlands.Rules;
using WorldMap = Dreamlands.Map.Map;

namespace Dreamlands.MapTests;

public class MapTests
{
    static WorldMap MakeMap(int size = 3)
    {
        var map = new WorldMap(size, size);
        foreach (var node in map.AllNodes())
            node.Terrain = Terrain.Plains;
        return map;
    }

    [Fact]
    public void InBounds_InsideGrid_True()
    {
        var map = MakeMap();
        Assert.True(map.InBounds(0, 0));
        Assert.True(map.InBounds(1, 1));
        Assert.True(map.InBounds(2, 2));
    }

    [Fact]
    public void InBounds_OutsideGrid_False()
    {
        var map = MakeMap();
        Assert.False(map.InBounds(-1, 0));
        Assert.False(map.InBounds(0, -1));
        Assert.False(map.InBounds(3, 0));
        Assert.False(map.InBounds(0, 3));
    }

    [Fact]
    public void GetNeighbor_ValidDirection_ReturnsNeighbor()
    {
        var map = MakeMap();
        var center = map[1, 1];

        Assert.Equal(map[1, 0], map.GetNeighbor(center, Direction.North));
        Assert.Equal(map[1, 2], map.GetNeighbor(center, Direction.South));
        Assert.Equal(map[2, 1], map.GetNeighbor(center, Direction.East));
        Assert.Equal(map[0, 1], map.GetNeighbor(center, Direction.West));
    }

    [Fact]
    public void GetNeighbor_AtEdge_ReturnsNull()
    {
        var map = MakeMap();
        Assert.Null(map.GetNeighbor(map[0, 0], Direction.North));
        Assert.Null(map.GetNeighbor(map[0, 0], Direction.West));
        Assert.Null(map.GetNeighbor(map[2, 2], Direction.South));
        Assert.Null(map.GetNeighbor(map[2, 2], Direction.East));
    }

    [Fact]
    public void CanTraverse_BothLand_True()
    {
        var map = MakeMap();
        Assert.True(map.CanTraverse(map[1, 1], Direction.North));
    }

    [Fact]
    public void CanTraverse_TargetIsWater_False()
    {
        var map = MakeMap();
        map[1, 0].Terrain = Terrain.Lake;
        Assert.False(map.CanTraverse(map[1, 1], Direction.North));
    }

    [Fact]
    public void CanTraverse_SourceIsWater_False()
    {
        var map = MakeMap();
        map[1, 1].Terrain = Terrain.Lake;
        Assert.False(map.CanTraverse(map[1, 1], Direction.North));
    }

    [Fact]
    public void CanTraverse_AtEdge_False()
    {
        var map = MakeMap();
        Assert.False(map.CanTraverse(map[0, 0], Direction.North));
    }

    [Fact]
    public void LandNeighbors_SkipsWater()
    {
        var map = MakeMap();
        map[1, 0].Terrain = Terrain.Lake;
        map[0, 1].Terrain = Terrain.Lake;

        var neighbors = map.LandNeighbors(map[1, 1]).ToList();

        Assert.Equal(2, neighbors.Count);
        Assert.Contains(neighbors, n => n.Dir == Direction.South);
        Assert.Contains(neighbors, n => n.Dir == Direction.East);
    }
}
