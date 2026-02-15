using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Dreamlands.Rules;

/// <summary>Food category definition from food.yaml.</summary>
public sealed class FoodCategory
{
    public string Id { get; init; } = "";
    public int BasePrice { get; init; }
    public int Slots { get; init; } = 1;
    public int StackSize { get; init; } = 10;
    public IReadOnlyDictionary<string, FoodFlavors> Flavors { get; init; } =
        new Dictionary<string, FoodFlavors>();
}

/// <summary>Vendor and foraged flavor names for a biome.</summary>
public readonly record struct FoodFlavors(IReadOnlyList<string> Vendor, IReadOnlyList<string> Foraged);

/// <summary>A special food with prophylactic properties.</summary>
public sealed class SpecialFood
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string Category { get; init; } = "";
    public int BasePrice { get; init; }
    public int Slots { get; init; } = 1;
    public int StackSize { get; init; } = 5;
    public string ProphylacticCondition { get; init; } = "";
    public int ResistBonus { get; init; }
}

/// <summary>Meal bonus definition.</summary>
public readonly record struct MealBonus(string Type, int HealthRestore, int SpiritsRestore);

/// <summary>Foraging rules.</summary>
public sealed class ForagingRules
{
    public string Skill { get; init; } = "bushcraft";
    public string Difficulty { get; init; } = "easy";
    public int UnitsOnSuccess { get; init; } = 1;
    public int AttemptsPerDay { get; init; } = 2;
    public IReadOnlyDictionary<string, int> BiomeModifiers { get; init; } = new Dictionary<string, int>();
    public double CriticalFailureChance { get; init; } = 0.1;
}

/// <summary>All food balance data from food.yaml.</summary>
public sealed class FoodBalance
{
    public int UnitsPerDay { get; init; } = 3;
    public IReadOnlyDictionary<string, FoodCategory> Categories { get; init; } = new Dictionary<string, FoodCategory>();
    public IReadOnlyList<MealBonus> MealBonuses { get; init; } = [];
    public ForagingRules Foraging { get; init; } = new();
    public IReadOnlyDictionary<string, SpecialFood> SpecialFoods { get; init; } = new Dictionary<string, SpecialFood>();

    internal static FoodBalance Load(string balancePath)
    {
        var path = Path.Combine(balancePath, "food.yaml");
        if (!File.Exists(path)) return new FoodBalance();

        var yaml = File.ReadAllText(path);
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();

        var doc = deserializer.Deserialize<FoodDoc>(yaml);
        if (doc?.Food == null) return new FoodBalance();
        var f = doc.Food;

        var categories = new Dictionary<string, FoodCategory>();
        if (f.Categories != null)
        {
            foreach (var (id, cat) in f.Categories)
            {
                var flavors = new Dictionary<string, FoodFlavors>();
                if (cat.Flavors != null)
                {
                    foreach (var (biome, fl) in cat.Flavors)
                    {
                        flavors[biome] = new FoodFlavors(
                            fl.Vendor ?? [],
                            fl.Foraged ?? []);
                    }
                }
                categories[id] = new FoodCategory
                {
                    Id = id,
                    BasePrice = cat.BasePrice,
                    Slots = cat.Slots > 0 ? cat.Slots : 1,
                    StackSize = cat.StackSize > 0 ? cat.StackSize : 10,
                    Flavors = flavors,
                };
            }
        }

        var meals = new List<MealBonus>();
        if (f.Meals != null)
        {
            foreach (var (type, meal) in f.Meals)
            {
                meals.Add(new MealBonus(
                    type,
                    meal.Effects?.HealthRestore ?? 0,
                    meal.Effects?.SpiritsRestore ?? 0));
            }
        }

        var foraging = new ForagingRules();
        if (f.Foraging != null)
        {
            var biomeModifiers = new Dictionary<string, int>();
            if (f.Foraging.BiomeModifiers != null)
            {
                foreach (var (biome, mod) in f.Foraging.BiomeModifiers)
                    biomeModifiers[biome] = mod;
            }
            foraging = new ForagingRules
            {
                Skill = f.Foraging.Skill ?? "bushcraft",
                Difficulty = f.Foraging.Difficulty ?? "easy",
                UnitsOnSuccess = f.Foraging.UnitsOnSuccess > 0 ? f.Foraging.UnitsOnSuccess : 1,
                AttemptsPerDay = f.Foraging.AttemptsPerDay > 0 ? f.Foraging.AttemptsPerDay : 2,
                BiomeModifiers = biomeModifiers,
                CriticalFailureChance = f.Foraging.Failure?.CriticalFailureChance ?? 0.1,
            };
        }

        var specialFoods = new Dictionary<string, SpecialFood>();
        if (f.SpecialFoods != null)
        {
            foreach (var (id, sf) in f.SpecialFoods)
            {
                specialFoods[id] = new SpecialFood
                {
                    Id = id,
                    Name = sf.Name ?? id,
                    Category = sf.Category ?? "",
                    BasePrice = sf.BasePrice,
                    Slots = sf.Slots > 0 ? sf.Slots : 1,
                    StackSize = sf.StackSize > 0 ? sf.StackSize : 5,
                    ProphylacticCondition = sf.Prophylactic?.Condition ?? "",
                    ResistBonus = sf.Prophylactic?.ResistBonus ?? 0,
                };
            }
        }

        return new FoodBalance
        {
            UnitsPerDay = f.UnitsPerDay > 0 ? f.UnitsPerDay : 3,
            Categories = categories,
            MealBonuses = meals,
            Foraging = foraging,
            SpecialFoods = specialFoods,
        };
    }

    // DTOs
    class FoodDoc { public FoodYaml? Food { get; set; } }
    class FoodYaml
    {
        public int UnitsPerDay { get; set; }
        public Dictionary<string, FoodCatYaml>? Categories { get; set; }
        public Dictionary<string, MealYaml>? Meals { get; set; }
        public ForagingYaml? Foraging { get; set; }
        public Dictionary<string, SpecialFoodYaml>? SpecialFoods { get; set; }
    }
    class FoodCatYaml
    {
        public int BasePrice { get; set; }
        public int Slots { get; set; }
        public int StackSize { get; set; }
        public Dictionary<string, FlavorListYaml>? Flavors { get; set; }
    }
    class FlavorListYaml
    {
        public List<string>? Vendor { get; set; }
        public List<string>? Foraged { get; set; }
    }
    class MealYaml
    {
        public string? Description { get; set; }
        public object? Requires { get; set; }
        public MealEffectsYaml? Effects { get; set; }
    }
    class MealEffectsYaml
    {
        public int HealthRestore { get; set; }
        public int SpiritsRestore { get; set; }
        public string? AppliesCondition { get; set; }
    }
    class ForagingYaml
    {
        public string? Skill { get; set; }
        public string? Difficulty { get; set; }
        public int UnitsOnSuccess { get; set; }
        public int AttemptsPerDay { get; set; }
        public string? Category { get; set; }
        public string? FlavorSource { get; set; }
        public Dictionary<string, int>? BiomeModifiers { get; set; }
        public ForagingFailureYaml? Failure { get; set; }
    }
    class ForagingFailureYaml
    {
        public double CriticalFailureChance { get; set; }
        public string? CriticalFailureEffect { get; set; }
    }
    class SpecialFoodYaml
    {
        public string? Name { get; set; }
        public string? Category { get; set; }
        public int BasePrice { get; set; }
        public int Slots { get; set; }
        public int StackSize { get; set; }
        public ProphylacticYaml? Prophylactic { get; set; }
        public string? Availability { get; set; }
        public string? Note { get; set; }
    }
    class ProphylacticYaml
    {
        public string? Condition { get; set; }
        public int ResistBonus { get; set; }
    }
}
