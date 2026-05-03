using System;
using System.Collections.Generic;
using System.Linq;

namespace TripleAction;

public sealed record Move(string Base, IReadOnlySet<string> Mutators)
{
    public string Encoded => Mutators.Count == 0
        ? Capitalize(Base)
        : string.Join(" ", Mutators.OrderBy(m => m, StringComparer.Ordinal).Select(Capitalize)) + " " + Capitalize(Base);

    public static Move Parse(string s)
    {
        var tokens = s.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                      .Select(t => t.ToLowerInvariant()).ToArray();
        if (tokens.Length == 0) throw new ArgumentException("empty move string");
        var basis = tokens[^1];
        if (!Bases.Contains(basis)) throw new ArgumentException($"unknown base action: '{basis}' in '{s}'");
        var muts = tokens[..^1].ToHashSet();
        var allowed = MutatorsFor(basis);
        foreach (var m in muts)
            if (!allowed.Contains(m)) throw new ArgumentException($"invalid mutator '{m}' on {basis}");
        return new Move(basis, muts);
    }

    static string Capitalize(string s) => char.ToUpper(s[0]) + s[1..];

    static readonly HashSet<string> Bases = new() { "attack", "defend", "recover", "read", "skipped" };

    public static readonly HashSet<string> AttackMutators = new()
    {
        "big", "weak", "riposte", "brutal", "lattice", "irradiated", "venomous",
        "terrifying", "provoking", "stunning", "rare", "mythic", "exhausting", "telegraphed"
    };
    public static readonly HashSet<string> DefendMutators = new()
    {
        "big", "perfect", "shielding", "stunning", "rare", "mythic"
    };
    public static readonly HashSet<string> RecoverMutators = new()
    {
        "big", "wary", "shielded", "rare", "mythic", "enraging"
    };
    public static readonly HashSet<string> ReadMutators = new()
    {
        "deep", "wary"
    };

    static IReadOnlySet<string> MutatorsFor(string @base) => @base switch
    {
        "attack" => AttackMutators,
        "defend" => DefendMutators,
        "recover" => RecoverMutators,
        "read" => ReadMutators,
        "skipped" => new HashSet<string>(),
        _ => throw new InvalidOperationException()
    };
}

public sealed record Side(string Name, int Hp, IReadOnlyList<Move> Moves);

public sealed record EncounterTemplate(string Name, Side Monster, Side Player);

public sealed record SlotResult(int PlayerDelta, int AiDelta, bool StunPlayerNext, bool StunAiNext);

public static class Resolver
{
    public static SlotResult Resolve(Move p, Move a, Random rng)
    {
        // Wary: Recover/Read paired against Attack converts to basic Defend.
        if (HasWary(p) && a.Base == "attack") p = new Move("defend", new HashSet<string>());
        if (HasWary(a) && p.Base == "attack") a = new Move("defend", new HashSet<string>());

        int pOut = OutgoingDamage(p, a);
        int aOut = OutgoingDamage(a, p);
        int pPrev = Prevention(p, a);
        int aPrev = Prevention(a, p);

        int pTaken = Math.Max(0, aOut - pPrev);
        int aTaken = Math.Max(0, pOut - aPrev);

        int pHeal = Heal(p, a);
        int aHeal = Heal(a, p);

        bool pStun = StunsTarget(a, p, rng);
        bool aStun = StunsTarget(p, a, rng);

        // Exhausting attack self-stuns.
        if (p.Base == "attack" && p.Mutators.Contains("exhausting")) pStun = true;
        if (a.Base == "attack" && a.Mutators.Contains("exhausting")) aStun = true;

        // Shielding Defend blocks all incoming statuses.
        if (p.Base == "defend" && p.Mutators.Contains("shielding")) pStun = false;
        if (a.Base == "defend" && a.Mutators.Contains("shielding")) aStun = false;

        return new SlotResult(-pTaken + pHeal, -aTaken + aHeal, pStun, aStun);
    }

    static bool HasWary(Move m) => (m.Base == "recover" || m.Base == "read") && m.Mutators.Contains("wary");

    static int OutgoingDamage(Move attacker, Move defender)
    {
        if (attacker.Base != "attack") return 0;
        int dmg = 4;
        if (attacker.Mutators.Contains("big")) dmg += 4;
        if (attacker.Mutators.Contains("weak")) dmg -= 2;
        if (attacker.Mutators.Contains("riposte") && defender.Base == "attack") dmg += 2;
        return Math.Max(0, dmg);
    }

    static int Prevention(Move defender, Move attacker)
    {
        if (defender.Base == "defend")
        {
            if (defender.Mutators.Contains("perfect")) return 999;
            int p = 2;
            if (defender.Mutators.Contains("big")) p += 2;
            return p;
        }
        if (defender.Base == "attack" && defender.Mutators.Contains("riposte") && attacker.Base == "attack")
            return 2;
        if (defender.Base == "recover" && defender.Mutators.Contains("shielded"))
            return 2;
        return 0;
    }

    static int Heal(Move m, Move opp)
    {
        if (m.Base != "recover") return 0;
        if (opp.Base == "attack") return 0; // Attack cancels Recover.
        int heal = 4;
        if (m.Mutators.Contains("big")) heal += 2;
        return heal;
    }

    // Returns: does `actor` cause `target` to be stunned next slot?
    static bool StunsTarget(Move actor, Move target, Random rng)
    {
        // Attack against Recover always stuns the recoverer.
        if (actor.Base == "attack" && target.Base == "recover") return true;
        // Stunning Attack: 50% chance to stun whoever they hit.
        if (actor.Base == "attack" && actor.Mutators.Contains("stunning") && rng.NextDouble() < 0.5) return true;
        // Stunning Defend vs Attack: 50% chance to stun the attacker.
        if (actor.Base == "defend" && actor.Mutators.Contains("stunning") && target.Base == "attack" && rng.NextDouble() < 0.5) return true;
        return false;
    }
}

public static class Tells
{
    public static string For(IReadOnlyList<Move> aiCommit, string enemyName)
    {
        if (aiCommit.Any(m => m.Base == "attack" && m.Mutators.Contains("telegraphed")))
            return $"{enemyName} is preparing a heavy attack!";
        if (aiCommit.Count(m => m.Base == "attack") >= 2)
            return $"{enemyName} is pressing the attack";
        if (aiCommit.Count(m => m.Base == "defend") >= 2)
            return $"{enemyName} is on their back foot";
        if (aiCommit.Count(m => m.Base == "recover") >= 2)
            return $"{enemyName} is winded";
        return $"{enemyName} is wary and awaits your move";
    }
}

public static class Templates
{
    public static IReadOnlyDictionary<string, EncounterTemplate> Load(string path)
    {
        var lines = File.ReadAllLines(path);
        var result = new Dictionary<string, EncounterTemplate>();

        string? id = null, display = null;
        string? side = null;          // "monster" or "player"
        string? sideName = null;
        int sideHp = 0;
        var sideMoves = new List<Move>();
        Side? monster = null, player = null;

        void Flush()
        {
            if (side == "monster") monster = new Side(sideName!, sideHp, sideMoves.ToList());
            else if (side == "player") player = new Side(sideName!, sideHp, sideMoves.ToList());
            sideMoves.Clear();
            side = null;
        }

        void Commit()
        {
            Flush();
            if (id != null)
            {
                if (monster == null) throw new InvalidOperationException($"template '{id}' missing monster section");
                if (player == null) throw new InvalidOperationException($"template '{id}' missing player section");
                result[id] = new EncounterTemplate(display ?? id, monster, player);
            }
            id = display = null;
            monster = player = null;
        }

        for (int lineNum = 0; lineNum < lines.Length; lineNum++)
        {
            var raw = lines[lineNum];
            var trimmed = raw.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith("#")) continue;

            if (trimmed.StartsWith("==="))
            {
                Commit();
                var rest = trimmed.TrimStart('=').Trim();
                var bar = rest.IndexOf('|');
                id = (bar >= 0 ? rest[..bar] : rest).Trim();
                display = bar >= 0 ? rest[(bar + 1)..].Trim() : id;
                continue;
            }

            if (trimmed.StartsWith("monster:") || trimmed.StartsWith("player:"))
            {
                Flush();
                var colon = trimmed.IndexOf(':');
                side = trimmed[..colon];
                var header = trimmed[(colon + 1)..].Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (header.Length < 2 || !int.TryParse(header[^1], out sideHp))
                    throw new FormatException($"line {lineNum + 1}: expected '<side>: <name> <hp>'");
                sideName = string.Join(' ', header[..^1]);
                continue;
            }

            if (side == null) throw new FormatException($"line {lineNum + 1}: move outside any side section");
            sideMoves.Add(Move.Parse(trimmed));
        }
        Commit();
        return result;
    }
}
