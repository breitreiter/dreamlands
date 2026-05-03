namespace Dreamlands.Encounter;

/// <summary>
/// One slot's chosen action. Always a base family (attack/defend/recover/read/skipped)
/// plus zero or more mutators that adjust its behaviour. Encoded form is what the
/// CLI/UI surface — e.g. "Big Telegraphed Rare Attack". Parse is the inverse.
///
/// Mutator validity is enforced at parse time per family. The `Skipped` family takes
/// no mutators; it's only ever produced by Stunned conversions or Flee.
/// </summary>
public sealed class Move
{
    public string Base { get; set; } = "";
    public HashSet<string> Mutators { get; set; } = new();

    /// <summary>Authoring marker (the "Better" adjective in source form). When
    /// true, this move is a strict improvement over its unmutated base verb,
    /// and the player's pool drops that bare base when this move is in it.
    /// Not a gameplay mutator — the resolver never reads it, and it does not
    /// appear in <see cref="Encoded"/>.</summary>
    public bool SupersedesBase { get; set; }

    public Move() { }
    public Move(string @base, HashSet<string> mutators)
    {
        Base = @base;
        Mutators = mutators;
    }

    public bool Has(string mutator) => Mutators.Contains(mutator);

    public string Encoded => Mutators.Count == 0
        ? Capitalize(Base)
        : string.Join(" ", Mutators.OrderBy(m => m, StringComparer.Ordinal).Select(Capitalize)) + " " + Capitalize(Base);

    public override string ToString() => Encoded;

    static string Capitalize(string s) => s.Length == 0 ? s : char.ToUpper(s[0]) + s[1..];

    public static Move Skipped() => new("skipped", new HashSet<string>());

    /// <summary>Authoring adjective for <see cref="SupersedesBase"/>. Stripped
    /// before mutator validation; never appears in canonical encoded form.</summary>
    public const string BetterAdjective = "better";

    public static Move Parse(string s)
    {
        var raw = s.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                   .Select(t => t.ToLowerInvariant()).ToList();
        bool supersedes = raw.Remove(BetterAdjective);
        var tokens = raw.ToArray();
        if (tokens.Length == 0) throw new ArgumentException("empty move string");
        var basis = tokens[^1];
        if (!Bases.Contains(basis)) throw new ArgumentException($"unknown base action: '{basis}' in '{s}'");
        var muts = tokens[..^1].ToHashSet();
        var allowed = MutatorsFor(basis);
        foreach (var m in muts)
            if (!allowed.Contains(m)) throw new ArgumentException($"invalid mutator '{m}' on {basis}: '{s}'");
        return new Move(basis, muts) { SupersedesBase = supersedes };
    }

    public static readonly HashSet<string> Bases = new() { "attack", "defend", "recover", "read", "skipped" };

    // Mutator catalogues are the canonical source of truth; super_rps.md tracks the
    // design names. Keep these in lockstep with that doc.
    public static readonly HashSet<string> AttackMutators = new()
    {
        "heavy", "weak", "riposte",
        "brutal", "tainted", "glowing", "venomous",
        "terrifying", "provoking", "stunning",
        "power", "slow", "exhausting", "telegraphed"
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
        "wary"
    };

    static IReadOnlySet<string> MutatorsFor(string @base) => @base switch
    {
        "attack"  => AttackMutators,
        "defend"  => DefendMutators,
        "recover" => RecoverMutators,
        "read"    => ReadMutators,
        "skipped" => new HashSet<string>(),
        _ => throw new InvalidOperationException()
    };
}
