using Dreamlands.Encounter;
using Dreamlands.Game;
using Dreamlands.Map;
using Dreamlands.Orchestration;
using Dreamlands.Rules;

namespace DreamlandsCli;

class Program
{
    static int Main(string[] args)
    {
        string? mapPath = null;
        string? bundlePath = null;

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--map" && i + 1 < args.Length) { mapPath = args[++i]; }
            else if (args[i] == "--bundle" && i + 1 < args.Length) { bundlePath = args[++i]; }
        }

        if (mapPath == null || bundlePath == null)
        {
            Console.Error.WriteLine("Usage: dreamlands-cli --map <map.json> --bundle <encounters.bundle.json>");
            return 1;
        }

        Console.Error.WriteLine("Loading...");

        Map map;
        EncounterBundle bundle;
        try
        {
            map = MapSerializer.Load(mapPath);
            bundle = EncounterBundle.Load(bundlePath);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to load game data: {ex.Message}");
            return 1;
        }

        var balance = BalanceData.Default;
        var rng = new Random();
        var player = PlayerState.NewGame(Guid.NewGuid().ToString("N"), rng.Next(), balance);

        // Place player at starting city
        if (map.StartingCity != null)
        {
            player.X = map.StartingCity.X;
            player.Y = map.StartingCity.Y;
        }

        var session = new GameSession(player, map, bundle, balance, rng);
        session.MarkVisited();

        Console.Error.WriteLine("Ready.");
        Console.WriteLine();

        while (session.Mode != SessionMode.GameOver)
        {
            if (session.Mode == SessionMode.Exploring)
                ExploreMode.Run(session);
            else if (session.Mode == SessionMode.InEncounter)
                EncounterMode.Run(session);
        }

        Display.WriteLn("\nYou have perished in the Dreamlands.", ConsoleColor.Red);
        Display.WriteStatusBar(session);
        return 0;
    }
}
