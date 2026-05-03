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

        // Weapon contribution. T-0 (no weapon equipped) = no Attack family at all.
        if (weapon != null)
            foreach (var encoded in weapon.RpsMoves)
                pool.Add(Move.Parse(encoded));

        // Armor contribution. T-0 (no armor equipped) = basic Defend fallback.
        if (armor != null)
            foreach (var encoded in armor.RpsMoves)
                pool.Add(Move.Parse(encoded));
        else
            pool.Add(Move.Parse("defend"));

        // Recover and Read are universal player options. Mutator-bearing variants
        // (e.g. Wary Read, Big Wary Recover) come from gear.
        pool.Add(Move.Parse("recover"));
        pool.Add(Move.Parse("read"));

        return new CombatPlayerProfile
        {
            Weapon = weapon?.WeaponClass,
            Armor = armor?.ArmorClass,
            MovePool = pool,
        };
    }
}
