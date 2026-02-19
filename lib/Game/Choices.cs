using Dreamlands.Rules;

namespace Dreamlands.Game;

/// <summary>Result of resolving a choice's outcome branch.</summary>
public record ResolvedChoice(string? Preamble, string Text, IReadOnlyList<string> Mechanics, SkillCheckResult? CheckResult);

/// <summary>Choice filtering (requires-gating) and branch resolution.</summary>
public static class Choices
{
    /// <summary>
    /// Filter an encounter's choices to only those whose Requires conditions are met.
    /// </summary>
    public static List<Encounter.Choice> GetVisible(Encounter.Encounter encounter, PlayerState state, BalanceData balance)
    {
        var visible = new List<Encounter.Choice>();
        foreach (var choice in encounter.Choices)
        {
            if (choice.Requires == null || Conditions.Evaluate(choice.Requires, state, balance, Random.Shared))
                visible.Add(choice);
        }
        return visible;
    }

    /// <summary>
    /// Resolve which branch of a choice applies. For conditional choices, evaluates branches
    /// top-to-bottom; first matching condition wins. For single choices, returns directly.
    /// </summary>
    public static ResolvedChoice Resolve(Encounter.Choice choice, PlayerState state, BalanceData balance, Random rng)
    {
        if (choice.Single != null)
        {
            return new ResolvedChoice(
                null,
                choice.Single.Part.Text,
                choice.Single.Part.Mechanics,
                null);
        }

        if (choice.Conditional != null)
        {
            var preamble = string.IsNullOrEmpty(choice.Conditional.Preamble) ? null : choice.Conditional.Preamble;
            SkillCheckResult? lastCheckResult = null;

            foreach (var branch in choice.Conditional.Branches)
            {
                // For check conditions, we need to capture the skill check result
                SkillCheckResult? checkResult = null;
                bool passed;

                var tokens = ActionVerb.Tokenize(branch.Condition);
                if (tokens.Count >= 3 && tokens[0] == "check")
                {
                    var skill = Skills.FromScriptName(tokens[1]);
                    var difficulty = Difficulties.FromScriptName(tokens[2]);
                    if (skill != null && difficulty != null)
                    {
                        checkResult = SkillChecks.Roll(skill.Value, difficulty.Value, state, balance, rng);
                        lastCheckResult = checkResult;
                        passed = checkResult.Passed;
                    }
                    else
                    {
                        passed = false;
                    }
                }
                else
                {
                    passed = Conditions.Evaluate(branch.Condition, state, balance, rng);
                }

                if (passed)
                {
                    return new ResolvedChoice(
                        preamble,
                        branch.Outcome.Text,
                        branch.Outcome.Mechanics,
                        checkResult);
                }
            }

            // No branch matched — use fallback (preserve last check result so player sees the failed roll)
            if (choice.Conditional.Fallback != null)
            {
                return new ResolvedChoice(
                    preamble,
                    choice.Conditional.Fallback.Text,
                    choice.Conditional.Fallback.Mechanics,
                    lastCheckResult);
            }

            // No fallback either — empty result
            return new ResolvedChoice(preamble, "", [], lastCheckResult);
        }

        return new ResolvedChoice(null, "", [], null);
    }
}
