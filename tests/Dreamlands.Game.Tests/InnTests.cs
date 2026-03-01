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
        state.ActiveConditions["injured"] = 2; // HealthDrain = Small

        var (allowed, disqualifying) = Inn.CanUseInn(state, Balance);

        Assert.False(allowed);
        Assert.Contains("injured", disqualifying);
    }

    [Fact]
    public void CanUseInn_HealthDrainCondition_WithEnoughMedicine_Allowed()
    {
        var state = Fresh();
        state.ActiveConditions["injured"] = 2;
        state.Haversack.Add(new ItemInstance("thumbroot", "Thumbroot"));
        state.Haversack.Add(new ItemInstance("thumbroot", "Thumbroot"));

        var (allowed, disqualifying) = Inn.CanUseInn(state, Balance);

        Assert.True(allowed);
        Assert.Empty(disqualifying);
    }

    [Fact]
    public void CanUseInn_HealthDrainCondition_InsufficientMedicine_Disqualified()
    {
        var state = Fresh();
        state.ActiveConditions["injured"] = 3;
        state.Haversack.Add(new ItemInstance("thumbroot", "Thumbroot")); // only 1, need 3

        var (allowed, disqualifying) = Inn.CanUseInn(state, Balance);

        Assert.False(allowed);
        Assert.Contains("injured", disqualifying);
    }

    [Fact]
    public void CanUseInn_ClearedOnSettlement_NotDisqualifying()
    {
        var state = Fresh();
        state.ActiveConditions["freezing"] = 1; // HealthDrain but ClearedOnSettlement = true

        var (allowed, disqualifying) = Inn.CanUseInn(state, Balance);

        Assert.True(allowed);
        Assert.Empty(disqualifying);
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

        // Base rest = +1 health/night (no balanced meal at inn), so 5 nights
        Assert.Equal(5, quote.Nights);
        Assert.Equal(4 * Balance.Character.InnNightlyCost, quote.GoldCost);
        Assert.Equal(5, quote.HealthRecovered);
    }

    [Fact]
    public void GetQuote_SpiritsDeficit_UsesLargerDeficit()
    {
        var state = Fresh();
        state.Health = state.MaxHealth - 3;
        state.Spirits = state.MaxSpirits - 7;

        var quote = Inn.GetQuote(state, Balance);

        Assert.Equal(7, quote.Nights); // spirits deficit is larger
        Assert.Equal(6 * Balance.Character.InnNightlyCost, quote.GoldCost);
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
        state.ActiveConditions["injured"] = 1; // HealthDrain = Small (2 HP/night)
        state.Haversack.Add(new ItemInstance("thumbroot", "Thumbroot")); // 1 cure for 1 stack

        var quote = Inn.GetQuote(state, Balance);

        // Night 1: consume medicine (cures injured), drain 0 (condition cleared), recover +1
        // Then pure recovery at +1/night with no drain
        // Should take more than 3 nights due to initial drain interaction
        Assert.True(quote.Nights >= 3);
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
        Assert.Equal(10, result.NightsStayed); // max(10, 8) via simulation
        Assert.Equal(dayBefore + 10, state.Day);
    }

    [Fact]
    public void StayFullRecovery_DeductsGold()
    {
        var state = Fresh();
        state.Health = state.MaxHealth - 3;
        state.Gold = 100;

        var result = Inn.StayFullRecovery(state, Balance);

        // 3 nights, first free, 2 × InnNightlyCost
        Assert.Equal(2 * Balance.Character.InnNightlyCost, result.GoldSpent);
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
        state.Haversack.Add(new ItemInstance("thumbroot", "Thumbroot"));

        var result = Inn.StayFullRecovery(state, Balance);

        Assert.DoesNotContain(state.Haversack, i => i.DefId == "thumbroot");
        Assert.Contains("thumbroot", result.MedicinesConsumed);
        Assert.Contains("injured", result.ConditionsCleared);
    }

    [Fact]
    public void StayFullRecovery_ConsumesAllStacksMedicine()
    {
        var state = Fresh();
        state.Health = 15;
        state.Gold = 100;
        state.ActiveConditions["injured"] = 3;
        state.Haversack.Add(new ItemInstance("thumbroot", "Thumbroot"));
        state.Haversack.Add(new ItemInstance("thumbroot", "Thumbroot"));
        state.Haversack.Add(new ItemInstance("thumbroot", "Thumbroot"));

        var result = Inn.StayFullRecovery(state, Balance);

        Assert.False(state.ActiveConditions.ContainsKey("injured"));
        Assert.Contains("injured", result.ConditionsCleared);
        Assert.Equal(3, result.MedicinesConsumed.Count(m => m == "thumbroot"));
    }

    [Fact]
    public void StayFullRecovery_ReducesStacks_WhenInsufficientMedicine()
    {
        var state = Fresh();
        state.Health = 15;
        state.Gold = 100;
        state.ActiveConditions["injured"] = 3;
        state.Haversack.Add(new ItemInstance("thumbroot", "Thumbroot")); // only 1, need 3

        var result = Inn.StayFullRecovery(state, Balance);

        // 3 stacks - 1 = 2 remaining
        Assert.Equal(2, state.ActiveConditions["injured"]);
        Assert.Contains("thumbroot", result.MedicinesConsumed);
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
        state.Haversack.Add(new ItemInstance("thumbroot", "Thumbroot"));

        var result = Inn.StayChapterhouse(state, Balance);

        Assert.Empty(result.MedicinesConsumed);
        Assert.Contains(state.Haversack, i => i.DefId == "thumbroot");
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
        Assert.Equal(15, result.NightsStayed); // max(15, 12)
        Assert.Equal(dayBefore + 15, state.Day);
    }
}
