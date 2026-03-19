namespace Dreamlands.Encounter;

/// <summary>Root model for a parsed encounter.</summary>
public sealed class Encounter
{
    public string Id { get; init; } = "";
    public string ShortId => Id.Contains('/') ? Id[(Id.LastIndexOf('/') + 1)..] : Id;
    public string Category { get; init; } = "";
    public string Title { get; init; } = "";
    public string Body { get; init; } = "";
    public string? Trigger { get; init; }
    public int? Tier { get; init; }
    public string? Vignette { get; init; }
    public IReadOnlyList<string> Requires { get; init; } = Array.Empty<string>();
    public IReadOnlyList<Choice> Choices { get; init; } = Array.Empty<Choice>();
}
