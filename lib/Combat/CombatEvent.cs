using Dreamlands.Encounter;
using Dreamlands.Game;

namespace Dreamlands.Combat;

/// <summary>
/// Structural events emitted by the resolver. UI / CLI render these into prose; tests
/// assert on their shape. No flavor text in here — narration comes from the encounter
/// (intro/win/lose) and from the monster move's narration variants, surfaced by the
/// runner via <see cref="SlotResolved.MonsterNarration"/>.
/// </summary>
public abstract record CombatEvent
{
    public sealed record Intro(string EncounterId, string Title, string Text) : CombatEvent;

    /// <summary>
    /// Emitted at the start of every turn (including turn 1, after Intro). Carries the
    /// AI's tell and — only when the player committed Read on the previous turn — the
    /// AI's full plan (its three-slot commitment for this turn, in order).
    /// </summary>
    public sealed record TurnStarted(int Turn, string Tell, IReadOnlyList<Move>? Plan) : CombatEvent;

    /// <summary>
    /// One per slot, slot 1..3. Carries both sides' chosen moves, the resulting
    /// HP deltas, and any forward-rider effects that fire from this resolution.
    /// </summary>
    public sealed record SlotResolved(
        int Slot,
        Move PlayerMove,
        Move MonsterMove,
        string MonsterNarration,
        int PlayerDelta,
        int MonsterDelta,
        DamageBreakdown? PlayerDamageAbsorbed,
        int PlayerSpiritsAfter,
        int PlayerHealthAfter,
        int MonsterHpAfter,
        bool StunPlayerNext,
        bool StunMonsterNext,
        IReadOnlyList<string> ConditionsAppliedToPlayer) : CombatEvent;

    public sealed record PlayerFleeAttempted(bool Success) : CombatEvent;

    public sealed record Outcome(
        bool PlayerWon,
        bool PlayerLost,
        bool PlayerFled,
        bool MonsterFled,
        int Turns,
        string Text,
        IReadOnlyList<string> Mechanics) : CombatEvent;
}

/// <summary>Result of applying damage to the player's Spirits-then-Health pool.</summary>
public sealed record DamageBreakdown(int Total, int OnSpirits, int OnHealth, int SpiritsBefore, int HealthBefore);
