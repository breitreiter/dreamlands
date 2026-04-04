using Dreamlands.Encounter;
using Dreamlands.Game;
using Dreamlands.Map;
using Dreamlands.Orchestration;
using Dreamlands.Rules;
using Dreamlands.Tactical;

namespace TacticalSim;

// ── Platonic decks ──────────────────────────────────────────────
//
// Hand-crafted 15-card decks representing "what fun feels like."
// Cancel deck: control-kill fantasy, pairs with cautious approach.
// Aggro deck: damage-race fantasy, pairs with aggressive approach.
// These are the gold standard. Difficulty = swapping good cards for chaff.

static class PlatonicDecks
{
    public static readonly string[] Cancel =
    [
        // Cancel engine (5)
        "momentum_to_cancel",
        "momentum_to_cancel",
        "spirits_to_cancel",
        "free_cancel",
        "momentum_to_cancel",
        // Momentum fuel (4)
        "free_momentum",
        "free_momentum",
        "free_momentum",
        "free_momentum",
        // Light progress (3)
        "momentum_to_progress",
        "momentum_to_progress",
        "momentum_to_progress",
        // Chaff (3)
        "free_progress_small",
        "free_progress_small",
        "free_progress_small",
    ];

    public static readonly string[] Aggro =
    [
        // Damage engine (5)
        "momentum_to_progress_huge",
        "momentum_to_progress_large",
        "momentum_to_progress_large",
        "threat_to_progress_large",
        "momentum_to_progress_large",
        // Momentum fuel (4)
        "free_momentum",
        "free_momentum",
        "free_momentum",
        "free_momentum",
        // Bread and butter (3)
        "momentum_to_progress",
        "momentum_to_progress",
        "momentum_to_progress",
        // Chaff (3)
        "free_progress_small",
        "free_progress_small",
        "free_progress_small",
    ];

    // Quality tier for degradation — higher = swapped out first
    static readonly Dictionary<string, int> QualityTier = new()
    {
        ["free_cancel"] = 5,
        ["momentum_to_progress_huge"] = 5,
        ["spirits_to_cancel"] = 4,
        ["momentum_to_cancel"] = 4,
        ["momentum_to_progress_large"] = 4,
        ["threat_to_progress_large"] = 4,
        ["spirits_to_progress"] = 3,
        ["threat_to_progress"] = 3,
        ["spirits_to_momentum"] = 2,
        ["momentum_to_progress"] = 2,
        ["free_momentum"] = 1,
        ["free_momentum_small"] = 0,
        ["free_progress_small"] = 0,
    };

    /// <summary>Replace the N highest-quality cards with chaff.</summary>
    public static string[] Degrade(string[] deck, int n)
    {
        var indexed = deck.Select((arch, i) => (i, arch, tier: QualityTier.GetValueOrDefault(arch, 0)))
            .OrderByDescending(x => x.tier)
            .ToList();

        var result = (string[])deck.Clone();
        for (int i = 0; i < Math.Min(n, indexed.Count); i++)
            result[indexed[i].i] = "free_progress_small";
        return result;
    }
}

// ── Encounter definitions ───────────────────────────────────────

record EncounterScenario(
    string Name,
    int Clock,
    List<ChallengeDef> Challenges,
    List<OpeningDef> Openings,
    List<ApproachDef> Approaches);

static class Scenarios
{
    // Filler openings — enough to pad any deck to 15
    static readonly List<OpeningDef> StandardFiller =
    [
        new("Desperate lunge", "free_progress_small"),
        new("Use the terrain", "free_momentum"),
        new("Press the advantage", "momentum_to_progress"),
        new("Brace yourself", "free_momentum_small"),
        new("Find an angle", "free_progress_small"),
        new("Shout a challenge", "free_momentum"),
        new("Scramble forward", "momentum_to_progress"),
        new("Duck and weave", "free_progress_small"),
        new("Grit your teeth", "free_momentum_small"),
        new("Rush in", "momentum_to_progress"),
        new("Hold your ground", "free_progress_small"),
        new("Look for cover", "free_momentum"),
        new("Force a gap", "momentum_to_progress"),
    ];

    public static readonly EncounterScenario Platonic = new(
        "Platonic",
        Clock: 12,
        Challenges:
        [
            new("First wave", "Break through", 4),
            new("Second wave", "Overrun them", 5),
            new("Final stand", "End it", 8),
        ],
        Openings: StandardFiller,
        Approaches:
        [
            new(ApproachKind.Aggressive),
            new(ApproachKind.Cautious),
        ]);

    // ── Factories ─────────────────────────────────────────────────

    public static GameSession BuildSession(int seed, BalanceData? balance = null)
    {
        balance ??= BalanceData.Default;
        var player = PlayerState.NewGame("sim", seed, balance);

        var map = new Dreamlands.Map.Map(1, 1);
        map.AllNodes().First().Terrain = Terrain.Plains;
        var bundle = EncounterBundle.FromJson("""{"index":{"byId":{},"byCategory":{}},"encounters":[]}""");
        return new GameSession(player, map, bundle, balance, new Random(seed));
    }

    static T ParseSnakeCase<T>(string value) where T : struct, Enum =>
        Enum.Parse<T>(value.Replace("_", ""), ignoreCase: true);

    public static List<OpeningSnapshot> BuildDeck(string[] archetypeIds, BalanceData balance, Random rng)
    {
        var archetypes = balance.Tactical.Archetypes;
        var deck = new List<OpeningSnapshot>(archetypeIds.Length);
        foreach (var id in archetypeIds)
        {
            var arch = archetypes[id];
            deck.Add(new OpeningSnapshot
            {
                Name = id,
                CostKind = ParseSnakeCase<CostKind>(arch.CostKind),
                CostAmount = arch.CostAmount,
                EffectKind = ParseSnakeCase<EffectKind>(arch.EffectKind),
                EffectAmount = arch.EffectAmount,
            });
        }
        for (int i = deck.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (deck[i], deck[j]) = (deck[j], deck[i]);
        }
        return deck;
    }

    public static TacticalEncounter BuildEncounter(EncounterScenario scenario) => new()
    {
        Id = "sim/" + scenario.Name,
        Title = scenario.Name,
        Stat = "combat",
        Clock = scenario.Clock,
        Challenges = scenario.Challenges,
        Openings = scenario.Openings,
        Approaches = scenario.Approaches,
    };
}
