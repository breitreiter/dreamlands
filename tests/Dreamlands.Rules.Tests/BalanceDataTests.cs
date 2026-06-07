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
        Assert.Equal(4, c.StartingHealth);
        Assert.Equal(20, c.StartingSpirits);
        Assert.Equal(50, c.StartingGold);
        Assert.Equal(2, (int)SkillTier.Expert);
    }

    [Fact]
    public void Conditions_HasExpectedEntries()
    {
        var conditions = BalanceData.Default.Conditions;
        Assert.NotEmpty(conditions);
        Assert.True(conditions.ContainsKey("injured"));
        Assert.True(conditions.ContainsKey("poisoned"));
        Assert.True(conditions.ContainsKey("irradiated"));
        Assert.True(conditions.ContainsKey("lattice_sickness"));
        // Travel conditions are gone — replaced by travails hazards (plans/travel_travails.md)
        Assert.False(conditions.ContainsKey("freezing"));
        Assert.False(conditions.ContainsKey("thirsty"));
        Assert.False(conditions.ContainsKey("exhausted"));
    }

    [Fact]
    public void Hazards_CoverTravelChannels()
    {
        var hazards = BalanceData.Default.Hazards;
        Assert.True(hazards.ContainsKey("thirst"));
        Assert.True(hazards.ContainsKey("cold"));
        Assert.True(hazards.ContainsKey("fatigue"));
        // Every hazard names a real mitigating item and a threshold per skill tier
        foreach (var h in hazards.Values)
        {
            Assert.True(BalanceData.Default.Items.ContainsKey(h.MitigatingItemId), h.Id);
            Assert.Equal(3, h.UnitsPerSpirit.Count);
        }
    }

    [Fact]
    public void Items_IsNonEmpty_AndContainsHuntingKnife()
    {
        var items = BalanceData.Default.Items;
        Assert.NotEmpty(items);
        Assert.True(items.ContainsKey("hunting_knife"), "Expected 'hunting_knife' item");
        Assert.Equal("Hunting Knife", items["hunting_knife"].Name);
    }

    [Theory]
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
        var tokens = ActionVerb.Tokenize("damage_spirits 2");
        Assert.Equal(2, tokens.Count);
        Assert.Equal("damage_spirits", tokens[0]);
        Assert.Equal("2", tokens[1]);
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
    [InlineData("easy", Difficulty.Easy, 8)]
    [InlineData("medium", Difficulty.Medium, 12)]
    [InlineData("hard", Difficulty.Hard, 15)]
    [InlineData("very_hard", Difficulty.VeryHard, 18)]
    [InlineData("epic", Difficulty.Epic, 22)]
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

}
