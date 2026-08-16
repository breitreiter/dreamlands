using Dreamlands.Encounter;
using Dreamlands.Game;

namespace Dreamlands.CombatHarness;

/// <summary>
/// What the tell told us. Parsed from <see cref="Dreamlands.Combat.Tells.For"/>
/// output, which is the only read a player gets before committing.
///
/// Measured turn-1 distribution (see plans/rps_combat_harness.md §1): the tell
/// encodes how MANY attacks are coming, never which slot. <see cref="Wary"/> is
/// the most precise of them — it means exactly one attack, 100% of the time.
/// </summary>
public enum TellKind { Unknown, TelegraphedHeavy, Pressing, BackFoot, Winded, Wary }

public static class Tell
{
    /// <summary>
    /// Count of tells we could not classify. These strings are matched by
    /// substring against lib/Combat/Tells.cs; if that table is reworded, the
    /// policies silently go blind. Program checks this and shouts.
    /// </summary>
    public static int UnknownCount;

    public static TellKind Parse(string tell)
    {
        var kind =
            tell.Contains("preparing a heavy attack") ? TellKind.TelegraphedHeavy
          : tell.Contains("pressing the attack")      ? TellKind.Pressing
          : tell.Contains("on their back foot")       ? TellKind.BackFoot
          : tell.Contains("is winded")                ? TellKind.Winded
          : tell.Contains("awaits your move")         ? TellKind.Wary
          : TellKind.Unknown;
        if (kind == TellKind.Unknown) Interlocked.Increment(ref UnknownCount);
        return kind;
    }

    /// <summary>Expected number of attacking slots, from the tell alone.</summary>
    public static int ExpectedAttacks(TellKind k) => k switch
    {
        TellKind.TelegraphedHeavy => 2,
        TellKind.Pressing         => 2,
        TellKind.Wary             => 1,
        TellKind.BackFoot         => 1,
        TellKind.Winded           => 0,
        _                         => 1,
    };
}

/// <summary>Shared move lookup. All picks are filtered through <see cref="Legality"/>.</summary>
static class Pick
{
    public static Move? Best(List<Move> avail, params Func<Move, bool>[] preferences)
    {
        foreach (var want in preferences)
        {
            var hit = avail.Where(want).ToList();
            if (hit.Count > 0) return hit[0];
        }
        return null;
    }

    public static bool IsAttack(Move m)  => m.Base == "attack";
    public static bool IsDefend(Move m)  => m.Base == "defend";
    public static bool IsRecover(Move m) => m.Base == "recover";
    public static bool IsRead(Move m)    => m.Base == "read";

    /// <summary>
    /// Biggest attack available. Exhausting is deprioritised: it self-stuns
    /// regardless of outcome (Resolver.cs:78), costing the next slot, which is a
    /// worse trade than the +4 damage buys back. Taken only if nothing else exists.
    /// </summary>
    public static Move? Attack(List<Move> a) =>
        Best(a,
            m => IsAttack(m) && m.Has("heavy") && !m.Has("exhausting"),
            m => IsAttack(m) && m.Has("riposte") && !m.Has("exhausting"),
            m => IsAttack(m) && !m.Has("exhausting"),
            IsAttack);

    /// <summary>Strongest defend available: Perfect is total immunity, Heavy prevents 4.</summary>
    public static Move? Defend(List<Move> a) =>
        Best(a, m => IsDefend(m) && m.Has("perfect"), m => IsDefend(m) && m.Has("heavy"), IsDefend);

    /// <summary>Wary Read is strictly better — it reveals AND defends if attacked.</summary>
    public static Move? Read(List<Move> a) =>
        Best(a, m => IsRead(m) && m.Has("wary"), IsRead);

    public static Move? Recover(List<Move> a) => Best(a, IsRecover);
}

/// <summary>
/// Race. Attack every slot; the player's 24-point pool out-sustains 14 of 19
/// fights in a straight trade. Modest sophistication: give ground when the tell
/// warns of a telegraphed heavy, since that is the one hit big enough to break
/// the race math.
/// </summary>
public sealed class AggroPolicy : IPolicy
{
    public string Name => "aggro";

    public Move[] Commit(TurnContext ctx, CombatState state)
    {
        var kind = Tell.Parse(ctx.Tell);
        int defends = kind switch
        {
            TellKind.TelegraphedHeavy => 2,   // the hit that actually threatens us
            TellKind.Pressing         => 1,
            _                         => 0,
        };

        var slots = new Move[3];
        var chosen = new List<Move>(3);
        for (int i = 0; i < 3; i++)
        {
            var avail = ctx.Available(chosen, state);
            var want = i < defends
                ? Pick.Defend(avail) ?? Pick.Attack(avail)
                : Pick.Attack(avail) ?? Pick.Defend(avail);
            slots[i] = want ?? avail[0];
            chosen.Add(slots[i]);
        }
        return slots;
    }
}

/// <summary>
/// Control. Spend a slot on Read, then counter the revealed plan exactly:
/// defend the attacks, attack into everything else. Attacking into a Recover is
/// the payoff — it cancels the heal AND always stuns, a swing of roughly 12.
///
/// Its known hard counter is a monster that never recovers, which turns every
/// read into a mere defend (net -2 tempo). No such monster currently ships.
/// </summary>
public sealed class ControlPolicy : IPolicy
{
    public string Name => "control";

    public Move[] Commit(TurnContext ctx, CombatState state)
    {
        var plan = ctx.RevealedPlan;
        var slots = new Move[3];
        var chosen = new List<Move>(3);

        // Read where it is safe: a slot the plan says is not an attack. With a
        // Wary Read the risk is nil, so read regardless.
        int readSlot = -1;
        var probe = ctx.Available(chosen, state);
        var read = Pick.Read(probe);
        bool waryRead = read?.Has("wary") == true;
        if (read != null)
        {
            if (plan != null)
            {
                for (int i = 0; i < 3 && readSlot < 0; i++)
                    if (plan[i].Base != "attack") readSlot = i;
                if (readSlot < 0 && waryRead) readSlot = 2;
            }
            else if (waryRead || Tell.ExpectedAttacks(Tell.Parse(ctx.Tell)) <= 1)
            {
                readSlot = 2;
            }
        }

        for (int i = 0; i < 3; i++)
        {
            var avail = ctx.Available(chosen, state);
            Move? want;
            if (i == readSlot) want = Pick.Read(avail);
            else if (plan != null)
                want = plan[i].Base == "attack" ? Pick.Defend(avail) : Pick.Attack(avail);
            else
                want = Tell.ExpectedAttacks(Tell.Parse(ctx.Tell)) >= 2
                     ? Pick.Defend(avail) : Pick.Attack(avail);

            slots[i] = want ?? Pick.Attack(avail) ?? avail[0];
            chosen.Add(slots[i]);
        }
        return slots;
    }
}

/// <summary>
/// Turtle. Defend by default and let the monster break itself on the guard;
/// modest sophistication: attack through recover and defend hints, since a
/// monster that is winded or on its back foot is not swinging, and defending
/// against nothing accomplishes nothing.
/// </summary>
public sealed class TurtlePolicy : IPolicy
{
    public string Name => "turtle";

    public Move[] Commit(TurnContext ctx, CombatState state)
    {
        var kind = Tell.Parse(ctx.Tell);
        int attacks = kind switch
        {
            TellKind.Winded   => 3,   // recovering: punish, cancels the heal and stuns
            TellKind.BackFoot => 2,   // defending: chip in, nothing incoming
            TellKind.Wary     => 1,
            _                 => 0,
        };

        var slots = new Move[3];
        var chosen = new List<Move>(3);
        for (int i = 0; i < 3; i++)
        {
            var avail = ctx.Available(chosen, state);
            var want = i < attacks
                ? Pick.Attack(avail) ?? Pick.Defend(avail)
                : Pick.Defend(avail) ?? Pick.Recover(avail) ?? Pick.Attack(avail);
            slots[i] = want ?? avail[0];
            chosen.Add(slots[i]);
        }
        return slots;
    }
}
