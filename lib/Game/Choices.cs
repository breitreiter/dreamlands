using Dreamlands.Rules;

namespace Dreamlands.Game;

/// <summary>Result of resolving a choice's outcome branch.</summary>
public record ResolvedChoice(string? Preamble, string Text, IReadOnlyList<string> Mechanics, SkillCheckResult? CheckResult);

/// <summary>A choice with its original index and lock state for display.</summary>
public record GatedChoice(Encounter.Choice Choice, int OriginalIndex, bool Locked);

/// <summary>Choice filtering (requires-gating) and branch resolution.</summary>
public static class Choices
{
    /// <summary>
    /// Filter an encounter's choices to only those whose Requires conditions are met.
    /// Surviving condition forms in [requires]: has | tag | quality | meets
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
    /// Return all choices with their original index and whether they are locked by an unmet Requires gate.
    /// </summary>
    public static List<GatedChoice> GetAllWithLockState(Encounter.Encounter encounter, PlayerState state, BalanceData balance)
    {
        var result = new List<GatedChoice>();
        for (var i = 0; i < encounter.Choices.Count; i++)
        {
            var choice = encounter.Choices[i];
            var locked = choice.Requires != null && !Conditions.Evaluate(choice.Requires, state, balance, Random.Shared);
            result.Add(new GatedChoice(choice, i, locked));
        }
        return result;
    }

    /// <summary>
    /// Resolve which branch of a choice applies. For conditional choices, evaluates branches
    /// top-to-bottom; first matching condition wins. For single choices, returns directly.
    ///
    /// Supported branch condition forms: has | tag | quality | meets
    /// The old "check" form (d20 roll) is removed — picker resolution lands in Phase 5.
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
                bool passed;
                SkillCheckResult? checkResult = null;

                var tokens = ActionVerb.Tokenize(branch.Condition);
                if (tokens.Count >= 3 && tokens[0] == "meets")
                {
                    var skill = Skills.FromScriptName(tokens[1]);
                    if (skill != null)
                    {
                        var targetTier = tokens[2].ToLowerInvariant() switch
                        {
                            "untrained" => (SkillTier?)SkillTier.Untrained,
                            "trained"   => SkillTier.Trained,
                            "expert"    => SkillTier.Expert,
                            var s when int.TryParse(s, out var n) => (SkillTier)Math.Clamp(n, 0, 2),
                            _ => null,
                        };

                        if (targetTier != null)
                        {
                            var playerTier = state.Skills.GetValueOrDefault(skill.Value);
                            passed = playerTier >= targetTier.Value;
                            // Emit a meets-check result for the UI roll display (IsMeetsCheck = true)
                            checkResult = new SkillCheckResult(
                                passed, (int)playerTier, (int)targetTier.Value, (int)playerTier,
                                (int)playerTier, skill.Value, IsMeetsCheck: true);
                            lastCheckResult = checkResult;
                        }
                        else
                        {
                            passed = false;
                        }
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

            // No branch matched — use fallback (preserve last check result so player sees the result)
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
