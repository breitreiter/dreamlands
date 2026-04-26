using Dreamlands.Combat;
using Dreamlands.Encounter;
using Dreamlands.Game;

namespace Dreamlands.Combat.Tests;

public class CombatRunnerTests
{
    static CombatEncounter MakeEncounter() => CmbParser.ParseString("""
        +title Test Foe
        +stats hp=8 ac=10 to_hit=+0 damage=1d4

        +move strike
          intent: attack
          preview: "It strikes."
          timer: 0
          narration: "It strikes."
          > deal_damage 1d4

        +intro
        A test foe appears.

        +win
        > +give_gold 1
        You win.

        +lose
        > +damage_spirits 1
        You lose.
        """);

    static PlayerState MakePlayer(int spirits = 20, int health = 4)
    {
        return new PlayerState
        {
            Spirits = spirits,
            MaxSpirits = spirits,
            Health = health,
            MaxHealth = health,
        };
    }

    static CombatState MakeState(int atkBonus = 10, int dmgDie = 8) => new()
    {
        Profile = new CombatPlayerProfile
        {
            BaseAc = 18,
            AttackBonus = atkBonus,
            DamageBonus = 5,
            DamageDieCount = 1,
            DamageDieSize = dmgDie,
            Bushcraft = 5,
            Cunning = 0,
        }
    };

    [Fact]
    public void Begin_emits_intro_surprise_and_intent()
    {
        var enc = MakeEncounter();
        enc.Id = "test/foe";
        var player = MakePlayer();
        var state = MakeState();

        var events = CombatRunner.Begin(enc, player, state, new Random(1));
        Assert.IsType<CombatEvent.Intro>(events[0]);
        Assert.IsType<CombatEvent.SurpriseChecked>(events[1]);
        Assert.IsType<CombatEvent.RoundStarted>(events[2]);
        // Last event is always either Outcome or IntentPreviewed once we land waiting on player.
        Assert.True(events[^1] is CombatEvent.IntentPreviewed or CombatEvent.Outcome);
    }

    [Fact]
    public void Player_attack_kills_low_hp_monster_and_resolves()
    {
        var enc = MakeEncounter();
        enc.Id = "test/foe";
        var player = MakePlayer();
        var state = MakeState(atkBonus: 30, dmgDie: 12); // guaranteed hit + heavy damage

        // Force player-acts-first so the player gets the swing immediately.
        // Bushcraft +5 vs DC 12 will sometimes win surprise; seed until it does.
        // Easier: skip surprise by pre-setting state and skipping Begin.
        var rng = new Random(0);
        CombatRunner.Begin(enc, player, state, rng);
        // Drive the encounter with attacks until resolution. Player will eventually land.
        for (int i = 0; i < 20 && !state.Resolved; i++)
        {
            CombatRunner.Step(enc, player, state, new PlayerCombatAction.Attack(), rng);
        }
        Assert.True(state.Resolved);
        Assert.True(state.PlayerWon);
        Assert.Equal(0, state.MonsterHp);
    }

    [Fact]
    public void Win_sets_resolved_and_clears_path_for_outcome()
    {
        var enc = MakeEncounter();
        enc.Id = "test/foe";
        var player = MakePlayer();
        var state = MakeState(atkBonus: 50, dmgDie: 12);

        var rng = new Random(2);
        CombatRunner.Begin(enc, player, state, rng);
        var lastEvents = (IReadOnlyList<CombatEvent>)Array.Empty<CombatEvent>();
        for (int i = 0; i < 10 && !state.Resolved; i++)
        {
            lastEvents = CombatRunner.Step(enc, player, state, new PlayerCombatAction.Attack(), rng);
        }
        Assert.True(state.PlayerWon);
        Assert.IsType<CombatEvent.Outcome>(lastEvents[^1]);
        var outcome = (CombatEvent.Outcome)lastEvents[^1];
        Assert.True(outcome.PlayerWon);
        Assert.Contains("give_gold 1", outcome.Mechanics);
    }

    [Fact]
    public void Stance_change_is_free_action()
    {
        var enc = MakeEncounter();
        enc.Id = "test/foe";
        var player = MakePlayer();
        var state = MakeState();

        var rng = new Random(3);
        CombatRunner.Begin(enc, player, state, rng);
        int monsterHpBefore = state.MonsterHp;

        var events = CombatRunner.Step(enc, player, state, new PlayerCombatAction.SetStance(SwordStance.Aggressive), rng);
        Assert.Equal(SwordStance.Aggressive, state.Stance);
        Assert.Single(events);
        Assert.IsType<CombatEvent.StanceChanged>(events[0]);
        Assert.Equal(monsterHpBefore, state.MonsterHp); // no attack happened
        Assert.False(state.Resolved);
    }

    [Fact]
    public void Damage_ablates_spirits_then_health()
    {
        var enc = CmbParser.ParseString("""
            +title Pummeler
            +stats hp=999 ac=5 to_hit=+50 damage=1d4

            +move smash
              intent: attack
              timer: 0
              > deal_damage 100
            """);
        enc.Id = "test/pummeler";
        var player = MakePlayer(spirits: 20, health: 4);
        var state = MakeState(atkBonus: 0); // can't hit pummeler

        var rng = new Random(0);
        CombatRunner.Begin(enc, player, state, rng);
        // First round either way, the 100-damage hit goes through and kills the player.
        for (int i = 0; i < 5 && !state.Resolved; i++)
        {
            CombatRunner.Step(enc, player, state, new PlayerCombatAction.Attack(), rng);
        }
        Assert.True(state.PlayerLost);
        Assert.Equal(0, player.Spirits);
        Assert.Equal(0, player.Health);
    }

    [Fact]
    public void Cooldowns_tick_and_special_fires_on_schedule()
    {
        var enc = CmbParser.ParseString("""
            +title Timed
            +stats hp=999 ac=10 to_hit=+0 damage=1d4

            +move basic
              intent: attack
              timer: 0
              > deal_damage 0

            +move special
              intent: heavy_attack
              timer: 2
              > deal_damage 0
            """);
        enc.Id = "test/timed";
        var player = MakePlayer(spirits: 999, health: 999);
        var state = MakeState(atkBonus: 0); // can't kill the dummy

        var rng = new Random(0);
        CombatRunner.Begin(enc, player, state, rng);
        // After Begin, special.cooldown should be 2 (or 1 if monster already moved once).
        // Step a player turn — that closes one round → cooldown ticks.
        Assert.True(state.Cooldowns.ContainsKey("special"));
        var initialSpecialCd = state.Cooldowns["special"];

        CombatRunner.Step(enc, player, state, new PlayerCombatAction.Attack(), rng);
        // After one full round, the special cooldown ticks at least once.
        Assert.True(state.Cooldowns["special"] < initialSpecialCd
            || state.NextMoveId == "special");
    }
}
