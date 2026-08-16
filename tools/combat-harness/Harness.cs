using Dreamlands.Combat;
using Dreamlands.Encounter;
using Dreamlands.Game;
using Dreamlands.Rules;

namespace Dreamlands.CombatHarness;

public enum Outcome { Won, Lost, Fled, MonsterFled, Stalled }

public sealed record FightResult(Outcome Outcome, int Turns, int SpiritsLeft, int HealthLeft);

/// <summary>
/// Everything a policy is allowed to see when choosing a commit. This is
/// deliberately the player's information set and no more: the tell, the plan
/// when a Read revealed it, and the moves currently legal. A policy that peeks
/// at <see cref="CombatState.MonsterCommit"/> is cheating and its numbers mean
/// nothing.
/// </summary>
public sealed record TurnContext(
    CombatPlayerProfile Profile,
    string Tell,
    IReadOnlyList<Move>? RevealedPlan,
    int Turn,
    int MonsterHp,
    int Spirits,
    int Health,
    Random Rng)
{
    /// <summary>Moves legal for the next slot, given what is already committed.</summary>
    public List<Move> Available(IReadOnlyList<Move> chosenSoFar, CombatState state) =>
        Legality.Available(Profile, state, chosenSoFar);
}

public interface IPolicy
{
    string Name { get; }
    Move[] Commit(TurnContext ctx, CombatState state);
}

/// <summary>
/// Player-side move legality. NOTE: this mirrors the web UI
/// (<c>ui/web/src/screens/Combat.tsx</c> <c>moveAvailability</c>) because there
/// is no shared implementation to reuse — the server does not validate player
/// commits at all, it just <c>Move.Parse</c>es the three strings, and gating is
/// purely a client-side affordance. If that ever moves into lib, delete this and
/// call it instead.
/// </summary>
public static class Legality
{
    public static List<Move> Available(
        CombatPlayerProfile profile, CombatState state, IReadOnlyList<Move> chosenSoFar)
    {
        var result = new List<Move>();
        foreach (var em in profile.MovePool)
        {
            var m = em.Move;
            // Power: once per turn, so not twice in the same three-slot commit.
            if (m.Has("power") && chosenSoFar.Any(c => c.Encoded == m.Encoded)) continue;
            // Slow: once every other turn.
            if (m.Has("slow")
                && state.PlayerLastUsedTurn.TryGetValue(m.Encoded, out var last)
                && state.Turn - last < 2) continue;
            result.Add(m);
        }
        return result;
    }
}

/// <summary>Uniform random over legal moves. The floor any real policy must beat.</summary>
public sealed class RandomPolicy : IPolicy
{
    public string Name => "random";

    public Move[] Commit(TurnContext ctx, CombatState state)
    {
        var slots = new Move[3];
        var chosen = new List<Move>(3);
        for (int i = 0; i < 3; i++)
        {
            var avail = ctx.Available(chosen, state);
            slots[i] = avail[ctx.Rng.Next(avail.Count)];
            chosen.Add(slots[i]);
        }
        return slots;
    }
}

public static class Runner
{
    public const int MaxTurns = 60;

    /// <summary>
    /// Runs one fight through the live engine. All mechanics come from
    /// <see cref="CombatRunner"/> — the harness only chooses moves and reads events.
    /// </summary>
    /// <param name="entrySpirits">
    /// Spirits the player walks in with. The 20-spirit start is a CAP, not a
    /// guarantee: travel drains it (fatigue alone is 1/night untrained, before
    /// biome hazards), and the T2/T3 fights sit a long way from any town. Full
    /// spirits is therefore the best case, not the typical one. MaxSpirits is
    /// left at 20, so Recover can still heal back toward the cap.
    /// </param>
    public static FightResult Run(
        CombatEncounter enc, ItemDef? weapon, ItemDef? armor,
        IPolicy policy, BalanceData balance, Random rng, int? entrySpirits = null)
    {
        var player = PlayerState.NewGame("harness", 0, balance);
        if (entrySpirits is { } sp) player.Spirits = Math.Clamp(sp, 0, player.MaxSpirits);
        var state = new CombatState { Profile = CombatPlayerProfile.From(weapon, armor) };

        var events = CombatRunner.Begin(enc, player, state, rng);
        var (tell, plan) = LastTurnStarted(events);

        int turns = 0;
        while (!state.Resolved && turns < MaxTurns)
        {
            var ctx = new TurnContext(
                state.Profile, tell, plan, state.Turn, state.MonsterHp,
                player.Spirits, player.Health, rng);

            var slots = policy.Commit(ctx, state);
            events = CombatRunner.Step(
                enc, player, state,
                new PlayerCombatAction.Commit(slots[0], slots[1], slots[2]), rng);
            (tell, plan) = LastTurnStarted(events);
            turns++;
        }

        var outcome = state.PlayerWon ? Outcome.Won
                    : state.PlayerLost ? Outcome.Lost
                    : state.PlayerFled ? Outcome.Fled
                    : state.MonsterFled ? Outcome.MonsterFled
                    : Outcome.Stalled;

        return new FightResult(outcome, turns, player.Spirits, player.Health);
    }

    static (string Tell, IReadOnlyList<Move>? Plan) LastTurnStarted(IReadOnlyList<CombatEvent> events)
    {
        var ts = events.OfType<CombatEvent.TurnStarted>().LastOrDefault();
        return (ts?.Tell ?? "", ts?.Plan);
    }
}
