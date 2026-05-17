using Dreamlands.Rules;

namespace Dreamlands.Game;

/// <summary>
/// Outcome of a picker-based skill check.
/// </summary>
public enum PickerOutcome { Succeed, Fail }

/// <summary>
/// Describes the narrative bridge between the player's pick and the outcome.
/// The connector is emitted by the engine before the authored success/fail body;
/// two cells (correct→succeed, wrong→fail) are "direct" and need no connector.
/// </summary>
public enum ConnectorKind
{
    /// <summary>Pick matched the expected outcome cleanly — no connector needed.</summary>
    Direct,
    /// <summary>Untrained picked the correct approach but still failed the coinflip.</summary>
    CorrectFailed,
    /// <summary>Expert picked a neutral approach and succeeded on craft alone.</summary>
    NeutralSuccess,
    /// <summary>Trained/Untrained picked a neutral approach and fell short.</summary>
    NeutralFail,
}

/// <summary>
/// Tier-driven skill resolution. Replaces d20 SkillChecks.Roll for encounter checks
/// and ambient condition resist rolls.
/// </summary>
public static class SkillResolution
{
    /// <summary>
    /// Resolve a picker-based skill check.
    ///
    /// Resolution table:
    ///   Expert:   correct → succeed (Direct), neutral → succeed (NeutralSuccess), wrong → fail (Direct)
    ///   Trained:  correct → succeed (Direct), neutral → fail (NeutralFail),       wrong → fail (Direct)
    ///   Untrained:correct → 50% coinflip (succeed=Direct, fail=CorrectFailed),    neutral/wrong → fail (NeutralFail/Direct)
    /// </summary>
    /// <param name="tier">The player's tier in the relevant skill.</param>
    /// <param name="pick">The approach id the player picked.</param>
    /// <param name="correct">The correct approach id for this check.</param>
    /// <param name="wrong">The wrong approach id for this check (the third is neutral).</param>
    /// <param name="rng">Random for the Untrained coinflip.</param>
    public static (PickerOutcome Outcome, ConnectorKind Connector) ResolvePicker(
        SkillTier tier, string pick, string correct, string wrong, Random rng)
    {
        var pickIsCorrect = string.Equals(pick, correct, StringComparison.OrdinalIgnoreCase);
        var pickIsWrong   = string.Equals(pick, wrong,   StringComparison.OrdinalIgnoreCase);

        if (pickIsCorrect)
        {
            return tier switch
            {
                SkillTier.Expert or SkillTier.Trained => (PickerOutcome.Succeed, ConnectorKind.Direct),
                // Untrained: 50% coinflip
                _ => rng.Next(2) == 0
                    ? (PickerOutcome.Succeed, ConnectorKind.Direct)
                    : (PickerOutcome.Fail, ConnectorKind.CorrectFailed),
            };
        }

        if (pickIsWrong)
            return (PickerOutcome.Fail, ConnectorKind.Direct);

        // Neutral pick
        return tier switch
        {
            SkillTier.Expert => (PickerOutcome.Succeed, ConnectorKind.NeutralSuccess),
            _                => (PickerOutcome.Fail,    ConnectorKind.NeutralFail),
        };
    }

    /// <summary>
    /// Passive resist roll for ambient conditions. Used by EndOfDay for travel and serious
    /// conditions.
    ///
    /// Probabilities by tier:
    ///   Untrained = 0%  (never resist)
    ///   Trained   = 40%
    ///   Expert    = 80%
    ///
    /// Skill domain:
    ///   Travel conditions (exhausted, freezing, thirsty, lost) → Skill.Bushcraft
    ///   Serious conditions (injured, poisoned, irradiated, lattice_sickness) → Skill.Cunning
    /// </summary>
    public static bool RollPassiveResist(SkillTier tier, Random rng) =>
        tier switch
        {
            SkillTier.Trained => rng.Next(100) < 40,
            SkillTier.Expert  => rng.Next(100) < 80,
            _                 => false,
        };
}
