using Dreamlands.Rules;

namespace Dreamlands.CombatHarness;

public sealed record Loadout(string Tier, ItemDef? Weapon, ItemDef? Armor)
{
    public string Label => $"{Weapon?.Id ?? "-"} / {Armor?.Id ?? "-"}";
}

/// <summary>
/// Tier-coherent gear. <see cref="ItemDef.RequiredCombat"/> gates weapon and armor
/// class together (0 = Dagger/Light, 2 = Axe/Medium, 4 = Sword/Heavy), so a
/// player who trains Combat and buys level-appropriate kit moves both at once.
/// We only cross gear *within* a band: "capstone armor and a starting dagger" is
/// a legal loadout but not one worth spending trials on.
///
/// Note Combat skill has no mechanical effect inside a fight — it is purely a
/// purchase gate — so the band IS the progression axis, and the harness does not
/// need to set PlayerState.Skills at all.
/// </summary>
public static class Loadouts
{
    public static readonly (int Req, string Name)[] Bands =
    [
        (0, "T0 dagger/light"),
        (2, "T1 axe/medium"),
        (4, "T2 sword/heavy"),
    ];

    public static List<Loadout> ForBand(int req)
    {
        var weapons = ItemDef.All.Values.Where(d => d.Type == ItemType.Weapon && d.RequiredCombat == req).ToList();
        var armors  = ItemDef.All.Values.Where(d => d.Type == ItemType.Armor  && d.RequiredCombat == req).ToList();
        var name = Bands.First(b => b.Req == req).Name;
        return (from w in weapons from a in armors select new Loadout(name, w, a)).ToList();
    }

    /// <summary>The floor: no gear at all. No weapon means no Attack in the pool.</summary>
    public static Loadout Unarmed => new("unarmed", null, null);
}
