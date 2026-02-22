using Dreamlands.Game;
using Dreamlands.Rules;

namespace Dreamlands.Game.Tests;

public class MechanicsTests
{
    static readonly BalanceData Balance = BalanceData.Default;
    static readonly Random Rng = new(42);

    static PlayerState Fresh() => PlayerState.NewGame("test", 99, Balance);

    [Fact]
    public void DamageHealth_Small_ReducesHealth()
    {
        var state = Fresh();
        var initial = state.Health;
        var results = Mechanics.Apply(["damage_health small"], state, Balance, Rng);

        Assert.Single(results);
        var r = Assert.IsType<MechanicResult.HealthChanged>(results[0]);
        Assert.Equal(-2, r.Delta);
        Assert.Equal(initial - 2, r.NewValue);
        Assert.Equal(initial - 2, state.Health);
    }

    [Fact]
    public void Heal_Small_IncreasesHealth_CappedAtMax()
    {
        var state = Fresh();
        state.Health = 19;
        var results = Mechanics.Apply(["heal small"], state, Balance, Rng);

        var r = Assert.IsType<MechanicResult.HealthChanged>(results[0]);
        Assert.Equal(2, r.Delta);
        Assert.Equal(state.MaxHealth, state.Health); // capped at 20
    }

    [Fact]
    public void Heal_DoesNotExceedMaxHealth()
    {
        var state = Fresh();
        // Health already at max
        var results = Mechanics.Apply(["heal huge"], state, Balance, Rng);
        Assert.Equal(state.MaxHealth, state.Health);
    }

    [Fact]
    public void DamageSpirits_ReducesSpirits()
    {
        var state = Fresh();
        var initial = state.Spirits;
        Mechanics.Apply(["damage_spirits medium"], state, Balance, Rng);
        Assert.Equal(initial - 3, state.Spirits);
    }

    [Fact]
    public void HealSpirits_IncreasesSpirits_CappedAtMax()
    {
        var state = Fresh();
        state.Spirits = 19;
        Mechanics.Apply(["heal_spirits small"], state, Balance, Rng);
        Assert.Equal(state.MaxSpirits, state.Spirits);
    }

    [Fact]
    public void GiveGold_IncreasesGold()
    {
        var state = Fresh();
        var initial = state.Gold;
        var results = Mechanics.Apply(["give_gold large"], state, Balance, Rng);

        var r = Assert.IsType<MechanicResult.GoldChanged>(results[0]);
        Assert.Equal(4, r.Delta);
        Assert.Equal(initial + 4, state.Gold);
    }

    [Fact]
    public void RemGold_FloorsAtZero()
    {
        var state = Fresh();
        state.Gold = 1;
        Mechanics.Apply(["rem_gold huge"], state, Balance, Rng);
        Assert.Equal(0, state.Gold);
    }

    [Fact]
    public void IncreaseSkill_CappedAtMaxLevel()
    {
        var state = Fresh();
        state.Skills[Skill.Combat] = 3;
        var results = Mechanics.Apply(["increase_skill combat 5"], state, Balance, Rng);

        var r = Assert.IsType<MechanicResult.SkillChanged>(results[0]);
        Assert.Equal(Balance.Character.MaxSkillLevel, state.Skills[Skill.Combat]);
    }

    [Fact]
    public void DecreaseSkill_FloorsAtZero()
    {
        var state = Fresh();
        state.Skills[Skill.Cunning] = 1;
        Mechanics.Apply(["decrease_skill cunning 5"], state, Balance, Rng);
        Assert.Equal(Balance.Character.MinSkillLevel, state.Skills[Skill.Cunning]);
    }

    [Fact]
    public void AddItem_Consumable_GoesToHaversack()
    {
        var state = Fresh();
        var results = Mechanics.Apply(["add_item bandages"], state, Balance, Rng);

        var r = Assert.IsType<MechanicResult.ItemGained>(results[0]);
        Assert.Equal("bandages", r.DefId);
        Assert.Contains(state.Haversack, i => i.DefId == "bandages");
    }

    [Fact]
    public void AddItem_Weapon_GoesToPack()
    {
        var state = Fresh();
        var results = Mechanics.Apply(["add_item bodkin"], state, Balance, Rng);

        Assert.IsType<MechanicResult.ItemGained>(results[0]);
        Assert.Contains(state.Pack, i => i.DefId == "bodkin");
    }

    [Fact]
    public void AddTag_AppearsInTags()
    {
        var state = Fresh();
        Mechanics.Apply(["add_tag hero"], state, Balance, Rng);
        Assert.Contains("hero", state.Tags);
    }

    [Fact]
    public void RemoveTag_RemovesFromTags()
    {
        var state = Fresh();
        state.Tags.Add("hero");
        Mechanics.Apply(["remove_tag hero"], state, Balance, Rng);
        Assert.DoesNotContain("hero", state.Tags);
    }

    [Fact]
    public void AddCondition_AppearsInActiveConditions()
    {
        var state = Fresh();
        var results = Mechanics.Apply(["add_condition freezing"], state, Balance, Rng);

        var r = Assert.IsType<MechanicResult.ConditionAdded>(results[0]);
        Assert.Equal("freezing", r.ConditionId);
        Assert.True(state.ActiveConditions.ContainsKey("freezing"));
    }

    [Fact]
    public void AddCondition_UsesStacksFromBalance()
    {
        var state = Fresh();
        Mechanics.Apply(["add_condition freezing"], state, Balance, Rng);

        var expectedStacks = Balance.Conditions["freezing"].Stacks;
        Assert.Equal(expectedStacks, state.ActiveConditions["freezing"]);
    }

    [Fact]
    public void RemoveCondition_RemovesFromActiveConditions()
    {
        var state = Fresh();
        state.ActiveConditions["freezing"] = 3;
        var results = Mechanics.Apply(["remove_condition freezing"], state, Balance, Rng);

        Assert.IsType<MechanicResult.ConditionRemoved>(results[0]);
        Assert.False(state.ActiveConditions.ContainsKey("freezing"));
    }

    [Fact]
    public void Equip_MovesFromPackToEquipment()
    {
        var state = Fresh();
        state.Pack.Add(new ItemInstance("bodkin", "Bodkin"));

        var results = Mechanics.Apply(["equip bodkin"], state, Balance, Rng);
        var r = Assert.IsType<MechanicResult.ItemEquipped>(results[0]);
        Assert.Equal("weapon", r.Slot);
        Assert.NotNull(state.Equipment.Weapon);
        Assert.Equal("bodkin", state.Equipment.Weapon!.DefId);
        Assert.DoesNotContain(state.Pack, i => i.DefId == "bodkin");
    }

    [Fact]
    public void Equip_SwapsOldItemBackToPack()
    {
        var state = Fresh();
        state.Equipment.Weapon = new ItemInstance("old_sword", "Old Sword");
        state.Pack.Add(new ItemInstance("bodkin", "Bodkin"));

        Mechanics.Apply(["equip bodkin"], state, Balance, Rng);

        Assert.Equal("bodkin", state.Equipment.Weapon!.DefId);
        Assert.Contains(state.Pack, i => i.DefId == "old_sword");
    }

    [Fact]
    public void Unequip_MovesFromEquipmentToPack()
    {
        var state = Fresh();
        state.Equipment.Weapon = new ItemInstance("bodkin", "Bodkin");

        var results = Mechanics.Apply(["unequip weapon"], state, Balance, Rng);
        var r = Assert.IsType<MechanicResult.ItemUnequipped>(results[0]);
        Assert.Equal("weapon", r.Slot);
        Assert.Null(state.Equipment.Weapon);
        Assert.Contains(state.Pack, i => i.DefId == "bodkin");
    }

    [Fact]
    public void Open_ReturnsNavigation()
    {
        var state = Fresh();
        var results = Mechanics.Apply(["open \"The Ghosts\""], state, Balance, Rng);

        var r = Assert.IsType<MechanicResult.Navigation>(results[0]);
        Assert.Equal("The Ghosts", r.EncounterId);
    }

    [Fact]
    public void FinishDungeon_AddsToCompletedDungeons()
    {
        var state = Fresh();
        state.CurrentDungeonId = "dungeon_1";
        var results = Mechanics.Apply(["finish_dungeon"], state, Balance, Rng);

        Assert.IsType<MechanicResult.DungeonFinished>(results[0]);
        Assert.Contains("dungeon_1", state.CompletedDungeons);
    }

    [Fact]
    public void SkipTime_AdvancesTimePeriod()
    {
        var state = Fresh();
        state.Time = TimePeriod.Morning;
        state.Day = 1;

        var results = Mechanics.Apply(["skip_time evening"], state, Balance, Rng);
        var r = Assert.IsType<MechanicResult.TimeAdvanced>(results[0]);
        Assert.Equal(TimePeriod.Evening, state.Time);
        Assert.Equal(1, state.Day);
    }

    [Fact]
    public void SkipTime_WrapsDay_WhenTargetIsBeforeCurrent()
    {
        var state = Fresh();
        state.Time = TimePeriod.Evening;
        state.Day = 1;

        Mechanics.Apply(["skip_time morning"], state, Balance, Rng);
        Assert.Equal(TimePeriod.Morning, state.Time);
        Assert.Equal(2, state.Day);
    }

    [Fact]
    public void Apply_EmptyList_ReturnsEmpty()
    {
        var state = Fresh();
        var results = Mechanics.Apply([], state, Balance, Rng);
        Assert.Empty(results);
    }

    [Fact]
    public void Apply_MultipleActions_AppliesAll()
    {
        var state = Fresh();
        var results = Mechanics.Apply(["add_tag quest_started", "give_gold small"], state, Balance, Rng);
        Assert.Equal(2, results.Count);
        Assert.Contains("quest_started", state.Tags);
        Assert.True(state.Gold > Balance.Character.StartingGold);
    }
}
