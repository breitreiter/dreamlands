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
        var threats = EndOfDay.GetThreats("plains", 3, Balance);
        Assert.DoesNotContain(threats, t => t.Id == "poisoned");
        Assert.DoesNotContain(threats, t => t.Id == "injured");
        Assert.DoesNotContain(threats, t => t.Id == "irradiated");
        Assert.DoesNotContain(threats, t => t.Id == "lattice_sickness");
    }

    [Fact]
    public void ResolveFood_NoFood_EmitsStarving()
    {
        var state = Fresh();
        var events = EndOfDay.Resolve(state, "plains", 1, Balance, new Random(42));
        Assert.Contains(events, e => e is EndOfDayEvent.Starving);
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
        state.Haversack.Add(new ItemInstance("food_protein", "Meat"));
        state.Haversack.Add(new ItemInstance("food_protein", "More Meat"));
        state.Haversack.Add(new ItemInstance("food_grain", "Bread"));
        state.Haversack.Add(new ItemInstance("food_sweets", "Sweets"));

        var events = EndOfDay.Resolve(state, "plains", 1, Balance, new Random(42));

        var foodEvent = events.OfType<EndOfDayEvent.FoodConsumed>().Single();
        Assert.True(foodEvent.Balanced);
        Assert.Equal(3, foodEvent.FoodEaten.Count);
        Assert.Single(state.Haversack, i => i.DefId == "food_protein");
    }

    [Fact]
    public void Resolve_Rest_RecoverSpiritsOnly()
    {
        var state = Fresh();
        state.Health = 2;
        state.Spirits = 15;
        foreach (var skill in state.Skills.Keys.ToList())
            state.Skills[skill] = Balance.Character.MaxSkillLevel;
        state.Haversack.Add(new ItemInstance("food_protein", "Meat"));
        state.Haversack.Add(new ItemInstance("food_grain", "Bread"));
        state.Haversack.Add(new ItemInstance("food_sweets", "Sweets"));

        var healthBefore = state.Health;
        var events = EndOfDay.Resolve(state, "plains", 1, Balance, new Random(42));

        var rest = events.OfType<EndOfDayEvent.RestRecovery>().Single();
        Assert.Equal(0, rest.HealthGained); // no health recovery at camp
        Assert.True(rest.SpiritsGained > 0);
        Assert.Equal(healthBefore, state.Health); // health unchanged
    }

    [Fact]
    public void Resolve_BalancedMeal_BonusSpiritRecovery()
    {
        var state = Fresh();
        state.Spirits = 10;
        state.Haversack.Add(new ItemInstance("food_protein", "Meat"));
        state.Haversack.Add(new ItemInstance("food_grain", "Bread"));
        state.Haversack.Add(new ItemInstance("food_sweets", "Sweets"));

        var events = EndOfDay.Resolve(state, "plains", 1, Balance, new Random(42));

        var restEvent = events.OfType<EndOfDayEvent.RestRecovery>().Single();
        Assert.Equal(0, restEvent.HealthGained);
        Assert.Equal(Balance.Character.BaseRestSpirits + Balance.Character.BalancedMealSpiritsBonus, restEvent.SpiritsGained);
    }

    [Fact]
    public void Resolve_SevereCondition_Loses1Health()
    {
        var state = Fresh();
        state.ActiveConditions["injured"] = 3;
        state.Haversack.Add(new ItemInstance("food_protein", "Meat"));
        state.Haversack.Add(new ItemInstance("food_grain", "Bread"));
        state.Haversack.Add(new ItemInstance("food_sweets", "Sweets"));

        var healthBefore = state.Health;
        var events = EndOfDay.Resolve(state, "plains", 1, Balance, new Random(42));

        // Should lose exactly 1 HP from untreated severe condition
        Assert.Equal(healthBefore - 1, state.Health);
        var drain = events.OfType<EndOfDayEvent.ConditionDrain>()
            .FirstOrDefault(d => d.HealthLost > 0);
        Assert.NotNull(drain);
        Assert.Equal(1, drain.HealthLost);
    }

    [Fact]
    public void Resolve_MultipleSevereConditions_StillLoses1Health()
    {
        var state = Fresh();
        state.ActiveConditions["injured"] = 3;
        state.ActiveConditions["poisoned"] = 2;
        state.Haversack.Add(new ItemInstance("food_protein", "Meat"));
        state.Haversack.Add(new ItemInstance("food_grain", "Bread"));
        state.Haversack.Add(new ItemInstance("food_sweets", "Sweets"));

        var healthBefore = state.Health;
        EndOfDay.Resolve(state, "plains", 1, Balance, new Random(42));

        // Still only 1 HP loss total, not per-condition
        Assert.Equal(healthBefore - 1, state.Health);
    }

    [Fact]
    public void Resolve_SevereConditionTreated_NoHealthLoss()
    {
        var state = Fresh();
        state.ActiveConditions["injured"] = 1; // 1 stack
        state.Haversack.Add(new ItemInstance("bandages", "Bandages")); // cures it
        state.Haversack.Add(new ItemInstance("food_protein", "Meat"));
        state.Haversack.Add(new ItemInstance("food_grain", "Bread"));
        state.Haversack.Add(new ItemInstance("food_sweets", "Sweets"));

        var healthBefore = state.Health;
        EndOfDay.Resolve(state, "plains", 1, Balance, new Random(42));

        // Medicine cured it before drain check, so no HP loss
        Assert.Equal(healthBefore, state.Health);
    }

    [Fact]
    public void Resolve_FreezingDrainsSpiritsOnly()
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
        Assert.Equal(0, drain.HealthLost);
        Assert.True(drain.SpiritsLost > 0);
    }

    [Fact]
    public void Resolve_ThirstyDrainsSpiritsOnly()
    {
        var state = Fresh();
        state.ActiveConditions["thirsty"] = 1;
        state.Haversack.Add(new ItemInstance("food_protein", "Meat"));
        state.Haversack.Add(new ItemInstance("food_grain", "Bread"));
        state.Haversack.Add(new ItemInstance("food_sweets", "Sweets"));

        var events = EndOfDay.Resolve(state, "plains", 1, Balance, new Random(42));

        var drain = events.OfType<EndOfDayEvent.ConditionDrain>()
            .FirstOrDefault(d => d.ConditionId == "thirsty");
        Assert.NotNull(drain);
        Assert.Equal(0, drain.HealthLost);
        Assert.True(drain.SpiritsLost > 0);
    }

    [Fact]
    public void Resolve_UntreatedSevereCondition_TriggersRescue()
    {
        var state = Fresh();
        state.Health = 1;
        state.PendingNoSleep = true;
        state.ActiveConditions["injured"] = 3;
        state.Haversack.Add(new ItemInstance("food_protein", "Meat"));
        state.Haversack.Add(new ItemInstance("food_grain", "Bread"));
        state.Haversack.Add(new ItemInstance("food_sweets", "Sweets"));

        var events = EndOfDay.Resolve(state, "plains", 1, Balance, new Random(42));

        Assert.Contains(events, e => e is EndOfDayEvent.PlayerDied);
        Assert.Contains(events, e => e is EndOfDayEvent.PlayerRescued);
        Assert.Equal(state.MaxHealth, state.Health); // rescue restores health
    }

    [Fact]
    public void Resolve_ConditionDrain_RescuesBeforeRest()
    {
        var state = Fresh();
        state.Health = 1;
        state.ActiveConditions["injured"] = 3;
        state.Haversack.Add(new ItemInstance("food_protein", "Meat"));
        state.Haversack.Add(new ItemInstance("food_grain", "Bread"));
        state.Haversack.Add(new ItemInstance("food_sweets", "Sweets"));

        var events = EndOfDay.Resolve(state, "plains", 1, Balance, new Random(42));

        Assert.Contains(events, e => e is EndOfDayEvent.PlayerDied);
        Assert.Contains(events, e => e is EndOfDayEvent.PlayerRescued);
        Assert.DoesNotContain(events, e => e is EndOfDayEvent.RestRecovery);
    }

    [Fact]
    public void Resolve_Medicine_AutoConsumedForActiveCondition()
    {
        var state = Fresh();
        state.ActiveConditions["injured"] = 3;
        state.Haversack.Add(new ItemInstance("bandages", "Bandages"));
        state.Haversack.Add(new ItemInstance("food_protein", "Meat"));
        state.Haversack.Add(new ItemInstance("food_grain", "Bread"));
        state.Haversack.Add(new ItemInstance("food_sweets", "Sweets"));

        var events = EndOfDay.Resolve(state, "plains", 1, Balance, new Random(42));

        Assert.DoesNotContain(state.Haversack, i => i.DefId == "bandages");
        Assert.Contains(events, e => e is EndOfDayEvent.CureApplied);
    }

    [Fact]
    public void Resolve_NewCondition_AppliedAfterMedicine()
    {
        var state = Fresh();
        state.Spirits = 0;
        foreach (var skill in state.Skills.Keys.ToList())
            state.Skills[skill] = 0;
        state.Haversack.Add(new ItemInstance("food_protein", "Meat"));
        state.Haversack.Add(new ItemInstance("food_grain", "Bread"));
        state.Haversack.Add(new ItemInstance("food_sweets", "Sweets"));

        var rng = new Random(0);
        var events = EndOfDay.Resolve(state, "mountains", 1, Balance, rng);

        var acquired = events.OfType<EndOfDayEvent.ConditionAcquired>().ToList();
        var resisted = events.OfType<EndOfDayEvent.ResistPassed>().ToList();
        Assert.True(acquired.Count + resisted.Count > 0);
    }

    [Fact]
    public void Resolve_NoSleep_SkipsRest()
    {
        var state = Fresh();
        state.Spirits = 10;
        state.PendingNoSleep = true;
        state.Haversack.Add(new ItemInstance("food_protein", "Meat"));
        state.Haversack.Add(new ItemInstance("food_grain", "Bread"));
        state.Haversack.Add(new ItemInstance("food_sweets", "Sweets"));

        var events = EndOfDay.Resolve(state, "plains", 1, Balance, new Random(42));

        Assert.DoesNotContain(events, e => e is EndOfDayEvent.RestRecovery);
    }

    [Fact]
    public void ResolveForaging_HighModifier_FindsFood()
    {
        bool foundAny = false;
        for (int seed = 0; seed < 20; seed++)
        {
            var s = Fresh();
            s.Skills[Skill.Bushcraft] = 4;
            s.Haversack.Add(new ItemInstance("food_protein", "Meat"));
            s.Haversack.Add(new ItemInstance("food_grain", "Bread"));
            s.Haversack.Add(new ItemInstance("food_sweets", "Sweets"));

            var events = EndOfDay.Resolve(s, "plains", 1, Balance, new Random(seed));
            var foraged = events.OfType<EndOfDayEvent.Foraged>().Single();
            if (foraged.ItemsFound.Count > 0) { foundAny = true; break; }
        }
        Assert.True(foundAny, "High bushcraft should find food on at least one seed");
    }

    [Fact]
    public void ResolveForaging_NoBiome_SkipsForaging()
    {
        var state = Fresh();
        state.PendingNoBiome = true;
        state.Haversack.Add(new ItemInstance("food_protein", "Meat"));
        state.Haversack.Add(new ItemInstance("food_grain", "Bread"));
        state.Haversack.Add(new ItemInstance("food_sweets", "Sweets"));

        var events = EndOfDay.Resolve(state, "plains", 1, Balance, new Random(42));

        Assert.DoesNotContain(events, e => e is EndOfDayEvent.Foraged);
    }
}
