namespace Dreamlands.Rules;

/// <summary>Trade balance data. Trade goods are defined as ItemDefs with ItemType.TradeGood.</summary>
public sealed class TradeBalance
{
    public static readonly TradeBalance Default = new();

    public int PerLevelBonusPercent { get; init; } = 5;
}
