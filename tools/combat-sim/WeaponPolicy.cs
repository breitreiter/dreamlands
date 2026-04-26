using Dreamlands.Encounter;

namespace CombatSim;

/// <summary>
/// Local enum, intentionally duplicated from <c>Dreamlands.Game.SwordStance</c>.
/// The sim is decoupled from <c>lib/Game</c> so the harness can iterate on weapon
/// shapes without touching production state types — don't "fix" the duplication.
/// </summary>
public enum SwordStance { Aggressive, Balanced, Defensive }

/// <summary>Per-turn context handed to a policy when it's time to act.</summary>
public sealed record PolicyTurn(
    IntentClass NextMonsterIntent,
    int MonsterAc,
    int MonsterHp,
    int PlayerSpirits,
    int PlayerHealth);

/// <summary>
/// A weapon's per-turn rhythm. The sim calls <see cref="ChooseAndExecute"/> on the
/// player's turn, optionally <see cref="TickOngoingMonsterDamage"/> at the start of
/// each monster turn (for DOTs like dagger bleed), and reads <see cref="EffectiveAc"/>
/// + <see cref="ImmuneToIncomingDamage"/> when the monster swings.
/// </summary>
public abstract class WeaponPolicy
{
    public abstract string Name { get; }

    protected PcProfile Pc { get; private set; } = null!;

    public virtual int EffectiveAc => Pc.BaseAc;

    /// <summary>If true, the immediately-following monster attack is fully negated.</summary>
    public virtual bool ImmuneToIncomingDamage => false;

    public virtual void Reset(PcProfile pc) { Pc = pc; }

    /// <summary>Resolve the player's turn. Returns damage dealt to the monster.</summary>
    public abstract int ChooseAndExecute(PolicyTurn turn, Random rng);

    /// <summary>
    /// Damage from any DOTs on the monster, applied at the start of each monster turn
    /// (before the monster swings). Bypasses AC. Default: no DOTs.
    /// </summary>
    public virtual int TickOngoingMonsterDamage(Random rng) => 0;

    public virtual void OnMonsterTurnComplete() { }

    /// <summary>EV of a single attack swing — hit chance × average damage. For
    /// finishing-swing decisions like axe's "swing if EV ≥ monster HP".</summary>
    protected double ExpectedAttackDamage(int monsterAc, int attackBonus, DiceRoll dice)
    {
        int needed = Math.Max(2, monsterAc - attackBonus);  // d20 ≥ needed; nat-20 always hits
        double hitChance = Math.Clamp((21.0 - needed) / 20.0, 0.05, 0.95);
        double avgDie = dice.Count * (dice.Sides + 1) / 2.0;
        return hitChance * (avgDie + dice.Modifier);
    }
}
