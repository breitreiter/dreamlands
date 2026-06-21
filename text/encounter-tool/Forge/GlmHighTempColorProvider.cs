// FOSS-friendly fallback color provider — DOCUMENTED, NOT BUILT, and never the one
// we run. Behind the same IColorProvider seam so it drops in via --provider glm-hi-temp.
//
// The idea (for anyone without our imp/loom GPU setup): generate the color bank from
// a single very-high-temperature GLM call per beat instead of the gemma+LoRA
// logit-steering triplet. It is deliberately INFERIOR — it lacks the genuinely alien,
// anti-"Claude-tic" texture loom exists to inject (see RESEARCH.md) — but it gives a
// DIYer a working end-to-end pipeline. We will not switch to it.
//
// To implement: one high-temp GLM call per beat producing the four stages.color
// fields { enriched, facts, register, length } (facts from the beat text, register
// from tone, length from a word-count bucket). See plans/forge_dotnet_port.md.
namespace Forge;

public sealed class GlmHighTempColorProvider : IColorProvider
{
    public Task<IReadOnlyList<ColorRecord>> EnrichAsync(string arc, IReadOnlyList<BeatJob> jobs)
        => throw new NotImplementedException(
            "GlmHighTempColorProvider is a documented FOSS-fallback stub, not implemented — "
            + "we run the imp loom provider. See the class header and plans/forge_dotnet_port.md.");
}
