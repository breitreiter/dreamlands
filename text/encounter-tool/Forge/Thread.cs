// Walk a single navigable path through an arc's enc graph. The .NET answer to
// forge/thread.py — but instead of re-implementing a second enc parser/engine (the
// Python re-impl's parsing problems were what halted forge), it drives the REAL
// engine: EncounterParser for structure, Conditions/Choices for gating, Mechanics
// for navigation and tags. The peer JSON supplies beat identity (source, line); the
// dreamlands engine never reads the JSON.
//
// Label conventions bridged here: a thread plan names choices by their pre-'='
// option text (engine OptionLink); the peer JSON keys a beat's enclosing choice by
// the post-'=' preview (engine OptionPreview). Both fall back to OptionText when
// there is no '='.
namespace Forge;

using Dreamlands.Encounter;
using Dreamlands.Game;
using Dreamlands.Rules;

public static class Thread
{
    public sealed record BeatRef(string Source, int Line);

    /// Replay `plan` (ordered choice labels) from `start`. Returns the beats read in
    /// reading order (each encounter's preamble once, on first entry) and the final
    /// tag set. An illegal plan (missing choice, unmet gate, dead end) throws.
    public static (List<BeatRef> Order, HashSet<string> Tags) Walk(
        string arcDir, IReadOnlyList<string> plan, string start = "Start")
    {
        var encs = new Dictionary<string, (Encounter Enc, string Source)>(StringComparer.OrdinalIgnoreCase);
        var peerBeats = new Dictionary<string, List<PeerBeat>>(StringComparer.Ordinal);
        foreach (var file in Directory.GetFiles(arcDir, "*.enc"))
        {
            var pr = EncounterParser.Parse(File.ReadAllText(file));
            if (pr.Encounter is null) continue;
            var source = Path.GetFileName(file);
            encs[Path.GetFileNameWithoutExtension(file)] = (pr.Encounter, source);
            var pp = file + ".json";
            if (File.Exists(pp)) peerBeats[source] = Peer.Load(pp).Beats;
        }

        var balance = BalanceData.Default;
        var state = PlayerState.NewGame("thread", 0, balance);
        var rng = new Random(42);
        var order = new List<BeatRef>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var cur = start;

        foreach (var label in plan)
        {
            if (!encs.TryGetValue(cur, out var e))
                throw new ThreadPlanException($"thread: no encounter '{cur}'");
            var (enc, source) = e;
            var beats = peerBeats.GetValueOrDefault(source) ?? [];

            // Preamble beats (choice == null) once, on first entry.
            if (seen.Add(cur))
                foreach (var b in beats.Where(b => b.Choice is null).OrderBy(b => b.Line))
                    order.Add(new BeatRef(source, b.Line));

            var choice = enc.Choices.FirstOrDefault(c => (c.OptionLink ?? c.OptionText) == label);
            if (choice is null)
                throw new ThreadPlanException(
                    $"thread: no choice '{label}' in '{cur}' — have: "
                    + string.Join(" | ", enc.Choices.Select(c => c.OptionLink ?? c.OptionText)));
            if (!string.IsNullOrEmpty(choice.Requires) && !Conditions.Evaluate(choice.Requires, state, balance, rng))
                throw new ThreadPlanException(
                    $"thread: gate fails at '{cur}' / '{label}': requires [{choice.Requires}], "
                    + $"tags [{string.Join(", ", state.Tags.Order())}]");

            // This choice's beats, by the post-'=' preview the peer JSON keys on.
            var beatLabel = choice.OptionPreview ?? choice.OptionText;
            foreach (var b in beats.Where(b => b.Choice == beatLabel).OrderBy(b => b.Line))
                order.Add(new BeatRef(source, b.Line));

            // Navigation + tags via the real engine. Apply every outcome's mechanics
            // (re-entrant hubs set tags in a branch other than the one taken).
            var results = Mechanics.Apply(AllMechanics(choice), state, balance, rng);
            string? nav = null;
            var terminal = false;
            foreach (var r in results)
                switch (r)
                {
                    case MechanicResult.Navigation n: nav ??= n.EncounterId; break;
                    case MechanicResult.DungeonFinished or MechanicResult.DungeonFled: terminal = true; break;
                }

            if (terminal) break;
            if (nav is null)
                throw new ThreadPlanException($"thread: choice '{label}' in '{cur}' neither navigates nor terminates");
            cur = nav;
        }

        return (order, state.Tags);
    }

    private static IReadOnlyList<string> AllMechanics(Choice c)
    {
        if (c.Single is not null) return c.Single.Part.Mechanics;
        if (c.Conditional is null) return [];
        var all = new List<string>();
        foreach (var br in c.Conditional.Branches) all.AddRange(br.Outcome.Mechanics);
        if (c.Conditional.Fallback is not null) all.AddRange(c.Conditional.Fallback.Mechanics);
        all.AddRange(c.Conditional.Mechanics); // choice-level, outside the branches
        return all;
    }
}
