using Dreamlands.Encounter;
using Dreamlands.Game;
using Dreamlands.Orchestration;
using Dreamlands.Rules;

namespace Dreamlands.Orchestration.Tests;

public class TableauTests
{
    // ── Helpers ──────────────────────────────────────────────────────────────

    static Encounter.Encounter EncounterWithMechanics(string[] mechanics, string id = "test_enc") =>
        new()
        {
            Id = $"plains/tier1/{id}",
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
                            Mechanics = mechanics,
                        }
                    }
                }
            }
        };

    // ── 1. +add_level increments PendingLevels ────────────────────────────

    [Fact]
    public void AddLevel_Mechanic_IncrementsPendingLevels()
    {
        var session = Helpers.MakeSession();
        Assert.Equal(0, session.Player.PendingLevels);

        Mechanics.Apply(["add_level"], session.Player, BalanceData.Default, new Random(42));

        Assert.Equal(1, session.Player.PendingLevels);
    }

    [Fact]
    public void AddLevel_Mechanic_ReturnsLevelAddedResult()
    {
        var session = Helpers.MakeSession();
        var results = Mechanics.Apply(["add_level"], session.Player, BalanceData.Default, new Random(42));

        var levelAdded = Assert.Single(results.OfType<MechanicResult.LevelAdded>());
        Assert.Equal(1, levelAdded.PendingLevels);
    }

    // ── 2. Resolving a choice with PendingLevels > 0 suspends ────────────

    [Fact]
    public void Choose_WhenAddLevelFired_SuspendsOnAwaitTableauPick()
    {
        var session = Helpers.MakeSession();
        var enc = EncounterWithMechanics(["add_level"]);
        EncounterRunner.Begin(session, enc);

        var result = EncounterRunner.Choose(session, enc.Choices[0]);

        Assert.IsType<EncounterStep.AwaitTableauPick>(result);
    }

    [Fact]
    public void Choose_WhenAddLevelFired_AwaitTableauPickHasAllSlots()
    {
        var session = Helpers.MakeSession();
        var enc = EncounterWithMechanics(["add_level"]);
        EncounterRunner.Begin(session, enc);

        var result = (EncounterStep.AwaitTableauPick)EncounterRunner.Choose(session, enc.Choices[0]);

        Assert.Equal(6, result.AvailableSlots.Count); // all 6 reward slots
        Assert.Equal(1, result.PendingLevels);
    }

    [Fact]
    public void Choose_WhenAddLevelFired_SetsPendingTableauReturn()
    {
        var session = Helpers.MakeSession();
        var enc = EncounterWithMechanics(["add_level"]);
        EncounterRunner.Begin(session, enc);
        EncounterRunner.Choose(session, enc.Choices[0]);

        // PendingTableauReturn should be set to the encounter id
        Assert.NotNull(session.Player.PendingTableauReturn);
    }

    // ── 3. PickReward applies skill bump and decrements PendingLevels ─────

    [Fact]
    public void PickReward_SkillSlot_BumpsSkillTier()
    {
        var session = Helpers.MakeSession();
        Assert.Equal(SkillTier.Untrained, session.Player.Skills.GetValueOrDefault(Skill.Combat));

        session.Player.PendingLevels = 1;
        var enc = EncounterWithMechanics([]); // no mechanics needed; we drive directly
        EncounterRunner.Begin(session, enc);
        // Resolve the choice to get an outcome we can pass as resumeOutcome
        // But PickReward doesn't strictly need one; pass null.
        var step = EncounterRunner.PickReward(session, "combat", null);

        Assert.Equal(SkillTier.Trained, session.Player.Skills.GetValueOrDefault(Skill.Combat));
    }

    [Fact]
    public void PickReward_DecrementsPendingLevels()
    {
        var session = Helpers.MakeSession();
        session.Player.PendingLevels = 2;

        EncounterRunner.PickReward(session, "combat", null);

        Assert.Equal(1, session.Player.PendingLevels);
    }

    [Fact]
    public void PickReward_IncreasesArcRewardsTaken()
    {
        var session = Helpers.MakeSession();
        session.Player.PendingLevels = 1;

        EncounterRunner.PickReward(session, "health", null);

        Assert.Equal(1, session.Player.ArcRewardsTaken.GetValueOrDefault("health"));
    }

    [Fact]
    public void PickReward_HealthSlot_BumpsMaxHealthAndHealth()
    {
        var session = Helpers.MakeSession();
        session.Player.PendingLevels = 1;
        var healthBefore = session.Player.MaxHealth;

        EncounterRunner.PickReward(session, "health", null);

        Assert.Equal(healthBefore + ArcRewards.HealthPerPick, session.Player.MaxHealth);
        Assert.Equal(session.Player.MaxHealth, session.Player.Health); // health also bumped
    }

    [Fact]
    public void PickReward_InventorySlot_BumpsPackCapacity()
    {
        var session = Helpers.MakeSession();
        session.Player.PendingLevels = 1;
        var capacityBefore = session.Player.PackCapacity;

        EncounterRunner.PickReward(session, "inventory", null);

        Assert.Equal(capacityBefore + ArcRewards.InventoryPerPick, session.Player.PackCapacity);
    }

    // ── 4. PickReward at cap is rejected ─────────────────────────────────

    [Fact]
    public void PickReward_AtCap_Throws()
    {
        var session = Helpers.MakeSession();
        session.Player.PendingLevels = 1;
        session.Player.ArcRewardsTaken["combat"] = 2; // already at cap

        Assert.Throws<InvalidOperationException>(() =>
            EncounterRunner.PickReward(session, "combat", null));
    }

    // ── 5. Available slots = those under cap ─────────────────────────────

    [Fact]
    public void GetAvailableSlots_ExcludesAtCap()
    {
        var session = Helpers.MakeSession();
        session.Player.ArcRewardsTaken["combat"] = 2;
        session.Player.ArcRewardsTaken["health"] = 2;

        var available = EncounterRunner.GetAvailableSlots(session.Player);

        Assert.DoesNotContain(available, s => s.Id == "combat");
        Assert.DoesNotContain(available, s => s.Id == "health");
        Assert.Equal(4, available.Count); // 6 - 2 = 4
    }

    // ── 6. Closed-tab resume: PendingLevels > 0 re-emits AwaitTableauPick

    [Fact]
    public void GetAvailableSlots_WithPendingLevels_ReturnsAllUncappedSlots()
    {
        var session = Helpers.MakeSession();
        session.Player.PendingLevels = 1;
        session.Player.PendingTableauReturn = "plains/tier1/some_enc";
        session.Player.ArcRewardsTaken["negotiation"] = 2;

        var available = EncounterRunner.GetAvailableSlots(session.Player);

        // negotiation is capped, so 5 slots remain
        Assert.Equal(5, available.Count);
        Assert.DoesNotContain(available, s => s.Id == "negotiation");
    }

    // ── 7. PickReward with more picks left re-emits AwaitTableauPick ─────

    [Fact]
    public void PickReward_WithMorePicsRemaining_ReturnsAwaitTableauPick()
    {
        var session = Helpers.MakeSession();
        session.Player.PendingLevels = 2;

        var step = EncounterRunner.PickReward(session, "combat", null);

        Assert.IsType<EncounterStep.AwaitTableauPick>(step);
        var tableau = (EncounterStep.AwaitTableauPick)step;
        Assert.Equal(1, tableau.PendingLevels);
    }

    // ── 8. A level granted alongside +finish_dungeon still leaves the dungeon ──

    [Fact]
    public void Choose_AddLevelWithFinishDungeon_ParksThePendingExit()
    {
        var session = Helpers.MakeSession();
        session.Player.CurrentDungeonId = "brides_cave";
        var enc = EncounterWithMechanics(["add_level", "finish_dungeon"]);
        EncounterRunner.Begin(session, enc);

        var step = EncounterRunner.Choose(session, enc.Choices[0]);

        Assert.IsType<EncounterStep.AwaitTableauPick>(step);
        Assert.True(session.Player.PendingDungeonExit);
        Assert.Contains("brides_cave", session.Player.CompletedDungeons);
    }

    [Fact]
    public void PickReward_LastPick_CompletesTheDeferredDungeonExit()
    {
        var session = Helpers.MakeSession();
        session.Player.CurrentDungeonId = "brides_cave";
        var enc = EncounterWithMechanics(["add_level", "finish_dungeon"]);
        EncounterRunner.Begin(session, enc);
        EncounterRunner.Choose(session, enc.Choices[0]);

        var step = EncounterRunner.PickReward(session, "combat", null);

        var finished = Assert.IsType<EncounterStep.Finished>(step);
        Assert.Equal(FinishReason.DungeonFinished, finished.Reason);
        Assert.Null(session.Player.CurrentDungeonId);
        Assert.False(session.Player.PendingDungeonExit);
    }

    [Fact]
    public void PickReward_WithPicksRemaining_KeepsTheDungeonExitParked()
    {
        var session = Helpers.MakeSession();
        session.Player.CurrentDungeonId = "brides_cave";
        var enc = EncounterWithMechanics(["add_level", "add_level", "finish_dungeon"]);
        EncounterRunner.Begin(session, enc);
        EncounterRunner.Choose(session, enc.Choices[0]);

        EncounterRunner.PickReward(session, "combat", null);

        Assert.True(session.Player.PendingDungeonExit);
        Assert.Equal("brides_cave", session.Player.CurrentDungeonId);
    }

    [Fact]
    public void PickReward_WithoutADungeon_DoesNotClaimAFinishedExit()
    {
        var session = Helpers.MakeSession();
        session.Player.PendingLevels = 1;

        var step = EncounterRunner.PickReward(session, "combat", null);

        Assert.IsType<EncounterStep.Finished>(step);
        Assert.Equal(FinishReason.Completed, ((EncounterStep.Finished)step).Reason);
    }
}
