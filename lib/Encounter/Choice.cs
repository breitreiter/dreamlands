namespace Dreamlands.Encounter;

/// <summary>One player choice: option text and either a conditional (multi-branch) or single outcome.</summary>
public sealed class Choice
{
    /// <summary>Full option line (level-1 text). Use this when link/preview are not needed.</summary>
    public string OptionText { get; init; } = "";

    /// <summary>When the option line contains '=', the terse part before '=' (bold/clickable link). Null if no '='.</summary>
    public string? OptionLink { get; init; }

    /// <summary>When the option line contains '=', the verbose part after '=' (secondary preview). Null if no '='.</summary>
    public string? OptionPreview { get; init; }

    /// <summary>Condition that must be met for this choice to appear (e.g. "has rusted_key"). Null if always visible.</summary>
    public string? Requires { get; init; }

    /// <summary>Non-null for multi-branch conditionals (@if/@elif/@else).</summary>
    public ConditionalOutcome? Conditional { get; init; }

    /// <summary>Non-null when there is no conditional (single outcome).</summary>
    public SingleOutcome? Single { get; init; }
}

/// <summary>One branch in a conditional: a condition string and its outcome.</summary>
public sealed class ConditionalBranch
{
    /// <summary>Condition expression, e.g. "check negotiation correct:reason wrong:threaten" or "has rusted_key".</summary>
    public string Condition { get; init; } = "";

    /// <summary>
    /// For picker-check branches: the correct approach verb (e.g. "reason").
    /// Null for static-condition branches and legacy DC checks.
    /// When non-null, <see cref="IsPickerCheck"/> is true.
    /// </summary>
    public string? PickerSkill { get; init; }

    /// <summary>
    /// For picker-check branches: the correct approach verb (e.g. "reason").
    /// Null for static-condition branches and legacy DC checks.
    /// </summary>
    public string? PickerCorrect { get; init; }

    /// <summary>
    /// For picker-check branches: the wrong approach verb (e.g. "threaten").
    /// Null for static-condition branches and legacy DC checks.
    /// </summary>
    public string? PickerWrong { get; init; }

    /// <summary>True when this branch uses the picker check form (correct:/wrong: attributes).</summary>
    public bool IsPickerCheck => PickerCorrect != null;

    public OutcomePart Outcome { get; init; } = new();
}

/// <summary>Multi-branch conditional outcome from @if/@elif/@else blocks.</summary>
public sealed class ConditionalOutcome
{
    /// <summary>Prose before the @if block (always rendered).</summary>
    public string Preamble { get; init; } = "";

    /// <summary>Ordered branches (@if first, then @elif). Evaluated top-to-bottom.</summary>
    public IReadOnlyList<ConditionalBranch> Branches { get; init; } = [];

    /// <summary>@else branch, null if absent.</summary>
    public OutcomePart? Fallback { get; init; }

    /// <summary>
    /// Choice-level mechanics written outside the @if/@else blocks — before the first
    /// @if, or after the closing brace. They run in addition to whichever branch fires,
    /// so a hub return (`+open "Hub"`) can be written once rather than repeated in
    /// every arm. Empty when the choice puts all its mechanics inside branches.
    /// </summary>
    public IReadOnlyList<string> Mechanics { get; init; } = Array.Empty<string>();
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
