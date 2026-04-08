using Dreamlands.Game;
using Dreamlands.Rules;

namespace Dreamlands.Game.Tests;

public class InnTests
{
    static readonly BalanceData Balance = BalanceData.Default;

    static PlayerState Fresh() => PlayerState.NewGame("test", 99, Balance);

    // ── GetServiceOptions ──

    [Fact]
    public void GetServiceOptions_ReturnsThreeTiers()
    {
        var services = Inn.GetServiceOptions(Balance);

        Assert.Equal(3, services.Count);
        Assert.Contains(services, s => s.Id == Inn.BedServiceId);
        Assert.Contains(services, s => s.Id == Inn.BathServiceId);
        Assert.Contains(services, s => s.Id == Inn.FullServiceId);
    }

    [Fact]
    public void GetServiceOptions_PricesMatchBalance()
    {
        var services = Inn.GetServiceOptions(Balance);
        var bed = services.Single(s => s.Id == Inn.BedServiceId);
        var bath = services.Single(s => s.Id == Inn.BathServiceId);
        var full = services.Single(s => s.Id == Inn.FullServiceId);

        Assert.Equal(Balance.Character.InnBedCost, bed.Cost);
        Assert.Equal(Balance.Character.InnBedSpirits, bed.Spirits);
        Assert.False(bed.RestoresFull);

        Assert.Equal(Balance.Character.InnBathCost, bath.Cost);
        Assert.Equal(Balance.Character.InnBathSpirits, bath.Spirits);
        Assert.False(bath.RestoresFull);

        Assert.Equal(Balance.Character.InnFullCost, full.Cost);
        Assert.True(full.RestoresFull);
    }

    // ── BookService ──

    [Fact]
    public void BookService_Bed_DeductsGoldRestoresSpirits()
    {
        var p = Fresh();
        p.Spirits = 10;
        var goldBefore = p.Gold;

        var result = Inn.BookService(p, Balance, Inn.BedServiceId);

        Assert.True(result.Success);
        Assert.Equal(Balance.Character.InnBedCost, result.GoldSpent);
        Assert.Equal(goldBefore - Balance.Character.InnBedCost, p.Gold);
        Assert.Equal(10 + Balance.Character.InnBedSpirits, p.Spirits);
    }

    [Fact]
    public void BookService_Bath_DeductsGoldRestoresSpirits()
    {
        var p = Fresh();
        p.Spirits = 5;

        var result = Inn.BookService(p, Balance, Inn.BathServiceId);

        Assert.True(result.Success);
        Assert.Equal(Balance.Character.InnBathCost, result.GoldSpent);
        Assert.Equal(5 + Balance.Character.InnBathSpirits, p.Spirits);
    }

    [Fact]
    public void BookService_Full_RestoresToMaxRegardlessOfStartingValue()
    {
        var p = Fresh();
        p.Spirits = 1;

        var result = Inn.BookService(p, Balance, Inn.FullServiceId);

        Assert.True(result.Success);
        Assert.Equal(Balance.Character.InnFullCost, result.GoldSpent);
        Assert.Equal(p.MaxSpirits, p.Spirits);
    }

    [Fact]
    public void BookService_Full_AtMaxStillCharges()
    {
        var p = Fresh();
        p.Spirits = p.MaxSpirits;

        var result = Inn.BookService(p, Balance, Inn.FullServiceId);

        // Premium tier is a vibe purchase — pays full price even at max
        Assert.True(result.Success);
        Assert.Equal(Balance.Character.InnFullCost, result.GoldSpent);
        Assert.Equal(0, result.SpiritsRestored);
    }

    [Fact]
    public void BookService_BathCappedAtMaxSpirits()
    {
        var p = Fresh();
        p.Spirits = p.MaxSpirits - 3; // Bath restores 10, but only 3 needed

        var result = Inn.BookService(p, Balance, Inn.BathServiceId);

        Assert.Equal(p.MaxSpirits, p.Spirits);
        Assert.Equal(3, result.SpiritsRestored);
    }

    [Fact]
    public void BookService_InsufficientGold_Fails()
    {
        var p = Fresh();
        p.Gold = 0;
        var spiritsBefore = p.Spirits;

        var result = Inn.BookService(p, Balance, Inn.BedServiceId);

        Assert.False(result.Success);
        Assert.Equal("Not enough gold", result.Reason);
        Assert.Equal(0, p.Gold);
        Assert.Equal(spiritsBefore, p.Spirits);
    }

    [Fact]
    public void BookService_UnknownService_Fails()
    {
        var p = Fresh();

        var result = Inn.BookService(p, Balance, "penthouse");

        Assert.False(result.Success);
    }

    [Fact]
    public void BookService_AdvancesOneDay()
    {
        var p = Fresh();
        var dayBefore = p.Day;

        Inn.BookService(p, Balance, Inn.BedServiceId);

        Assert.Equal(dayBefore + 1, p.Day);
    }

    [Fact]
    public void BookService_ConsumesMatchingMedicineForSerious()
    {
        var p = Fresh();
        p.ActiveConditions.Add("injured");
        p.Haversack.Add(new ItemInstance("bandages", "Bandages"));

        var result = Inn.BookService(p, Balance, Inn.BedServiceId);

        Assert.True(result.Success);
        Assert.DoesNotContain("injured", p.ActiveConditions);
        Assert.DoesNotContain(p.Haversack, i => i.DefId == "bandages");
        Assert.Contains("bandages", result.MedicinesConsumed);
    }

    [Fact]
    public void BookService_NoMedicineForSerious_LeavesConditionActive()
    {
        var p = Fresh();
        p.ActiveConditions.Add("injured");

        var result = Inn.BookService(p, Balance, Inn.BedServiceId);

        Assert.True(result.Success); // booking still succeeds
        Assert.Contains("injured", p.ActiveConditions);
        Assert.Empty(result.MedicinesConsumed);
    }
}
