using Dreamlands.Game;
using Dreamlands.Rules;

namespace Dreamlands.Game.Tests;

/// <summary>
/// Tests for SkillResolution — the tier-driven picker and passive resist model.
/// Replaces SkillChecksTests (d20 era).
/// </summary>
public class SkillResolutionTests
{
    // ── ResolvePicker — Expert tier ──

    [Fact]
    public void Expert_Correct_Succeeds_Direct()
    {
        var (outcome, connector) = SkillResolution.ResolvePicker(
            SkillTier.Expert, "plan", "plan", "push", new Random(0));
        Assert.Equal(PickerOutcome.Succeed, outcome);
        Assert.Equal(ConnectorKind.Direct, connector);
    }

    [Fact]
    public void Expert_Neutral_Succeeds_NeutralSuccess()
    {
        // "reroute" is neither correct ("plan") nor wrong ("push")
        var (outcome, connector) = SkillResolution.ResolvePicker(
            SkillTier.Expert, "reroute", "plan", "push", new Random(0));
        Assert.Equal(PickerOutcome.Succeed, outcome);
        Assert.Equal(ConnectorKind.NeutralSuccess, connector);
    }

    [Fact]
    public void Expert_Wrong_Fails_Direct()
    {
        var (outcome, connector) = SkillResolution.ResolvePicker(
            SkillTier.Expert, "push", "plan", "push", new Random(0));
        Assert.Equal(PickerOutcome.Fail, outcome);
        Assert.Equal(ConnectorKind.Direct, connector);
    }

    // ── ResolvePicker — Trained tier ──

    [Fact]
    public void Trained_Correct_Succeeds_Direct()
    {
        var (outcome, connector) = SkillResolution.ResolvePicker(
            SkillTier.Trained, "plan", "plan", "push", new Random(0));
        Assert.Equal(PickerOutcome.Succeed, outcome);
        Assert.Equal(ConnectorKind.Direct, connector);
    }

    [Fact]
    public void Trained_Neutral_Fails_NeutralFail()
    {
        var (outcome, connector) = SkillResolution.ResolvePicker(
            SkillTier.Trained, "reroute", "plan", "push", new Random(0));
        Assert.Equal(PickerOutcome.Fail, outcome);
        Assert.Equal(ConnectorKind.NeutralFail, connector);
    }

    [Fact]
    public void Trained_Wrong_Fails_Direct()
    {
        var (outcome, connector) = SkillResolution.ResolvePicker(
            SkillTier.Trained, "push", "plan", "push", new Random(0));
        Assert.Equal(PickerOutcome.Fail, outcome);
        Assert.Equal(ConnectorKind.Direct, connector);
    }

    // ── ResolvePicker — Untrained tier ──

    [Fact]
    public void Untrained_Correct_CoinflipHappensAtAll()
    {
        // Run enough seeds to see both outcomes (50% coinflip)
        bool sawSucceed = false, sawFail = false;
        for (int seed = 0; seed < 100; seed++)
        {
            var (outcome, connector) = SkillResolution.ResolvePicker(
                SkillTier.Untrained, "plan", "plan", "push", new Random(seed));
            if (outcome == PickerOutcome.Succeed && connector == ConnectorKind.Direct)
                sawSucceed = true;
            if (outcome == PickerOutcome.Fail && connector == ConnectorKind.CorrectFailed)
                sawFail = true;
            if (sawSucceed && sawFail) break;
        }
        Assert.True(sawSucceed, "Untrained correct should sometimes succeed");
        Assert.True(sawFail,    "Untrained correct should sometimes fail with CorrectFailed connector");
    }

    [Fact]
    public void Untrained_Neutral_Fails_NeutralFail()
    {
        var (outcome, connector) = SkillResolution.ResolvePicker(
            SkillTier.Untrained, "reroute", "plan", "push", new Random(0));
        Assert.Equal(PickerOutcome.Fail, outcome);
        Assert.Equal(ConnectorKind.NeutralFail, connector);
    }

    [Fact]
    public void Untrained_Wrong_Fails_Direct()
    {
        var (outcome, connector) = SkillResolution.ResolvePicker(
            SkillTier.Untrained, "push", "plan", "push", new Random(0));
        Assert.Equal(PickerOutcome.Fail, outcome);
        Assert.Equal(ConnectorKind.Direct, connector);
    }

    // ── RollPassiveResist ──

    [Fact]
    public void PassiveResist_Untrained_AlwaysFails()
    {
        for (int seed = 0; seed < 50; seed++)
            Assert.False(SkillResolution.RollPassiveResist(SkillTier.Untrained, new Random(seed)));
    }

    [Fact]
    public void PassiveResist_Trained_40Percent()
    {
        int successes = 0;
        for (int seed = 0; seed < 1000; seed++)
            if (SkillResolution.RollPassiveResist(SkillTier.Trained, new Random(seed)))
                successes++;
        // Should be around 400 ± 50 (3σ)
        Assert.InRange(successes, 350, 450);
    }

    [Fact]
    public void PassiveResist_Expert_80Percent()
    {
        int successes = 0;
        for (int seed = 0; seed < 1000; seed++)
            if (SkillResolution.RollPassiveResist(SkillTier.Expert, new Random(seed)))
                successes++;
        // Should be around 800 ± 50 (3σ)
        Assert.InRange(successes, 750, 850);
    }

    // ── Equip mutual-exclusion via Mechanics ──

    static readonly BalanceData Balance = BalanceData.Default;
    static PlayerState Fresh() => PlayerState.NewGame("test", 99, Balance);

    [Fact]
    public void Equip_SetsIsEquippedFlag()
    {
        var state = Fresh();
        state.Pack.Add(new ItemInstance("hunting_knife", "Hunting Knife"));
        Mechanics.Apply(["equip hunting_knife"], state, Balance, new Random(0));
        Assert.NotNull(state.EquippedWeapon);
        Assert.Equal("hunting_knife", state.EquippedWeapon!.DefId);
        Assert.Contains(state.Pack, i => i.DefId == "hunting_knife" && i.IsEquipped);
    }

    [Fact]
    public void Equip_MutualExclusion_UnequipsOldItem()
    {
        var state = Fresh();
        // Add scimitar and mark it equipped
        var scimitar = new ItemInstance("scimitar", "Scimitar") { IsEquipped = true };
        state.Pack.Add(scimitar);
        state.Pack.Add(new ItemInstance("hunting_knife", "Hunting Knife"));

        Mechanics.Apply(["equip hunting_knife"], state, Balance, new Random(0));

        Assert.Equal("hunting_knife", state.EquippedWeapon!.DefId);
        Assert.False(state.Pack.First(i => i.DefId == "scimitar").IsEquipped);
    }

    [Fact]
    public void Unequip_ClearsFlag()
    {
        var state = Fresh();
        var knife = new ItemInstance("hunting_knife", "Hunting Knife") { IsEquipped = true };
        state.Pack.Add(knife);

        Mechanics.Apply(["unequip weapon"], state, Balance, new Random(0));

        Assert.Null(state.EquippedWeapon);
        Assert.False(state.Pack.First(i => i.DefId == "hunting_knife").IsEquipped);
    }
}
