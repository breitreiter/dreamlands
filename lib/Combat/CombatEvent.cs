using Dreamlands.Encounter;
using Dreamlands.Game;

namespace Dreamlands.Combat;

/// <summary>
/// Structural events emitted by the resolver. UI / CLI render these into prose; tests
/// assert on their shape. No flavor text in here — narration comes from the encounter
/// (intro/win/lose) and from move <c>narration</c> fields, surfaced by name only.
/// </summary>
public abstract record CombatEvent
{
    public sealed record Intro(string EncounterId, string Title, string Text) : CombatEvent;

    public sealed record SurpriseChecked(int Roll, int Bonus, int Dc, bool PlayerActsFirst) : CombatEvent;

    public sealed record RoundStarted(int Round, bool PlayerActsFirst) : CombatEvent;

    public sealed record IntentPreviewed(string MoveId, IntentClass IntentClass, string IntentText) : CombatEvent;

    public sealed record StanceChanged(SwordStance From, SwordStance To) : CombatEvent;

    public sealed record PlayerAttacked(
        Resolver.AttackOutcome Attack,
        Resolver.DamageResult? Damage,
        int MonsterHpAfter,
        int MonsterMaxHp) : CombatEvent;

    public sealed record PlayerFleeAttempted(Resolver.SaveOutcome Save) : CombatEvent;

    public sealed record MonsterMoved(string MoveId, IntentClass IntentClass, string Narration) : CombatEvent;

    public sealed record MonsterAttacked(
        string MoveId,
        Resolver.AttackOutcome Attack,
        Resolver.DamageResult? Damage,
        DamageBreakdown? Absorbed,
        int PlayerSpiritsAfter,
        int PlayerHealthAfter) : CombatEvent;

    public sealed record MonsterPierced(
        string MoveId,
        Resolver.SaveOutcome Save,
        Resolver.DamageResult? Damage,
        DamageBreakdown? Absorbed,
        int PlayerSpiritsAfter,
        int PlayerHealthAfter) : CombatEvent;

    public sealed record MonsterConditioned(
        string MoveId,
        string ConditionId,
        double Chance,
        bool Procced,
        Resolver.SaveOutcome? Save,
        bool Applied) : CombatEvent;

    public sealed record MonsterDefended(string MoveId, int AcBonus, int MonsterEffectiveAc) : CombatEvent;

    public sealed record MonsterFledEvt(string MoveId) : CombatEvent;

    public sealed record Outcome(
        bool PlayerWon,
        bool PlayerLost,
        bool PlayerFled,
        bool MonsterFled,
        int Rounds,
        string Text,
        IReadOnlyList<string> Mechanics) : CombatEvent;
}
