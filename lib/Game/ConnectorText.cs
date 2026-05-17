using Dreamlands.Rules;

namespace Dreamlands.Game;

/// <summary>
/// Synthesizes the engine-emitted connector line that precedes the authored success/fail body
/// when the player's pick and outcome diverge.
///
/// Two cells are "direct" (no connector needed):
///   Correct → Succeed: clean success, body speaks for itself.
///   Wrong   → Fail:    direct fail, body speaks for itself.
///
/// Three cells emit a connector:
///   Correct → Fail (Untrained coinflip):
///     "You try to {correct.verb}, but find yourself {wrong.gerund}."
///   Neutral → Succeed (Expert):
///     "You work the angles and the better play is to {correct.verb}."
///   Neutral → Fail (Trained / Untrained-passed-preroll):
///     "Your {neutral.noun} doesn't pan out, and you end up {wrong.gerund}."
/// </summary>
public static class ConnectorText
{
    /// <summary>
    /// Build the connector string for a resolved picker check.
    /// Returns null for the two "direct" cells (Correct→Succeed and Wrong→Fail).
    /// </summary>
    public static string? Build(
        Skill skill,
        string pickId,
        string correctId,
        string wrongId,
        ConnectorKind kind)
    {
        if (kind == ConnectorKind.Direct) return null;

        var correct = ApproachRoster.GetApproach(skill, correctId);
        var wrong   = ApproachRoster.GetApproach(skill, wrongId);
        var neutral = kind is ConnectorKind.NeutralSuccess or ConnectorKind.NeutralFail
            ? ApproachRoster.GetNeutral(skill, correctId, wrongId)
            : null;

        // Guard against bad authored data — fall back to raw ids if lookup fails
        var correctVerb  = correct?.DisplayLabel.ToLowerInvariant() ?? correctId;
        var wrongGerund  = wrong?.Gerund ?? wrongId;
        var neutralNoun  = neutral?.Noun ?? pickId;

        return kind switch
        {
            // Untrained correct pick, coinflip failed
            ConnectorKind.CorrectFailed =>
                $"You try to {correctVerb}, but find yourself {wrongGerund}.",

            // Expert, neutral pick but still succeeds
            ConnectorKind.NeutralSuccess =>
                $"You work the angles and the better play is to {correctVerb}.",

            // Trained (or Untrained with passing preroll), neutral pick fails
            ConnectorKind.NeutralFail =>
                $"Your {neutralNoun} doesn't pan out, and you end up {wrongGerund}.",

            // Direct — already handled above
            _ => null,
        };
    }
}
