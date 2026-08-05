using Dreamlands.Encounter;

namespace Dreamlands.Encounter.Tests;

/// <summary>
/// Regression cover for bugs/choice_mechanics_after_conditional_dropped.md — a `+verb`
/// written at choice level (outside the @if/@else) used to be parsed away entirely,
/// which silently killed `+open` hub returns in five shipped arcs.
/// </summary>
public class ChoiceLevelMechanicsTests
{
    private const string AfterBlock = """
        Hub Spoke
        [trigger none]
        Body text.
        choices:

        * Ask about the knife = The wrapped blade
            @if tag fugitive.knife_truth {
                She confirms it.
            } @else {
                She deflects.
            }
            +open "Mareen"
        """;

    [Fact]
    public void MechanicAfterConditionalBlock_IsKept()
    {
        var enc = EncounterParser.Parse(AfterBlock).Encounter!;
        var conditional = enc.Choices[0].Conditional!;

        Assert.Equal(["open \"Mareen\""], conditional.Mechanics);
    }

    [Fact]
    public void MechanicBeforeConditionalBlock_IsKept()
    {
        var source = """
            Hub Spoke
            [trigger none]
            Body text.
            choices:

            * Look = Peer in
                +add_tag saw_it
                @if tag lamp {
                    You see it clearly.
                } @else {
                    You see a shape.
                }
            """;

        var enc = EncounterParser.Parse(source).Encounter!;
        Assert.Equal(["add_tag saw_it"], enc.Choices[0].Conditional!.Mechanics);
    }

    [Fact]
    public void BranchMechanics_AreUnaffected()
    {
        var source = """
            Both
            [trigger none]
            Body text.
            choices:

            * Go = Move
                @if tag a {
                    Left.
                    +add_tag went_left
                } @else {
                    Right.
                    +add_tag went_right
                }
                +open "Hub"
            """;

        var conditional = EncounterParser.Parse(source).Encounter!.Choices[0].Conditional!;

        Assert.Equal(["add_tag went_left"], conditional.Branches[0].Outcome.Mechanics);
        Assert.Equal(["add_tag went_right"], conditional.Fallback!.Mechanics);
        Assert.Equal(["open \"Hub\""], conditional.Mechanics);
    }

    [Fact]
    public void SingleOutcome_MechanicsStillLandOnSingle()
    {
        var source = """
            Plain
            [trigger none]
            Body text.
            choices:

            * Go = Move
                You go.
                +open "Hub"
            """;

        var choice = EncounterParser.Parse(source).Encounter!.Choices[0];

        Assert.Null(choice.Conditional);
        Assert.Equal(["open \"Hub\""], choice.Single!.Part.Mechanics);
    }
}
