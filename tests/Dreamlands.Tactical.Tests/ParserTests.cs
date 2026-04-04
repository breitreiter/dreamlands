using Xunit;
using Dreamlands.Tactical;

namespace Dreamlands.Tactical.Tests;

public class ParserTests
{
    const string CombatEncounter = """
        Wolves of the Cairn Road
        [stat combat]
        [tier 2]

        Three wolves materialize from the scrub grass on either side of the road.
        The largest has a scar across its muzzle.

        clock:
          10

        challenges:
          * Flanking Maneuver [counter Break their flank]: 4
          * Alpha's Howl [counter Silence the alpha]: 5
          * Cornered Prey: 3

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
        [stat bushcraft]
        [tier 1]

        The bridge across the gorge has collapsed to a skeleton of stone pillars
        and dangling rope.

        clock:
          8

        challenges:
          * Crumbling Pillar: 4
          * Wind Gust: 4

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
    public void CombatClock()
    {
        var enc = TacticalParser.Parse(CombatEncounter).Encounter!;
        Assert.Equal(10, enc.Clock);
    }

    [Fact]
    public void CombatChallenges()
    {
        var enc = TacticalParser.Parse(CombatEncounter).Encounter!;
        Assert.Equal(3, enc.Challenges.Count);

        var flanking = enc.Challenges[0];
        Assert.Equal("Flanking Maneuver", flanking.Name);
        Assert.Equal("Break their flank", flanking.CounterName);
        Assert.Equal(4, flanking.Resistance);

        var howl = enc.Challenges[1];
        Assert.Equal("Alpha's Howl", howl.Name);
        Assert.Equal("Silence the alpha", howl.CounterName);
        Assert.Equal(5, howl.Resistance);

        var prey = enc.Challenges[2];
        Assert.Equal("Cornered Prey", prey.Name);
        Assert.Null(prey.CounterName);
        Assert.Equal(3, prey.Resistance);
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
    public void TraverseChallenges()
    {
        var enc = TacticalParser.Parse(TraverseEncounter).Encounter!;
        Assert.Equal(2, enc.Challenges.Count);
        Assert.Equal(4, enc.Challenges[0].Resistance);
        Assert.Equal(4, enc.Challenges[1].Resistance);
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
    public void ClockParsed()
    {
        var source = """
            Test Encounter
            [stat combat]

            Body text.

            clock:
              12

            challenges:
              * Timer A: 6

            openings:
              * Strike: momentum_to_progress_large

            failure:
              You fail.
            """;
        var result = TacticalParser.Parse(source);
        Assert.True(result.IsSuccess, string.Join("; ", result.Errors));
        Assert.Equal(12, result.Encounter!.Clock);
        Assert.Single(result.Encounter.Challenges);
        Assert.Equal(6, result.Encounter.Challenges[0].Resistance);
    }

    [Fact]
    public void MixedGroupAndEncounterErrors()
    {
        var source = """
            Test
            [stat combat]

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
            [stat combat]

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
            [stat combat]
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

    // ── Challenges ────────────────────────────────────────────────

    [Fact]
    public void ChallengeWithCounterParses()
    {
        var source = """
            Treacherous Path
            [stat bushcraft]

            The path is treacherous.

            clock:
              8

            challenges:
              * Exhausting Climb [counter Pace yourself]: 6

            openings:
              * Step: free_progress_small

            failure:
              You collapse.
            """;
        var result = TacticalParser.Parse(source);
        Assert.True(result.IsSuccess, string.Join("; ", result.Errors));

        var challenge = result.Encounter!.Challenges[0];
        Assert.Equal("Exhausting Climb", challenge.Name);
        Assert.Equal("Pace yourself", challenge.CounterName);
        Assert.Equal(6, challenge.Resistance);
    }

    [Fact]
    public void MultipleChallengesParse()
    {
        var source = """
            Mixed Fight
            [stat combat]

            Danger everywhere.

            clock:
              12

            challenges:
              * First wave: 5
              * Second push [counter Break through]: 7
              * Final stand: 10

            openings:
              * Strike: momentum_to_progress_large

            failure:
              You lose.
            """;
        var result = TacticalParser.Parse(source);
        Assert.True(result.IsSuccess, string.Join("; ", result.Errors));
        Assert.Equal(3, result.Encounter!.Challenges.Count);
        Assert.Equal(5, result.Encounter.Challenges[0].Resistance);
        Assert.Equal(7, result.Encounter.Challenges[1].Resistance);
        Assert.Equal(10, result.Encounter.Challenges[2].Resistance);
    }

    [Fact]
    public void NoChallengesIsValid()
    {
        var source = """
            Simple Fight
            [stat combat]

            A straightforward encounter with no challenges.

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
        Assert.Empty(result.Encounter!.Challenges);
    }

    // ── Legacy section ignored ───────────────────────────────────

    [Fact]
    public void TimersSectionIsIgnored()
    {
        var source = """
            Old Format
            [tier 1]

            Body.

            timers:
              * Threat: spirits 1 every 3 resist 5

            openings:
              * Strike: momentum_to_progress

            failure:
              You fail.
            """;
        var result = TacticalParser.Parse(source);
        Assert.True(result.IsSuccess, string.Join("; ", result.Errors));
    }

    [Fact]
    public void PathSectionIsIgnored()
    {
        var source = """
            Old Format
            [tier 1]

            Body.

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
