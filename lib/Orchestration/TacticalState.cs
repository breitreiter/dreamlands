using Dreamlands.Tactical;

namespace Dreamlands.Orchestration;

/// <summary>
/// Mutable runtime state for an active tactical encounter.
/// Serializable for persistence between requests.
/// </summary>
public class TacticalState
{
    public string EncounterId { get; set; } = "";
    public int Momentum { get; set; }
    public int Turn { get; set; }

    /// <summary>The chosen approach for this encounter.</summary>
    public ApproachKind? Approach { get; set; }

    /// <summary>Index of the current active timer. Timers are sequential.</summary>
    public int CurrentTimerIndex { get; set; }

    /// <summary>Whether press or force has been used this turn.</summary>
    public bool DigUsedThisTurn { get; set; }

    /// <summary>Active timers in sequence order. Only CurrentTimerIndex is active.</summary>
    public List<ActiveTimer> Timers { get; set; } = [];

    /// <summary>The opening(s) presented this turn.</summary>
    public List<OpeningSnapshot> Openings { get; set; } = [];

    /// <summary>Traverse only: the visible queue of upcoming path entries.</summary>
    public List<OpeningSnapshot>? Queue { get; set; }

    /// <summary>The shuffled 15-card deck. Draw from DrawIndex.</summary>
    public List<OpeningSnapshot> Deck { get; set; } = [];

    /// <summary>Current draw position in the deck. Reset to 0 on reshuffle.</summary>
    public int DrawIndex { get; set; }

    /// <summary>Traverse only: current position along the authored path.</summary>
    public int PathIndex { get; set; }

    /// <summary>Condition IDs accumulated from condition timers. Resolved on encounter completion.</summary>
    public List<string> PendingConditions { get; set; } = [];

    /// <summary>Timers that fired on the most recent turn advance. Used by TurnData for the UI.</summary>
    public List<TimerFired> LastTimersFired { get; set; } = [];
}

public class ActiveTimer
{
    public string Name { get; set; } = "";
    public string? CounterName { get; set; }
    public TimerEffect Effect { get; set; }
    public int Amount { get; set; }
    public int Countdown { get; set; }
    public int Current { get; set; }
    public int Resistance { get; set; }
    public bool Stopped { get; set; }
    public string? ConditionId { get; set; }
}

/// <summary>Snapshot of an opening for the UI.</summary>
public class OpeningSnapshot
{
    public string Name { get; set; } = "";
    public CostKind CostKind { get; set; }
    public int CostAmount { get; set; }
    public EffectKind EffectKind { get; set; }
    public int EffectAmount { get; set; }
    public int? StopsTimerIndex { get; set; }
}
