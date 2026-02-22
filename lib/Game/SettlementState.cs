namespace Dreamlands.Game;

public class SettlementState
{
    public Dictionary<string, int> Stock { get; set; } = new();
    public Dictionary<string, int> Prices { get; set; } = new();
    public string? FeaturedSellItem { get; set; }
    public string? FeaturedBuyItem { get; set; }
    public int LastRestockDay { get; set; }
}
