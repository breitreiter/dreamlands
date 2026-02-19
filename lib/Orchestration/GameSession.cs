using Dreamlands.Encounter;
using Dreamlands.Game;
using Dreamlands.Rules;

namespace Dreamlands.Orchestration;

public enum SessionMode { Exploring, InEncounter, GameOver }

public class GameSession
{
    public PlayerState Player { get; }
    public Dreamlands.Map.Map Map { get; }
    public EncounterBundle Bundle { get; }
    public BalanceData Balance { get; }
    public Random Rng { get; }

    public SessionMode Mode { get; set; } = SessionMode.Exploring;
    public Encounter.Encounter? CurrentEncounter { get; set; }
    public bool SkipEncounterTrigger { get; set; }

    public GameSession(PlayerState player, Dreamlands.Map.Map map, EncounterBundle bundle, BalanceData balance, Random rng)
    {
        Player = player;
        Map = map;
        Bundle = bundle;
        Balance = balance;
        Rng = rng;
    }

    public Dreamlands.Map.Node CurrentNode => Map[Player.X, Player.Y];

    public HashSet<Dreamlands.Map.Node> GetVisitedNodeSet()
    {
        var set = new HashSet<Dreamlands.Map.Node>();
        foreach (var encoded in Player.VisitedNodes)
        {
            var (x, y) = PlayerState.DecodePosition(encoded);
            if (Map.InBounds(x, y))
                set.Add(Map[x, y]);
        }
        return set;
    }

    public void MarkVisited()
    {
        Player.VisitedNodes.Add(PlayerState.EncodePosition(Player.X, Player.Y));
    }
}
