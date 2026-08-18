using Dreamlands.Encounter;

namespace Dreamlands.Combat;

/// <summary>
/// Per-slot result from <see cref="Resolver.Resolve"/>. Damage deltas are signed
/// (negative = damage taken, positive = healing). Forward-rider flags indicate that
/// the *next* slot for the named side should be locked to Skipped (or carry into
/// next turn's slot 1 if already at slot 3). Berzerk/Fear apply to the named side's
/// next-turn pool restriction.
///
/// Conditions applied land on the player or monster's global condition pool — for
/// the player, that's <c>PlayerState.ActiveConditions</c>. The monster has no
/// persistent condition pool today, so monster conditions are recorded but not
/// stored anywhere; combat-internal RPS conditions (Stunned/Berzerk/Fear) are
/// returned via the dedicated flags.
/// </summary>
public sealed record SlotResult(
    int PlayerDelta,
    int MonsterDelta,
    bool StunPlayerNext,
    bool StunMonsterNext,
    bool BerzerkPlayerNext,
    bool BerzerkMonsterNext,
    bool FearPlayerNext,
    bool FearMonsterNext,
    IReadOnlyList<string> ConditionsAppliedToPlayer,
    IReadOnlyList<string> ConditionsAppliedToMonster);

/// <summary>
/// Pure resolution of one slot pair. No HP clamping, no state mutation — that's
/// the runner's job. The damage values here are the *would-be* deltas; the runner
/// then routes player damage through the Spirits-then-Health ablation pipeline.
/// </summary>
public static class Resolver
{
    /// <summary>
    /// Default proc chance for condition-applying mutators (Brutal, Tainted, Glowing,
    /// Venomous, Stunning). Tunable; matches the prototype's coin-flip baseline.
    /// </summary>
    public const double ConditionProcChance = 0.5;

    public static SlotResult Resolve(Move p, Move m, Random rng)
    {
        // Wary: Recover or Read paired against Attack converts to a basic Defend
        // before resolution. The conversion is symmetric — applies to either side.
        if (HasWary(p) && m.Base == "attack") p = new Move("defend", new HashSet<string>());
        if (HasWary(m) && p.Base == "attack") m = new Move("defend", new HashSet<string>());

        int pOut = OutgoingDamage(p, m);
        int mOut = OutgoingDamage(m, p);
        int pPrev = Prevention(p, m);
        int mPrev = Prevention(m, p);

        int pTaken = Absorb(p, mOut, pPrev);
        int mTaken = Absorb(m, pOut, mPrev);

        int pHeal = Heal(p, m);
        int mHeal = Heal(m, p);

        bool stunP = StunsTarget(m, p, rng);
        bool stunM = StunsTarget(p, m, rng);

        // A guard that stops an Attack costs the attacker their next slot. This is what
        // makes Attack lose to Defend, and it is deliberately TEMPO rather than damage:
        // mitigation never repays the slot it costs, so the only lever that changes the
        // meta is one that takes slots off the aggressor. Applies to every Defend,
        // mutated or not, and to a Wary Recover/Read that converted to one above.
        // See plans/defend_stagger.md.
        if (p.Base == "defend" && m.Base == "attack") stunM = true;
        if (m.Base == "defend" && p.Base == "attack") stunP = true;

        // Exhausting attack self-stuns regardless of outcome.
        if (p.Base == "attack" && p.Has("exhausting")) stunP = true;
        if (m.Base == "attack" && m.Has("exhausting")) stunM = true;

        // Shielding Defend blocks all incoming statuses (stun, conditions).
        bool pShielded = p.Base == "defend" && p.Has("shielding");
        bool mShielded = m.Base == "defend" && m.Has("shielding");
        if (pShielded) stunP = false;
        if (mShielded) stunM = false;

        // Provoking attack → Berzerk on target; Terrifying → Fear on target. Shielding
        // Defend blocks both. Enraging Recover (handled below) self-Berzerks.
        bool berzerkPNext = m.Base == "attack" && m.Has("provoking")  && !pShielded;
        bool berzerkMNext = p.Base == "attack" && p.Has("provoking")  && !mShielded;
        bool fearPNext    = m.Base == "attack" && m.Has("terrifying") && !pShielded;
        bool fearMNext    = p.Base == "attack" && p.Has("terrifying") && !mShielded;

        // Enraging Recover paired against an Attack → self-berzerks.
        if (p.Base == "recover" && p.Has("enraging") && m.Base == "attack") berzerkPNext = true;
        if (m.Base == "recover" && m.Has("enraging") && p.Base == "attack") berzerkMNext = true;

        // Condition mutators on Attack — chance-to-inflict on the target. Names track
        // super_rps.md § Attack Mutators; the global condition ids are lowercase tags.
        var condsP = new List<string>();
        var condsM = new List<string>();
        if (m.Base == "attack" && !pShielded) PossiblyInflict(m, condsP, rng);
        if (p.Base == "attack" && !mShielded) PossiblyInflict(p, condsM, rng);

        int playerDelta = -pTaken + pHeal;
        int monsterDelta = -mTaken + mHeal;

        return new SlotResult(
            playerDelta, monsterDelta,
            stunP, stunM,
            berzerkPNext, berzerkMNext,
            fearPNext, fearMNext,
            condsP, condsM);
    }

    static bool HasWary(Move m) => (m.Base == "recover" || m.Base == "read") && m.Has("wary");

    static int OutgoingDamage(Move attacker, Move defender)
    {
        if (attacker.Base != "attack") return 0;
        int dmg = BaseAttackDamage;
        if (attacker.Has("heavy")) dmg += 4;
        if (attacker.Has("weak")) dmg -= 2;
        if (attacker.Has("riposte") && defender.Base == "attack") dmg += 2;
        return Math.Max(0, dmg);
    }

    /// <summary>
    /// How much of <paramref name="incoming"/> actually lands.
    ///
    /// A Defend CAPS what gets through rather than shaving a flat amount, so a
    /// guard holds against a haymaker as well as it does against a jab. Everything
    /// else keeps the old subtractive prevention.
    ///
    /// The cap is deliberately not zero for a plain Defend: full immunity was tried
    /// during the pivot and made combat tedious — every exchange became a stall. The
    /// trickle is what keeps a guard from being an off-switch.
    /// </summary>
    static int Absorb(Move defender, int incoming, int prevention) =>
        DefendCap(defender) is { } cap
            ? Math.Min(incoming, cap)
            : Math.Max(0, incoming - prevention);

    /// <summary>
    /// Damage ceiling while guarding, or null for moves that are not a Defend.
    ///
    /// A Defend deals no damage back — a guard that hits for a full attack is a
    /// riposte, which is a gear rider, not a property of the base move. What it does
    /// instead is stagger the attacker (see Resolve).
    ///
    /// The leak is load-bearing: the trickle a guard concedes is the guaranteed
    /// loss-per-turn that stops a defensive player stalling forever. Clamping it
    /// tighter strengthens control and does nothing to aggro — measured, see
    /// plans/defend_stagger.md §4. Do not lower these numbers.
    /// </summary>
    static int? DefendCap(Move defender)
    {
        if (defender.Base != "defend") return null;
        if (defender.Has("perfect")) return 0;
        if (defender.Has("heavy")) return 1;
        return 2;
    }

    /// <summary>Damage of a plain, unmutated Attack.</summary>
    public const int BaseAttackDamage = 4;

    /// <summary>
    /// Subtractive mitigation for the moves that are not a Defend. Defends do not
    /// appear here — they are handled by <see cref="DefendCap"/>, which is the single
    /// source of truth for how much a guard lets through.
    /// </summary>
    static int Prevention(Move defender, Move attacker)
    {
        if (defender.Base == "attack" && defender.Has("riposte") && attacker.Base == "attack")
            return 2;
        if (defender.Base == "recover" && defender.Has("shielded"))
            return 2;
        return 0;
    }

    static int Heal(Move m, Move opp)
    {
        if (m.Base != "recover") return 0;
        // Attack cancels Recover — the recoverer gets nothing (and gets stunned via StunsTarget).
        if (opp.Base == "attack") return 0;
        int heal = 4;
        if (m.Has("heavy")) heal += 2;
        return heal;
    }

    /// <summary>Does <paramref name="actor"/> cause <paramref name="target"/> to be stunned next slot?</summary>
    static bool StunsTarget(Move actor, Move target, Random rng)
    {
        // Attack against Recover always stuns the recoverer (overrides resistance — see super_rps.md).
        if (actor.Base == "attack" && target.Base == "recover") return true;
        // Stunning Attack: chance to stun whoever they hit.
        if (actor.Base == "attack" && actor.Has("stunning") && rng.NextDouble() < ConditionProcChance) return true;
        // Stunning Defend vs Attack: chance to stun the attacker.
        if (actor.Base == "defend" && actor.Has("stunning") && target.Base == "attack" && rng.NextDouble() < ConditionProcChance) return true;
        return false;
    }

    /// <summary>
    /// Chance-to-inflict global conditions from Attack mutators. Single roll per
    /// mutator; super_rps doesn't enumerate per-mutator chances yet, so use the
    /// shared baseline. Tuning happens in playtest.
    /// </summary>
    static void PossiblyInflict(Move attack, List<string> sink, Random rng)
    {
        if (attack.Has("brutal")   && rng.NextDouble() < ConditionProcChance) sink.Add("injured");
        if (attack.Has("tainted")  && rng.NextDouble() < ConditionProcChance) sink.Add("lattice_sickness");
        if (attack.Has("glowing")  && rng.NextDouble() < ConditionProcChance) sink.Add("irradiated");
        if (attack.Has("venomous") && rng.NextDouble() < ConditionProcChance) sink.Add("poisoned");
    }
}
