using Dreamlands.Map;

namespace Dreamlands.Orchestration;

public static class Movement
{
    public static List<(Direction Dir, Dreamlands.Map.Node Target)> GetExits(GameSession session)
    {
        var map = session.Map;
        var node = session.CurrentNode;
        var exits = new List<(Direction, Dreamlands.Map.Node)>();
        foreach (var (dir, neighbor) in map.LandNeighbors(node))
            exits.Add((dir, neighbor));
        return exits;
    }

    public static Dreamlands.Map.Node? TryMove(GameSession session, Direction dir)
    {
        var map = session.Map;
        var node = session.CurrentNode;
        if (!map.CanTraverse(node, dir))
            return null;
        return map.GetNeighbor(node, dir);
    }

    public static Dreamlands.Map.Node Execute(GameSession session, Direction dir)
    {
        var target = TryMove(session, dir)
            ?? throw new InvalidOperationException($"No connection {dir} from ({session.Player.X},{session.Player.Y})");
        session.Player.X = target.X;
        session.Player.Y = target.Y;
        session.MarkVisited();
        return target;
    }
}
