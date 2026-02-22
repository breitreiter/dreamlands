using Dreamlands.Rules;

namespace Dreamlands.Rules.Tests;

public class BalanceDataTests
{
    [Fact]
    public void Default_IsNotNull()
    {
        Assert.NotNull(BalanceData.Default);
    }

    [Fact]
    public void Default_CharacterBalance_IsPopulated()
    {
        var c = BalanceData.Default.Character;
        Assert.NotNull(c);
        Assert.Equal(20, c.StartingHealth);
        Assert.Equal(20, c.StartingSpirits);
        Assert.Equal(50, c.StartingGold);
        Assert.Equal(10, c.MaxSkillLevel);
    }

    [Fact]
    public void Conditions_HasExpectedEntries()
    {
        var conditions = BalanceData.Default.Conditions;
        Assert.NotEmpty(conditions);
        Assert.True(conditions.ContainsKey("freezing"));
        Assert.True(conditions.ContainsKey("hungry"));
        Assert.True(conditions.ContainsKey("thirsty"));
        Assert.True(conditions.ContainsKey("exhausted"));
        Assert.True(conditions.ContainsKey("poisoned"));
    }

    [Fact]
    public void Items_IsNonEmpty_AndContainsBodkin()
    {
        var items = BalanceData.Default.Items;
        Assert.NotEmpty(items);
        Assert.True(items.ContainsKey("bodkin"), "Expected 'bodkin' item");
        Assert.Equal("Bodkin", items["bodkin"].Name);
    }

    [Theory]
    [InlineData("damage_health")]
    [InlineData("heal")]
    [InlineData("give_gold")]
    [InlineData("add_item")]
    [InlineData("add_tag")]
    [InlineData("open")]
    [InlineData("skip_time")]
    [InlineData("equip")]
    public void ActionVerb_FromName_RoundTrips(string name)
    {
        var verb = ActionVerb.FromName(name);
        Assert.NotNull(verb);
        Assert.Equal(name, verb!.Name);
    }

    [Fact]
    public void ActionVerb_FromName_ReturnsNull_ForUnknown()
    {
        Assert.Null(ActionVerb.FromName("not_a_verb"));
    }

    [Fact]
    public void ActionVerb_Tokenize_SplitsSimpleTokens()
    {
        var tokens = ActionVerb.Tokenize("damage_health small");
        Assert.Equal(2, tokens.Count);
        Assert.Equal("damage_health", tokens[0]);
        Assert.Equal("small", tokens[1]);
    }

    [Fact]
    public void ActionVerb_Tokenize_HandlesQuotedStrings()
    {
        var tokens = ActionVerb.Tokenize("open \"The Ghosts\"");
        Assert.Equal(2, tokens.Count);
        Assert.Equal("open", tokens[0]);
        Assert.Equal("The Ghosts", tokens[1]);
    }

    [Theory]
    [InlineData("combat", Skill.Combat)]
    [InlineData("negotiation", Skill.Negotiation)]
    [InlineData("bushcraft", Skill.Bushcraft)]
    [InlineData("cunning", Skill.Cunning)]
    [InlineData("luck", Skill.Luck)]
    [InlineData("mercantile", Skill.Mercantile)]
    public void Skills_FromScriptName_RoundTrips(string name, Skill expected)
    {
        var skill = Skills.FromScriptName(name);
        Assert.NotNull(skill);
        Assert.Equal(expected, skill!.Value);
        Assert.Equal(name, expected.ScriptName());
    }

    [Fact]
    public void Skills_FromScriptName_ReturnsNull_ForUnknown()
    {
        Assert.Null(Skills.FromScriptName("alchemy"));
    }

    [Theory]
    [InlineData("trivial", Difficulty.Trivial, 5)]
    [InlineData("easy", Difficulty.Easy, 10)]
    [InlineData("medium", Difficulty.Medium, 15)]
    [InlineData("hard", Difficulty.Hard, 20)]
    [InlineData("very_hard", Difficulty.VeryHard, 25)]
    [InlineData("heroic", Difficulty.Heroic, 30)]
    public void Difficulties_FromScriptName_RoundTrips(string name, Difficulty expected, int dc)
    {
        var difficulty = Difficulties.FromScriptName(name);
        Assert.NotNull(difficulty);
        Assert.Equal(expected, difficulty!.Value);
        Assert.Equal(name, expected.ScriptName());
        Assert.Equal(dc, expected.Target());
    }

    [Fact]
    public void Difficulties_FromScriptName_ReturnsNull_ForUnknown()
    {
        Assert.Null(Difficulties.FromScriptName("impossible"));
    }

    [Fact]
    public void DamageMagnitudes_HasExpectedValues()
    {
        var mags = BalanceData.Default.Character.DamageMagnitudes;
        Assert.Equal(1, mags[Magnitude.Trivial]);
        Assert.Equal(2, mags[Magnitude.Small]);
        Assert.Equal(3, mags[Magnitude.Medium]);
        Assert.Equal(4, mags[Magnitude.Large]);
        Assert.Equal(5, mags[Magnitude.Huge]);
    }
}
