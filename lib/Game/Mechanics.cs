using Dreamlands.Rules;
using ArcRewardSlot = Dreamlands.Rules.ArcRewardSlot;

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
            "damage_spirits" => ApplyDamageSpirits(args, state, balance),
            "heal_spirits" => ApplyHealSpirits(args, state, balance),
            "give_gold" => ApplyGiveGold(args, state, balance),
            "rem_gold" => ApplyRemGold(args, state, balance),
            "add_level" => ApplyAddLevel(state),
            "add_item" => ApplyAddItem(args, state, balance, rng),
            "add_random_items" => ApplyAddRandomItems(args, state, balance, rng),
            "lose_random_item" => ApplyLoseRandomItem(state, rng),
            "equip" => ApplyEquip(args, state, balance),
            "unequip" => ApplyUnequip(args, state, balance),
            "discard" => ApplyDiscard(args, state),
            "upgrade_pack" => ApplyUpgradePack(args, state),
            "set_name" => ApplySetName(args, state),
            "add_tag" => ApplyAddTag(args, state),
            "remove_tag" => ApplyRemoveTag(args, state),
            "quality" => ApplyQuality(args, state),
            "add_condition" => ApplyAddCondition(args, state, balance, rng),
            "remove_condition" => ApplyRemoveCondition(args, state),
            "skip_time" => ApplySkipTime(args, state),
            "advance_time" => ApplyAdvanceTime(args, state),
            "open" => ApplyOpen(args),
            "combat" => args.Count >= 1 ? new MechanicResult.CombatStarted(args[0]) : null,
            "chain" => args.Count >= 1 ? new MechanicResult.ChainQueued(args[0]) : null,
            "repool" => new MechanicResult.Repooled(),
            "finish_dungeon" => ApplyFinishDungeon(state),
            "flee_dungeon" => new MechanicResult.DungeonFled(),
            _ => null,
        };
    }

    static MechanicResult ApplyDamageSpirits(List<string> args, PlayerState state, BalanceData balance)
    {
        var amount = ParseAmount(args);
        state.Spirits = Math.Max(0, state.Spirits - amount);
        return new MechanicResult.SpiritsChanged(-amount, state.Spirits);
    }

    static MechanicResult ApplyHealSpirits(List<string> args, PlayerState state, BalanceData balance)
    {
        var amount = ParseAmount(args);
        state.Spirits = Math.Min(state.MaxSpirits, state.Spirits + amount);
        return new MechanicResult.SpiritsChanged(amount, state.Spirits);
    }

    static MechanicResult ApplyGiveGold(List<string> args, PlayerState state, BalanceData balance)
    {
        var amount = ParseAmount(args);
        state.Gold += amount;
        return new MechanicResult.GoldChanged(amount, state.Gold);
    }

    static MechanicResult ApplyRemGold(List<string> args, PlayerState state, BalanceData balance)
    {
        var amount = ParseAmount(args);
        state.Gold = Math.Max(0, state.Gold - amount);
        return new MechanicResult.GoldChanged(-amount, state.Gold);
    }

    static MechanicResult ApplyAddLevel(PlayerState state)
    {
        state.PendingLevels++;
        return new MechanicResult.LevelAdded(state.PendingLevels);
    }

    /// <summary>
    /// Apply a tableau reward slot pick. Validates cap, applies effect, decrements PendingLevels.
    /// Returns null if the slot id is invalid or already at cap.
    /// </summary>
    public static MechanicResult? ApplyArcReward(PlayerState state, string slotId)
    {
        var slot = Array.Find(ArcRewards.All, s => s.Id == slotId);
        if (slot == null) return null;

        var taken = state.ArcRewardsTaken.GetValueOrDefault(slotId);
        if (taken >= slot.Cap) return null;

        switch (slot.Kind)
        {
            case ArcRewardKind.Skill:
                if (slot.Skill.HasValue)
                {
                    var current = (int)state.Skills.GetValueOrDefault(slot.Skill.Value);
                    var newTier = (SkillTier)Math.Min(current + 1, (int)SkillTier.Expert);
                    state.Skills[slot.Skill.Value] = newTier;
                }
                break;

            case ArcRewardKind.Health:
                state.MaxHealth += ArcRewards.HealthPerPick;
                state.Health += ArcRewards.HealthPerPick;
                break;

            case ArcRewardKind.Inventory:
                state.PackCapacity += ArcRewards.InventoryPerPick;
                break;
        }

        state.ArcRewardsTaken[slotId] = taken + 1;
        if (state.PendingLevels > 0)
            state.PendingLevels--;

        return new MechanicResult.ArcRewardTaken(slotId, slot.Label, taken + 1);
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

    static MechanicResult? ApplyEquip(List<string> args, PlayerState state, BalanceData balance)
    {
        if (args.Count < 1) return null;
        var itemId = args[0];

        var item = state.Pack.FirstOrDefault(i => i.DefId == itemId && !i.IsEquipped);
        if (item == null) return null;
        if (!balance.Items.TryGetValue(itemId, out var def)) return null;
        if (def.Type is not (ItemType.Weapon or ItemType.Armor)) return null;

        var slot = def.Type switch
        {
            ItemType.Weapon => "weapon",
            ItemType.Armor => "armor",
            _ => ""
        };

        // Unequip any currently equipped item of this type
        foreach (var existing in state.Pack)
        {
            if (!existing.IsEquipped) continue;
            if (balance.Items.TryGetValue(existing.DefId, out var existingDef) && existingDef.Type == def.Type)
                existing.IsEquipped = false;
        }

        item.IsEquipped = true;
        return new MechanicResult.ItemEquipped(itemId, def.Name, slot);
    }

    static MechanicResult? ApplyUnequip(List<string> args, PlayerState state, BalanceData balance)
    {
        if (args.Count < 1) return null;
        var slot = args[0].ToLowerInvariant();

        var itemType = slot switch
        {
            "weapon" => (ItemType?)ItemType.Weapon,
            "armor" => ItemType.Armor,
            _ => null
        };
        if (itemType == null) return null;

        var item = state.Pack.FirstOrDefault(i => i.IsEquipped
            && balance.Items.TryGetValue(i.DefId, out var d) && d.Type == itemType);

        if (item == null) return null;
        item.IsEquipped = false;

        var displayName = item.DisplayName;
        if (balance.Items.TryGetValue(item.DefId, out var def))
            displayName = def.Name;

        return new MechanicResult.ItemUnequipped(item.DefId, displayName, slot);
    }

    static MechanicResult? ApplyDiscard(List<string> args, PlayerState state)
    {
        if (args.Count < 1) return null;
        var itemId = args[0];

        var index = state.Pack.FindIndex(i => i.DefId == itemId);
        if (index >= 0)
        {
            var item = state.Pack[index];
            state.Pack.RemoveAt(index);
            return new MechanicResult.ItemLost(item.DefId, item.DisplayName);
        }

        return null;
    }

    static MechanicResult ApplyUpgradePack(List<string> args, PlayerState state)
    {
        var amount = ParseAmount(args);
        state.PackCapacity += amount;
        return new MechanicResult.PackUpgraded(amount, state.PackCapacity);
    }

    /// <summary>Route an item to Pack. Haversack is now a view over Pack.</summary>
    static void AddItemToInventory(ItemDef? def, ItemInstance instance, PlayerState state)
    {
        state.Pack.Add(instance);
    }

    static MechanicResult? ApplyQuality(List<string> args, PlayerState state)
    {
        if (args.Count < 2) return null;
        if (!int.TryParse(args[1], out var amount)) return null;

        var id = args[0];
        var current = state.Qualities.GetValueOrDefault(id);
        var newValue = current + amount;
        state.Qualities[id] = newValue;
        return new MechanicResult.QualityChanged(id, amount, newValue);
    }

    static MechanicResult? ApplySetName(List<string> args, PlayerState state)
    {
        if (args.Count < 1 || string.IsNullOrWhiteSpace(args[0])) return null;
        state.Name = args[0];
        return new MechanicResult.NameChanged(args[0]);
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

    static MechanicResult? ApplyAddCondition(List<string> args, PlayerState state, BalanceData balance, Random rng)
    {
        if (args.Count < 1) return null;
        var id = args[0];
        if (state.ActiveConditions.Contains(id)) return null;

        // Passive resist: travel conditions → Bushcraft, serious → Cunning
        var resistSkill = id switch
        {
            "exhausted" or "freezing" or "thirsty" or "lost" => Skill.Bushcraft,
            "injured" or "poisoned" or "irradiated" or "lattice_sickness" => Skill.Cunning,
            _ => (Skill?)null,
        };

        var tier = resistSkill.HasValue
            ? state.Skills.GetValueOrDefault(resistSkill.Value)
            : SkillTier.Untrained;

        if (SkillResolution.RollPassiveResist(tier, rng))
            return new MechanicResult.ConditionResisted(id, null);

        state.ActiveConditions.Add(id);
        return new MechanicResult.ConditionAdded(id, null);
    }

    static MechanicResult? ApplyRemoveCondition(List<string> args, PlayerState state)
    {
        if (args.Count < 1) return null;
        var id = args[0];
        if (!state.ActiveConditions.Remove(id)) return null;
        state.ConditionsClearedThisTurn.Add(id);
        return new MechanicResult.ConditionRemoved(id);
    }

    static MechanicResult ApplySkipTime(List<string> args, PlayerState state)
    {
        if (args.Count < 1) return new MechanicResult.TimeAdvanced(state.Time, state.Day);

        var target = TimePeriods.FromScriptName(args[0]);
        if (target == null) return new MechanicResult.TimeAdvanced(state.Time, state.Day);

        // Parse flags (args after the time period)
        var flags = args.Skip(1).ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Advance forward — if target is before or equal to current, wrap to next day
        if (target.Value <= state.Time)
        {
            state.Day++;
            state.PendingEndOfDay = true;
            if (flags.Contains("no_sleep")) state.PendingNoSleep = true;
            if (flags.Contains("no_meal")) state.PendingNoMeal = true;
            if (flags.Contains("no_biome")) state.PendingNoBiome = true;
        }

        state.Time = target.Value;
        return new MechanicResult.TimeAdvanced(state.Time, state.Day);
    }

    static MechanicResult ApplyAdvanceTime(List<string> args, PlayerState state)
    {
        if (args.Count < 1 || !int.TryParse(args[0], out var steps) || steps < 1)
            return new MechanicResult.TimeAdvanced(state.Time, state.Day);

        var flags = args.Skip(1).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var (newPeriod, daysCrossed) = TimePeriods.Advance(state.Time, steps);

        if (daysCrossed > 0)
        {
            state.Day += daysCrossed;
            state.PendingEndOfDay = true;
            if (flags.Contains("no_sleep")) state.PendingNoSleep = true;
            if (flags.Contains("no_meal")) state.PendingNoMeal = true;
            if (flags.Contains("no_biome")) state.PendingNoBiome = true;
        }

        state.Time = newPeriod;
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

    static int ParseAmount(List<string> args)
    {
        if (args.Count < 1) return 1;
        return int.TryParse(args[0], out var n) && n > 0 ? n : 1;
    }
}
