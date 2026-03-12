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
}
