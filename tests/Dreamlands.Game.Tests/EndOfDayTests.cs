using Dreamlands.Game;
using Dreamlands.Rules;

namespace Dreamlands.Game.Tests;

/// <summary>
/// EndOfDay covers food cadence, medicine cures, condition drains, HP tick, and rescue.
/// Travel hazards (thirst/cold/fatigue) live in Travails — see TravailsTests.
/// </summary>
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
            p.Pack.Add(new ItemInstance(Rations.RationDefId, "Rations"));
    }

    [Fact]
    public void Resolve_NoFood_EmitsStarving()
    {
        var p = Fresh();
        var spiritsBefore = p.Spirits;

        var events = EndOfDay.Resolve(p, Balance);

        Assert.Contains(events, e => e is EndOfDayEvent.Starving);
        Assert.Equal(spiritsBefore - 1, p.Spirits);
    }

    [Fact]
    public void Resolve_HasRation_ConsumesOne_WhenUntrainedBushcraft()
    {
        var p = Fresh();
        p.Skills[Skill.Bushcraft] = SkillTier.Untrained; // eats every night
        p.Day = 1; // odd day
        AddRation(p, 3);

        EndOfDay.Resolve(p, Balance);

        Assert.Equal(2, p.Pack.Count(i => i.DefId == Rations.RationDefId));
    }

    [Fact]
    public void Resolve_TrainedBushcraft_EatsOnOddDays()
    {
        // Day 1 (odd) → should eat
        var p = Fresh();
        p.Skills[Skill.Bushcraft] = SkillTier.Trained;
        p.Day = 1;
        AddRation(p, 3);

        EndOfDay.Resolve(p, Balance);
        Assert.Equal(2, p.Pack.Count(i => i.DefId == Rations.RationDefId));
    }

    [Fact]
    public void Resolve_TrainedBushcraft_SkipsEatingOnEvenDays()
    {
        // Day 2 (even) → skips eating
        var p = Fresh();
        p.Skills[Skill.Bushcraft] = SkillTier.Trained;
        p.Day = 2;
        AddRation(p, 3);

        EndOfDay.Resolve(p, Balance);
        Assert.Equal(3, p.Pack.Count(i => i.DefId == Rations.RationDefId)); // unchanged
    }

    [Fact]
    public void Resolve_ExpertBushcraft_SameCadenceAsTrained()
    {
        // Expert: eat on odd days
        var p = Fresh();
        p.Skills[Skill.Bushcraft] = SkillTier.Expert;
        p.Day = 3; // odd → eats
        AddRation(p, 3);

        EndOfDay.Resolve(p, Balance);
        Assert.Equal(2, p.Pack.Count(i => i.DefId == Rations.RationDefId));
    }

    [Fact]
    public void Resolve_CleanRestDay_NoSpiritsChange()
    {
        var p = Fresh();
        AddRation(p);
        var spiritsBefore = p.Spirits;

        EndOfDay.Resolve(p, Balance);

        Assert.Equal(spiritsBefore, p.Spirits);
    }

    [Fact]
    public void Resolve_NoSpiritsRegen_OnRoad()
    {
        var p = Fresh();
        p.Spirits = 10;
        AddRation(p);

        EndOfDay.Resolve(p, Balance);

        // No daily passive +1 anymore — spirits unchanged
        Assert.Equal(10, p.Spirits);
    }

    [Fact]
    public void Resolve_SpiritDrainingCondition_Drains1Spirit()
    {
        // Interim until the phase-3 content sweep: encounter-applied travel
        // conditions still exist and still drain (plans/travel_travails.md).
        var p = Fresh();
        p.Spirits = 10;
        p.ActiveConditions.Add("freezing");
        AddRation(p);

        EndOfDay.Resolve(p, Balance);

        Assert.Equal(9, p.Spirits);
    }

    [Fact]
    public void Resolve_SeriousCondition_LosesHealth()
    {
        var p = Fresh();
        p.ActiveConditions.Add("injured");
        AddRation(p);

        var hpBefore = p.Health;
        EndOfDay.Resolve(p, Balance);

        Assert.Equal(hpBefore - 1, p.Health);
    }

    [Fact]
    public void Resolve_NoSeriousCondition_RegensHealth()
    {
        var p = Fresh();
        p.MaxHealth = 4;
        p.Health = 2;
        AddRation(p);

        EndOfDay.Resolve(p, Balance);

        Assert.Equal(3, p.Health);
    }

    [Fact]
    public void Resolve_HealthRegen_CappedAtMax()
    {
        var p = Fresh();
        AddRation(p);

        EndOfDay.Resolve(p, Balance);

        Assert.Equal(p.MaxHealth, p.Health);
    }

    [Fact]
    public void Resolve_MinorConditionDoesNotBlockHealthRegen()
    {
        var p = Fresh();
        p.MaxHealth = 4;
        p.Health = 2;
        p.ActiveConditions.Add("freezing");
        AddRation(p);

        EndOfDay.Resolve(p, Balance);

        Assert.Equal(3, p.Health);
    }

    [Fact]
    public void Resolve_MultipleSeriousConditions_StillLoses1Health()
    {
        var p = Fresh();
        p.ActiveConditions.Add("injured");
        p.ActiveConditions.Add("poisoned");
        AddRation(p);

        var hpBefore = p.Health;
        EndOfDay.Resolve(p, Balance);

        // Flat -1 regardless of count
        Assert.Equal(hpBefore - 1, p.Health);
    }

    [Fact]
    public void Resolve_MedicalKit_CuresInjured_WithoutConsuming()
    {
        var p = Fresh();
        p.MaxHealth = 4;
        p.Health = 2;
        p.ActiveConditions.Add("injured");
        p.Pack.Add(new ItemInstance("medical_kit", "Medical Kit"));
        AddRation(p);

        EndOfDay.Resolve(p, Balance);

        // Medical kit removes injured before the HP tick — regen kicks in same day
        Assert.DoesNotContain("injured", p.ActiveConditions);
        // Kit is NOT consumed
        Assert.Contains(p.Pack, i => i.DefId == "medical_kit");
        Assert.Equal(3, p.Health);
    }

    [Fact]
    public void Resolve_MedicalKit_PersistsAcrossMultipleDays()
    {
        // Verify kit still present and functional next night
        var p = Fresh();
        p.MaxHealth = 4;
        p.Health = 1;
        p.ActiveConditions.Add("injured");
        p.Pack.Add(new ItemInstance("medical_kit", "Medical Kit"));
        AddRation(p, 5);

        // Night 1: kit cures injured, HP regens
        EndOfDay.Resolve(p, Balance);
        Assert.DoesNotContain("injured", p.ActiveConditions);
        Assert.Equal(2, p.Health);
        Assert.Contains(p.Pack, i => i.DefId == "medical_kit");

        // Night 2: no condition, HP regens again
        EndOfDay.Resolve(p, Balance);
        Assert.Equal(3, p.Health);
        Assert.Contains(p.Pack, i => i.DefId == "medical_kit");
    }

    [Fact]
    public void Resolve_MissedMealAndCondition_DrainsBoth()
    {
        var p = Fresh();
        p.Spirits = 10;
        p.ActiveConditions.Add("freezing");
        // No ration — and Untrained bushcraft eats every night

        EndOfDay.Resolve(p, Balance);

        // -1 missed meal, -1 freezing
        Assert.Equal(8, p.Spirits);
    }

    [Fact]
    public void Resolve_PendingNoMeal_NoStarvingPenalty()
    {
        var p = Fresh();
        p.Spirits = 10;
        p.PendingNoMeal = true;

        EndOfDay.Resolve(p, Balance);

        // noMeal skips the food consumption path entirely — no starving event, no penalty
        Assert.Equal(10, p.Spirits);
    }

    [Fact]
    public void Resolve_UntreatedSerious_LowHealth_TriggersRescue()
    {
        var p = Fresh();
        p.Health = 1;
        p.ActiveConditions.Add("injured");
        AddRation(p);

        var events = EndOfDay.Resolve(p, Balance);

        Assert.Contains(events, e => e is EndOfDayEvent.PlayerDied);
        Assert.Contains(events, e => e is EndOfDayEvent.PlayerRescued);
        Assert.Equal(p.MaxHealth, p.Health);
    }
}
