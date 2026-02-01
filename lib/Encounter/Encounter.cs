namespace Dreamlands.Encounter;

/// <summary>Root model for a parsed encounter.</summary>
public sealed class Encounter
{
    public string Title { get; init; } = "";
    public string Body { get; init; } = "";
    public IReadOnlyList<Choice> Choices { get; init; } = Array.Empty<Choice>();
}
