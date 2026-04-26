using CombatPrototype.Cmb;

namespace CombatPrototype.Combat;

/// <summary>
/// Pure resolution helpers. d20 + bonus vs DC; crits on nat-20, fumbles on nat-1.
/// </summary>
public static class Resolver
{
    public sealed record AttackOutcome(int Roll, int Total, int TargetAc, bool Hit, bool Crit, bool Fumble);

    public static AttackOutcome RollAttack(Random rng, int bonus, int targetAc)
    {
        int d = rng.Next(1, 21);
        bool crit = d == 20;
        bool fumble = d == 1;
        int total = d + bonus;
        bool hit = !fumble && (crit || total >= targetAc);
        return new AttackOutcome(d, total, targetAc, hit, crit, fumble);
    }

    public sealed record SaveOutcome(int Roll, int Total, int Dc, bool Success);

    public static SaveOutcome RollSave(Random rng, int bonus, int dc)
    {
        int d = rng.Next(1, 21);
        int total = d + bonus;
        return new SaveOutcome(d, total, dc, total >= dc);
    }

    public sealed record DamageResult(int[] Dice, int Sides, int Modifier, bool Crit, int Total)
    {
        public string DiceString => $"{Dice.Length}d{Sides}({string.Join(",", Dice)})";
    }

    /// <summary>Roll damage, doubling the dice on a crit (D&D 5e style — modifier not doubled).</summary>
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
        int total = Math.Max(1, diceTotal + dmg.Modifier);  // never less than 1 on a hit
        return new DamageResult(dice, dmg.Sides, dmg.Modifier, crit, total);
    }
}
