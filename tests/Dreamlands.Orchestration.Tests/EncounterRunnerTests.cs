using Dreamlands.Encounter;
using Dreamlands.Game;
using Dreamlands.Orchestration;

namespace Dreamlands.Orchestration.Tests;

public class EncounterRunnerTests
{
    static Encounter.Encounter SimpleEncounter(string id = "test_enc", string category = "plains/tier1", string[]? mechanics = null) =>
        new()
        {
            Id = $"{category}/{id}",
            Category = category,
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

        Assert.Contains("plains/tier1/unique_enc", session.Player.UsedEncounterIds);
    }

    [Fact]
    public void Begin_ReturnsGatedChoices()
    {
        var session = Helpers.MakeSession();
        var enc = SimpleEncounter();
        var step = EncounterRunner.Begin(session, enc);

        Assert.Single(step.GatedChoices);
        Assert.Equal("Continue", step.GatedChoices[0].Choice.OptionText);
        Assert.False(step.GatedChoices[0].Locked);
    }

    [Fact]
    public void Begin_LockedChoice_MarkedAsLocked()
    {
        var session = Helpers.MakeSession();
        var enc = new Encounter.Encounter
        {
            Id = "test", Category = "test", Title = "Test", Body = "Test body.",
            Choices = new List<Choice>
            {
                new() { OptionText = "Open", Single = new SingleOutcome { Part = new OutcomePart { Text = "Ok." } } },
                new() { OptionText = "Secret", Requires = "tag knows_secret", Single = new SingleOutcome { Part = new OutcomePart { Text = "Hidden." } } },
            }
        };
        var step = EncounterRunner.Begin(session, enc);

        Assert.Equal(2, step.GatedChoices.Count);
        Assert.False(step.GatedChoices[0].Locked);
        Assert.True(step.GatedChoices[1].Locked);
        Assert.Equal(1, step.GatedChoices[1].OriginalIndex);
    }

    [Fact]
    public void Choose_SingleOutcome_ReturnsShowOutcome()
    {
        var session = Helpers.MakeSession();
        var enc = SimpleEncounter();
        var begin = EncounterRunner.Begin(session, enc);

        var result = EncounterRunner.Choose(session, begin.GatedChoices[0].Choice);

        Assert.IsType<EncounterStep.ShowOutcome>(result);
    }

    [Fact]
    public void Choose_Navigation_ReturnsFinishedWithNavigateId()
    {
        var session = Helpers.MakeSession();
        var enc = SimpleEncounter(mechanics: new[] { "open next_room" });
        var begin = EncounterRunner.Begin(session, enc);

        var result = EncounterRunner.Choose(session, begin.GatedChoices[0].Choice);

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

        var result = EncounterRunner.Choose(session, begin.GatedChoices[0].Choice);

        var finished = Assert.IsType<EncounterStep.Finished>(result);
        Assert.Equal(FinishReason.DungeonFinished, finished.Reason);
        Assert.Equal(SessionMode.Exploring, session.Mode);
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
    public void Choose_Repool_RemovesFromUsedEncounterIds()
    {
        var session = Helpers.MakeSession();
        var enc = SimpleEncounter("repool_enc", mechanics: new[] { "repool" });
        EncounterRunner.Begin(session, enc);

        Assert.Contains("plains/tier1/repool_enc", session.Player.UsedEncounterIds);

        EncounterRunner.Choose(session, enc.Choices[0]);

        Assert.DoesNotContain("plains/tier1/repool_enc", session.Player.UsedEncounterIds);
    }

}
