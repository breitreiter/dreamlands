using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Dreamlands.Flavor;

/// <summary>Biome-specific flavor names for equipment, food, and trade goods.</summary>
public sealed class FlavorNames
{
    // quality -> biome -> list of (name, class)
    readonly Dictionary<string, Dictionary<string, List<WeaponName>>> _weapons = new();
    // quality -> biome -> list of names
    readonly Dictionary<string, Dictionary<string, List<string>>> _armor = new();
    // category -> biome -> (vendor, foraged)
    readonly Dictionary<string, Dictionary<string, FoodNames>> _food = new();
    // category -> list of names
    readonly Dictionary<string, List<string>> _trade = new();

    public IReadOnlyList<WeaponName> WeaponNames(string quality, string biome) =>
        _weapons.TryGetValue(quality, out var byBiome) && byBiome.TryGetValue(biome, out var list) ? list : [];

    public IReadOnlyList<string> ArmorNames(string quality, string biome) =>
        _armor.TryGetValue(quality, out var byBiome) && byBiome.TryGetValue(biome, out var list) ? list : [];

    public FoodNames FoodNames(string category, string biome) =>
        _food.TryGetValue(category, out var byBiome) && byBiome.TryGetValue(biome, out var names) ? names : default;

    public IReadOnlyList<string> TradeNames(string category) =>
        _trade.TryGetValue(category, out var list) ? list : [];

    public static FlavorNames Load(string flavorPath)
    {
        var result = new FlavorNames();
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();

        LoadEquipment(result, flavorPath, deserializer);
        LoadFood(result, flavorPath, deserializer);
        LoadTrade(result, flavorPath, deserializer);

        return result;
    }

    static void LoadEquipment(FlavorNames result, string flavorPath, IDeserializer deserializer)
    {
        var path = Path.Combine(flavorPath, "equipment_names.yaml");
        if (!File.Exists(path)) return;

        var doc = deserializer.Deserialize<EquipmentNamesYaml>(File.ReadAllText(path));

        if (doc.Weapons != null)
        {
            foreach (var (quality, biomes) in doc.Weapons)
            {
                var byBiome = new Dictionary<string, List<WeaponName>>();
                foreach (var (biome, items) in biomes)
                {
                    byBiome[biome] = items.Select(w =>
                        new WeaponName(w.Name ?? "", w.Class ?? "")).ToList();
                }
                result._weapons[quality] = byBiome;
            }
        }

        if (doc.Armor != null)
        {
            foreach (var (quality, biomes) in doc.Armor)
            {
                var byBiome = new Dictionary<string, List<string>>();
                foreach (var (biome, names) in biomes)
                    byBiome[biome] = names;
                result._armor[quality] = byBiome;
            }
        }
    }

    static void LoadFood(FlavorNames result, string flavorPath, IDeserializer deserializer)
    {
        var path = Path.Combine(flavorPath, "food_names.yaml");
        if (!File.Exists(path)) return;

        var doc = deserializer.Deserialize<Dictionary<string, Dictionary<string, FoodFlavorYaml>>>(
            File.ReadAllText(path));
        if (doc == null) return;

        foreach (var (category, biomes) in doc)
        {
            var byBiome = new Dictionary<string, FoodNames>();
            foreach (var (biome, fl) in biomes)
                byBiome[biome] = new FoodNames(fl.Vendor ?? [], fl.Foraged ?? []);
            result._food[category] = byBiome;
        }
    }

    static void LoadTrade(FlavorNames result, string flavorPath, IDeserializer deserializer)
    {
        var path = Path.Combine(flavorPath, "trade_names.yaml");
        if (!File.Exists(path)) return;

        var doc = deserializer.Deserialize<Dictionary<string, List<string>>>(File.ReadAllText(path));
        if (doc == null) return;

        foreach (var (category, names) in doc)
            result._trade[category] = names;
    }

    // DTOs
    class EquipmentNamesYaml
    {
        public Dictionary<string, Dictionary<string, List<WeaponYaml>>>? Weapons { get; set; }
        public Dictionary<string, Dictionary<string, List<string>>>? Armor { get; set; }
    }
    class WeaponYaml
    {
        public string? Name { get; set; }
        public string? Class { get; set; }
    }
    class FoodFlavorYaml
    {
        public List<string>? Vendor { get; set; }
        public List<string>? Foraged { get; set; }
    }
}

/// <summary>A biome-specific weapon flavor name with weapon class.</summary>
public readonly record struct WeaponName(string Name, string Class);

/// <summary>Vendor and foraged flavor names for a food category in a biome.</summary>
public readonly record struct FoodNames(IReadOnlyList<string> Vendor, IReadOnlyList<string> Foraged);
