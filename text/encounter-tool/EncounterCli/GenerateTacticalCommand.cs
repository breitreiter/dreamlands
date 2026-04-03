namespace EncounterCli;

static class GenerateTacticalCommand
{
    // Archetype pools: (id, progress_value)
    static readonly (string Id, int Value)[] MomentumArchetypes =
    [
        ("free_momentum_small", 1),
        ("free_momentum", 2),
        ("threat_to_momentum", 2),
        ("spirits_to_momentum", 3),
    ];

    static readonly (string Id, int Value)[] ProgressArchetypes =
    [
        ("free_progress_small", 1),
        ("momentum_to_progress", 2),
        ("momentum_to_progress_large", 3),
        ("momentum_to_progress_huge", 5),
        ("spirits_to_progress", 3),
        ("spirits_to_progress_large", 5),
        ("threat_to_progress", 2),
        ("threat_to_progress_large", 3),
    ];

    static readonly string[] CancelArchetypes = ["momentum_to_cancel", "spirits_to_cancel"];
    static readonly string[] TimerEffects = ["spirits", "resistance"];

    // Tier tables: keyed by tier (1-3)
    record TierData(
        (int Lo, int Hi) Resistance,
        (int Lo, int Hi) TimerCount,
        (int Lo, int Hi) TimerCountdown,
        (int Lo, int Hi) TimerDamage,
        (int Lo, int Hi) TimerResistance,
        (int Lo, int Hi) OpeningsTraverse,
        (int Lo, int Hi) OpeningsCombat,
        (int Lo, int Hi) PathCards);

    static readonly Dictionary<int, TierData> Tiers = new()
    {
        [1] = new(
            Resistance: (6, 8),
            TimerCount: (2, 3),
            TimerCountdown: (3, 5),
            TimerDamage: (1, 1),
            TimerResistance: (4, 6),
            OpeningsTraverse: (9, 12),
            OpeningsCombat: (10, 14),
            PathCards: (4, 6)),
        [2] = new(
            Resistance: (8, 12),
            TimerCount: (3, 4),
            TimerCountdown: (3, 4),
            TimerDamage: (1, 2),
            TimerResistance: (5, 8),
            OpeningsTraverse: (11, 14),
            OpeningsCombat: (12, 16),
            PathCards: (5, 8)),
        [3] = new(
            Resistance: (12, 16),
            TimerCount: (4, 6),
            TimerCountdown: (2, 4),
            TimerDamage: (1, 2),
            TimerResistance: (6, 10),
            OpeningsTraverse: (13, 16),
            OpeningsCombat: (14, 18),
            PathCards: (6, 10)),
    };

    public static int Run(string[] args)
    {
        string? variant = null;
        int? tier = null;
        string? outPath = null;
        int? seed = null;
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--out" && i + 1 < args.Length) { outPath = args[i + 1]; i++; }
            else if (args[i] == "--seed" && i + 1 < args.Length && int.TryParse(args[i + 1], out var s)) { seed = s; i++; }
            else if (!args[i].StartsWith('-'))
            {
                if (variant == null) variant = args[i].ToLowerInvariant();
                else if (tier == null && int.TryParse(args[i], out var t)) tier = t;
            }
        }

        if (variant == null || tier == null)
        {
            Console.Error.WriteLine("Usage: encounter generate-tactical <variant> <tier> [--out <file>] [--seed <n>]");
            return 1;
        }

        if (variant is not "combat" and not "traverse")
        {
            Console.Error.WriteLine($"Unknown variant: {variant}. Must be 'combat' or 'traverse'.");
            return 1;
        }

        if (!Tiers.ContainsKey(tier.Value))
        {
            Console.Error.WriteLine($"Invalid tier: {tier}. Must be 1, 2, or 3.");
            return 1;
        }

        var output = Generate(variant, tier.Value, seed);

        if (outPath != null)
        {
            outPath = Path.GetFullPath(outPath);
            var dir = Path.GetDirectoryName(outPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(outPath, output);
            Console.WriteLine($"Wrote {outPath}");
        }
        else
        {
            // Default: write to cwd with a placeholder name
            outPath = Path.GetFullPath("FIXME Skeleton.tac");
            File.WriteAllText(outPath, output);
            Console.WriteLine($"Wrote {outPath}");
        }

        return 0;
    }

    // --- Generation ---

    static string Generate(string variant, int tier, int? seed)
    {
        var rng = seed.HasValue ? new Random(seed.Value) : new Random();
        var td = Tiers[tier];

        var (timers, resTimerCount) = GenerateTimers(rng, td);
        var resistance = RandRange(rng, td.Resistance);

        var lines = new List<string>();

        // Header
        lines.Add("FIXME: Title");
        lines.Add($"[variant {variant}]");
        lines.Add($"[stat FIXME]");
        lines.Add($"[tier {tier}]");
        lines.Add("");
        lines.Add("FIXME: body text");
        lines.Add("");

        // Timers
        lines.Add("timers:");
        foreach (var (effect, damage, countdown, timerResist) in timers)
            lines.Add($"  * FIXME [counter FIXME]: {effect} {damage} every {countdown} resist {timerResist}");
        lines.Add("");

        // Openings
        lines.Add("openings:");
        List<string> openings;
        if (variant == "traverse")
        {
            var openingCount = RandRange(rng, td.OpeningsTraverse);
            openings = GenerateTraverseOpenings(rng, tier, openingCount);
        }
        else
        {
            var openingCount = RandRange(rng, td.OpeningsCombat);
            openings = GenerateCombatOpenings(rng, tier, openingCount);
        }
        foreach (var arch in openings)
            lines.Add($"  * FIXME: {arch}");

        // Path (traverse only)
        if (variant == "traverse")
        {
            var cardCount = RandRange(rng, td.PathCards);
            var extraPressure = EstimateResistanceTimerPressure(timers, resistance);
            var target = resistance + extraPressure;
            var path = GeneratePath(rng, tier, target, cardCount);
            var actualSum = path.Sum(ProgressValue);

            lines.Add("");
            lines.Add("path:");
            foreach (var arch in path)
                lines.Add($"  * FIXME: {arch}");
            var comment = $"  # path sum: {actualSum} (resistance: {resistance}";
            if (extraPressure > 0)
                comment += $" + ~{extraPressure} timer pressure";
            comment += ")";
            lines.Add(comment);
        }

        // Approaches (combat only)
        if (variant == "combat")
        {
            lines.Add("");
            lines.Add("approaches:");
            lines.Add("  * aggressive");
            lines.Add("  * cautious");
        }

        // Success (combat only)
        if (variant == "combat")
        {
            lines.Add("");
            lines.Add("success:");
            lines.Add("  FIXME: success text");
        }

        // Failure
        lines.Add("");
        lines.Add("failure:");
        lines.Add("  FIXME: failure text");
        lines.Add("  FIXME: failure outcomes");
        lines.Add("");

        return string.Join("\n", lines);
    }

    // --- Timers ---

    static (List<(string Effect, int Damage, int Countdown, int Resistance)> Timers, int ResTimerCount) GenerateTimers(
        Random rng, TierData td)
    {
        var count = RandRange(rng, td.TimerCount);

        var timers = new List<(string, int, int, int)>();
        var resTimerCount = 0;

        for (int i = 0; i < count; i++)
        {
            // Bias toward spirits; resistance timers are rarer
            var effect = rng.NextDouble() < 0.25 ? "resistance" : "spirits";
            if (effect == "resistance")
                resTimerCount++;
            var damage = RandRange(rng, td.TimerDamage);
            var countdown = RandRange(rng, td.TimerCountdown);
            var timerResist = RandRange(rng, td.TimerResistance);
            timers.Add((effect, damage, countdown, timerResist));
        }

        return (timers, resTimerCount);
    }

    // --- Traverse openings (momentum only) ---

    static List<string> GenerateTraverseOpenings(Random rng, int tier, int count)
    {
        var cards = new List<string>
        {
            "free_momentum",
            "free_momentum",
            "threat_to_momentum",
        };

        // Maybe a spirits_to_momentum haymaker at higher tiers
        if (tier >= 2 || rng.NextDouble() < 0.3)
            cards.Add("spirits_to_momentum");

        // Maybe a second threat_to_momentum at higher tiers
        if (tier >= 2 && rng.NextDouble() < 0.5)
            cards.Add("threat_to_momentum");

        // Fill the rest with free_momentum_small
        while (cards.Count < count)
            cards.Add("free_momentum_small");

        Shuffle(rng, cards);
        return cards;
    }

    // --- Combat openings (mixed) ---

    static List<string> GenerateCombatOpenings(Random rng, int tier, int count)
    {
        var cards = new List<string>();

        // Momentum generators (~35% of deck)
        var momentumCount = Math.Max(4, (int)Math.Round(count * 0.35));
        cards.Add("free_momentum");
        cards.Add("free_momentum_small");
        cards.Add("free_momentum_small");
        cards.Add("threat_to_momentum");
        var momentumExtras = momentumCount - 4;
        for (int i = 0; i < momentumExtras; i++)
        {
            if (rng.NextDouble() < 0.3 && tier >= 2)
                cards.Add("spirits_to_momentum");
            else
                cards.Add("free_momentum_small");
        }

        // Cancel card (optional, more likely at higher tiers)
        if (rng.NextDouble() < 0.2 + tier * 0.15)
            cards.Add("momentum_to_cancel");

        // Progress converters (fill remaining slots)
        var progressCount = count - cards.Count;

        // Caps on expensive cards
        var budget = new Dictionary<string, int>
        {
            ["momentum_to_progress_huge"] = tier >= 2 ? 1 : 0,
            ["spirits_to_progress_large"] = tier >= 3 ? 1 : 0,
            ["momentum_to_progress_large"] = 1,
            ["spirits_to_progress"] = 1,
            ["threat_to_progress_large"] = tier >= 2 ? 1 : 0,
        };
        var placed = budget.Keys.ToDictionary(k => k, _ => 0);

        // Guarantee one haymaker
        var haymakers = budget.Where(kv => kv.Value > 0).Select(kv => kv.Key).ToList();
        var pick = haymakers[rng.Next(haymakers.Count)];
        cards.Add(pick);
        placed[pick]++;
        progressCount--;

        // Fill the rest, weighted toward cheap cards
        for (int i = 0; i < progressCount; i++)
        {
            var roll = rng.NextDouble();
            if (roll < 0.30)
                cards.Add("free_progress_small");
            else if (roll < 0.55)
                cards.Add("momentum_to_progress");
            else if (roll < 0.70)
                cards.Add("threat_to_progress");
            else
            {
                var candidates = budget.Where(kv => placed[kv.Key] < kv.Value).Select(kv => kv.Key).ToList();
                if (candidates.Count > 0)
                {
                    pick = candidates[rng.Next(candidates.Count)];
                    cards.Add(pick);
                    placed[pick]++;
                }
                else
                {
                    cards.Add("momentum_to_progress");
                }
            }
        }

        Shuffle(rng, cards);
        return cards;
    }

    // --- Path generation (traverse only) ---

    static List<string> GeneratePath(Random rng, int tier, int targetProgress, int cardCount)
    {
        var allowSpirits = tier >= 2 || rng.NextDouble() < 0.2;
        var cards = new List<string>();
        var remaining = targetProgress;

        for (int i = 0; i < cardCount; i++)
        {
            if (remaining <= 0)
                break;

            // Fine-tune with small cards when close
            if (remaining <= 2)
            {
                if (remaining == 1)
                {
                    cards.Add("free_progress_small");
                    remaining -= 1;
                }
                else
                {
                    cards.Add("momentum_to_progress");
                    remaining -= 2;
                }
                continue;
            }

            var roll = rng.NextDouble();
            if (roll < 0.15 && allowSpirits && remaining >= 3)
            {
                cards.Add("spirits_to_progress");
                remaining -= 3;
            }
            else if (roll < 0.35 && remaining >= 3)
            {
                cards.Add("momentum_to_progress_large");
                remaining -= 3;
            }
            else if (remaining >= 2)
            {
                cards.Add("momentum_to_progress");
                remaining -= 2;
            }
            else
            {
                cards.Add("free_progress_small");
                remaining -= 1;
            }
        }

        // Cover any remaining progress
        while (remaining > 0)
        {
            if (remaining >= 2)
            {
                cards.Add("momentum_to_progress");
                remaining -= 2;
            }
            else
            {
                cards.Add("free_progress_small");
                remaining -= 1;
            }
        }

        return cards;
    }

    // --- Balance helpers ---

    static int EstimateResistanceTimerPressure(List<(string Effect, int Damage, int Countdown, int Resistance)> timers, int resistance)
    {
        var turns = resistance / 2 + 1;
        var extra = 0;
        foreach (var (effect, damage, countdown, _) in timers)
        {
            if (effect == "resistance")
            {
                var ticks = turns / countdown;
                extra += ticks * damage;
            }
        }
        return extra;
    }

    static int ProgressValue(string archetype) => archetype switch
    {
        "free_progress_small" => 1,
        "momentum_to_progress" => 2,
        "momentum_to_progress_large" => 3,
        "momentum_to_progress_huge" => 5,
        "spirits_to_progress" => 3,
        "spirits_to_progress_large" => 5,
        "threat_to_progress" => 2,
        "threat_to_progress_large" => 3,
        _ => 0,
    };

    // --- Utilities ---

    static int RandRange(Random rng, (int Lo, int Hi) range) => rng.Next(range.Lo, range.Hi + 1);

    static void Shuffle<T>(Random rng, List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
