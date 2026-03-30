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

        timers:
          draw 2
          * Flanking Maneuver: spirits 2 every 4
          * Alpha's Howl: resistance 2 every 5
          * Cornered Prey: spirits 1 every 3

        openings:
          * Throat Strike: momentum_to_progress_large
          * Feint and Slash: momentum_to_progress
          * Defensive Stance: free_momentum
          * Break Away: momentum_to_cancel
          * Spring the Trap: spirits_to_progress_large [requires has bear_trap]

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

        timers:
          draw 1
          * Crumbling Pillar: resistance 1 every 3
          * Wind Gust: spirits 1 every 2

        openings:
          * Rope Swing: threat_to_progress_large
          * Careful Step: free_progress_small
          * Anchor Point: spirits_to_progress [requires has climbing_rope]
          * Rest and Assess: free_momentum_small

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
        Assert.Equal("momentum_to_progress_large", throat.Archetype);
        Assert.Null(throat.Requires);

        var stance = enc.Openings[2];
        Assert.Equal("Defensive Stance", stance.Name);
        Assert.Equal("free_momentum", stance.Archetype);

        var breakAway = enc.Openings[3];
        Assert.Equal("momentum_to_cancel", breakAway.Archetype);

        var trap = enc.Openings[4];
        Assert.Equal("Spring the Trap", trap.Name);
        Assert.Equal("spirits_to_progress_large", trap.Archetype);
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
    }

    [Fact]
    public void TraverseOpeningsWithRequires()
    {
        var enc = TacticalParser.Parse(TraverseEncounter).Encounter!;
        var anchor = enc.Openings[2];
        Assert.Equal("Anchor Point", anchor.Name);
        Assert.Equal("spirits_to_progress", anchor.Archetype);
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

            openings:
              * Strike: momentum_to_progress_large

            failure:
              You fail.
            """;
        var result = TacticalParser.Parse(source);
        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Message.Contains("variant"));
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

            timers:
              draw 5
              * Timer A: spirits 1 every 3

            openings:
              * Strike: momentum_to_progress_large

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

            branches:
              * Fight -> some_encounter
            """;
        var result = TacticalParser.Parse(source);
        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Message.Contains("both"));
    }

    [Fact]
    public void UnknownArchetypeErrors()
    {
        var source = """
            Test Encounter
            [variant combat]

            Body text.

            stats:
              resistance 8

            openings:
              * Strike: mana_blast_supreme

            failure:
              You fail.
            """;
        var result = TacticalParser.Parse(source);
        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Message.Contains("Unknown archetype"));
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

            openings:
              * Strike: momentum_to_progress_large

            failure:
              You fail.
            """;
        var result = TacticalParser.Parse(source);
        Assert.True(result.IsSuccess, string.Join("; ", result.Errors));
        Assert.Equal(2, result.Encounter!.Requires.Count);
        Assert.Equal("tag some_flag", result.Encounter.Requires[0]);
        Assert.Equal("has sword", result.Encounter.Requires[1]);
    }

    // ── Condition timers ───────────────────────────────────────────

    [Fact]
    public void ConditionTimerParses()
    {
        var source = """
            Jagged Terrain
            [variant combat]

            Sharp rocks everywhere.

            stats:
              resistance 8

            timers:
              draw 1
              * Falling Rocks: condition injured every 4

            openings:
              * Strike: momentum_to_progress_large

            failure:
              You stumble.
            """;
        var result = TacticalParser.Parse(source);
        Assert.True(result.IsSuccess, string.Join("; ", result.Errors));

        var timer = result.Encounter!.Timers[0];
        Assert.Equal("Falling Rocks", timer.Name);
        Assert.Equal(TimerEffect.Condition, timer.Effect);
        Assert.Equal("injured", timer.ConditionId);
        Assert.Equal(0, timer.Amount);
        Assert.Equal(4, timer.Countdown);
    }

    [Fact]
    public void ConditionTimerWithCounterParses()
    {
        var source = """
            Treacherous Path
            [variant traverse]

            The path is treacherous.

            stats:
              resistance 6

            timers:
              draw 1
              * Exhausting Climb [counter Pace yourself]: condition exhausted every 3

            openings:
              * Step: free_progress_small

            failure:
              You collapse.
            """;
        var result = TacticalParser.Parse(source);
        Assert.True(result.IsSuccess, string.Join("; ", result.Errors));

        var timer = result.Encounter!.Timers[0];
        Assert.Equal("Exhausting Climb", timer.Name);
        Assert.Equal("Pace yourself", timer.CounterName);
        Assert.Equal(TimerEffect.Condition, timer.Effect);
        Assert.Equal("exhausted", timer.ConditionId);
        Assert.Equal(3, timer.Countdown);
    }

    [Fact]
    public void MixedTimerTypesParse()
    {
        var source = """
            Mixed Fight
            [variant combat]

            Danger everywhere.

            stats:
              resistance 10

            timers:
              draw 2
              * Drain: spirits 2 every 4
              * Wound Risk: condition injured every 3
              * Regen: resistance 1 every 5

            openings:
              * Strike: momentum_to_progress_large

            failure:
              You lose.
            """;
        var result = TacticalParser.Parse(source);
        Assert.True(result.IsSuccess, string.Join("; ", result.Errors));
        Assert.Equal(3, result.Encounter!.Timers.Count);
        Assert.Equal(TimerEffect.Spirits, result.Encounter.Timers[0].Effect);
        Assert.Equal(TimerEffect.Condition, result.Encounter.Timers[1].Effect);
        Assert.Equal(TimerEffect.Resistance, result.Encounter.Timers[2].Effect);
    }

    [Fact]
    public void UnknownConditionErrors()
    {
        var source = """
            Bad Timer
            [variant combat]

            Test.

            stats:
              resistance 8

            timers:
              draw 1
              * Bad: condition cursed_by_the_moon every 3

            openings:
              * Strike: momentum_to_progress_large

            failure:
              You fail.
            """;
        var result = TacticalParser.Parse(source);
        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Message.Contains("Unknown condition"));
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

            openings:
              * Strike: momentum_to_progress
              * Guard: free_momentum_small

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
