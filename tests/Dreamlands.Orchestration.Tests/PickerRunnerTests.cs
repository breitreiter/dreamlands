using Dreamlands.Encounter;
using Dreamlands.Game;
using Dreamlands.Orchestration;
using Dreamlands.Rules;

namespace Dreamlands.Orchestration.Tests;

/// <summary>
/// Phase 5 tests: picker check suspend/resume through EncounterRunner.
///
/// Skill used: Negotiation — correct: "reason", wrong: "threaten", neutral: "flatter".
/// The picker branch success body is "You convinced them." and mechanics = [].
/// The @else (fail) body is "They turn you away." and mechanics = [].
/// </summary>
public class PickerRunnerTests
{
    // ── Fixture builders ──────────────────────────────────────────────────────

    static Encounter.Encounter MakePickerEncounter(
        string choiceLabel = "Negotiate",
        string? tagCondition = null)
    {
        var branches = new List<ConditionalBranch>();

        // Optional static branch preceding the picker (for "static branch wins" test)
        if (tagCondition != null)
        {
            branches.Add(new ConditionalBranch
            {
                Condition = tagCondition,
                Outcome = new OutcomePart { Text = "Static branch fired.", Mechanics = Array.Empty<string>() },
            });
        }

        // Terminal picker-check branch
        branches.Add(new ConditionalBranch
        {
            Condition = "check negotiation correct:reason wrong:threaten",
            PickerSkill = "negotiation",
            PickerCorrect = "reason",
            PickerWrong = "threaten",
            Outcome = new OutcomePart { Text = "You convinced them.", Mechanics = Array.Empty<string>() },
        });

        return new Encounter.Encounter
        {
            Id = "test/picker_enc",
            Category = "test",
            Title = "Encounter",
            Body = "A tense situation.",
            Choices = new List<Choice>
            {
                new()
                {
                    OptionText = choiceLabel,
                    Conditional = new ConditionalOutcome
                    {
                        Preamble = "You size up the situation.",
                        Branches = branches,
                        Fallback = new OutcomePart { Text = "They turn you away.", Mechanics = Array.Empty<string>() },
                    }
                }
            }
        };
    }

    static GameSession MakeSession(SkillTier negotiationTier, int rngSeed = 0)
    {
        var session = Helpers.MakeSession();
        session.Player.Skills[Skill.Negotiation] = negotiationTier;
        // Replace the seeded RNG with a known seed for determinism
        return new GameSession(
            session.Player,
            session.Map,
            session.Bundle,
            session.Balance,
            new Random(rngSeed));
    }

    // ── Expert / Trained: AwaitApproach emitted ────────────────────────────────

    [Theory]
    [InlineData(SkillTier.Expert)]
    [InlineData(SkillTier.Trained)]
    public void Choose_WithPickerBranch_EmitsAwaitApproach(SkillTier tier)
    {
        var session = MakeSession(tier);
        var enc = MakePickerEncounter();
        EncounterRunner.Begin(session, enc);

        var step = EncounterRunner.Choose(session, enc.Choices[0]);

        var await_ = Assert.IsType<EncounterStep.AwaitApproach>(step);
        Assert.Equal(Skill.Negotiation, await_.Skill);
        Assert.Equal("reason",   await_.CorrectId);
        Assert.Equal("threaten", await_.WrongId);
        Assert.Equal(3, await_.Approaches.Count);
        Assert.Equal("You size up the situation.", await_.Preamble);
    }

    [Theory]
    [InlineData(SkillTier.Expert)]
    [InlineData(SkillTier.Trained)]
    public void Choose_WithPickerBranch_PersistsActivePickerCheck(SkillTier tier)
    {
        var session = MakeSession(tier);
        var enc = MakePickerEncounter();
        EncounterRunner.Begin(session, enc);

        EncounterRunner.Choose(session, enc.Choices[0]);

        var check = session.Player.ActivePickerCheck;
        Assert.NotNull(check);
        Assert.Equal(Skill.Negotiation, check.Skill);
        Assert.Equal("reason",   check.CorrectId);
        Assert.Equal("threaten", check.WrongId);
        Assert.Null(check.PreRollPassed); // null = Expert/Trained, no pre-roll needed
    }

    // ── Trained: correct succeeds (Direct), no connector ──────────────────────

    [Fact]
    public void Pick_Trained_CorrectApproach_Succeeds_DirectConnector()
    {
        var session = MakeSession(SkillTier.Trained);
        var enc = MakePickerEncounter();
        EncounterRunner.Begin(session, enc);
        EncounterRunner.Choose(session, enc.Choices[0]);

        var step = EncounterRunner.Pick(session, "reason"); // correct

        var outcome = Assert.IsType<EncounterStep.ShowOutcome>(step);
        Assert.Contains("You convinced them.", outcome.Resolved.Text);
        // No connector prefix — direct success
        Assert.DoesNotContain("You try to", outcome.Resolved.Text);
        Assert.True(outcome.Resolved.CheckResult!.Passed);
    }

    // ── Trained: neutral pick fails, NeutralFail connector ───────────────────

    [Fact]
    public void Pick_Trained_NeutralApproach_Fails_NeutralFailConnector()
    {
        var session = MakeSession(SkillTier.Trained);
        var enc = MakePickerEncounter();
        EncounterRunner.Begin(session, enc);
        EncounterRunner.Choose(session, enc.Choices[0]);

        var step = EncounterRunner.Pick(session, "flatter"); // neutral

        var outcome = Assert.IsType<EncounterStep.ShowOutcome>(step);
        // @else body
        Assert.Contains("They turn you away.", outcome.Resolved.Text);
        // NeutralFail connector prepended
        Assert.StartsWith("Your", outcome.Resolved.Text);
        Assert.False(outcome.Resolved.CheckResult!.Passed);
    }

    // ── Trained: wrong pick fails (Direct), no connector ─────────────────────

    [Fact]
    public void Pick_Trained_WrongApproach_Fails_DirectConnector()
    {
        var session = MakeSession(SkillTier.Trained);
        var enc = MakePickerEncounter();
        EncounterRunner.Begin(session, enc);
        EncounterRunner.Choose(session, enc.Choices[0]);

        var step = EncounterRunner.Pick(session, "threaten"); // wrong

        var outcome = Assert.IsType<EncounterStep.ShowOutcome>(step);
        Assert.Contains("They turn you away.", outcome.Resolved.Text);
        // Direct fail — no connector
        Assert.DoesNotContain("doesn't pan out", outcome.Resolved.Text);
        Assert.DoesNotContain("You try to", outcome.Resolved.Text);
        Assert.False(outcome.Resolved.CheckResult!.Passed);
    }

    // ── Expert: neutral pick succeeds, NeutralSuccess connector ───────────────

    [Fact]
    public void Pick_Expert_NeutralApproach_Succeeds_NeutralSuccessConnector()
    {
        var session = MakeSession(SkillTier.Expert);
        var enc = MakePickerEncounter();
        EncounterRunner.Begin(session, enc);
        EncounterRunner.Choose(session, enc.Choices[0]);

        var step = EncounterRunner.Pick(session, "flatter"); // neutral for Expert

        var outcome = Assert.IsType<EncounterStep.ShowOutcome>(step);
        Assert.Contains("You convinced them.", outcome.Resolved.Text);
        Assert.Contains("better play", outcome.Resolved.Text); // NeutralSuccess connector
        Assert.True(outcome.Resolved.CheckResult!.Passed);
    }

    // ── ActivePickerCheck cleared after Pick ──────────────────────────────────

    [Fact]
    public void Pick_ClearsActivePickerCheck()
    {
        var session = MakeSession(SkillTier.Trained);
        var enc = MakePickerEncounter();
        EncounterRunner.Begin(session, enc);
        EncounterRunner.Choose(session, enc.Choices[0]);

        Assert.NotNull(session.Player.ActivePickerCheck);

        EncounterRunner.Pick(session, "reason");

        Assert.Null(session.Player.ActivePickerCheck);
    }

    // ── Untrained pre-roll fails: NO AwaitApproach emitted ────────────────────

    [Fact]
    public void Choose_Untrained_PreRollFails_ReturnsFailBodyDirectly()
    {
        // RNG seed 0: first Next(2) for Untrained path — test multiple seeds to hit both outcomes.
        // We need a seed where Next(2)==1 (fail). Scan for one.
        int failSeed = -1;
        for (int s = 0; s < 100; s++)
        {
            if (new Random(s).Next(2) != 0) { failSeed = s; break; }
        }
        Assert.True(failSeed >= 0, "Could not find a failing seed — unexpected RNG behaviour");

        var session = MakeSession(SkillTier.Untrained, failSeed);
        var enc = MakePickerEncounter();
        EncounterRunner.Begin(session, enc);

        var step = EncounterRunner.Choose(session, enc.Choices[0]);

        // Must be ShowOutcome (fail body directly), NOT AwaitApproach
        var outcome = Assert.IsType<EncounterStep.ShowOutcome>(step);
        Assert.Contains("They turn you away.", outcome.Resolved.Text);
        // No ActivePickerCheck written
        Assert.Null(session.Player.ActivePickerCheck);
    }

    // ── Untrained pre-roll passes: AwaitApproach emitted, PreRollPassed=true ───

    [Fact]
    public void Choose_Untrained_PreRollPasses_EmitsAwaitApproach_WithPreRollPassed()
    {
        // Find seed where Next(2)==0 (win)
        int winSeed = -1;
        for (int s = 0; s < 100; s++)
        {
            if (new Random(s).Next(2) == 0) { winSeed = s; break; }
        }
        Assert.True(winSeed >= 0);

        var session = MakeSession(SkillTier.Untrained, winSeed);
        var enc = MakePickerEncounter();
        EncounterRunner.Begin(session, enc);

        var step = EncounterRunner.Choose(session, enc.Choices[0]);

        var await_ = Assert.IsType<EncounterStep.AwaitApproach>(step);
        Assert.Equal(Skill.Negotiation, await_.Skill);
        Assert.NotNull(session.Player.ActivePickerCheck);
        Assert.Equal(true, session.Player.ActivePickerCheck!.PreRollPassed);
    }

    // ── Untrained pre-roll passed: correct succeeds (Trained-tier behavior) ───

    [Fact]
    public void Pick_Untrained_PreRollPassed_CorrectApproach_Succeeds()
    {
        int winSeed = -1;
        for (int s = 0; s < 100; s++)
        {
            if (new Random(s).Next(2) == 0) { winSeed = s; break; }
        }

        var session = MakeSession(SkillTier.Untrained, winSeed);
        var enc = MakePickerEncounter();
        EncounterRunner.Begin(session, enc);
        EncounterRunner.Choose(session, enc.Choices[0]); // emits AwaitApproach

        var step = EncounterRunner.Pick(session, "reason"); // correct

        var outcome = Assert.IsType<EncounterStep.ShowOutcome>(step);
        Assert.Contains("You convinced them.", outcome.Resolved.Text);
        Assert.True(outcome.Resolved.CheckResult!.Passed);
    }

    // ── Untrained pre-roll passed: neutral pick fails (Trained-tier behavior) ──

    [Fact]
    public void Pick_Untrained_PreRollPassed_NeutralApproach_Fails()
    {
        int winSeed = -1;
        for (int s = 0; s < 100; s++)
        {
            if (new Random(s).Next(2) == 0) { winSeed = s; break; }
        }

        var session = MakeSession(SkillTier.Untrained, winSeed);
        var enc = MakePickerEncounter();
        EncounterRunner.Begin(session, enc);
        EncounterRunner.Choose(session, enc.Choices[0]);

        var step = EncounterRunner.Pick(session, "flatter"); // neutral

        var outcome = Assert.IsType<EncounterStep.ShowOutcome>(step);
        Assert.Contains("They turn you away.", outcome.Resolved.Text);
        Assert.False(outcome.Resolved.CheckResult!.Passed);
    }

    // ── Static branch upstream of picker wins ────────────────────────────────

    [Fact]
    public void Choose_StaticBranchBeforePicker_WinsWhenConditionMet()
    {
        var session = MakeSession(SkillTier.Trained);
        session.Player.Tags.Add("guild_member");
        var enc = MakePickerEncounter(tagCondition: "tag guild_member");
        EncounterRunner.Begin(session, enc);

        var step = EncounterRunner.Choose(session, enc.Choices[0]);

        // Static branch matched — no picker emitted
        var outcome = Assert.IsType<EncounterStep.ShowOutcome>(step);
        Assert.Contains("Static branch fired.", outcome.Resolved.Text);
        Assert.Null(session.Player.ActivePickerCheck);
    }

    [Fact]
    public void Choose_StaticBranchBeforePicker_FallsThroughToPickerWhenUnmet()
    {
        var session = MakeSession(SkillTier.Trained);
        // tag NOT set — static branch fails, picker should fire
        var enc = MakePickerEncounter(tagCondition: "tag guild_member");
        EncounterRunner.Begin(session, enc);

        var step = EncounterRunner.Choose(session, enc.Choices[0]);

        Assert.IsType<EncounterStep.AwaitApproach>(step);
    }

    // ── Resume from ActivePickerCheck (closed-tab resilience) ─────────────────

    [Fact]
    public void Pick_ResumesFromActivePickerCheck_CorrectlyResolves()
    {
        var session = MakeSession(SkillTier.Trained);
        var enc = MakePickerEncounter();
        EncounterRunner.Begin(session, enc);
        EncounterRunner.Choose(session, enc.Choices[0]); // suspend → saves check

        // Simulate a fresh session with the same persisted state (picker check is already on player)
        var freshSession = new GameSession(
            session.Player,
            session.Map,
            session.Bundle,
            session.Balance,
            new Random(99));
        freshSession.Mode = SessionMode.InEncounter;
        freshSession.CurrentEncounter = enc;

        var step = EncounterRunner.Pick(freshSession, "reason");

        var outcome = Assert.IsType<EncounterStep.ShowOutcome>(step);
        Assert.Contains("You convinced them.", outcome.Resolved.Text);
        Assert.True(outcome.Resolved.CheckResult!.Passed);
    }
}
