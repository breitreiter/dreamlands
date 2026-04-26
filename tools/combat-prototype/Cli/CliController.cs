using CombatPrototype.Cmb;
using CombatPrototype.Combat;

namespace CombatPrototype.Cli;

public sealed class CliController : IPlayerController
{
    public PlayerAction GetAction(CombatState state)
    {
        while (true)
        {
            Console.Write("> ");
            string? input = Console.ReadLine();
            if (input is null) return new PlayerAction.Abort();
            string line = input.Trim();
            if (line.Length == 0) continue;

            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            string verb = parts[0].ToLowerInvariant();
            string? arg = parts.Length > 1 ? parts[1].ToLowerInvariant() : null;

            switch (verb)
            {
                case "attack": case "a": case "1":
                    return new PlayerAction.Attack();

                case "flee": case "f": case "3":
                    return new PlayerAction.Flee();

                case "stance": case "s": case "2":
                    var stance = ParseStance(arg) ?? PromptStance(state.Player.Stance);
                    return new PlayerAction.SetStance(stance);

                case "abort": case "quit": case "q": case "exit":
                    return new PlayerAction.Abort();

                case "help": case "?":
                    PrintHelp();
                    break;

                case "status": case "state":
                    PrintStatus(state);
                    break;

                case "inspect": case "monster": case "m":
                    PrintMonsterDetail(state);
                    break;

                default:
                    Console.WriteLine($"  Unknown command '{verb}'. Type 'help' for options.");
                    break;
            }
        }
    }

    private static SwordStance? ParseStance(string? arg) => arg switch
    {
        null         => null,
        "1" or "a" or "aggressive" => SwordStance.Aggressive,
        "2" or "b" or "balanced"   => SwordStance.Balanced,
        "3" or "d" or "defensive"  => SwordStance.Defensive,
        _ => null
    };

    private static SwordStance PromptStance(SwordStance current)
    {
        while (true)
        {
            Console.Write($"  Stance (current: {current}) [aggressive | balanced | defensive]: ");
            string? input = Console.ReadLine();
            if (input is null) return current;
            string line = input.Trim().ToLowerInvariant();
            var s = ParseStance(line);
            if (s is not null) return s.Value;
            Console.WriteLine("  Unknown — try aggressive / balanced / defensive (or a / b / d).");
        }
    }

    private static void PrintHelp()
    {
        Console.WriteLine("  Actions (consume your turn):");
        Console.WriteLine("    attack, a               attack the monster");
        Console.WriteLine("    stance <which>, s <w>   change sword stance (aggressive/balanced/defensive)");
        Console.WriteLine("    flee, f                 attempt to flee (Cunning save)");
        Console.WriteLine("  Inspection (free, do not consume your turn):");
        Console.WriteLine("    status, state           reprint current combat state");
        Console.WriteLine("    inspect, monster        show monster moves, timers, and intent");
        Console.WriteLine("    help, ?                 this help");
        Console.WriteLine("  Other:");
        Console.WriteLine("    abort, quit             abandon the encounter (no result applied)");
    }

    private static void PrintStatus(CombatState s)
    {
        Console.WriteLine($"  Round {s.Round}");
        Console.WriteLine($"  Player:  Spirits {s.Player.Spirits}/{s.Player.MaxSpirits}, Health {s.Player.Health}/{s.Player.MaxHealth}, AC {s.Player.EffectiveAc}, stance {s.Player.Stance}");
        Console.WriteLine($"           attack {s.Player.AttackToHitBonus:+0;-0;0} / damage {s.Player.DamageRoll}, Bushcraft {s.Player.Bushcraft:+0;-0;0}, Cunning {s.Player.Cunning:+0;-0;0}");
        Console.WriteLine($"  Monster: {s.Encounter.Title}, HP {s.MonsterHp}/{s.Encounter.Stats.Hp}, AC {s.MonsterEffectiveAc}");
        Console.WriteLine($"  Intent:  {s.NextMove.IntentClass} — \"{s.NextMove.IntentText}\"");
    }

    private static void PrintMonsterDetail(CombatState s)
    {
        var enc = s.Encounter;
        Console.WriteLine($"  {enc.Title} — HP {enc.Stats.Hp}, AC {enc.Stats.Ac}, to-hit {enc.Stats.ToHit:+0;-0;0}, dmg {enc.Stats.Damage}");
        Console.WriteLine("  Moves:");
        foreach (var move in enc.Moves)
        {
            string cd;
            if (move.IsBasic) cd = "basic (always)";
            else
            {
                int rem = s.Cooldowns.TryGetValue(move.Id, out var v) ? v : 0;
                cd = rem == 0 ? "ready next turn" : $"in {rem} turn(s)";
            }
            string marker = ReferenceEquals(move, s.NextMove) ? " ◀ next" : "";
            Console.WriteLine($"    {move.Id} [{move.IntentClass}] timer {move.Timer}, {cd}{marker}");
            foreach (var m in move.Mechanics)
                Console.WriteLine($"      > {m}");
        }
    }
}
