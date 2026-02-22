namespace Dreamlands.Rules;

/// <summary>Trade balance data. Trade goods are defined as ItemDefs with ItemType.TradeGood.</summary>
public sealed class TradeBalance
{
    public static readonly TradeBalance Default = new();

    public double FeaturedSellDiscount { get; init; } = 0.15;
    public double FeaturedBuyPremium { get; init; } = 0.25;
    public double PriceJitter { get; init; } = 0.05;
    public double SameBiomeBuyPenalty { get; init; } = 0.10;
    public double MercantileDiscountPerPoint { get; init; } = 0.02;

    public IReadOnlyDictionary<SettlementSize, int> MaxStock { get; init; } = new Dictionary<SettlementSize, int>
    {
        [SettlementSize.Camp] = 1,
        [SettlementSize.Outpost] = 2,
        [SettlementSize.Village] = 3,
        [SettlementSize.Town] = 4,
        [SettlementSize.City] = 5,
    };

    public IReadOnlyDictionary<SettlementSize, int> RestockPerDay { get; init; } = new Dictionary<SettlementSize, int>
    {
        [SettlementSize.Camp] = 1,
        [SettlementSize.Outpost] = 1,
        [SettlementSize.Village] = 1,
        [SettlementSize.Town] = 2,
        [SettlementSize.City] = 2,
    };
}
