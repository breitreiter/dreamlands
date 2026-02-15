namespace Dreamlands.Game;

/// <summary>An item in the player's inventory. DefId references the balance catalog.</summary>
public record ItemInstance(string DefId, string DisplayName);
