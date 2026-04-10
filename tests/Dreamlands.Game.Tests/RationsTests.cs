using Dreamlands.Game;
using Dreamlands.Rules;

namespace Dreamlands.Game.Tests;

public class RationsTests
{
    static readonly BalanceData Balance = BalanceData.Default;
    const string RationName = "Rations (test)";

    static PlayerState Fresh()
    {
        var p = PlayerState.NewGame("test", 99, Balance);
        p.Gold = 999; // plenty of gold for refill tests; cost-limited cases set explicitly
        return p;
    }

    static int RationCost => Balance.Items[Rations.RationDefId].Cost ?? 0;

    [Fact]
    public void Refill_Empty_FillsHaversackToCapacityAndCharges()
    {
        var p = Fresh();
        var goldBefore = p.Gold;
        var result = Rations.Refill(p, Balance, () => RationName);

        Assert.Equal(p.HaversackCapacity, result.Added);
        Assert.Equal(p.HaversackCapacity * RationCost, result.GoldSpent);
        Assert.Equal(goldBefore - result.GoldSpent, p.Gold);
        Assert.Equal(p.HaversackCapacity, p.Haversack.Count);
        Assert.True(p.Haversack.All(i => i.DefId == Rations.RationDefId));
    }

    [Fact]
    public void Refill_PartiallyFilled_TopsUpRemainingSlots()
    {
        var p = Fresh();
        // Pre-load with 3 non-rations (trinkets, keys, etc.)
        for (int i = 0; i < 3; i++)
            p.Haversack.Add(new ItemInstance("trinket", "Trinket"));

        var result = Rations.Refill(p, Balance, () => RationName);

        Assert.Equal(p.HaversackCapacity - 3, result.Added);
        Assert.Equal(p.HaversackCapacity, p.Haversack.Count);
    }

    [Fact]
    public void Refill_Full_AddsZero()
    {
        var p = Fresh();
        for (int i = 0; i < p.HaversackCapacity; i++)
            p.Haversack.Add(new ItemInstance("trinket", "Trinket"));

        var goldBefore = p.Gold;
        var result = Rations.Refill(p, Balance, () => RationName);

        Assert.Equal(0, result.Added);
        Assert.Equal(0, result.GoldSpent);
        Assert.Equal(goldBefore, p.Gold);
        Assert.Equal(p.HaversackCapacity, p.Haversack.Count);
        Assert.True(p.Haversack.All(i => i.DefId == "trinket"));
    }

    [Fact]
    public void Refill_DoesNotDisplaceExistingItems()
    {
        var p = Fresh();
        p.Haversack.Add(new ItemInstance("dungeon_key", "Brass Key"));
        p.Haversack.Add(new ItemInstance("ivory_comb", "Ivory Comb"));

        Rations.Refill(p, Balance, () => RationName);

        Assert.Contains(p.Haversack, i => i.DefId == "dungeon_key");
        Assert.Contains(p.Haversack, i => i.DefId == "ivory_comb");
    }

    [Fact]
    public void Refill_Idempotent()
    {
        var p = Fresh();
        Rations.Refill(p, Balance, () => RationName);
        var second = Rations.Refill(p, Balance, () => RationName);

        Assert.Equal(0, second.Added);
        Assert.Equal(0, second.GoldSpent);
    }

    [Fact]
    public void Refill_AfterConsumingSome_TopsBackUp()
    {
        var p = Fresh();
        Rations.Refill(p, Balance, () => RationName);

        // Simulate eating 4 rations
        for (int i = 0; i < 4; i++)
            p.Haversack.RemoveAt(p.Haversack.FindIndex(it => it.DefId == Rations.RationDefId));

        var result = Rations.Refill(p, Balance, () => RationName);

        Assert.Equal(4, result.Added);
        Assert.Equal(p.HaversackCapacity, p.Haversack.Count);
    }

    [Fact]
    public void Refill_LimitedByGold_OnlyBuysWhatPlayerCanAfford()
    {
        var p = Fresh();
        p.Gold = RationCost * 3 + 1; // enough for 3 rations, with 1g left over

        var result = Rations.Refill(p, Balance, () => RationName);

        Assert.Equal(3, result.Added);
        Assert.Equal(RationCost * 3, result.GoldSpent);
        Assert.Equal(1, p.Gold);
    }

    [Fact]
    public void Refill_NoGold_AddsNothing()
    {
        var p = Fresh();
        p.Gold = 0;

        var result = Rations.Refill(p, Balance, () => RationName);

        Assert.Equal(0, result.Added);
        Assert.Equal(0, result.GoldSpent);
        Assert.Empty(p.Haversack);
    }
}
