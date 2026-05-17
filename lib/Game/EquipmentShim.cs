using Dreamlands.Rules;

namespace Dreamlands.Game;

/// <summary>
/// Shim exposing Equipment.Weapon/Armor/Boots read/write against the Pack's IsEquipped flag.
/// Replaces the deleted EquippedGear class. Phase 2 will remove callers of this shim.
/// </summary>
public class EquipmentShim(PlayerState state)
{
    public ItemInstance? Weapon
    {
        get => state.EquippedWeapon;
        set => SetEquipped(ItemType.Weapon, value);
    }

    public ItemInstance? Armor
    {
        get => state.EquippedArmor;
        set => SetEquipped(ItemType.Armor, value);
    }

    public ItemInstance? Boots
    {
        get => state.EquippedBoots;
        set => SetEquipped(ItemType.Boots, value);
    }

    void SetEquipped(ItemType type, ItemInstance? value)
    {
        // Clear any existing equipped item of this type
        foreach (var item in state.Pack)
        {
            if (!item.IsEquipped) continue;
            if (ItemDef.All.TryGetValue(item.DefId, out var d) && d.Type == type)
                item.IsEquipped = false;
        }

        if (value == null) return;

        // If the instance is already in Pack, mark it equipped
        var existing = state.Pack.FirstOrDefault(i => ReferenceEquals(i, value)
            || (i.DefId == value.DefId && !i.IsEquipped));
        if (existing != null)
        {
            existing.IsEquipped = true;
        }
        else
        {
            // Item not in pack — add it (legacy callers may do Equipment.Weapon = new ItemInstance(...))
            value.IsEquipped = true;
            state.Pack.Add(value);
        }
    }
}
