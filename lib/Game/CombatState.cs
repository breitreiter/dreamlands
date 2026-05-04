using Dreamlands.Encounter;

namespace Dreamlands.Game;

/// <summary>
/// In-progress combat state, persisted on the player's Cosmos document. Pure data;
/// resolution logic lives in <see cref="Dreamlands.Combat.CombatRunner"/>.
///
/// Persistence model: at end-of-Step (or Begin) the state holds the AI's three-slot
/// commitment for the *next* turn the player will play against, plus all carry-over
/// state (cooldowns, carry-stuns, pending Berzerk/Fear, reveal-plan flag). The next
/// HTTP request reads the same state and applies the player's commit against it.
/// </summary>
public sealed class CombatState
{
    public string EncounterId { get; set; } = "";

    public int MonsterHp { get; set; }
    public int MonsterMaxHp { get; set; }

    /// <summary>1-indexed turn counter. Turn 1 begins after Begin emits the first Tell.</summary>
    public int Turn { get; set; } = 1;

    /// <summary>
    /// AI's three-slot commitment for the upcoming turn. Set by Begin (turn 1) and at
    /// the end of every Step. Length is always 3; Skipped slots are explicit.
    /// </summary>
    public List<Move> MonsterCommit { get; set; } = new();

    /// <summary>
    /// Monster narration string selected per committed slot. Parallel to MonsterCommit;
    /// preserved across the request boundary so the UI can display the monster's chosen
    /// flavour even if the move pool has multiple narration variants.
    /// </summary>
    public List<string> MonsterCommitNarration { get; set; } = new();

    /// <summary>Carry-stun: index N true means slot N+1 of the *next* turn is locked to Skipped.</summary>
    public bool[] PlayerCarryStun { get; set; } = new bool[3];
    public bool[] MonsterCarryStun { get; set; } = new bool[3];

    /// <summary>Cooldown bookkeeping for Power ("once per turn") / Slow ("once every other turn").
    /// Keyed by <c>Move.Encoded</c>; value is the turn number when the move was last used.</summary>
    public Dictionary<string, int> PlayerLastUsedTurn { get; set; } = new();
    public Dictionary<string, int> MonsterLastUsedTurn { get; set; } = new();

    /// <summary>
    /// True iff the player committed Read this turn — consumed at the start of the
    /// next turn to reveal the AI's plan, then cleared.
    /// </summary>
    public bool RevealPlanNextTurn { get; set; }

    /// <summary>Pool restrictions on the upcoming turn's commitment (per super_rps.md § Conditions).</summary>
    public bool PlayerBerzerkNextTurn { get; set; }
    public bool PlayerFearNextTurn { get; set; }
    public bool MonsterBerzerkNextTurn { get; set; }
    public bool MonsterFearNextTurn { get; set; }

    public CombatPlayerProfile Profile { get; set; } = new();

    public bool PlayerWon { get; set; }
    public bool PlayerLost { get; set; }
    public bool PlayerFled { get; set; }
    public bool MonsterFled { get; set; }

    public bool Resolved => PlayerWon || PlayerLost || PlayerFled || MonsterFled;
}
