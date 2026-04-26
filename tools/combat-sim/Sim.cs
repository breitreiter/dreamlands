using Dreamlands.Combat;
using Dreamlands.Encounter;

namespace CombatSim;

public sealed record TrialOutcome(
    bool PlayerWon, bool PlayerLost, int Rounds, int DamageDealt, int DamageTaken);

public sealed class CellResult
{
    public required string PolicyName;
    public required PcProfile Pc;
    public required MonsterBaseline Monster;

    public int Wins;
    public int Losses;
    public int Stalemates;
    public List<int> RoundsWon = new();
    public List<int> RoundsLost = new();
    public List<int> DamageDealt = new();
    public List<int> DamageTaken = new();

    public int Trials => Wins + Losses + Stalemates;
    public double FatalityRate => Trials == 0 ? 0 : (double)Losses / Trials;
    public double AvgRounds => RoundsWon.Concat(RoundsLost).DefaultIfEmpty(0).Average();
    public double AvgDmgDealtPerRound =>
        RoundsWon.Concat(RoundsLost).Sum() == 0 ? 0
        : (double)DamageDealt.Sum() / RoundsWon.Concat(RoundsLost).Sum();
}

public static class Sim
{
    const int MaxRounds = 100;  // safety cap; resolved combats are well below

    public static CellResult Run(
        WeaponPolicy policy, PcProfile pc, MonsterBaseline monster,
        int trials, int seed)
    {
        var cell = new CellResult { PolicyName = policy.Name, Pc = pc, Monster = monster };
        for (int t = 0; t < trials; t++)
        {
            var rng = new Random(seed + t);
            policy.Reset(pc);
            var outcome = RunTrial(policy, pc, monster, rng);
            if (outcome.PlayerWon)      { cell.Wins++;       cell.RoundsWon.Add(outcome.Rounds); }
            else if (outcome.PlayerLost){ cell.Losses++;     cell.RoundsLost.Add(outcome.Rounds); }
            else                        { cell.Stalemates++; }
            cell.DamageDealt.Add(outcome.DamageDealt);
            cell.DamageTaken.Add(outcome.DamageTaken);
        }
        return cell;
    }

    static TrialOutcome RunTrial(WeaponPolicy policy, PcProfile pc, MonsterBaseline monster, Random rng)
    {
        int spirits = Baselines.StartingSpirits;
        int health  = Baselines.StartingHealth;
        int monsterHp = monster.Hp;
        int heavyCd = monster.HeavyTimer;
        int round = 0;
        int damageDealt = 0;
        int damageTaken = 0;

        bool playerActsFirst = Resolver.RollSave(rng, pc.Bushcraft, Baselines.SurpriseDc).Success;

        while (monsterHp > 0 && health > 0 && round < MaxRounds)
        {
            round++;

            if (playerActsFirst)
            {
                var intent = ChooseMonsterIntent(heavyCd);
                int dmg = policy.ChooseAndExecute(intent, monster.Ac, monsterHp, spirits, health, rng);
                damageDealt += dmg;
                monsterHp -= dmg;
                if (monsterHp <= 0) break;

                ResolveMonsterTurn(intent, ref heavyCd, ref spirits, ref health, ref damageTaken,
                    monster, policy, rng);
                if (health <= 0) break;
            }
            else
            {
                var intent = ChooseMonsterIntent(heavyCd);
                ResolveMonsterTurn(intent, ref heavyCd, ref spirits, ref health, ref damageTaken,
                    monster, policy, rng);
                if (health <= 0) break;

                // Player now sees the NEXT monster turn's intent (computed from the just-ticked cooldown).
                var nextIntent = ChooseMonsterIntent(heavyCd);
                int dmg = policy.ChooseAndExecute(nextIntent, monster.Ac, monsterHp, spirits, health, rng);
                damageDealt += dmg;
                monsterHp -= dmg;
                if (monsterHp <= 0) break;
            }
        }

        return new TrialOutcome(
            PlayerWon: monsterHp <= 0 && health > 0,
            PlayerLost: health <= 0,
            Rounds: round,
            DamageDealt: damageDealt,
            DamageTaken: damageTaken);
    }

    static IntentClass ChooseMonsterIntent(int heavyCooldown) =>
        heavyCooldown <= 0 ? IntentClass.HeavyAttack : IntentClass.Attack;

    static void ResolveMonsterTurn(
        IntentClass intent, ref int heavyCd, ref int spirits, ref int health, ref int damageTaken,
        MonsterBaseline monster, WeaponPolicy policy, Random rng)
    {
        int playerAc = policy.EffectiveAc;
        int dmg = 0;
        if (intent == IntentClass.HeavyAttack)
        {
            var atk = Resolver.RollAttack(rng, monster.ToHit, playerAc);
            if (atk.Hit) dmg = Resolver.RollDamage(rng, monster.HeavyDamage, atk.Crit).Total;
            heavyCd = monster.HeavyTimer;
        }
        else
        {
            var atk = Resolver.RollAttack(rng, monster.ToHit, playerAc);
            if (atk.Hit) dmg = Resolver.RollDamage(rng, monster.BasicDamage, atk.Crit).Total;
            heavyCd = Math.Max(0, heavyCd - 1);
        }

        // Full negation if the player declared an immunity action this turn (axe Block).
        if (policy.ImmuneToIncomingDamage) dmg = 0;

        damageTaken += dmg;
        int onSpirits = Math.Min(spirits, dmg);
        spirits -= onSpirits;
        int overflow = dmg - onSpirits;
        int onHealth = Math.Min(health, overflow);
        health -= onHealth;

        policy.OnMonsterTurnComplete();
    }
}

public static class Report
{
    public static void PrintTable(string heading, IReadOnlyList<CellResult> cells)
    {
        Console.WriteLine();
        Console.WriteLine($"=== {heading} ===");
        Console.WriteLine($"{"profile",-22} {"trials",7} {"fatality",10} {"avgRnds",8} {"dmg/r",7}");
        Console.WriteLine(new string('-', 60));
        foreach (var c in cells)
        {
            Console.WriteLine(
                $"{c.Pc.Label,-22} {c.Trials,7} " +
                $"{c.FatalityRate,9:P1} {c.AvgRounds,8:F1} {c.AvgDmgDealtPerRound,7:F2}");
        }
    }
}
