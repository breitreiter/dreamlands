namespace Dreamlands.Rules;

/// <summary>All balance data loaded from YAML files.</summary>
public sealed class BalanceData
{
    public CharacterBalance Character { get; init; } = new();
    public IReadOnlyDictionary<string, ConditionDef> Conditions { get; init; } = new Dictionary<string, ConditionDef>();
    public IReadOnlyDictionary<string, ItemDef> Items { get; init; } = new Dictionary<string, ItemDef>();
    public FoodBalance Food { get; init; } = new();
    public CombatBalance Combat { get; init; } = new();
    public TradeBalance Trade { get; init; } = new();
    public SettlementBalance Settlements { get; init; } = new();

    /// <summary>
    /// Load all balance data from a directory containing YAML files.
    /// </summary>
    public static BalanceData Load(string balancePath) => new()
    {
        Character = CharacterBalance.Load(balancePath),
        Conditions = ConditionDef.Load(balancePath),
        Items = ItemDef.Load(balancePath),
        Food = FoodBalance.Load(balancePath),
        Combat = CombatBalance.Load(balancePath),
        Trade = TradeBalance.Load(balancePath),
        Settlements = SettlementBalance.Load(balancePath),
    };
}
