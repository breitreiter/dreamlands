namespace Dreamlands.Map;

[Flags]
public enum Direction
{
    None  = 0,
    North = 1,
    South = 2,
    East  = 4,
    West  = 8
}

public static class DirectionExtensions
{
    public static Direction Opposite(this Direction dir) => dir switch
    {
        Direction.North => Direction.South,
        Direction.South => Direction.North,
        Direction.East => Direction.West,
        Direction.West => Direction.East,
        _ => Direction.None
    };

    public static (int dx, int dy) ToOffset(this Direction dir) => dir switch
    {
        Direction.North => (0, -1),
        Direction.South => (0, 1),
        Direction.East => (1, 0),
        Direction.West => (-1, 0),
        _ => (0, 0)
    };

    public static IEnumerable<Direction> Each()
    {
        yield return Direction.North;
        yield return Direction.South;
        yield return Direction.East;
        yield return Direction.West;
    }
}
