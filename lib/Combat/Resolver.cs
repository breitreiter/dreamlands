using Dreamlands.Encounter;

namespace Dreamlands.Combat;

/// <summary>
/// Pure resolution helpers. d20 + bonus vs DC; crits on nat-20 (double dice, modifier
/// not doubled — D&D 5e style); fumbles on nat-1.
/// </summary>
public static class Resolver
{
    public sealed record AttackOutcome(int Roll, int Total, int TargetAc, bool Hit, bool Crit, bool Fumble);
    public sealed record SaveOutcome(int Roll, int Total, int Dc, bool Success);
    public sealed record DamageResult(int[] Dice, int Sides, int Modifier, bool Crit, int Total);

    public static AttackOutcome RollAttack(Random rng, int bonus, int targetAc)
    {
        int d = rng.Next(1, 21);
        bool crit = d == 20;
        bool fumble = d == 1;
        int total = d + bonus;
        bool hit = !fumble && (crit || total >= targetAc);
        return new AttackOutcome(d, total, targetAc, hit, crit, fumble);
    }

    public static SaveOutcome RollSave(Random rng, int bonus, int dc)
    {
        int d = rng.Next(1, 21);
        int total = d + bonus;
        return new SaveOutcome(d, total, dc, total >= dc);
    }

    public static DamageResult RollDamage(Random rng, DiceRoll dmg, bool crit)
    {
        int rolls = dmg.Count * (crit ? 2 : 1);
        int[] dice = new int[rolls];
        int diceTotal = 0;
        for (int i = 0; i < rolls; i++)
        {
            dice[i] = rng.Next(1, dmg.Sides + 1);
            diceTotal += dice[i];
        }
        // Hit damage is at least 1 (negative weapon mod can't reduce below 1).
        int total = Math.Max(1, diceTotal + dmg.Modifier);
        return new DamageResult(dice, dmg.Sides, dmg.Modifier, crit, total);
    }
}

/// <summary>Result of applying damage to the player's Spirits-then-Health pool.</summary>
public sealed record DamageBreakdown(int Total, int OnSpirits, int OnHealth, int SpiritsBefore, int HealthBefore);
