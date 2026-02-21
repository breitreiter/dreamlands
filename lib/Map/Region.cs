using Dreamlands.Rules;

namespace Dreamlands.Map;

public class Region
{
    public int Id { get; }
    public Terrain Terrain { get; }
    public string? Name { get; set; }
    public int Tier { get; set; }
    public List<Node> Nodes { get; } = new();

    public Region(int id, Terrain terrain)
    {
        Id = id;
        Terrain = terrain;
    }

    public int Size => Nodes.Count;
}
