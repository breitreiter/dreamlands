using Dreamlands.Rules;

namespace Dreamlands.Game;

// RollMode and SkillCheckResult retained for server/GameFunctions.cs compat — Phase 5 will remove them.

public enum RollMode { Normal, Advantage, Disadvantage }

/// <summary>Result of a skill check roll (retained for server compat; d20 roll path removed).</summary>
public record SkillCheckResult(
    bool Passed, int Rolled, int Target, int Modifier,
    int SkillLevel, Skill Skill,
    RollMode RollMode = RollMode.Normal,
    int NaturalRoll = 0,
    bool WasLuckyReroll = false,
    bool IsMeetsCheck = false);

/// <summary>
/// Gear-bonus helpers retained for server/GameFunctions.cs display panel (Phase 5 cleans this up).
/// The d20 Roll/RollResist/RollD20 paths are deleted — see SkillResolution.cs for the tier model.
/// Skill item bonuses are removed (dead under the approach picker model); GetItemBonus now returns 0.
/// </summary>
public static class SkillChecks
{
    /// <summary>
    /// Item bonus for encounter skill checks. Skill bonuses on gear are retired under the
    /// approach-picker model; this returns 0 for all skills. Retained for API compat.
    /// </summary>
    public static int GetItemBonus(Skill skill, PlayerState state, BalanceData balance) => 0;

    /// <summary>
    /// Get resist bonus for a condition resist check.
    /// Retained for the server mechanics-display panel; not called by EndOfDay internally.
    /// </summary>
    public static int GetResistBonus(string conditionId, PlayerState state, BalanceData balance)
    {
        return conditionId switch
        {
            "injured" => GetEquippedResist(state.EquippedArmor, conditionId, balance),
            "poison" => GetEquippedResist(state.EquippedArmor, conditionId, balance),
            "exhausted" => GetBestPackResist(conditionId, state, balance, 1),
            "freezing" or "thirsty" or "lost" =>
                GetEquippedResist(state.EquippedArmor, conditionId, balance)
                + GetBestPackResist(conditionId, state, balance, 2),
            "irradiated" or "lattice_sickness" =>
                GetBestPackResist(conditionId, state, balance, 1),
            _ => 0,
        };
    }

    static int GetEquippedResist(ItemInstance? slot, string conditionId, BalanceData balance)
    {
        if (slot == null) return 0;
        if (balance.Items.TryGetValue(slot.DefId, out var def)
            && def.ResistModifiers.TryGetValue(conditionId, out var bonus))
            return bonus;
        return 0;
    }

    static int GetBestPackResist(string conditionId, PlayerState state, BalanceData balance, int count)
    {
        var bonuses = new List<int>();
        foreach (var item in state.Pack)
        {
            if (!balance.Items.TryGetValue(item.DefId, out var def)) continue;
            if (def.Type != ItemType.Tool) continue;
            if (def.ResistModifiers.TryGetValue(conditionId, out var bonus) && bonus > 0)
                bonuses.Add(bonus);
        }
        bonuses.Sort((a, b) => b.CompareTo(a));
        int total = 0;
        for (int i = 0; i < Math.Min(count, bonuses.Count); i++)
            total += bonuses[i];
        return total;
    }
}
