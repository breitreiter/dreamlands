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
        ["generic_parcel"] = new() { Id = "generic_parcel", Name = "Unmarked Parcel", OriginBiome = "", DestBiome = "", IsGeneric = true, OriginFlavor = "A parcel" },
    };

    static readonly List<HaulGeneration.HaulDestination> TwoCandidates =
    [
        new("dest1", "Woodhaven", Terrain.Forest, 20, 10, Depth: 3),
        new("dest2", "Stonepeak", Terrain.Mountains, 30, 5, Depth: 5),
    ];

    [Fact]
    public void NonLeaf_GeneratesTwo()
    {
        var result = HaulGeneration.Generate(
            10, 10, "Aldgate", Terrain.Plains, isLeaf: false,
            TwoCandidates, TestHauls, [], [], new Random(42));

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void Leaf_GeneratesOne()
    {
        var result = HaulGeneration.Generate(
            10, 10, "Aldgate", Terrain.Plains, isLeaf: true,
            TwoCandidates, TestHauls, [], [], new Random(42));

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
            10, 10, "Aldgate", Terrain.Plains, isLeaf: false,
            TwoCandidates, TestHauls, existing, [], new Random(42));

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
            10, 10, "Aldgate", Terrain.Plains, isLeaf: false,
            TwoCandidates, TestHauls, existing, [], new Random(42));

        Assert.Empty(result);
    }

    [Fact]
    public void Payout_BaseDistanceDepthExploration()
    {
        var candidates = new List<HaulGeneration.HaulDestination>
        {
            new("dest1", "Woodhaven", Terrain.Forest, 20, 15, Depth: 8, IsVisited: false)
        };

        var result = HaulGeneration.Generate(
            10, 10, "Aldgate", Terrain.Plains, isLeaf: true,
            candidates, TestHauls, [], [], new Random(42));

        Assert.Single(result);
        // base=5, manhattan=15 × 2=30, depth=8 × 1=8, exploration=8 → 51
        Assert.Equal(51, result[0].Payout);
    }

    [Fact]
    public void Payout_VisitedDestination_NoExplorationBonus()
    {
        var candidates = new List<HaulGeneration.HaulDestination>
        {
            new("dest1", "Woodhaven", Terrain.Forest, 20, 15, Depth: 8, IsVisited: true)
        };

        var result = HaulGeneration.Generate(
            10, 10, "Aldgate", Terrain.Plains, isLeaf: true,
            candidates, TestHauls, [], [], new Random(42));

        Assert.Single(result);
        // base=5, manhattan=15 × 2=30, depth=8 × 1=8, no exploration → 43
        Assert.Equal(43, result[0].Payout);
    }

    [Fact]
    public void Payout_ShallowDestination_LowDepthBonus()
    {
        var candidates = new List<HaulGeneration.HaulDestination>
        {
            new("dest1", "Aldgate", Terrain.Plains, 20, 15, Depth: 0, IsVisited: true)
        };

        var result = HaulGeneration.Generate(
            10, 10, "Riverton", Terrain.Plains, isLeaf: true,
            candidates, TestHauls, [], [], new Random(42));

        Assert.Single(result);
        // base=5, manhattan=15 × 2=30, depth=0, no exploration → 35
        Assert.Equal(35, result[0].Payout);
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
            10, 10, "Aldgate", Terrain.Plains, isLeaf: false,
            candidates, TestHauls, [], [], new Random(42));

        Assert.Equal(2, result.Count);
        Assert.NotEqual(result[0].HaulDefId, result[1].HaulDefId);
    }

    [Fact]
    public void NonLeaf_AtMostOneBespoke()
    {
        var hauls = new Dictionary<string, HaulDef>
        {
            ["bespoke_1"] = new() { Id = "bespoke_1", Name = "Fine Grain", OriginBiome = "plains", DestBiome = "forest", OriginFlavor = "Grain" },
            ["bespoke_2"] = new() { Id = "bespoke_2", Name = "Rough Wool", OriginBiome = "plains", DestBiome = "forest", OriginFlavor = "Wool" },
            ["generic_crate"] = new() { Id = "generic_crate", Name = "Sealed Crate", OriginBiome = "", DestBiome = "", IsGeneric = true, OriginFlavor = "A crate" },
        };
        var candidates = new List<HaulGeneration.HaulDestination>
        {
            new("dest1", "Woodhaven", Terrain.Forest, 20, 10),
            new("dest2", "Elmgrove", Terrain.Forest, 25, 15),
        };

        var result = HaulGeneration.Generate(
            10, 10, "Aldgate", Terrain.Plains, isLeaf: false,
            candidates, hauls, [], [], new Random(42));

        Assert.Equal(2, result.Count);
        Assert.Single(result, r => !r.IsGeneric);
        Assert.Single(result, r => r.IsGeneric);
    }

    [Fact]
    public void ExistingBespoke_ForcesGenericOnly()
    {
        var hauls = new Dictionary<string, HaulDef>
        {
            ["bespoke_1"] = new() { Id = "bespoke_1", Name = "Fine Grain", OriginBiome = "plains", DestBiome = "forest", OriginFlavor = "Grain" },
            ["generic_crate"] = new() { Id = "generic_crate", Name = "Sealed Crate", OriginBiome = "", DestBiome = "", IsGeneric = true, OriginFlavor = "A crate" },
        };
        var candidates = new List<HaulGeneration.HaulDestination>
        {
            new("dest1", "Woodhaven", Terrain.Forest, 20, 10),
        };
        var existing = new List<ItemInstance>
        {
            new("haul", "Existing Bespoke") { HaulDefId = "bespoke_1" }
        };

        var result = HaulGeneration.Generate(
            10, 10, "Aldgate", Terrain.Plains, isLeaf: false,
            candidates, hauls, existing, [], new Random(42));

        Assert.Single(result);
        Assert.True(result[0].IsGeneric);
    }

    [Fact]
    public void NoMatchingDefs_NoGenerics_ReturnsEmpty()
    {
        var bespokeOnly = new Dictionary<string, HaulDef>
        {
            ["forest_plains_1"] = new() { Id = "forest_plains_1", Name = "Timber Bundle", OriginBiome = "forest", DestBiome = "plains", OriginFlavor = "Fresh timber" },
        };
        var candidates = new List<HaulGeneration.HaulDestination>
        {
            new("dest1", "Bogtown", Terrain.Swamp, 20, 10) // no plains->swamp defs and no generics
        };

        var result = HaulGeneration.Generate(
            10, 10, "Aldgate", Terrain.Plains, isLeaf: false,
            candidates, bespokeOnly, [], [], new Random(42));

        Assert.Empty(result);
    }

    [Fact]
    public void BespokePreferredOverGeneric()
    {
        var hauls = new Dictionary<string, HaulDef>
        {
            ["bespoke"] = new() { Id = "bespoke", Name = "Bespoke Grain", OriginBiome = "plains", DestBiome = "forest", OriginFlavor = "Fine grain" },
            ["generic_crate"] = new() { Id = "generic_crate", Name = "Sealed Crate", OriginBiome = "", DestBiome = "", IsGeneric = true, OriginFlavor = "A crate" },
        };
        var candidates = new List<HaulGeneration.HaulDestination>
        {
            new("dest1", "Woodhaven", Terrain.Forest, 20, 10),
        };

        var result = HaulGeneration.Generate(
            10, 10, "Aldgate", Terrain.Plains, isLeaf: true,
            candidates, hauls, [], [], new Random(42));

        Assert.Single(result);
        Assert.Equal("bespoke", result[0].HaulDefId);
        Assert.False(result[0].IsGeneric);
    }

    [Fact]
    public void GenericHaul_WhenNoBespokeMatch()
    {
        var hauls = new Dictionary<string, HaulDef>
        {
            ["bespoke"] = new() { Id = "bespoke", Name = "Bespoke Grain", OriginBiome = "plains", DestBiome = "forest", OriginFlavor = "Fine grain" },
            ["generic_crate"] = new() { Id = "generic_crate", Name = "Sealed Crate", OriginBiome = "", DestBiome = "", IsGeneric = true, OriginFlavor = "A crate" },
        };
        var candidates = new List<HaulGeneration.HaulDestination>
        {
            new("dest1", "Bogtown", Terrain.Swamp, 20, 10), // no bespoke plains->swamp
        };

        var result = HaulGeneration.Generate(
            10, 10, "Aldgate", Terrain.Plains, isLeaf: true,
            candidates, hauls, [], [], new Random(42));

        Assert.Single(result);
        Assert.Equal("generic_crate", result[0].HaulDefId);
        Assert.True(result[0].IsGeneric);
    }

    [Fact]
    public void GenericHauls_ExcludedFromReGeneration()
    {
        var hauls = new Dictionary<string, HaulDef>
        {
            ["generic_crate"] = new() { Id = "generic_crate", Name = "Sealed Crate", OriginBiome = "", DestBiome = "", IsGeneric = true, OriginFlavor = "A crate" },
        };
        var candidates = new List<HaulGeneration.HaulDestination>
        {
            new("dest1", "Bogtown", Terrain.Swamp, 20, 10),
        };
        var existing = new List<ItemInstance>
        {
            new("haul", "Sealed Crate") { HaulDefId = "generic_crate" }
        };

        var result = HaulGeneration.Generate(
            10, 10, "Aldgate", Terrain.Plains, isLeaf: false,
            candidates, hauls, existing, [], new Random(42));

        Assert.Empty(result);
    }

    [Fact]
    public void GenericHaul_SetsIsGenericOnItemInstance()
    {
        var hauls = new Dictionary<string, HaulDef>
        {
            ["generic_crate"] = new() { Id = "generic_crate", Name = "Sealed Crate", OriginBiome = "", DestBiome = "", IsGeneric = true, OriginFlavor = "A crate" },
        };
        var candidates = new List<HaulGeneration.HaulDestination>
        {
            new("dest1", "Bogtown", Terrain.Swamp, 20, 10),
        };

        var result = HaulGeneration.Generate(
            10, 10, "Aldgate", Terrain.Plains, isLeaf: true,
            candidates, hauls, [], [], new Random(42));

        Assert.Single(result);
        Assert.True(result[0].IsGeneric);
        Assert.Equal("generic_crate", result[0].HaulDefId);
    }

    [Theory]
    [InlineData(10, 0, "north")]       // due north
    [InlineData(20, 0, "east")]        // due east
    [InlineData(10, 20, "south")]      // due south
    [InlineData(0, 10, "west")]        // due west
    [InlineData(18, 5, "northeast")]   // mostly east, slightly north
    [InlineData(20, 20, "southeast")]  // exact diagonal
    public void Hint_DirectionIsCorrect(int destX, int destY, string expectedDir)
    {
        var dest = new HaulGeneration.HaulDestination("test", "Fartown", Terrain.Forest, destX, destY);
        var hint = HaulGeneration.BuildHint(10, 10, "Aldgate", dest);
        Assert.Contains(expectedDir, hint);
        Assert.Contains("of Aldgate", hint);
    }

    [Fact]
    public void Hint_DistanceInDays()
    {
        // 25 tiles manhattan = 5 days
        var dest = new HaulGeneration.HaulDestination("test", "Fartown", Terrain.Forest, 30, 15);
        var hint = HaulGeneration.BuildHint(10, 10, "Aldgate", dest);
        Assert.Contains("5 days", hint);
    }

    [Fact]
    public void Hint_SingleDay()
    {
        // 4 tiles manhattan rounds to 1 day
        var dest = new HaulGeneration.HaulDestination("test", "Neartown", Terrain.Forest, 12, 12);
        var hint = HaulGeneration.BuildHint(10, 10, "Aldgate", dest);
        Assert.Contains("1 day", hint);
        Assert.DoesNotContain("1 days", hint);
    }

    [Fact]
    public void Hint_IncludesBiome()
    {
        var dest = new HaulGeneration.HaulDestination("test", "Bogtown", Terrain.Swamp, 20, 10);
        var hint = HaulGeneration.BuildHint(10, 10, "Aldgate", dest);
        Assert.StartsWith("A swamp settlement", hint);
    }

    [Theory]
    [InlineData(10, 0, "north")]
    [InlineData(20, 0, "east")]
    [InlineData(10, 20, "south")]
    [InlineData(0, 10, "west")]
    [InlineData(18, 5, "northeast")]
    [InlineData(20, 20, "southeast")]
    public void RelativeHint_DirectionIsCorrect(int destX, int destY, string expectedDir)
    {
        var hint = HaulGeneration.BuildRelativeHint(10, 10, destX, destY);
        Assert.Contains(expectedDir, hint);
        Assert.Contains("of here", hint);
    }

    [Fact]
    public void RelativeHint_SameTile_ReturnsRightHere()
    {
        var hint = HaulGeneration.BuildRelativeHint(10, 10, 10, 10);
        Assert.Equal("Right here", hint);
    }

    [Fact]
    public void RelativeHint_DistanceInDays()
    {
        // manhattan = |10-30| + |10-15| = 25, days = round(25/5) = 5
        var hint = HaulGeneration.BuildRelativeHint(10, 10, 30, 15);
        Assert.Contains("about 5 days", hint);
    }

    [Fact]
    public void RelativeHint_SingleDay()
    {
        // manhattan = 4, days = max(1, round(4/5)) = 1
        var hint = HaulGeneration.BuildRelativeHint(10, 10, 12, 12);
        Assert.Contains("about 1 day", hint);
        Assert.DoesNotContain("1 days", hint);
    }

    [Fact]
    public void GeneratedItems_HaveCorrectFields()
    {
        var candidates = new List<HaulGeneration.HaulDestination>
        {
            new("dest1", "Woodhaven", Terrain.Forest, 20, 10)
        };

        var result = HaulGeneration.Generate(
            10, 10, "Aldgate", Terrain.Plains, isLeaf: true,
            candidates, TestHauls, [], [], new Random(42));

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
