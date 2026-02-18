namespace Dreamlands.Rules;

/// <summary>Food category definition.</summary>
public sealed class FoodCategory
{
    public string Id { get; init; } = "";
    public int BasePrice { get; init; }
    public int Slots { get; init; } = 1;
    public int StackSize { get; init; } = 10;
}

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

/// <summary>All food balance data.</summary>
public sealed class FoodBalance
{
    public static readonly FoodBalance Default = new();

    public int UnitsPerDay { get; init; } = 3;
    public IReadOnlyDictionary<string, FoodCategory> Categories { get; init; } = new Dictionary<string, FoodCategory>();
    public IReadOnlyList<MealBonus> MealBonuses { get; init; } = [];
    public ForagingRules Foraging { get; init; } = new();
    public IReadOnlyDictionary<string, SpecialFood> SpecialFoods { get; init; } = new Dictionary<string, SpecialFood>();
}
