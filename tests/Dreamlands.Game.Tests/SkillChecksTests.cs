using Dreamlands.Game;
using Dreamlands.Rules;

namespace Dreamlands.Game.Tests;

public class SkillChecksTests
{
    static readonly BalanceData Balance = BalanceData.Default;

    static PlayerState Fresh() => PlayerState.NewGame("test", 99, Balance);

    [Fact]
    public void Roll_ReturnsCorrectStructure()
    {
        var state = Fresh();
        var rng = new Random(100);
        var result = SkillChecks.Roll(Skill.Combat, Difficulty.Easy, state, Balance, rng);

        Assert.Equal(Skill.Combat, result.Skill);
        Assert.Equal(8, result.Target); // Easy DC
        Assert.InRange(result.NaturalRoll, 1, 20);
        Assert.Equal(state.Skills[Skill.Combat], result.SkillLevel);
    }

    [Fact]
    public void Roll_HighSkill_TrivialDifficulty_Passes()
    {
        var state = Fresh();
        state.Skills[Skill.Combat] = 10;
        // d20 (1-20) + 10 >= 5 always passes (except nat 1)
        // Use a seed that won't roll natural 1
        var rng = new Random(1);
        var result = SkillChecks.Roll(Skill.Combat, Difficulty.Trivial, state, Balance, rng);
        Assert.True(result.Passed);
    }

    [Fact]
    public void Roll_ZeroSkill_EpicDifficulty_UsuallyFails()
    {
        var state = Fresh();
        state.Skills[Skill.Cunning] = 0;
        state.Spirits = 20; // no disadvantage

        // DC 22, modifier 0 — only nat 20 passes (and nat 1 always fails)
        int failCount = 0;
        for (int seed = 0; seed < 50; seed++)
        {
            var rng = new Random(seed);
            var result = SkillChecks.Roll(Skill.Cunning, Difficulty.Epic, state, Balance, rng);
            if (!result.Passed) failCount++;
        }
        Assert.True(failCount > 40, "Expected most epic checks with skill 0 to fail");
    }

    [Fact]
    public void Natural1_AlwaysFails()
    {
        var state = Fresh();
        state.Skills[Skill.Combat] = 10; // huge modifier
        state.Spirits = 20;

        // Find a seed that rolls natural 1
        for (int seed = 0; seed < 1000; seed++)
        {
            var rng = new Random(seed);
            var result = SkillChecks.Roll(Skill.Combat, Difficulty.Trivial, state, Balance, rng);
            if (result.NaturalRoll == 1)
            {
                Assert.False(result.Passed, "Natural 1 should always fail");
                return;
            }
        }
        Assert.Fail("Could not find a seed that rolls natural 1");
    }

    [Fact]
    public void Natural20_AlwaysPasses()
    {
        var state = Fresh();
        state.Skills[Skill.Combat] = -2; // lowest modifier
        state.Spirits = 20;

        // Find a seed that rolls natural 20
        for (int seed = 0; seed < 1000; seed++)
        {
            var rng = new Random(seed);
            var result = SkillChecks.Roll(Skill.Combat, Difficulty.Epic, state, Balance, rng);
            if (result.NaturalRoll == 20)
            {
                Assert.True(result.Passed, "Natural 20 should always pass");
                return;
            }
        }
        Assert.Fail("Could not find a seed that rolls natural 20");
    }

    [Fact]
    public void HasSpiritsDisadvantage_AboveThreshold_False()
    {
        Assert.False(SkillChecks.HasSpiritsDisadvantage(20, Balance));
        Assert.False(SkillChecks.HasSpiritsDisadvantage(11, Balance));
    }

    [Fact]
    public void HasSpiritsDisadvantage_AtOrBelowThreshold_True()
    {
        Assert.True(SkillChecks.HasSpiritsDisadvantage(10, Balance));
        Assert.True(SkillChecks.HasSpiritsDisadvantage(0, Balance));
    }

    [Fact]
    public void Roll_LowSpirits_ImposesDisadvantage()
    {
        var state = Fresh();
        state.Skills[Skill.Combat] = 0;
        state.Spirits = 5; // below threshold

        var rng = new Random(42);
        var result = SkillChecks.Roll(Skill.Combat, Difficulty.Medium, state, Balance, rng);
        Assert.Equal(RollMode.Disadvantage, result.RollMode);
    }

    [Fact]
    public void Roll_Advantage_CancelledBySpiritsDisadvantage()
    {
        var state = Fresh();
        state.Skills[Skill.Combat] = 0;
        state.Spirits = 5; // below threshold → disadvantage

        var rng = new Random(42);
        var result = SkillChecks.Roll(Skill.Combat, Difficulty.Medium, state, Balance, rng,
            rollMode: RollMode.Advantage);
        Assert.Equal(RollMode.Normal, result.RollMode);
    }

    [Fact]
    public void Roll_RollModeDefaultsToNormal()
    {
        var state = Fresh();
        state.Spirits = 20;
        var rng = new Random(42);
        var result = SkillChecks.Roll(Skill.Combat, Difficulty.Medium, state, Balance, rng);
        Assert.Equal(RollMode.Normal, result.RollMode);
    }
}
