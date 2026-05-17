namespace Dreamlands.Encounter;

/// <summary>Result of parsing an encounter file: either a valid encounter or a list of errors.</summary>
public sealed class ParseResult
{
    public Encounter? Encounter { get; init; }
    public IReadOnlyList<ParseError> Errors { get; init; } = Array.Empty<ParseError>();

    /// <summary>True when there are no hard errors (warnings do not affect success).</summary>
    public bool IsSuccess => Errors.All(e => e.IsWarning) && Encounter is not null;

    /// <summary>Warning-level diagnostics only (deprecation notices etc.).</summary>
    public IReadOnlyList<ParseError> Warnings => Errors.Where(e => e.IsWarning).ToList();
}

/// <summary>Single parse error or warning with optional line number (1-based).</summary>
public sealed class ParseError
{
    public int? Line { get; init; }
    public string Message { get; init; } = "";

    /// <summary>When true, this is a deprecation warning rather than a hard error.</summary>
    public bool IsWarning { get; init; }

    public override string ToString() => Line.HasValue
        ? $"{(IsWarning ? "Warning" : "Error")} Line {Line.Value}: {Message}"
        : $"{(IsWarning ? "Warning" : "Error")}: {Message}";
}
