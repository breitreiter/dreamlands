using Dreamlands.Rules;

namespace Dreamlands.Flavor;

public static class RegionNames
{
    // Each biome has exactly 1 T1 region and 1 T3 region — fixed names.
    // T2 regions vary in count — draw from a pool.

    // Plains: archaic British, orderly, agricultural
    static readonly string PlainsT1 = "The Hearthlands";
    static readonly string PlainsT3 = "The Spires";
    static readonly string[] PlainsT2 =
    [
        "The Wychdowns", "Barrow Heath", "The Fallow Reach",
        "Whitmarsh Steppe", "Copperfield Green", "Thornhurst Plain",
        "The Windbreak", "Ashburn Steppe", "Dunmere Flats", "The Longmarch",
    ];

    // Mountains: Appalachian (low), Swiss-German (mid), stark (high)
    static readonly string MountainsT1 = "The Lower Dells";
    static readonly string MountainsT3 = "The Stift";
    static readonly string[] MountainsT2 =
    [
        "Kaltbach Reach", "The Greywalls", "Felsgrund Shelf",
        "Hawkridge", "The Ironcrags", "Sourwood Hollow",
        "Windthal", "Sturmfels Pass", "The Cold Fork", "Nebelthal",
    ];

    // Forest: laconic, blunt, consonant-heavy
    static readonly string ForestT1 = "The Millwood";
    static readonly string ForestT3 = "The Hunting Ground";
    static readonly string[] ForestT2 =
    [
        "Brackenfall", "The Snagwood", "Nettledark",
        "Wrenwood", "The Briars", "Foxhollow",
        "The Knotwood", "Drygrove", "Stagrun", "Darkfen",
    ];

    // Scrub: hard consonants, mesa/canyon geography
    static readonly string ScrubT1 = "The Kethari Reach";
    static readonly string ScrubT3 = "The Overshine";
    static readonly string[] ScrubT2 =
    [
        "Gheltok Shelf", "The Dralg Flats", "Tarsin Mesa",
        "Brekh Canyon", "Krozht Basin", "The Vosht Barrens",
        "Skarn Plateau", "Reshk Corridor", "The Mordak Steppe", "Thurm Ridge",
    ];

    // Swamp: Revathikh (old) and Revashu (modern), soft vowels, memory-laden
    static readonly string SwampT1 = "The Shallows";
    static readonly string SwampT3 = "The Dreaming Land";
    static readonly string[] SwampT2 =
    [
        "Ashëmîr Fen", "The Kephîr Pools", "Drîmath Basin",
        "The Stiltwaters", "Velashîr Reach", "The Root Hollows",
        "Nothëgul Marsh", "Sulvesh Mere", "The Blackwater", "Thîral Fen",
    ];

    static readonly Dictionary<Terrain, string> Tier1Names = new()
    {
        [Terrain.Plains] = PlainsT1,
        [Terrain.Mountains] = MountainsT1,
        [Terrain.Forest] = ForestT1,
        [Terrain.Scrub] = ScrubT1,
        [Terrain.Swamp] = SwampT1,
    };

    static readonly Dictionary<Terrain, string> Tier3Names = new()
    {
        [Terrain.Plains] = PlainsT3,
        [Terrain.Mountains] = MountainsT3,
        [Terrain.Forest] = ForestT3,
        [Terrain.Scrub] = ScrubT3,
        [Terrain.Swamp] = SwampT3,
    };

    static readonly Dictionary<Terrain, string[]> Tier2Pools = new()
    {
        [Terrain.Plains] = PlainsT2,
        [Terrain.Mountains] = MountainsT2,
        [Terrain.Forest] = ForestT2,
        [Terrain.Scrub] = ScrubT2,
        [Terrain.Swamp] = SwampT2,
    };

    public static string? Draw(Terrain biome, int tier, Random rng, HashSet<string> used)
    {
        if (tier == 1 && Tier1Names.TryGetValue(biome, out var t1))
            return t1;

        if (tier == 3 && Tier3Names.TryGetValue(biome, out var t3))
            return t3;

        if (tier == 2 && Tier2Pools.TryGetValue(biome, out var pool))
        {
            var eligible = pool.Where(n => !used.Contains(n)).ToArray();
            if (eligible.Length == 0)
                return null;

            var name = eligible[rng.Next(eligible.Length)];
            used.Add(name);
            return name;
        }

        return null;
    }
}
