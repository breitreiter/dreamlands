using Dreamlands.Rules;

namespace Dreamlands.Game;

/// <summary>
/// Core mechanic engine. Every method takes state + balance + rng, mutates state, returns results.
/// Stateless: all context passed in, no singletons.
/// </summary>
public static class Mechanics
{
    /// <summary>Apply a list of mechanic action strings to player state.</summary>
    public static List<MechanicResult> Apply(IReadOnlyList<string> mechanics, PlayerState state, BalanceData balance, Random rng)
    {
        var results = new List<MechanicResult>();
        foreach (var mechanic in mechanics)
        {
            var result = ApplyOne(mechanic, state, balance, rng);
            if (result != null)
                results.Add(result);
        }
        return results;
    }

    static MechanicResult? ApplyOne(string mechanic, PlayerState state, BalanceData balance, Random rng)
    {
        var tokens = ActionVerb.Tokenize(mechanic);
        if (tokens.Count == 0) return null;

        var verb = tokens[0];
        var args = tokens.GetRange(1, tokens.Count - 1);

        return verb switch
        {
            "damage_health" => ApplyDamageHealth(args, state, balance),
            "heal" => ApplyHeal(args, state, balance),
            "damage_spirits" => ApplyDamageSpirits(args, state, balance),
            "heal_spirits" => ApplyHealSpirits(args, state, balance),
            "give_gold" => ApplyGiveGold(args, state, balance),
            "rem_gold" => ApplyRemGold(args, state, balance),
            "increase_skill" => ApplyIncreaseSkill(args, state, balance),
            "decrease_skill" => ApplyDecreaseSkill(args, state, balance),
            "add_item" => ApplyAddItem(args, state, balance, rng),
            "add_random_items" => ApplyAddRandomItems(args, state, balance, rng),
            "lose_random_item" => ApplyLoseRandomItem(state, rng),
            "get_random_treasure" => ApplyGetRandomTreasure(state, balance, rng),
            "equip" => ApplyEquip(args, state, balance),
            "unequip" => ApplyUnequip(args, state, balance),
            "discard" => ApplyDiscard(args, state),
            "add_tag" => ApplyAddTag(args, state),
            "remove_tag" => ApplyRemoveTag(args, state),
            "add_condition" => ApplyAddCondition(args, state, balance),
            "remove_condition" => ApplyRemoveCondition(args, state),
            "skip_time" => ApplySkipTime(args, state),
            "open" => ApplyOpen(args),
            "finish_dungeon" => ApplyFinishDungeon(state),
            "flee_dungeon" => new MechanicResult.DungeonFled(),
            _ => null,
        };
    }

    static MechanicResult ApplyDamageHealth(List<string> args, PlayerState state, BalanceData balance)
    {
        var amount = ResolveMagnitude(args, balance.Character.DamageMagnitudes);
        state.Health = Math.Max(0, state.Health - amount);
        return new MechanicResult.HealthChanged(-amount, state.Health);
    }

    static MechanicResult ApplyHeal(List<string> args, PlayerState state, BalanceData balance)
    {
        var amount = ResolveMagnitude(args, balance.Character.DamageMagnitudes);
        state.Health = Math.Min(state.MaxHealth, state.Health + amount);
        return new MechanicResult.HealthChanged(amount, state.Health);
    }

    static MechanicResult ApplyDamageSpirits(List<string> args, PlayerState state, BalanceData balance)
    {
        var amount = ResolveMagnitude(args, balance.Character.DamageMagnitudes);
        state.Spirits = Math.Max(0, state.Spirits - amount);
        return new MechanicResult.SpiritsChanged(-amount, state.Spirits);
    }

    static MechanicResult ApplyHealSpirits(List<string> args, PlayerState state, BalanceData balance)
    {
        var amount = ResolveMagnitude(args, balance.Character.DamageMagnitudes);
        state.Spirits = Math.Min(state.MaxSpirits, state.Spirits + amount);
        return new MechanicResult.SpiritsChanged(amount, state.Spirits);
    }

    static MechanicResult ApplyGiveGold(List<string> args, PlayerState state, BalanceData balance)
    {
        var amount = ResolveMagnitude(args, balance.Character.DamageMagnitudes);
        state.Gold += amount;
        return new MechanicResult.GoldChanged(amount, state.Gold);
    }

    static MechanicResult ApplyRemGold(List<string> args, PlayerState state, BalanceData balance)
    {
        var amount = ResolveMagnitude(args, balance.Character.DamageMagnitudes);
        state.Gold = Math.Max(0, state.Gold - amount);
        return new MechanicResult.GoldChanged(-amount, state.Gold);
    }

    static MechanicResult ApplyIncreaseSkill(List<string> args, PlayerState state, BalanceData balance)
    {
        if (args.Count < 2) return new MechanicResult.SkillChanged(Skill.Combat, 0, 0);

        var skill = Skills.FromScriptName(args[0]);
        if (skill == null) return new MechanicResult.SkillChanged(Skill.Combat, 0, 0);

        var amount = ResolveMagnitude(args.GetRange(1, 1), balance.Character.SkillBumpMagnitudes);
        var current = state.Skills.GetValueOrDefault(skill.Value);
        var newLevel = Math.Min(balance.Character.MaxSkillLevel, current + amount);
        var delta = newLevel - current;
        state.Skills[skill.Value] = newLevel;

        return new MechanicResult.SkillChanged(skill.Value, delta, newLevel);
    }

    static MechanicResult ApplyDecreaseSkill(List<string> args, PlayerState state, BalanceData balance)
    {
        if (args.Count < 2) return new MechanicResult.SkillChanged(Skill.Combat, 0, 0);

        var skill = Skills.FromScriptName(args[0]);
        if (skill == null) return new MechanicResult.SkillChanged(Skill.Combat, 0, 0);

        var amount = ResolveMagnitude(args.GetRange(1, 1), balance.Character.SkillBumpMagnitudes);
        var current = state.Skills.GetValueOrDefault(skill.Value);
        var newLevel = Math.Max(0, current - amount);
        var delta = newLevel - current;
        state.Skills[skill.Value] = newLevel;

        return new MechanicResult.SkillChanged(skill.Value, delta, newLevel);
    }

    static MechanicResult ApplyAddItem(List<string> args, PlayerState state, BalanceData balance, Random rng)
    {
        if (args.Count < 1) return new MechanicResult.ItemGained("", "unknown");

        var itemId = args[0];
        var displayName = itemId;
        ItemDef? def = null;

        if (balance.Items.TryGetValue(itemId, out def))
            displayName = def.Name;

        var instance = new ItemInstance(itemId, displayName);
        AddItemToInventory(def, instance, state);
        return new MechanicResult.ItemGained(itemId, displayName);
    }

    static MechanicResult? ApplyAddRandomItems(List<string> args, PlayerState state, BalanceData balance, Random rng)
    {
        if (args.Count < 2) return null;
        if (!int.TryParse(args[0], out var count)) return null;
        var category = args[1];

        var candidates = balance.Items.Values
            .Where(i => i.Type.ToString().Equals(category, StringComparison.OrdinalIgnoreCase) ||
                        i.Id.Contains(category, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (candidates.Count == 0) return null;

        MechanicResult? last = null;
        for (int i = 0; i < count; i++)
        {
            var item = candidates[rng.Next(candidates.Count)];
            var instance = new ItemInstance(item.Id, item.Name);
            AddItemToInventory(item, instance, state);
            last = new MechanicResult.ItemGained(item.Id, item.Name);
        }
        return last;
    }

    static MechanicResult? ApplyLoseRandomItem(PlayerState state, Random rng)
    {
        // Lose from Pack only (valuable gear — thief/disaster scenario)
        if (state.Pack.Count == 0) return null;

        var index = rng.Next(state.Pack.Count);
        var item = state.Pack[index];
        state.Pack.RemoveAt(index);
        return new MechanicResult.ItemLost(item.DefId, item.DisplayName);
    }

    static MechanicResult? ApplyGetRandomTreasure(PlayerState state, BalanceData balance, Random rng)
    {
        var candidates = balance.Items.Values
            .Where(i => i.Cost is Magnitude.Medium or Magnitude.Large or Magnitude.Huge)
            .ToList();

        if (candidates.Count == 0) return null;

        var item = candidates[rng.Next(candidates.Count)];
        var instance = new ItemInstance(item.Id, item.Name);
        AddItemToInventory(item, instance, state);
        return new MechanicResult.ItemGained(item.Id, item.Name);
    }

    static MechanicResult? ApplyEquip(List<string> args, PlayerState state, BalanceData balance)
    {
        if (args.Count < 1) return null;
        var itemId = args[0];

        var index = state.Pack.FindIndex(i => i.DefId == itemId);
        if (index < 0) return null;
        if (!balance.Items.TryGetValue(itemId, out var def)) return null;
        if (def.Type is not (ItemType.Weapon or ItemType.Armor or ItemType.Boots)) return null;

        var item = state.Pack[index];
        state.Pack.RemoveAt(index);

        // Swap: move currently equipped item back to Pack
        var slot = def.Type switch
        {
            ItemType.Weapon => "weapon",
            ItemType.Armor => "armor",
            ItemType.Boots => "boots",
            _ => ""
        };

        var old = def.Type switch
        {
            ItemType.Weapon => state.Equipment.Weapon,
            ItemType.Armor => state.Equipment.Armor,
            ItemType.Boots => state.Equipment.Boots,
            _ => null
        };

        if (old != null)
            state.Pack.Add(old);

        switch (def.Type)
        {
            case ItemType.Weapon: state.Equipment.Weapon = item; break;
            case ItemType.Armor: state.Equipment.Armor = item; break;
            case ItemType.Boots: state.Equipment.Boots = item; break;
        }

        return new MechanicResult.ItemEquipped(itemId, def.Name, slot);
    }

    static MechanicResult? ApplyUnequip(List<string> args, PlayerState state, BalanceData balance)
    {
        if (args.Count < 1) return null;
        var slot = args[0].ToLowerInvariant();

        ItemInstance? item = slot switch
        {
            "weapon" => state.Equipment.Weapon,
            "armor" => state.Equipment.Armor,
            "boots" => state.Equipment.Boots,
            _ => null
        };

        if (item == null) return null;

        switch (slot)
        {
            case "weapon": state.Equipment.Weapon = null; break;
            case "armor": state.Equipment.Armor = null; break;
            case "boots": state.Equipment.Boots = null; break;
        }

        state.Pack.Add(item);

        var defId = item.DefId;
        var displayName = item.DisplayName;
        if (balance.Items.TryGetValue(defId, out var def))
            displayName = def.Name;

        return new MechanicResult.ItemUnequipped(defId, displayName, slot);
    }

    static MechanicResult? ApplyDiscard(List<string> args, PlayerState state)
    {
        if (args.Count < 1) return null;
        var itemId = args[0];

        // Try Pack first, then Haversack
        var index = state.Pack.FindIndex(i => i.DefId == itemId);
        if (index >= 0)
        {
            var item = state.Pack[index];
            state.Pack.RemoveAt(index);
            return new MechanicResult.ItemLost(item.DefId, item.DisplayName);
        }

        index = state.Haversack.FindIndex(i => i.DefId == itemId);
        if (index >= 0)
        {
            var item = state.Haversack[index];
            state.Haversack.RemoveAt(index);
            return new MechanicResult.ItemLost(item.DefId, item.DisplayName);
        }

        return null;
    }

    /// <summary>Route an item to Pack (equippable gear) or Haversack (tools, consumables).</summary>
    static void AddItemToInventory(ItemDef? def, ItemInstance instance, PlayerState state)
    {
        if (def != null && !def.IsPackItem)
            state.Haversack.Add(instance);
        else
            state.Pack.Add(instance);
    }

    static MechanicResult? ApplyAddTag(List<string> args, PlayerState state)
    {
        if (args.Count < 1) return null;
        state.Tags.Add(args[0]);
        return new MechanicResult.TagAdded(args[0]);
    }

    static MechanicResult? ApplyRemoveTag(List<string> args, PlayerState state)
    {
        if (args.Count < 1) return null;
        state.Tags.Remove(args[0]);
        return new MechanicResult.TagRemoved(args[0]);
    }

    static MechanicResult? ApplyAddCondition(List<string> args, PlayerState state, BalanceData balance)
    {
        if (args.Count < 1) return null;
        var id = args[0];
        if (state.ActiveConditions.ContainsKey(id)) return null;

        var stacks = balance.Conditions.TryGetValue(id, out var def) ? def.Stacks : 1;
        state.ActiveConditions[id] = stacks;
        return new MechanicResult.ConditionAdded(id, stacks);
    }

    static MechanicResult? ApplyRemoveCondition(List<string> args, PlayerState state)
    {
        if (args.Count < 1) return null;
        var id = args[0];
        if (!state.ActiveConditions.Remove(id)) return null;
        return new MechanicResult.ConditionRemoved(id);
    }

    static MechanicResult ApplySkipTime(List<string> args, PlayerState state)
    {
        if (args.Count < 1) return new MechanicResult.TimeAdvanced(state.Time, state.Day);

        var target = TimePeriods.FromScriptName(args[0]);
        if (target == null) return new MechanicResult.TimeAdvanced(state.Time, state.Day);

        // Advance forward — if target is before or equal to current, wrap to next day
        if (target.Value <= state.Time)
            state.Day++;

        state.Time = target.Value;
        return new MechanicResult.TimeAdvanced(state.Time, state.Day);
    }

    static MechanicResult? ApplyOpen(List<string> args)
    {
        if (args.Count < 1) return null;
        return new MechanicResult.Navigation(args[0]);
    }

    static MechanicResult ApplyFinishDungeon(PlayerState state)
    {
        if (state.CurrentDungeonId != null)
            state.CompletedDungeons.Add(state.CurrentDungeonId);
        return new MechanicResult.DungeonFinished();
    }

    /// <summary>Resolve a magnitude argument to an integer value from a lookup table.</summary>
    static int ResolveMagnitude(List<string> args, IReadOnlyDictionary<Magnitude, int> table)
    {
        if (args.Count < 1) return 1;
        var mag = Magnitudes.FromScriptName(args[0]);
        if (mag == null) return 1;
        return table.GetValueOrDefault(mag.Value, 1);
    }
}
