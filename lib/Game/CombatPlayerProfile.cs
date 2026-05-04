using Dreamlands.Encounter;
using Dreamlands.Rules;

namespace Dreamlands.Game;

/// <summary>
/// One entry in the player's combat move pool: the mechanical <see cref="Move"/>
/// plus the player-facing <see cref="DisplayName"/> rendered on the action button.
/// Display names are authored per-move on each item (see <see cref="RpsMove"/>);
/// universal moves (Recover, Read, fallback Defend) display as the bare verb.
/// </summary>
public sealed record EquippedMove(Move Move, string DisplayName)
{
    public string Encoded => Move.Encoded;
}

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
    /// Cooldowns (Power/Slow) are tracked separately in
    /// <see cref="CombatState.PlayerLastUsedTurn"/>.</summary>
    public List<EquippedMove> MovePool { get; set; } = new();

    public static CombatPlayerProfile From(ItemDef? weapon, ItemDef? armor)
    {
        var pool = new List<EquippedMove>();
        var superseded = new HashSet<string>();

        void Contribute(ItemDef? item)
        {
            if (item == null) return;
            foreach (var entry in item.RpsMoves)
            {
                var move = Move.Parse(entry.Encoding);
                pool.Add(new EquippedMove(move, entry.DisplayName));
                if (move.SupersedesBase) superseded.Add(Move.Parse(move.Base).Encoded);
            }
        }

        // GOTCHA — gear contributes EXACTLY what its RpsMoves list says, with
        // no implicit basic Attack or basic Defend filled in. The two slots
        // are also asymmetric, which trips up gear authors:
        //
        //   Weapon: no fallback at all. No weapon equipped → zero Attack-family
        //     moves in the pool (T-0 = "disable Attack", by design). An equipped
        //     weapon whose RpsMoves omits plain "Attack" means the player can
        //     never throw a basic swing — only the declared variants. T-4
        //     weapons (The Old Tooth, Revathi Labrys) exploit this on purpose.
        //
        //   Armor: the Defend fallback below ONLY fires when armor is null.
        //     The instant any armor is equipped the fallback is suppressed and
        //     the pool gets only what that armor declares. Equipping armor whose
        //     RpsMoves contains no Defend-base variant leaves the player with
        //     ZERO Defend in the pool — strictly worse than fighting unarmored.
        //     Every armor itemdef must declare at least one Defend-base move
        //     (plain or mutated). Power/Slow-gated Defends count, but mean the
        //     player has no Defend after the cooldown burns.
        Contribute(weapon);

        if (armor != null)
            Contribute(armor);
        else
            pool.Add(new EquippedMove(Move.Parse("defend"), "Defend"));

        // Recover and Read are universal player options. Mutator-bearing variants
        // (e.g. Wary Read, Heavy Wary Recover) come from gear.
        pool.Add(new EquippedMove(Move.Parse("recover"), "Recover"));
        pool.Add(new EquippedMove(Move.Parse("read"), "Read Intent"));

        // Drop any move an equipped item flagged as redundant. Compared on
        // canonical Encoded form so mutator order doesn't matter.
        if (superseded.Count > 0)
            pool.RemoveAll(em => superseded.Contains(em.Encoded));

        return new CombatPlayerProfile
        {
            Weapon = weapon?.WeaponClass,
            Armor = armor?.ArmorClass,
            MovePool = pool,
        };
    }
}
