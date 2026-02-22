using Dreamlands.Encounter;

namespace Dreamlands.Encounter.Tests;

public class BundleTests
{
    static readonly string TestBundleJson = """
    {
        "index": {
            "byId": {
                "well_encounter": { "category": "forest/tier1", "encounterIndex": 0 },
                "cave_encounter": { "category": "forest/tier1", "encounterIndex": 1 },
                "peak_encounter": { "category": "mountain/tier2", "encounterIndex": 2 }
            },
            "byCategory": {
                "forest/tier1": ["well_encounter", "cave_encounter"],
                "mountain/tier2": ["peak_encounter"]
            }
        },
        "encounters": [
            {
                "id": "well_encounter",
                "category": "forest/tier1",
                "title": "The Old Well",
                "body": "A crumbling well.",
                "choices": [
                    {
                        "optionText": "Look down",
                        "optionLink": null,
                        "optionPreview": null,
                        "requires": null,
                        "conditional": null,
                        "single": { "text": "You see darkness.", "mechanics": [] }
                    }
                ]
            },
            {
                "id": "cave_encounter",
                "category": "forest/tier1",
                "title": "Hidden Cave",
                "body": "A dark cave entrance.",
                "choices": []
            },
            {
                "id": "peak_encounter",
                "category": "mountain/tier2",
                "title": "The Summit",
                "body": "Wind howls.",
                "choices": []
            }
        ]
    }
    """;

    [Fact]
    public void FromJson_LoadsEncounters()
    {
        var bundle = EncounterBundle.FromJson(TestBundleJson);
        Assert.Equal(3, bundle.Encounters.Count);
    }

    [Fact]
    public void GetById_ReturnsCorrectEncounter()
    {
        var bundle = EncounterBundle.FromJson(TestBundleJson);
        var enc = bundle.GetById("well_encounter");

        Assert.NotNull(enc);
        Assert.Equal("The Old Well", enc!.Title);
        Assert.Equal("forest/tier1", enc.Category);
        Assert.Single(enc.Choices);
    }

    [Fact]
    public void GetById_ReturnsNull_ForUnknownId()
    {
        var bundle = EncounterBundle.FromJson(TestBundleJson);
        Assert.Null(bundle.GetById("nonexistent"));
    }

    [Fact]
    public void GetByCategory_ReturnsCorrectGroup()
    {
        var bundle = EncounterBundle.FromJson(TestBundleJson);
        var forest = bundle.GetByCategory("forest/tier1");

        Assert.Equal(2, forest.Count);
        Assert.Contains(forest, e => e.Id == "well_encounter");
        Assert.Contains(forest, e => e.Id == "cave_encounter");
    }

    [Fact]
    public void GetByCategory_ReturnsEmpty_ForUnknownCategory()
    {
        var bundle = EncounterBundle.FromJson(TestBundleJson);
        var result = bundle.GetByCategory("swamp/tier3");
        Assert.Empty(result);
    }

    [Fact]
    public void GetCategories_ReturnsAllCategories()
    {
        var bundle = EncounterBundle.FromJson(TestBundleJson);
        var categories = bundle.GetCategories();

        Assert.Equal(2, categories.Count);
        Assert.Contains("forest/tier1", categories);
        Assert.Contains("mountain/tier2", categories);
    }

    [Fact]
    public void FromJson_ParsesChoicesWithConditional()
    {
        var json = """
        {
            "index": {
                "byId": { "test": { "category": "test", "encounterIndex": 0 } },
                "byCategory": { "test": ["test"] }
            },
            "encounters": [{
                "id": "test",
                "category": "test",
                "title": "Test",
                "body": "Body.",
                "choices": [{
                    "optionText": "Fight",
                    "optionLink": null,
                    "optionPreview": null,
                    "requires": null,
                    "conditional": {
                        "preamble": "You draw your sword.",
                        "branches": [
                            { "condition": "check combat hard", "text": "Victory!", "mechanics": ["give_gold large"] }
                        ],
                        "fallback": { "text": "Defeat.", "mechanics": ["damage_health small"] }
                    },
                    "single": null
                }]
            }]
        }
        """;

        var bundle = EncounterBundle.FromJson(json);
        var choice = bundle.GetById("test")!.Choices[0];

        Assert.NotNull(choice.Conditional);
        Assert.Equal("You draw your sword.", choice.Conditional!.Preamble);
        Assert.Single(choice.Conditional.Branches);
        Assert.Equal("check combat hard", choice.Conditional.Branches[0].Condition);
        Assert.Equal("Victory!", choice.Conditional.Branches[0].Outcome.Text);
        Assert.NotNull(choice.Conditional.Fallback);
        Assert.Equal("Defeat.", choice.Conditional.Fallback!.Text);
    }
}
