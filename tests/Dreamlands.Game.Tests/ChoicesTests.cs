using Dreamlands.Encounter;
using Dreamlands.Game;
using Dreamlands.Rules;

namespace Dreamlands.Game.Tests;

public class ChoicesTests
{
    static readonly BalanceData Balance = BalanceData.Default;

    static PlayerState Fresh() => PlayerState.NewGame("test", 99, Balance);

    static Encounter.Encounter MakeEncounter(params Choice[] choices) => new()
    {
        Id = "test", Category = "test", Title = "Test", Body = "Test body",
        Choices = choices
    };

    [Fact]
    public void GetVisible_NoRequires_AlwaysVisible()
    {
        var choice = new Choice
        {
            OptionText = "Go forward",
            Single = new SingleOutcome { Part = new OutcomePart { Text = "You go forward." } }
        };
        var encounter = MakeEncounter(choice);
        var state = Fresh();

        var visible = Choices.GetVisible(encounter, state, Balance);
        Assert.Single(visible);
    }

    [Fact]
    public void GetVisible_HasItem_HiddenWhenMissing()
    {
        var choice = new Choice
        {
            OptionText = "Use torch",
            Requires = "has torch",
            Single = new SingleOutcome { Part = new OutcomePart { Text = "Light the way." } }
        };
        var encounter = MakeEncounter(choice);
        var state = Fresh();

        var visible = Choices.GetVisible(encounter, state, Balance);
        Assert.Empty(visible);
    }

    [Fact]
    public void GetVisible_HasItem_VisibleWhenPresent()
    {
        var choice = new Choice
        {
            OptionText = "Use torch",
            Requires = "has torch",
            Single = new SingleOutcome { Part = new OutcomePart { Text = "Light the way." } }
        };
        var encounter = MakeEncounter(choice);
        var state = Fresh();
        state.Haversack.Add(new ItemInstance("torch", "Torch"));

        var visible = Choices.GetVisible(encounter, state, Balance);
        Assert.Single(visible);
    }

    [Fact]
    public void GetVisible_Tag_VisibleWhenTagPresent()
    {
        var choice = new Choice
        {
            OptionText = "Secret path",
            Requires = "tag knows_secret",
            Single = new SingleOutcome { Part = new OutcomePart { Text = "Hidden way." } }
        };
        var encounter = MakeEncounter(choice);
        var state = Fresh();
        state.Tags.Add("knows_secret");

        var visible = Choices.GetVisible(encounter, state, Balance);
        Assert.Single(visible);
    }

    [Fact]
    public void Resolve_SingleOutcome_ReturnsTextAndMechanics()
    {
        var choice = new Choice
        {
            OptionText = "Rest",
            Single = new SingleOutcome
            {
                Part = new OutcomePart
                {
                    Text = "You rest by the fire.",
                    Mechanics = ["heal small"]
                }
            }
        };

        var state = Fresh();
        var resolved = Choices.Resolve(choice, state, Balance, new Random(1));

        Assert.Null(resolved.Preamble);
        Assert.Null(resolved.CheckResult);
        Assert.Equal("You rest by the fire.", resolved.Text);
        Assert.Single(resolved.Mechanics);
        Assert.Equal("heal small", resolved.Mechanics[0]);
    }

    [Fact]
    public void Resolve_Conditional_HasItem_ReturnsMatchingBranch()
    {
        var choice = new Choice
        {
            OptionText = "Open chest",
            Conditional = new ConditionalOutcome
            {
                Preamble = "You approach the chest.",
                Branches =
                [
                    new ConditionalBranch
                    {
                        Condition = "has rusted_key",
                        Outcome = new OutcomePart { Text = "The key fits!", Mechanics = ["add_tag chest_opened"] }
                    }
                ],
                Fallback = new OutcomePart { Text = "It's locked tight.", Mechanics = [] }
            }
        };

        var state = Fresh();
        state.Haversack.Add(new ItemInstance("rusted_key", "Rusted Key"));

        var resolved = Choices.Resolve(choice, state, Balance, new Random(1));
        Assert.Equal("You approach the chest.", resolved.Preamble);
        Assert.Equal("The key fits!", resolved.Text);
        Assert.Contains("add_tag chest_opened", resolved.Mechanics);
    }

    [Fact]
    public void Resolve_Conditional_NoMatch_ReturnsFallback()
    {
        var choice = new Choice
        {
            OptionText = "Open chest",
            Conditional = new ConditionalOutcome
            {
                Branches =
                [
                    new ConditionalBranch
                    {
                        Condition = "has rusted_key",
                        Outcome = new OutcomePart { Text = "The key fits!" }
                    }
                ],
                Fallback = new OutcomePart { Text = "It's locked tight." }
            }
        };

        var state = Fresh();
        var resolved = Choices.Resolve(choice, state, Balance, new Random(1));
        Assert.Equal("It's locked tight.", resolved.Text);
    }

    [Fact]
    public void Resolve_Conditional_SkillCheck_CapturesCheckResult()
    {
        var choice = new Choice
        {
            OptionText = "Sneak past",
            Conditional = new ConditionalOutcome
            {
                Branches =
                [
                    new ConditionalBranch
                    {
                        Condition = "check stealth medium",
                        Outcome = new OutcomePart { Text = "You slip by unnoticed." }
                    }
                ],
                Fallback = new OutcomePart { Text = "You're spotted!" }
            }
        };

        var state = Fresh();
        state.Skills[Skill.Stealth] = 5;
        var resolved = Choices.Resolve(choice, state, Balance, new Random(42));

        // Either branch should have a check result
        Assert.NotNull(resolved.CheckResult);
        Assert.Equal(Skill.Stealth, resolved.CheckResult!.Skill);
    }
}
