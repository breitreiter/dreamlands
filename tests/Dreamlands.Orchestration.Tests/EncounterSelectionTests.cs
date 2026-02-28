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
        session.Player.UsedEncounterIds.Add("enc1");

        var picked = EncounterSelection.PickOverworld(session, session.CurrentNode);

        Assert.NotNull(picked);
        Assert.Equal("enc2", picked.Id);
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
        session.Player.UsedEncounterIds.Add("enc1");

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
            new Helpers.BundleEntry("Other", "dungeons/crypt"),
            new Helpers.BundleEntry("Start", "dungeons/crypt"));

        var session = Helpers.MakeSession(bundle: bundle);

        var enc = EncounterSelection.GetDungeonStart(session, "crypt");

        Assert.NotNull(enc);
        Assert.Equal("Start", enc.Id);
    }

    [Fact]
    public void ResolveNavigation_InDungeon_SearchesDungeonFirst()
    {
        // Bundle has "room2" only in the dungeon category.
        // ResolveNavigation should find it there before falling back to GetById.
        var bundle = Helpers.MakeBundle(
            new Helpers.BundleEntry("room2", "dungeons/crypt"),
            new Helpers.BundleEntry("other_enc", "plains/tier1"));

        var session = Helpers.MakeSession(bundle: bundle);
        session.Player.CurrentDungeonId = "crypt";

        var enc = EncounterSelection.ResolveNavigation(session, "room2");

        Assert.NotNull(enc);
        Assert.Equal("dungeons/crypt", enc.Category);
    }
}
