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

    /// <summary>Master clock. Decrements each turn. Hit zero = lose.</summary>
    public int Clock { get; set; }

    /// <summary>Index of the current active challenge.</summary>
    public int CurrentChallengeIndex { get; set; }

    /// <summary>Whether press or force has been used this turn.</summary>
    public bool DigUsedThisTurn { get; set; }

    /// <summary>All challenges in authored order.</summary>
    public List<ActiveChallenge> Challenges { get; set; } = [];

    /// <summary>The opening(s) presented this turn.</summary>
    public List<OpeningSnapshot> Openings { get; set; } = [];

    /// <summary>The shuffled 15-card deck. Draw from DrawIndex.</summary>
    public List<OpeningSnapshot> Deck { get; set; } = [];

    /// <summary>Current draw position in the deck. Reset to 0 on reshuffle.</summary>
    public int DrawIndex { get; set; }
}

public class ActiveChallenge
{
    public string Name { get; set; } = "";
    public string? CounterName { get; set; }
    public int Resistance { get; set; }
    public int MaxResistance { get; set; }
    public bool Cleared { get; set; }
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
