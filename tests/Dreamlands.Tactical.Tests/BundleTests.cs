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
              "tier": 1,
              "requires": [],
              "timers": [
                { "name": "Flanking", "effect": "spirits", "amount": 2, "countdown": 4, "resistance": 8 }
              ],
              "openings": [
                { "name": "Lunge", "archetype": "momentum_to_progress_large", "requires": null },
                { "name": "Break", "archetype": "momentum_to_cancel", "requires": null },
                { "name": "Guard", "archetype": "free_momentum", "requires": null },
                { "name": "Trap", "archetype": "spirits_to_progress_large", "requires": "has bear_trap" }
              ],
              "approaches": [
                { "kind": "aggressive" },
                { "kind": "cautious" }
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
                { "label": "Fight", "encounterRef": "wolves", "requires": null },
                { "label": "Sneak", "encounterRef": "bypass", "requires": "has light_armor" }
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
        Assert.Equal(8, enc.Timers[0].Resistance);
    }

    [Fact]
    public void LoadsConditionTimer()
    {
        var json = """
            {
              "index": { "encountersById": { "t": 0 }, "groupsById": {}, "encountersByCategory": {} },
              "encounters": [
                {
                  "id": "t", "category": "", "title": "T", "body": ".", "variant": "combat",
                  "timers": [
                    { "name": "Jagged", "effect": "condition", "amount": 0, "countdown": 4, "resistance": 8, "conditionId": "injured" }
                  ],
                  "openings": [{ "name": "Hit", "archetype": "momentum_to_progress", "requires": null }],
                  "failure": { "text": "Fail.", "mechanics": [] }
                }
              ],
              "groups": []
            }
            """;
        var enc = TacticalBundle.FromJson(json).Encounters[0];
        Assert.Single(enc.Timers);
        Assert.Equal(TimerEffect.Condition, enc.Timers[0].Effect);
        Assert.Equal("injured", enc.Timers[0].ConditionId);
        Assert.Equal(4, enc.Timers[0].Countdown);
    }

    [Fact]
    public void LoadsOpeningsWithArchetypes()
    {
        var enc = TacticalBundle.FromJson(BundleJson).Encounters[0];
        Assert.Equal(4, enc.Openings.Count);

        Assert.Equal("Lunge", enc.Openings[0].Name);
        Assert.Equal("momentum_to_progress_large", enc.Openings[0].Archetype);

        Assert.Equal("Break", enc.Openings[1].Name);
        Assert.Equal("momentum_to_cancel", enc.Openings[1].Archetype);

        Assert.Equal("Guard", enc.Openings[2].Name);
        Assert.Equal("free_momentum", enc.Openings[2].Archetype);

        Assert.Equal("Trap", enc.Openings[3].Name);
        Assert.Equal("spirits_to_progress_large", enc.Openings[3].Archetype);
        Assert.Equal("has bear_trap", enc.Openings[3].Requires);
    }

    [Fact]
    public void LoadsApproaches()
    {
        var enc = TacticalBundle.FromJson(BundleJson).Encounters[0];
        Assert.Equal(2, enc.Approaches.Count);
        Assert.Equal(ApproachKind.Aggressive, enc.Approaches[0].Kind);
        Assert.Equal(ApproachKind.Cautious, enc.Approaches[1].Kind);
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
