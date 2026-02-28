using Dreamlands.Rules;

namespace Dreamlands.Rules.Tests;

public class SkillFlavorTests
{
    [Theory]
    [InlineData(Skill.Combat, 0, "Violence is a tool for simpletons and savages")]
    [InlineData(Skill.Combat, 2, "You know drills and forms, but have spilled little blood")]
    [InlineData(Skill.Combat, 4, "You read intent in a shoulder twitch and end fights decisively")]
    [InlineData(Skill.Negotiation, 0, "You speak plainly and without artifice")]
    [InlineData(Skill.Bushcraft, 2, "You can find water, shelter, and a way through")]
    [InlineData(Skill.Cunning, 4, "You are always three moves ahead")]
    [InlineData(Skill.Luck, 0, "You've learned to rely on skill, never chance")]
    [InlineData(Skill.Mercantile, 4, "You walk markets like a lion, hungry and calculating")]
    public void Get_ReturnsCorrectFlavor(Skill skill, int level, string expected)
    {
        Assert.Equal(expected, SkillFlavor.Get(skill, level));
    }

    [Fact]
    public void Get_NegativeLevel_ReturnsTierZero()
    {
        var result = SkillFlavor.Get(Skill.Combat, -2);
        Assert.Equal("Violence is a tool for simpletons and savages", result);
    }

    [Fact]
    public void Get_Level1_ReturnsTrained()
    {
        // Level 1 should map to trained (tier 1), same as level 2
        var result = SkillFlavor.Get(Skill.Bushcraft, 1);
        Assert.Equal("You can find water, shelter, and a way through", result);
    }

    [Fact]
    public void Get_Level3_ReturnsExpert()
    {
        // Level 3 should map to expert (tier 2), same as level 4
        var result = SkillFlavor.Get(Skill.Luck, 3);
        Assert.Equal("You live a charmed life", result);
    }

    [Fact]
    public void Get_AllSkills_HaveAllThreeTiers()
    {
        foreach (var si in Skills.All)
        {
            var unskilled = SkillFlavor.Get(si.Skill, 0);
            var trained = SkillFlavor.Get(si.Skill, 2);
            var expert = SkillFlavor.Get(si.Skill, 4);

            Assert.False(string.IsNullOrEmpty(unskilled), $"{si.DisplayName} missing unskilled flavor");
            Assert.False(string.IsNullOrEmpty(trained), $"{si.DisplayName} missing trained flavor");
            Assert.False(string.IsNullOrEmpty(expert), $"{si.DisplayName} missing expert flavor");
            Assert.NotEqual(unskilled, trained);
            Assert.NotEqual(trained, expert);
        }
    }
}
