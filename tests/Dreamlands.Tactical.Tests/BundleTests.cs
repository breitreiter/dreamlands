using Xunit;
using Dreamlands.Tactical;

namespace Dreamlands.Tactical.Tests;

public class BundleTests
{
    // Minimal valid bundle JSON matching the serialization format from TacticalBundleCommand
    const string BundleJson = """
        {
          "index": {
            "encountersById": { "plains/tier1/Wolves": 0 },
            "groupsById": { "plains/tier2/Ambush": 0 },
            "encountersByCategory": { "plains/tier1": [0] }
          },
          "encounters": [
            {
              "id": "plains/tier1/Wolves",
              "category": "plains/tier1",
              "title": "Wolves",
              "body": "Three wolves.",
              "variant": "combat",
              "intent": "violence",
              "tier": 1,
              "requires": [],
              "resistance": 8,
              "momentum": 3,
              "queueDepth": null,
              "timerDraw": 1,
              "timers": [
                { "name": "Flanking", "effect": "spirits", "amount": 2, "countdown": 4 }
              ],
              "openings": [
                { "name": "Lunge", "costKind": "momentum", "costAmount": 2, "effectKind": "damage", "effectAmount": 3, "requires": null },
                { "name": "Break", "costKind": "tick", "costAmount": 0, "effectKind": "stop_timer", "effectAmount": 0, "requires": null },
                { "name": "Guard", "costKind": "free", "costAmount": 0, "effectKind": "momentum", "effectAmount": 2, "requires": null },
                { "name": "Trap", "costKind": "spirits", "costAmount": 1, "effectKind": "damage", "effectAmount": 5, "requires": "has bear_trap" }
              ],
              "approaches": [
                { "kind": "scout", "momentum": 0, "timerCount": 1, "bonusOpenings": 3 },
                { "kind": "direct", "momentum": 3, "timerCount": 1, "bonusOpenings": 0 }
              ],
              "failure": { "text": "You lose.", "mechanics": ["damage_spirits 2"] }
            }
          ],
          "groups": [
            {
              "id": "plains/tier2/Ambush",
              "category": "plains/tier2",
              "title": "Ambush",
              "body": "Bandits ahead.",
              "tier": 2,
              "requires": [],
              "branches": [
                { "label": "Fight", "intent": "violence", "encounterRef": "wolves", "requires": null },
                { "label": "Sneak", "intent": "stealth", "encounterRef": "bypass", "requires": "has light_armor" }
              ]
            }
          ]
        }
        """;

    [Fact]
    public void LoadsEncounter()
    {
        var bundle = TacticalBundle.FromJson(BundleJson);
        Assert.Single(bundle.Encounters);

        var enc = bundle.Encounters[0];
        Assert.Equal("Wolves", enc.Title);
        Assert.Equal(Variant.Combat, enc.Variant);
        Assert.Equal(8, enc.Resistance);
        Assert.Equal(3, enc.Momentum);
    }

    [Fact]
    public void LoadsTimers()
    {
        var enc = TacticalBundle.FromJson(BundleJson).Encounters[0];
        Assert.Single(enc.Timers);
        Assert.Equal("Flanking", enc.Timers[0].Name);
        Assert.Equal(TimerEffect.Spirits, enc.Timers[0].Effect);
        Assert.Equal(2, enc.Timers[0].Amount);
        Assert.Equal(4, enc.Timers[0].Countdown);
    }

    [Fact]
    public void LoadsOpeningsWithAllCostKinds()
    {
        var enc = TacticalBundle.FromJson(BundleJson).Encounters[0];
        Assert.Equal(4, enc.Openings.Count);

        Assert.Equal(CostKind.Momentum, enc.Openings[0].Cost.Kind);
        Assert.Equal(2, enc.Openings[0].Cost.Amount);
        Assert.Equal(EffectKind.Damage, enc.Openings[0].Effect.Kind);

        Assert.Equal(CostKind.Tick, enc.Openings[1].Cost.Kind);
        Assert.Equal(EffectKind.StopTimer, enc.Openings[1].Effect.Kind);

        Assert.Equal(CostKind.Free, enc.Openings[2].Cost.Kind);
        Assert.Equal(EffectKind.Momentum, enc.Openings[2].Effect.Kind);

        Assert.Equal(CostKind.Spirits, enc.Openings[3].Cost.Kind);
        Assert.Equal("has bear_trap", enc.Openings[3].Requires);
    }

    [Fact]
    public void LoadsApproaches()
    {
        var enc = TacticalBundle.FromJson(BundleJson).Encounters[0];
        Assert.Equal(2, enc.Approaches.Count);
        Assert.Equal(ApproachKind.Scout, enc.Approaches[0].Kind);
        Assert.Equal(3, enc.Approaches[0].BonusOpenings);
        Assert.Equal(ApproachKind.Direct, enc.Approaches[1].Kind);
    }

    [Fact]
    public void LoadsFailure()
    {
        var enc = TacticalBundle.FromJson(BundleJson).Encounters[0];
        Assert.NotNull(enc.Failure);
        Assert.Equal("You lose.", enc.Failure.Text);
        Assert.Single(enc.Failure.Mechanics);
        Assert.Equal("damage_spirits 2", enc.Failure.Mechanics[0]);
    }

    [Fact]
    public void LoadsGroup()
    {
        var bundle = TacticalBundle.FromJson(BundleJson);
        Assert.Single(bundle.Groups);

        var grp = bundle.Groups[0];
        Assert.Equal("Ambush", grp.Title);
        Assert.Equal(2, grp.Tier);
        Assert.Equal(2, grp.Branches.Count);
        Assert.Equal("Fight", grp.Branches[0].Label);
        Assert.Equal("violence", grp.Branches[0].Intent);
        Assert.Equal("wolves", grp.Branches[0].EncounterRef);
        Assert.Null(grp.Branches[0].Requires);
        Assert.Equal("has light_armor", grp.Branches[1].Requires);
    }

    [Fact]
    public void IndexLookups()
    {
        var bundle = TacticalBundle.FromJson(BundleJson);

        Assert.NotNull(bundle.GetEncounterById("plains/tier1/Wolves"));
        Assert.Null(bundle.GetEncounterById("nonexistent"));

        Assert.NotNull(bundle.GetGroupById("plains/tier2/Ambush"));
        Assert.Null(bundle.GetGroupById("nonexistent"));

        var byCategory = bundle.GetByCategory("plains/tier1");
        Assert.Single(byCategory);
        Assert.Equal("Wolves", byCategory[0].Title);

        Assert.Empty(bundle.GetByCategory("nonexistent"));
    }
}
