namespace Dreamlands.Encounter;

/// <summary>One player choice: option text and either a branched (skill-check) or single outcome.</summary>
public sealed class Choice
{
    /// <summary>Full option line (level-1 text). Use this when link/preview are not needed.</summary>
    public string OptionText { get; init; } = "";

    /// <summary>When the option line contains '=', the terse part before '=' (bold/clickable link). Null if no '='.</summary>
    public string? OptionLink { get; init; }

    /// <summary>When the option line contains '=', the verbose part after '=' (secondary preview). Null if no '='.</summary>
    public string? OptionPreview { get; init; }

    /// <summary>Non-null for skill-check / binary choices.</summary>
    public BranchedOutcome? Branched { get; init; }

    /// <summary>Non-null when there is no [if]/[else] (single outcome).</summary>
    public SingleOutcome? Single { get; init; }
}

/// <summary>Success and failure branches (from [if action] / [else]).</summary>
public sealed class BranchedOutcome
{
    /// <summary>Action from [if ...], e.g. "skill_check persuade 15".</summary>
    public string ConditionAction { get; init; } = "";

    public OutcomePart Success { get; init; } = new();
    public OutcomePart Failure { get; init; } = new();
}

/// <summary>One outcome path: prose text and optional mechanics.</summary>
public sealed class SingleOutcome
{
    public OutcomePart Part { get; init; } = new();
}

/// <summary>Outcome text and list of mechanic action strings.</summary>
public sealed class OutcomePart
{
    public string Text { get; init; } = "";
    public IReadOnlyList<string> Mechanics { get; init; } = Array.Empty<string>();
}
