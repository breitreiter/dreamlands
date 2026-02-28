using Dreamlands.Encounter;
using Dreamlands.Game;
using Dreamlands.Orchestration;

namespace Dreamlands.Orchestration.Tests;

public class EncounterRunnerTests
{
    static Encounter.Encounter SimpleEncounter(string id = "test_enc", string[]? mechanics = null) =>
        new()
        {
            Id = id,
            Category = "plains/tier1",
            Title = "Test",
            Body = "Test body.",
            Choices = new List<Choice>
            {
                new()
                {
                    OptionText = "Continue",
                    Single = new SingleOutcome
                    {
                        Part = new OutcomePart
                        {
                            Text = "You continue.",
                            Mechanics = mechanics ?? Array.Empty<string>()
                        }
                    }
                }
            }
        };

    [Fact]
    public void Begin_SetsMode_InEncounter()
    {
        var session = Helpers.MakeSession();
        var enc = SimpleEncounter();
        EncounterRunner.Begin(session, enc);

        Assert.Equal(SessionMode.InEncounter, session.Mode);
    }

    [Fact]
    public void Begin_AddsToUsedEncounterIds()
    {
        var session = Helpers.MakeSession();
        var enc = SimpleEncounter("unique_enc");
        EncounterRunner.Begin(session, enc);

        Assert.Contains("unique_enc", session.Player.UsedEncounterIds);
    }

    [Fact]
    public void Begin_ReturnsVisibleChoices()
    {
        var session = Helpers.MakeSession();
        var enc = SimpleEncounter();
        var step = EncounterRunner.Begin(session, enc);

        Assert.Single(step.VisibleChoices);
        Assert.Equal("Continue", step.VisibleChoices[0].OptionText);
    }

    [Fact]
    public void Choose_SingleOutcome_ReturnsShowOutcome()
    {
        var session = Helpers.MakeSession();
        var enc = SimpleEncounter();
        var begin = EncounterRunner.Begin(session, enc);

        var result = EncounterRunner.Choose(session, begin.VisibleChoices[0]);

        Assert.IsType<EncounterStep.ShowOutcome>(result);
    }

    [Fact]
    public void Choose_Navigation_ReturnsFinishedWithNavigateId()
    {
        var session = Helpers.MakeSession();
        var enc = SimpleEncounter(mechanics: new[] { "open next_room" });
        var begin = EncounterRunner.Begin(session, enc);

        var result = EncounterRunner.Choose(session, begin.VisibleChoices[0]);

        var finished = Assert.IsType<EncounterStep.Finished>(result);
        Assert.Equal(FinishReason.NavigatedTo, finished.Reason);
        Assert.Equal("next_room", finished.NavigateToId);
    }

    [Fact]
    public void Choose_DungeonFinished_SetsExploringMode()
    {
        var session = Helpers.MakeSession();
        var enc = SimpleEncounter(mechanics: new[] { "finish_dungeon" });
        var begin = EncounterRunner.Begin(session, enc);

        var result = EncounterRunner.Choose(session, begin.VisibleChoices[0]);

        var finished = Assert.IsType<EncounterStep.Finished>(result);
        Assert.Equal(FinishReason.DungeonFinished, finished.Reason);
        Assert.Equal(SessionMode.Exploring, session.Mode);
    }

    [Fact]
    public void Choose_PlayerDied_SetsGameOverMode()
    {
        var session = Helpers.MakeSession();
        session.Player.Health = 1;
        var enc = SimpleEncounter(mechanics: new[] { "damage_health small" });
        var begin = EncounterRunner.Begin(session, enc);

        var result = EncounterRunner.Choose(session, begin.VisibleChoices[0]);

        var finished = Assert.IsType<EncounterStep.Finished>(result);
        Assert.Equal(FinishReason.PlayerDied, finished.Reason);
        Assert.Equal(SessionMode.GameOver, session.Mode);
    }

    [Fact]
    public void EndEncounter_ResetsMode()
    {
        var session = Helpers.MakeSession();
        var enc = SimpleEncounter();
        EncounterRunner.Begin(session, enc);

        EncounterRunner.EndEncounter(session);

        Assert.Equal(SessionMode.Exploring, session.Mode);
        Assert.Null(session.CurrentEncounter);
    }

    [Fact]
    public void EndEncounter_SetsSkipEncounterTrigger()
    {
        var session = Helpers.MakeSession();
        var enc = SimpleEncounter();
        EncounterRunner.Begin(session, enc);

        EncounterRunner.EndEncounter(session);

        Assert.True(session.SkipEncounterTrigger);
    }
}
