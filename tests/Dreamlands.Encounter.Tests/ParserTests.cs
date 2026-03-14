using Dreamlands.Encounter;

namespace Dreamlands.Encounter.Tests;

public class ParserTests
{
    [Fact]
    public void MinimalEncounter_ParsesCorrectly()
    {
        var source = """
            The Old Well
            You peer into the darkness below.
            choices:
            * Look down
            You see nothing but blackness.
            """;

        var result = EncounterParser.Parse(source);
        Assert.True(result.IsSuccess);

        var enc = result.Encounter!;
        Assert.Equal("The Old Well", enc.Title);
        Assert.Contains("darkness below", enc.Body);
        Assert.Single(enc.Choices);

        var choice = enc.Choices[0];
        Assert.Equal("Look down", choice.OptionText);
        Assert.NotNull(choice.Single);
        Assert.Equal("You see nothing but blackness.", choice.Single!.Part.Text);
    }

    [Fact]
    public void Choice_WithLinkPreview_ParsesCorrectly()
    {
        var source = """
            Test
            Body text.
            choices:
            * Go left = explore the dark passage
            You venture into shadow.
            """;

        var result = EncounterParser.Parse(source);
        Assert.True(result.IsSuccess);

        var choice = result.Encounter!.Choices[0];
        Assert.Equal("Go left", choice.OptionLink);
        Assert.Equal("explore the dark passage", choice.OptionPreview);
    }

    [Fact]
    public void Choice_WithRequires_ParsesCondition()
    {
        var source = """
            Test
            Body.
            choices:
            * Unlock the door [requires has rusted_key]
            The door swings open.
            """;

        var result = EncounterParser.Parse(source);
        Assert.True(result.IsSuccess);

        var choice = result.Encounter!.Choices[0];
        Assert.Equal("has rusted_key", choice.Requires);
        Assert.Equal("Unlock the door", choice.OptionText);
    }

    [Fact]
    public void ConditionalChoice_IfElse_ParsesCorrectly()
    {
        var source = """
            Test
            Body.
            choices:
            * Fight the beast
            @if check combat hard {
            You strike true!
            +damage_spirits 2
            } @else {
            The beast overpowers you.
            +damage_spirits 3
            }
            """;

        var result = EncounterParser.Parse(source);
        Assert.True(result.IsSuccess);

        var choice = result.Encounter!.Choices[0];
        Assert.NotNull(choice.Conditional);
        Assert.Null(choice.Single);

        var cond = choice.Conditional!;
        Assert.Single(cond.Branches);
        Assert.Equal("check combat hard", cond.Branches[0].Condition);
        Assert.Contains("strike true", cond.Branches[0].Outcome.Text);
        Assert.Single(cond.Branches[0].Outcome.Mechanics);
        Assert.Equal("damage_spirits 2", cond.Branches[0].Outcome.Mechanics[0]);

        Assert.NotNull(cond.Fallback);
        Assert.Contains("overpowers", cond.Fallback!.Text);
        Assert.Equal("damage_spirits 3", cond.Fallback.Mechanics[0]);
    }

    [Fact]
    public void ConditionalChoice_MultipleElif_ParsesAllBranches()
    {
        var source = """
            Test
            Body.
            choices:
            * Investigate
            @if check perception hard {
            You spot the trap immediately.
            } @elif check perception medium {
            You notice something off.
            } @elif has lantern {
            The lantern reveals a wire.
            } @else {
            You see nothing unusual.
            }
            """;

        var result = EncounterParser.Parse(source);
        Assert.True(result.IsSuccess);

        var cond = result.Encounter!.Choices[0].Conditional!;
        Assert.Equal(3, cond.Branches.Count);
        Assert.Equal("check perception hard", cond.Branches[0].Condition);
        Assert.Equal("check perception medium", cond.Branches[1].Condition);
        Assert.Equal("has lantern", cond.Branches[2].Condition);
        Assert.NotNull(cond.Fallback);
    }

    [Fact]
    public void MechanicsLines_ParsedCorrectly()
    {
        var source = """
            Test
            Body.
            choices:
            * Take the treasure
            You grab the gold.
            +give_gold 4
            +add_tag found_treasure
            """;

        var result = EncounterParser.Parse(source);
        Assert.True(result.IsSuccess);

        var mechanics = result.Encounter!.Choices[0].Single!.Part.Mechanics;
        Assert.Equal(2, mechanics.Count);
        Assert.Equal("give_gold 4", mechanics[0]);
        Assert.Equal("add_tag found_treasure", mechanics[1]);
    }

    [Fact]
    public void Preamble_BeforeIf_Captured()
    {
        var source = """
            Test
            Body.
            choices:
            * Try the lock
            You kneel before the ancient mechanism.
            @if check stealth easy {
            It clicks open.
            } @else {
            The pick snaps.
            }
            """;

        var result = EncounterParser.Parse(source);
        Assert.True(result.IsSuccess);

        var cond = result.Encounter!.Choices[0].Conditional!;
        Assert.Contains("ancient mechanism", cond.Preamble);
    }

    [Fact]
    public void Error_MissingChoices_ReportsError()
    {
        var source = """
            Test
            This encounter has no choices marker.
            """;

        var result = EncounterParser.Parse(source);
        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Message.Contains("choices:"));
    }

    [Fact]
    public void Error_UnclosedBrace_ReportsError()
    {
        var source = """
            Test
            Body.
            choices:
            * Try it
            @if check combat easy {
            You succeed.
            """;

        var result = EncounterParser.Parse(source);
        Assert.Contains(result.Errors, e => e.Message.Contains("Unclosed"));
    }

    [Fact]
    public void Error_StrayCheck_ReportsError()
    {
        var source = """
            Test
            Body.
            choices:
            * Old style
            @check combat easy
            You win.
            """;

        var result = EncounterParser.Parse(source);
        Assert.Contains(result.Errors, e => e.Message.Contains("@check"));
    }

    [Fact]
    public void BlankLinesInSingleOutcome_CreateParagraphBreaks()
    {
        var source = """
            Test
            Body.
            choices:
            * Look around
            The room is empty.

            Dust motes drift in the light.
            """;

        var result = EncounterParser.Parse(source);
        Assert.True(result.IsSuccess);

        var text = result.Encounter!.Choices[0].Single!.Part.Text;
        Assert.Contains("The room is empty.", text);
        Assert.Contains("Dust motes drift", text);
        // Blank line produces a \n\n paragraph break
        Assert.Contains("\n\n", text);
    }

    [Fact]
    public void BlankLinesInConditionalBranch_Preserved()
    {
        var source = """
            Test
            Body.
            choices:
            * Try it
            @if check combat easy {
            You swing your blade.

            The beast falls.
            } @else {
            You miss.
            }
            """;

        var result = EncounterParser.Parse(source);
        Assert.True(result.IsSuccess);

        var branch = result.Encounter!.Choices[0].Conditional!.Branches[0];
        Assert.Contains("\n\n", branch.Outcome.Text);
        Assert.Contains("You swing your blade.", branch.Outcome.Text);
        Assert.Contains("The beast falls.", branch.Outcome.Text);
    }

    [Fact]
    public void BlankLinesInFallback_Preserved()
    {
        var source = """
            Test
            Body.
            choices:
            * Try it
            @if check combat easy {
            You succeed.
            } @else {
            You stumble badly.

            The beast snarls.
            }
            """;

        var result = EncounterParser.Parse(source);
        Assert.True(result.IsSuccess);

        var fallback = result.Encounter!.Choices[0].Conditional!.Fallback!;
        Assert.Contains("\n\n", fallback.Text);
        Assert.Contains("You stumble badly.", fallback.Text);
        Assert.Contains("The beast snarls.", fallback.Text);
    }

    [Fact]
    public void EncounterRequires_SingleCondition_Parsed()
    {
        var source = """
            The Return
            [requires tag briar_backed_dara]

            You arrive at the commons.
            choices:
            * Enter
            You step inside.
            """;

        var result = EncounterParser.Parse(source);
        Assert.True(result.IsSuccess);

        var enc = result.Encounter!;
        Assert.Single(enc.Requires);
        Assert.Equal("tag briar_backed_dara", enc.Requires[0]);
        Assert.Contains("You arrive", enc.Body);
        Assert.DoesNotContain("requires", enc.Body);
    }

    [Fact]
    public void EncounterRequires_MultipleConditions_Parsed()
    {
        var source = """
            The Return
            [requires tag briar_backed_dara]
            [requires quality exiles 2]

            The commune has grown.
            choices:
            * Enter
            You step inside.
            """;

        var result = EncounterParser.Parse(source);
        Assert.True(result.IsSuccess);

        var enc = result.Encounter!;
        Assert.Equal(2, enc.Requires.Count);
        Assert.Equal("tag briar_backed_dara", enc.Requires[0]);
        Assert.Equal("quality exiles 2", enc.Requires[1]);
        Assert.Contains("commune has grown", enc.Body);
        Assert.DoesNotContain("requires", enc.Body);
    }

    [Fact]
    public void EncounterRequires_None_EmptyList()
    {
        var source = """
            The Old Well
            You peer into the darkness below.
            choices:
            * Look down
            You see nothing but blackness.
            """;

        var result = EncounterParser.Parse(source);
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Encounter!.Requires);
    }

    [Fact]
    public void Trigger_Parsed()
    {
        var source = """
            Test
            [trigger settlement]
            Body.
            choices:
            * Go
            Done.
            """;

        var result = EncounterParser.Parse(source);
        Assert.True(result.IsSuccess);
        Assert.Equal("settlement", result.Encounter!.Trigger);
    }

    [Fact]
    public void Tier_Parsed()
    {
        var source = """
            Test
            [tier 2]
            Body.
            choices:
            * Go
            Done.
            """;

        var result = EncounterParser.Parse(source);
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Encounter!.Tier);
    }

    [Fact]
    public void AllFrontMatter_Parsed()
    {
        var source = """
            Test
            [trigger settlement]
            [tier 3]
            [requires tag some_flag]

            Body.
            choices:
            * Go
            Done.
            """;

        var result = EncounterParser.Parse(source);
        Assert.True(result.IsSuccess);

        var enc = result.Encounter!;
        Assert.Equal("settlement", enc.Trigger);
        Assert.Equal(3, enc.Tier);
        Assert.Single(enc.Requires);
        Assert.Equal("tag some_flag", enc.Requires[0]);
        Assert.Contains("Body.", enc.Body);
    }

    [Fact]
    public void NoTriggerOrTier_DefaultsNull()
    {
        var source = """
            Test
            Body.
            choices:
            * Go
            Done.
            """;

        var result = EncounterParser.Parse(source);
        Assert.True(result.IsSuccess);
        Assert.Null(result.Encounter!.Trigger);
        Assert.Null(result.Encounter!.Tier);
    }

    [Fact]
    public void InvalidTrigger_Error()
    {
        var source = """
            Test
            [trigger dungeon]
            Body.
            choices:
            * Go
            Done.
            """;

        var result = EncounterParser.Parse(source);
        Assert.Contains(result.Errors, e => e.Message.Contains("Invalid trigger"));
    }

    [Fact]
    public void InvalidTier_Error()
    {
        var source = """
            Test
            [tier 5]
            Body.
            choices:
            * Go
            Done.
            """;

        var result = EncounterParser.Parse(source);
        Assert.Contains(result.Errors, e => e.Message.Contains("Invalid tier"));
    }

    [Fact]
    public void DuplicateTrigger_Error()
    {
        var source = """
            Test
            [trigger road]
            [trigger settlement]
            Body.
            choices:
            * Go
            Done.
            """;

        var result = EncounterParser.Parse(source);
        Assert.Contains(result.Errors, e => e.Message.Contains("Duplicate [trigger]"));
    }

    [Fact]
    public void DuplicateTier_Error()
    {
        var source = """
            Test
            [tier 1]
            [tier 2]
            Body.
            choices:
            * Go
            Done.
            """;

        var result = EncounterParser.Parse(source);
        Assert.Contains(result.Errors, e => e.Message.Contains("Duplicate [tier]"));
    }

    [Fact]
    public void Vignette_Parsed()
    {
        var source = """
            Test
            [vignette intro/00_Intro]
            Body.
            choices:
            * Go
            Done.
            """;

        var result = EncounterParser.Parse(source);
        Assert.True(result.IsSuccess);
        Assert.Equal("intro/00_Intro", result.Encounter!.Vignette);
    }

    [Fact]
    public void Vignette_DefaultsNull()
    {
        var source = """
            Test
            Body.
            choices:
            * Go
            Done.
            """;

        var result = EncounterParser.Parse(source);
        Assert.True(result.IsSuccess);
        Assert.Null(result.Encounter!.Vignette);
    }

    [Fact]
    public void DuplicateVignette_Error()
    {
        var source = """
            Test
            [vignette intro/00_Intro]
            [vignette dungeons/cave]
            Body.
            choices:
            * Go
            Done.
            """;

        var result = EncounterParser.Parse(source);
        Assert.Contains(result.Errors, e => e.Message.Contains("Duplicate [vignette]"));
    }

    [Fact]
    public void Vignette_WithOtherFrontMatter_Parsed()
    {
        var source = """
            Test
            [trigger settlement]
            [vignette dungeons/brides_cave]
            [tier 2]
            [requires tag some_flag]

            Body.
            choices:
            * Go
            Done.
            """;

        var result = EncounterParser.Parse(source);
        Assert.True(result.IsSuccess);

        var enc = result.Encounter!;
        Assert.Equal("settlement", enc.Trigger);
        Assert.Equal("dungeons/brides_cave", enc.Vignette);
        Assert.Equal(2, enc.Tier);
        Assert.Single(enc.Requires);
    }

    [Fact]
    public void MultipleChoices_AllParsed()
    {
        var source = """
            Test
            Body.
            choices:
            * First option
            Result one.
            * Second option
            Result two.
            * Third option
            Result three.
            """;

        var result = EncounterParser.Parse(source);
        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Encounter!.Choices.Count);
        Assert.Equal("First option", result.Encounter.Choices[0].OptionText);
        Assert.Equal("Second option", result.Encounter.Choices[1].OptionText);
        Assert.Equal("Third option", result.Encounter.Choices[2].OptionText);
    }
}
