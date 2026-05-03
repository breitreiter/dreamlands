using Dreamlands.Encounter;
using System.IO;

namespace Dreamlands.Combat.Tests;

public class CmbParserTests
{
    const string SampleEncounter = """
        +title Test Goblin
        +image foo/bar.webp
        +blood #7a0a0a
        +stats hp=18

        +move Attack
          narration: It lunges with a stick.

        +move Heavy Telegraphed Slow Attack
          narration: It hauls the maul over its head.
          narration: It bellows and winds up the strike.

        +move Defend
          narration: It hunches behind its shield.

        +intro
        A goblin steps from the brush.

        +win
        The goblin slumps.
        > gold 8
        > tag killed_goblin

        +lose
        Everything goes black.
        """;

    [Fact]
    public void Parses_top_level_directives()
    {
        var enc = CmbParser.ParseString(SampleEncounter);
        Assert.Equal("Test Goblin", enc.Title);
        Assert.Equal("foo/bar.webp", enc.Image);
        Assert.Equal("#7a0a0a", enc.BloodColor);
        Assert.Equal(18, enc.Stats.Hp);
    }

    [Fact]
    public void Parses_move_block_with_mutators_and_narration_variants()
    {
        var enc = CmbParser.ParseString(SampleEncounter);
        Assert.Equal(3, enc.Moves.Count);

        var heavy = enc.Moves[1];
        Assert.Equal("attack", heavy.Action.Base);
        Assert.Contains("heavy", heavy.Action.Mutators);
        Assert.Contains("slow", heavy.Action.Mutators);
        Assert.Contains("telegraphed", heavy.Action.Mutators);
        Assert.Equal(2, heavy.NarrationVariants.Count);

        var def = enc.Moves[2];
        Assert.Equal("defend", def.Action.Base);
        Assert.Empty(def.Action.Mutators);
        Assert.Single(def.NarrationVariants);
    }

    [Fact]
    public void Parses_intro_win_lose_blocks()
    {
        var enc = CmbParser.ParseString(SampleEncounter);
        Assert.Contains("goblin", enc.Intro);
        Assert.Contains("slumps", enc.WinText);
        Assert.Contains("gold 8", enc.WinMechanics);
        Assert.Contains("tag killed_goblin", enc.WinMechanics);
        Assert.Contains("black", enc.LoseText);
    }

    [Fact]
    public void Rejects_unknown_directive()
    {
        var bad = """
            +title Foo
            +stats hp=10
            +bogus thing
            """;
        Assert.Throws<FormatException>(() => CmbParser.ParseString(bad));
    }

    [Fact]
    public void Rejects_unknown_mutator()
    {
        var bad = """
            +title Foo
            +stats hp=10

            +move Sparkly Attack
              narration: shimmers.
            """;
        Assert.Throws<FormatException>(() => CmbParser.ParseString(bad));
    }

    [Fact]
    public void Rejects_unknown_base()
    {
        var bad = """
            +title Foo
            +stats hp=10

            +move Sneeze
              narration: gesundheit.
            """;
        Assert.Throws<FormatException>(() => CmbParser.ParseString(bad));
    }

    [Fact]
    public void Loads_on_disk_monsters_directory()
    {
        // Smoke test that the bundled .fight files in tools/combat-prototype/Monsters
        // parse as the new format. Walks up from the test bin dir to the repo root.
        var dir = AppContext.BaseDirectory;
        while (dir != null && !File.Exists(Path.Combine(dir, "Dreamlands.sln")))
            dir = Path.GetDirectoryName(dir);
        Assert.NotNull(dir);
        var monsters = Path.Combine(dir!, "tools", "combat-prototype", "Monsters");
        if (!Directory.Exists(monsters)) return; // skip if not present

        var bundle = CombatBundle.LoadDirectory(monsters);
        Assert.NotEmpty(bundle.Encounters);
        foreach (var e in bundle.Encounters)
        {
            Assert.True(e.Stats.Hp > 0, $"{e.Id} has non-positive HP");
            Assert.NotEmpty(e.Moves);
        }
    }

    [Fact]
    public void Requires_narration_on_move()
    {
        var bad = """
            +title Foo
            +stats hp=10

            +move Attack

            +intro
            test
            """;
        Assert.Throws<FormatException>(() => CmbParser.ParseString(bad));
    }
}
