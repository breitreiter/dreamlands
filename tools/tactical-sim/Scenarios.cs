using Dreamlands.Encounter;
using Dreamlands.Game;
using Dreamlands.Map;
using Dreamlands.Orchestration;
using Dreamlands.Rules;
using Dreamlands.Tactical;

namespace TacticalSim;

record PlayerProfile(string Name, Skill GoverningSkill, int SkillLevel, string? WeaponId, string? ArmorId = null, string[]? CustomDeck = null);

record EncounterScenario(
    string Name,
    Variant Variant,
    string Stat,
    int Resistance,
    int TimerDraw,
    List<TimerDef> Timers,
    List<OpeningDef> Openings,
    List<ApproachDef>? Approaches = null,
    List<OpeningDef>? Path = null);

static class Scenarios
{
    // ── Player profiles ───────────────────────────────────────────

    public static readonly PlayerProfile[] Profiles =
    [
        new("Early (Combat 1, bodkin)", Skill.Combat, 1, "bodkin"),
        new("Mid (Combat 2, short_sword)", Skill.Combat, 2, "short_sword"),
        new("Mid (Combat 3, tulwar)", Skill.Combat, 3, "tulwar"),
        new("Late (Combat 4, scimitar)", Skill.Combat, 4, "scimitar"),
    ];

    // ── Custom deck profiles ─────────────────────────────────────

    static readonly string[] Chaff =
    [
        "free_progress_small",
        "free_progress_small",
        "free_momentum_small",
        "free_momentum_small",
        "free_progress_small",
    ];

    public static readonly PlayerProfile[] CustomProfiles =
    [
        new("3 cancel + momentum", Skill.Combat, 0, null, CustomDeck:
        [
            // 3 cancels
            "momentum_to_cancel",
            "momentum_to_cancel",
            "spirits_to_cancel",
            // 7 momentum builders
            "free_momentum",
            "free_momentum",
            "free_momentum",
            "spirits_to_momentum",
            "spirits_to_momentum",
            "free_momentum_small",
            "free_momentum",
            // 5 chaff
            ..Chaff,
        ]),
        new("1 cancel + hybrid", Skill.Combat, 0, null, CustomDeck:
        [
            // 1 cancel
            "momentum_to_cancel",
            // 4 momentum builders
            "free_momentum",
            "free_momentum",
            "spirits_to_momentum",
            "free_momentum_small",
            // 5 damage dealers
            "momentum_to_progress",
            "momentum_to_progress",
            "momentum_to_progress_large",
            "momentum_to_progress_large",
            "threat_to_progress",
            // 5 chaff
            ..Chaff,
        ]),
        new("0 cancel + momentum/damage", Skill.Combat, 0, null, CustomDeck:
        [
            // 5 momentum builders
            "free_momentum",
            "free_momentum",
            "spirits_to_momentum",
            "free_momentum",
            "free_momentum_small",
            // 5 damage dealers
            "momentum_to_progress",
            "momentum_to_progress",
            "momentum_to_progress_large",
            "momentum_to_progress_large",
            "momentum_to_progress_huge",
            // 5 chaff
            ..Chaff,
        ]),
    ];

    // ── Encounter scenarios ───────────────────────────────────────

    static readonly List<OpeningDef> StandardFiller =
    [
        new("Scramble forward", "free_progress_small"),
        new("Brace yourself", "free_momentum_small"),
        new("Catch your breath", "free_momentum"),
        new("Dig in", "free_progress_small"),
        new("Wait for an opening", "free_momentum_small"),
        new("Steady your nerve", "free_momentum"),
    ];

    // Timer counts match the encounter's TimerDraw — approaches vary momentum/openings, not timer count.
    // Built per-encounter by ScaleApproaches().
    static List<ApproachDef> ScaleApproaches(int timerDraw) =>
    [
        new(ApproachKind.Scout, 0, timerDraw, 3),
        new(ApproachKind.Direct, 3, timerDraw),
        new(ApproachKind.Wild, 6, timerDraw),
    ];

    public static readonly EncounterScenario[] Encounters =
    [
        new("Easy (res 6, 2 timers)",
            Variant.Combat, "combat", Resistance: 6, TimerDraw: 2,
            Timers:
            [
                new("Threat", TimerEffect.Spirits, 1, 4, "Stop Threat"),
                new("Pressure", TimerEffect.Spirits, 1, 3, "Relieve Pressure"),
                new("Recovery", TimerEffect.Resistance, 1, 5, "Prevent Recovery"),
            ],
            Openings: StandardFiller,
            Approaches: ScaleApproaches(2)),

        new("Medium (res 7, 3 timers)",
            Variant.Combat, "combat", Resistance: 7, TimerDraw: 3,
            Timers:
            [
                new("Threat", TimerEffect.Spirits, 1, 4, "Stop Threat"),
                new("Pressure", TimerEffect.Spirits, 1, 3, "Relieve Pressure"),
                new("Recovery", TimerEffect.Resistance, 1, 5, "Prevent Recovery"),
                new("Menace", TimerEffect.Spirits, 1, 3, "Face the Menace"),
            ],
            Openings: StandardFiller,
            Approaches: ScaleApproaches(3)),

        new("Hard (res 8, 4 timers)",
            Variant.Combat, "combat", Resistance: 8, TimerDraw: 4,
            Timers:
            [
                new("Threat", TimerEffect.Spirits, 2, 4, "Stop Threat"),
                new("Pressure", TimerEffect.Spirits, 1, 3, "Relieve Pressure"),
                new("Recovery", TimerEffect.Resistance, 2, 5, "Prevent Recovery"),
                new("Menace", TimerEffect.Spirits, 1, 3, "Face the Menace"),
                new("Dread", TimerEffect.Spirits, 1, 4, "Overcome Dread"),
            ],
            Openings: StandardFiller,
            Approaches: ScaleApproaches(4)),
    ];

    // ── Factories ─────────────────────────────────────────────────

    public static GameSession BuildSession(PlayerProfile profile, int seed, BalanceData? balance = null)
    {
        balance ??= BalanceData.Default;
        var player = PlayerState.NewGame("sim", seed, balance);
        player.Skills[profile.GoverningSkill] = profile.SkillLevel;

        if (profile.WeaponId != null)
            player.Equipment.Weapon = new ItemInstance(profile.WeaponId, balance.Items[profile.WeaponId].Name);
        if (profile.ArmorId != null)
            player.Equipment.Armor = new ItemInstance(profile.ArmorId, balance.Items[profile.ArmorId].Name);

        var map = new Dreamlands.Map.Map(1, 1);
        map.AllNodes().First().Terrain = Terrain.Plains;

        var bundle = EncounterBundle.FromJson("""{"index":{"byId":{},"byCategory":{}},"encounters":[]}""");
        return new GameSession(player, map, bundle, balance, new Random(seed));
    }

    static T ParseSnakeCase<T>(string value) where T : struct, Enum =>
        Enum.Parse<T>(value.Replace("_", ""), ignoreCase: true);

    public static List<OpeningSnapshot> BuildCustomDeck(string[] archetypeIds, BalanceData balance, Random rng)
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
        // Fisher-Yates shuffle
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
        Variant = scenario.Variant,
        Stat = scenario.Stat,
        Resistance = scenario.Resistance,
        TimerDraw = scenario.TimerDraw,
        Timers = scenario.Timers,
        Openings = scenario.Openings,
        Path = scenario.Path ?? [],
        Approaches = scenario.Approaches ?? [],
    };
}
