using Dreamlands.Combat;
using Dreamlands.Encounter;
using Dreamlands.Game;

namespace Dreamlands.Combat.Tests;

public class CombatRunnerTests
{
    static CombatEncounter MakeEncounter(string moves = """
        +move Attack
          narration: It strikes.

        +move Defend
          narration: It hunches.

        +move Recover
          narration: It catches its breath.
        """)
    {
        var enc = CmbParser.ParseString($"""
            +title Test Foe
            +stats hp=12

            {moves}

            +intro
            A test foe appears.

            +win
            > gold 1
            You win.

            +lose
            > tag died
            You lose.
            """);
        enc.Id = "test/foe";
        return enc;
    }

    static PlayerState MakePlayer(int spirits = 20, int health = 4) =>
        new()
        {
            Spirits = spirits,
            MaxSpirits = spirits,
            Health = health,
            MaxHealth = health,
        };

    static CombatState MakeState() => new()
    {
        // Falchion (T-1 sword) + Hide Armor (T-1 medium): basic Attack + Defend pool.
        Profile = CombatPlayerProfile.From(
            weapon: Rules.ItemDef.All["falchion"],
            armor:  Rules.ItemDef.All["hide_armor"]),
    };

    [Fact]
    public void PlayerProfile_composes_pool_from_gear_plus_universals()
    {
        // Hunting Knife (T-1 dagger): just "Attack".
        // Hide Armor (T-1 medium): just "Defend".
        // Universal: Recover, Read.
        var profile = CombatPlayerProfile.From(
            weapon: Rules.ItemDef.All["hunting_knife"],
            armor:  Rules.ItemDef.All["hide_armor"]);

        var encoded = profile.MovePool.Select(m => m.Encoded).ToList();
        Assert.Equal(new[] { "Attack", "Defend", "Recover", "Read" }, encoded);
    }

    [Fact]
    public void PlayerProfile_no_weapon_drops_attack_family()
    {
        // No weapon → no Attack at all (T-0 weapon = "disable attack action").
        var profile = CombatPlayerProfile.From(
            weapon: null,
            armor:  Rules.ItemDef.All["tunic"]);

        Assert.DoesNotContain(profile.MovePool, m => m.Move.Base == "attack");
        Assert.Contains(profile.MovePool, m => m.Move.Base == "defend");
        Assert.Contains(profile.MovePool, m => m.Move.Base == "recover");
        Assert.Contains(profile.MovePool, m => m.Move.Base == "read");
    }

    [Fact]
    public void PlayerProfile_no_armor_falls_back_to_basic_defend()
    {
        var profile = CombatPlayerProfile.From(
            weapon: Rules.ItemDef.All["falchion"],
            armor:  null);

        Assert.Contains(profile.MovePool, m => m.Move.Base == "defend" && m.Move.Mutators.Count == 0);
    }

    [Fact]
    public void PlayerProfile_better_adjective_drops_unmutated_base()
    {
        // Per-move "Better" adjective marks "Wary Read" as a strict improvement
        // over plain Read; equipping this hat drops universal Read from the pool.
        // "Wary Read" (no Better) on a sibling item would NOT drop plain Read —
        // the flag is per-move, not per-item.
        var hat = new Rules.ItemDef
        {
            Id = "test_hat", Name = "Test Hat", Type = Rules.ItemType.Armor,
            ArmorClass = Rules.ArmorClass.Light,
            RpsMoves = [new("Better Wary Read", "Better Wary Read")],
        };

        var profile = CombatPlayerProfile.From(
            weapon: Rules.ItemDef.All["hunting_knife"],
            armor:  hat);

        var encoded = profile.MovePool.Select(m => m.Encoded).ToList();
        // "Better" is metadata; it never appears in canonical encoded form.
        Assert.Contains("Wary Read", encoded);
        Assert.DoesNotContain("Read", encoded);
        Assert.DoesNotContain("Better Wary Read", encoded);
        // Other universals are untouched.
        Assert.Contains("Recover", encoded);
    }

    [Fact]
    public void PlayerProfile_legendary_kit_surfaces_full_moveset()
    {
        // The Old Tooth (T-4 dagger) + Robe of Twilight (T-4 light).
        var profile = CombatPlayerProfile.From(
            weapon: Rules.ItemDef.All["the_old_tooth"],
            armor:  Rules.ItemDef.All["robe_of_twilight"]);

        // Mutators are sorted alphabetically in Move.Encoded — match canonical form.
        var encoded = profile.MovePool.Select(m => m.Encoded).ToHashSet();
        Assert.Contains("Riposte Attack", encoded);
        Assert.Contains("Heavy Power Provoking Attack", encoded);
        Assert.Contains("Heavy Power Wary Recover", encoded);
        Assert.Contains("Shielding Defend", encoded);
        Assert.Contains("Recover", encoded);
        Assert.Contains("Read", encoded);
    }

    [Fact]
    public void Begin_emits_intro_and_first_turn_with_tell()
    {
        var enc = MakeEncounter();
        var player = MakePlayer();
        var state = MakeState();

        var events = CombatRunner.Begin(enc, player, state, new Random(1));

        Assert.IsType<CombatEvent.Intro>(events[0]);
        var turn = Assert.IsType<CombatEvent.TurnStarted>(events[1]);
        Assert.Equal(1, turn.Turn);
        Assert.False(string.IsNullOrEmpty(turn.Tell));
        Assert.Null(turn.Plan); // Read isn't active turn 1
        Assert.Equal(3, state.MonsterCommit.Count);
    }

    [Fact]
    public void Step_resolves_three_slots()
    {
        var enc = MakeEncounter();
        var player = MakePlayer();
        var state = MakeState();

        var rng = new Random(0);
        CombatRunner.Begin(enc, player, state, rng);
        var events = CombatRunner.Step(enc, player, state,
            new PlayerCombatAction.Commit(Move.Parse("attack"), Move.Parse("defend"), Move.Parse("recover")),
            rng);

        var slots = events.OfType<CombatEvent.SlotResolved>().ToList();
        // May be fewer than 3 if combat resolves mid-turn; baseline encounter shouldn't drop monster in one turn.
        Assert.True(slots.Count >= 1);
        Assert.Equal(1, slots[0].Slot);
    }

    [Fact]
    public void Player_winning_emits_outcome_and_clears_state()
    {
        var enc = MakeEncounter("""
            +move Defend
              narration: It hunches.
            """);
        var player = MakePlayer();
        var state = MakeState();

        var rng = new Random(42);
        CombatRunner.Begin(enc, player, state, rng);

        // Bash repeatedly with Heavy Attack — 6 dmg/slot through Defend (4+4-2).
        // Three slots × 6 = 18 per turn; 12 HP monster dies turn 1.
        for (int i = 0; i < 5 && !state.Resolved; i++)
        {
            CombatRunner.Step(enc, player, state,
                new PlayerCombatAction.Commit(
                    Move.Parse("heavy attack"),
                    Move.Parse("heavy attack"),
                    Move.Parse("heavy attack")),
                rng);
        }

        Assert.True(state.PlayerWon);
        Assert.Equal(0, state.MonsterHp);
    }

    [Fact]
    public void Death_converts_remaining_slots_to_skipped()
    {
        // 4-HP monster, only Attack in pool. One Heavy Attack from the player kills
        // it on slot 1, but the runner should still emit slot-2 and slot-3 events
        // with both moves zeroed out — analogous to stun, "being dead" replaces
        // your subsequent actions.
        var enc = CmbParser.ParseString("""
            +title Frail Foe
            +stats hp=4

            +move Attack
              narration: It strikes.

            +intro
            A frail foe appears.

            +win
            > gold 1
            You win.

            +lose
            > tag died
            You lose.
            """);
        enc.Id = "test/frail";

        var player = MakePlayer(spirits: 20, health: 20);
        var state = MakeState();

        var rng = new Random(0);
        CombatRunner.Begin(enc, player, state, rng);
        var events = CombatRunner.Step(enc, player, state,
            new PlayerCombatAction.Commit(
                Move.Parse("heavy attack"),  // 8 dmg, kills monster outright
                Move.Parse("heavy attack"),
                Move.Parse("heavy attack")),
            rng);

        var slots = events.OfType<CombatEvent.SlotResolved>().ToList();
        Assert.Equal(3, slots.Count);

        // Slot 1: real kill. Move != Skipped.
        Assert.NotEqual("skipped", slots[0].PlayerMove.Base);
        Assert.True(slots[0].MonsterDelta < 0);

        // Slots 2 and 3: both sides converted to Skipped, no damage either way.
        for (int i = 1; i < 3; i++)
        {
            Assert.Equal("skipped", slots[i].PlayerMove.Base);
            Assert.Equal("skipped", slots[i].MonsterMove.Base);
            Assert.Equal(0, slots[i].PlayerDelta);
            Assert.Equal(0, slots[i].MonsterDelta);
            Assert.Equal(0, slots[i].MonsterHpAfter);
        }

        Assert.True(state.PlayerWon);
        Assert.Equal(0, state.MonsterHp);
    }

    [Fact]
    public void Recover_heals_spirits_up_to_max_only()
    {
        var enc = MakeEncounter();
        var player = MakePlayer(spirits: 20, health: 4);
        // Knock the player's Spirits down so Recover has somewhere to land.
        player.Spirits = 5;
        var state = MakeState();

        var rng = new Random(0);
        CombatRunner.Begin(enc, player, state, rng);
        // Three Recovers — they should heal up to the original 20 cap, no further.
        CombatRunner.Step(enc, player, state,
            new PlayerCombatAction.Commit(Move.Parse("heavy recover"), Move.Parse("heavy recover"), Move.Parse("heavy recover")),
            rng);

        Assert.True(player.Spirits <= player.MaxSpirits);
        // Health is never replenished by Recover.
        Assert.Equal(player.MaxHealth, player.Health);
    }

    [Fact]
    public void Read_reveals_plan_on_following_turn()
    {
        var enc = MakeEncounter();
        var player = MakePlayer();
        var state = MakeState();

        var rng = new Random(0);
        CombatRunner.Begin(enc, player, state, rng);
        // Player commits Read on turn 1.
        var events = CombatRunner.Step(enc, player, state,
            new PlayerCombatAction.Commit(Move.Parse("read"), Move.Parse("defend"), Move.Parse("defend")),
            rng);

        var nextTurn = events.OfType<CombatEvent.TurnStarted>().Last();
        Assert.NotNull(nextTurn.Plan);
        Assert.Equal(3, nextTurn.Plan!.Count);
    }

    [Fact]
    public void Stun_on_slot_3_carries_to_slot_1_of_next_turn()
    {
        // Encounter with only Recover in the pool — AI commits Recover/Recover/Recover
        // every turn. Player attacks at slot 3 → Attack vs Recover always stuns the
        // recoverer (monster). Slot 3 stun bleeds into slot 1 of next turn.
        var enc = CmbParser.ParseString("""
            +title Stationary Healer
            +stats hp=999

            +move Recover
              narration: It catches its breath.

            +intro
            Test foe.

            +win
            Won.

            +lose
            Lost.
            """);
        enc.Id = "test/healer";
        var player = MakePlayer(spirits: 999, health: 999);
        var state = MakeState();

        var rng = new Random(0);
        CombatRunner.Begin(enc, player, state, rng);

        CombatRunner.Step(enc, player, state,
            new PlayerCombatAction.Commit(
                Move.Parse("defend"),
                Move.Parse("defend"),
                Move.Parse("attack")),
            rng);

        Assert.True(state.MonsterCarryStun[0],
            "Slot 3 Attack vs Recover should carry-stun the monster's slot 1 of next turn.");
    }

    [Fact]
    public void Flee_burns_turn_and_ends_combat()
    {
        var enc = MakeEncounter();
        var player = MakePlayer();
        var state = MakeState();

        var rng = new Random(0);
        CombatRunner.Begin(enc, player, state, rng);
        var events = CombatRunner.Step(enc, player, state, new PlayerCombatAction.Flee(), rng);

        Assert.IsType<CombatEvent.PlayerFleeAttempted>(events[0]);
        // Slots still resolve (player skipped, monster acts) — terminal Outcome at the end.
        Assert.IsType<CombatEvent.Outcome>(events[^1]);
        Assert.True(state.PlayerFled);
    }
}
