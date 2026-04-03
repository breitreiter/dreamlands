namespace Dreamlands.Tactical;

// ── Enums ──────────────────────────────────────────────────────────

public enum CostKind { Free, Momentum, Spirits, Tick }

public enum EffectKind { Damage, StopTimer, Momentum }

public enum TimerEffect { Spirits, Resistance, Condition, TickTimer, Fatal }

public enum ApproachKind { Aggressive, Cautious }

// ── Value types ────────────────────────────────────────────────────

/// <summary>
/// Timer definition. Two kinds determined by Resistance:
///   Resistance > 0 → sequential (has HP, one active at a time, player depletes to advance)
///   Resistance = 0 → ambient (always ticking, auto-cleared when all sequential timers are done)
/// </summary>
public sealed record TimerDef(
    string Name,
    TimerEffect Effect,
    int Amount,
    int Countdown,
    int Resistance,
    string? CounterName = null,
    string? ConditionId = null,
    string? TicksTimerName = null);

public sealed record OpeningDef(string Name, string Archetype, string? Requires = null);

/// <summary>
/// Approach definition. Aggressive = +2 momentum/turn, draw 1. Cautious = +1 momentum/turn, draw 2.
/// </summary>
public sealed record ApproachDef(ApproachKind Kind);

public sealed record FailureOutcome(string Text, IReadOnlyList<string> Mechanics);

public sealed record SuccessOutcome(string Text, IReadOnlyList<string> Mechanics);

public sealed record BranchDef(string Label, string EncounterRef, string? Requires = null);

// ── Root types ─────────────────────────────────────────────────────

public sealed record TacticalEncounter
{
    public string Id { get; init; } = "";
    public string Category { get; init; } = "";
    public string Title { get; init; } = "";
    public string Body { get; init; } = "";
    public string? Stat { get; init; }
    public int? Tier { get; init; }
    public IReadOnlyList<string> Requires { get; init; } = [];

    public IReadOnlyList<TimerDef> Timers { get; init; } = [];
    public IReadOnlyList<OpeningDef> Openings { get; init; } = [];
    public IReadOnlyList<ApproachDef> Approaches { get; init; } = [];
    public FailureOutcome? Failure { get; init; }
    public SuccessOutcome? Success { get; init; }
}

public sealed record TacticalGroup
{
    public string Id { get; init; } = "";
    public string Category { get; init; } = "";
    public string Title { get; init; } = "";
    public string Body { get; init; } = "";
    public int? Tier { get; init; }
    public IReadOnlyList<string> Requires { get; init; } = [];
    public IReadOnlyList<BranchDef> Branches { get; init; } = [];
}

// ── Parse result ───────────────────────────────────────────────────

public sealed record ParseError(int? Line, string Message)
{
    public override string ToString() =>
        Line.HasValue ? $"Line {Line}: {Message}" : Message;
}

public sealed record TacticalParseResult
{
    public TacticalEncounter? Encounter { get; init; }
    public TacticalGroup? Group { get; init; }
    public IReadOnlyList<ParseError> Errors { get; init; } = [];
    public bool IsSuccess => Errors.Count == 0 && (Encounter is not null || Group is not null);
}
