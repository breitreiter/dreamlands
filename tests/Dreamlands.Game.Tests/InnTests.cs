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
    public void CanUseInn_HealthDrainCondition_WithoutMedicine_Disqualified()
    {
        var state = Fresh();
        state.ActiveConditions["injured"] = 2; // HealthDrain = Huge

        var (allowed, disqualifying) = Inn.CanUseInn(state, Balance);

        Assert.False(allowed);
        Assert.Contains("injured", disqualifying);
    }

    [Fact]
    public void CanUseInn_HealthDrainCondition_WithEnoughMedicine_Allowed()
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
    public void CanUseInn_HealthDrainCondition_InsufficientMedicine_Disqualified()
    {
        var state = Fresh();
        state.ActiveConditions["injured"] = 3;
        state.Haversack.Add(new ItemInstance("bandages", "Bandages")); // only 1, need 3

        var (allowed, disqualifying) = Inn.CanUseInn(state, Balance);

        Assert.False(allowed);
        Assert.Contains("injured", disqualifying);
    }

    [Fact]
    public void CanUseInn_FreezingNoHealthDrain_NotDisqualifying()
    {
        var state = Fresh();
        state.ActiveConditions["freezing"] = 1; // spirit drain only, no health drain

        var (allowed, disqualifying) = Inn.CanUseInn(state, Balance);

        Assert.True(allowed);
        Assert.Empty(disqualifying);
    }

    [Fact]
    public void CanUseInn_ThirstyNoHealthDrain_NotDisqualifying()
    {
        var state = Fresh();
        state.ActiveConditions["thirsty"] = 1; // spirit drain only, no health drain

        var (allowed, disqualifying) = Inn.CanUseInn(state, Balance);

        Assert.True(allowed);
        Assert.Empty(disqualifying);
    }

    [Fact]
    public void CanUseInn_LatticeSickness_Disqualified()
    {
        var state = Fresh();
        state.ActiveConditions["lattice_sickness"] = 3;

        var (allowed, disqualifying) = Inn.CanUseInn(state, Balance);

        Assert.False(allowed);
        Assert.Contains("lattice_sickness", disqualifying);
    }

    [Fact]
    public void CanUseInn_SpiritsDrainOnly_NotDisqualifying()
    {
        var state = Fresh();
        state.ActiveConditions["exhausted"] = 1; // SpiritsDrain only, no HealthDrain

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
    public void GetQuote_FullHealth_OneNightFree()
    {
        var state = Fresh();

        var quote = Inn.GetQuote(state, Balance);

        Assert.Equal(1, quote.Nights);
        Assert.Equal(0, quote.GoldCost);
        Assert.Equal(0, quote.HealthRecovered);
        Assert.Equal(0, quote.SpiritsRecovered);
    }

    [Fact]
    public void GetQuote_HealthDeficit_CalculatesNightsAndCost()
    {
        var state = Fresh();
        state.Health = state.MaxHealth - 5;

        var quote = Inn.GetQuote(state, Balance);

        // Inn recovery = BaseRest + BalancedMeal = 2 health/night, so ceil(5/2) = 3 nights
        Assert.Equal(3, quote.Nights);
        Assert.Equal(2 * Balance.Character.InnNightlyCost, quote.GoldCost);
        Assert.Equal(5, quote.HealthRecovered);
    }

    [Fact]
    public void GetQuote_SpiritsDeficit_UsesLargerDeficit()
    {
        var state = Fresh();
        state.Health = state.MaxHealth - 3;
        state.Spirits = state.MaxSpirits - 7;

        var quote = Inn.GetQuote(state, Balance);

        Assert.Equal(4, quote.Nights); // spirits deficit is larger, ceil(7/2) = 4
        Assert.Equal(3 * Balance.Character.InnNightlyCost, quote.GoldCost);
        Assert.Equal(3, quote.HealthRecovered);
        Assert.Equal(7, quote.SpiritsRecovered);
    }

    [Fact]
    public void GetQuote_OneNightDeficit_FreeStay()
    {
        var state = Fresh();
        state.Health = state.MaxHealth - 1;

        var quote = Inn.GetQuote(state, Balance);

        Assert.Equal(1, quote.Nights);
        Assert.Equal(0, quote.GoldCost);
    }

    [Fact]
    public void GetQuote_WithDrain_TakesLonger()
    {
        var state = Fresh();
        state.Health = state.MaxHealth - 3;
        state.ActiveConditions["injured"] = 1; // HealthDrain = Huge (4 HP/night)
        state.Haversack.Add(new ItemInstance("bandages", "Bandages")); // 1 cure for 1 stack

        var quote = Inn.GetQuote(state, Balance);

        // Night 1: consume medicine (cures injured), drain 0 (condition cleared), recover +2
        // Then pure recovery at +2/night with no drain
        // Should take more than 1 night due to initial drain interaction
        Assert.True(quote.Nights >= 2);
    }

    [Fact]
    public void GetQuote_SkipsClearedOnSettlementDrain()
    {
        var state = Fresh();
        state.Health = state.MaxHealth - 4;

        var quoteHealthy = Inn.GetQuote(state, Balance);

        // Add a ClearedOnSettlement condition — should NOT affect night count
        state.ActiveConditions["freezing"] = 1; // SpiritsDrain=Small, ClearedOnSettlement=true

        var quoteWithFreezing = Inn.GetQuote(state, Balance);

        Assert.Equal(quoteHealthy.Nights, quoteWithFreezing.Nights);
        Assert.Equal(quoteHealthy.GoldCost, quoteWithFreezing.GoldCost);
    }

    // ── StayOneNight ──

    [Fact]
    public void StayOneNight_ClearsExhausted()
    {
        var state = Fresh();
        state.ActiveConditions["exhausted"] = 1;
        state.Haversack.Add(new ItemInstance("food_protein", "Meat"));
        state.Haversack.Add(new ItemInstance("food_grain", "Bread"));
        state.Haversack.Add(new ItemInstance("food_sweets", "Sweets"));

        var events = Inn.StayOneNight(state, "plains", 1, Balance, new Random(42));

        Assert.False(state.ActiveConditions.ContainsKey("exhausted"));
        Assert.Contains(events, e => e is EndOfDayEvent.ConditionCured c && c.ConditionId == "exhausted");
    }

    [Fact]
    public void StayOneNight_TriggersEndOfDay()
    {
        var state = Fresh();
        state.Health = 15;
        state.Spirits = 15;
        state.Haversack.Add(new ItemInstance("food_protein", "Meat"));
        state.Haversack.Add(new ItemInstance("food_grain", "Bread"));
        state.Haversack.Add(new ItemInstance("food_sweets", "Sweets"));

        var events = Inn.StayOneNight(state, "plains", 1, Balance, new Random(42));

        // Should have food and rest events from EndOfDay.Resolve
        Assert.Contains(events, e => e is EndOfDayEvent.FoodConsumed);
        Assert.Contains(events, e => e is EndOfDayEvent.RestRecovery);
    }

    // ── StayFullRecovery ──

    [Fact]
    public void StayFullRecovery_RestoresToMax()
    {
        var state = Fresh();
        state.Health = 10;
        state.Spirits = 12;
        state.Gold = 100;
        var dayBefore = state.Day;

        var result = Inn.StayFullRecovery(state, Balance);

        Assert.Equal(state.MaxHealth, state.Health);
        Assert.Equal(state.MaxSpirits, state.Spirits);
        Assert.Equal(10, result.HealthRecovered);
        Assert.Equal(8, result.SpiritsRecovered);
        Assert.Equal(5, result.NightsStayed); // max(ceil(10/2), ceil(8/2)) = 5 at 2/night
        Assert.Equal(dayBefore + 5, state.Day);
    }

    [Fact]
    public void StayFullRecovery_DeductsGold()
    {
        var state = Fresh();
        state.Health = state.MaxHealth - 3;
        state.Gold = 100;

        var result = Inn.StayFullRecovery(state, Balance);

        // ceil(3/2) = 2 nights, first free, 1 × InnNightlyCost
        Assert.Equal(1 * Balance.Character.InnNightlyCost, result.GoldSpent);
        Assert.Equal(100 - result.GoldSpent, state.Gold);
    }

    [Fact]
    public void StayFullRecovery_ClearsExhausted()
    {
        var state = Fresh();
        state.Health = 15;
        state.Gold = 100;
        state.ActiveConditions["exhausted"] = 1;

        var result = Inn.StayFullRecovery(state, Balance);

        Assert.False(state.ActiveConditions.ContainsKey("exhausted"));
        Assert.Contains("exhausted", result.ConditionsCleared);
    }

    [Fact]
    public void StayFullRecovery_ConsumesMedicine()
    {
        var state = Fresh();
        state.Health = 15;
        state.Gold = 100;
        state.ActiveConditions["injured"] = 1;
        state.Haversack.Add(new ItemInstance("bandages", "Bandages"));

        var result = Inn.StayFullRecovery(state, Balance);

        Assert.DoesNotContain(state.Haversack, i => i.DefId == "bandages");
        Assert.Contains("bandages", result.MedicinesConsumed);
        Assert.Contains("injured", result.ConditionsCleared);
    }

    [Fact]
    public void StayFullRecovery_ConsumesAllStacksMedicine()
    {
        var state = Fresh();
        state.Health = 15;
        state.Gold = 100;
        state.ActiveConditions["injured"] = 3;
        state.Haversack.Add(new ItemInstance("bandages", "Bandages"));
        state.Haversack.Add(new ItemInstance("bandages", "Bandages"));
        state.Haversack.Add(new ItemInstance("bandages", "Bandages"));

        var result = Inn.StayFullRecovery(state, Balance);

        Assert.False(state.ActiveConditions.ContainsKey("injured"));
        Assert.Contains("injured", result.ConditionsCleared);
        Assert.Equal(3, result.MedicinesConsumed.Count(m => m == "bandages"));
    }

    [Fact]
    public void StayFullRecovery_ReducesStacks_WhenInsufficientMedicine()
    {
        var state = Fresh();
        state.Health = 15;
        state.Gold = 100;
        state.ActiveConditions["injured"] = 3;
        state.Haversack.Add(new ItemInstance("bandages", "Bandages")); // only 1, need 3

        var result = Inn.StayFullRecovery(state, Balance);

        // 3 stacks - 1 = 2 remaining
        Assert.Equal(2, state.ActiveConditions["injured"]);
        Assert.Contains("bandages", result.MedicinesConsumed);
        Assert.DoesNotContain("injured", result.ConditionsCleared);
    }

    [Fact]
    public void StayFullRecovery_ClearsClearedOnSettlement()
    {
        var state = Fresh();
        state.Health = 15;
        state.Gold = 100;
        state.ActiveConditions["freezing"] = 1; // ClearedOnSettlement = true

        var result = Inn.StayFullRecovery(state, Balance);

        Assert.False(state.ActiveConditions.ContainsKey("freezing"));
        Assert.Contains("freezing", result.ConditionsCleared);
    }

    // ── StayChapterhouse ──

    [Fact]
    public void StayChapterhouse_ClearsAllConditions()
    {
        var state = Fresh();
        state.Health = 10;
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
        state.Health = 10;
        state.Gold = 50;

        var result = Inn.StayChapterhouse(state, Balance);

        Assert.Equal(0, result.GoldSpent);
        Assert.Equal(50, state.Gold);
    }

    [Fact]
    public void StayChapterhouse_NoMedicineConsumed()
    {
        var state = Fresh();
        state.Health = 15;
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
        state.Health = 5;
        state.Spirits = 8;
        var dayBefore = state.Day;

        var result = Inn.StayChapterhouse(state, Balance);

        Assert.Equal(state.MaxHealth, state.Health);
        Assert.Equal(state.MaxSpirits, state.Spirits);
        Assert.Equal(15, result.HealthRecovered);
        Assert.Equal(12, result.SpiritsRecovered);
        Assert.Equal(8, result.NightsStayed); // max(ceil(15/2), ceil(12/2)) = 8 at 2/night
        Assert.Equal(dayBefore + 8, state.Day);
    }
}
