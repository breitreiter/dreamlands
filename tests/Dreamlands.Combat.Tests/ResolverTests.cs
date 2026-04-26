using Dreamlands.Combat;
using Dreamlands.Encounter;

namespace Dreamlands.Combat.Tests;

public class ResolverTests
{
    [Fact]
    public void Crit_doubles_dice_not_modifier()
    {
        // Force a deterministic damage roll with seeded RNG.
        var rng = new Random(7);
        var dmg = Resolver.RollDamage(rng, new DiceRoll(2, 6, 3), crit: true);
        Assert.Equal(4, dmg.Dice.Length); // 2d6 doubled to 4d6
        Assert.Equal(3, dmg.Modifier);
        Assert.Equal(dmg.Dice.Sum() + 3, dmg.Total);
    }

    [Fact]
    public void Hit_damage_floors_at_one()
    {
        var rng = new Random(0);
        // 1d4 - 10 → minimum die roll is 1, but result floored to 1, not negative.
        var dmg = Resolver.RollDamage(rng, new DiceRoll(1, 4, -10), crit: false);
        Assert.True(dmg.Total >= 1);
    }

    [Fact]
    public void Nat_one_is_fumble_and_misses()
    {
        // d20 always returns 1 if seeded carefully? Easier: assert the property holds across many rolls.
        var rng = new Random(123);
        for (int i = 0; i < 200; i++)
        {
            var outcome = Resolver.RollAttack(rng, bonus: 100, targetAc: 5);
            if (outcome.Roll == 1)
            {
                Assert.True(outcome.Fumble);
                Assert.False(outcome.Hit);
                Assert.False(outcome.Crit);
            }
            if (outcome.Roll == 20)
            {
                Assert.True(outcome.Crit);
                Assert.True(outcome.Hit);
                Assert.False(outcome.Fumble);
            }
        }
    }

    [Fact]
    public void Save_succeeds_when_total_meets_dc()
    {
        var rng = new Random(42);
        var save = Resolver.RollSave(rng, bonus: 5, dc: 10);
        Assert.Equal(save.Roll + 5, save.Total);
        Assert.Equal(save.Total >= 10, save.Success);
    }
}
