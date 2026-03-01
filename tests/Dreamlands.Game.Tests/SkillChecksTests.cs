using Dreamlands.Game;
using Dreamlands.Rules;

namespace Dreamlands.Game.Tests;

public class SkillChecksTests
{
    static readonly BalanceData Balance = BalanceData.Default;

    static PlayerState Fresh() => PlayerState.NewGame("test", 99, Balance);

    [Fact]
    public void Roll_ReturnsCorrectStructure()
    {
        var state = Fresh();
        var rng = new Random(100);
        var result = SkillChecks.Roll(Skill.Combat, Difficulty.Easy, state, Balance, rng);

        Assert.Equal(Skill.Combat, result.Skill);
        Assert.Equal(8, result.Target); // Easy DC
        Assert.InRange(result.NaturalRoll, 1, 20);
        Assert.Equal(state.Skills[Skill.Combat], result.SkillLevel);
    }

    [Fact]
    public void Roll_HighSkill_TrivialDifficulty_Passes()
    {
        var state = Fresh();
        state.Skills[Skill.Combat] = 10;
        // d20 (1-20) + 10 >= 5 always passes (except nat 1)
        // Use a seed that won't roll natural 1
        var rng = new Random(1);
        var result = SkillChecks.Roll(Skill.Combat, Difficulty.Trivial, state, Balance, rng);
        Assert.True(result.Passed);
    }

    [Fact]
    public void Roll_ZeroSkill_EpicDifficulty_UsuallyFails()
    {
        var state = Fresh();
        state.Skills[Skill.Cunning] = 0;
        state.Spirits = 20; // no disadvantage

        // DC 22, modifier 0 — only nat 20 passes (and nat 1 always fails)
        int failCount = 0;
        for (int seed = 0; seed < 50; seed++)
        {
            var rng = new Random(seed);
            var result = SkillChecks.Roll(Skill.Cunning, Difficulty.Epic, state, Balance, rng);
            if (!result.Passed) failCount++;
        }
        Assert.True(failCount > 40, "Expected most epic checks with skill 0 to fail");
    }

    [Fact]
    public void Natural1_AlwaysFails()
    {
        var state = Fresh();
        state.Skills[Skill.Combat] = 10; // huge modifier
        state.Spirits = 20;

        // Find a seed that rolls natural 1
        for (int seed = 0; seed < 1000; seed++)
        {
            var rng = new Random(seed);
            var result = SkillChecks.Roll(Skill.Combat, Difficulty.Trivial, state, Balance, rng);
            if (result.NaturalRoll == 1)
            {
                Assert.False(result.Passed, "Natural 1 should always fail");
                return;
            }
        }
        Assert.Fail("Could not find a seed that rolls natural 1");
    }

    [Fact]
    public void Natural20_AlwaysPasses()
    {
        var state = Fresh();
        state.Skills[Skill.Combat] = -2; // lowest modifier
        state.Spirits = 20;

        // Find a seed that rolls natural 20
        for (int seed = 0; seed < 1000; seed++)
        {
            var rng = new Random(seed);
            var result = SkillChecks.Roll(Skill.Combat, Difficulty.Epic, state, Balance, rng);
            if (result.NaturalRoll == 20)
            {
                Assert.True(result.Passed, "Natural 20 should always pass");
                return;
            }
        }
        Assert.Fail("Could not find a seed that rolls natural 20");
    }

    [Fact]
    public void Roll_Disheartened_ImposesDisadvantage()
    {
        var state = Fresh();
        state.Skills[Skill.Combat] = 0;
        state.ActiveConditions["disheartened"] = 1;

        var rng = new Random(42);
        var result = SkillChecks.Roll(Skill.Combat, Difficulty.Medium, state, Balance, rng);
        Assert.Equal(RollMode.Disadvantage, result.RollMode);
    }

    [Fact]
    public void Roll_Advantage_CancelledByDisheartened()
    {
        var state = Fresh();
        state.Skills[Skill.Combat] = 0;
        state.ActiveConditions["disheartened"] = 1;

        var rng = new Random(42);
        var result = SkillChecks.Roll(Skill.Combat, Difficulty.Medium, state, Balance, rng,
            rollMode: RollMode.Advantage);
        Assert.Equal(RollMode.Normal, result.RollMode);
    }

    [Fact]
    public void Roll_RollModeDefaultsToNormal()
    {
        var state = Fresh();
        state.Spirits = 20;
        var rng = new Random(42);
        var result = SkillChecks.Roll(Skill.Combat, Difficulty.Medium, state, Balance, rng);
        Assert.Equal(RollMode.Normal, result.RollMode);
    }

    // ── Gear sourcing tests ──

    [Fact]
    public void GetItemBonus_Combat_UsesWeaponOnly()
    {
        var state = Fresh();
        state.Equipment.Weapon = new ItemInstance("arming_sword", "Arming Sword");
        state.Equipment.Armor = new ItemInstance("scale_armor", "Scale Armor");

        Assert.Equal(4, SkillChecks.GetItemBonus(Skill.Combat, state, Balance));
    }

    [Fact]
    public void GetItemBonus_Cunning_UsesArmorOnly()
    {
        var state = Fresh();
        state.Equipment.Armor = new ItemInstance("chainmail", "Chainmail");
        state.Equipment.Weapon = new ItemInstance("bodkin", "Bodkin");

        Assert.Equal(-3, SkillChecks.GetItemBonus(Skill.Cunning, state, Balance));
    }

    [Fact]
    public void GetItemBonus_Negotiation_TwoHighestTools()
    {
        var state = Fresh();
        state.Pack.Add(new ItemInstance("peoples_borderlands", "Peoples of the Borderlands"));
        state.Pack.Add(new ItemInstance("writing_kit", "Writing Kit"));

        Assert.Equal(4, SkillChecks.GetItemBonus(Skill.Negotiation, state, Balance)); // 3 + 1
    }

    [Fact]
    public void GetItemBonus_Bushcraft_SingleTool()
    {
        var state = Fresh();
        state.Pack.Add(new ItemInstance("yoriks_guide", "Yorik's Guide"));
        state.Equipment.Weapon = new ItemInstance("hatchet", "Hatchet"); // Combat only now

        Assert.Equal(2, SkillChecks.GetItemBonus(Skill.Bushcraft, state, Balance));
    }

    [Fact]
    public void GetItemBonus_Mercantile_UsesTool()
    {
        var state = Fresh();
        state.Pack.Add(new ItemInstance("writing_kit", "Writing Kit"));

        Assert.Equal(2, SkillChecks.GetItemBonus(Skill.Mercantile, state, Balance));
    }

    [Fact]
    public void GetItemBonus_Luck_AlwaysZero()
    {
        var state = Fresh();
        state.Equipment.Weapon = new ItemInstance("arming_sword", "Arming Sword");
        state.Equipment.Armor = new ItemInstance("chainmail", "Chainmail");
        state.Pack.Add(new ItemInstance("writing_kit", "Writing Kit"));

        Assert.Equal(0, SkillChecks.GetItemBonus(Skill.Luck, state, Balance));
    }

    [Fact]
    public void GetItemBonus_NoGear_ReturnsZero()
    {
        var state = Fresh();
        Assert.Equal(0, SkillChecks.GetItemBonus(Skill.Combat, state, Balance));
        Assert.Equal(0, SkillChecks.GetItemBonus(Skill.Cunning, state, Balance));
        Assert.Equal(0, SkillChecks.GetItemBonus(Skill.Negotiation, state, Balance));
    }

    [Fact]
    public void GetItemBonus_BootsDontAffectEncounterChecks()
    {
        var state = Fresh();
        state.Equipment.Boots = new ItemInstance("heavy_work_boots", "Heavy Work Boots");

        foreach (var skill in new[] { Skill.Combat, Skill.Cunning, Skill.Negotiation, Skill.Bushcraft, Skill.Mercantile, Skill.Luck })
            Assert.Equal(0, SkillChecks.GetItemBonus(skill, state, Balance));
    }

    [Fact]
    public void GetItemBonus_ConsumablesInHaversack_Ignored()
    {
        var state = Fresh();
        state.Haversack.Add(new ItemInstance("bandages", "Bandages")); // consumable, not a tool

        Assert.Equal(0, SkillChecks.GetItemBonus(Skill.Negotiation, state, Balance));
    }

    // ── Token bonus tests ──

    [Fact]
    public void GetItemBonus_TokenAddsOneToMatchingSkill()
    {
        var state = Fresh();
        state.Equipment.Weapon = new ItemInstance("arming_sword", "Arming Sword"); // +4 combat
        state.Haversack.Add(new ItemInstance("ivory_comb", "Ivory Comb")); // +1 negotiation token

        Assert.Equal(4, SkillChecks.GetItemBonus(Skill.Combat, state, Balance)); // no token for combat
        Assert.Equal(1, SkillChecks.GetItemBonus(Skill.Negotiation, state, Balance)); // token only
    }

    [Fact]
    public void GetItemBonus_TokenStacksWithGear()
    {
        var state = Fresh();
        state.Pack.Add(new ItemInstance("peoples_borderlands", "Peoples of the Borderlands")); // +3 negotiation
        state.Haversack.Add(new ItemInstance("ivory_comb", "Ivory Comb")); // +1 negotiation token

        Assert.Equal(4, SkillChecks.GetItemBonus(Skill.Negotiation, state, Balance)); // 3 + 1
    }

    // ── Resist bonus tests ──

    [Fact]
    public void GetResistBonus_Injured_UsesArmor()
    {
        var state = Fresh();
        state.Equipment.Armor = new ItemInstance("leather", "Leather"); // injured = Small → +2

        Assert.Equal(2, SkillChecks.GetResistBonus("injured", state, Balance));
    }

    [Fact]
    public void GetResistBonus_Exhausted_UsesBootsPlusPackTool()
    {
        var state = Fresh();
        state.Equipment.Boots = new ItemInstance("heavy_work_boots", "Heavy Work Boots"); // exhausted = Medium → +3
        state.Pack.Add(new ItemInstance("sleeping_kit", "Sleeping Kit")); // exhausted = Medium → +3

        Assert.Equal(6, SkillChecks.GetResistBonus("exhausted", state, Balance)); // 3 + 3
    }

    [Fact]
    public void GetResistBonus_Freezing_UsesTwoBestPackTools()
    {
        var state = Fresh();
        state.Pack.Add(new ItemInstance("heavy_furs", "Heavy Furs")); // freezing = Large → +4

        Assert.Equal(4, SkillChecks.GetResistBonus("freezing", state, Balance));
    }

    [Fact]
    public void GetResistBonus_SwampFever_UsesTool()
    {
        var state = Fresh();
        state.Pack.Add(new ItemInstance("insect_netting", "Insect Netting")); // swamp_fever = Medium → +3

        Assert.Equal(3, SkillChecks.GetResistBonus("swamp_fever", state, Balance));
    }

    [Fact]
    public void GetResistBonus_NoGear_ReturnsZero()
    {
        var state = Fresh();
        Assert.Equal(0, SkillChecks.GetResistBonus("injured", state, Balance));
        Assert.Equal(0, SkillChecks.GetResistBonus("freezing", state, Balance));
        Assert.Equal(0, SkillChecks.GetResistBonus("swamp_fever", state, Balance));
    }
}
