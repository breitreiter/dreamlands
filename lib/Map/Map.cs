namespace Dreamlands.Map;

public class Map
{
    private readonly Node[,] _nodes;

    public int Width { get; }
    public int Height { get; }
    public List<Region> Regions { get; } = new();
    public Node? StartingCity { get; set; }

    public Map(int width, int height)
    {
        Width = width;
        Height = height;
        _nodes = new Node[width, height];

        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
            _nodes[x, y] = new Node(x, y);
    }

    public Node this[int x, int y] => _nodes[x, y];

    public bool InBounds(int x, int y) =>
        x >= 0 && x < Width && y >= 0 && y < Height;

    public Node? GetNeighbor(Node node, Direction dir)
    {
        var (dx, dy) = dir.ToOffset();
        int nx = node.X + dx;
        int ny = node.Y + dy;
        return InBounds(nx, ny) ? _nodes[nx, ny] : null;
    }

    public bool CanTraverse(Node node, Direction dir)
    {
        var neighbor = GetNeighbor(node, dir);
        if (neighbor == null || neighbor.IsWater || node.IsWater) return false;
        return true;
    }

    public IEnumerable<(Direction Dir, Node Neighbor)> LandNeighbors(Node node)
    {
        foreach (var dir in DirectionExtensions.Each())
        {
            if (CanTraverse(node, dir))
                yield return (dir, GetNeighbor(node, dir)!);
        }
    }

    public IEnumerable<Node> AllNodes()
    {
        for (int y = 0; y < Height; y++)
        for (int x = 0; x < Width; x++)
            yield return _nodes[x, y];
    }
}
