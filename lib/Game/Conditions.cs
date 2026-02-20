using Dreamlands.Rules;

namespace Dreamlands.Game;

/// <summary>Evaluates @if condition strings against player state.</summary>
public static class Conditions
{
    /// <summary>
    /// Evaluate a condition string (e.g. "check combat hard", "has torch", "tag dragon_slayer").
    /// </summary>
    public static bool Evaluate(string condition, PlayerState state, BalanceData balance, Random rng)
    {
        var parts = ActionVerb.Tokenize(condition);
        if (parts.Count == 0) return false;

        return parts[0] switch
        {
            "check" when parts.Count >= 3 => EvaluateCheck(parts, state, balance, rng),
            "has" when parts.Count >= 2 => EvaluateHas(parts[1], state),
            "tag" when parts.Count >= 2 => EvaluateTag(parts[1], state),
            _ => false,
        };
    }

    static bool EvaluateCheck(List<string> parts, PlayerState state, BalanceData balance, Random rng)
    {
        var skill = Skills.FromScriptName(parts[1]);
        var difficulty = Difficulties.FromScriptName(parts[2]);
        if (skill == null || difficulty == null) return false;

        var result = SkillChecks.Roll(skill.Value, difficulty.Value, state, balance, rng);
        return result.Passed;
    }

    static bool EvaluateHas(string itemId, PlayerState state) =>
        state.Pack.Any(i => i.DefId == itemId) || state.Haversack.Any(i => i.DefId == itemId);

    static bool EvaluateTag(string tagId, PlayerState state) =>
        state.Tags.Contains(tagId);
}
