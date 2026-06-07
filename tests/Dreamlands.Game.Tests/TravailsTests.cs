using Dreamlands.Game;
using Dreamlands.Rules;

namespace Dreamlands.Game.Tests;

/// <summary>
/// Travails engine tests (plans/travel_travails.md).
///
/// Old-system equivalence, for the record: travel conditions gave Untrained 0% resist,
/// so the player was afflicted on night 1 and drained 1 spirit/night while in-biome —
/// roughly 1 spirit per hazard-day. The travails model runs deliberately hotter
/// (Untrained ≈ 2-3 spirits per hazard-day): constant low-level spirits drain is the
/// game's money sink (inn refills), and deterministic costs carry no jackpot variance
/// to fear. Route data backing the calibration cases below:
/// project/reference/map_route_lengths.md.
/// </summary>
public class TravailsTests
{
    static readonly BalanceData Balance = BalanceData.Default;

    static PlayerState Fresh()
    {
        var p = PlayerState.NewGame("test", 99, Balance);
        p.Spirits = 20;
        p.MaxSpirits = 20;
        return p;
    }

    static void Walk(PlayerState p, string biome, int steps)
    {
        for (int i = 0; i < steps; i++)
            Travails.AccrueStep(p, biome, Balance);
    }

    static void Camp(PlayerState p, int nights)
    {
        for (int i = 0; i < nights; i++)
            Travails.AccrueNight(p, Balance);
    }

    // ── Step accrual ──

    [Fact]
    public void Step_NonHazardBiome_CostsNothing()
    {
        var p = Fresh();
        Walk(p, "plains", 10);
        Walk(p, "forest", 10);
        Walk(p, "swamp", 10);
        Assert.Equal(20, p.Spirits);
    }

    [Fact]
    public void Step_Scrub_Untrained_ChargesEveryTwoSteps()
    {
        var p = Fresh();
        Walk(p, "scrub", 1);
        Assert.Equal(20, p.Spirits);   // below threshold
        Walk(p, "scrub", 1);
        Assert.Equal(19, p.Spirits);   // 2 steps -> 1 spirit
        Walk(p, "scrub", 2);
        Assert.Equal(18, p.Spirits);   // 4 steps -> 2 spirits
    }

    [Fact]
    public void Step_Mountains_AccruesColdChannel()
    {
        var p = Fresh();
        Walk(p, "mountains", 2);
        Assert.Equal(19, p.Spirits);
        Assert.Equal(2, p.TravailLedger["cold"].Units);
        Assert.False(p.TravailLedger.ContainsKey("thirst"));
    }

    [Fact]
    public void Step_Trained_ChargesEveryFourSteps()
    {
        var p = Fresh();
        p.Skills[Skill.Bushcraft] = SkillTier.Trained;
        Walk(p, "scrub", 3);
        Assert.Equal(20, p.Spirits);
        Walk(p, "scrub", 1);
        Assert.Equal(19, p.Spirits);
    }

    [Fact]
    public void Step_Expert_ChargesEveryEightSteps()
    {
        var p = Fresh();
        p.Skills[Skill.Bushcraft] = SkillTier.Expert;
        Walk(p, "scrub", 7);
        Assert.Equal(20, p.Spirits);
        Walk(p, "scrub", 1);
        Assert.Equal(19, p.Spirits);
    }

    [Fact]
    public void Step_Waterskin_SparesThirstEntirely()
    {
        var p = Fresh();
        p.Pack.Add(new ItemInstance("waterskin", "Waterskin"));
        Walk(p, "scrub", 10);
        Assert.Equal(20, p.Spirits);
        Assert.Equal(0, p.TravailLedger["thirst"].Units);
        Assert.Equal(10, p.TravailLedger["thirst"].Spared);
    }

    [Fact]
    public void Step_WaterskinPickedUpMidJourney_StopsFurtherAccrual()
    {
        var p = Fresh();
        Walk(p, "scrub", 2);                                  // 1 spirit gone
        p.Pack.Add(new ItemInstance("waterskin", "Waterskin"));
        Walk(p, "scrub", 6);                                  // all spared
        Assert.Equal(19, p.Spirits);
        Assert.Equal(2, p.TravailLedger["thirst"].Units);
        Assert.Equal(6, p.TravailLedger["thirst"].Spared);
    }

    // ── Night accrual (fatigue) ──

    [Fact]
    public void Night_Untrained_OneSpiritPerNight_FromNightOne()
    {
        var p = Fresh();
        Camp(p, 1);
        Assert.Equal(19, p.Spirits);   // no grace night — decision 5
        Camp(p, 3);
        Assert.Equal(16, p.Spirits);
    }

    [Fact]
    public void Night_Trained_EveryOtherNight()
    {
        var p = Fresh();
        p.Skills[Skill.Bushcraft] = SkillTier.Trained;
        Camp(p, 1);
        Assert.Equal(20, p.Spirits);
        Camp(p, 1);
        Assert.Equal(19, p.Spirits);
        Camp(p, 2);
        Assert.Equal(18, p.Spirits);
    }

    [Fact]
    public void Night_ScarecrowBoots_ZeroFatigue()
    {
        var p = Fresh();
        p.Pack.Add(new ItemInstance("scarecrow_boots", "Scarecrow Boots"));
        Camp(p, 8);
        Assert.Equal(20, p.Spirits);
        Assert.Equal(8, p.TravailLedger["fatigue"].Spared);
    }

    // ── Charging edges ──

    [Fact]
    public void Charge_FloorsAtZeroSpirits()
    {
        var p = Fresh();
        p.Spirits = 1;
        Walk(p, "scrub", 8);   // owes 4
        Assert.Equal(0, p.Spirits);
    }

    [Fact]
    public void Charge_TierUpgradeMidJourney_NeverRefunds()
    {
        var p = Fresh();
        Walk(p, "scrub", 4);   // 2 spirits charged untrained
        p.Skills[Skill.Bushcraft] = SkillTier.Expert;
        Walk(p, "scrub", 4);   // 8 steps / 8 = 1 owed total, already paid 2 -> no charge, no refund
        Assert.Equal(18, p.Spirits);
        Assert.Equal(2, p.TravailLedger["thirst"].SpiritsCharged);
    }

    // ── Summarize ──

    [Fact]
    public void Summarize_Empty_ReturnsNull()
    {
        var p = Fresh();
        Assert.Null(Travails.Summarize(p, Balance));
    }

    [Fact]
    public void Summarize_FlushesLedger_SecondCallReturnsNull()
    {
        var p = Fresh();
        Walk(p, "scrub", 4);
        var summary = Travails.Summarize(p, Balance);
        Assert.NotNull(summary);
        Assert.Empty(p.TravailLedger);
        Assert.Null(Travails.Summarize(p, Balance));
    }

    [Fact]
    public void Summarize_TotalsAcrossChannels()
    {
        var p = Fresh();
        Walk(p, "scrub", 4);       // 2 spirits
        Walk(p, "mountains", 2);   // 1 spirit
        Camp(p, 2);                // 2 spirits
        var summary = Travails.Summarize(p, Balance)!;
        Assert.Equal(5, summary.TotalSpiritsLost);
        Assert.Equal(3, summary.Lines.Count);
    }

    [Fact]
    public void Summarize_SufferedLine_ReadsAsNarrative()
    {
        var p = Fresh();
        Walk(p, "scrub", 2);
        var line = Travails.Summarize(p, Balance)!.Lines.Single();
        Assert.Equal("Walked the dry scrubland for half a day, lost 1 spirit to thirst.", line.Text);
        Assert.False(line.SparedByGear);
    }

    [Fact]
    public void Summarize_SparedLine_CreditsGear()
    {
        var p = Fresh();
        p.Pack.Add(new ItemInstance("waterskin", "Waterskin"));
        Walk(p, "scrub", 6);
        var line = Travails.Summarize(p, Balance)!.Lines.Single();
        Assert.True(line.SparedByGear);
        Assert.Equal("Your waterskin kept the thirst at bay.", line.Text);
        Assert.Equal(0, line.SpiritsLost);
    }

    [Fact]
    public void Summarize_FatigueLine_CountsNights()
    {
        var p = Fresh();
        Camp(p, 3);
        var line = Travails.Summarize(p, Balance)!.Lines.Single();
        Assert.Equal("Three nights on the road, lost 3 spirits to fatigue.", line.Text);
    }

    [Fact]
    public void Summarize_BelowThreshold_NoTollPhrasing()
    {
        var p = Fresh();
        Walk(p, "scrub", 1);
        var line = Travails.Summarize(p, Balance)!.Lines.Single();
        Assert.Equal(0, line.SpiritsLost);
        Assert.Contains("took no toll", line.Text);
    }

    [Fact]
    public void Summarize_MarksSkillEasing()
    {
        var p = Fresh();
        p.Skills[Skill.Bushcraft] = SkillTier.Trained;
        Walk(p, "scrub", 4);
        var line = Travails.Summarize(p, Balance)!.Lines.Single();
        Assert.True(line.EasedBySkill);
    }

    // ── Calibration cases from project/reference/map_route_lengths.md ──

    [Fact]
    public void Calibration_FoundryRoundTrip_UntrainedUngeared_Busts()
    {
        // foundry RT: 64 steps (12 scrub, 8 mountain), 12 road nights.
        // Untrained no gear: 12/2 + 8/2 + 12 = 22 > 20 budget — cannot come home.
        var p = Fresh();
        Walk(p, "scrub", 12);
        Walk(p, "mountains", 8);
        Walk(p, "plains", 42);
        Camp(p, 12);
        Assert.Equal(0, p.Spirits);
        var total = p.TravailLedger.Values.Sum(t => t.SpiritsCharged);
        Assert.Equal(22, total);
    }

    [Fact]
    public void Calibration_FoundryRoundTrip_TrainedGeared_Comfortable()
    {
        // Trained + waterskin + bedroll: hazards spared, fatigue 12 nights / 2 = 6.
        var p = Fresh();
        p.Skills[Skill.Bushcraft] = SkillTier.Trained;
        p.Pack.Add(new ItemInstance("waterskin", "Waterskin"));
        p.Pack.Add(new ItemInstance("sleeping_kit", "Wool Bedroll"));
        Walk(p, "scrub", 12);
        Walk(p, "mountains", 8);
        Walk(p, "plains", 42);
        Camp(p, 12);
        Assert.Equal(14, p.Spirits);
    }

    [Fact]
    public void Calibration_T1Hop_CostsOneSpirit()
    {
        // Median T1 hop: 6 plains steps, 1 road night -> exactly the constant
        // low-level drain the economy wants (1 spirit -> inn bed every ~5 legs).
        var p = Fresh();
        Walk(p, "plains", 6);
        Camp(p, 1);
        Assert.Equal(19, p.Spirits);
    }
}
