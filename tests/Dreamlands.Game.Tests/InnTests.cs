using Dreamlands.Game;
using Dreamlands.Rules;

namespace Dreamlands.Game.Tests;

public class InnTests
{
    static readonly BalanceData Balance = BalanceData.Default;

    static PlayerState Fresh() => PlayerState.NewGame("test", 99, Balance);

    // ── CanUseInn ──

    [Fact]
    public void CanUseInn_Healthy_Allowed()
    {
        var state = Fresh();
        var (allowed, disqualifying) = Inn.CanUseInn(state, Balance);

        Assert.True(allowed);
        Assert.Empty(disqualifying);
    }

    [Fact]
    public void CanUseInn_SevereCondition_WithoutMedicine_Disqualified()
    {
        var state = Fresh();
        state.ActiveConditions["injured"] = 2;

        var (allowed, disqualifying) = Inn.CanUseInn(state, Balance);

        Assert.False(allowed);
        Assert.Contains("injured", disqualifying);
    }

    [Fact]
    public void CanUseInn_SevereCondition_WithEnoughMedicine_Allowed()
    {
        var state = Fresh();
        state.ActiveConditions["injured"] = 2;
        state.Haversack.Add(new ItemInstance("bandages", "Bandages"));
        state.Haversack.Add(new ItemInstance("bandages", "Bandages"));

        var (allowed, disqualifying) = Inn.CanUseInn(state, Balance);

        Assert.True(allowed);
        Assert.Empty(disqualifying);
    }

    [Fact]
    public void CanUseInn_SevereCondition_InsufficientMedicine_Disqualified()
    {
        var state = Fresh();
        state.ActiveConditions["injured"] = 3;
        state.Haversack.Add(new ItemInstance("bandages", "Bandages")); // only 1, need 3

        var (allowed, disqualifying) = Inn.CanUseInn(state, Balance);

        Assert.False(allowed);
        Assert.Contains("injured", disqualifying);
    }

    [Fact]
    public void CanUseInn_MinorCondition_NotDisqualifying()
    {
        var state = Fresh();
        state.ActiveConditions["freezing"] = 1;

        var (allowed, disqualifying) = Inn.CanUseInn(state, Balance);

        Assert.True(allowed);
        Assert.Empty(disqualifying);
    }

    [Fact]
    public void CanUseInn_ExhaustedNotDisqualifying()
    {
        var state = Fresh();
        state.ActiveConditions["exhausted"] = 1;

        var (allowed, disqualifying) = Inn.CanUseInn(state, Balance);

        Assert.True(allowed);
        Assert.Empty(disqualifying);
    }

    [Fact]
    public void CanUseInn_MultipleConditions_ReportsAll()
    {
        var state = Fresh();
        state.ActiveConditions["injured"] = 2;
        state.ActiveConditions["poisoned"] = 1;

        var (allowed, disqualifying) = Inn.CanUseInn(state, Balance);

        Assert.False(allowed);
        Assert.Contains("injured", disqualifying);
        Assert.Contains("poisoned", disqualifying);
    }

    // ── GetQuote ──

    [Fact]
    public void GetQuote_FullHealth_OneNightMinimum()
    {
        var state = Fresh();

        var quote = Inn.GetQuote(state, Balance);

        Assert.Equal(1, quote.Nights);
        Assert.Equal(Balance.Character.InnNightlyCost, quote.GoldCost);
        Assert.Equal(0, quote.HealthRecovered);
        Assert.Equal(0, quote.SpiritsRecovered);
    }

    [Fact]
    public void GetQuote_HealthDeficit_CalculatesNightsAndCost()
    {
        var state = Fresh();
        state.Health = 1; // deficit of 3

        var quote = Inn.GetQuote(state, Balance);

        // Health recovers 1/night, so 3 nights for deficit of 3
        Assert.Equal(3, quote.Nights);
        Assert.Equal(3 * Balance.Character.InnNightlyCost, quote.GoldCost);
        Assert.Equal(3, quote.HealthRecovered);
    }

    [Fact]
    public void GetQuote_SpiritsDeficit_UsesLargerDeficit()
    {
        var state = Fresh();
        state.Health = 2; // health deficit = 2
        state.Spirits = 10; // spirits deficit = 10, ceil(10/2) = 5

        var quote = Inn.GetQuote(state, Balance);

        Assert.Equal(5, quote.Nights); // spirits deficit is larger
        Assert.Equal(5 * Balance.Character.InnNightlyCost, quote.GoldCost);
        Assert.Equal(2, quote.HealthRecovered);
        Assert.Equal(10, quote.SpiritsRecovered);
    }

    [Fact]
    public void GetQuote_OddSpiritsDeficit_CeilsDivision()
    {
        var state = Fresh();
        state.Spirits = 13; // deficit of 7, ceil(7/2) = 4

        var quote = Inn.GetQuote(state, Balance);

        Assert.Equal(4, quote.Nights);
    }

    // ── StayAtInn ──

    [Fact]
    public void StayAtInn_RestoresToMax()
    {
        var state = Fresh();
        state.Health = 1;
        state.Spirits = 12;
        state.Gold = 100;
        var dayBefore = state.Day;

        var result = Inn.StayAtInn(state, Balance);

        Assert.Equal(state.MaxHealth, state.Health);
        Assert.Equal(state.MaxSpirits, state.Spirits);
        Assert.Equal(3, result.HealthRecovered); // 4-1
        Assert.Equal(8, result.SpiritsRecovered); // 20-12
    }

    [Fact]
    public void StayAtInn_DeductsGold()
    {
        var state = Fresh();
        state.Health = 2; // deficit 2, so 2 nights
        state.Gold = 100;

        var result = Inn.StayAtInn(state, Balance);

        Assert.Equal(2 * Balance.Character.InnNightlyCost, result.GoldSpent);
        Assert.Equal(100 - result.GoldSpent, state.Gold);
    }

    [Fact]
    public void StayAtInn_ClearsExhausted()
    {
        var state = Fresh();
        state.Gold = 100;
        state.ActiveConditions["exhausted"] = 1;

        var result = Inn.StayAtInn(state, Balance);

        Assert.False(state.ActiveConditions.ContainsKey("exhausted"));
        Assert.Contains("exhausted", result.ConditionsCleared);
    }

    [Fact]
    public void StayAtInn_ConsumesMedicine()
    {
        var state = Fresh();
        state.Gold = 100;
        state.ActiveConditions["injured"] = 1;
        state.Haversack.Add(new ItemInstance("bandages", "Bandages"));

        var result = Inn.StayAtInn(state, Balance);

        Assert.DoesNotContain(state.Haversack, i => i.DefId == "bandages");
        Assert.Contains("bandages", result.MedicinesConsumed);
        Assert.Contains("injured", result.ConditionsCleared);
    }

    [Fact]
    public void StayAtInn_ConsumesAllStacksMedicine()
    {
        var state = Fresh();
        state.Gold = 100;
        state.ActiveConditions["injured"] = 3;
        state.Haversack.Add(new ItemInstance("bandages", "Bandages"));
        state.Haversack.Add(new ItemInstance("bandages", "Bandages"));
        state.Haversack.Add(new ItemInstance("bandages", "Bandages"));

        var result = Inn.StayAtInn(state, Balance);

        Assert.False(state.ActiveConditions.ContainsKey("injured"));
        Assert.Contains("injured", result.ConditionsCleared);
        Assert.Equal(3, result.MedicinesConsumed.Count(m => m == "bandages"));
    }

    [Fact]
    public void StayAtInn_ClearsClearedOnSettlement()
    {
        var state = Fresh();
        state.Gold = 100;
        state.ActiveConditions["freezing"] = 1; // ClearedOnSettlement = true

        var result = Inn.StayAtInn(state, Balance);

        Assert.False(state.ActiveConditions.ContainsKey("freezing"));
        Assert.Contains("freezing", result.ConditionsCleared);
    }

    [Fact]
    public void StayAtInn_AdvancesDays()
    {
        var state = Fresh();
        state.Health = 1; // deficit 3, so 3 nights
        state.Gold = 100;
        var dayBefore = state.Day;

        Inn.StayAtInn(state, Balance);

        Assert.Equal(dayBefore + 3, state.Day);
    }

    // ── StayChapterhouse ──

    [Fact]
    public void StayChapterhouse_ClearsAllConditions()
    {
        var state = Fresh();
        state.Health = 1;
        state.Spirits = 12;
        state.Gold = 50;
        state.ActiveConditions["injured"] = 3;
        state.ActiveConditions["poisoned"] = 2;
        state.ActiveConditions["exhausted"] = 1;

        var result = Inn.StayChapterhouse(state, Balance);

        Assert.Empty(state.ActiveConditions);
        Assert.Contains("injured", result.ConditionsCleared);
        Assert.Contains("poisoned", result.ConditionsCleared);
        Assert.Contains("exhausted", result.ConditionsCleared);
    }

    [Fact]
    public void StayChapterhouse_NoGoldCost()
    {
        var state = Fresh();
        state.Health = 1;
        state.Gold = 50;

        var result = Inn.StayChapterhouse(state, Balance);

        Assert.Equal(0, result.GoldSpent);
        Assert.Equal(50, state.Gold);
    }

    [Fact]
    public void StayChapterhouse_NoMedicineConsumed()
    {
        var state = Fresh();
        state.ActiveConditions["injured"] = 1;
        state.Haversack.Add(new ItemInstance("bandages", "Bandages"));

        var result = Inn.StayChapterhouse(state, Balance);

        Assert.Empty(result.MedicinesConsumed);
        Assert.Contains(state.Haversack, i => i.DefId == "bandages");
        Assert.False(state.ActiveConditions.ContainsKey("injured"));
    }

    [Fact]
    public void StayChapterhouse_RestoresToMax()
    {
        var state = Fresh();
        state.Health = 1;
        state.Spirits = 8;
        var dayBefore = state.Day;

        var result = Inn.StayChapterhouse(state, Balance);

        Assert.Equal(state.MaxHealth, state.Health);
        Assert.Equal(state.MaxSpirits, state.Spirits);
        Assert.Equal(3, result.HealthRecovered); // 4-1
        Assert.Equal(12, result.SpiritsRecovered); // 20-8
        Assert.Equal(6, result.NightsStayed); // max(3, ceil(12/2)=6) = 6
        Assert.Equal(dayBefore + 6, state.Day);
    }
}
