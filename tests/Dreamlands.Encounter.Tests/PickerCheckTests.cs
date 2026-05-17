using Dreamlands.Encounter;

namespace Dreamlands.Encounter.Tests;

/// <summary>
/// Tests for Phase 3: picker check parsing, terminal-check enforcement,
/// meets-tier condition, and bundle JSON round-trip with picker fields.
/// </summary>
public class PickerCheckTests
{
    // ── Picker check parsing ─────────────────────────────────────────────

    [Fact]
    public void PickerCheck_CorrectBeforeWrong_ParsesAttributes()
    {
        var source = """
            Test
            [trigger none]
            Body.
            choices:
            * Try to negotiate
            @if check negotiation correct:reason wrong:threaten {
            You make a compelling case.
            } @else {
            They don't buy it.
            }
            """;

        var result = EncounterParser.Parse(source);
        Assert.True(result.IsSuccess, string.Join("; ", result.Errors.Where(e => !e.IsWarning).Select(e => e.Message)));

        var branch = result.Encounter!.Choices[0].Conditional!.Branches[0];
        Assert.True(branch.IsPickerCheck);
        Assert.Equal("negotiation", branch.PickerSkill);
        Assert.Equal("reason", branch.PickerCorrect);
        Assert.Equal("threaten", branch.PickerWrong);
        Assert.Equal("check negotiation correct:reason wrong:threaten", branch.Condition);
    }

    [Fact]
    public void PickerCheck_WrongBeforeCorrect_ParsesAttributes()
    {
        var source = """
            Test
            [trigger none]
            Body.
            choices:
            * Use cunning
            @if check cunning wrong:bluff correct:hide {
            You slip away unnoticed.
            } @else {
            They spot you immediately.
            }
            """;

        var result = EncounterParser.Parse(source);
        Assert.True(result.IsSuccess, string.Join("; ", result.Errors.Where(e => !e.IsWarning).Select(e => e.Message)));

        var branch = result.Encounter!.Choices[0].Conditional!.Branches[0];
        Assert.True(branch.IsPickerCheck);
        Assert.Equal("cunning", branch.PickerSkill);
        Assert.Equal("hide", branch.PickerCorrect);
        Assert.Equal("bluff", branch.PickerWrong);
    }

    [Fact]
    public void PickerCheck_AfterStaticBranches_IsTerminalAndValid()
    {
        var source = """
            Test
            [trigger none]
            Body.
            choices:
            * Investigate
            @if tag already_bribed {
            They wave you through.
            } @elif check negotiation correct:reason wrong:threaten {
            You talk your way past.
            } @else {
            They block you.
            }
            """;

        var result = EncounterParser.Parse(source);
        Assert.True(result.IsSuccess, string.Join("; ", result.Errors.Where(e => !e.IsWarning).Select(e => e.Message)));

        var cond = result.Encounter!.Choices[0].Conditional!;
        Assert.Equal(2, cond.Branches.Count);
        Assert.False(cond.Branches[0].IsPickerCheck);
        Assert.True(cond.Branches[1].IsPickerCheck);
        Assert.NotNull(cond.Fallback);
    }

    // ── Legacy DC check — deprecation diagnostic ─────────────────────────

    [Fact]
    public void LegacyDcCheck_ParsesSuccessfully()
    {
        var source = """
            Test
            [trigger none]
            Body.
            choices:
            * Fight the beast
            @if check combat hard {
            You strike true!
            } @else {
            The beast wins.
            }
            """;

        var result = EncounterParser.Parse(source);
        // IsSuccess tolerates warnings
        Assert.True(result.IsSuccess, string.Join("; ", result.Errors.Where(e => !e.IsWarning).Select(e => e.Message)));
        Assert.Empty(result.Errors.Where(e => !e.IsWarning));
    }

    [Fact]
    public void LegacyDcCheck_EmitsDeprecationWarning()
    {
        var source = """
            Test
            [trigger none]
            Body.
            choices:
            * Fight
            @if check combat hard {
            You win.
            } @else {
            You lose.
            }
            """;

        var result = EncounterParser.Parse(source);
        Assert.True(result.IsSuccess);
        Assert.Single(result.Warnings);
        Assert.Contains("legacy DC check", result.Warnings[0].Message);
        Assert.Contains("Phase 4", result.Warnings[0].Message);
    }

    [Fact]
    public void LegacyDcCheck_IsExemptFromTerminalRule_MidChain_NoError()
    {
        // Legacy DC checks are not picker checks — they don't trigger terminal enforcement.
        var source = """
            Test
            [trigger none]
            Body.
            choices:
            * Try
            @if check combat hard {
            You win.
            } @elif tag bribed {
            They let you through.
            } @else {
            Nope.
            }
            """;

        var result = EncounterParser.Parse(source);
        // No hard errors from terminal-check rule (legacy is exempt)
        Assert.Empty(result.Errors.Where(e => !e.IsWarning));
    }

    // ── Terminal-check enforcement: four illegal shapes ──────────────────

    [Fact]
    public void Error_PickerCheck_MidChain_FollowedByElif()
    {
        // Illegal: picker check is NOT the terminal branch (followed by @elif tag)
        var source = """
            Test
            [trigger none]
            Body.
            choices:
            * Try
            @if check negotiation correct:reason wrong:threaten {
            You succeeded.
            } @elif tag bribed {
            They let you through.
            } @else {
            Blocked.
            }
            """;

        var result = EncounterParser.Parse(source);
        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => !e.IsWarning && e.Message.Contains("picker check") && e.Message.Contains("terminal"));
    }

    [Fact]
    public void Error_PickerCheck_NoElseFallback()
    {
        // Illegal: picker check with no @else
        var source = """
            Test
            [trigger none]
            Body.
            choices:
            * Try
            @if check negotiation correct:reason wrong:threaten {
            You succeeded.
            }
            """;

        var result = EncounterParser.Parse(source);
        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => !e.IsWarning && e.Message.Contains("picker check") && e.Message.Contains("@else"));
    }

    [Fact]
    public void Error_ElifAfterPickerCheck()
    {
        // Illegal: @elif appears after a picker-check branch (same as mid-chain, different code path)
        // This is the same as Error_PickerCheck_MidChain but tests explicit @elif after the check.
        var source = """
            Test
            [trigger none]
            Body.
            choices:
            * Try
            @if tag X {
            Static gate.
            } @elif check negotiation correct:reason wrong:threaten {
            You succeeded.
            } @elif tag Y {
            Another branch.
            } @else {
            Fallback.
            }
            """;

        var result = EncounterParser.Parse(source);
        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => !e.IsWarning && (e.Message.Contains("picker check") || e.Message.Contains("@elif after a picker")));
    }

    [Fact]
    public void Error_LoneIfPickerCheck_NoElse()
    {
        // Illegal: lone @if check ... {} with no @else
        var source = """
            Test
            [trigger none]
            Body.
            choices:
            * Try
            @if check bushcraft correct:reroute wrong:push {
            You found a path.
            }
            """;

        var result = EncounterParser.Parse(source);
        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => !e.IsWarning && e.Message.Contains("picker check") && e.Message.Contains("@else"));
    }

    // ── meets <skill> <tier> condition ───────────────────────────────────

    [Fact]
    public void MeetsTierCondition_ParsesCorrectly()
    {
        var source = """
            Test
            [trigger none]
            Body.
            choices:
            * Use skill
            @if meets negotiation trained {
            You have the skill.
            } @else {
            You lack the training.
            }
            """;

        var result = EncounterParser.Parse(source);
        Assert.True(result.IsSuccess, string.Join("; ", result.Errors.Select(e => e.Message)));

        var branch = result.Encounter!.Choices[0].Conditional!.Branches[0];
        Assert.Equal("meets negotiation trained", branch.Condition);
        Assert.False(branch.IsPickerCheck);
    }

    [Fact]
    public void MeetsTierRequires_ExpertForm_ParsesCorrectly()
    {
        var source = """
            Test
            [trigger none]
            Body.
            choices:
            * Open the vault [requires meets cunning expert]
            The vault yields to your expertise.
            """;

        var result = EncounterParser.Parse(source);
        Assert.True(result.IsSuccess, string.Join("; ", result.Errors.Select(e => e.Message)));
        Assert.Equal("meets cunning expert", result.Encounter!.Choices[0].Requires);
    }

    // ── Bundle JSON round-trip with picker fields ────────────────────────

    [Fact]
    public void BundleRoundtrip_PickerBranch_PreservesFields()
    {
        const string json = """
        {
            "index": {
                "byId": { "test/test": { "category": "test", "encounterIndex": 0 } },
                "byCategory": { "test": [0] }
            },
            "encounters": [{
                "id": "test/test",
                "category": "test",
                "title": "Test",
                "body": "Body.",
                "choices": [{
                    "optionText": "Try to reason",
                    "optionLink": null,
                    "optionPreview": null,
                    "requires": null,
                    "conditional": {
                        "preamble": "They eye you suspiciously.",
                        "branches": [
                            {
                                "condition": "check negotiation correct:reason wrong:threaten",
                                "branchKind": "picker",
                                "pickerSkill": "negotiation",
                                "pickerCorrect": "reason",
                                "pickerWrong": "threaten",
                                "text": "You make a compelling case.",
                                "mechanics": []
                            }
                        ],
                        "fallback": { "text": "They don't buy it.", "mechanics": [] }
                    },
                    "single": null
                }]
            }]
        }
        """;

        var bundle = EncounterBundle.FromJson(json);
        var choice = bundle.GetById("test/test")!.Choices[0];
        var branch = choice.Conditional!.Branches[0];

        Assert.True(branch.IsPickerCheck);
        Assert.Equal("negotiation", branch.PickerSkill);
        Assert.Equal("reason", branch.PickerCorrect);
        Assert.Equal("threaten", branch.PickerWrong);
        Assert.Equal("check negotiation correct:reason wrong:threaten", branch.Condition);
    }

    [Fact]
    public void BundleRoundtrip_StaticBranch_NoPickerFields()
    {
        const string json = """
        {
            "index": {
                "byId": { "test/test": { "category": "test", "encounterIndex": 0 } },
                "byCategory": { "test": [0] }
            },
            "encounters": [{
                "id": "test/test",
                "category": "test",
                "title": "Test",
                "body": "Body.",
                "choices": [{
                    "optionText": "Check your key",
                    "optionLink": null,
                    "optionPreview": null,
                    "requires": null,
                    "conditional": {
                        "preamble": "",
                        "branches": [
                            {
                                "condition": "has rusted_key",
                                "branchKind": "static",
                                "text": "The key fits.",
                                "mechanics": []
                            }
                        ],
                        "fallback": { "text": "No key.", "mechanics": [] }
                    },
                    "single": null
                }]
            }]
        }
        """;

        var bundle = EncounterBundle.FromJson(json);
        var branch = bundle.GetById("test/test")!.Choices[0].Conditional!.Branches[0];
        Assert.False(branch.IsPickerCheck);
        Assert.Null(branch.PickerCorrect);
        Assert.Null(branch.PickerWrong);
    }

    [Fact]
    public void BundleRoundtrip_LegacyBundleWithoutPickerFields_Tolerates()
    {
        // Legacy bundles without branchKind/picker fields should load without error
        const string json = """
        {
            "index": {
                "byId": { "test/test": { "category": "test", "encounterIndex": 0 } },
                "byCategory": { "test": [0] }
            },
            "encounters": [{
                "id": "test/test",
                "category": "test",
                "title": "Test",
                "body": "Body.",
                "choices": [{
                    "optionText": "Fight",
                    "optionLink": null,
                    "optionPreview": null,
                    "requires": null,
                    "conditional": {
                        "preamble": "",
                        "branches": [
                            { "condition": "check combat hard", "text": "Victory!", "mechanics": [] }
                        ],
                        "fallback": { "text": "Defeat.", "mechanics": [] }
                    },
                    "single": null
                }]
            }]
        }
        """;

        var bundle = EncounterBundle.FromJson(json);
        var branch = bundle.GetById("test/test")!.Choices[0].Conditional!.Branches[0];
        // No crash; picker fields are null (missing = not a picker check)
        Assert.False(branch.IsPickerCheck);
        Assert.Equal("check combat hard", branch.Condition);
    }
}
