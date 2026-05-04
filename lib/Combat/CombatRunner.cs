using Dreamlands.Encounter;
using Dreamlands.Game;

namespace Dreamlands.Combat;

/// <summary>
/// Stateless RPS combat resolver. <see cref="Begin"/> opens combat — emits the intro,
/// rolls the AI's first three-slot commitment, and lands the state waiting on a
/// player commit. <see cref="Step"/> consumes one player action (Commit or Flee),
/// resolves all three slots in order, applies forward-rider stuns, advances the turn
/// counter, and rolls the next AI commitment.
///
/// The same Begin/Step shape works for both the CLI test harness and the web UI:
/// the server persists <see cref="CombatState"/> on the player document and applies
/// one Step per HTTP request.
/// </summary>
public static class CombatRunner
{
    public static IReadOnlyList<CombatEvent> Begin(
        CombatEncounter encounter, PlayerState player, CombatState state, Random rng)
    {
        var events = new List<CombatEvent>();

        state.EncounterId = encounter.Id;
        state.MonsterHp = encounter.Stats.Hp;
        state.MonsterMaxHp = encounter.Stats.Hp;
        state.Turn = 1;
        state.PlayerCarryStun = new bool[3];
        state.MonsterCarryStun = new bool[3];
        state.PlayerLastUsedTurn.Clear();
        state.MonsterLastUsedTurn.Clear();
        state.RevealPlanNextTurn = false;
        state.PlayerBerzerkNextTurn = false;
        state.PlayerFearNextTurn = false;
        state.MonsterBerzerkNextTurn = false;
        state.MonsterFearNextTurn = false;
        state.PlayerWon = state.PlayerLost = state.PlayerFled = state.MonsterFled = false;

        events.Add(new CombatEvent.Intro(encounter.Id, encounter.Title, encounter.Intro));

        // Seed the AI's commitment for turn 1, then emit the tell. Read isn't active
        // turn 1 by definition, so no plan reveal here.
        ChooseAiCommit(encounter, state, rng);
        var tell = Tells.For(state.MonsterCommit, encounter.Title);
        events.Add(new CombatEvent.TurnStarted(state.Turn, tell, Plan: null));

        return events;
    }

    public static IReadOnlyList<CombatEvent> Step(
        CombatEncounter encounter, PlayerState player, CombatState state,
        PlayerCombatAction action, Random rng)
    {
        if (state.Resolved)
            throw new InvalidOperationException("Combat is already resolved.");

        var events = new List<CombatEvent>();

        // Resolve the player's intent into three slots. Flee = all-Skipped.
        Move[] playerSlots;
        bool fleeing = false;
        switch (action)
        {
            case PlayerCombatAction.Commit c:
                playerSlots = c.AsArray();
                break;
            case PlayerCombatAction.Flee:
                fleeing = true;
                events.Add(new CombatEvent.PlayerFleeAttempted(true));
                playerSlots = new[] { Move.Skipped(), Move.Skipped(), Move.Skipped() };
                break;
            default:
                throw new InvalidOperationException($"Unknown player action: {action}");
        }

        // Apply carry-stuns from last turn (these convert specific slots to Skipped
        // before resolution). Monster's commit was already chosen and stored; mutate
        // a copy so we keep the original around for narration playback.
        for (int i = 0; i < 3; i++)
        {
            if (state.PlayerCarryStun[i]) playerSlots[i] = Move.Skipped();
        }
        var monsterSlots = state.MonsterCommit.ToArray();
        var monsterNarration = state.MonsterCommitNarration.ToArray();
        for (int i = 0; i < 3; i++)
        {
            if (state.MonsterCarryStun[i])
            {
                monsterSlots[i] = Move.Skipped();
                monsterNarration[i] = "";
            }
        }

        // Track this turn's usage for Power/Slow next-turn cooldowns.
        for (int i = 0; i < 3; i++)
        {
            var p = playerSlots[i];
            if (p.Base != "skipped") state.PlayerLastUsedTurn[p.Encoded] = state.Turn;
            var m = monsterSlots[i];
            if (m.Base != "skipped") state.MonsterLastUsedTurn[m.Encoded] = state.Turn;
        }

        // Resolve slots in order.
        var nextPlayerStun = new bool[3];
        var nextMonsterStun = new bool[3];
        bool playerBerzerkNext = false, playerFearNext = false;
        bool monsterBerzerkNext = false, monsterFearNext = false;

        for (int i = 0; i < 3; i++)
        {
            var p = playerSlots[i];
            var m = monsterSlots[i];
            var r = Resolver.Resolve(p, m, rng);

            // Apply HP deltas.
            DamageBreakdown? absorbed = null;
            if (r.PlayerDelta < 0) absorbed = ApplyDamage(player, -r.PlayerDelta);
            else if (r.PlayerDelta > 0) ApplyHeal(player, r.PlayerDelta);

            int monsterDeltaApplied = ClampMonsterDelta(state, r.MonsterDelta);
            state.MonsterHp += monsterDeltaApplied;

            // Forward-rider stuns target the *next* slot of the named side. If we're
            // already at slot 3, the stun bleeds into slot 1 of next turn.
            if (r.StunPlayerNext) ApplyForwardStun(i, playerSlots, nextPlayerStun);
            if (r.StunMonsterNext) ApplyForwardStun(i, monsterSlots, nextMonsterStun);

            // Berzerk/Fear riders set next-turn pool restrictions, not stuns.
            playerBerzerkNext  |= r.BerzerkPlayerNext;
            playerFearNext     |= r.FearPlayerNext;
            monsterBerzerkNext |= r.BerzerkMonsterNext;
            monsterFearNext    |= r.FearMonsterNext;

            // Inflict global conditions on the player. Monster conditions are
            // recorded on the event but not stored — there's no monster state pool.
            foreach (var cond in r.ConditionsAppliedToPlayer)
                player.ActiveConditions.Add(cond);

            events.Add(new CombatEvent.SlotResolved(
                Slot: i + 1,
                PlayerMove: p,
                MonsterMove: m,
                MonsterNarration: monsterNarration[i] ?? "",
                PlayerDelta: r.PlayerDelta,
                MonsterDelta: monsterDeltaApplied,
                PlayerDamageAbsorbed: absorbed,
                PlayerSpiritsAfter: player.Spirits,
                PlayerHealthAfter: player.Health,
                MonsterHpAfter: state.MonsterHp,
                StunPlayerNext: r.StunPlayerNext,
                StunMonsterNext: r.StunMonsterNext,
                ConditionsAppliedToPlayer: r.ConditionsAppliedToPlayer));

            // Terminal checks — break the slot loop on any side dropping.
            if (player.Health <= 0) { state.PlayerLost = true; break; }
            if (state.MonsterHp <= 0) { state.PlayerWon = true; break; }
        }

        // End-of-turn flag updates. Read fires on commit; consumed at start of next turn.
        bool revealPlanNext = !fleeing && playerSlots.Any(p => p.Base == "read");

        if (fleeing && !state.Resolved)
            state.PlayerFled = true;

        if (TryAddOutcome(encounter, state, events))
            return events;

        // Carry over to next turn.
        state.Turn++;
        state.PlayerCarryStun = nextPlayerStun;
        state.MonsterCarryStun = nextMonsterStun;
        state.RevealPlanNextTurn = revealPlanNext;
        state.PlayerBerzerkNextTurn = playerBerzerkNext;
        state.PlayerFearNextTurn = playerFearNext;
        state.MonsterBerzerkNextTurn = monsterBerzerkNext;
        state.MonsterFearNextTurn = monsterFearNext;

        // Roll the AI's next commitment, then emit the new tell + optional plan.
        ChooseAiCommit(encounter, state, rng);
        var tell = Tells.For(state.MonsterCommit, encounter.Title);
        IReadOnlyList<Move>? plan = state.RevealPlanNextTurn
            ? state.MonsterCommit.ToList()
            : null;
        events.Add(new CombatEvent.TurnStarted(state.Turn, tell, plan));

        // Plan, once shown, is consumed.
        if (state.RevealPlanNextTurn) state.RevealPlanNextTurn = false;

        return events;
    }

    /// <summary>
    /// Choose three moves from the encounter pool for the upcoming turn. Honours
    /// MonsterCarryStun (slots locked to Skipped), Power/Slow cooldowns from
    /// MonsterLastUsedTurn, and Berzerk/Fear pool restrictions for the upcoming turn.
    /// Mutates state in place: writes <see cref="CombatState.MonsterCommit"/> and
    /// <see cref="CombatState.MonsterCommitNarration"/>.
    /// </summary>
    static void ChooseAiCommit(CombatEncounter encounter, CombatState state, Random rng)
    {
        state.MonsterCommit = new List<Move>(3);
        state.MonsterCommitNarration = new List<string>(3);

        // Used-this-turn tracker for in-turn Power cooldowns (a move can't appear twice
        // in the same three-slot commitment if it's Power).
        var usedThisCommit = new HashSet<string>();

        for (int slot = 0; slot < 3; slot++)
        {
            if (state.MonsterCarryStun[slot])
            {
                state.MonsterCommit.Add(Move.Skipped());
                state.MonsterCommitNarration.Add("");
                continue;
            }

            var available = AvailableForAi(encounter, state, usedThisCommit);
            if (available.Count == 0)
            {
                // Fallback: ignore cooldown filters if every move is locked out, so
                // we never emit an empty commitment.
                available = encounter.Moves.Where(d => !IsAttackBaseLocked(d, state)
                                                       && !IsNonAttackBaseLocked(d, state)).ToList();
                if (available.Count == 0)
                    available = encounter.Moves.ToList();
            }

            var pick = available[rng.Next(available.Count)];
            state.MonsterCommit.Add(pick.Action);
            state.MonsterCommitNarration.Add(pick.PickNarration(rng));
            usedThisCommit.Add(pick.Action.Encoded);
        }
    }

    static List<MonsterMoveDef> AvailableForAi(CombatEncounter encounter, CombatState state, HashSet<string> usedThisCommit)
    {
        var result = new List<MonsterMoveDef>();
        foreach (var def in encounter.Moves)
        {
            var move = def.Action;

            // Berzerk: must pick Attack-family if any are available.
            if (state.MonsterBerzerkNextTurn && move.Base != "attack") continue;
            // Fear: must pick Defend or Recover if available.
            if (state.MonsterFearNextTurn && move.Base != "defend" && move.Base != "recover") continue;

            // Once-per-turn cooldown.
            if (move.Has("power") && usedThisCommit.Contains(move.Encoded)) continue;

            // Once-every-other-turn cooldown.
            if (move.Has("slow")
                && state.MonsterLastUsedTurn.TryGetValue(move.Encoded, out int last)
                && state.Turn - last < 2) continue;

            result.Add(def);
        }

        // If Berzerk filtered everything out (no Attack moves in pool), allow any
        // legal move so the monster doesn't lock itself out of the turn.
        if (state.MonsterBerzerkNextTurn && result.Count == 0)
            return encounter.Moves.ToList();
        if (state.MonsterFearNextTurn && result.Count == 0)
            return encounter.Moves.ToList();

        return result;
    }

    // Helpers used only by the rare empty-pool fallback. Conservative: in practice
    // cooldowns are infrequent enough that AvailableForAi returns non-empty.
    static bool IsAttackBaseLocked(MonsterMoveDef def, CombatState state) =>
        state.MonsterFearNextTurn && def.Action.Base == "attack";
    static bool IsNonAttackBaseLocked(MonsterMoveDef def, CombatState state) =>
        state.MonsterBerzerkNextTurn && def.Action.Base != "attack";

    static int ClampMonsterDelta(CombatState state, int delta)
    {
        int newHp = Math.Max(0, Math.Min(state.MonsterMaxHp, state.MonsterHp + delta));
        return newHp - state.MonsterHp;
    }

    static DamageBreakdown ApplyDamage(PlayerState player, int amount)
    {
        int spiritsBefore = player.Spirits;
        int healthBefore = player.Health;
        int onSpirits = Math.Min(player.Spirits, amount);
        player.Spirits -= onSpirits;
        int overflow = amount - onSpirits;
        int onHealth = Math.Min(player.Health, overflow);
        player.Health -= onHealth;
        return new DamageBreakdown(amount, onSpirits, onHealth, spiritsBefore, healthBefore);
    }

    /// <summary>
    /// Recover heals Spirits up to <see cref="PlayerState.MaxSpirits"/>. Overflow does
    /// not refill Health. Per the project_spirits_mechanics decision: combat keeps the
    /// Spirits-then-Health buffer; heals replenish the cheap pool, not the expensive one.
    /// </summary>
    static void ApplyHeal(PlayerState player, int amount)
    {
        player.Spirits = Math.Min(player.MaxSpirits, player.Spirits + amount);
    }

    static void ApplyForwardStun(int slotJustResolved, Move[] thisTurn, bool[] nextTurnCarry)
    {
        int next = slotJustResolved + 1;
        if (next < 3) thisTurn[next] = Move.Skipped();
        else nextTurnCarry[0] = true;
    }

    static bool TryAddOutcome(CombatEncounter encounter, CombatState state, List<CombatEvent> events)
    {
        if (!state.Resolved) return false;
        var (text, mech) = (state.PlayerWon, state.PlayerLost, state.PlayerFled, state.MonsterFled) switch
        {
            (true, _, _, _)  => (encounter.WinText, (IReadOnlyList<string>)encounter.WinMechanics),
            (_, true, _, _)  => (encounter.LoseText, encounter.LoseMechanics),
            _                => ("", Array.Empty<string>()),
        };
        events.Add(new CombatEvent.Outcome(
            state.PlayerWon, state.PlayerLost, state.PlayerFled, state.MonsterFled,
            state.Turn, text, mech));
        return true;
    }
}
