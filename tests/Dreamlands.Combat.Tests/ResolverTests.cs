using Dreamlands.Combat;
using Dreamlands.Encounter;

namespace Dreamlands.Combat.Tests;

public class ResolverTests
{
    static Move M(string s) => Move.Parse(s);

    [Fact]
    public void Attack_vs_defend_basic_damage_minus_prevention()
    {
        var rng = new Random(0);
        var r = Resolver.Resolve(M("attack"), M("defend"), rng);
        // Attack 4 - Defend 2 = 2 damage to monster.
        Assert.Equal(0, r.PlayerDelta);
        Assert.Equal(-2, r.MonsterDelta);
    }

    [Fact]
    public void Big_attack_pushes_through_basic_defend()
    {
        var rng = new Random(0);
        var r = Resolver.Resolve(M("heavy attack"), M("defend"), rng);
        // Wait — Attack mutators in super_rps use "Heavy" for +4. Confirm.
        Assert.Equal(0, r.PlayerDelta);
        Assert.Equal(-6, r.MonsterDelta); // 4 base + 4 heavy - 2 defend = 6
    }

    [Fact]
    public void Perfect_defend_blocks_all_damage()
    {
        var rng = new Random(0);
        var r = Resolver.Resolve(M("attack"), M("perfect defend"), rng);
        Assert.Equal(0, r.MonsterDelta);
    }

    [Fact]
    public void Attack_vs_recover_cancels_heal_and_stuns()
    {
        var rng = new Random(0);
        // Player attacks; monster recovers. Recover gets cancelled (no heal),
        // recoverer (monster) is stunned for next slot.
        var r = Resolver.Resolve(M("attack"), M("recover"), rng);
        Assert.Equal(0, r.MonsterDelta + r.PlayerDelta - (-4)); // monster takes 4, no heal
        Assert.Equal(-4, r.MonsterDelta);
        Assert.True(r.StunMonsterNext);
    }

    [Fact]
    public void Riposte_vs_attack_prevents_two_and_adds_two()
    {
        var rng = new Random(0);
        // Both swing; player has Riposte. Player takes 4 - 2 = 2; monster takes 4 + 2 = 6.
        var r = Resolver.Resolve(M("riposte attack"), M("attack"), rng);
        Assert.Equal(-2, r.PlayerDelta);
        Assert.Equal(-6, r.MonsterDelta);
    }

    [Fact]
    public void Wary_recover_vs_attack_converts_to_defend()
    {
        var rng = new Random(0);
        // Wary Recover treated as basic Defend. Attack 4 - Defend 2 = 2 damage to player.
        var r = Resolver.Resolve(M("wary recover"), M("attack"), rng);
        Assert.Equal(-2, r.PlayerDelta);
        Assert.Equal(0, r.MonsterDelta);
        // No stun on the (converted-to-defend) target.
        Assert.False(r.StunPlayerNext);
    }

    [Fact]
    public void Recover_heals_when_unopposed()
    {
        var rng = new Random(0);
        var r = Resolver.Resolve(M("recover"), M("defend"), rng);
        Assert.Equal(4, r.PlayerDelta);
    }

    [Fact]
    public void Heavy_recover_heals_six()
    {
        var rng = new Random(0);
        var r = Resolver.Resolve(M("heavy recover"), M("defend"), rng);
        Assert.Equal(6, r.PlayerDelta);
    }

    [Fact]
    public void Exhausting_attack_self_stuns()
    {
        var rng = new Random(0);
        // Player commits Exhausting Attack — gets stunned next slot regardless.
        var r = Resolver.Resolve(M("exhausting attack"), M("defend"), rng);
        Assert.True(r.StunPlayerNext);
    }

    [Fact]
    public void Provoking_attack_berzerks_target_next_turn()
    {
        var rng = new Random(0);
        // Player Provoking Attack hits monster. Monster gets Berzerk for next turn.
        var r = Resolver.Resolve(M("provoking attack"), M("defend"), rng);
        Assert.True(r.BerzerkMonsterNext);
        Assert.False(r.BerzerkPlayerNext);
    }

    [Fact]
    public void Terrifying_attack_fears_target_next_turn()
    {
        var rng = new Random(0);
        var r = Resolver.Resolve(M("terrifying attack"), M("defend"), rng);
        Assert.True(r.FearMonsterNext);
        Assert.False(r.FearPlayerNext);
    }

    [Fact]
    public void Stunning_attack_can_proc_forward_stun_on_target()
    {
        // Stunning Attack has a chance to stun the target. The proc path should be
        // reachable across seeds — we don't pin a specific seed, just assert that
        // stun fires at least once across enough samples.
        bool sawStun = false;
        for (int seed = 0; seed < 50; seed++)
        {
            var rng = new Random(seed);
            var r = Resolver.Resolve(M("stunning attack"), M("defend"), rng);
            if (r.StunMonsterNext) { sawStun = true; break; }
        }
        Assert.True(sawStun, "Stunning Attack should be able to forward-stun the target.");
    }

    [Fact]
    public void Shielding_defend_blocks_stun_proc()
    {
        // Stunning Attack against Shielding Defend: even if proc rolls true, Shielding clears it.
        var rng = new Random(0);
        for (int i = 0; i < 50; i++)
        {
            var r = Resolver.Resolve(M("stunning attack"), M("shielding defend"), rng);
            Assert.False(r.StunMonsterNext);
        }
    }

    [Fact]
    public void Skipped_does_nothing()
    {
        var rng = new Random(0);
        var r = Resolver.Resolve(Move.Skipped(), M("attack"), rng);
        Assert.Equal(-4, r.PlayerDelta);
        Assert.Equal(0, r.MonsterDelta);
    }
}
