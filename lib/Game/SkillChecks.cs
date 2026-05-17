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
/// </summary>
public static class SkillChecks
{
    /// <summary>
    /// Get item bonus for an encounter skill check. Each skill draws from specific gear sources.
    /// Retained for the server mechanics-display panel; not called by the engine internally.
    /// </summary>
    public static int GetItemBonus(Skill skill, PlayerState state, BalanceData balance)
    {
        return skill switch
        {
            Skill.Combat => GetEquippedMod(state.EquippedWeapon, Skill.Combat, balance),
            Skill.Cunning => GetEquippedMod(state.EquippedArmor, Skill.Cunning, balance),
            Skill.Negotiation => GetBestToolBonuses(Skill.Negotiation, state, balance),
            Skill.Bushcraft => GetBestToolBonuses(Skill.Bushcraft, state, balance),
            _ => 0,
        };
    }

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
            "exhausted" => GetEquippedResist(state.EquippedBoots, conditionId, balance)
                         + GetBestPackResist(conditionId, state, balance, 1),
            "freezing" or "thirsty" or "lost" =>
                GetEquippedResist(state.EquippedArmor, conditionId, balance)
                + GetBestPackResist(conditionId, state, balance, 2),
            "irradiated" or "lattice_sickness" =>
                GetBestPackResist(conditionId, state, balance, 1),
            _ => 0,
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

    static int GetEquippedResist(ItemInstance? slot, string conditionId, BalanceData balance)
    {
        if (slot == null) return 0;
        if (balance.Items.TryGetValue(slot.DefId, out var def)
            && def.ResistModifiers.TryGetValue(conditionId, out var bonus))
            return bonus;
        return 0;
    }

    static int GetBestToolBonuses(Skill skill, PlayerState state, BalanceData balance)
    {
        int best = 0, secondBest = 0;
        var seen = new HashSet<string>();

        foreach (var item in state.Pack)
        {
            if (!seen.Add(item.DefId)) continue;
            if (!balance.Items.TryGetValue(item.DefId, out var def)) continue;
            if (def.Type != ItemType.Tool) continue;
            if (!def.SkillModifiers.TryGetValue(skill, out var mod) || mod <= 0) continue;

            if (mod >= best) { secondBest = best; best = mod; }
            else if (mod > secondBest) { secondBest = mod; }
        }

        return best + secondBest;
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
