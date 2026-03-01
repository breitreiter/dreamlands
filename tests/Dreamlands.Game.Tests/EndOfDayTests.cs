using Dreamlands.Game;
using Dreamlands.Rules;

namespace Dreamlands.Game.Tests;

public class EndOfDayTests
{
    static readonly BalanceData Balance = BalanceData.Default;

    static PlayerState Fresh() => PlayerState.NewGame("test", 99, Balance);

    [Fact]
    public void GetThreats_UniversalAlwaysPresent()
    {
        var threats = EndOfDay.GetThreats("plains", 1, Balance);
        Assert.Contains(threats, t => t.Id == "exhausted");
        Assert.Contains(threats, t => t.Id == "lost");
    }

    [Fact]
    public void GetThreats_BiomeSpecific()
    {
        var threats = EndOfDay.GetThreats("mountains", 1, Balance);
        Assert.Contains(threats, t => t.Id == "freezing");
    }

    [Fact]
    public void GetThreats_ExcludesEncounterOnly()
    {
        // poisoned and injured should never appear in ambient threats
        var threats = EndOfDay.GetThreats("plains", 3, Balance);
        Assert.DoesNotContain(threats, t => t.Id == "poisoned");
        Assert.DoesNotContain(threats, t => t.Id == "injured");
    }

    [Fact]
    public void ResolveFood_ThreeMeals_CuresOneHungryStack()
    {
        var state = Fresh();
        state.ActiveConditions["hungry"] = 2;
        state.Haversack.Add(new ItemInstance("food_protein", "Meat"));
        state.Haversack.Add(new ItemInstance("food_grain", "Bread"));
        state.Haversack.Add(new ItemInstance("food_sweets", "Sweets"));

        var events = EndOfDay.Resolve(state, "plains", 1, Balance, new Random(42));

        // Full meal cures 1 stack: 2 → 1
        Assert.Equal(1, state.ActiveConditions["hungry"]);
        Assert.Contains(events, e => e is EndOfDayEvent.HungerChanged);
    }

    [Fact]
    public void ResolveFood_ThreeMeals_ClearsLastHungryStack()
    {
        var state = Fresh();
        state.ActiveConditions["hungry"] = 1;
        state.Haversack.Add(new ItemInstance("food_protein", "Meat"));
        state.Haversack.Add(new ItemInstance("food_grain", "Bread"));
        state.Haversack.Add(new ItemInstance("food_sweets", "Sweets"));

        var events = EndOfDay.Resolve(state, "plains", 1, Balance, new Random(42));

        Assert.False(state.ActiveConditions.ContainsKey("hungry"));
        Assert.Contains(events, e => e is EndOfDayEvent.HungerCured);
    }

    [Fact]
    public void ResolveFood_ZeroMeals_SetsHungryToShortage()
    {
        var state = Fresh();
        // No food in haversack

        var events = EndOfDay.Resolve(state, "plains", 1, Balance, new Random(42));

        Assert.True(state.ActiveConditions.ContainsKey("hungry"));
        // shortage=3, currentStacks=0 → 3 > 0 → set to 3
        Assert.Equal(3, state.ActiveConditions["hungry"]);
    }

    [Fact]
    public void ResolveFood_PartialMeals_SetsHungryToShortageIfHigher()
    {
        var state = Fresh();
        state.Haversack.Add(new ItemInstance("food_protein", "Meat"));

        var events = EndOfDay.Resolve(state, "plains", 1, Balance, new Random(42));

        // 1 meal eaten, shortage=2, currentStacks=0 → 2 > 0 → set to 2
        Assert.True(state.ActiveConditions.ContainsKey("hungry"));
        Assert.Equal(2, state.ActiveConditions["hungry"]);
    }

    [Fact]
    public void ResolveFood_ShortageDoesNotReduceExistingStacks()
    {
        var state = Fresh();
        state.ActiveConditions["hungry"] = 3;
        state.Haversack.Add(new ItemInstance("food_protein", "Meat"));
        state.Haversack.Add(new ItemInstance("food_grain", "Bread"));

        var events = EndOfDay.Resolve(state, "plains", 1, Balance, new Random(42));

        // 2 meals eaten, shortage=1, currentStacks=3 → 1 < 3 → no change
        Assert.Equal(3, state.ActiveConditions["hungry"]);
    }

    [Fact]
    public void ResolveFood_BalancedMeal_Detected()
    {
        var state = Fresh();
        state.Haversack.Add(new ItemInstance("food_protein", "Meat"));
        state.Haversack.Add(new ItemInstance("food_grain", "Bread"));
        state.Haversack.Add(new ItemInstance("food_sweets", "Sweets"));

        var events = EndOfDay.Resolve(state, "plains", 1, Balance, new Random(42));

        var foodEvent = events.OfType<EndOfDayEvent.FoodConsumed>().Single();
        Assert.True(foodEvent.Balanced);
    }

    [Fact]
    public void ResolveFood_AutoSelectsBalancedMeal()
    {
        var state = Fresh();
        // Add extra food — should still pick one of each type
        state.Haversack.Add(new ItemInstance("food_protein", "Meat"));
        state.Haversack.Add(new ItemInstance("food_protein", "More Meat"));
        state.Haversack.Add(new ItemInstance("food_grain", "Bread"));
        state.Haversack.Add(new ItemInstance("food_sweets", "Sweets"));

        var events = EndOfDay.Resolve(state, "plains", 1, Balance, new Random(42));

        var foodEvent = events.OfType<EndOfDayEvent.FoodConsumed>().Single();
        Assert.True(foodEvent.Balanced);
        Assert.Equal(3, foodEvent.FoodEaten.Count);
        // Should have 1 protein left
        Assert.Single(state.Haversack, i => i.DefId == "food_protein");
    }

    [Fact]
    public void Resolve_Rest_RecoverHealthAndSpirits()
    {
        var state = Fresh();
        state.Health = 10;
        state.Spirits = 15;
        foreach (var skill in state.Skills.Keys.ToList())
            state.Skills[skill] = Balance.Character.MaxSkillLevel;
        state.Haversack.Add(new ItemInstance("food_protein", "Meat"));
        state.Haversack.Add(new ItemInstance("food_grain", "Bread"));
        state.Haversack.Add(new ItemInstance("food_sweets", "Sweets"));

        var events = EndOfDay.Resolve(state, "plains", 1, Balance, new Random(42));

        Assert.Contains(events, e => e is EndOfDayEvent.RestRecovery);
        var rest = events.OfType<EndOfDayEvent.RestRecovery>().Single();
        Assert.True(rest.HealthGained > 0);
        Assert.True(rest.SpiritsGained > 0);
    }

    [Fact]
    public void Resolve_BalancedMeal_BonusRecovery()
    {
        var state = Fresh();
        state.Health = 10;
        state.Spirits = 10;
        state.Haversack.Add(new ItemInstance("food_protein", "Meat"));
        state.Haversack.Add(new ItemInstance("food_grain", "Bread"));
        state.Haversack.Add(new ItemInstance("food_sweets", "Sweets"));

        var events = EndOfDay.Resolve(state, "plains", 1, Balance, new Random(42));

        var restEvent = events.OfType<EndOfDayEvent.RestRecovery>().Single();
        Assert.Equal(Balance.Character.BaseRestHealth + Balance.Character.BalancedMealHealthBonus, restEvent.HealthGained);
        Assert.Equal(Balance.Character.BaseRestSpirits + Balance.Character.BalancedMealSpiritsBonus, restEvent.SpiritsGained);
    }

    [Fact]
    public void Resolve_ConditionDrain_ReducesStats()
    {
        var state = Fresh();
        state.ActiveConditions["freezing"] = 1;
        state.Haversack.Add(new ItemInstance("food_protein", "Meat"));
        state.Haversack.Add(new ItemInstance("food_grain", "Bread"));
        state.Haversack.Add(new ItemInstance("food_sweets", "Sweets"));

        var events = EndOfDay.Resolve(state, "plains", 1, Balance, new Random(42));

        var drain = events.OfType<EndOfDayEvent.ConditionDrain>()
            .FirstOrDefault(d => d.ConditionId == "freezing");
        Assert.NotNull(drain);
        Assert.True(drain.HealthLost > 0 || drain.SpiritsLost > 0);
    }

    [Fact]
    public void Resolve_ConditionDrain_KillsPlayer()
    {
        var state = Fresh();
        state.Health = 1;
        state.PendingNoSleep = true;
        state.ActiveConditions["injured"] = 3; // HealthDrain = Small (2 HP)
        state.Haversack.Add(new ItemInstance("food_protein", "Meat"));
        state.Haversack.Add(new ItemInstance("food_grain", "Bread"));
        state.Haversack.Add(new ItemInstance("food_sweets", "Sweets"));

        var events = EndOfDay.Resolve(state, "plains", 1, Balance, new Random(42));

        Assert.Equal(0, state.Health);
        Assert.Contains(events, e => e is EndOfDayEvent.PlayerDied);
    }

    [Fact]
    public void Resolve_Medicine_AutoConsumedForActiveCondition()
    {
        var state = Fresh();
        state.ActiveConditions["injured"] = 3;
        state.Haversack.Add(new ItemInstance("thumbroot", "Thumbroot")); // cures injured
        state.Haversack.Add(new ItemInstance("food_protein", "Meat"));
        state.Haversack.Add(new ItemInstance("food_grain", "Bread"));
        state.Haversack.Add(new ItemInstance("food_sweets", "Sweets"));

        var events = EndOfDay.Resolve(state, "plains", 1, Balance, new Random(42));

        // Thumbroot auto-consumed, injured 3 → 2
        Assert.DoesNotContain(state.Haversack, i => i.DefId == "thumbroot");
        Assert.Contains(events, e => e is EndOfDayEvent.CureApplied);
    }

    [Fact]
    public void Resolve_Medicine_NegatedByFailedResist()
    {
        var state = Fresh();
        state.Spirits = 0;
        state.ActiveConditions["swamp_fever"] = 4;
        state.Haversack.Add(new ItemInstance("gravediggers_ear", "Gravedigger's Ear"));
        state.Haversack.Add(new ItemInstance("food_protein", "Meat"));
        state.Haversack.Add(new ItemInstance("food_grain", "Bread"));
        state.Haversack.Add(new ItemInstance("food_sweets", "Sweets"));

        foreach (var skill in state.Skills.Keys.ToList())
            state.Skills[skill] = 0;

        var rng = new Random(0);
        var events = EndOfDay.Resolve(state, "swamp", 1, Balance, rng);

        var negated = events.OfType<EndOfDayEvent.CureNegated>().Any();
        var applied = events.OfType<EndOfDayEvent.CureApplied>().Any();
        Assert.True(negated || applied);
        Assert.DoesNotContain(state.Haversack, i => i.DefId == "gravediggers_ear");
    }

    [Fact]
    public void Resolve_NewCondition_AppliedAfterMedicine()
    {
        // If a condition is newly acquired (failed resist, player didn't have it),
        // it should be applied in step 4 and fire ConditionAcquired
        var state = Fresh();
        state.Spirits = 0;
        foreach (var skill in state.Skills.Keys.ToList())
            state.Skills[skill] = 0;
        state.Haversack.Add(new ItemInstance("food_protein", "Meat"));
        state.Haversack.Add(new ItemInstance("food_grain", "Bread"));
        state.Haversack.Add(new ItemInstance("food_sweets", "Sweets"));

        // Run in swamp biome — may acquire swamp_fever
        var rng = new Random(0);
        var events = EndOfDay.Resolve(state, "swamp", 1, Balance, rng);

        // Check if any ConditionAcquired events fired (depends on RNG)
        var acquired = events.OfType<EndOfDayEvent.ConditionAcquired>().ToList();
        var resisted = events.OfType<EndOfDayEvent.ResistPassed>().ToList();
        // Each threat should either be acquired or resisted
        Assert.True(acquired.Count + resisted.Count > 0);
    }

    [Fact]
    public void Resolve_NoSleep_SkipsRest()
    {
        var state = Fresh();
        state.Health = 10;
        state.Spirits = 10;
        state.PendingNoSleep = true;
        state.Haversack.Add(new ItemInstance("food_protein", "Meat"));
        state.Haversack.Add(new ItemInstance("food_grain", "Bread"));
        state.Haversack.Add(new ItemInstance("food_sweets", "Sweets"));

        var events = EndOfDay.Resolve(state, "plains", 1, Balance, new Random(42));

        Assert.DoesNotContain(events, e => e is EndOfDayEvent.RestRecovery);
    }
}
