using Dreamlands.Rules;

namespace Dreamlands.Game;

/// <summary>Result of resolving a choice's outcome branch.</summary>
public record ResolvedChoice(string? Preamble, string Text, IReadOnlyList<string> Mechanics, SkillCheckResult? CheckResult);

/// <summary>A choice with its original index and lock state for display.</summary>
public record GatedChoice(Encounter.Choice Choice, int OriginalIndex, bool Locked);

/// <summary>
/// Signals that a picker check branch is pending; no further static resolution is possible.
/// Carries the data the runner needs to emit AwaitApproach.
/// </summary>
public record PickerPending(
    Skill Skill,
    string CorrectId,
    string WrongId,
    string? Preamble);

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
    /// Resolve which branch of a choice applies.
    /// - For Single choices, returns the outcome directly.
    /// - For Conditional choices, walks static branches first (tag/quality/has/meets).
    ///   If a static branch passes, returns it. If the terminal branch is a picker check
    ///   and no static branch passed, returns null (caller checks TryGetPicker for the pending info).
    ///
    /// Returns non-null when resolved. Returns null when a picker branch is pending.
    /// Use <see cref="TryGetPickerPending"/> to distinguish null-for-picker from fallback.
    /// </summary>
    public static ResolvedChoice? Resolve(
        Encounter.Choice choice,
        PlayerState state,
        BalanceData balance,
        Random rng,
        out PickerPending? pickerPending)
    {
        pickerPending = null;

        if (choice.Single != null)
        {
            return new ResolvedChoice(null, choice.Single.Part.Text, choice.Single.Part.Mechanics, null);
        }

        if (choice.Conditional != null)
        {
            var preamble = string.IsNullOrEmpty(choice.Conditional.Preamble) ? null : choice.Conditional.Preamble;
            SkillCheckResult? lastCheckResult = null;

            foreach (var branch in choice.Conditional.Branches)
            {
                // Picker-check terminal branch — don't evaluate as a static predicate
                if (branch.IsPickerCheck)
                {
                    // No static branch matched; this is the terminal picker check
                    var skill = Skills.FromScriptName(branch.PickerSkill!);
                    if (skill != null)
                    {
                        pickerPending = new PickerPending(
                            skill.Value,
                            branch.PickerCorrect!,
                            branch.PickerWrong!,
                            preamble);
                        return null;
                    }
                    // Unknown skill — fall through to fallback
                    break;
                }

                bool passed;
                SkillCheckResult? checkResult = null;

                var tokens = ActionVerb.Tokenize(branch.Condition);
                if (tokens.Count >= 3 && tokens[0] == "meets")
                {
                    var meetSkill = Skills.FromScriptName(tokens[1]);
                    if (meetSkill != null)
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
                            var playerTier = state.Skills.GetValueOrDefault(meetSkill.Value);
                            passed = playerTier >= targetTier.Value;
                            checkResult = new SkillCheckResult(
                                passed, (int)playerTier, (int)targetTier.Value, (int)playerTier,
                                (int)playerTier, meetSkill.Value, IsMeetsCheck: true);
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

            // No branch matched; use fallback
            if (choice.Conditional.Fallback != null)
            {
                return new ResolvedChoice(
                    preamble,
                    choice.Conditional.Fallback.Text,
                    choice.Conditional.Fallback.Mechanics,
                    lastCheckResult);
            }

            return new ResolvedChoice(preamble, "", [], lastCheckResult);
        }

        return new ResolvedChoice(null, "", [], null);
    }

    /// <summary>
    /// Convenience overload that preserves the old call-site signature (pickerPending discarded).
    /// Only safe when the caller is certain the choice has no picker branch (e.g. Single choices,
    /// or after PickerPending has already been handled by the runner).
    /// </summary>
    public static ResolvedChoice Resolve(Encounter.Choice choice, PlayerState state, BalanceData balance, Random rng)
    {
        var resolved = Resolve(choice, state, balance, rng, out _);
        // If resolved is null here, the caller passed a picker-check choice and discarded the pending.
        // Fall back to an empty result so compile-time callers aren't forced to null-check.
        return resolved ?? new ResolvedChoice(null, "", [], null);
    }
}
