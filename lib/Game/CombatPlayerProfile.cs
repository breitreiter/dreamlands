using Dreamlands.Encounter;
using Dreamlands.Rules;

namespace Dreamlands.Game;

/// <summary>
/// Snapshot of the player's combat-relevant kit taken at encounter start. The
/// move pool is composed from the equipped weapon and armor (each contributes
/// its <see cref="ItemDef.RpsMoves"/>) plus the player's universal Recover and
/// Read actions. With no weapon equipped the pool has no Attack family at all
/// (T-0 weapon = "disable attack"); with no armor equipped the pool gets a
/// fallback basic Defend (T-0 armor = "basic defense").
///
/// See super_rps.md § Player Weapon Philosophy / Player Armor Philosophy / Item
/// Movesets for the design.
/// </summary>
public sealed class CombatPlayerProfile
{
    public WeaponClass? Weapon { get; set; }
    public ArmorClass? Armor { get; set; }

    /// <summary>The full set of moves the player can choose from for any slot.
    /// Cooldowns (Rare/Mythic/Power/Slow) are tracked separately in
    /// <see cref="CombatState.PlayerLastUsedTurn"/>.</summary>
    public List<Move> MovePool { get; set; } = new();

    public static CombatPlayerProfile From(ItemDef? weapon, ItemDef? armor)
    {
        var pool = new List<Move>();
        var superseded = new HashSet<string>();

        void Contribute(ItemDef? item)
        {
            if (item == null) return;
            foreach (var encoded in item.RpsMoves)
            {
                var move = Move.Parse(encoded);
                pool.Add(move);
                if (move.SupersedesBase) superseded.Add(Move.Parse(move.Base).Encoded);
            }
        }

        // Weapon contribution. T-0 (no weapon equipped) = no Attack family at all.
        Contribute(weapon);

        // Armor contribution. T-0 (no armor equipped) = basic Defend fallback.
        if (armor != null)
            Contribute(armor);
        else
            pool.Add(Move.Parse("defend"));

        // Recover and Read are universal player options. Mutator-bearing variants
        // (e.g. Wary Read, Big Wary Recover) come from gear.
        pool.Add(Move.Parse("recover"));
        pool.Add(Move.Parse("read"));

        // Drop any move an equipped item flagged as redundant. Compared on
        // canonical Encoded form so mutator order doesn't matter.
        if (superseded.Count > 0)
            pool.RemoveAll(m => superseded.Contains(m.Encoded));

        return new CombatPlayerProfile
        {
            Weapon = weapon?.WeaponClass,
            Armor = armor?.ArmorClass,
            MovePool = pool,
        };
    }
}
