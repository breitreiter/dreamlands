// Navigable thread definitions — ordered choice labels through an arc's enc graph.
// Ported from forge/weave.py THREADS. Labels are the pre-'=' option text (what the
// thread author types); the walk matches them against the engine's OptionLink.
namespace Forge;

public static class Threads
{
    public const string Default = "vastand";

    public static readonly IReadOnlyDictionary<string, string[]> All = new Dictionary<string, string[]>
    {
        // The richest gated ending: enter, read the turning point and the coded
        // ledgers (-> economic_value), the girl and the kitchen cabinet (-> danger),
        // then walk out and tell Vastand what it cost.
        ["vastand"] =
        [
            "Slip in through the servants' gate",
            "Skip past the early notes",
            "Review the coded ledgers",
            "Read on into the worse of it",
            "Read about the girl he hired",
            "Open the kitchen cabinet",
            "Set the journal down and decide",
            "Carry the journal out and tell Vastand what it cost",
        ],
        // The cosmic-horror branch: skip to the last pages, read the final entries
        // and the impossible diagrams (-> read_lastwords), descend to the crystal
        // cave (-> saw_cave), then keep the journal out of obsession.
        ["cave"] =
        [
            "Slip in through the servants' gate",
            "Skip to the last pages",
            "Read the last entries he wrote",
            "Skip to the final page",
            "Try to find this cave",
            "Set the journal down and decide",
            "Take the journal and keep it for yourself",
        ],
    };
}
