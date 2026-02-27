using Dreamlands.Rules;

namespace Dreamlands.Map;

public class Node
{
    public int X { get; }
    public int Y { get; }
    public Terrain Terrain { get; set; }
    public Direction RiverSides { get; set; }
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
    }

    public bool HasRiver => RiverSides != Direction.None;
    public bool HasRiverOn(Direction dir) => (RiverSides & dir) != 0;
    public bool IsWater => Terrain == Terrain.Lake;

    public void AddRiver(Direction dir) => RiverSides |= dir;
}
