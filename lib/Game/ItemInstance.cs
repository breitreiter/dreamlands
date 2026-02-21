using Dreamlands.Rules;

namespace Dreamlands.Game;

/// <summary>An item in the player's inventory. DefId references the balance catalog.</summary>
public record ItemInstance(string DefId, string DisplayName)
{
    public FoodType? FoodType { get; init; }
    public string? Description { get; init; }
}
