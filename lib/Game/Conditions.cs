using Dreamlands.Rules;

namespace Dreamlands.Game;

/// <summary>
/// Evaluates condition expressions against player state.
/// Supports compound expressions: &amp;&amp;, ||, and ! prefix negation on stateless conditions.
/// meets cannot appear in compound expressions or be negated.
///
/// Surviving condition forms: has | tag | quality | meets
/// The old "check" predicate (d20 roll) is removed — Phase 5 will wire picker resolution.
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
            "has"     => pos < tokens.Count ? EvaluateHas(tokens[pos++], state) : false,
            "tag"     => pos < tokens.Count ? EvaluateTag(tokens[pos++], state) : false,
            "meets"   => EvaluateMeets(tokens, ref pos, state),
            "quality" => EvaluateQuality(tokens, ref pos, state),
            _         => false,
        };

        return negated ? !result : result;
    }

    static bool EvaluateHas(string itemId, PlayerState state) =>
        state.Pack.Any(i => i.DefId == itemId);

    static bool EvaluateTag(string tagId, PlayerState state) =>
        state.Tags.Contains(tagId);

    /// <summary>
    /// Evaluate "meets &lt;skill&gt; &lt;tier&gt;" — tier is Untrained|Trained|Expert (case-insensitive).
    /// True when the player's skill tier is >= the target tier.
    /// </summary>
    static bool EvaluateMeets(List<string> tokens, ref int pos, PlayerState state)
    {
        if (pos + 1 >= tokens.Count) return false;
        var skillToken = tokens[pos++];
        var tierToken  = tokens[pos++];

        var skill = Skills.FromScriptName(skillToken);
        if (skill == null) return false;

        var targetTier = tierToken.ToLowerInvariant() switch
        {
            "untrained" => (SkillTier?)SkillTier.Untrained,
            "trained"   => SkillTier.Trained,
            "expert"    => SkillTier.Expert,
            // Legacy int form — accept for backward compat during Phase 4 sweep
            var s when int.TryParse(s, out var n) => (SkillTier)Math.Clamp(n, 0, 2),
            _ => null,
        };

        if (targetTier == null) return false;

        var playerTier = state.Skills.GetValueOrDefault(skill.Value);
        return playerTier >= targetTier.Value;
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
