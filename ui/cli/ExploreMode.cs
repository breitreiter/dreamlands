using System.Linq;
using Dreamlands.Map;
using Dreamlands.Orchestration;
using Dreamlands.Rules;

namespace DreamlandsCli;

static class ExploreMode
{
    public static void Run(GameSession session)
    {
        Display.WriteMap(session);
        Display.WriteNodeDescription(session);
        Display.WriteExits(session);
        Display.WriteStatusBar(session);

        // Auto-trigger encounters at encounter POI nodes (skip if we just finished one here)
        var node = session.CurrentNode;
        if (node.Poi?.Kind == PoiKind.Encounter && !session.SkipEncounterTrigger)
        {
            var enc = EncounterSelection.PickOverworld(session, node);
            if (enc != null)
            {
                Display.WriteLn("\nYou stumble upon something...", ConsoleColor.DarkYellow);
                EncounterMode.StartEncounter(session, enc);
                return;
            }
        }
        session.SkipEncounterTrigger = false;

        // Prompt for dungeon entry
        if (node.Poi?.Kind == PoiKind.Dungeon && node.Poi.DungeonId != null
            && !session.Player.CompletedDungeons.Contains(node.Poi.DungeonId))
        {
            var dungeonName = node.Poi.Name ?? node.Poi.DungeonId;
            Console.Write($"\n  Enter {dungeonName}? [y/n] ");
            var answer = Console.ReadKey(intercept: true).KeyChar;
            Console.WriteLine();
            if (answer is 'y' or 'Y')
            {
                session.Player.CurrentDungeonId = node.Poi.DungeonId;
                var start = EncounterSelection.GetDungeonStart(session, node.Poi.DungeonId);
                if (start != null)
                {
                    EncounterMode.StartEncounter(session, start);
                    return;
                }
                Display.WriteLn("  The dungeon entrance is sealed.", ConsoleColor.DarkGray);
                session.Player.CurrentDungeonId = null;
            }
        }

        // Input loop — single keypress
        while (true)
        {
            WriteKeybinds();
            var key = Console.ReadKey(intercept: true).KeyChar;

            var dir = ParseDirection(key);
            if (dir != null)
            {
                var target = Movement.TryMove(session, dir.Value);
                if (target == null)
                {
                    Display.WriteLn("  Can't go that way.", ConsoleColor.DarkGray);
                    continue;
                }
                Movement.Execute(session, dir.Value);
                return;
            }

            switch (char.ToLowerInvariant(key))
            {
                case 'm':
                    Display.WriteMap(session);
                    break;
                case 'l':
                    Display.WriteNodeDescription(session);
                    Display.WriteExits(session);
                    break;
                case 't':
                    WriteFullStatus(session);
                    break;
                case 'i':
                    WriteInventory(session);
                    break;
                case '?':
                    WriteHelp();
                    break;
                case 'q':
                    session.Mode = SessionMode.GameOver;
                    return;
                default:
                    break;
            }
        }
    }

    static Direction? ParseDirection(char key) => char.ToLowerInvariant(key) switch
    {
        'n' => Direction.North,
        's' => Direction.South,
        'e' => Direction.East,
        'w' => Direction.West,
        _ => null
    };

    static void WriteFullStatus(GameSession session)
    {
        var p = session.Player;
        Display.WriteStatusBar(session);
        Console.WriteLine("  Skills:");
        foreach (var (skill, level) in p.Skills)
            Console.WriteLine($"    {skill.GetInfo().DisplayName}: {level}");
        if (p.ActiveConditions.Count > 0)
            Console.WriteLine($"  Conditions: {string.Join(", ", p.ActiveConditions.Select(c => $"{c.Key} x{c.Value}"))}");
        if (p.Tags.Count > 0)
            Console.WriteLine($"  Tags: {string.Join(", ", p.Tags)}");
        if (p.CompletedDungeons.Count > 0)
            Console.WriteLine($"  Completed dungeons: {string.Join(", ", p.CompletedDungeons)}");
    }

    static void WriteInventory(GameSession session)
    {
        var p = session.Player;
        Console.WriteLine($"\n  Pack ({p.Pack.Count}/{p.PackCapacity}):");
        if (p.Pack.Count == 0)
            Console.WriteLine("    (empty)");
        else
            foreach (var item in p.Pack)
                Console.WriteLine($"    {item.DisplayName}");

        Console.WriteLine($"  Haversack ({p.Haversack.Count}/{p.HaversackCapacity}):");
        if (p.Haversack.Count == 0)
            Console.WriteLine("    (empty)");
        else
            foreach (var item in p.Haversack)
                Console.WriteLine($"    {item.DisplayName}");

        if (p.Equipment.Weapon != null)
            Console.WriteLine($"  Weapon: {p.Equipment.Weapon.DisplayName}");
        if (p.Equipment.Armor != null)
            Console.WriteLine($"  Armor: {p.Equipment.Armor.DisplayName}");
        if (p.Equipment.Boots != null)
            Console.WriteLine($"  Boots: {p.Equipment.Boots.DisplayName}");
    }

    static void WriteKeybinds()
    {
        Console.WriteLine();
        Display.Write("  [", ConsoleColor.DarkGray);
        Display.Write("n/s/e/w", ConsoleColor.White);
        Display.Write("] move  [", ConsoleColor.DarkGray);
        Display.Write("m", ConsoleColor.White);
        Display.Write("]ap  [", ConsoleColor.DarkGray);
        Display.Write("l", ConsoleColor.White);
        Display.Write("]ook  s[", ConsoleColor.DarkGray);
        Display.Write("t", ConsoleColor.White);
        Display.Write("]atus  [", ConsoleColor.DarkGray);
        Display.Write("i", ConsoleColor.White);
        Display.Write("]nv  [", ConsoleColor.DarkGray);
        Display.Write("?", ConsoleColor.White);
        Display.Write("]help  [", ConsoleColor.DarkGray);
        Display.Write("q", ConsoleColor.White);
        Display.Write("]uit", ConsoleColor.DarkGray);
        Console.WriteLine();
    }

    static void WriteHelp()
    {
        Console.WriteLine(@"
  n/s/e/w   Move in a direction
  m         Show the map
  l         Describe current location
  t         Show full character status
  i         Show inventory
  q         Exit the game
  ?         Show this help");
    }
}
