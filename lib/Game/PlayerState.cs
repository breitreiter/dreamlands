using Dreamlands.Rules;

namespace Dreamlands.Game;

/// <summary>
/// Complete game state for one playthrough. JSON-serializable for Cosmos DB.
/// All operations are pure mutations on this object — no session state, no singletons.
/// </summary>
public class PlayerState
{
    // Identity
    public string GameId { get; set; } = "";
    public int Seed { get; set; }

    // Position
    public int X { get; set; }
    public int Y { get; set; }
    public string? CurrentDungeonId { get; set; }
    public string? CurrentSettlementId { get; set; }
    public string? CurrentEncounterId { get; set; }

    // Vitals
    public int Health { get; set; }
    public int MaxHealth { get; set; }
    public int Spirits { get; set; }
    public int MaxSpirits { get; set; }
    public int Gold { get; set; }

    // Skills
    public Dictionary<Skill, int> Skills { get; set; } = new();
    public bool SkipEncounterTrigger { get; set; }

    // Inventory
    public List<ItemInstance> Pack { get; set; } = new();
    public int PackCapacity { get; set; } = 10;
    public List<ItemInstance> Haversack { get; set; } = new();
    public int HaversackCapacity { get; set; } = 10;
    public EquippedGear Equipment { get; set; } = new();

    // Time
    public TimePeriod Time { get; set; } = TimePeriod.Morning;
    public int Day { get; set; } = 1;

    // End-of-day flags (set by skip_time, consumed by EndOfDay.Resolve)
    public bool PendingEndOfDay { get; set; }
    public bool PendingNoSleep { get; set; }
    public bool PendingNoMeal { get; set; }
    public bool PendingNoBiome { get; set; }

    // World state
    public HashSet<string> Tags { get; set; } = new();
    public Dictionary<string, int> ActiveConditions { get; set; } = new();
    public HashSet<string> CompletedDungeons { get; set; } = new();
    public HashSet<string> UsedEncounterIds { get; set; } = new();
    public HashSet<long> VisitedNodes { get; set; } = new();
    public Dictionary<string, SettlementState> Settlements { get; set; } = new();
    public HashSet<string> ClaimedFeaturedBuys { get; set; } = new();

    /// <summary>Encode (x, y) into a single long for VisitedNodes.</summary>
    public static long EncodePosition(int x, int y) => ((long)x << 32) | (uint)y;

    /// <summary>Decode a VisitedNodes entry back to (x, y).</summary>
    public static (int X, int Y) DecodePosition(long encoded) =>
        ((int)(encoded >> 32), (int)(encoded & 0xFFFFFFFF));

    /// <summary>Create a new game with starting stats from balance data.</summary>
    public static PlayerState NewGame(string gameId, int seed, BalanceData balance)
    {
        var state = new PlayerState
        {
            GameId = gameId,
            Seed = seed,
            Health = balance.Character.StartingHealth,
            MaxHealth = balance.Character.StartingHealth,
            Spirits = balance.Character.StartingSpirits,
            MaxSpirits = balance.Character.StartingSpirits,
            Gold = balance.Character.StartingGold,
            PackCapacity = balance.Character.StartingPackSlots,
            HaversackCapacity = balance.Character.StartingHaversackSlots,
        };

        // All skills start at 0 (untrained) — the intro encounter sets real values
        foreach (var skillInfo in Rules.Skills.All)
            state.Skills[skillInfo.Skill] = 0;

        return state;
    }
}
