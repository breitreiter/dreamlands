using System.Text.Json.Serialization;
using Dreamlands.Game;
using Dreamlands.Rules;

namespace GameServer;

// Mode-based discriminated response — the `mode` field tells the client which shape to expect.

public class GameResponse
{
    public string Mode { get; init; } = "";
    public StatusInfo Status { get; init; } = new();

    // Exploring
    public NodeInfo? Node { get; init; }
    public List<ExitInfo>? Exits { get; init; }

    // Encounter
    public EncounterInfo? Encounter { get; init; }

    // Outcome
    public OutcomeInfo? Outcome { get; init; }

    // Game over
    public string? Reason { get; init; }

    // Settlement
    public SettlementInfo? Settlement { get; init; }

    // Market order results
    public MarketOrderResultInfo? MarketResult { get; init; }

    // Always include inventory for client state
    public InventoryInfo? Inventory { get; init; }
}

public class MarketOrderResultInfo
{
    public bool Success { get; init; }
    public List<MarketLineResultInfo> Results { get; init; } = [];
}

public class MarketLineResultInfo
{
    public string Action { get; init; } = "";
    public string ItemId { get; init; } = "";
    public bool Success { get; init; }
    public string Message { get; init; } = "";
}

public class StatusInfo
{
    public int Health { get; init; }
    public int MaxHealth { get; init; }
    public int Spirits { get; init; }
    public int MaxSpirits { get; init; }
    public int Gold { get; init; }
    public string Time { get; init; } = "";
    public int Day { get; init; }
    public Dictionary<string, int> Conditions { get; init; } = new();
    public Dictionary<string, int> Skills { get; init; } = new();
}

public class NodeInfo
{
    public int X { get; init; }
    public int Y { get; init; }
    public string Terrain { get; init; } = "";
    public string? Region { get; init; }
    public int? RegionTier { get; init; }
    public string? Description { get; init; }
    public PoiInfo? Poi { get; init; }
}

public class PoiInfo
{
    public string Kind { get; init; } = "";
    public string? Name { get; init; }
    public string? DungeonId { get; init; }
    public bool? DungeonCompleted { get; init; }
}

public class ExitInfo
{
    public string Direction { get; init; } = "";
    public string Terrain { get; init; } = "";
    public string? Poi { get; init; }
}

public class EncounterInfo
{
    public string Title { get; init; } = "";
    public string Body { get; init; } = "";
    public List<ChoiceInfo> Choices { get; init; } = [];
}

public class ChoiceInfo
{
    public int Index { get; init; }
    public string Label { get; init; } = "";
    public string? Preview { get; init; }
}

public class OutcomeInfo
{
    public string? Preamble { get; init; }
    public string Text { get; init; } = "";
    public SkillCheckInfo? SkillCheck { get; init; }
    public List<MechanicResultInfo> Mechanics { get; init; } = [];
    public string NextAction { get; init; } = "end_encounter";
}

public class SkillCheckInfo
{
    public string Skill { get; init; } = "";
    public bool Passed { get; init; }
    public int Rolled { get; init; }
    public int Target { get; init; }
    public int Modifier { get; init; }
}

public class MechanicResultInfo
{
    public string Type { get; init; } = "";
    public string Description { get; init; } = "";
}

public class SettlementInfo
{
    public string Name { get; init; } = "";
    public int Tier { get; init; }
    public List<string> Services { get; init; } = [];
}

public class InventoryInfo
{
    public List<ItemInfo> Pack { get; init; } = [];
    public int PackCapacity { get; init; }
    public List<ItemInfo> Haversack { get; init; } = [];
    public int HaversackCapacity { get; init; }
    public EquipmentInfo Equipment { get; init; } = new();
}

public class ItemInfo
{
    public string DefId { get; init; } = "";
    public string Name { get; init; } = "";
    public string? Description { get; init; }
}

public class EquipmentInfo
{
    public ItemInfo? Weapon { get; init; }
    public ItemInfo? Armor { get; init; }
    public ItemInfo? Boots { get; init; }
}

// Request DTOs

public class ActionRequest
{
    public string Action { get; set; } = "";
    public string? Direction { get; set; }
    public int? ChoiceIndex { get; set; }
    public string? ItemId { get; set; }
    public int? Quantity { get; set; }
    public MarketOrderRequest? Order { get; set; }
}

public class MarketOrderRequest
{
    public List<MarketBuyLine> Buys { get; set; } = [];
    public List<MarketSellLine> Sells { get; set; } = [];
}

public class MarketBuyLine
{
    public string ItemId { get; set; } = "";
    public int Quantity { get; set; } = 1;
}

public class MarketSellLine
{
    public string ItemDefId { get; set; } = "";
}
