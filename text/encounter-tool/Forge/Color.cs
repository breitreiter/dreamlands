// The color stage's plugin boundary — the one real extension seam in Forge.
//
// Color enriches each beat into a vivid (but factually-unreliable) image bank that
// synthesis mines under its logic filter. Two backends sit behind this interface:
//   - ImpLoomColorProvider  — what we run: the gemma+LoRA logit-steering triplet on
//     imp, treated as an opaque ssh/scp service (ship beats, get enriched beats).
//   - GlmHighTempColorProvider — a documented FOSS-fallback stub (never run here).
namespace Forge;

using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

public interface IColorProvider
{
    /// Enrich each beat job into a color record. May be SLOW (the imp gemma triplet
    /// is an overnight run for a full arc — it owns the GPU box).
    Task<IReadOnlyList<ColorRecord>> EnrichAsync(string arc, IReadOnlyList<BeatJob> jobs);
}

/// One unit of work shipped to a color provider. id = "<source>:<line>".
public sealed record BeatJob(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("tone")] string? Tone,
    [property: JsonPropertyName("text")] string Text);

/// One enriched beat back from a provider. Extra fields a provider may emit (tone,
/// source) are ignored — only these four merge into stages.color.
public sealed record ColorRecord(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("enriched")] string Enriched,
    [property: JsonPropertyName("facts")] List<string> Facts,
    [property: JsonPropertyName("register")] string Register,
    [property: JsonPropertyName("length")] string Length);

internal static class ColorJson
{
    public static readonly JsonSerializerOptions Opts = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };
}
