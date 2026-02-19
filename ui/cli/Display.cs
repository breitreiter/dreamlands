using Dreamlands.Game;
using Dreamlands.Map;
using Dreamlands.Orchestration;
using Dreamlands.Rules;

namespace DreamlandsCli;

static class Display
{
    const string Reset = "\x1b[0m";

    public static void WriteLn(string text, ConsoleColor color)
    {
        Console.ForegroundColor = color;
        Console.WriteLine(text);
        Console.ResetColor();
    }

    public static void Write(string text, ConsoleColor color)
    {
        Console.ForegroundColor = color;
        Console.Write(text);
        Console.ResetColor();
    }

    public static void WriteStatusBar(GameSession session)
    {
        var p = session.Player;
        var node = session.CurrentNode;
        var terrain = node.Terrain;
        var regionName = node.Region?.Name ?? terrain.ToString();

        Console.WriteLine();
        Write($" HP {p.Health}/{p.MaxHealth}", ConsoleColor.Red);
        Write($"  SP {p.Spirits}/{p.MaxSpirits}", ConsoleColor.Cyan);
        Write($"  Gold {p.Gold}", ConsoleColor.Yellow);
        Write($"  Day {p.Day} {p.Time}", ConsoleColor.Gray);
        Console.WriteLine();
        Write($" {regionName}", ConsoleColor.White);
        Write($" ({terrain})", ConsoleColor.DarkGray);
        Write($"  [{p.X},{p.Y}]", ConsoleColor.DarkGray);
        if (p.CurrentDungeonId != null)
            Write($"  Dungeon: {p.CurrentDungeonId}", ConsoleColor.Magenta);
        Console.WriteLine();
    }

    public static void WriteMap(GameSession session)
    {
        var visited = session.GetVisitedNodeSet();
        MapRenderer.Render(session.Map, playerLocation: session.CurrentNode, visitedNodes: visited);
    }

    public static void WriteNodeDescription(GameSession session)
    {
        var node = session.CurrentNode;
        var terrain = node.Terrain;
        var regionName = node.Region?.Name ?? terrain.ToString();

        WriteLn($"\n--- {regionName} ---", ConsoleColor.White);

        if (node.Poi != null)
        {
            var poi = node.Poi;
            switch (poi.Kind)
            {
                case PoiKind.Settlement:
                    WriteLn($"  Settlement: {poi.Name ?? poi.Type} ({poi.Size})", ConsoleColor.Yellow);
                    break;
                case PoiKind.Dungeon:
                    var done = session.Player.CompletedDungeons.Contains(poi.DungeonId ?? "");
                    var status = done ? " (completed)" : "";
                    WriteLn($"  Dungeon: {poi.Name ?? poi.DungeonId ?? poi.Type}{status}", ConsoleColor.Magenta);
                    break;
                case PoiKind.Encounter:
                    WriteLn("  Something stirs here...", ConsoleColor.DarkYellow);
                    break;
                case PoiKind.Landmark:
                    WriteLn($"  Landmark: {poi.Name ?? poi.Type}", ConsoleColor.Cyan);
                    break;
            }
        }
    }

    public static void WriteExits(GameSession session)
    {
        var exits = Movement.GetExits(session);
        if (exits.Count == 0)
        {
            WriteLn("  No exits.", ConsoleColor.DarkGray);
            return;
        }

        Console.Write("  Exits: ");
        for (int i = 0; i < exits.Count; i++)
        {
            if (i > 0) Console.Write(", ");
            var (dir, target) = exits[i];
            var dirChar = dir switch
            {
                Direction.North => "n",
                Direction.South => "s",
                Direction.East => "e",
                Direction.West => "w",
                _ => "?"
            };
            Write($"{dirChar}", ConsoleColor.White);
            Write($"({target.Terrain.ToString()[..3]})", ConsoleColor.DarkGray);
        }
        Console.WriteLine();
    }

    public static void WriteMechanicResults(List<MechanicResult> results)
    {
        foreach (var r in results)
        {
            var (text, color) = r switch
            {
                MechanicResult.HealthChanged h =>
                    (h.Delta >= 0 ? $"  Health +{h.Delta} (now {h.NewValue})" : $"  Health {h.Delta} (now {h.NewValue})",
                     h.Delta >= 0 ? ConsoleColor.Green : ConsoleColor.Red),
                MechanicResult.SpiritsChanged s =>
                    (s.Delta >= 0 ? $"  Spirits +{s.Delta} (now {s.NewValue})" : $"  Spirits {s.Delta} (now {s.NewValue})",
                     s.Delta >= 0 ? ConsoleColor.Cyan : ConsoleColor.DarkCyan),
                MechanicResult.GoldChanged g =>
                    (g.Delta >= 0 ? $"  Gold +{g.Delta} (now {g.NewValue})" : $"  Gold {g.Delta} (now {g.NewValue})",
                     ConsoleColor.Yellow),
                MechanicResult.SkillChanged sk =>
                    ($"  {sk.Skill.GetInfo().DisplayName} {(sk.Delta >= 0 ? "+" : "")}{sk.Delta} (now {sk.NewValue})",
                     ConsoleColor.White),
                MechanicResult.ItemGained ig =>
                    ($"  Gained: {ig.DisplayName}", ConsoleColor.Green),
                MechanicResult.ItemLost il =>
                    ($"  Lost: {il.DisplayName}", ConsoleColor.Red),
                MechanicResult.TagAdded t =>
                    ($"  [{t.TagId}]", ConsoleColor.DarkGray),
                MechanicResult.TagRemoved t =>
                    ($"  [removed {t.TagId}]", ConsoleColor.DarkGray),
                MechanicResult.ConditionAdded c =>
                    ($"  Condition: {c.ConditionId}", ConsoleColor.DarkYellow),
                MechanicResult.TimeAdvanced ta =>
                    ($"  Time: Day {ta.NewDay}, {ta.NewPeriod}", ConsoleColor.Gray),
                MechanicResult.Navigation nav =>
                    ($"  -> {nav.EncounterId}", ConsoleColor.Cyan),
                MechanicResult.DungeonFinished =>
                    ("  Dungeon completed!", ConsoleColor.Green),
                MechanicResult.DungeonFled =>
                    ("  Fled the dungeon!", ConsoleColor.Yellow),
                _ => ("", ConsoleColor.Gray)
            };
            if (!string.IsNullOrEmpty(text))
                WriteLn(text, color);
        }
    }

    public static void WriteSkillCheck(SkillCheckResult check)
    {
        var color = check.Passed ? ConsoleColor.Green : ConsoleColor.Red;
        var result = check.Passed ? "PASSED" : "FAILED";
        var total = check.Rolled + check.Modifier;
        WriteLn($"  [{check.Skill.GetInfo().DisplayName} {check.SkillLevel} — d20({check.Rolled}){check.Modifier:+0;-0} = {total} vs DC {check.Target} — {result}]", color);
    }
}
