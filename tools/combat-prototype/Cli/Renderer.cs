using CombatPrototype.Cmb;
using CombatPrototype.Combat;

namespace CombatPrototype.Cli;

public sealed class Renderer : ICombatObserver
{
    public void OnIntro(CombatState s)
    {
        Console.WriteLine();
        Console.WriteLine($"=== {s.Encounter.Title} ===");
        Console.WriteLine();
        if (!string.IsNullOrEmpty(s.Encounter.Intro))
        {
            Console.WriteLine(s.Encounter.Intro);
            Console.WriteLine();
        }
        Console.WriteLine($"Monster: HP {s.MonsterHp}, AC {s.Encounter.Stats.Ac}, to-hit {s.Encounter.Stats.ToHit:+0;-0;0}, dmg {s.Encounter.Stats.Damage}");
        Console.WriteLine($"Player:  Spirits {s.Player.Spirits}/{s.Player.MaxSpirits}, Health {s.Player.Health}/{s.Player.MaxHealth}, AC {s.Player.EffectiveAc}, attack {s.Player.AttackToHitBonus:+0;-0;0} / damage {s.Player.DamageRoll}, stance {s.Player.Stance}");
        Console.WriteLine();
        Console.WriteLine("Type 'help' to see commands.");
        Console.WriteLine();
    }

    public void OnSurpriseCheck(int roll, int bonus, int dc, bool playerFirst)
    {
        string verdict = playerFirst ? "you go first" : "monster goes first";
        Console.WriteLine($"Surprise: d20({roll}) + Bushcraft({bonus}) vs DC {dc} → {verdict}.");
        Console.WriteLine();
    }

    public void OnRoundStart(CombatState s, bool playerFirst)
    {
        Console.WriteLine($"--- Round {s.Round} ---");
        Console.WriteLine($"  Player: {s.Player.Spirits}sp / {s.Player.Health}hp, AC {s.Player.EffectiveAc}, stance {s.Player.Stance}");
        Console.WriteLine($"  Monster: {s.MonsterHp}/{s.Encounter.Stats.Hp} hp");
        Console.WriteLine($"  Order: {(playerFirst ? "you act first" : "monster acts first")}");
    }

    public void OnIntentPreview(CombatState s, MonsterMove next)
    {
        Console.WriteLine($"  ▶ Intent: {IntentLabel(next.IntentClass)} — \"{next.IntentText}\"");
    }

    public void OnPlayerStanceChanged(SwordStance from, SwordStance to)
    {
        Console.WriteLine($"  Player switches stance: {from} → {to}.");
    }

    public void OnPlayerAttack(CombatState s, Resolver.AttackOutcome o, Resolver.DamageResult? damage)
    {
        int baseAtk = s.Player.BaseAttackBonus;
        int stanceAtk = StanceModifiers.For(s.Player.Stance).attack;
        int baseDmg = s.Player.BaseDamageBonus;

        if (o.Fumble)
        {
            Console.WriteLine($"  Player attacks: d20(1) → fumble (miss)");
            return;
        }
        if (!o.Hit)
        {
            Console.WriteLine($"  Player attacks: d20({o.Roll}) {Sign(baseAtk)}(atk) {Sign(stanceAtk)}(stance) = {o.Total} vs AC {o.TargetAc} → miss");
            return;
        }

        string atkLine = o.Crit
            ? $"  Player attacks: d20(20) → CRIT"
            : $"  Player attacks: d20({o.Roll}) {Sign(baseAtk)}(atk) {Sign(stanceAtk)}(stance) = {o.Total} vs AC {o.TargetAc} → hit";
        Console.WriteLine(atkLine);

        if (damage is not null)
        {
            Console.WriteLine($"  damage: {damage.DiceString} {Sign(baseDmg)}(base) {Sign(stanceAtk)}(stance) = {damage.Total} (monster HP {s.MonsterHp}/{s.Encounter.Stats.Hp})");
        }
    }

    public void OnPlayerFleeAttempt(Resolver.SaveOutcome save)
    {
        string verdict = save.Success ? "ESCAPE" : "fail";
        Console.WriteLine($"  Flee: d20({save.Roll}) + Cunning = {save.Total} vs DC {save.Dc} → {verdict}.");
        if (!save.Success) Console.WriteLine("  Monster gets a free hit.");
    }

    public void OnMonsterMove(CombatState s, MonsterMove move)
    {
        Console.WriteLine($"  ▶ {s.Encounter.Title}: {move.Narration}");
    }

    public void OnMonsterAttack(CombatState s, MonsterMove move, Resolver.AttackOutcome o, Resolver.DamageResult? damage, DamageBreakdown? absorbed)
    {
        int atkBonus = s.Encounter.Stats.ToHit;

        if (o.Fumble)
        {
            Console.WriteLine($"    attack: d20(1) → fumble (miss)");
            return;
        }
        if (!o.Hit)
        {
            Console.WriteLine($"    attack: d20({o.Roll}) {Sign(atkBonus)}(atk) = {o.Total} vs AC {o.TargetAc} → miss");
            return;
        }

        string atkLine = o.Crit
            ? "    attack: d20(20) → CRIT"
            : $"    attack: d20({o.Roll}) {Sign(atkBonus)}(atk) = {o.Total} vs AC {o.TargetAc} → hit";
        Console.WriteLine(atkLine);

        if (damage is not null && absorbed is not null)
        {
            Console.WriteLine($"    damage: {damage.DiceString} {Sign(damage.Modifier)} = {damage.Total} → {absorbed.OnSpirits}sp + {absorbed.OnHealth}hp absorbed. Player: {s.Player.Spirits}sp / {s.Player.Health}hp");
        }
    }

    public void OnMonsterPierce(CombatState s, MonsterMove move, Resolver.SaveOutcome save, Resolver.DamageResult? damage, DamageBreakdown? absorbed)
    {
        string verdict = save.Success ? "evade" : "fail";
        Console.WriteLine($"    Cunning save: d20({save.Roll}) {Sign(s.Player.Cunning)}(cun) = {save.Total} vs DC {save.Dc} → {verdict}");

        if (!save.Success && damage is not null && absorbed is not null)
        {
            Console.WriteLine($"    damage: {damage.DiceString} {Sign(damage.Modifier)} = {damage.Total} → {absorbed.OnSpirits}sp + {absorbed.OnHealth}hp absorbed. Player: {s.Player.Spirits}sp / {s.Player.Health}hp");
        }
    }

    public void OnMonsterCondition(CombatState s, MonsterMove move, string id, double chance, bool procced, Resolver.SaveOutcome? save)
    {
        if (!procced)
        {
            Console.WriteLine($"    {id} ({chance:P0} chance) → did not proc.");
            return;
        }
        if (save is null)
        {
            Console.WriteLine($"    {id} applied (stub — condition system not in prototype).");
            return;
        }
        if (save.Success)
            Console.WriteLine($"    {id}: d20({save.Roll}) + Cunning = {save.Total} vs DC {save.Dc} → resisted.");
        else
            Console.WriteLine($"    {id}: d20({save.Roll}) + Cunning = {save.Total} vs DC {save.Dc} → applied (stub).");
    }

    public void OnMonsterDefend(CombatState s, MonsterMove move, int acBonus)
    {
        Console.WriteLine($"    AC +{acBonus} this turn (now {s.MonsterEffectiveAc}).");
    }

    public void OnMonsterFlee(CombatState s, MonsterMove move)
    {
        Console.WriteLine($"    {s.Encounter.Title} disengages and flees.");
    }

    public void OnOutcome(CombatState s)
    {
        Console.WriteLine();
        Console.WriteLine("=== Outcome ===");
        if (s.PlayerWins)
        {
            Console.WriteLine("Result: VICTORY");
            if (!string.IsNullOrEmpty(s.Encounter.WinText)) Console.WriteLine(s.Encounter.WinText);
            if (s.Encounter.WinMechanics.Count > 0)
                Console.WriteLine($"Rewards: {string.Join(", ", s.Encounter.WinMechanics)}");
        }
        else if (s.PlayerLoses)
        {
            Console.WriteLine("Result: DEFEAT");
            if (!string.IsNullOrEmpty(s.Encounter.LoseText)) Console.WriteLine(s.Encounter.LoseText);
            if (s.Encounter.LoseMechanics.Count > 0)
                Console.WriteLine($"Penalty: {string.Join(", ", s.Encounter.LoseMechanics)}");
        }
        else if (s.PlayerFled)
        {
            Console.WriteLine("Result: PLAYER FLED");
        }
        else if (s.MonsterFled)
        {
            Console.WriteLine("Result: MONSTER FLED");
        }
        else if (s.Aborted)
        {
            Console.WriteLine("Result: ABORTED (no rewards or penalties applied)");
        }
        Console.WriteLine($"Rounds: {s.Round}");
    }

    private static string Sign(int n) => n >= 0 ? $"+{n}" : n.ToString();

    private static string IntentLabel(IntentClass c) => c switch
    {
        IntentClass.Attack       => "Attack",
        IntentClass.HeavyAttack  => "BIG ATTACK",
        IntentClass.Defend       => "Defend",
        IntentClass.Pierce       => "Pierce (armor-bypass)",
        IntentClass.Condition    => "Inflict",
        IntentClass.Flee         => "Flee",
        _ => c.ToString()
    };
}
