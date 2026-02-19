using Dreamlands.Map;

namespace Dreamlands.Orchestration;

public static class Movement
{
    public static List<(Direction Dir, Dreamlands.Map.Node Target)> GetExits(GameSession session)
    {
        var node = session.CurrentNode;
        var exits = new List<(Direction, Dreamlands.Map.Node)>();
        foreach (var dir in DirectionExtensions.Each())
        {
            if (node.HasConnection(dir))
            {
                var neighbor = session.Map.GetNeighbor(node, dir);
                if (neighbor != null)
                    exits.Add((dir, neighbor));
            }
        }
        return exits;
    }

    public static Dreamlands.Map.Node? TryMove(GameSession session, Direction dir)
    {
        var node = session.CurrentNode;
        if (!node.HasConnection(dir))
            return null;
        return session.Map.GetNeighbor(node, dir);
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
