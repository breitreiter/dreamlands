using Dreamlands.Encounter;

namespace Dreamlands.Combat.Tests;

public class CmbParserTests
{
    const string Sample = """
        +title Test Beast
        +image monsters/test.png
        +repool false
        +stats hp=10 ac=12 to_hit=+3 damage=1d6

        +hitbox torso left=0.3 top=0.3 right=0.7 bottom=0.7

        +move basic_swing
          intent: attack
          preview: "It hefts its weapon."
          timer: 0
          sprite: swing
          anchor: center
          narration: "It swings."
          > deal_damage 1d6

        +move heavy
          intent: heavy_attack
          preview: "It rears back for a big strike."
          timer: 3
          sprite: heavy
          narration: "It crashes down."
          > deal_damage 2d6+1

        +intro
        You meet the test beast.

        +win
        > +give_gold 5
        > +add_tag killed_test
        It falls.

        +lose
        > +damage_spirits 100
        It stands over you.
        """;

    [Fact]
    public void Parses_basic_fields()
    {
        var enc = CmbParser.ParseString(Sample);
        Assert.Equal("Test Beast", enc.Title);
        Assert.Equal("monsters/test.png", enc.Image);
        Assert.False(enc.Repool);
        Assert.Equal(10, enc.Stats.Hp);
        Assert.Equal(12, enc.Stats.Ac);
        Assert.Equal(3, enc.Stats.ToHit);
        Assert.Equal(new DiceRoll(1, 6, 0), enc.Stats.Damage);
    }

    [Fact]
    public void Parses_hitboxes_and_moves()
    {
        var enc = CmbParser.ParseString(Sample);
        Assert.Single(enc.Hitboxes);
        Assert.Equal("torso", enc.Hitboxes[0].Id);
        Assert.Equal(2, enc.Moves.Count);

        var basic = enc.Moves[0];
        Assert.Equal("basic_swing", basic.Id);
        Assert.True(basic.IsBasic);
        Assert.Equal(IntentClass.Attack, basic.IntentClass);
        Assert.Equal("It hefts its weapon.", basic.IntentText);
        Assert.Single(basic.Mechanics);
        Assert.Equal("deal_damage", basic.Mechanics[0].Verb);
        Assert.Equal("1d6", basic.Mechanics[0].Args);

        var heavy = enc.Moves[1];
        Assert.Equal(3, heavy.Timer);
        Assert.False(heavy.IsBasic);
        Assert.Equal(IntentClass.HeavyAttack, heavy.IntentClass);
    }

    [Fact]
    public void Strips_plus_prefix_from_outcome_mechanics()
    {
        var enc = CmbParser.ParseString(Sample);
        Assert.Equal(new[] { "give_gold 5", "add_tag killed_test" }, enc.WinMechanics);
        Assert.Equal(new[] { "damage_spirits 100" }, enc.LoseMechanics);
        Assert.Contains("It falls.", enc.WinText);
        Assert.Contains("It stands over you.", enc.LoseText);
    }

    [Fact]
    public void Basic_move_required()
    {
        var enc = CmbParser.ParseString(Sample);
        var basic = enc.BasicMove;
        Assert.Equal("basic_swing", basic.Id);
    }

    [Fact]
    public void Missing_basic_throws_on_access()
    {
        var enc = CmbParser.ParseString("""
            +title NoBasic
            +stats hp=1 ac=10 to_hit=+0 damage=1d4
            +move only
              intent: attack
              timer: 2
              > deal_damage 1d4
            """);
        Assert.Throws<InvalidOperationException>(() => _ = enc.BasicMove);
    }

    [Fact]
    public void Dice_parser_handles_forms()
    {
        Assert.Equal(new DiceRoll(1, 8, 0), DiceParser.Parse("1d8"));
        Assert.Equal(new DiceRoll(2, 8, 2), DiceParser.Parse("2d8+2"));
        Assert.Equal(new DiceRoll(1, 4, -1), DiceParser.Parse("1d4-1"));
        Assert.Equal(new DiceRoll(0, 0, 4), DiceParser.Parse("+4"));
        Assert.Equal(new DiceRoll(0, 0, -2), DiceParser.Parse("-2"));
        Assert.Equal(new DiceRoll(0, 0, 5), DiceParser.Parse("5"));
    }
}
