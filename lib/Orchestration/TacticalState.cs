using Dreamlands.Tactical;

namespace Dreamlands.Orchestration;

/// <summary>
/// Mutable runtime state for an active tactical encounter.
/// Serializable for persistence between requests.
/// </summary>
public class TacticalState
{
    public string EncounterId { get; set; } = "";
    public int Resistance { get; set; }
    public int Momentum { get; set; }
    public int Turn { get; set; }
    public bool BonusNextTurn { get; set; }

    /// <summary>Active timers drawn from the pool, with current countdown values.</summary>
    public List<ActiveTimer> Timers { get; set; } = [];

    /// <summary>The opening(s) presented this turn. Normally 1; 3 after press/force.</summary>
    public List<OpeningSnapshot> Openings { get; set; } = [];

    /// <summary>Traverse only: the visible queue of upcoming openings.</summary>
    public List<OpeningSnapshot>? Queue { get; set; }

    /// <summary>Full opening pool (filtered by player gear at encounter start).</summary>
    public List<OpeningSnapshot> Pool { get; set; } = [];
}

public class ActiveTimer
{
    public string Name { get; set; } = "";
    public TimerEffect Effect { get; set; }
    public int Amount { get; set; }
    public int Countdown { get; set; }
    public int Current { get; set; }
    public bool Stopped { get; set; }
}

/// <summary>Snapshot of an opening for the UI. Index references Pool position.</summary>
public class OpeningSnapshot
{
    public int PoolIndex { get; set; }
    public string Name { get; set; } = "";
    public CostKind CostKind { get; set; }
    public int CostAmount { get; set; }
    public EffectKind EffectKind { get; set; }
    public int EffectAmount { get; set; }
}
