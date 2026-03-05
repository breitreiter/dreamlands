using Dreamlands.Game;
using Dreamlands.Rules;

namespace Dreamlands.Game.Tests;

public class HaulGenerationTests
{
    static readonly IReadOnlyDictionary<string, HaulDef> TestHauls = new Dictionary<string, HaulDef>
    {
        ["plains_forest_1"] = new() { Id = "plains_forest_1", Name = "Grain Sack", OriginBiome = "plains", DestBiome = "forest", OriginFlavor = "Surplus harvest" },
        ["plains_forest_2"] = new() { Id = "plains_forest_2", Name = "Wool Bale", OriginBiome = "plains", DestBiome = "forest", OriginFlavor = "Sheared wool" },
        ["plains_mountains_1"] = new() { Id = "plains_mountains_1", Name = "Salt Barrel", OriginBiome = "plains", DestBiome = "mountains", OriginFlavor = "Mined salt" },
        ["forest_plains_1"] = new() { Id = "forest_plains_1", Name = "Timber Bundle", OriginBiome = "forest", DestBiome = "plains", OriginFlavor = "Fresh timber" },
    };

    static readonly List<HaulGeneration.HaulDestination> TwoCandidates =
    [
        new("dest1", "Woodhaven", Terrain.Forest, 20, 10),
        new("dest2", "Stonepeak", Terrain.Mountains, 30, 5),
    ];

    [Fact]
    public void NonLeaf_GeneratesTwo()
    {
        var result = HaulGeneration.Generate(
            10, 10, Terrain.Plains, isLeaf: false,
            TwoCandidates, TestHauls, 60, 60, [], new Random(42));

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void Leaf_GeneratesOne()
    {
        var result = HaulGeneration.Generate(
            10, 10, Terrain.Plains, isLeaf: true,
            TwoCandidates, TestHauls, 60, 60, [], new Random(42));

        Assert.Single(result);
    }

    [Fact]
    public void RespectsExistingOffers_NonLeaf()
    {
        var existing = new List<ItemInstance>
        {
            new("haul", "Existing") { HaulDefId = "plains_forest_1" }
        };

        var result = HaulGeneration.Generate(
            10, 10, Terrain.Plains, isLeaf: false,
            TwoCandidates, TestHauls, 60, 60, existing, new Random(42));

        Assert.Single(result);
    }

    [Fact]
    public void RespectsExistingOffers_AtCap_ReturnsEmpty()
    {
        var existing = new List<ItemInstance>
        {
            new("haul", "A") { HaulDefId = "plains_forest_1" },
            new("haul", "B") { HaulDefId = "plains_forest_2" }
        };

        var result = HaulGeneration.Generate(
            10, 10, Terrain.Plains, isLeaf: false,
            TwoCandidates, TestHauls, 60, 60, existing, new Random(42));

        Assert.Empty(result);
    }

    [Fact]
    public void Payout_IsManhattanTimesThree()
    {
        var candidates = new List<HaulGeneration.HaulDestination>
        {
            new("dest1", "Woodhaven", Terrain.Forest, 20, 15)
        };

        var result = HaulGeneration.Generate(
            10, 10, Terrain.Plains, isLeaf: true,
            candidates, TestHauls, 60, 60, [], new Random(42));

        Assert.Single(result);
        // manhattan = |10-20| + |10-15| = 15, payout = 15 * 3 = 45
        Assert.Equal(45, result[0].Payout);
    }

    [Fact]
    public void NoDuplicateHaulDefIds()
    {
        // Both candidates are forest, so both could match same defs
        var candidates = new List<HaulGeneration.HaulDestination>
        {
            new("dest1", "Woodhaven", Terrain.Forest, 20, 10),
            new("dest2", "Elmgrove", Terrain.Forest, 25, 15),
        };

        var result = HaulGeneration.Generate(
            10, 10, Terrain.Plains, isLeaf: false,
            candidates, TestHauls, 60, 60, [], new Random(42));

        Assert.Equal(2, result.Count);
        Assert.NotEqual(result[0].HaulDefId, result[1].HaulDefId);
    }

    [Fact]
    public void NoMatchingDefs_ReturnsEmpty()
    {
        var candidates = new List<HaulGeneration.HaulDestination>
        {
            new("dest1", "Bogtown", Terrain.Swamp, 20, 10) // no plains->swamp defs in test data
        };

        var result = HaulGeneration.Generate(
            10, 10, Terrain.Plains, isLeaf: false,
            candidates, TestHauls, 60, 60, [], new Random(42));

        Assert.Empty(result);
    }

    [Theory]
    [InlineData(5, 5, 60, 60, "northwest")]
    [InlineData(30, 30, 60, 60, "center")]
    [InlineData(55, 55, 60, 60, "southeast")]
    [InlineData(55, 5, 60, 60, "northeast")]
    [InlineData(5, 55, 60, 60, "southwest")]
    public void SectorHint_CorrectForCoordinates(int dx, int dy, int w, int h, string expectedSector)
    {
        var dest = new HaulGeneration.HaulDestination("test", "Test", Terrain.Forest, dx, dy);
        var hint = HaulGeneration.BuildHint(dest, w, h);
        Assert.Equal($"A forest settlement in the {expectedSector}", hint);
    }

    [Fact]
    public void GeneratedItems_HaveCorrectFields()
    {
        var candidates = new List<HaulGeneration.HaulDestination>
        {
            new("dest1", "Woodhaven", Terrain.Forest, 20, 10)
        };

        var result = HaulGeneration.Generate(
            10, 10, Terrain.Plains, isLeaf: true,
            candidates, TestHauls, 60, 60, [], new Random(42));

        Assert.Single(result);
        var item = result[0];
        Assert.Equal("haul", item.DefId);
        Assert.NotNull(item.HaulDefId);
        Assert.Equal("dest1", item.DestinationSettlementId);
        Assert.NotNull(item.DestinationHint);
        Assert.NotNull(item.Payout);
        Assert.NotNull(item.Description);
    }
}
