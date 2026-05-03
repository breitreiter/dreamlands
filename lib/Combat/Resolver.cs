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

        int pTaken = Math.Max(0, mOut - pPrev);
        int mTaken = Math.Max(0, pOut - mPrev);

        int pHeal = Heal(p, m);
        int mHeal = Heal(m, p);

        bool stunP = StunsTarget(m, p, rng);
        bool stunM = StunsTarget(p, m, rng);

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
        int dmg = 4;
        if (attacker.Has("heavy")) dmg += 4;
        if (attacker.Has("weak")) dmg -= 2;
        if (attacker.Has("riposte") && defender.Base == "attack") dmg += 2;
        return Math.Max(0, dmg);
    }

    static int Prevention(Move defender, Move attacker)
    {
        if (defender.Base == "defend")
        {
            if (defender.Has("perfect")) return 999;
            int p = 2;
            if (defender.Has("big")) p += 2;
            return p;
        }
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
        if (m.Has("big")) heal += 2;
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
