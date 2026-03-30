namespace Dreamlands.Tactical;

// ── Enums ──────────────────────────────────────────────────────────

public enum Variant { Combat, Traverse }

public enum CostKind { Free, Momentum, Spirits, Tick }

public enum EffectKind { Damage, StopTimer, Momentum }

public enum TimerEffect { Spirits, Resistance, Condition }

public enum ApproachKind { Scout, Direct, Wild }

// ── Value types ────────────────────────────────────────────────────

public sealed record TimerDef(string Name, TimerEffect Effect, int Amount, int Countdown, string? CounterName = null, string? ConditionId = null);

public sealed record OpeningDef(string Name, string Archetype, string? Requires = null);

public sealed record ApproachDef(ApproachKind Kind, int Momentum, int TimerCount, int BonusOpenings = 0);

public sealed record FailureOutcome(string Text, IReadOnlyList<string> Mechanics);

public sealed record BranchDef(string Label, string? Intent, string EncounterRef, string? Requires = null);

// ── Root types ─────────────────────────────────────────────────────

public sealed record TacticalEncounter
{
    public string Id { get; init; } = "";
    public string Category { get; init; } = "";
    public string Title { get; init; } = "";
    public string Body { get; init; } = "";
    public Variant Variant { get; init; }
    public string? Intent { get; init; }
    public string? Stat { get; init; }
    public int? Tier { get; init; }
    public IReadOnlyList<string> Requires { get; init; } = [];

    public int Resistance { get; init; }

    public int TimerDraw { get; init; }
    public IReadOnlyList<TimerDef> Timers { get; init; } = [];
    public IReadOnlyList<OpeningDef> Openings { get; init; } = [];
    public IReadOnlyList<OpeningDef> Path { get; init; } = [];
    public IReadOnlyList<ApproachDef> Approaches { get; init; } = [];
    public FailureOutcome? Failure { get; init; }
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
