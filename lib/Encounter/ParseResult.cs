namespace Dreamlands.Encounter;

/// <summary>Result of parsing an encounter file: either a valid encounter or a list of errors.</summary>
public sealed class ParseResult
{
    public Encounter? Encounter { get; init; }
    public IReadOnlyList<ParseError> Errors { get; init; } = Array.Empty<ParseError>();

    public bool IsSuccess => Errors.Count == 0 && Encounter is not null;
}

/// <summary>Single parse error with optional line number (1-based).</summary>
public sealed class ParseError
{
    public int? Line { get; init; }
    public string Message { get; init; } = "";

    public override string ToString() => Line.HasValue ? $"Line {Line.Value}: {Message}" : Message;
}
