using Dreamlands.Encounter;

namespace Dreamlands.Combat;

/// <summary>
/// One step's worth of player input. Either a three-slot commitment or a Flee.
/// Flee burns the entire turn — the monster's three-slot commitment resolves
/// against an all-Skipped player commit, then if the player survives the encounter
/// ends with PlayerFled (and the encounter goes back into the pool).
/// </summary>
public abstract record PlayerCombatAction
{
    public sealed record Commit(Move Slot1, Move Slot2, Move Slot3) : PlayerCombatAction
    {
        public Move[] AsArray() => new[] { Slot1, Slot2, Slot3 };
    }

    public sealed record Flee : PlayerCombatAction;
}
