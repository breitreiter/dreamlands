namespace Dreamlands.Rules;

/// <summary>All balance data.</summary>
public sealed class BalanceData
{
    public static readonly BalanceData Default = new();

    public CharacterBalance Character { get; init; } = CharacterBalance.Default;
    public IReadOnlyDictionary<string, ConditionDef> Conditions { get; init; } = ConditionDef.All;
    public IReadOnlyDictionary<string, ItemDef> Items { get; init; } = ItemDef.All;
    public IReadOnlyDictionary<string, ConditionFlavor> ConditionFlavors { get; init; } = ConditionFlavor.All;
    public ForagingRules Foraging { get; init; } = new();
    public TradeBalance Trade { get; init; } = TradeBalance.Default;
    public SettlementBalance Settlements { get; init; } = SettlementBalance.Default;
}
