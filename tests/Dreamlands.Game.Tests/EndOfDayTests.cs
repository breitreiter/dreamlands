using Dreamlands.Game;
using Dreamlands.Rules;

namespace Dreamlands.Game.Tests;

public class EndOfDayTests
{
    static readonly BalanceData Balance = BalanceData.Default;

    static PlayerState Fresh()
    {
        var p = PlayerState.NewGame("test", 99, Balance);
        p.MaxHealth = 4;
        p.Health = 4;
        return p;
    }

    static void AddRation(PlayerState p, int count = 1)
    {
        for (int i = 0; i < count; i++)
            p.Haversack.Add(new ItemInstance(Rations.RationDefId, "Rations"));
    }

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
    public void Resolve_NoFood_EmitsStarving()
    {
        var p = Fresh();
        // Make resists trivially pass and bushcraft trivially fail to skip foraging
        p.PendingNoBiome = true; // skips both resists and foraging
        var spiritsBefore = p.Spirits;

        var events = EndOfDay.Resolve(p, "plains", 1, Balance, new Random(42));

        Assert.Contains(events, e => e is EndOfDayEvent.Starving);
        Assert.Equal(spiritsBefore - 1, p.Spirits);
    }

    [Fact]
    public void Resolve_HasRation_ConsumesOne()
    {
        var p = Fresh();
        p.PendingNoBiome = true;
        AddRation(p, 3);

        EndOfDay.Resolve(p, "plains", 1, Balance, new Random(42));

        Assert.Equal(2, p.Haversack.Count(i => i.DefId == Rations.RationDefId));
    }

    [Fact]
    public void Resolve_CleanRestDay_NoSpiritsChange()
    {
        var p = Fresh();
        p.PendingNoBiome = true; // no resist rolls, no foraging
        AddRation(p);
        var spiritsBefore = p.Spirits;

        EndOfDay.Resolve(p, "plains", 1, Balance, new Random(42));

        Assert.Equal(spiritsBefore, p.Spirits);
    }

    [Fact]
    public void Resolve_NoSpiritsRegen_OnRoad()
    {
        var p = Fresh();
        p.Spirits = 10;
        p.PendingNoBiome = true;
        AddRation(p);

        EndOfDay.Resolve(p, "plains", 1, Balance, new Random(42));

        // No daily passive +1 anymore — spirits unchanged
        Assert.Equal(10, p.Spirits);
    }

    [Fact]
    public void Resolve_MinorCondition_Drains1Spirit()
    {
        var p = Fresh();
        p.Spirits = 10;
        p.PendingNoBiome = true;
        p.ActiveConditions.Add("freezing");
        AddRation(p);

        EndOfDay.Resolve(p, "plains", 1, Balance, new Random(42));

        Assert.Equal(9, p.Spirits);
    }

    [Fact]
    public void Resolve_MultipleMinorConditions_DrainsStack()
    {
        var p = Fresh();
        p.Spirits = 10;
        p.PendingNoBiome = true;
        p.ActiveConditions.Add("freezing");
        p.ActiveConditions.Add("thirsty");
        AddRation(p);

        EndOfDay.Resolve(p, "plains", 1, Balance, new Random(42));

        Assert.Equal(8, p.Spirits);
    }

    [Fact]
    public void Resolve_SeriousCondition_LosesHealth()
    {
        var p = Fresh();
        p.PendingNoBiome = true;
        p.ActiveConditions.Add("injured");
        AddRation(p);

        var hpBefore = p.Health;
        EndOfDay.Resolve(p, "plains", 1, Balance, new Random(42));

        Assert.Equal(hpBefore - 1, p.Health);
    }

    [Fact]
    public void Resolve_NoSeriousCondition_RegensHealth()
    {
        var p = Fresh();
        p.MaxHealth = 4;
        p.Health = 2;
        p.PendingNoBiome = true;
        AddRation(p);

        EndOfDay.Resolve(p, "plains", 1, Balance, new Random(42));

        Assert.Equal(3, p.Health);
    }

    [Fact]
    public void Resolve_HealthRegen_CappedAtMax()
    {
        var p = Fresh();
        p.PendingNoBiome = true;
        AddRation(p);

        EndOfDay.Resolve(p, "plains", 1, Balance, new Random(42));

        Assert.Equal(p.MaxHealth, p.Health);
    }

    [Fact]
    public void Resolve_MinorConditionDoesNotBlockHealthRegen()
    {
        var p = Fresh();
        p.MaxHealth = 4;
        p.Health = 2;
        p.PendingNoBiome = true;
        p.ActiveConditions.Add("freezing");
        AddRation(p);

        EndOfDay.Resolve(p, "plains", 1, Balance, new Random(42));

        Assert.Equal(3, p.Health);
    }

    [Fact]
    public void Resolve_MultipleSeriousConditions_StillLoses1Health()
    {
        var p = Fresh();
        p.PendingNoBiome = true;
        p.ActiveConditions.Add("injured");
        p.ActiveConditions.Add("poisoned");
        AddRation(p);

        var hpBefore = p.Health;
        EndOfDay.Resolve(p, "plains", 1, Balance, new Random(42));

        // Flat -1 regardless of count
        Assert.Equal(hpBefore - 1, p.Health);
    }

    [Fact]
    public void Resolve_CuredSerious_NoHealthLoss_AndRegens()
    {
        var p = Fresh();
        p.MaxHealth = 4;
        p.Health = 2;
        p.PendingNoBiome = true;
        p.ActiveConditions.Add("injured");
        p.Haversack.Add(new ItemInstance("bandages", "Bandages"));
        AddRation(p);

        EndOfDay.Resolve(p, "plains", 1, Balance, new Random(42));

        // Bandage removes injured before the HP tick — regen kicks in same day
        Assert.DoesNotContain("injured", p.ActiveConditions);
        Assert.DoesNotContain(p.Haversack, i => i.DefId == "bandages");
        Assert.Equal(3, p.Health);
    }

    [Fact]
    public void Resolve_BandageCuresInjured_HealsOverMultipleDays()
    {
        var p = Fresh();
        p.MaxHealth = 4;
        p.Health = 1;
        p.PendingNoBiome = true;
        p.ActiveConditions.Add("injured");
        p.Haversack.Add(new ItemInstance("bandages", "Bandages"));
        AddRation(p, 5);

        EndOfDay.Resolve(p, "plains", 1, Balance, new Random(42));
        Assert.DoesNotContain("injured", p.ActiveConditions);
        Assert.Equal(2, p.Health);

        p.PendingNoBiome = true;
        EndOfDay.Resolve(p, "plains", 1, Balance, new Random(42));
        Assert.Equal(3, p.Health);
    }

    [Fact]
    public void Resolve_MissedMealAndCondition_DrainsBoth()
    {
        var p = Fresh();
        p.Spirits = 10;
        p.PendingNoBiome = true;
        p.ActiveConditions.Add("freezing");
        // No ration

        EndOfDay.Resolve(p, "plains", 1, Balance, new Random(42));

        // -1 missed meal, -1 freezing
        Assert.Equal(8, p.Spirits);
    }

    [Fact]
    public void ExhaustionDC_ScalesWithConsecutiveWildernessNights()
    {
        // Verify the DC math by checking it produces different outcomes at different night counts
        var seed = 7; // chosen so the d20 falls in the band where the scaling matters
        bool resistedEarly = false, resistedLate = false;

        for (int trial = 0; trial < 50; trial++)
        {
            var early = Fresh();
            early.ConsecutiveWildernessNights = 0;
            AddRation(early);
            EndOfDay.Resolve(early, "plains", 1, Balance, new Random(seed + trial));
            if (!early.ActiveConditions.Contains("exhausted")) resistedEarly = true;

            var late = Fresh();
            late.ConsecutiveWildernessNights = 10;
            AddRation(late);
            EndOfDay.Resolve(late, "plains", 1, Balance, new Random(seed + trial));
            if (late.ActiveConditions.Contains("exhausted")) resistedLate = true;

            if (resistedEarly && resistedLate) break;
        }

        // At 0 nights some seeds should resist; at 10 nights some should fail
        Assert.True(resistedEarly, "Should resist exhaustion at least once with 0 nights");
        Assert.True(resistedLate, "Should fail exhaustion at least once with 10 nights");
    }

    [Fact]
    public void Resolve_IncrementsConsecutiveWildernessNights()
    {
        var p = Fresh();
        p.PendingNoBiome = false;
        p.ConsecutiveWildernessNights = 3;
        AddRation(p);

        EndOfDay.Resolve(p, "plains", 1, Balance, new Random(42));

        Assert.Equal(4, p.ConsecutiveWildernessNights);
    }

    [Fact]
    public void Resolve_NoBiome_DoesNotIncrementCounter()
    {
        var p = Fresh();
        p.PendingNoBiome = true;
        p.ConsecutiveWildernessNights = 3;
        AddRation(p);

        EndOfDay.Resolve(p, "plains", 1, Balance, new Random(42));

        Assert.Equal(3, p.ConsecutiveWildernessNights);
    }

    [Fact]
    public void Resolve_PendingNoMeal_NoStarvingPenalty()
    {
        var p = Fresh();
        p.Spirits = 10;
        p.PendingNoMeal = true;
        p.PendingNoBiome = true;

        EndOfDay.Resolve(p, "plains", 1, Balance, new Random(42));

        // noMeal skips the food consumption path entirely — no starving event, no penalty
        Assert.Equal(10, p.Spirits);
    }

    [Fact]
    public void Resolve_UntreatedSerious_LowHealth_TriggersRescue()
    {
        var p = Fresh();
        p.Health = 1;
        p.PendingNoBiome = true;
        p.ActiveConditions.Add("injured");
        AddRation(p);

        var events = EndOfDay.Resolve(p, "plains", 1, Balance, new Random(42));

        Assert.Contains(events, e => e is EndOfDayEvent.PlayerDied);
        Assert.Contains(events, e => e is EndOfDayEvent.PlayerRescued);
        Assert.Equal(p.MaxHealth, p.Health);
    }

    [Fact]
    public void Resolve_ForageSuccess_SkipsRationConsumption()
    {
        // Stack the deck: max bushcraft so the d20+modifier reliably beats DC 20
        var p = Fresh();
        p.Skills[Skill.Bushcraft] = Balance.Character.MaxSkillLevel;
        AddRation(p, 3);

        bool sawSkippedConsumption = false;
        for (int seed = 0; seed < 50; seed++)
        {
            var fresh = Fresh();
            fresh.Skills[Skill.Bushcraft] = Balance.Character.MaxSkillLevel;
            AddRation(fresh, 3);

            var rationsBefore = fresh.Haversack.Count(i => i.DefId == Rations.RationDefId);
            var events = EndOfDay.Resolve(fresh, "plains", 1, Balance, new Random(seed));
            var foraged = events.OfType<EndOfDayEvent.Foraged>().FirstOrDefault();
            if (foraged != null && foraged.Fed)
            {
                var rationsAfter = fresh.Haversack.Count(i => i.DefId == Rations.RationDefId);
                if (rationsAfter == rationsBefore) sawSkippedConsumption = true;
                break;
            }
        }
        Assert.True(sawSkippedConsumption, "High bushcraft should occasionally feed the player");
    }
}
