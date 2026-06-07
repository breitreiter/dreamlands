using Dreamlands.Rules;

namespace Dreamlands.Rules.Tests;

public class SkillFlavorTests
{
    [Theory]
    [InlineData(Skill.Combat, 0, "Daggers and light armor only. Encounter checks are punishing.")]
    [InlineData(Skill.Combat, 2, "Adds axes and medium armor. Encounter checks are fair.")]
    [InlineData(Skill.Combat, 4, "Adds swords and heavy armor. Encounter checks are generous.")]
    [InlineData(Skill.Negotiation, 0, "No contract bonus. Encounter checks are punishing.")]
    [InlineData(Skill.Bushcraft, 2, "Halves travel hazard costs; eat every other night. Encounter checks are fair.")]
    [InlineData(Skill.Cunning, 4, "80% chance to resist serious conditions. Encounter checks are generous.")]
    public void Get_ReturnsCorrectFlavor(Skill skill, int level, string expected)
    {
        Assert.Equal(expected, SkillFlavor.Get(skill, level));
    }

    [Fact]
    public void Get_NegativeLevel_ReturnsTierZero()
    {
        var result = SkillFlavor.Get(Skill.Combat, -2);
        Assert.Equal("Daggers and light armor only. Encounter checks are punishing.", result);
    }

    [Fact]
    public void Get_Level1_ReturnsTrained()
    {
        // Level 1 should map to trained (tier 1), same as level 2
        var result = SkillFlavor.Get(Skill.Bushcraft, 1);
        Assert.Equal("Halves travel hazard costs; eat every other night. Encounter checks are fair.", result);
    }

    [Fact]
    public void Get_Level3_ReturnsExpert()
    {
        // Level 3 should map to expert (tier 2), same as level 4
        var result = SkillFlavor.Get(Skill.Cunning, 3);
        Assert.Equal("80% chance to resist serious conditions. Encounter checks are generous.", result);
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
