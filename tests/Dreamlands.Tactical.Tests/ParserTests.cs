using Xunit;
using Dreamlands.Tactical;

namespace Dreamlands.Tactical.Tests;

public class ParserTests
{
    const string CombatEncounter = """
        Wolves of the Cairn Road
        [variant combat]
        [intent violence]
        [tier 2]

        Three wolves materialize from the scrub grass on either side of the road.
        The largest has a scar across its muzzle.

        stats:
          resistance 12
          momentum 4

        timers:
          draw 2
          * Flanking Maneuver: spirits 2 every 4
          * Alpha's Howl: resistance 2 every 5
          * Cornered Prey: spirits 1 every 3

        openings:
          * Throat Strike: momentum 2 -> damage 4
          * Feint and Slash: momentum 1 -> damage 2
          * Defensive Stance: free -> momentum 2
          * Break Away: tick -> stop_timer
          * Spring the Trap: free -> damage 6 [requires has bear_trap]

        approaches:
          * scout: momentum 0, timers 3, openings 3
          * direct: momentum 4, timers 3
          * wild: momentum 6, timers 4

        failure:
          The wolves drag you down. You stagger away bloodied.
          +damage_spirits 2
          +lose_random_item
        """;

    const string TraverseEncounter = """
        The Shattered Bridge
        [variant traverse]
        [intent exploration]
        [tier 1]

        The bridge across the gorge has collapsed to a skeleton of stone pillars
        and dangling rope.

        stats:
          resistance 8
          queue_depth 4

        timers:
          draw 1
          * Crumbling Pillar: resistance 1 every 3
          * Wind Gust: spirits 1 every 2

        openings:
          * Rope Swing: tick -> damage 3
          * Careful Step: free -> damage 1
          * Anchor Point: tick -> damage 4 [requires has climbing_rope]
          * Rest and Assess: tick -> momentum 1

        failure:
          You lose your grip halfway across.
          +damage_spirits 3
          +lose_random_item
        """;

    const string GroupFile = """
        The Cairn Road Ambush
        [tier 2]

        The road bends through a narrow passage between two cairns.
        Something is wrong.

        branches:
          * Fight through [intent violence] -> wolves_of_the_cairn_road
          * Sneak past [intent stealth] -> cairn_road_bypass [requires has light_armor]
          * Parley [intent negotiation] -> cairn_road_parley
        """;

    // ── Combat encounter ───────────────────────────────────────────

    [Fact]
    public void ParsesCombatEncounter()
    {
        var result = TacticalParser.Parse(CombatEncounter);
        Assert.True(result.IsSuccess, string.Join("; ", result.Errors));
        Assert.NotNull(result.Encounter);
        Assert.Null(result.Group);
    }

    [Fact]
    public void CombatTitle()
    {
        var enc = TacticalParser.Parse(CombatEncounter).Encounter!;
        Assert.Equal("Wolves of the Cairn Road", enc.Title);
    }

    [Fact]
    public void CombatFrontMatter()
    {
        var enc = TacticalParser.Parse(CombatEncounter).Encounter!;
        Assert.Equal(Variant.Combat, enc.Variant);
        Assert.Equal("violence", enc.Intent);
        Assert.Equal(2, enc.Tier);
    }

    [Fact]
    public void CombatBody()
    {
        var enc = TacticalParser.Parse(CombatEncounter).Encounter!;
        Assert.Contains("Three wolves materialize", enc.Body);
        Assert.Contains("scar across its muzzle", enc.Body);
    }

    [Fact]
    public void CombatStats()
    {
        var enc = TacticalParser.Parse(CombatEncounter).Encounter!;
        Assert.Equal(12, enc.Resistance);
        Assert.Equal(4, enc.Momentum);
        Assert.Null(enc.QueueDepth);
    }

    [Fact]
    public void CombatTimers()
    {
        var enc = TacticalParser.Parse(CombatEncounter).Encounter!;
        Assert.Equal(2, enc.TimerDraw);
        Assert.Equal(3, enc.Timers.Count);

        var flanking = enc.Timers[0];
        Assert.Equal("Flanking Maneuver", flanking.Name);
        Assert.Equal(TimerEffect.Spirits, flanking.Effect);
        Assert.Equal(2, flanking.Amount);
        Assert.Equal(4, flanking.Countdown);

        var howl = enc.Timers[1];
        Assert.Equal("Alpha's Howl", howl.Name);
        Assert.Equal(TimerEffect.Resistance, howl.Effect);
    }

    [Fact]
    public void CombatOpenings()
    {
        var enc = TacticalParser.Parse(CombatEncounter).Encounter!;
        Assert.Equal(5, enc.Openings.Count);

        var throat = enc.Openings[0];
        Assert.Equal("Throat Strike", throat.Name);
        Assert.Equal(CostKind.Momentum, throat.Cost.Kind);
        Assert.Equal(2, throat.Cost.Amount);
        Assert.Equal(EffectKind.Damage, throat.Effect.Kind);
        Assert.Equal(4, throat.Effect.Amount);
        Assert.Null(throat.Requires);

        var stance = enc.Openings[2];
        Assert.Equal("Defensive Stance", stance.Name);
        Assert.Equal(CostKind.Free, stance.Cost.Kind);
        Assert.Equal(EffectKind.Momentum, stance.Effect.Kind);
        Assert.Equal(2, stance.Effect.Amount);

        var breakAway = enc.Openings[3];
        Assert.Equal(CostKind.Tick, breakAway.Cost.Kind);
        Assert.Equal(EffectKind.StopTimer, breakAway.Effect.Kind);

        var trap = enc.Openings[4];
        Assert.Equal("Spring the Trap", trap.Name);
        Assert.Equal("has bear_trap", trap.Requires);
    }

    [Fact]
    public void CombatApproaches()
    {
        var enc = TacticalParser.Parse(CombatEncounter).Encounter!;
        Assert.Equal(3, enc.Approaches.Count);

        Assert.Equal(ApproachKind.Scout, enc.Approaches[0].Kind);
        Assert.Equal(0, enc.Approaches[0].Momentum);
        Assert.Equal(3, enc.Approaches[0].TimerCount);
        Assert.Equal(3, enc.Approaches[0].BonusOpenings);

        Assert.Equal(ApproachKind.Direct, enc.Approaches[1].Kind);
        Assert.Equal(4, enc.Approaches[1].Momentum);
        Assert.Equal(0, enc.Approaches[1].BonusOpenings);

        Assert.Equal(ApproachKind.Wild, enc.Approaches[2].Kind);
        Assert.Equal(6, enc.Approaches[2].Momentum);
        Assert.Equal(4, enc.Approaches[2].TimerCount);
    }

    [Fact]
    public void CombatFailure()
    {
        var enc = TacticalParser.Parse(CombatEncounter).Encounter!;
        Assert.NotNull(enc.Failure);
        Assert.Contains("wolves drag you down", enc.Failure.Text);
        Assert.Equal(2, enc.Failure.Mechanics.Count);
        Assert.Equal("damage_spirits 2", enc.Failure.Mechanics[0]);
        Assert.Equal("lose_random_item", enc.Failure.Mechanics[1]);
    }

    // ── Traverse encounter ─────────────────────────────────────────

    [Fact]
    public void ParsesTraverseEncounter()
    {
        var result = TacticalParser.Parse(TraverseEncounter);
        Assert.True(result.IsSuccess, string.Join("; ", result.Errors));
        var enc = result.Encounter!;
        Assert.Equal(Variant.Traverse, enc.Variant);
        Assert.Equal("exploration", enc.Intent);
    }

    [Fact]
    public void TraverseStats()
    {
        var enc = TacticalParser.Parse(TraverseEncounter).Encounter!;
        Assert.Equal(8, enc.Resistance);
        Assert.Null(enc.Momentum);
        Assert.Equal(4, enc.QueueDepth);
    }

    [Fact]
    public void TraverseOpeningsWithRequires()
    {
        var enc = TacticalParser.Parse(TraverseEncounter).Encounter!;
        var anchor = enc.Openings[2];
        Assert.Equal("Anchor Point", anchor.Name);
        Assert.Equal("has climbing_rope", anchor.Requires);
    }

    // ── Group ──────────────────────────────────────────────────────

    [Fact]
    public void ParsesGroup()
    {
        var result = TacticalParser.Parse(GroupFile);
        Assert.True(result.IsSuccess, string.Join("; ", result.Errors));
        Assert.Null(result.Encounter);
        Assert.NotNull(result.Group);
    }

    [Fact]
    public void GroupMetadata()
    {
        var grp = TacticalParser.Parse(GroupFile).Group!;
        Assert.Equal("The Cairn Road Ambush", grp.Title);
        Assert.Equal(2, grp.Tier);
        Assert.Contains("narrow passage", grp.Body);
    }

    [Fact]
    public void GroupBranches()
    {
        var grp = TacticalParser.Parse(GroupFile).Group!;
        Assert.Equal(3, grp.Branches.Count);

        Assert.Equal("Fight through", grp.Branches[0].Label);
        Assert.Equal("violence", grp.Branches[0].Intent);
        Assert.Equal("wolves_of_the_cairn_road", grp.Branches[0].EncounterRef);
        Assert.Null(grp.Branches[0].Requires);

        Assert.Equal("Sneak past", grp.Branches[1].Label);
        Assert.Equal("stealth", grp.Branches[1].Intent);
        Assert.Equal("cairn_road_bypass", grp.Branches[1].EncounterRef);
        Assert.Equal("has light_armor", grp.Branches[1].Requires);

        Assert.Equal("Parley", grp.Branches[2].Label);
        Assert.Equal("negotiation", grp.Branches[2].Intent);
    }

    // ── Validation errors ──────────────────────────────────────────

    [Fact]
    public void EmptyFileErrors()
    {
        var result = TacticalParser.Parse("");
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void MissingVariantErrors()
    {
        var source = """
            Test Encounter

            Body text.

            stats:
              resistance 8
              momentum 3

            openings:
              * Strike: momentum 2 -> damage 3

            failure:
              You fail.
            """;
        var result = TacticalParser.Parse(source);
        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Message.Contains("variant"));
    }

    [Fact]
    public void CombatWithoutMomentumErrors()
    {
        var source = """
            Test Encounter
            [variant combat]

            Body text.

            stats:
              resistance 8

            openings:
              * Strike: momentum 2 -> damage 3

            failure:
              You fail.
            """;
        var result = TacticalParser.Parse(source);
        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Message.Contains("momentum"));
    }

    [Fact]
    public void TraverseWithoutQueueDepthErrors()
    {
        var source = """
            Test Encounter
            [variant traverse]

            Body text.

            stats:
              resistance 8

            openings:
              * Step: free -> damage 1

            failure:
              You fail.
            """;
        var result = TacticalParser.Parse(source);
        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Message.Contains("queue_depth"));
    }

    [Fact]
    public void TimerDrawExceedsPoolErrors()
    {
        var source = """
            Test Encounter
            [variant combat]

            Body text.

            stats:
              resistance 8
              momentum 3

            timers:
              draw 5
              * Timer A: spirits 1 every 3

            openings:
              * Strike: momentum 2 -> damage 3

            failure:
              You fail.
            """;
        var result = TacticalParser.Parse(source);
        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Message.Contains("draw"));
    }

    [Fact]
    public void MixedGroupAndEncounterErrors()
    {
        var source = """
            Test
            [variant combat]

            Body.

            stats:
              resistance 8
              momentum 3

            branches:
              * Fight -> some_encounter
            """;
        var result = TacticalParser.Parse(source);
        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Message.Contains("both"));
    }

    [Fact]
    public void InvalidCostErrors()
    {
        var source = """
            Test Encounter
            [variant combat]

            Body text.

            stats:
              resistance 8
              momentum 3

            openings:
              * Strike: mana 2 -> damage 3

            failure:
              You fail.
            """;
        var result = TacticalParser.Parse(source);
        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Message.Contains("cost") || e.Message.Contains("Invalid"));
    }

    [Fact]
    public void InvalidEffectErrors()
    {
        var source = """
            Test Encounter
            [variant combat]

            Body text.

            stats:
              resistance 8
              momentum 3

            openings:
              * Strike: momentum 2 -> heal 3

            failure:
              You fail.
            """;
        var result = TacticalParser.Parse(source);
        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Message.Contains("effect") || e.Message.Contains("Invalid"));
    }

    [Fact]
    public void RequiresOnEncounterLevel()
    {
        var source = """
            Test Encounter
            [variant combat]
            [requires tag some_flag]
            [requires has sword]

            Body text.

            stats:
              resistance 8
              momentum 3

            openings:
              * Strike: momentum 2 -> damage 3

            failure:
              You fail.
            """;
        var result = TacticalParser.Parse(source);
        Assert.True(result.IsSuccess, string.Join("; ", result.Errors));
        Assert.Equal(2, result.Encounter!.Requires.Count);
        Assert.Equal("tag some_flag", result.Encounter.Requires[0]);
        Assert.Equal("has sword", result.Encounter.Requires[1]);
    }

    [Fact]
    public void NoTimerSectionIsValid()
    {
        var source = """
            Simple Fight
            [variant combat]

            A straightforward encounter with no timers.

            stats:
              resistance 6
              momentum 3

            openings:
              * Strike: momentum 1 -> damage 2
              * Guard: free -> momentum 1

            approaches:
              * scout: momentum 0, timers 0, openings 3
              * direct: momentum 3, timers 0
              * wild: momentum 6, timers 0

            failure:
              You lose.
            """;
        var result = TacticalParser.Parse(source);
        Assert.True(result.IsSuccess, string.Join("; ", result.Errors));
        Assert.Equal(0, result.Encounter!.TimerDraw);
        Assert.Empty(result.Encounter.Timers);
    }
}
