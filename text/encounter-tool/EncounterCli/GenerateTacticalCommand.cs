namespace EncounterCli;

static class GenerateTacticalCommand
{
    // Tier tables: keyed by tier (1-3)
    record TierData(
        (int Lo, int Hi) Clock,
        (int Lo, int Hi) ChallengeCount,
        (int Lo, int Hi) ChallengeResistance);

    static readonly Dictionary<int, TierData> Tiers = new()
    {
        [1] = new(
            Clock: (8, 10),
            ChallengeCount: (2, 3),
            ChallengeResistance: (3, 5)),
        [2] = new(
            Clock: (9, 12),
            ChallengeCount: (3, 4),
            ChallengeResistance: (4, 7)),
        [3] = new(
            Clock: (10, 14),
            ChallengeCount: (3, 5),
            ChallengeResistance: (5, 9)),
    };

    public static int Run(string[] args)
    {
        int? tier = null;
        string? outPath = null;
        int? seed = null;
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--out" && i + 1 < args.Length) { outPath = args[i + 1]; i++; }
            else if (args[i] == "--seed" && i + 1 < args.Length && int.TryParse(args[i + 1], out var s)) { seed = s; i++; }
            else if (!args[i].StartsWith('-'))
            {
                if (tier == null && int.TryParse(args[i], out var t)) tier = t;
            }
        }

        if (tier == null)
        {
            Console.Error.WriteLine("Usage: encounter generate-tactical <tier> [--out <file>] [--seed <n>]");
            return 1;
        }

        if (!Tiers.ContainsKey(tier.Value))
        {
            Console.Error.WriteLine($"Invalid tier: {tier}. Must be 1, 2, or 3.");
            return 1;
        }

        var output = Generate(tier.Value, seed);

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

    static string Generate(int tier, int? seed)
    {
        var rng = seed.HasValue ? new Random(seed.Value) : new Random();
        var td = Tiers[tier];

        var clock = RandRange(rng, td.Clock);
        var challengeCount = RandRange(rng, td.ChallengeCount);
        var challenges = new List<int>();
        for (int i = 0; i < challengeCount; i++)
            challenges.Add(RandRange(rng, td.ChallengeResistance));

        var lines = new List<string>();

        // Header
        lines.Add("FIXME: Title");
        lines.Add($"[stat FIXME]");
        lines.Add($"[tier {tier}]");
        lines.Add("");
        lines.Add("FIXME: body text");
        lines.Add("");

        // Clock
        lines.Add("clock:");
        lines.Add($"  {clock}");
        lines.Add("");

        // Challenges
        lines.Add("challenges:");
        for (int i = 0; i < challenges.Count; i++)
            lines.Add($"  * FIXME [counter FIXME]: {challenges[i]}");
        lines.Add("");

        // Openings
        lines.Add("openings:");
        var openings = GenerateOpenings(rng, tier);
        foreach (var arch in openings)
            lines.Add($"  * FIXME: {arch}");

        // Approaches
        lines.Add("");
        lines.Add("approaches:");
        lines.Add("  * aggressive");
        lines.Add("  * cautious");

        // Success
        lines.Add("");
        lines.Add("success:");
        lines.Add("  FIXME: success text");

        // Failure
        lines.Add("");
        lines.Add("failure:");
        lines.Add("  FIXME: failure text");
        lines.Add("  FIXME: failure outcomes");
        lines.Add("");

        return string.Join("\n", lines);
    }

    // --- Openings (tier-degraded from canonical base) ---
    //
    // The canonical base deck (from GA optimization) represents "what fun feels like."
    // Higher tiers degrade by removing the best cards from the top, replacing with chaff.
    // This forces players to bring good cards via gear. Cancels are T1 only — bring your own.
    //
    // The base is 15 cards. Player collection fills some slots, filler fills the rest.
    // The generator produces filler openings (typically 10-14 depending on player gear).

    // Canonical filler ranked best-to-worst. Degradation removes from the top.
    static readonly string[] CanonicalFiller =
    [
        // Top tier — removed first at higher tiers
        "spirits_to_cancel",           // insurance cancel (T1 only)
        "spirits_to_progress_large",   // big burst
        "spirits_to_progress_large",
        "threat_to_progress",          // always-playable damage
        "threat_to_progress",
        "spirits_to_momentum",         // spirits-to-momentum ramp
        // Mid tier — removed at T3
        "momentum_to_progress",        // bread and butter
        "threat_to_progress",
        "free_momentum",               // setup
        "free_momentum",
        // Floor — always present
        "free_progress_small",
        "free_progress_small",
        "free_progress_small",
        "free_momentum_small",
    ];

    // How many top cards to remove per tier
    static int DegradeCount(int tier) => tier switch
    {
        1 => 0,   // full canonical — tutorial, everything handed to you
        2 => 3,   // lose the cancel + 2 best damage cards
        3 => 6,   // lose cancel + all burst damage + ramp. Gear or suffer.
        _ => 0,
    };

    const int FillerCount = 14; // deck is 15; assume at least 1 collection card

    static List<string> GenerateOpenings(Random rng, int tier)
    {
        int degrade = DegradeCount(tier);

        // Start from canonical, degrade top N to chaff
        var pool = new List<string>(CanonicalFiller);
        for (int i = 0; i < Math.Min(degrade, pool.Count); i++)
            pool[i] = "free_progress_small";

        // Trim or pad to exactly FillerCount
        while (pool.Count > FillerCount)
            pool.RemoveAt(pool.Count - 1);
        while (pool.Count < FillerCount)
            pool.Add("free_progress_small");

        Shuffle(rng, pool);
        return pool;
    }

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
