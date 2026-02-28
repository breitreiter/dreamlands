using Dreamlands.Rules;

namespace Dreamlands.Map;

public class Node
{
    public int X { get; }
    public int Y { get; }
    public Terrain Terrain { get; set; }
    public int DistanceFromCity { get; set; } = int.MaxValue;
    public Region? Region { get; set; }
    public string? Description { get; set; }
    public Poi? Poi { get; set; }

    public Node(int x, int y, Terrain terrain = Terrain.Plains)
    {
        X = x;
        Y = y;
        Terrain = terrain;
    }

    public bool IsWater => Terrain == Terrain.Lake;
}
