namespace Dreamlands.Rules;

public enum FoodType { Protein, Grain, Sweets }

/// <summary>Foraging rules.</summary>
public readonly record struct ForagingRules(
    string Skill = "bushcraft",
    string Difficulty = "easy",
    int UnitsOnSuccess = 1,
    int AttemptsPerDay = 2);
