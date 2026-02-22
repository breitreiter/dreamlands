using Dreamlands.Rules;

namespace Dreamlands.Map;

public class Node
{
    public int X { get; }
    public int Y { get; }
    public Terrain Terrain { get; set; }
    public Direction RiverSides { get; set; }
    public Direction Crossings { get; set; }
    public int LakeNeighbors { get; set; }
    public int DistanceFromCity { get; set; } = int.MaxValue;
    public Region? Region { get; set; }
    public string? Description { get; set; }
    public Poi? Poi { get; set; }

    public Node(int x, int y, Terrain terrain = Terrain.Plains)
    {
        X = x;
        Y = y;
        Terrain = terrain;
        RiverSides = Direction.None;
        Crossings = Direction.None;
    }

    public bool HasRiver => RiverSides != Direction.None;
    public bool HasRiverOn(Direction dir) => (RiverSides & dir) != 0;
    public bool IsCrossableOn(Direction dir) => (Crossings & dir) != 0;
    public bool IsLakeAdjacent => LakeNeighbors > 0 && !IsWater;
    public bool IsWater => Terrain == Terrain.Lake;

    public void AddRiver(Direction dir) => RiverSides |= dir;
    public void AddCrossing(Direction dir) => Crossings |= dir;
}
