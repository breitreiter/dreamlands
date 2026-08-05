// Suggest a covering thread set for an arc: enumerate legal playthroughs, then greedily
// pick the fewest that touch every choice.
//
// The arc is a STATE MACHINE, not a graph. Hub spokes are gated `[requires !tag met_X]`,
// so taking one removes an edge — which choices exist depends on the tags you carry. The
// search space is therefore (encounter, tagset), and any fixed-edge-set algorithm
// (shortest path, edge cover, Chinese postman) answers the wrong question.
//
// Output is a STARTING POINT for a human, not a finished _threads.json. The algorithm
// optimises coverage; it cannot judge whether a walk reads as something a player would
// plausibly do, which is the other half of a good thread.
namespace Forge;

using Dreamlands.Encounter;
using Dreamlands.Game;
using Dreamlands.Rules;

public static class ThreadSuggestCommand
{
    private sealed record Walk(List<string> Path, List<string> Covers, bool Terminal);

    public static int Run(string[] args)
    {
        var maxDepth = 40;
        var maxWalks = 200_000;
        var positional = new List<string>();
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] == "--max-depth" && i + 1 < args.Length) maxDepth = int.Parse(args[++i]);
            else if (args[i] == "--max-walks" && i + 1 < args.Length) maxWalks = int.Parse(args[++i]);
            else if (!args[i].StartsWith("--")) positional.Add(args[i]);
        }
        if (positional.Count == 0)
        {
            Console.Error.WriteLine("usage: forge suggest-threads <arc-dir> [--max-depth N] [--max-walks N]");
            return 2;
        }
        var arcDir = positional[0];

        var encs = LoadEncounters(arcDir);
        if (encs.Count == 0) { Console.Error.WriteLine($"no .enc in '{arcDir}'"); return 1; }

        var allChoices = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (name, enc) in encs)
            foreach (var c in enc.Choices)
                allChoices.Add(ChoiceId(name, Label(c)));

        var walks = new List<Walk>();
        var truncated = !Enumerate(arcDir, encs, [], maxDepth, maxWalks, walks);

        var terminal = walks.Where(w => w.Terminal).ToList();
        Console.WriteLine($"suggest — {new DirectoryInfo(arcDir.TrimEnd('/', '\\')).Name}");
        Console.WriteLine($"  {allChoices.Count} choice(s); {walks.Count} legal walk(s) enumerated"
                          + $", {terminal.Count} reaching a terminal"
                          + (truncated ? "  [TRUNCATED — raise --max-walks]" : ""));

        // Greedy set cover. Prefer the walk covering the most uncovered choices; break ties
        // toward the LONGER walk, since depth of story-so-far is the quality currency and
        // repeated beats are free (weave reuses cells).
        var pool = terminal.Count > 0 ? terminal : walks;
        var uncovered = new HashSet<string>(allChoices, StringComparer.Ordinal);
        var chosen = new List<Walk>();
        while (uncovered.Count > 0)
        {
            Walk? best = null;
            var bestGain = 0;
            foreach (var w in pool)
            {
                var gain = w.Covers.Count(uncovered.Contains);
                if (gain > bestGain || (gain == bestGain && gain > 0 && w.Path.Count > best!.Path.Count))
                {
                    best = w;
                    bestGain = gain;
                }
            }
            if (best is null) break; // nothing left can cover more
            chosen.Add(best);
            foreach (var c in best.Covers) uncovered.Remove(c);
        }

        Console.WriteLine($"\n  {chosen.Count} thread(s) cover {allChoices.Count - uncovered.Count}/{allChoices.Count} choices\n");
        for (var i = 0; i < chosen.Count; i++)
        {
            Console.WriteLine($"  thread {i + 1}  ({chosen[i].Path.Count} steps)");
            foreach (var label in chosen[i].Path) Console.WriteLine($"      {label}");
            Console.WriteLine();
        }

        if (uncovered.Count > 0)
        {
            Console.WriteLine("  UNREACHABLE by any legal walk:");
            foreach (var c in uncovered.OrderBy(x => x, StringComparer.Ordinal))
                Console.WriteLine($"    {c}");
            Console.WriteLine("\n  (a choice is unreachable when no legal path exists, or when its encounter is\n"
                              + "   only entered via a branch the walker cannot select — Thread.Walk takes the\n"
                              + "   first branch's navigation.)");
        }

        Console.WriteLine("\n  Paste into _threads.json, then NAME each thread and write its `note`.");
        Console.WriteLine("  Reorder or merge freely — coverage is mechanical, plausibility is not.");
        return uncovered.Count == 0 ? 0 : 1;
    }

    /// DFS over (encounter, tagset). Returns false if the walk cap was hit.
    private static bool Enumerate(string arcDir, Dictionary<string, Encounter> encs,
        List<string> prefix, int maxDepth, int maxWalks, List<Walk> outWalks)
    {
        var replay = Replay(arcDir, encs, prefix);
        if (replay is null) return true;                       // illegal prefix; prune

        if (replay.Terminal) { outWalks.Add(new Walk([.. prefix], replay.Covers, true)); return true; }
        if (outWalks.Count >= maxWalks) return false;
        if (prefix.Count >= maxDepth) { outWalks.Add(new Walk([.. prefix], replay.Covers, false)); return true; }

        // Loop guard: the same (encounter, tags) twice on one path means the steps between
        // changed nothing, so continuing can only repeat itself.
        if (replay.Signatures.Count != replay.Signatures.Distinct(StringComparer.Ordinal).Count())
        {
            outWalks.Add(new Walk([.. prefix], replay.Covers, false));
            return true;
        }

        if (!encs.TryGetValue(replay.Current, out var enc)) return true;
        var ok = true;
        foreach (var c in enc.Choices)
        {
            prefix.Add(Label(c));
            ok &= Enumerate(arcDir, encs, prefix, maxDepth, maxWalks, outWalks);
            prefix.RemoveAt(prefix.Count - 1);
            if (!ok) break;
        }
        return ok;
    }

    private sealed record ReplayResult(string Current, List<string> Covers, List<string> Signatures, bool Terminal);

    /// Re-execute a prefix from a fresh state. Replaying beats cloning PlayerState: it is
    /// correct by construction and these arcs are small enough that the cost is irrelevant.
    private static ReplayResult? Replay(string arcDir, Dictionary<string, Encounter> encs, List<string> prefix)
    {
        var balance = BalanceData.Default;
        var state = PlayerState.NewGame("suggest", 0, balance);
        var rng = new Random(42);
        var cur = "Start";
        var covers = new List<string>();
        var sigs = new List<string> { Signature(cur, state) };

        foreach (var label in prefix)
        {
            if (!encs.TryGetValue(cur, out var enc)) return null;
            var choice = enc.Choices.FirstOrDefault(c => Label(c) == label);
            if (choice is null) return null;
            if (!string.IsNullOrEmpty(choice.Requires)
                && !Conditions.Evaluate(choice.Requires, state, balance, rng)) return null;

            covers.Add(ChoiceId(cur, label));

            var results = Mechanics.Apply(AllMechanics(choice), state, balance, rng);
            string? nav = null;
            foreach (var r in results)
                switch (r)
                {
                    case MechanicResult.Navigation n: nav ??= n.EncounterId; break;
                    case MechanicResult.DungeonFinished or MechanicResult.DungeonFled:
                        return new ReplayResult(cur, covers, sigs, true);
                }
            if (nav is null) return null;
            cur = nav;
            sigs.Add(Signature(cur, state));
        }
        return new ReplayResult(cur, covers, sigs, false);
    }

    private static string Signature(string enc, PlayerState state)
        => enc + "|" + string.Join(",", state.Tags.OrderBy(t => t, StringComparer.Ordinal));

    private static string Label(Choice c) => c.OptionLink ?? c.OptionText;

    private static string ChoiceId(string enc, string label) => $"{enc} :: {label}";

    private static Dictionary<string, Encounter> LoadEncounters(string arcDir)
    {
        var encs = new Dictionary<string, Encounter>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in Directory.GetFiles(arcDir, "*.enc"))
        {
            var pr = EncounterParser.Parse(File.ReadAllText(file));
            if (pr.Encounter is not null) encs[Path.GetFileNameWithoutExtension(file)] = pr.Encounter;
        }
        return encs;
    }

    private static IReadOnlyList<string> AllMechanics(Choice c)
    {
        if (c.Single is not null) return c.Single.Part.Mechanics;
        if (c.Conditional is null) return [];
        var all = new List<string>();
        foreach (var br in c.Conditional.Branches) all.AddRange(br.Outcome.Mechanics);
        if (c.Conditional.Fallback is not null) all.AddRange(c.Conditional.Fallback.Mechanics);
        all.AddRange(c.Conditional.Mechanics);
        return all;
    }
}
