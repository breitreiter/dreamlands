using Dreamlands.Map;
using Dreamlands.Rules;
using WorldMap = Dreamlands.Map.Map;

namespace Dreamlands.MapTests;

public class MapSerializerTests
{
    static WorldMap MakeMap()
    {
        var map = new WorldMap(3, 3);
        foreach (var node in map.AllNodes())
            node.Terrain = Terrain.Plains;
        return map;
    }

    static WorldMap Roundtrip(WorldMap map, int seed = 42)
    {
        var json = MapSerializer.ToJson(map, seed);
        return MapSerializer.FromJson(json);
    }

    [Fact]
    public void Roundtrip_PreservesSize()
    {
        var map = MakeMap();
        var result = Roundtrip(map);

        Assert.Equal(map.Width, result.Width);
        Assert.Equal(map.Height, result.Height);
    }

    [Fact]
    public void Roundtrip_PreservesStartingCity()
    {
        var map = MakeMap();
        map.StartingCity = map[1, 2];
        var result = Roundtrip(map);

        Assert.NotNull(result.StartingCity);
        Assert.Equal(1, result.StartingCity.X);
        Assert.Equal(2, result.StartingCity.Y);
    }

    [Fact]
    public void Roundtrip_PreservesTerrain()
    {
        var map = MakeMap();
        map[0, 0].Terrain = Terrain.Forest;
        map[1, 1].Terrain = Terrain.Lake;
        map[2, 2].Terrain = Terrain.Mountains;

        var result = Roundtrip(map);

        Assert.Equal(Terrain.Forest, result[0, 0].Terrain);
        Assert.Equal(Terrain.Lake, result[1, 1].Terrain);
        Assert.Equal(Terrain.Mountains, result[2, 2].Terrain);
    }

    [Fact]
    public void Roundtrip_PreservesRegions()
    {
        var map = MakeMap();
        var region = new Region(1, Terrain.Plains) { Name = "TestRegion", Tier = 2 };
        map.Regions.Add(region);
        map[0, 0].Region = region;
        region.Nodes.Add(map[0, 0]);

        var result = Roundtrip(map);

        Assert.Single(result.Regions);
        Assert.Equal("TestRegion", result.Regions[0].Name);
        Assert.Equal(2, result.Regions[0].Tier);
        Assert.Equal(result.Regions[0], result[0, 0].Region);
    }

    [Fact]
    public void Roundtrip_PreservesPoi()
    {
        var map = MakeMap();
        map[1, 1].Poi = new Poi(PoiKind.Settlement, "town")
        {
            Name = "Riverton",
            Size = SettlementSize.Town,
        };

        var result = Roundtrip(map);

        var poi = result[1, 1].Poi;
        Assert.NotNull(poi);
        Assert.Equal(PoiKind.Settlement, poi.Kind);
        Assert.Equal("town", poi.Type);
        Assert.Equal("Riverton", poi.Name);
        Assert.Equal(SettlementSize.Town, poi.Size);
    }

    [Fact]
    public void Roundtrip_PreservesDistanceFromCity()
    {
        var map = MakeMap();
        map[0, 0].DistanceFromCity = 5;
        map[1, 1].DistanceFromCity = 10;

        var result = Roundtrip(map);

        Assert.Equal(5, result[0, 0].DistanceFromCity);
        Assert.Equal(10, result[1, 1].DistanceFromCity);
    }

    [Fact]
    public void Roundtrip_NullDistanceFromCity_BecomesMaxValue()
    {
        var map = MakeMap();
        Assert.Equal(int.MaxValue, map[0, 0].DistanceFromCity);

        var result = Roundtrip(map);

        Assert.Equal(int.MaxValue, result[0, 0].DistanceFromCity);
    }
}
