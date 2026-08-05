using Dreamlands.Encounter;
using Dreamlands.Game;
using Dreamlands.Rules;

namespace Dreamlands.Game.Tests;

/// <summary>
/// Choice-level mechanics (written outside the @if/@else) must run whichever arm fires —
/// otherwise a hub return reaches the model but never the player.
/// See bugs/choice_mechanics_after_conditional_dropped.md.
/// </summary>
public class ChoiceLevelMechanicsResolveTests
{
    static readonly BalanceData Balance = BalanceData.Default;

    static Choice HubSpoke() => new()
    {
        OptionText = "Ask",
        Conditional = new ConditionalOutcome
        {
            Branches =
            [
                new ConditionalBranch
                {
                    Condition = "tag knows",
                    Outcome = new OutcomePart { Text = "She confirms.", Mechanics = ["add_tag confirmed"] }
                }
            ],
            Fallback = new OutcomePart { Text = "She deflects.", Mechanics = ["add_tag deflected"] },
            Mechanics = ["open \"Hub\""],
        }
    };

    [Fact]
    public void BranchFires_ChoiceLevelMechanicsAlsoRun()
    {
        var state = PlayerState.NewGame("test", 99, Balance);
        state.Tags.Add("knows");

        var resolved = Choices.Resolve(HubSpoke(), state, Balance, new Random(1), out _);

        Assert.NotNull(resolved);
        Assert.Equal(["add_tag confirmed", "open \"Hub\""], resolved!.Mechanics);
    }

    [Fact]
    public void FallbackFires_ChoiceLevelMechanicsAlsoRun()
    {
        var state = PlayerState.NewGame("test", 99, Balance);

        var resolved = Choices.Resolve(HubSpoke(), state, Balance, new Random(1), out _);

        Assert.NotNull(resolved);
        Assert.Equal(["add_tag deflected", "open \"Hub\""], resolved!.Mechanics);
    }

    [Fact]
    public void NoChoiceLevelMechanics_BranchMechanicsUnchanged()
    {
        var choice = new Choice
        {
            OptionText = "Ask",
            Conditional = new ConditionalOutcome
            {
                Branches = [],
                Fallback = new OutcomePart { Text = "Plain.", Mechanics = ["add_tag plain"] },
            }
        };
        var state = PlayerState.NewGame("test", 99, Balance);

        var resolved = Choices.Resolve(choice, state, Balance, new Random(1), out _);

        Assert.Equal(["add_tag plain"], resolved!.Mechanics);
    }
}
