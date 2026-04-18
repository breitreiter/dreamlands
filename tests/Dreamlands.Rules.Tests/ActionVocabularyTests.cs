using Dreamlands.Rules;

namespace Dreamlands.Rules.Tests;

public class ActionVocabularyTests
{
    static string? Ok(string condition) => ActionVerb.Validate(condition, VerbUsage.Condition);

    // ── Atomic conditions ────────────────────────────────────────

    [Fact]
    public void Atomic_Tag_Valid() => Assert.Null(Ok("tag briar_resolved"));

    [Fact]
    public void Atomic_Has_Valid() => Assert.Null(Ok("has torch"));

    [Fact]
    public void Atomic_Quality_Valid() => Assert.Null(Ok("quality guild 2"));

    [Fact]
    public void Atomic_Quality_NegativeThreshold_Valid() => Assert.Null(Ok("quality kesharat -2"));

    [Fact]
    public void Atomic_Check_Valid() => Assert.Null(Ok("check combat hard"));

    [Fact]
    public void Atomic_Meets_Valid() => Assert.Null(Ok("meets combat 3"));

    [Fact]
    public void Atomic_UnknownVerb_ReturnsError()
    {
        var err = Ok("foobar thing");
        Assert.NotNull(err);
        Assert.Contains("foobar", err);
    }

    [Fact]
    public void Atomic_MechanicUsedAsCondition_ReturnsError()
    {
        var err = Ok("add_tag some_flag");
        Assert.NotNull(err);
        Assert.Contains("mechanic", err);
    }

    [Fact]
    public void Atomic_WrongArgType_ReturnsError()
    {
        var err = Ok("check notaskill hard");
        Assert.NotNull(err);
        Assert.Contains("skill", err);
    }

    [Fact]
    public void Atomic_TooFewArgs_ReturnsError()
    {
        var err = Ok("check combat");
        Assert.NotNull(err);
        Assert.Contains("expects", err);
    }

    // ── ! negation ───────────────────────────────────────────────

    [Fact]
    public void Not_Tag_Valid() => Assert.Null(Ok("!tag briar_resolved"));

    [Fact]
    public void Not_Has_Valid() => Assert.Null(Ok("!has torch"));

    [Fact]
    public void Not_Quality_Valid() => Assert.Null(Ok("!quality guild 2"));

    [Fact]
    public void Not_Check_ReturnsError()
    {
        var err = Ok("!check combat hard");
        Assert.NotNull(err);
        Assert.Contains("check", err);
    }

    [Fact]
    public void Not_Meets_ReturnsError()
    {
        var err = Ok("!meets combat 3");
        Assert.NotNull(err);
        Assert.Contains("meets", err);
    }

    // ── && compound ──────────────────────────────────────────────

    [Fact]
    public void And_TwoTags_Valid() => Assert.Null(Ok("tag met_envoy && tag guild_member"));

    [Fact]
    public void And_TagAndHas_Valid() => Assert.Null(Ok("tag explored && has torch"));

    [Fact]
    public void And_TagAndQuality_Valid() => Assert.Null(Ok("tag guild_member && quality guild 2"));

    [Fact]
    public void And_WithNot_Valid() => Assert.Null(Ok("tag met_envoy && !tag patrol_alerted"));

    [Fact]
    public void And_CheckProhibited_ReturnsError()
    {
        var err = Ok("check combat hard && tag explored");
        Assert.NotNull(err);
        Assert.Contains("check", err);
    }

    [Fact]
    public void And_MeetsProhibited_ReturnsError()
    {
        var err = Ok("tag explored && meets combat 3");
        Assert.NotNull(err);
        Assert.Contains("meets", err);
    }

    // ── || compound ──────────────────────────────────────────────

    [Fact]
    public void Or_TwoTags_Valid() => Assert.Null(Ok("tag guild_member || tag kesharat_contact"));

    [Fact]
    public void Or_TagAndQuality_Valid() => Assert.Null(Ok("tag guild_member || quality guild 2"));

    [Fact]
    public void Or_CheckProhibited_ReturnsError()
    {
        var err = Ok("tag explored || check combat hard");
        Assert.NotNull(err);
        Assert.Contains("check", err);
    }

    // ── malformed expressions ────────────────────────────────────

    [Fact]
    public void Operator_AtStart_ReturnsError()
    {
        var err = Ok("&& tag foo");
        Assert.NotNull(err);
    }

    [Fact]
    public void Operator_AtEnd_ReturnsError()
    {
        var err = Ok("tag foo &&");
        Assert.NotNull(err);
    }

    [Fact]
    public void MissingArgBeforeOperator_ReturnsError()
    {
        var err = Ok("quality guild && tag foo");
        Assert.NotNull(err);
        Assert.Contains("quality", err);
    }

    [Fact]
    public void Empty_ReturnsError()
    {
        var err = Ok("");
        Assert.NotNull(err);
    }
}
