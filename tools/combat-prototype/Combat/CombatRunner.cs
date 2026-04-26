using CombatPrototype.Cmb;

namespace CombatPrototype.Combat;

public interface ICombatObserver
{
    void OnIntro(CombatState s);
    void OnSurpriseCheck(int roll, int bonus, int dc, bool playerFirst);
    void OnRoundStart(CombatState s, bool playerFirst);
    void OnIntentPreview(CombatState s, MonsterMove next);
    void OnPlayerStanceChanged(SwordStance from, SwordStance to);
    void OnPlayerAttack(CombatState s, Resolver.AttackOutcome outcome, Resolver.DamageResult? damage);
    void OnPlayerFleeAttempt(Resolver.SaveOutcome outcome);
    void OnMonsterMove(CombatState s, MonsterMove move);
    void OnMonsterAttack(CombatState s, MonsterMove move, Resolver.AttackOutcome outcome, Resolver.DamageResult? damage, DamageBreakdown? absorbed);
    void OnMonsterPierce(CombatState s, MonsterMove move, Resolver.SaveOutcome save, Resolver.DamageResult? damage, DamageBreakdown? absorbed);
    void OnMonsterCondition(CombatState s, MonsterMove move, string conditionId, double chance, bool procced, Resolver.SaveOutcome? save);
    void OnMonsterDefend(CombatState s, MonsterMove move, int acBonus);
    void OnMonsterFlee(CombatState s, MonsterMove move);
    void OnOutcome(CombatState s);
}

public sealed class CombatRunner
{
    private const int FleeDc = 12;
    private const int SurpriseDc = 12;

    private readonly Random _rng;
    private readonly ICombatObserver _obs;

    public CombatRunner(Random rng, ICombatObserver observer)
    {
        _rng = rng;
        _obs = observer;
    }

    public CombatState Run(CmbEncounter encounter, PlayerState player, IPlayerController controller)
    {
        var s = new CombatState(player, encounter);
        _obs.OnIntro(s);

        // Surprise: pass = player goes first.
        var surprise = Resolver.RollSave(_rng, player.Bushcraft, SurpriseDc);
        bool playerFirst = surprise.Success;
        _obs.OnSurpriseCheck(surprise.Roll, player.Bushcraft, SurpriseDc, playerFirst);

        // Pre-fight: pick what monster does first (basic, unless a timer's already at 0)
        s.NextMove = ChooseNextMove(s);

        while (!s.Resolved)
        {
            s.Round++;
            _obs.OnRoundStart(s, playerFirst);

            if (playerFirst)
            {
                ResolvePlayerTurn(s, controller);
                if (!s.Resolved) ResolveMonsterTurn(s);
            }
            else
            {
                ResolveMonsterTurn(s);
                if (!s.Resolved) ResolvePlayerTurn(s, controller);
            }
        }

        _obs.OnOutcome(s);
        return s;
    }

    private void ResolvePlayerTurn(CombatState s, IPlayerController controller)
    {
        _obs.OnIntentPreview(s, s.NextMove);
        while (true)
        {
            var action = controller.GetAction(s);
            switch (action)
            {
                case PlayerAction.SetStance set:
                    var prev = s.Player.Stance;
                    s.Player.Stance = set.Stance;
                    _obs.OnPlayerStanceChanged(prev, set.Stance);
                    continue;  // free action — re-prompt
                case PlayerAction.Attack:
                    ResolvePlayerAttack(s);
                    return;
                case PlayerAction.Flee:
                    var save = Resolver.RollSave(_rng, s.Player.Cunning, FleeDc);
                    _obs.OnPlayerFleeAttempt(save);
                    if (save.Success)
                    {
                        s.PlayerFled = true;
                    }
                    else
                    {
                        // Free hit from monster on failed flee — use basic move once.
                        var basic = s.Encounter.BasicMove;
                        ExecuteMonsterMove(s, basic, freeHit: true);
                    }
                    return;
                case PlayerAction.Abort:
                    s.Aborted = true;
                    return;
            }
        }
    }

    private void ResolvePlayerAttack(CombatState s)
    {
        var outcome = Resolver.RollAttack(_rng, s.Player.AttackToHitBonus, s.Encounter.Stats.Ac);
        Resolver.DamageResult? damage = null;
        if (outcome.Hit)
        {
            damage = Resolver.RollDamage(_rng, s.Player.DamageRoll, outcome.Crit);
            s.MonsterHp = Math.Max(0, s.MonsterHp - damage.Total);
        }
        _obs.OnPlayerAttack(s, outcome, damage);

        if (s.MonsterHp <= 0)
            s.PlayerWins = true;
    }

    private void ResolveMonsterTurn(CombatState s)
    {
        // Reset per-turn modifiers from the previous monster turn (e.g. defend bonus).
        s.MonsterAcBonusThisTurn = 0;

        var move = s.NextMove;
        ExecuteMonsterMove(s, move, freeHit: false);

        // Tick cooldowns: if we just fired this move, reset its cooldown to full;
        // tick everything else down by 1.
        var ids = s.Cooldowns.Keys.ToList();
        foreach (var id in ids)
        {
            if (id == move.Id)
                s.Cooldowns[id] = s.Encounter.Moves.First(m => m.Id == id).Timer;
            else
                s.Cooldowns[id] = Math.Max(0, s.Cooldowns[id] - 1);
        }

        s.NextMove = ChooseNextMove(s);
    }

    private MonsterMove ChooseNextMove(CombatState s)
    {
        // Pick the special whose cooldown is at 0 (lowest-id deterministic if multiple).
        var ready = s.Encounter.Moves
            .Where(m => !m.IsBasic && s.Cooldowns.TryGetValue(m.Id, out var c) && c <= 0)
            .OrderBy(m => m.Id, StringComparer.Ordinal)
            .ToList();
        return ready.FirstOrDefault() ?? s.Encounter.BasicMove;
    }

    private void ExecuteMonsterMove(CombatState s, MonsterMove move, bool freeHit)
    {
        if (!freeHit) _obs.OnMonsterMove(s, move);

        foreach (var mech in move.Mechanics)
        {
            if (s.Resolved && mech.Verb != "flee") return;
            switch (mech.Verb)
            {
                case "deal_damage":     DoDealDamage(s, move, mech); break;
                case "pierce":          DoPierce(s, move, mech); break;
                case "inflict_condition": DoInflictCondition(s, move, mech); break;
                case "defend":          DoDefend(s, move, mech); break;
                case "bleed":           DoBleed(s, move, mech); break;
                case "flee":            DoMonsterFlee(s, move); break;
                default:
                    throw new InvalidOperationException($"unknown monster mechanic '{mech.Verb}'");
            }
        }
    }

    private void DoDealDamage(CombatState s, MonsterMove move, MechanicLine mech)
    {
        // Standard attack roll vs player AC, deal weapon damage on hit.
        var dmgDice = DiceParser.Parse(mech.Args);
        var outcome = Resolver.RollAttack(_rng, s.Encounter.Stats.ToHit, s.Player.EffectiveAc);
        Resolver.DamageResult? damage = null;
        DamageBreakdown? absorbed = null;
        if (outcome.Hit)
        {
            damage = Resolver.RollDamage(_rng, dmgDice, outcome.Crit);
            absorbed = s.Player.TakeDamage(damage.Total);
            if (s.Player.IsDead) s.PlayerLoses = true;
        }
        _obs.OnMonsterAttack(s, move, outcome, damage, absorbed);
    }

    private void DoPierce(CombatState s, MonsterMove move, MechanicLine mech)
    {
        // Format: "<dice> dc <n>" — Cunning save vs DC, full damage on fail. Armor doesn't apply.
        var parts = mech.Args.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 3 || parts[1] != "dc")
            throw new FormatException($"pierce needs '<dice> dc <n>', got '{mech.Args}'");
        var dmgDice = DiceParser.Parse(parts[0]);
        int dc = int.Parse(parts[2]);

        var save = Resolver.RollSave(_rng, s.Player.Cunning, dc);
        Resolver.DamageResult? damage = null;
        DamageBreakdown? absorbed = null;
        if (!save.Success)
        {
            damage = Resolver.RollDamage(_rng, dmgDice, crit: false);
            absorbed = s.Player.TakeDamage(damage.Total);
            if (s.Player.IsDead) s.PlayerLoses = true;
        }
        _obs.OnMonsterPierce(s, move, save, damage, absorbed);
    }

    private void DoInflictCondition(CombatState s, MonsterMove move, MechanicLine mech)
    {
        // Format: "<id> dc <n> [<chance>]" — proc roll, then Cunning save vs DC.
        var parts = mech.Args.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 3 || parts[1] != "dc")
            throw new FormatException($"inflict_condition needs '<id> dc <n> [<chance>]', got '{mech.Args}'");
        string id = parts[0];
        int dc = int.Parse(parts[2]);
        double chance = 1.0;
        if (parts.Length >= 4)
        {
            string c = parts[3];
            if (c.EndsWith('%')) chance = double.Parse(c[..^1]) / 100.0;
            else chance = double.Parse(c);
        }

        bool procced = _rng.NextDouble() < chance;
        Resolver.SaveOutcome? save = null;
        if (procced)
        {
            save = Resolver.RollSave(_rng, s.Player.Cunning, dc);
            // Condition application is stubbed in prototype — log only.
        }
        _obs.OnMonsterCondition(s, move, id, chance, procced, save);
    }

    private void DoDefend(CombatState s, MonsterMove move, MechanicLine mech)
    {
        // Format: "+<n> ac"
        var parts = mech.Args.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        int bonus = parts[0].StartsWith('+') ? int.Parse(parts[0][1..]) : int.Parse(parts[0]);
        s.MonsterAcBonusThisTurn = bonus;
        _obs.OnMonsterDefend(s, move, bonus);
    }

    private void DoBleed(CombatState s, MonsterMove move, MechanicLine mech)
    {
        // Format: "<chance> <dice> <duration>" — stub for now (no condition system in prototype).
        // Treat as best-effort log.
        _obs.OnMonsterCondition(s, move, "bleeding", chance: 1.0, procced: true, save: null);
    }

    private void DoMonsterFlee(CombatState s, MonsterMove move)
    {
        s.MonsterFled = true;
        _obs.OnMonsterFlee(s, move);
    }
}
