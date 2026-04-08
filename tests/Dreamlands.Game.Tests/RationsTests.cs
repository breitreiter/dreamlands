using Dreamlands.Game;
using Dreamlands.Rules;

namespace Dreamlands.Game.Tests;

public class RationsTests
{
    static readonly BalanceData Balance = BalanceData.Default;
    const string RationName = "Rations (test)";

    static PlayerState Fresh() => PlayerState.NewGame("test", 99, Balance);

    [Fact]
    public void Refill_Empty_FillsHaversackToCapacity()
    {
        var p = Fresh();
        var added = Rations.Refill(p, Balance, RationName);

        Assert.Equal(p.HaversackCapacity, added);
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

        var added = Rations.Refill(p, Balance, RationName);

        Assert.Equal(p.HaversackCapacity - 3, added);
        Assert.Equal(p.HaversackCapacity, p.Haversack.Count);
    }

    [Fact]
    public void Refill_Full_AddsZero()
    {
        var p = Fresh();
        for (int i = 0; i < p.HaversackCapacity; i++)
            p.Haversack.Add(new ItemInstance("trinket", "Trinket"));

        var added = Rations.Refill(p, Balance, RationName);

        Assert.Equal(0, added);
        Assert.Equal(p.HaversackCapacity, p.Haversack.Count);
        Assert.True(p.Haversack.All(i => i.DefId == "trinket"));
    }

    [Fact]
    public void Refill_DoesNotDisplaceExistingItems()
    {
        var p = Fresh();
        p.Haversack.Add(new ItemInstance("dungeon_key", "Brass Key"));
        p.Haversack.Add(new ItemInstance("ivory_comb", "Ivory Comb"));

        Rations.Refill(p, Balance, RationName);

        Assert.Contains(p.Haversack, i => i.DefId == "dungeon_key");
        Assert.Contains(p.Haversack, i => i.DefId == "ivory_comb");
    }

    [Fact]
    public void Refill_Idempotent()
    {
        var p = Fresh();
        Rations.Refill(p, Balance, RationName);
        var addedSecond = Rations.Refill(p, Balance, RationName);

        Assert.Equal(0, addedSecond);
    }

    [Fact]
    public void Refill_AfterConsumingSome_TopsBackUp()
    {
        var p = Fresh();
        Rations.Refill(p, Balance, RationName);

        // Simulate eating 4 rations
        for (int i = 0; i < 4; i++)
            p.Haversack.RemoveAt(p.Haversack.FindIndex(it => it.DefId == Rations.RationDefId));

        var added = Rations.Refill(p, Balance, RationName);

        Assert.Equal(4, added);
        Assert.Equal(p.HaversackCapacity, p.Haversack.Count);
    }
}
