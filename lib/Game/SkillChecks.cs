using Dreamlands.Rules;

namespace Dreamlands.Game;

public enum RollMode { Normal, Advantage, Disadvantage }

/// <summary>Result of a skill check roll.</summary>
public record SkillCheckResult(
    bool Passed, int Rolled, int Target, int Modifier,
    int SkillLevel, Skill Skill,
    RollMode RollMode = RollMode.Normal,
    int NaturalRoll = 0,
    bool WasLuckyReroll = false);

/// <summary>Skill check dice rolling.</summary>
public static class SkillChecks
{
    /// <summary>
    /// Roll a skill check: d20 + skill level + equipment bonus >= DC.
    /// Low spirits imposes disadvantage. Natural 1 always fails, natural 20 always passes.
    /// On failure, luck may trigger a reroll.
    /// </summary>
    public static SkillCheckResult Roll(
        Skill skill, Difficulty difficulty, PlayerState state, BalanceData balance, Random rng,
        RollMode rollMode = RollMode.Normal)
    {
        var dc = difficulty.Target();
        var skillLevel = state.Skills.GetValueOrDefault(skill);
        var itemBonus = GetItemBonus(skill, state, balance);
        var modifier = skillLevel + itemBonus;

        // Low spirits imposes disadvantage
        if (HasSpiritsDisadvantage(state.Spirits, balance))
        {
            rollMode = rollMode switch
            {
                RollMode.Normal => RollMode.Disadvantage,
                RollMode.Advantage => RollMode.Normal, // cancel out
                _ => rollMode,
            };
        }

        var result = RollOnce(dc, modifier, skillLevel, skill, rollMode, rng);

        // Luck reroll on failure
        if (!result.Passed)
        {
            var luckLevel = state.Skills.GetValueOrDefault(Skill.Luck);
            if (TryLuckReroll(luckLevel, balance, rng))
            {
                var reroll = RollOnce(dc, modifier, skillLevel, skill, rollMode, rng);
                return reroll with { WasLuckyReroll = true };
            }
        }

        return result;
    }

    static SkillCheckResult RollOnce(
        int dc, int modifier, int skillLevel, Skill skill, RollMode rollMode, Random rng)
    {
        var natural = RollD20(rollMode, rng);

        // Natural 1 always fails, natural 20 always passes
        var passed = natural switch
        {
            1 => false,
            20 => true,
            _ => natural + modifier >= dc,
        };

        return new SkillCheckResult(passed, natural + modifier, dc, modifier, skillLevel, skill, rollMode, natural);
    }

    static int RollD20(RollMode mode, Random rng)
    {
        var first = rng.Next(1, 21);
        if (mode == RollMode.Normal) return first;

        var second = rng.Next(1, 21);
        return mode == RollMode.Advantage ? Math.Max(first, second) : Math.Min(first, second);
    }

    /// <summary>True if spirits are low enough to impose disadvantage on checks.</summary>
    public static bool HasSpiritsDisadvantage(int spirits, BalanceData balance) =>
        spirits <= balance.Character.SpiritDisadvantageThreshold;

    static bool TryLuckReroll(int luckLevel, BalanceData balance, Random rng)
    {
        if (luckLevel <= 0) return false;

        var chances = balance.Character.LuckRerollChance;
        var index = Math.Min(luckLevel, chances.Count - 1);
        var threshold = chances[index];
        if (threshold <= 0) return false;

        return rng.Next(100) < threshold;
    }

    /// <summary>
    /// Get item bonus for an encounter skill check. Each skill draws from specific gear sources:
    /// Combat = weapon, Cunning = armor, Negotiation/Bushcraft/Mercantile = two best tools, Luck = none.
    /// </summary>
    internal static int GetItemBonus(Skill skill, PlayerState state, BalanceData balance)
    {
        return skill switch
        {
            Skill.Combat => GetEquippedMod(state.Equipment.Weapon, Skill.Combat, balance),
            Skill.Cunning => GetEquippedMod(state.Equipment.Armor, Skill.Cunning, balance),
            Skill.Negotiation => GetBestToolBonuses(Skill.Negotiation, state, balance),
            Skill.Bushcraft => GetBestToolBonuses(Skill.Bushcraft, state, balance),
            Skill.Mercantile => GetBestToolBonuses(Skill.Mercantile, state, balance),
            _ => 0, // Luck gets no gear bonus
        };
    }

    static int GetEquippedMod(ItemInstance? slot, Skill skill, BalanceData balance)
    {
        if (slot == null) return 0;
        if (balance.Items.TryGetValue(slot.DefId, out var def)
            && def.SkillModifiers.TryGetValue(skill, out var mod))
            return mod;
        return 0;
    }

    static int GetBestToolBonuses(Skill skill, PlayerState state, BalanceData balance)
    {
        int best = 0, secondBest = 0;

        foreach (var item in state.Haversack)
        {
            if (!balance.Items.TryGetValue(item.DefId, out var def)) continue;
            if (def.Type != ItemType.Tool) continue;
            if (!def.SkillModifiers.TryGetValue(skill, out var mod) || mod <= 0) continue;

            if (mod >= best) { secondBest = best; best = mod; }
            else if (mod > secondBest) { secondBest = mod; }
        }

        return best + secondBest;
    }
}
