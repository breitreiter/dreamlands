using Dreamlands.Encounter;
using Dreamlands.Game;

namespace Dreamlands.Combat;

/// <summary>
/// Stateless step-based combat resolver. <see cref="Begin"/> opens combat (surprise
/// check, monster's first turn if it has the jump) and leaves the state waiting on a
/// player action. <see cref="Step"/> resolves one player action, then the matching
/// monster turn(s) needed to land back at "waiting for player action" or a terminal.
///
/// The same Begin/Step shape works for both the CLI test harness and the web UI:
/// the server persists <see cref="CombatState"/> on the player document and applies
/// one player action per HTTP request.
/// </summary>
public static class CombatRunner
{
    public const int FleeDc = 12;
    public const int SurpriseDc = 12;

    /// <summary>
    /// Initialize <paramref name="state"/> for a new combat against <paramref name="encounter"/>.
    /// Assumes <paramref name="state"/> is freshly constructed and <see cref="CombatState.Profile"/>
    /// is already populated.
    /// </summary>
    public static IReadOnlyList<CombatEvent> Begin(
        CombatEncounter encounter, PlayerState player, CombatState state, Random rng)
    {
        var events = new List<CombatEvent>();

        state.EncounterId = encounter.Id;
        state.MonsterHp = encounter.Stats.Hp;
        state.MonsterMaxHp = encounter.Stats.Hp;
        state.Cooldowns.Clear();
        foreach (var move in encounter.Moves)
            if (!move.IsBasic) state.Cooldowns[move.Id] = move.Timer;
        state.Round = 1;
        state.MonsterAcBonusThisTurn = 0;
        state.SkipNextMonsterTurn = false;
        state.Stance = SwordStance.Balanced;

        events.Add(new CombatEvent.Intro(encounter.Id, encounter.Title, encounter.Intro));

        var surprise = Resolver.RollSave(rng, state.Profile.Bushcraft, SurpriseDc);
        state.PlayerActsFirst = surprise.Success;
        events.Add(new CombatEvent.SurpriseChecked(surprise.Roll, state.Profile.Bushcraft, SurpriseDc, state.PlayerActsFirst));

        state.NextMoveId = ChooseNextMove(encounter, state).Id;

        events.Add(new CombatEvent.RoundStarted(state.Round, state.PlayerActsFirst));

        if (!state.PlayerActsFirst)
        {
            ResolveMonsterTurnOrSkip(encounter, player, state, rng, events);
            if (TryAddOutcome(encounter, state, events)) return events;
        }

        EmitIntentPreview(encounter, state, events);
        return events;
    }

    /// <summary>
    /// Resolve one player action. Free actions (e.g. SetStance) leave the state still
    /// waiting; turn-consuming actions also resolve the matching monster turn(s).
    /// </summary>
    public static IReadOnlyList<CombatEvent> Step(
        CombatEncounter encounter, PlayerState player, CombatState state,
        PlayerCombatAction action, Random rng)
    {
        if (state.Resolved)
            throw new InvalidOperationException("Combat is already resolved.");

        var events = new List<CombatEvent>();

        switch (action)
        {
            case PlayerCombatAction.SetStance set:
                var prev = state.Stance;
                state.Stance = set.Stance;
                events.Add(new CombatEvent.StanceChanged(prev, set.Stance));
                return events;

            case PlayerCombatAction.Attack:
                ResolvePlayerAttack(encounter, state, rng, events);
                break;

            case PlayerCombatAction.DaggerAttack dagger:
                ResolveDaggerAttack(state, dagger.Band, rng, events);
                break;

            case PlayerCombatAction.Flee:
                var save = Resolver.RollSave(rng, state.Profile.Cunning, FleeDc);
                events.Add(new CombatEvent.PlayerFleeAttempted(save));
                if (save.Success)
                {
                    state.PlayerFled = true;
                }
                else
                {
                    // Free monster basic-hit; does not tick cooldowns or advance the round.
                    ExecuteMonsterMove(encounter, player, state, encounter.BasicMove, rng, events, freeHit: true);
                }
                break;

            default:
                throw new InvalidOperationException($"Unknown player action: {action}");
        }

        if (TryAddOutcome(encounter, state, events)) return events;

        // Player turn is done. If player acts first, the monster turn closes out this round.
        if (state.PlayerActsFirst)
        {
            ResolveMonsterTurnOrSkip(encounter, player, state, rng, events);
            if (TryAddOutcome(encounter, state, events)) return events;
            state.Round++;
        }
        else
        {
            // Monster acts first each round; player just finished round N, so monster opens round N+1.
            state.Round++;
            ResolveMonsterTurnOrSkip(encounter, player, state, rng, events);
            if (TryAddOutcome(encounter, state, events)) return events;
        }

        events.Add(new CombatEvent.RoundStarted(state.Round, state.PlayerActsFirst));
        EmitIntentPreview(encounter, state, events);
        return events;
    }

    static void EmitIntentPreview(CombatEncounter encounter, CombatState state, List<CombatEvent> events)
    {
        var move = MoveById(encounter, state.NextMoveId)
            ?? throw new InvalidOperationException($"NextMoveId not set after Begin/Step for {encounter.Id}.");
        events.Add(new CombatEvent.IntentPreviewed(move.Id, move.IntentClass, move.IntentText));
    }

    static void ResolvePlayerAttack(CombatEncounter encounter, CombatState state, Random rng, List<CombatEvent> events)
    {
        var monsterAc = encounter.Stats.Ac + state.MonsterAcBonusThisTurn;
        var attack = Resolver.RollAttack(rng, StanceModifiers.PlayerAttackBonus(state), monsterAc);
        Resolver.DamageResult? damage = null;
        if (attack.Hit)
        {
            var dmg = new DiceRoll(state.Profile.DamageDieCount, state.Profile.DamageDieSize, StanceModifiers.PlayerDamageBonus(state));
            damage = Resolver.RollDamage(rng, dmg, attack.Crit);
            state.MonsterHp = Math.Max(0, state.MonsterHp - damage.Total);
        }
        events.Add(new CombatEvent.PlayerAttacked(attack, damage, state.MonsterHp, state.MonsterMaxHp));
        if (state.MonsterHp <= 0) state.PlayerWon = true;
    }

    static void ResolveDaggerAttack(CombatState state, TimingBand band, Random rng, List<CombatEvent> events)
    {
        // No d20 roll — band came from the client's timing minigame.
        // Stance is sword-specific; dagger ignores it (see project/design/dagger_reflex_minigame.md).
        Resolver.DamageResult? damage = null;
        bool crit = band is TimingBand.Crit or TimingBand.SuperCrit;
        bool superCrit = band == TimingBand.SuperCrit;

        if (band != TimingBand.Miss)
        {
            var dmg = new DiceRoll(state.Profile.DamageDieCount, state.Profile.DamageDieSize, state.Profile.DamageBonus);
            damage = Resolver.RollDamage(rng, dmg, crit);
            state.MonsterHp = Math.Max(0, state.MonsterHp - damage.Total);
        }

        if (superCrit) state.SkipNextMonsterTurn = true;

        events.Add(new CombatEvent.PlayerDaggerAttacked(band, damage, state.MonsterHp, state.MonsterMaxHp, superCrit));
        if (state.MonsterHp <= 0) state.PlayerWon = true;
    }

    static void ResolveMonsterTurnOrSkip(CombatEncounter encounter, PlayerState player, CombatState state, Random rng, List<CombatEvent> events)
    {
        if (!state.SkipNextMonsterTurn)
        {
            ResolveMonsterTurn(encounter, player, state, rng, events);
            return;
        }

        // Super-crit cancel: monster takes no action, but cooldowns still tick per design.
        state.SkipNextMonsterTurn = false;
        var move = MoveById(encounter, state.NextMoveId) ?? encounter.BasicMove;
        events.Add(new CombatEvent.MonsterTurnSkipped(move.Id, move.IntentClass));
        TickCooldowns(encounter, state, move);
        state.NextMoveId = ChooseNextMove(encounter, state).Id;
    }

    static void ResolveMonsterTurn(CombatEncounter encounter, PlayerState player, CombatState state, Random rng, List<CombatEvent> events)
    {
        // Defend bonus applies only to the turn it was used on.
        state.MonsterAcBonusThisTurn = 0;

        var move = MoveById(encounter, state.NextMoveId) ?? encounter.BasicMove;
        ExecuteMonsterMove(encounter, player, state, move, rng, events, freeHit: false);
        TickCooldowns(encounter, state, move);
        state.NextMoveId = ChooseNextMove(encounter, state).Id;
    }

    static void TickCooldowns(CombatEncounter encounter, CombatState state, MonsterMove justFired)
    {
        var ids = state.Cooldowns.Keys.ToList();
        foreach (var id in ids)
        {
            if (id == justFired.Id)
                state.Cooldowns[id] = encounter.Moves.First(m => m.Id == id).Timer;
            else
                state.Cooldowns[id] = Math.Max(0, state.Cooldowns[id] - 1);
        }
    }

    static MonsterMove ChooseNextMove(CombatEncounter encounter, CombatState state)
    {
        // Pick the special with cooldown 0; ties resolved by ordinal id for determinism.
        var ready = encounter.Moves
            .Where(m => !m.IsBasic && state.Cooldowns.TryGetValue(m.Id, out var c) && c <= 0)
            .OrderBy(m => m.Id, StringComparer.Ordinal)
            .ToList();
        return ready.FirstOrDefault() ?? encounter.BasicMove;
    }

    static void ExecuteMonsterMove(CombatEncounter encounter, PlayerState player, CombatState state,
        MonsterMove move, Random rng, List<CombatEvent> events, bool freeHit)
    {
        if (!freeHit)
            events.Add(new CombatEvent.MonsterMoved(move.Id, move.IntentClass, move.Narration));

        foreach (var mech in move.Mechanics)
        {
            if (state.Resolved && mech.Verb != "flee") return;
            switch (mech.Verb)
            {
                case "deal_damage":       DoDealDamage(encounter, player, state, move, mech, rng, events); break;
                case "pierce":            DoPierce(player, state, move, mech, rng, events); break;
                case "inflict_condition": DoInflictCondition(player, state, move, mech, rng, events); break;
                case "defend":            DoDefend(encounter, state, move, mech, events); break;
                case "flee":              DoMonsterFlee(state, move, events); break;
                default:
                    throw new InvalidOperationException($"unknown monster mechanic '{mech.Verb}'");
            }
        }
    }

    static void DoDealDamage(CombatEncounter encounter, PlayerState player, CombatState state,
        MonsterMove move, MoveMechanic mech, Random rng, List<CombatEvent> events)
    {
        var dmgDice = DiceParser.Parse(mech.Args);
        var attack = Resolver.RollAttack(rng, encounter.Stats.ToHit, StanceModifiers.PlayerEffectiveAc(state));
        Resolver.DamageResult? damage = null;
        DamageBreakdown? absorbed = null;
        if (attack.Hit)
        {
            damage = Resolver.RollDamage(rng, dmgDice, attack.Crit);
            absorbed = ApplyDamage(player, damage.Total);
            if (player.Health <= 0) state.PlayerLost = true;
        }
        events.Add(new CombatEvent.MonsterAttacked(move.Id, attack, damage, absorbed, player.Spirits, player.Health));
    }

    static void DoPierce(PlayerState player, CombatState state, MonsterMove move, MoveMechanic mech, Random rng, List<CombatEvent> events)
    {
        var parts = mech.Args.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 3 || parts[1] != "dc")
            throw new FormatException($"pierce needs '<dice> dc <n>', got '{mech.Args}'");
        var dmgDice = DiceParser.Parse(parts[0]);
        int dc = int.Parse(parts[2]);

        var save = Resolver.RollSave(rng, state.Profile.Cunning, dc);
        Resolver.DamageResult? damage = null;
        DamageBreakdown? absorbed = null;
        if (!save.Success)
        {
            damage = Resolver.RollDamage(rng, dmgDice, crit: false);
            absorbed = ApplyDamage(player, damage.Total);
            if (player.Health <= 0) state.PlayerLost = true;
        }
        events.Add(new CombatEvent.MonsterPierced(move.Id, save, damage, absorbed, player.Spirits, player.Health));
    }

    static void DoInflictCondition(PlayerState player, CombatState state, MonsterMove move, MoveMechanic mech, Random rng, List<CombatEvent> events)
    {
        var parts = mech.Args.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 3 || parts[1] != "dc")
            throw new FormatException($"inflict_condition needs '<id> dc <n> [<chance>]', got '{mech.Args}'");
        string id = parts[0];
        int dc = int.Parse(parts[2]);
        double chance = 1.0;
        if (parts.Length >= 4)
        {
            string c = parts[3];
            chance = c.EndsWith('%')
                ? double.Parse(c[..^1]) / 100.0
                : double.Parse(c);
        }

        bool procced = rng.NextDouble() < chance;
        Resolver.SaveOutcome? save = null;
        bool applied = false;
        if (procced)
        {
            save = Resolver.RollSave(rng, state.Profile.Cunning, dc);
            if (!save.Success)
            {
                player.ActiveConditions.Add(id);
                applied = true;
            }
        }
        events.Add(new CombatEvent.MonsterConditioned(move.Id, id, chance, procced, save, applied));
    }

    static void DoDefend(CombatEncounter encounter, CombatState state, MonsterMove move, MoveMechanic mech, List<CombatEvent> events)
    {
        var parts = mech.Args.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        int bonus = parts[0].StartsWith('+') ? int.Parse(parts[0][1..]) : int.Parse(parts[0]);
        state.MonsterAcBonusThisTurn = bonus;
        events.Add(new CombatEvent.MonsterDefended(move.Id, bonus, encounter.Stats.Ac + bonus));
    }

    static void DoMonsterFlee(CombatState state, MonsterMove move, List<CombatEvent> events)
    {
        state.MonsterFled = true;
        events.Add(new CombatEvent.MonsterFledEvt(move.Id));
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

    static MonsterMove? MoveById(CombatEncounter encounter, string? id) =>
        id is null ? null : encounter.Moves.FirstOrDefault(m => m.Id == id);

    static bool TryAddOutcome(CombatEncounter encounter, CombatState state, List<CombatEvent> events)
    {
        if (!state.Resolved) return false;
        var (text, mech) = (state.PlayerWon, state.PlayerLost) switch
        {
            (true, _) => (encounter.WinText, (IReadOnlyList<string>)encounter.WinMechanics),
            (_, true) => (encounter.LoseText, encounter.LoseMechanics),
            _         => ("", Array.Empty<string>()),
        };
        events.Add(new CombatEvent.Outcome(state.PlayerWon, state.PlayerLost, state.PlayerFled, state.MonsterFled, state.Round, text, mech));
        return true;
    }
}
