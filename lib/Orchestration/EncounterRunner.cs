using Dreamlands.Game;
using Dreamlands.Rules;
using ArcRewardSlot = Dreamlands.Rules.ArcRewardSlot;

namespace Dreamlands.Orchestration;

public abstract record EncounterStep
{
    public record ShowEncounter(Encounter.Encounter Encounter, List<GatedChoice> GatedChoices) : EncounterStep;
    public record ShowOutcome(ResolvedChoice Resolved, List<MechanicResult> Results) : EncounterStep;
    public record Finished(FinishReason Reason, string? NavigateToId = null, ShowOutcome? Outcome = null) : EncounterStep;

    /// <summary>
    /// Emitted when a picker-check branch is the next thing to resolve.
    /// The client renders the 3-approach picker UI from the Approaches list
    /// and calls back with the chosen approach id via EncounterRunner.Pick().
    /// </summary>
    public record AwaitApproach(
        Encounter.Encounter Encounter,
        int ChoiceIndex,
        Skill Skill,
        string CorrectId,
        string WrongId,
        string? Preamble,
        IReadOnlyList<Approach> Approaches
    ) : EncounterStep;

    /// <summary>
    /// Emitted after mechanics resolve and PendingLevels > 0.
    /// The client renders the tableau reward picker; the player calls PickReward().
    /// </summary>
    public record AwaitTableauPick(
        IReadOnlyList<ArcRewardSlot> AvailableSlots,
        int PendingLevels,
        ShowOutcome? Outcome
    ) : EncounterStep;
}

public enum FinishReason { Completed, NavigatedTo, DungeonFinished, DungeonFled, PlayerDied }

public static class EncounterRunner
{
    public static EncounterStep.ShowEncounter Begin(GameSession session, Encounter.Encounter encounter)
    {
        session.Mode = SessionMode.InEncounter;
        session.CurrentEncounter = encounter;
        session.Player.CurrentEncounterId = encounter.Id;
        session.Player.UsedEncounterIds.Add(encounter.Id);
        var gated = Choices.GetAllWithLockState(encounter, session.Player, session.Balance);
        return new EncounterStep.ShowEncounter(encounter, gated);
    }

    /// <summary>
    /// Process the player's top-level choice selection.
    /// If the choice ends in a picker-check branch, emits AwaitApproach (suspend).
    /// For Untrained players, the pre-roll happens here before the picker is offered.
    /// </summary>
    public static EncounterStep Choose(GameSession session, Encounter.Choice choice)
    {
        // Determine index by reference equality scan (IReadOnlyList has no IndexOf)
        int choiceIndex = 0;
        if (session.CurrentEncounter != null)
        {
            for (int i = 0; i < session.CurrentEncounter.Choices.Count; i++)
            {
                if (ReferenceEquals(session.CurrentEncounter.Choices[i], choice)) { choiceIndex = i; break; }
            }
        }

        var resolved = Choices.Resolve(choice, session.Player, session.Balance, session.Rng, out var pickerPending);

        if (pickerPending != null)
        {
            // A picker-check branch is the terminal branch; no static branch matched.
            return EnterPicker(session, pickerPending, choiceIndex, choice);
        }

        // Static resolution — apply mechanics and return.
        return FinishResolved(session, resolved!);
    }

    /// <summary>
    /// Resume a suspended picker by applying the player's approach choice.
    /// Reads persisted ActivePickerCheck from PlayerState for closed-tab resilience.
    /// </summary>
    public static EncounterStep Pick(GameSession session, string approachId)
    {
        var check = session.Player.ActivePickerCheck;
        if (check == null)
            throw new InvalidOperationException("No active picker check to resume.");

        var encounter = session.CurrentEncounter;
        if (encounter == null)
            throw new InvalidOperationException("No active encounter for picker resume.");

        var choice = encounter.Choices[check.ChoiceIndex];

        // If PreRollPassed == true the Untrained player passed their pre-roll; treat as Trained.
        var effectiveTier = check.PreRollPassed == true
            ? SkillTier.Trained
            : session.Player.Skills.GetValueOrDefault(check.Skill);

        var (outcome, connector) = SkillResolution.ResolvePicker(
            effectiveTier, approachId, check.CorrectId, check.WrongId, session.Rng);

        var connectorText = ConnectorText.Build(check.Skill, approachId, check.CorrectId, check.WrongId, connector);

        // Success body = picker branch Outcome; Fail body = Fallback (@else)
        Encounter.OutcomePart body;
        if (outcome == PickerOutcome.Succeed)
        {
            var pickerBranch = choice.Conditional!.Branches.First(b => b.IsPickerCheck);
            body = pickerBranch.Outcome;
        }
        else
        {
            body = choice.Conditional!.Fallback
                   ?? new Encounter.OutcomePart { Text = "", Mechanics = Array.Empty<string>() };
        }

        // Prepend connector text if present
        var finalText = connectorText != null
            ? $"{connectorText} {body.Text}".TrimEnd()
            : body.Text;

        // Build a SkillCheckResult so the server/UI can display the picker outcome
        var skillCheckResult = new SkillCheckResult(
            outcome == PickerOutcome.Succeed,
            Rolled: (int)effectiveTier,
            Target: (int)SkillTier.Trained,
            Modifier: 0,
            SkillLevel: (int)effectiveTier,
            check.Skill,
            IsMeetsCheck: false);

        var resolved = new ResolvedChoice(check.Preamble, finalText, body.Mechanics, skillCheckResult);

        // Clear picker state before applying mechanics (mechanics may alter pack etc.)
        session.Player.ActivePickerCheck = null;

        return FinishResolved(session, resolved);
    }

    public static void EndEncounter(GameSession session)
    {
        session.Mode = SessionMode.Exploring;
        session.CurrentEncounter = null;
        session.Player.CurrentEncounterId = null;
    }

    /// <summary>
    /// Resume a suspended tableau pick. Validates slotId, applies the reward, and returns the
    /// next step. If PendingLevels is still > 0 after the pick, re-emits AwaitTableauPick.
    /// Once drained, advances to the post-encounter state.
    /// </summary>
    public static EncounterStep PickReward(GameSession session, string slotId, EncounterStep.ShowOutcome? pendingOutcome)
    {
        var slot = Array.Find(ArcRewards.All, s => s.Id == slotId);
        if (slot == null)
            throw new InvalidOperationException($"Unknown reward slot '{slotId}'.");

        var taken = session.Player.ArcRewardsTaken.GetValueOrDefault(slotId);
        if (taken >= slot.Cap)
            throw new InvalidOperationException($"Reward slot '{slotId}' is already at cap.");

        var result = Mechanics.ApplyArcReward(session.Player, slotId);
        if (result == null)
            throw new InvalidOperationException($"ApplyArcReward returned null for '{slotId}'.");

        // If more picks remain, keep the tableau open.
        if (session.Player.PendingLevels > 0)
        {
            var stillAvailable = GetAvailableSlots(session.Player);
            return new EncounterStep.AwaitTableauPick(stillAvailable, session.Player.PendingLevels, pendingOutcome);
        }

        // All picks used — clear tableau persistence and hand back the original outcome.
        session.Player.PendingTableauReturn = null;

        // If the encounter session is still open, end it.
        if (session.CurrentEncounter != null)
        {
            session.Mode = SessionMode.Exploring;
            session.CurrentEncounter = null;
            session.Player.CurrentEncounterId = null;
        }

        return pendingOutcome is not null ? (EncounterStep)pendingOutcome : new EncounterStep.Finished(FinishReason.Completed);
    }

    /// <summary>Slots not yet at cap — what the player can still pick.</summary>
    public static IReadOnlyList<ArcRewardSlot> GetAvailableSlots(PlayerState player) =>
        ArcRewards.All
            .Where(s => player.ArcRewardsTaken.GetValueOrDefault(s.Id) < s.Cap)
            .ToList()
            .AsReadOnly();

    // ── Private helpers ──────────────────────────────────────────────────────

    static EncounterStep EnterPicker(GameSession session, PickerPending pending, int choiceIndex, Encounter.Choice choice)
    {
        var tier = session.Player.Skills.GetValueOrDefault(pending.Skill);
        var approaches = ApproachRoster.GetApproaches(pending.Skill);

        bool? preRollPassed = null;

        if (tier == SkillTier.Untrained)
        {
            // Pre-roll the coinflip now, before showing the picker.
            var preRollWin = session.Rng.Next(2) == 0;
            if (!preRollWin)
            {
                // Pre-roll failed: skip picker entirely, emit the @else (fail) body directly.
                var fallbackBody = choice.Conditional!.Fallback
                    ?? new Encounter.OutcomePart { Text = "", Mechanics = Array.Empty<string>() };
                var failResolved = new ResolvedChoice(pending.Preamble, fallbackBody.Text, fallbackBody.Mechanics, null);
                return FinishResolved(session, failResolved);
            }
            preRollPassed = true;
        }

        // Persist picker state for closed-tab resilience.
        session.Player.ActivePickerCheck = new ActivePickerCheck
        {
            EncounterId  = session.CurrentEncounter!.Id,
            ChoiceIndex  = choiceIndex,
            Skill        = pending.Skill,
            CorrectId    = pending.CorrectId,
            WrongId      = pending.WrongId,
            Preamble     = pending.Preamble,
            PreRollPassed = preRollPassed,
        };

        return new EncounterStep.AwaitApproach(
            session.CurrentEncounter!,
            choiceIndex,
            pending.Skill,
            pending.CorrectId,
            pending.WrongId,
            pending.Preamble,
            approaches);
    }

    static EncounterStep FinishResolved(GameSession session, ResolvedChoice resolved)
    {
        var results = Mechanics.Apply(resolved.Mechanics, session.Player, session.Balance, session.Rng);
        var outcome = new EncounterStep.ShowOutcome(resolved, results);

        // Track whether dungeon-finished/fled fired so we can suspend the tableau
        // *before* we return the finished step, keeping the dungeon exit clean.
        EncounterStep.Finished? pendingFinished = null;

        foreach (var r in results)
        {
            if (r is MechanicResult.Repooled)
                session.Player.UsedEncounterIds.Remove(session.CurrentEncounter!.Id);
            if (r is MechanicResult.Navigation nav)
                return new EncounterStep.Finished(FinishReason.NavigatedTo, nav.EncounterId, Outcome: outcome);
            if (r is MechanicResult.DungeonFinished)
            {
                session.Mode = SessionMode.Exploring;
                session.CurrentEncounter = null;
                session.Player.CurrentEncounterId = null;
                pendingFinished = new EncounterStep.Finished(FinishReason.DungeonFinished, Outcome: outcome);
                break;
            }
            if (r is MechanicResult.DungeonFled)
            {
                session.Mode = SessionMode.Exploring;
                session.CurrentEncounter = null;
                session.Player.CurrentEncounterId = null;
                pendingFinished = new EncounterStep.Finished(FinishReason.DungeonFled, Outcome: outcome);
                break;
            }
        }

        if (pendingFinished == null && session.Player.Health <= 0)
        {
            session.Mode = SessionMode.Exploring;
            session.CurrentEncounter = null;
            session.Player.CurrentEncounterId = null;
            return new EncounterStep.Finished(FinishReason.PlayerDied, Outcome: outcome);
        }

        // If a +add_level fired this turn, suspend on the tableau before continuing.
        if (session.Player.PendingLevels > 0)
        {
            session.Player.PendingTableauReturn = session.Player.CurrentEncounterId;
            var available = GetAvailableSlots(session.Player);
            // Wrap the pending finished step or outcome as the payload to hand back after picks.
            var resumeOutcome = pendingFinished?.Outcome ?? outcome;
            return new EncounterStep.AwaitTableauPick(available, session.Player.PendingLevels, resumeOutcome);
        }

        return pendingFinished ?? (EncounterStep)outcome;
    }
}
