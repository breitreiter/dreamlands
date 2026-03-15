using Dreamlands.Map;
using Dreamlands.Orchestration;
using Dreamlands.Rules;

namespace Dreamlands.Orchestration.Tests;

public class EncounterSelectionTests
{
    [Theory]
    [InlineData(Terrain.Plains, 1, "plains/tier1")]
    [InlineData(Terrain.Forest, 2, "forest/tier2")]
    [InlineData(Terrain.Mountains, 3, "mountains/tier3")]
    [InlineData(Terrain.Swamp, 1, "swamp/tier1")]
    [InlineData(Terrain.Scrub, 1, "scrub/tier1")]
    public void GetCategory_Terrain_ReturnsCorrectPath(Terrain terrain, int tier, string expected)
    {
        var node = new Node(0, 0, terrain);
        node.Region = new Region(1, terrain) { Tier = tier };
        Assert.Equal(expected, EncounterSelection.GetCategory(node));
    }

    [Fact]
    public void GetCategory_Lake_ReturnsNull()
    {
        var node = new Node(0, 0, Terrain.Lake);
        Assert.Null(EncounterSelection.GetCategory(node));
    }

    [Theory]
    [InlineData(Terrain.Plains, "arcs/plains/crypt")]
    [InlineData(Terrain.Forest, "arcs/forest/crypt")]
    [InlineData(Terrain.Mountains, "arcs/mountains/crypt")]
    public void GetPoiCategory_Dungeon_ReturnsArcsBiomePath(Terrain terrain, string expected)
    {
        var node = new Node(0, 0, terrain);
        node.Poi = new Poi(PoiKind.Dungeon, "Dungeon") { DungeonId = "crypt" };
        Assert.Equal(expected, EncounterSelection.GetPoiCategory(node));
    }

    [Theory]
    [InlineData(Terrain.Plains, "settlements/plains")]
    [InlineData(Terrain.Forest, "settlements/forest")]
    public void GetPoiCategory_Settlement_ReturnsSettlementsBiomePath(Terrain terrain, string expected)
    {
        var node = new Node(0, 0, terrain);
        node.Poi = new Poi(PoiKind.Settlement, "Settlement");
        Assert.Equal(expected, EncounterSelection.GetPoiCategory(node));
    }

    [Fact]
    public void GetPoiCategory_NoPoi_ReturnsNull()
    {
        var node = new Node(0, 0, Terrain.Plains);
        Assert.Null(EncounterSelection.GetPoiCategory(node));
    }

    [Fact]
    public void GetPoiCategory_Lake_ReturnsNull()
    {
        var node = new Node(0, 0, Terrain.Lake);
        node.Poi = new Poi(PoiKind.Dungeon, "Dungeon") { DungeonId = "crypt" };
        Assert.Null(EncounterSelection.GetPoiCategory(node));
    }

    [Fact]
    public void PickOverworld_FiltersUsedEncounters()
    {
        var bundle = Helpers.MakeBundle(
            new Helpers.BundleEntry("enc1", "plains/tier1"),
            new Helpers.BundleEntry("enc2", "plains/tier1"));

        var map = Helpers.MakeMap();
        var region = new Region(1, Terrain.Plains) { Tier = 1 };
        map[1, 1].Region = region;

        var session = Helpers.MakeSession(map: map, bundle: bundle);
        session.Player.UsedEncounterIds.Add("plains/tier1/enc1");

        var picked = EncounterSelection.PickOverworld(session, session.CurrentNode);

        Assert.NotNull(picked);
        Assert.Equal("plains/tier1/enc2", picked.Id);
    }

    [Fact]
    public void PickOverworld_AllUsed_ReturnsNull()
    {
        var bundle = Helpers.MakeBundle(
            new Helpers.BundleEntry("enc1", "plains/tier1"));

        var map = Helpers.MakeMap();
        var region = new Region(1, Terrain.Plains) { Tier = 1 };
        map[1, 1].Region = region;

        var session = Helpers.MakeSession(map: map, bundle: bundle);
        session.Player.UsedEncounterIds.Add("plains/tier1/enc1");

        Assert.Null(EncounterSelection.PickOverworld(session, session.CurrentNode));
    }

    [Fact]
    public void PickOverworld_NoCategory_ReturnsNull()
    {
        var map = Helpers.MakeMap();
        map[1, 1].Terrain = Terrain.Lake;
        var session = Helpers.MakeSession(map: map);

        Assert.Null(EncounterSelection.PickOverworld(session, session.CurrentNode));
    }

    [Fact]
    public void GetDungeonStart_PrefersStartId()
    {
        var bundle = Helpers.MakeBundle(
            new Helpers.BundleEntry("Other", "arcs/plains/crypt"),
            new Helpers.BundleEntry("Start", "arcs/plains/crypt"));

        var map = Helpers.MakeMap();
        map[1, 1].Poi = new Poi(PoiKind.Dungeon, "Dungeon") { DungeonId = "crypt" };

        var session = Helpers.MakeSession(map: map, bundle: bundle);

        var enc = EncounterSelection.GetDungeonStart(session, session.CurrentNode);

        Assert.NotNull(enc);
        Assert.Equal("arcs/plains/crypt/Start", enc.Id);
    }

    [Fact]
    public void PickOverworld_FiltersOutWhenRequiresFails()
    {
        var bundle = Helpers.MakeBundle(
            new Helpers.BundleEntry("gated", "plains/tier1", Requires: new[] { "tag special_flag" }),
            new Helpers.BundleEntry("open", "plains/tier1"));

        var map = Helpers.MakeMap();
        var region = new Region(1, Terrain.Plains) { Tier = 1 };
        map[1, 1].Region = region;

        var session = Helpers.MakeSession(map: map, bundle: bundle);
        // Player does NOT have "special_flag" tag, so "gated" should be filtered out

        var picked = EncounterSelection.PickOverworld(session, session.CurrentNode);

        Assert.NotNull(picked);
        Assert.Equal("plains/tier1/open", picked.Id);
    }

    [Fact]
    public void PickOverworld_IncludesWhenRequiresPasses()
    {
        var bundle = Helpers.MakeBundle(
            new Helpers.BundleEntry("gated", "plains/tier1", Requires: new[] { "tag special_flag" }));

        var map = Helpers.MakeMap();
        var region = new Region(1, Terrain.Plains) { Tier = 1 };
        map[1, 1].Region = region;

        var session = Helpers.MakeSession(map: map, bundle: bundle);
        session.Player.Tags.Add("special_flag");

        var picked = EncounterSelection.PickOverworld(session, session.CurrentNode);

        Assert.NotNull(picked);
        Assert.Equal("plains/tier1/gated", picked.Id);
    }

    [Fact]
    public void ResolveNavigation_InDungeon_SearchesDungeonFirst()
    {
        var bundle = Helpers.MakeBundle(
            new Helpers.BundleEntry("room2", "arcs/plains/crypt"),
            new Helpers.BundleEntry("other_enc", "plains/tier1"));

        var map = Helpers.MakeMap();
        map[1, 1].Poi = new Poi(PoiKind.Dungeon, "Dungeon") { DungeonId = "crypt" };

        var session = Helpers.MakeSession(map: map, bundle: bundle);
        session.Player.CurrentDungeonId = "crypt";

        var enc = EncounterSelection.ResolveNavigation(session, "room2", session.CurrentNode);

        Assert.NotNull(enc);
        Assert.Equal("arcs/plains/crypt", enc.Category);
    }

    [Fact]
    public void GetAvailableAtPoi_FiltersByUsedAndRequires()
    {
        var bundle = Helpers.MakeBundle(
            new Helpers.BundleEntry("Start", "arcs/plains/crypt"),
            new Helpers.BundleEntry("Room1", "arcs/plains/crypt"),
            new Helpers.BundleEntry("Room2", "arcs/plains/crypt", Requires: new[] { "tag has_key" }));

        var map = Helpers.MakeMap();
        map[1, 1].Poi = new Poi(PoiKind.Dungeon, "Dungeon") { DungeonId = "crypt" };

        var session = Helpers.MakeSession(map: map, bundle: bundle);
        session.Player.UsedEncounterIds.Add("arcs/plains/crypt/Start");

        var available = EncounterSelection.GetAvailableAtPoi(session, session.CurrentNode);

        // Start is used, Room2 requires tag — only Room1 remains
        Assert.Single(available);
        Assert.Equal("arcs/plains/crypt/Room1", available[0].Id);
    }

    [Fact]
    public void GetAvailableAtPoi_NoCategoryReturnsEmpty()
    {
        var map = Helpers.MakeMap();
        // No POI on node
        var session = Helpers.MakeSession(map: map);

        var available = EncounterSelection.GetAvailableAtPoi(session, session.CurrentNode);

        Assert.Empty(available);
    }

    [Fact]
    public void GetAvailableAtPoi_Settlement_ReturnsByBiome()
    {
        var bundle = Helpers.MakeBundle(
            new Helpers.BundleEntry("notice1", "settlements/plains"),
            new Helpers.BundleEntry("notice2", "settlements/plains"));

        var map = Helpers.MakeMap();
        map[1, 1].Poi = new Poi(PoiKind.Settlement, "Settlement");

        var session = Helpers.MakeSession(map: map, bundle: bundle);

        var available = EncounterSelection.GetAvailableAtPoi(session, session.CurrentNode);

        Assert.Equal(2, available.Count);
    }
}
