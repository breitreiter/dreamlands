using Dreamlands.Combat;
using Dreamlands.Encounter;

namespace CombatSim;

public enum SwordStance { Aggressive, Balanced, Defensive }

/// <summary>
/// A weapon's per-turn rhythm. The sim loop calls <see cref="ChooseAndExecute"/> on the
/// player's turn and reads <see cref="EffectiveAc"/> when the monster swings. Per-turn
/// modifiers (axe Block AC bump, etc.) are cleared by <see cref="OnMonsterTurnComplete"/>.
/// </summary>
public abstract class WeaponPolicy
{
    public abstract string Name { get; }
    protected PcProfile Pc { get; private set; } = null!;
    public virtual int EffectiveAc => Pc.BaseAc;

    /// <summary>If true, the immediately-following monster attack is fully negated.
    /// Used by axe Block when configured for immunity-on-Block.</summary>
    public virtual bool ImmuneToIncomingDamage => false;

    public virtual void Reset(PcProfile pc) { Pc = pc; }

    /// <summary>Decide and execute a player turn. Returns damage dealt to the monster.</summary>
    public abstract int ChooseAndExecute(
        IntentClass nextIntent, int monsterAc, int monsterHp,
        int playerSpirits, int playerHealth, Random rng);

    public virtual void OnMonsterTurnComplete() { }
}

// ── Sword: stances (reference policy — already validated in conversation) ──

public sealed class SwordPolicy : WeaponPolicy
{
    public override string Name => "Sword (stances)";

    SwordStance _stance = SwordStance.Balanced;

    public override int EffectiveAc => Pc.BaseAc + StanceMod(_stance).ac;

    public override void Reset(PcProfile pc)
    {
        base.Reset(pc);
        _stance = SwordStance.Balanced;
    }

    public override int ChooseAndExecute(
        IntentClass nextIntent, int monsterAc, int monsterHp,
        int playerSpirits, int playerHealth, Random rng)
    {
        // Decision rule (intent-aware): Defensive vs incoming Heavy/Pierce, otherwise Aggressive
        // for free damage on telegraphs that don't threaten us. Balanced fallback.
        if (Pc.ReadsIntent)
        {
            // Optimal play: Defensive only when a Big-Attack-class swing is about to land.
            // Aggressive otherwise — basic attacks deal little, the +2/-2 trade is
            // strongly favorable on attack tempo.
            _stance = nextIntent switch
            {
                IntentClass.HeavyAttack => SwordStance.Defensive,
                IntentClass.Pierce      => SwordStance.Defensive,
                _                       => SwordStance.Aggressive,
            };
        }
        else
        {
            _stance = SwordStance.Balanced;  // tourist mode — no read
        }

        var (atkMod, _) = StanceMod(_stance);
        var attack = Resolver.RollAttack(rng, Pc.AttackBonus + atkMod, monsterAc);
        if (!attack.Hit) return 0;
        var dmg = Resolver.RollDamage(rng, new DiceRoll(1, Pc.DamageDie, Pc.DamageBonus + atkMod), attack.Crit);
        return dmg.Total;
    }

    static (int attack, int ac) StanceMod(SwordStance s) => s switch
    {
        SwordStance.Aggressive => (+2, -2),
        SwordStance.Balanced   => ( 0,  0),
        SwordStance.Defensive  => (-2, +2),
        _ => (0, 0)
    };
}

// ── Axe: momentum (under test) ──

public sealed record AxeParams(
    int MomentumDamagePerStack = 1,
    int MomentumAcPenaltyPerStack = 1,
    int MomentumCap = 5,
    int SpiritsThresholdToBlock = 10);

public sealed class AxePolicy(AxeParams p) : WeaponPolicy
{
    readonly AxeParams _p = p;
    int _momentum;
    bool _immuneThisTurn;

    public override string Name => $"Axe (mDmg={_p.MomentumDamagePerStack} mAcPen={_p.MomentumAcPenaltyPerStack} cap={_p.MomentumCap} immune-block)";

    public override int EffectiveAc =>
        Pc.BaseAc - _momentum * _p.MomentumAcPenaltyPerStack;

    public override bool ImmuneToIncomingDamage => _immuneThisTurn;

    public override void Reset(PcProfile pc)
    {
        base.Reset(pc);
        _momentum = 0;
        _immuneThisTurn = false;
    }

    public override int ChooseAndExecute(
        IntentClass nextIntent, int monsterAc, int monsterHp,
        int playerSpirits, int playerHealth, Random rng)
    {
        // Decision rule:
        //   1. If a Heavy/Pierce is incoming AND spirits are low AND we can't finish the
        //      monster this turn, Block.
        //   2. Otherwise Attack — eat the hit on a fat Spirits buffer, or gamble on the
        //      kill if expected damage covers monster HP.
        bool considerBlock = Pc.ReadsIntent && (nextIntent is IntentClass.HeavyAttack or IntentClass.Pierce);
        bool block = considerBlock
                     && playerSpirits <= _p.SpiritsThresholdToBlock
                     && ExpectedAttackDamage(monsterAc) <= monsterHp;

        if (block)
        {
            _immuneThisTurn = true;
            _momentum = 0;
            return 0;
        }

        var attack = Resolver.RollAttack(rng, Pc.AttackBonus, monsterAc);
        int dmg = 0;
        if (attack.Hit)
        {
            int momBonus = _momentum * _p.MomentumDamagePerStack;
            // Axe class damage: 2d4 — same average as 1d8 + ~0.5, tighter floor (min 2 vs 1).
            var roll = Resolver.RollDamage(rng, new DiceRoll(2, 4, Pc.DamageBonus + momBonus), attack.Crit);
            dmg = roll.Total;
        }
        // Momentum builds on the action regardless of hit/miss (per design: "missing does
        // not reset momentum, only choosing a non-attack action does").
        _momentum = Math.Min(_momentum + 1, _p.MomentumCap);
        return dmg;
    }

    /// <summary>
    /// EV of this turn's attack = hit_chance × avg_damage. Used to decide whether to
    /// gamble on a kill vs Block: if EV ≥ monster HP, swing for the fences.
    /// </summary>
    double ExpectedAttackDamage(int monsterAc)
    {
        int needed = Math.Max(2, monsterAc - Pc.AttackBonus);  // d20 ≥ needed; nat-20 always hits
        double hitChance = Math.Clamp((21.0 - needed) / 20.0, 0.05, 0.95);
        double avgDie = 5.0;  // 2d4
        int momBonus = _momentum * _p.MomentumDamagePerStack;
        double avgDamage = avgDie + Pc.DamageBonus + momBonus;
        return hitChance * avgDamage;
    }

    public override void OnMonsterTurnComplete()
    {
        // Immunity protects only the immediately-following monster attack.
        _immuneThisTurn = false;
    }
}
