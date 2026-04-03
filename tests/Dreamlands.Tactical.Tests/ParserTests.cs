using Xunit;
using Dreamlands.Tactical;

namespace Dreamlands.Tactical.Tests;

public class ParserTests
{
    const string CombatEncounter = """
        Wolves of the Cairn Road
        [variant combat]
        [tier 2]

        Three wolves materialize from the scrub grass on either side of the road.
        The largest has a scar across its muzzle.

        timers:
          * Flanking Maneuver: spirits 2 every 4 resist 4
          * Alpha's Howl: resistance 2 every 5 resist 5
          * Cornered Prey: spirits 1 every 3 resist 3

        openings:
          * Throat Strike: momentum_to_progress_large
          * Feint and Slash: momentum_to_progress
          * Defensive Stance: free_momentum
          * Break Away: momentum_to_cancel
          * Spring the Trap: spirits_to_progress_large [requires has bear_trap]

        approaches:
          * aggressive
          * cautious

        failure:
          The wolves drag you down. You stagger away bloodied.
          +damage_spirits 2
          +lose_random_item
        """;

    const string TraverseEncounter = """
        The Shattered Bridge
        [variant traverse]
        [tier 1]

        The bridge across the gorge has collapsed to a skeleton of stone pillars
        and dangling rope.

        timers:
          * Crumbling Pillar: resistance 1 every 3 resist 4
          * Wind Gust: spirits 1 every 2 resist 4

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
          * Fight through -> wolves_of_the_cairn_road
          * Sneak past -> cairn_road_bypass [requires has light_armor]
          * Parley -> cairn_road_parley
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
    public void CombatTimers()
    {
        var enc = TacticalParser.Parse(CombatEncounter).Encounter!;
        Assert.Equal(3, enc.Timers.Count);

        var flanking = enc.Timers[0];
        Assert.Equal("Flanking Maneuver", flanking.Name);
        Assert.Equal(TimerEffect.Spirits, flanking.Effect);
        Assert.Equal(2, flanking.Amount);
        Assert.Equal(4, flanking.Countdown);
        Assert.Equal(4, flanking.Resistance);

        var howl = enc.Timers[1];
        Assert.Equal("Alpha's Howl", howl.Name);
        Assert.Equal(TimerEffect.Resistance, howl.Effect);
        Assert.Equal(5, howl.Resistance);
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
        Assert.Equal(2, enc.Approaches.Count);

        Assert.Equal(ApproachKind.Aggressive, enc.Approaches[0].Kind);
        Assert.Equal(ApproachKind.Cautious, enc.Approaches[1].Kind);
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
        Assert.NotNull(result.Encounter);
    }

    [Fact]
    public void TraverseTimerResistance()
    {
        var enc = TacticalParser.Parse(TraverseEncounter).Encounter!;
        Assert.Equal(2, enc.Timers.Count);
        Assert.Equal(4, enc.Timers[0].Resistance);
        Assert.Equal(4, enc.Timers[1].Resistance);
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
        Assert.Equal("wolves_of_the_cairn_road", grp.Branches[0].EncounterRef);
        Assert.Null(grp.Branches[0].Requires);

        Assert.Equal("Sneak past", grp.Branches[1].Label);
        Assert.Equal("cairn_road_bypass", grp.Branches[1].EncounterRef);
        Assert.Equal("has light_armor", grp.Branches[1].Requires);

        Assert.Equal("Parley", grp.Branches[2].Label);
    }

    // ── Validation errors ──────────────────────────────────────────

    [Fact]
    public void EmptyFileErrors()
    {
        var result = TacticalParser.Parse("");
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void NoVariantIsValid()
    {
        var source = """
            Test Encounter

            Body text.

            openings:
              * Strike: momentum_to_progress_large

            failure:
              You fail.
            """;
        var result = TacticalParser.Parse(source);
        Assert.True(result.IsSuccess, string.Join("; ", result.Errors));
    }

    [Fact]
    public void TimerResistanceParsed()
    {
        var source = """
            Test Encounter
            [variant combat]

            Body text.

            timers:
              * Timer A: spirits 1 every 3 resist 6

            openings:
              * Strike: momentum_to_progress_large

            failure:
              You fail.
            """;
        var result = TacticalParser.Parse(source);
        Assert.True(result.IsSuccess, string.Join("; ", result.Errors));
        Assert.Equal(6, result.Encounter!.Timers[0].Resistance);
    }

    [Fact]
    public void MixedGroupAndEncounterErrors()
    {
        var source = """
            Test
            [variant combat]

            Body.

            openings:
              * Strike: momentum_to_progress_large

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

            timers:
              * Falling Rocks: condition injured every 4 resist 8

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

            timers:
              * Exhausting Climb [counter Pace yourself]: condition exhausted every 3 resist 6

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

            timers:
              * Drain: spirits 2 every 4 resist 5
              * Wound Risk: condition injured every 3 resist 5
              * Regen: resistance 1 every 5 resist 5

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

            timers:
              * Bad: condition cursed_by_the_moon every 3 resist 5

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

            openings:
              * Strike: momentum_to_progress
              * Guard: free_momentum_small

            approaches:
              * aggressive
              * cautious

            failure:
              You lose.
            """;
        var result = TacticalParser.Parse(source);
        Assert.True(result.IsSuccess, string.Join("; ", result.Errors));
        Assert.Empty(result.Encounter!.Timers);
    }

    // ── Fatal timers ──────────────────────────────────────────────────

    [Fact]
    public void FatalTimerParses()
    {
        var source = """
            Chase
            [tier 1]

            They're after you.

            timers:
              * They're gaining on you: fatal every 20

            openings:
              * Run: free_progress_small

            failure:
              They catch you.
            """;
        var result = TacticalParser.Parse(source);
        Assert.True(result.IsSuccess, string.Join("; ", result.Errors));

        var timer = result.Encounter!.Timers[0];
        Assert.Equal("They're gaining on you", timer.Name);
        Assert.Equal(TimerEffect.Fatal, timer.Effect);
        Assert.Equal(20, timer.Countdown);
        Assert.Equal(0, timer.Resistance); // ambient
    }

    [Fact]
    public void FatalTimerWithResistParses()
    {
        var source = """
            Bomb
            [tier 2]

            Tick tick tick.

            timers:
              * Ticking Bomb: fatal every 5 resist 4

            openings:
              * Defuse: momentum_to_progress

            failure:
              Boom.
            """;
        var result = TacticalParser.Parse(source);
        Assert.True(result.IsSuccess, string.Join("; ", result.Errors));

        var timer = result.Encounter!.Timers[0];
        Assert.Equal(TimerEffect.Fatal, timer.Effect);
        Assert.Equal(5, timer.Countdown);
        Assert.Equal(4, timer.Resistance); // sequential — can be defused
    }

    // ── Tick-timer ────────────────────────────────────────────────────

    [Fact]
    public void TickTimerParses()
    {
        var source = """
            Traverse
            [tier 1]

            Navigate the hazards.

            timers:
              * Master: fatal every 20
              * Reach the creek: tick "Master" 3 every 4 resist 6

            openings:
              * Step: free_progress_small

            failure:
              Lost.
            """;
        var result = TacticalParser.Parse(source);
        Assert.True(result.IsSuccess, string.Join("; ", result.Errors));

        Assert.Equal(2, result.Encounter!.Timers.Count);

        var master = result.Encounter.Timers[0];
        Assert.Equal(TimerEffect.Fatal, master.Effect);
        Assert.Equal(0, master.Resistance);

        var creek = result.Encounter.Timers[1];
        Assert.Equal("Reach the creek", creek.Name);
        Assert.Equal(TimerEffect.TickTimer, creek.Effect);
        Assert.Equal(3, creek.Amount);
        Assert.Equal(4, creek.Countdown);
        Assert.Equal(6, creek.Resistance);
        Assert.Equal("Master", creek.TicksTimerName);
    }

    [Fact]
    public void TickTimerWithCounterParses()
    {
        var source = """
            Test
            [tier 1]

            Body.

            timers:
              * Master: fatal every 20
              * Climb [counter Find handholds]: tick "Master" 2 every 3 resist 5

            openings:
              * Step: free_progress_small

            failure:
              Fall.
            """;
        var result = TacticalParser.Parse(source);
        Assert.True(result.IsSuccess, string.Join("; ", result.Errors));

        var timer = result.Encounter!.Timers[1];
        Assert.Equal("Climb", timer.Name);
        Assert.Equal("Find handholds", timer.CounterName);
        Assert.Equal("Master", timer.TicksTimerName);
    }

    // ── Ambient timers (resist omitted) ───────────────────────────────

    [Fact]
    public void TimerWithoutResistIsAmbient()
    {
        var source = """
            Test
            [tier 1]

            Body.

            timers:
              * Constant Drain: spirits 1 every 3

            openings:
              * Strike: momentum_to_progress

            failure:
              You fail.
            """;
        var result = TacticalParser.Parse(source);
        Assert.True(result.IsSuccess, string.Join("; ", result.Errors));

        var timer = result.Encounter!.Timers[0];
        Assert.Equal(0, timer.Resistance);
        Assert.Equal(TimerEffect.Spirits, timer.Effect);
        Assert.Equal(3, timer.Countdown);
    }

    // ── Path section ignored ──────────────────────────────────────────

    [Fact]
    public void PathSectionIsIgnored()
    {
        var source = """
            Old Format
            [tier 1]

            Body.

            timers:
              * Threat: spirits 1 every 3 resist 5

            openings:
              * Strike: momentum_to_progress

            path:
              * Step: free_progress_small

            failure:
              You fail.
            """;
        var result = TacticalParser.Parse(source);
        Assert.True(result.IsSuccess, string.Join("; ", result.Errors));
    }
}
