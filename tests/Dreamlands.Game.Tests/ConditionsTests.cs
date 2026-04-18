using Dreamlands.Game;
using Dreamlands.Rules;

namespace Dreamlands.Game.Tests;

public class ConditionsTests
{
    static readonly BalanceData Balance = BalanceData.Default;
    static readonly Random Rng = new(42);

    static PlayerState Fresh() => PlayerState.NewGame("test", 99, Balance);

    [Fact]
    public void Quality_PositiveThreshold_TrueWhenMet()
    {
        var state = Fresh();
        state.Qualities["guild"] = 3;
        Assert.True(Conditions.Evaluate("quality guild 2", state, Balance, Rng));
    }

    [Fact]
    public void Quality_PositiveThreshold_TrueWhenExact()
    {
        var state = Fresh();
        state.Qualities["guild"] = 2;
        Assert.True(Conditions.Evaluate("quality guild 2", state, Balance, Rng));
    }

    [Fact]
    public void Quality_PositiveThreshold_FalseWhenBelow()
    {
        var state = Fresh();
        state.Qualities["guild"] = 1;
        Assert.False(Conditions.Evaluate("quality guild 2", state, Balance, Rng));
    }

    [Fact]
    public void Quality_NegativeThreshold_TrueWhenAtOrBelow()
    {
        var state = Fresh();
        state.Qualities["kesharat"] = -3;
        Assert.True(Conditions.Evaluate("quality kesharat -2", state, Balance, Rng));
    }

    [Fact]
    public void Quality_NegativeThreshold_TrueWhenExact()
    {
        var state = Fresh();
        state.Qualities["kesharat"] = -2;
        Assert.True(Conditions.Evaluate("quality kesharat -2", state, Balance, Rng));
    }

    [Fact]
    public void Quality_NegativeThreshold_FalseWhenAbove()
    {
        var state = Fresh();
        state.Qualities["kesharat"] = -1;
        Assert.False(Conditions.Evaluate("quality kesharat -2", state, Balance, Rng));
    }

    [Fact]
    public void Quality_UnsetKey_DefaultsToZero()
    {
        var state = Fresh();
        Assert.True(Conditions.Evaluate("quality guild 0", state, Balance, Rng));
        Assert.False(Conditions.Evaluate("quality guild 1", state, Balance, Rng));
    }

    [Fact]
    public void Quality_ZeroThreshold_TrueWhenZeroOrPositive()
    {
        var state = Fresh();
        state.Qualities["exiles"] = 0;
        Assert.True(Conditions.Evaluate("quality exiles 0", state, Balance, Rng));
        state.Qualities["exiles"] = 5;
        Assert.True(Conditions.Evaluate("quality exiles 0", state, Balance, Rng));
    }

    [Fact]
    public void Tag_ReturnsTrue_WhenSet()
    {
        var state = Fresh();
        state.Tags.Add("briar_resolved");
        Assert.True(Conditions.Evaluate("tag briar_resolved", state, Balance, Rng));
    }

    [Fact]
    public void Tag_ReturnsFalse_WhenNotSet()
    {
        var state = Fresh();
        Assert.False(Conditions.Evaluate("tag briar_resolved", state, Balance, Rng));
    }

    // ── ! negation ───────────────────────────────────────────────

    [Fact]
    public void Not_Tag_TrueWhenNotSet()
    {
        var state = Fresh();
        Assert.True(Conditions.Evaluate("!tag briar_resolved", state, Balance, Rng));
    }

    [Fact]
    public void Not_Tag_FalseWhenSet()
    {
        var state = Fresh();
        state.Tags.Add("briar_resolved");
        Assert.False(Conditions.Evaluate("!tag briar_resolved", state, Balance, Rng));
    }

    [Fact]
    public void Not_Has_TrueWhenNotOwned()
    {
        var state = Fresh();
        Assert.True(Conditions.Evaluate("!has torch", state, Balance, Rng));
    }

    [Fact]
    public void Not_Quality_NegatesPositiveThreshold()
    {
        var state = Fresh();
        state.Qualities["guild"] = 1;
        Assert.True(Conditions.Evaluate("!quality guild 2", state, Balance, Rng));
        state.Qualities["guild"] = 3;
        Assert.False(Conditions.Evaluate("!quality guild 2", state, Balance, Rng));
    }

    // ── && ───────────────────────────────────────────────────────

    [Fact]
    public void And_TrueWhenBothTrue()
    {
        var state = Fresh();
        state.Tags.Add("met_envoy");
        state.Tags.Add("guild_member");
        Assert.True(Conditions.Evaluate("tag met_envoy && tag guild_member", state, Balance, Rng));
    }

    [Fact]
    public void And_FalseWhenFirstFalse()
    {
        var state = Fresh();
        state.Tags.Add("guild_member");
        Assert.False(Conditions.Evaluate("tag met_envoy && tag guild_member", state, Balance, Rng));
    }

    [Fact]
    public void And_FalseWhenSecondFalse()
    {
        var state = Fresh();
        state.Tags.Add("met_envoy");
        Assert.False(Conditions.Evaluate("tag met_envoy && tag guild_member", state, Balance, Rng));
    }

    // ── || ───────────────────────────────────────────────────────

    [Fact]
    public void Or_TrueWhenFirstTrue()
    {
        var state = Fresh();
        state.Tags.Add("met_envoy");
        Assert.True(Conditions.Evaluate("tag met_envoy || tag guild_member", state, Balance, Rng));
    }

    [Fact]
    public void Or_TrueWhenSecondTrue()
    {
        var state = Fresh();
        state.Tags.Add("guild_member");
        Assert.True(Conditions.Evaluate("tag met_envoy || tag guild_member", state, Balance, Rng));
    }

    [Fact]
    public void Or_FalseWhenBothFalse()
    {
        var state = Fresh();
        Assert.False(Conditions.Evaluate("tag met_envoy || tag guild_member", state, Balance, Rng));
    }

    // ── compound ─────────────────────────────────────────────────

    [Fact]
    public void Compound_NotWithAnd()
    {
        var state = Fresh();
        state.Tags.Add("met_envoy");
        // !tag patrol_alerted is true (tag not set), tag met_envoy is true → true
        Assert.True(Conditions.Evaluate("tag met_envoy && !tag patrol_alerted", state, Balance, Rng));
        state.Tags.Add("patrol_alerted");
        // now !tag patrol_alerted is false → false
        Assert.False(Conditions.Evaluate("tag met_envoy && !tag patrol_alerted", state, Balance, Rng));
    }

    [Fact]
    public void Compound_TagAndQuality()
    {
        var state = Fresh();
        state.Tags.Add("guild_member");
        state.Qualities["guild"] = 3;
        Assert.True(Conditions.Evaluate("tag guild_member && quality guild 2", state, Balance, Rng));
        state.Qualities["guild"] = 1;
        Assert.False(Conditions.Evaluate("tag guild_member && quality guild 2", state, Balance, Rng));
    }

    [Fact]
    public void Compound_OrBindsLooserThanAnd()
    {
        var state = Fresh();
        state.Tags.Add("c");
        // "tag a || tag b && tag c" should parse as "tag a || (tag b && tag c)"
        // tag a = false, tag b = false, tag c = true → false || (false && true) = false
        Assert.False(Conditions.Evaluate("tag a || tag b && tag c", state, Balance, Rng));
        state.Tags.Add("b");
        // tag a = false, tag b = true, tag c = true → false || (true && true) = true
        Assert.True(Conditions.Evaluate("tag a || tag b && tag c", state, Balance, Rng));
    }
}
