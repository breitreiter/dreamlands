namespace Dreamlands.Game;

public class SettlementState
{
    public string Biome { get; set; } = "";
    public Dictionary<string, int> Stock { get; set; } = new();
    public Dictionary<string, int> Prices { get; set; } = new();
    public int LastRestockDay { get; set; }
    public List<ItemInstance> Bank { get; set; } = new();
    public List<ItemInstance> HaulOffers { get; set; } = new();
    public int LastHaulStockDay { get; set; }
    public List<string> StoryletOffers { get; set; } = new();
    public int LastStoryletStockDay { get; set; }
    public HashSet<string> Flags { get; set; } = new();
}
