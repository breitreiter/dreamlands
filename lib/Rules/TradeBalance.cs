namespace Dreamlands.Rules;

/// <summary>Trade balance data for market pricing.</summary>
public sealed class TradeBalance
{
    public static readonly TradeBalance Default = new();

    public double PriceJitter { get; init; } = 0.05;
    public double MercantileHaulBonusPerPoint { get; init; } = 0.10;
    public double SellRatio { get; init; } = 0.5;

    public IReadOnlyDictionary<SettlementSize, int> MaxStock { get; init; } = new Dictionary<SettlementSize, int>
    {
        [SettlementSize.Camp] = 3,
        [SettlementSize.Outpost] = 4,
        [SettlementSize.Village] = 5,
        [SettlementSize.Town] = 6,
        [SettlementSize.City] = 8,
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
