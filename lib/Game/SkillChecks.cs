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
    /// Roll a resist check for an ambient condition. Uses resist bonuses instead of skill bonuses,
    /// and maps each condition to its resist skill (if any).
    /// </summary>
    public static SkillCheckResult RollResist(
        string conditionId, Difficulty difficulty, PlayerState state, BalanceData balance, Random rng)
    {
        var dc = difficulty.Target();

        // Map condition to skill (some conditions are gear-only, no skill bonus)
        var skill = conditionId switch
        {
            "freezing" or "thirsty" or "lost" => (Skill?)Skill.Bushcraft,
            "poisoned" => Skill.Bushcraft,
            "injured" => Skill.Combat,
            _ => null, // swamp_fever, gut_worms, irradiated, exhausted — gear only
        };

        var skillLevel = skill != null ? state.Skills.GetValueOrDefault(skill.Value) : 0;
        var resistBonus = GetResistBonus(conditionId, state, balance);
        var modifier = skillLevel + resistBonus;
        var rollSkill = skill ?? Skill.Luck; // placeholder for result record

        var rollMode = RollMode.Normal;
        if (HasSpiritsDisadvantage(state.Spirits, balance))
        {
            rollMode = RollMode.Disadvantage;
        }

        var result = RollOnce(dc, modifier, skillLevel, rollSkill, rollMode, rng);

        // Luck reroll on failure
        if (!result.Passed)
        {
            var luckLevel = state.Skills.GetValueOrDefault(Skill.Luck);
            if (TryLuckReroll(luckLevel, balance, rng))
            {
                var reroll = RollOnce(dc, modifier, skillLevel, rollSkill, rollMode, rng);
                return reroll with { WasLuckyReroll = true };
            }
        }

        return result;
    }

    /// <summary>
    /// Get item bonus for an encounter skill check. Each skill draws from specific gear sources
    /// (per dice_mechanics.md): Combat = weapon + token, Cunning = armor + token,
    /// Negotiation/Bushcraft/Mercantile = two best tools + token, Luck = none.
    /// </summary>
    internal static int GetItemBonus(Skill skill, PlayerState state, BalanceData balance)
    {
        var gearBonus = skill switch
        {
            Skill.Combat => GetEquippedMod(state.Equipment.Weapon, Skill.Combat, balance),
            Skill.Cunning => GetEquippedMod(state.Equipment.Armor, Skill.Cunning, balance),
            Skill.Negotiation => GetBestToolBonuses(Skill.Negotiation, state, balance),
            Skill.Bushcraft => GetBestToolBonuses(Skill.Bushcraft, state, balance),
            Skill.Mercantile => GetBestToolBonuses(Skill.Mercantile, state, balance),
            _ => 0, // Luck gets no gear bonus
        };

        return gearBonus + GetTokenBonus(skill, state, balance);
    }

    /// <summary>
    /// Get resist bonus for a condition resist check. Converts ResistModifiers (Magnitude)
    /// to numeric bonuses via ResistBonusMagnitudes. Gear sources per dice_mechanics.md:
    /// Injury = armor(big) + token, Poison = armor(big) + token,
    /// Exhausted = boots(big) + best equipment(small) + token,
    /// Freezing/Thirsty = two best small gear + token,
    /// Swamp Fever/Gut Worms/Irradiated = consumable(big) + best equipment(small) + token.
    /// </summary>
    internal static int GetResistBonus(string conditionId, PlayerState state, BalanceData balance)
    {
        var magnitudes = balance.Character.ResistBonusMagnitudes;

        int bonus = conditionId switch
        {
            "injured" => GetEquippedResist(state.Equipment.Armor, conditionId, magnitudes, balance),
            "poison" => GetEquippedResist(state.Equipment.Armor, conditionId, magnitudes, balance),
            "exhausted" => GetEquippedResist(state.Equipment.Boots, conditionId, magnitudes, balance)
                         + GetBestPackResist(conditionId, magnitudes, state, balance, 1),
            "freezing" or "thirsty" or "lost" => GetBestPackResist(conditionId, magnitudes, state, balance, 2),
            "swamp_fever" or "gut_worms" or "irradiated" =>
                GetBestConsumedResist(conditionId, magnitudes, state, balance)
                + GetBestPackResist(conditionId, magnitudes, state, balance, 1),
            _ => 0,
        };

        return bonus + GetTokenResist(conditionId, magnitudes, state, balance);
    }

    static int GetEquippedMod(ItemInstance? slot, Skill skill, BalanceData balance)
    {
        if (slot == null) return 0;
        if (balance.Items.TryGetValue(slot.DefId, out var def)
            && def.SkillModifiers.TryGetValue(skill, out var mod))
            return mod;
        return 0;
    }

    static int GetEquippedResist(ItemInstance? slot, string conditionId,
        IReadOnlyDictionary<Magnitude, int> magnitudes, BalanceData balance)
    {
        if (slot == null) return 0;
        if (balance.Items.TryGetValue(slot.DefId, out var def)
            && def.ResistModifiers.TryGetValue(conditionId, out var mag)
            && magnitudes.TryGetValue(mag, out var bonus))
            return bonus;
        return 0;
    }

    static int GetBestToolBonuses(Skill skill, PlayerState state, BalanceData balance)
    {
        int best = 0, secondBest = 0;

        foreach (var item in state.Pack)
        {
            if (!balance.Items.TryGetValue(item.DefId, out var def)) continue;
            if (def.Type != ItemType.Tool) continue;
            if (!def.SkillModifiers.TryGetValue(skill, out var mod) || mod <= 0) continue;

            if (mod >= best) { secondBest = best; best = mod; }
            else if (mod > secondBest) { secondBest = mod; }
        }

        return best + secondBest;
    }

    /// <summary>Token bonus: best +1 from haversack tokens with a SkillModifier for this skill.</summary>
    static int GetTokenBonus(Skill skill, PlayerState state, BalanceData balance)
    {
        foreach (var item in state.Haversack)
        {
            if (!balance.Items.TryGetValue(item.DefId, out var def)) continue;
            if (def.Type != ItemType.Token) continue;
            if (def.SkillModifiers.TryGetValue(skill, out var mod) && mod > 0)
                return Math.Min(mod, 1); // tokens cap at +1
        }
        return 0;
    }

    /// <summary>Token resist bonus: best +1 from haversack tokens with a ResistModifier for this condition.</summary>
    static int GetTokenResist(string conditionId, IReadOnlyDictionary<Magnitude, int> magnitudes,
        PlayerState state, BalanceData balance)
    {
        foreach (var item in state.Haversack)
        {
            if (!balance.Items.TryGetValue(item.DefId, out var def)) continue;
            if (def.Type != ItemType.Token) continue;
            if (def.ResistModifiers.TryGetValue(conditionId, out var mag)
                && magnitudes.TryGetValue(mag, out var bonus) && bonus > 0)
                return Math.Min(bonus, 1); // tokens cap at +1
        }
        return 0;
    }

    /// <summary>Best N pack-held equipment (tools) resist bonuses for a condition.</summary>
    static int GetBestPackResist(string conditionId, IReadOnlyDictionary<Magnitude, int> magnitudes,
        PlayerState state, BalanceData balance, int count)
    {
        var bonuses = new List<int>();
        foreach (var item in state.Pack)
        {
            if (!balance.Items.TryGetValue(item.DefId, out var def)) continue;
            if (def.Type != ItemType.Tool) continue;
            if (def.ResistModifiers.TryGetValue(conditionId, out var mag)
                && magnitudes.TryGetValue(mag, out var bonus) && bonus > 0)
                bonuses.Add(bonus);
        }
        bonuses.Sort((a, b) => b.CompareTo(a));
        int total = 0;
        for (int i = 0; i < Math.Min(count, bonuses.Count); i++)
            total += bonuses[i];
        return total;
    }

    /// <summary>Best consumed (haversack consumable) resist bonus for a condition.</summary>
    static int GetBestConsumedResist(string conditionId, IReadOnlyDictionary<Magnitude, int> magnitudes,
        PlayerState state, BalanceData balance)
    {
        int best = 0;
        foreach (var item in state.Haversack)
        {
            if (!balance.Items.TryGetValue(item.DefId, out var def)) continue;
            if (def.Type != ItemType.Consumable) continue;
            if (def.ResistModifiers.TryGetValue(conditionId, out var mag)
                && magnitudes.TryGetValue(mag, out var bonus) && bonus > best)
                best = bonus;
        }
        return best;
    }
}
