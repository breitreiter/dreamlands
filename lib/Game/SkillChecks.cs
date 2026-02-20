using Dreamlands.Rules;

namespace Dreamlands.Game;

/// <summary>Result of a skill check roll.</summary>
public record SkillCheckResult(bool Passed, int Rolled, int Target, int Modifier, int SkillLevel, Skill Skill);

/// <summary>Skill check dice rolling.</summary>
public static class SkillChecks
{
    /// <summary>
    /// Roll a skill check: d20 + skill level + spirits penalty + equipment bonus >= DC.
    /// </summary>
    public static SkillCheckResult Roll(Skill skill, Difficulty difficulty, PlayerState state, BalanceData balance, Random rng)
    {
        var dc = difficulty.Target();
        var skillLevel = state.Skills.GetValueOrDefault(skill);
        var spiritsPenalty = GetSpiritsPenalty(state.Spirits, balance);
        var itemBonus = GetItemBonus(skill, state, balance);

        var modifier = skillLevel + spiritsPenalty + itemBonus;
        var roll = rng.Next(1, 21);
        var total = roll + modifier;

        return new SkillCheckResult(total >= dc, roll, dc, modifier, skillLevel, skill);
    }

    /// <summary>Get the spirits penalty based on current spirits value and balance thresholds.</summary>
    public static int GetSpiritsPenalty(int spirits, BalanceData balance)
    {
        foreach (var threshold in balance.Character.SpiritsThresholds)
        {
            if (spirits <= threshold.AtOrBelow)
                return threshold.Penalty;
        }
        return 0;
    }

    /// <summary>
    /// Get item bonus for a skill check: equipped gear + passive inventory items (tools, consumables).
    /// </summary>
    static int GetItemBonus(Skill skill, PlayerState state, BalanceData balance)
    {
        int bonus = 0;

        // Equipped gear
        foreach (var slot in new[] { state.Equipment.Weapon, state.Equipment.Armor, state.Equipment.Boots })
        {
            if (slot == null) continue;
            if (balance.Items.TryGetValue(slot.DefId, out var def)
                && def.SkillModifiers.TryGetValue(skill, out var mod))
                bonus += mod;
        }

        // Passive items in Pack and Haversack (non-equippable types only)
        foreach (var container in new[] { state.Pack, state.Haversack })
        {
            foreach (var item in container)
            {
                if (!balance.Items.TryGetValue(item.DefId, out var def)) continue;
                if (def.IsPackItem) continue; // equippable items only count when equipped
                if (def.SkillModifiers.TryGetValue(skill, out var mod))
                    bonus += mod;
            }
        }

        return bonus;
    }
}
