namespace Dreamlands.Game;

/// <summary>
/// In-progress combat state, persisted on the player's Cosmos document. Pure data;
/// resolution logic lives in <c>Dreamlands.Combat</c>. Lookups against the live
/// <c>Dreamlands.Encounter.CombatEncounter</c> are by <see cref="EncounterId"/>.
/// </summary>
public sealed class CombatState
{
    public string EncounterId { get; set; } = "";

    public int MonsterHp { get; set; }
    public int MonsterMaxHp { get; set; }

    /// <summary>Cooldown remaining (in turns) per non-basic move id. Counts down to 0 = ready.</summary>
    public Dictionary<string, int> Cooldowns { get; set; } = new();

    /// <summary>Move the monster will execute on its next turn. Drives the intent preview.</summary>
    public string? NextMoveId { get; set; }

    /// <summary>Transient per-turn AC bump from a Defend move. Reset before each monster turn.</summary>
    public int MonsterAcBonusThisTurn { get; set; }

    /// <summary>
    /// Set by a dagger super-crit; consumed by the next monster turn (which is skipped).
    /// Heavy cooldown still ticks per design — blanking a basic costs the monster a swing,
    /// blanking a heavy resets the cooldown without dealing damage.
    /// </summary>
    public bool SkipNextMonsterTurn { get; set; }

    public int Round { get; set; }
    public bool PlayerActsFirst { get; set; }
    public SwordStance Stance { get; set; } = SwordStance.Balanced;

    public CombatPlayerProfile Profile { get; set; } = new();

    public bool PlayerWon { get; set; }
    public bool PlayerLost { get; set; }
    public bool PlayerFled { get; set; }
    public bool MonsterFled { get; set; }

    public bool Resolved => PlayerWon || PlayerLost || PlayerFled || MonsterFled;
}
