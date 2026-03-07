using Dreamlands.Rules;

namespace Dreamlands.Flavor;

public static class SettlementNames
{
    // Plains T1: Archaic British village names
    static readonly string[] PlainsT1 =
    [
        "Bramfield", "Merewick", "Harthcombe", "Cresswell Vale", "Longmere",
        "Ashdown", "Wychford", "Eldenbury", "Thornhurst", "Dunmere",
        "Barrowfield", "Whitmarsh", "Stonehill", "Kingshollow", "Westmarch",
        "Reedham", "Oldham Cross", "Millhaven", "Drybridge", "Fenwick",
        "Hartwell", "Aldbury", "Copperfield", "Staveley", "Mossgate",
    ];

    // Plains T2: Ruined fort / military infrastructure
    static readonly string[] PlainsT2 =
    [
        "Fort Redbank", "Grainway Station", "Dunfield Watch", "Holt Garrison",
        "Marchford Keep", "Ashburn Stockade", "Fallowgate Post", "Redmire Station",
        "Drywell Fort", "Stonewall Halt", "Thornwatch", "Grimsby Redoubt",
        "Waymark Tower", "Ironfield Camp", "Sallow Barracks", "Windbreak Outpost",
        "Kettleford Depot", "Rampart Hill", "Shieldrow", "Garrison Crossing",
        "Beacon Tor", "Millstone Keep", "Dustgate Fort", "Longwatch", "Sentry Ridge",
    ];

    // Mountains T1: Appalachian terrain-based names
    static readonly string[] MountainsT1 =
    [
        "Briar Run", "Kestrel Hollow", "Ashfall", "Copperhead Gap", "Slate Creek",
        "Laurel Fork", "Hawthorn Ridge", "Rockbridge", "Coldwater", "Deerfield",
        "Stony Point", "Hickory Flat", "Ironstone", "Birch Hollow", "Millstone Gap",
        "Cinder Hill", "Hemlock Bend", "Eagle Roost", "Flint Knob", "Sourwood",
        "Raven Cliff", "Bear Wallow", "Lindenfall", "Cedar Branch", "Tumbling Shoals",
    ];

    // Mountains T2: Swiss-German cultural names
    static readonly string[] MountainsT2 =
    [
        "Hohrthal", "Kaltbach", "Steinmund", "Graufeld", "Eismark",
        "Felsgrund", "Dunkelberg", "Rotstein", "Schwarzbach", "Windthal",
        "Bittergrat", "Tiefenklamm", "Grauwand", "Hartzell", "Nordstein",
        "Sturmfels", "Dachgrat", "Weisshorn", "Silberkamm", "Nebelthal",
        "Klingstein", "Brenngrat", "Frostheim", "Altgipfel", "Schattwald",
    ];

    // Forest T1: Short, blunt forest folk names
    static readonly string[] ForestT1 =
    [
        "Thornrun", "Brask", "Redbark", "Skell Hollow", "Dellmark",
        "Stump End", "Rootfast", "Knotwood", "Bracken", "Foxden",
        "Logfall", "Ashgrove", "Pinemark", "Nettlebed", "Snagwood",
        "Hearthoak", "Fernbank", "Wolfrun", "Copse End", "Harewood",
        "Mossbed", "Briarholt", "Darkhollow", "Aldertree", "Wrenfall",
    ];

    // Forest T2: Border Reiver / camp-style names
    static readonly string[] ForestT2 =
    [
        "Burnfoot", "Timonscamp", "Crookholme", "Blackhaugh", "Netherburn",
        "Mosstruther", "Langside", "Dryhope", "Rankleburn", "Fairloaning",
        "Borthwick", "Rowanshiel", "Hawkshaw", "Cleughhead", "Stobswood",
        "Whithaugh", "Priesthaugh", "Deadwater", "Greenshiel", "Wormscleugh",
        "Harelaw", "Akeldrum", "Blindburn", "Corbie Knowe", "Swirefoot",
    ];

    // Scrub T1: Plateau clan — hard consonants, compact
    static readonly string[] ScrubT1 =
    [
        "Gharok", "Tarsin", "Kethar Pass", "Brekh", "Vosht",
        "Dralg", "Skarn", "Ghotrek", "Kharst", "Thurm",
        "Darvek", "Krozht", "Reshk", "Bashtal", "Grosk",
        "Vrek", "Teshk", "Mordak", "Ghelt", "Drekhar",
        "Khatim", "Balgur", "Torsk", "Vrethak", "Skeldri",
    ];

    // Scrub T2: Kesharat administrative — vowel-balanced
    static readonly string[] ScrubT2 =
    [
        "Talvek", "Orast", "Velyr", "Qastor", "Imreth",
        "Delani", "Suraq", "Aravel", "Kashtet", "Melori",
        "Pashev", "Toravel", "Kalimur", "Estavan", "Reshava",
        "Vashedi", "Lorath", "Miravel", "Qeneth", "Daviron",
        "Arkhest", "Belovar", "Thessani", "Kalesh", "Orvast",
    ];

    // Swamp T1: Modern Revashu compound names
    static readonly string[] SwampT1 =
    [
        "Sulvesh", "Revashol", "Nakhem", "Thalken", "Drevakh",
        "Morshev", "Kelthane", "Voshnir", "Reshvan", "Gulthek",
        "Dravesh", "Kolshen", "Murthane", "Ashveld", "Threvnik",
        "Reshkol", "Velthan", "Nakshem", "Golvesh", "Drashken",
        "Sulthane", "Krevash", "Molthen", "Thelvek", "Rashvol",
    ];

    // Swamp T2: Older Revathikh names with diacritics
    static readonly string[] SwampT2 =
    [
        "Ashëmîr", "Kephîralûn", "Sothëvani", "Drîmathek", "Velashîr",
        "Thalëkûn", "Rëvashîm", "Nothëgul", "Kîrathel", "Mîreshvan",
        "Ashthërul", "Drëvakhîn", "Sëlthane", "Vîrakhel", "Gulëthen",
        "Thîralvek", "Kreshëmol", "Nëvathir", "Solîrekh", "Dëmravesh",
        "Rathëkîn", "Veshîloth", "Mëthrakel", "Kîlshenvar", "Threvëshîm",
    ];

    static readonly Dictionary<(Terrain, int), string[]> Pools = new()
    {
        [(Terrain.Plains, 1)] = PlainsT1,
        [(Terrain.Plains, 2)] = PlainsT2,
        [(Terrain.Mountains, 1)] = MountainsT1,
        [(Terrain.Mountains, 2)] = MountainsT2,
        [(Terrain.Forest, 1)] = ForestT1,
        [(Terrain.Forest, 2)] = ForestT2,
        [(Terrain.Scrub, 1)] = ScrubT1,
        [(Terrain.Scrub, 2)] = ScrubT2,
        [(Terrain.Swamp, 1)] = SwampT1,
        [(Terrain.Swamp, 2)] = SwampT2,
    };

    public static string? Draw(Terrain biome, int tier, Random rng, HashSet<string> used)
    {
        if (!Pools.TryGetValue((biome, tier), out var pool))
            return null;

        // Collect eligible names
        var eligible = pool.Where(n => !used.Contains(n)).ToArray();
        if (eligible.Length == 0)
            return null;

        var name = eligible[rng.Next(eligible.Length)];
        used.Add(name);
        return name;
    }
}
