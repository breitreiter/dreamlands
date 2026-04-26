using CombatPrototype.Cmb;

namespace CombatPrototype.Combat;

public sealed class CombatState
{
    public PlayerState Player { get; }
    public CmbEncounter Encounter { get; }

    public int MonsterHp { get; set; }
    public int MonsterAcBonusThisTurn { get; set; }

    /// <summary>Cooldown remaining (in turns) for each non-basic move id.</summary>
    public Dictionary<string, int> Cooldowns { get; } = new();

    /// <summary>The move the monster will execute on its next turn (revealed to player).</summary>
    public MonsterMove NextMove { get; set; }

    public int Round { get; set; }
    public bool PlayerWins { get; set; }
    public bool PlayerLoses { get; set; }
    public bool PlayerFled { get; set; }
    public bool MonsterFled { get; set; }
    public bool Aborted { get; set; }

    public bool Resolved => PlayerWins || PlayerLoses || PlayerFled || MonsterFled || Aborted;

    public CombatState(PlayerState player, CmbEncounter encounter)
    {
        Player = player;
        Encounter = encounter;
        MonsterHp = encounter.Stats.Hp;
        foreach (var move in encounter.Moves)
        {
            if (!move.IsBasic)
                Cooldowns[move.Id] = move.Timer;  // start fresh: count down from full
        }
        NextMove = encounter.BasicMove;
    }

    public int MonsterEffectiveAc => Encounter.Stats.Ac + MonsterAcBonusThisTurn;
}
