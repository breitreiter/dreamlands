using Dreamlands.Rules;

namespace Dreamlands.Game;

/// <summary>Result of a skill check roll.</summary>
public record SkillCheckResult(bool Passed, int Rolled, int Target, int Modifier, Skill Skill);

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
        var equipmentBonus = GetEquipmentBonus(skill, state);

        var modifier = skillLevel + spiritsPenalty + equipmentBonus;
        var roll = rng.Next(1, 21);
        var total = roll + modifier;

        return new SkillCheckResult(total >= dc, roll, dc, modifier, skill);
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

    /// <summary>Get equipment bonus for a skill check.</summary>
    static int GetEquipmentBonus(Skill skill, PlayerState state)
    {
        if (skill != Skill.Combat) return 0;

        int bonus = 0;
        if (state.Equipment.Weapon != null)
        {
            // Combat bonus from weapon — lookup will happen via balance in the future
            // For now, the weapon's DefId encodes quality tier
        }
        return bonus;
    }
}
