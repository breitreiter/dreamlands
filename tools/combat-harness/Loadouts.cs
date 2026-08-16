using Dreamlands.Rules;

namespace Dreamlands.CombatHarness;

public sealed record Loadout(string Band, ItemDef? Weapon, ItemDef? Armor)
{
    public string Label => $"{Weapon?.Id ?? "-"} / {Armor?.Id ?? "-"}";
}

/// <summary>
/// Gear grouped the way a player actually acquires it.
///
/// <see cref="ItemDef.RequiredCombat"/> gates weapon and armor class together
/// (0 = Dagger/Light, 2 = Axe/Medium, 4 = Sword/Heavy), so training Combat moves
/// both at once. Within a band we split by where the gear comes from:
///
///   lower — <c>ShopTier 1</c>. What you buy off the shelf on arriving at this
///           level of training. The realistic kit for someone who just trained up.
///   upper — <c>ShopTier 2</c> plus the unique/quest items (no ShopTier at all).
///           Endgame kit for that level of training.
///
/// Gear crosses only within a half-band. Capstone armor with a starting dagger is
/// legal but not worth trials.
///
/// The interesting comparison this enables: <c>T0 upper</c> vs <c>T2 lower</c> —
/// a non-combatant with the best light kit in the game, against someone who spent
/// their first two levels on Combat and then bought whatever the store had.
///
/// Note Combat skill has no mechanical effect inside a fight — it is purely a
/// purchase gate — so the band IS the progression axis, and the harness never
/// needs to set PlayerState.Skills.
/// </summary>
public static class Loadouts
{
    public static readonly (int Req, string Name)[] Tiers =
    [
        (0, "T0 dagger/light"),
        (2, "T1 axe/medium"),
        (4, "T2 sword/heavy"),
    ];

    /// <summary>Store-bought (ShopTier 1) vs endgame (ShopTier 2 or unique).</summary>
    static bool IsUpper(ItemDef d) => d.ShopTier is null or >= 2;

    public static List<(string Name, List<Loadout> Kit)> All()
    {
        var result = new List<(string, List<Loadout>)>();
        foreach (var (req, tierName) in Tiers)
        {
            foreach (var upper in new[] { false, true })
            {
                var name = $"{tierName} {(upper ? "upper" : "lower")}";
                var weapons = ItemDef.All.Values
                    .Where(d => d.Type == ItemType.Weapon && d.RequiredCombat == req && IsUpper(d) == upper).ToList();
                var armors = ItemDef.All.Values
                    .Where(d => d.Type == ItemType.Armor && d.RequiredCombat == req && IsUpper(d) == upper).ToList();
                var kit = (from w in weapons from a in armors select new Loadout(name, w, a)).ToList();
                if (kit.Count > 0) result.Add((name, kit));
            }
        }
        return result;
    }

    /// <summary>The floor: no gear at all. No weapon means no Attack in the pool.</summary>
    public static Loadout Unarmed => new("unarmed", null, null);
}
