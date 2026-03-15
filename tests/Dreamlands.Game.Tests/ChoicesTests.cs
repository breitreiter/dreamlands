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
    public void GetAllWithLockState_UnlockedAndLocked()
    {
        var open = new Choice
        {
            OptionText = "Go forward",
            Single = new SingleOutcome { Part = new OutcomePart { Text = "Ok." } }
        };
        var locked = new Choice
        {
            OptionText = "Secret path",
            Requires = "quality guild 3",
            Single = new SingleOutcome { Part = new OutcomePart { Text = "Hidden." } }
        };
        var encounter = MakeEncounter(open, locked);
        var state = Fresh();

        var all = Choices.GetAllWithLockState(encounter, state, Balance);

        Assert.Equal(2, all.Count);
        Assert.False(all[0].Locked);
        Assert.Equal(0, all[0].OriginalIndex);
        Assert.True(all[1].Locked);
        Assert.Equal(1, all[1].OriginalIndex);
    }

    [Fact]
    public void GetAllWithLockState_QualityMet_NotLocked()
    {
        var choice = new Choice
        {
            OptionText = "Guild door",
            Requires = "quality guild 3",
            Single = new SingleOutcome { Part = new OutcomePart { Text = "Enter." } }
        };
        var encounter = MakeEncounter(choice);
        var state = Fresh();
        state.Qualities["guild"] = 5;

        var all = Choices.GetAllWithLockState(encounter, state, Balance);
        Assert.Single(all);
        Assert.False(all[0].Locked);
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
                    Mechanics = ["heal 2"]
                }
            }
        };

        var state = Fresh();
        var resolved = Choices.Resolve(choice, state, Balance, new Random(1));

        Assert.Null(resolved.Preamble);
        Assert.Null(resolved.CheckResult);
        Assert.Equal("You rest by the fire.", resolved.Text);
        Assert.Single(resolved.Mechanics);
        Assert.Equal("heal 2", resolved.Mechanics[0]);
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
                        Condition = "check cunning medium",
                        Outcome = new OutcomePart { Text = "You slip by unnoticed." }
                    }
                ],
                Fallback = new OutcomePart { Text = "You're spotted!" }
            }
        };

        var state = Fresh();
        state.Skills[Skill.Cunning] = 5;
        var resolved = Choices.Resolve(choice, state, Balance, new Random(42));

        // Either branch should have a check result
        Assert.NotNull(resolved.CheckResult);
        Assert.Equal(Skill.Cunning, resolved.CheckResult!.Skill);
    }

    [Fact]
    public void Resolve_MeetsCondition_PassesWhenSkillMeetsTarget()
    {
        var choice = new Choice
        {
            OptionText = "Barter",
            Conditional = new ConditionalOutcome
            {
                Branches =
                [
                    new ConditionalBranch
                    {
                        Condition = "meets mercantile 3",
                        Outcome = new OutcomePart { Text = "You drive a fair bargain." }
                    }
                ],
                Fallback = new OutcomePart { Text = "They won't budge on price." }
            }
        };

        var state = Fresh();
        state.Skills[Skill.Mercantile] = 3;

        var resolved = Choices.Resolve(choice, state, Balance, new Random(1));
        Assert.Equal("You drive a fair bargain.", resolved.Text);
    }

    [Fact]
    public void Resolve_MeetsCondition_FailsWhenSkillBelowTarget()
    {
        var choice = new Choice
        {
            OptionText = "Barter",
            Conditional = new ConditionalOutcome
            {
                Branches =
                [
                    new ConditionalBranch
                    {
                        Condition = "meets mercantile 5",
                        Outcome = new OutcomePart { Text = "You drive a fair bargain." }
                    }
                ],
                Fallback = new OutcomePart { Text = "They won't budge on price." }
            }
        };

        var state = Fresh();
        state.Skills[Skill.Mercantile] = 2;

        var resolved = Choices.Resolve(choice, state, Balance, new Random(1));
        Assert.Equal("They won't budge on price.", resolved.Text);
    }

    [Fact]
    public void Resolve_MeetsCondition_SetsIsMeetsCheckFlag()
    {
        var choice = new Choice
        {
            OptionText = "Barter",
            Conditional = new ConditionalOutcome
            {
                Branches =
                [
                    new ConditionalBranch
                    {
                        Condition = "meets combat 2",
                        Outcome = new OutcomePart { Text = "Success." }
                    }
                ],
                Fallback = new OutcomePart { Text = "Failure." }
            }
        };

        var state = Fresh();
        state.Skills[Skill.Combat] = 5;

        var resolved = Choices.Resolve(choice, state, Balance, new Random(1));
        Assert.NotNull(resolved.CheckResult);
        Assert.True(resolved.CheckResult!.IsMeetsCheck);
        Assert.Equal(Skill.Combat, resolved.CheckResult.Skill);
    }
}
