using Dreamlands.Rules;

namespace Dreamlands.Game;

/// <summary>
/// Snapshot of the player's combat-relevant stats taken at encounter start. Combat reads
/// from this rather than re-deriving from equipment each turn, so swapping gear mid-fight
/// (which the UI shouldn't allow anyway) can't perturb running combat.
///
/// Phase 1: simple weapon-class → die / armor-class → AC mapping. Tier scaling and
/// per-weapon dice come in later phases.
/// </summary>
public sealed class CombatPlayerProfile
{
    public int BaseAc { get; set; } = 10;
    public int AttackBonus { get; set; }
    public int DamageBonus { get; set; }

    /// <summary>Number of damage dice (1 for most weapons).</summary>
    public int DamageDieCount { get; set; } = 1;

    /// <summary>Damage die size (e.g. 8 for 1d8). 0 if unarmed.</summary>
    public int DamageDieSize { get; set; }

    public WeaponClass? Weapon { get; set; }
    public ArmorClass? Armor { get; set; }

    public int Bushcraft { get; set; }
    public int Cunning { get; set; }

    /// <summary>
    /// Build a Phase-1 profile from the live player state. Hard-coded weapon-class →
    /// damage die and armor-class → AC. Phase 5 will tune; Phase 2 wires per-weapon dice.
    /// </summary>
    public static CombatPlayerProfile From(int combatSkill, int bushcraft, int cunning,
        WeaponClass? weapon, ArmorClass? armor)
    {
        var (dieCount, dieSize) = WeaponDie(weapon);
        return new CombatPlayerProfile
        {
            BaseAc = ArmorAc(armor),
            AttackBonus = combatSkill,
            DamageBonus = 0,
            DamageDieCount = dieCount,
            DamageDieSize = dieSize,
            Weapon = weapon,
            Armor = armor,
            Bushcraft = bushcraft,
            Cunning = cunning,
        };
    }

    static (int count, int size) WeaponDie(WeaponClass? weapon) => weapon switch
    {
        WeaponClass.Sword  => (1, 8),
        WeaponClass.Axe    => (1, 8),
        WeaponClass.Dagger => (1, 4),
        _                  => (1, 4),  // unarmed: 1d4 fists
    };

    static int ArmorAc(ArmorClass? armor) => armor switch
    {
        ArmorClass.Heavy  => 17,
        ArmorClass.Medium => 14,
        ArmorClass.Light  => 12,
        _                 => 10,  // unarmored
    };
}
