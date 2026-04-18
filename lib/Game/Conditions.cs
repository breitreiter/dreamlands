using Dreamlands.Rules;

namespace Dreamlands.Game;

/// <summary>
/// Evaluates condition expressions against player state.
/// Supports compound expressions: &&, ||, and ! prefix negation on stateless conditions.
/// check and meets cannot appear in compound expressions or be negated.
/// </summary>
public static class Conditions
{
    public static bool Evaluate(string condition, PlayerState state, BalanceData balance, Random rng)
    {
        var tokens = ActionVerb.Tokenize(condition);
        if (tokens.Count == 0) return false;
        int pos = 0;
        return EvaluateOr(tokens, ref pos, state, balance, rng);
    }

    static bool EvaluateOr(List<string> tokens, ref int pos, PlayerState state, BalanceData balance, Random rng)
    {
        var result = EvaluateAnd(tokens, ref pos, state, balance, rng);
        while (pos < tokens.Count && tokens[pos] == "||")
        {
            pos++;
            var right = EvaluateAnd(tokens, ref pos, state, balance, rng);
            result = result | right;
        }
        return result;
    }

    static bool EvaluateAnd(List<string> tokens, ref int pos, PlayerState state, BalanceData balance, Random rng)
    {
        var result = EvaluateAtom(tokens, ref pos, state, balance, rng);
        while (pos < tokens.Count && tokens[pos] == "&&")
        {
            pos++;
            var right = EvaluateAtom(tokens, ref pos, state, balance, rng);
            result = result & right;
        }
        return result;
    }

    static bool EvaluateAtom(List<string> tokens, ref int pos, PlayerState state, BalanceData balance, Random rng)
    {
        if (pos >= tokens.Count) return false;

        var token = tokens[pos];
        var negated = token[0] == '!';
        var verbName = negated ? token[1..] : token;
        pos++;

        var result = verbName switch
        {
            "check"   => EvaluateCheck(tokens, ref pos, state, balance, rng),
            "has"     => pos < tokens.Count ? EvaluateHas(tokens[pos++], state) : false,
            "tag"     => pos < tokens.Count ? EvaluateTag(tokens[pos++], state) : false,
            "meets"   => EvaluateMeets(tokens, ref pos, state, balance),
            "quality" => EvaluateQuality(tokens, ref pos, state),
            _         => false,
        };

        return negated ? !result : result;
    }

    static bool EvaluateCheck(List<string> tokens, ref int pos, PlayerState state, BalanceData balance, Random rng)
    {
        if (pos + 1 > tokens.Count) return false;
        var skill = Skills.FromScriptName(tokens[pos++]);
        var difficulty = Difficulties.FromScriptName(tokens[pos++]);
        if (skill == null || difficulty == null) return false;
        return SkillChecks.Roll(skill.Value, difficulty.Value, state, balance, rng).Passed;
    }

    static bool EvaluateHas(string itemId, PlayerState state) =>
        state.Pack.Any(i => i.DefId == itemId) || state.Haversack.Any(i => i.DefId == itemId);

    static bool EvaluateTag(string tagId, PlayerState state) =>
        state.Tags.Contains(tagId);

    static bool EvaluateMeets(List<string> tokens, ref int pos, PlayerState state, BalanceData balance)
    {
        if (pos + 1 > tokens.Count) return false;
        var skill = Skills.FromScriptName(tokens[pos++]);
        if (skill == null || !int.TryParse(tokens[pos++], out var target)) return false;
        var skillLevel = state.Skills.GetValueOrDefault(skill.Value);
        var itemBonus = SkillChecks.GetItemBonus(skill.Value, state, balance);
        return skillLevel + itemBonus >= target;
    }

    static bool EvaluateQuality(List<string> tokens, ref int pos, PlayerState state)
    {
        if (pos + 1 > tokens.Count) return false;
        var id = tokens[pos++];
        if (!int.TryParse(tokens[pos++], out var threshold)) return false;
        var value = state.Qualities.GetValueOrDefault(id);
        return threshold >= 0 ? value >= threshold : value <= threshold;
    }
}
