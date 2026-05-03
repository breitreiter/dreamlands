using Dreamlands.Encounter;
using Dreamlands.Rules;

namespace Dreamlands.Game;

/// <summary>
/// Snapshot of the player's combat-relevant kit taken at encounter start. Composed
/// from the equipped weapon and armor: the weapon and armor each contribute moves
/// to the player's pool, plus the universal Read action.
///
/// Phase 1 stub: weapon class and armor class drive a hard-coded baseline pool.
/// Phase 6 reads RpsMoves from the itemdef.
/// </summary>
public sealed class CombatPlayerProfile
{
    public WeaponClass? Weapon { get; set; }
    public ArmorClass? Armor { get; set; }

    /// <summary>The full set of moves the player can choose from for any slot.
    /// Cooldowns (Rare/Mythic) are tracked in <see cref="CombatState.PlayerLastUsedTurn"/>.</summary>
    public List<Move> MovePool { get; set; } = new();

    public static CombatPlayerProfile From(WeaponClass? weapon, ArmorClass? armor)
    {
        var pool = new List<Move>();

        // Weapon contribution. T-0 (no weapon equipped) disables Attack; everything
        // else surfaces a basic Attack at minimum. Phase 6 will replace this with
        // ItemDef.RpsMoves lookups.
        if (weapon is not null)
            pool.Add(Move.Parse("attack"));

        // Armor contribution. Defend is universally available; specific armors will
        // upgrade or branch via Phase 6 itemdef moves.
        pool.Add(Move.Parse("defend"));

        // Recover and Read are universal player options today. (Recover may end up
        // gear-gated in a later pass; Read is player-only by design.)
        pool.Add(Move.Parse("recover"));
        pool.Add(Move.Parse("read"));

        return new CombatPlayerProfile
        {
            Weapon = weapon,
            Armor = armor,
            MovePool = pool,
        };
    }
}
