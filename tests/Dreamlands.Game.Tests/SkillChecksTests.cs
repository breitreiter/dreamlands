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
        Assert.Equal(10, result.Target); // Easy DC
        Assert.InRange(result.Rolled, 1, 20);
        Assert.Equal(state.Skills[Skill.Combat], result.SkillLevel);
    }

    [Fact]
    public void Roll_HighSkill_TrivialDifficulty_Passes()
    {
        var state = Fresh();
        state.Skills[Skill.Combat] = 10;
        // d20 (1-20) + 10 >= 5 always passes
        var rng = new Random(1);
        var result = SkillChecks.Roll(Skill.Combat, Difficulty.Trivial, state, Balance, rng);
        Assert.True(result.Passed);
    }

    [Fact]
    public void Roll_ZeroSkill_HeroicDifficulty_UsuallyFails()
    {
        var state = Fresh();
        state.Skills[Skill.Stealth] = 0;
        state.Spirits = 20; // no penalty

        // DC 30, need natural 20+10 (modifier 0) — impossible without high roll+modifier
        // Try many seeds, most should fail
        int failCount = 0;
        for (int seed = 0; seed < 50; seed++)
        {
            var rng = new Random(seed);
            var result = SkillChecks.Roll(Skill.Stealth, Difficulty.Heroic, state, Balance, rng);
            if (!result.Passed) failCount++;
        }
        Assert.True(failCount > 40, "Expected most heroic checks with skill 0 to fail");
    }

    [Fact]
    public void GetSpiritsPenalty_FullSpirits_NoPenalty()
    {
        Assert.Equal(0, SkillChecks.GetSpiritsPenalty(20, Balance));
    }

    [Fact]
    public void GetSpiritsPenalty_AboveAllThresholds_NoPenalty()
    {
        Assert.Equal(0, SkillChecks.GetSpiritsPenalty(16, Balance));
    }

    [Fact]
    public void GetSpiritsPenalty_AtFirstThreshold_ReturnsPenalty()
    {
        // First threshold is AtOrBelow=15, Penalty=-1
        Assert.Equal(-1, SkillChecks.GetSpiritsPenalty(15, Balance));
    }

    [Fact]
    public void GetSpiritsPenalty_AtZero_ReturnsPenalty()
    {
        var penalty = SkillChecks.GetSpiritsPenalty(0, Balance);
        Assert.True(penalty < 0, "Expected negative penalty at 0 spirits");
    }

    [Fact]
    public void Roll_WithSpiritsPenalty_AffectsModifier()
    {
        var state = Fresh();
        state.Skills[Skill.Combat] = 5;
        state.Spirits = 20; // no penalty

        var rng1 = new Random(42);
        var noPenalty = SkillChecks.Roll(Skill.Combat, Difficulty.Medium, state, Balance, rng1);

        state.Spirits = 10; // penalty
        var rng2 = new Random(42); // same seed = same d20 roll
        var withPenalty = SkillChecks.Roll(Skill.Combat, Difficulty.Medium, state, Balance, rng2);

        Assert.Equal(noPenalty.Rolled, withPenalty.Rolled); // same die roll
        Assert.True(withPenalty.Modifier < noPenalty.Modifier, "Spirits penalty should reduce modifier");
    }
}
