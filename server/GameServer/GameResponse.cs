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
    public OutcomeInfo? Outcome { get; set; }

    // Rescue (death → rescue at chapterhouse)
    public RescueInfo? Rescue { get; init; }

    // Market order results
    public MarketOrderResultInfo? MarketResult { get; init; }

    // Dungeon hub
    public DungeonHubInfo? DungeonHub { get; init; }

    // Camp
    public CampInfo? Camp { get; init; }

    // Inn recovery summary
    public InnRecoveryInfo? InnRecovery { get; init; }

    // Haul deliveries (populated on move when arriving at a settlement)
    public List<DeliveryInfo>? Deliveries { get; init; }

    // Always include inventory for client state
    public InventoryInfo? Inventory { get; init; }

    // Tactical encounter
    public TacticalInfo? Tactical { get; init; }

    // Computed mechanics summary for inventory screen
    public MechanicsInfo? Mechanics { get; init; }
}

public class DiscoveryInfo
{
    public int X { get; init; }
    public int Y { get; init; }
    public string Kind { get; init; } = "";
    public string Name { get; init; } = "";
}

public class DeliveryInfo
{
    public string Name { get; init; } = "";
    public int Payout { get; init; }
    public string? Flavor { get; init; }
}

public class InnRecoveryInfo
{
    public int NightsStayed { get; init; }
    public int GoldSpent { get; init; }
    public int HealthRecovered { get; init; }
    public int SpiritsRecovered { get; init; }
    public List<string> ConditionsCleared { get; init; } = [];
    public List<string> MedicinesConsumed { get; init; } = [];
}

public class RescueInfo
{
    public List<string> LostItems { get; init; } = [];
    public int GoldLost { get; init; }
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
    public string Name { get; init; } = "";
    public string Bio { get; init; } = "";
    public int Health { get; init; }
    public int MaxHealth { get; init; }
    public int Spirits { get; init; }
    public int MaxSpirits { get; init; }
    public int Gold { get; init; }
    public string Time { get; init; } = "";
    public int Day { get; init; }
    public List<ConditionInfo> Conditions { get; init; } = [];
    public List<SkillInfoDto> Skills { get; init; } = [];
}

public class ConditionInfo
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public int Stacks { get; init; }
    public string Description { get; init; } = "";
    public string Effect { get; init; } = "";
}

public class SkillInfoDto
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public int Level { get; init; }
    public string Formatted { get; init; } = "";
    public string Flavor { get; init; } = "";
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
    public List<string>? Services { get; init; }
}

public class ExitInfo
{
    public string Direction { get; init; } = "";
    public string Terrain { get; init; } = "";
    public string? Poi { get; init; }
}

public class EncounterInfo
{
    public string? Id { get; init; }
    public string? Category { get; init; }
    public string? Vignette { get; init; }
    public string Title { get; init; } = "";
    public string Body { get; init; } = "";
    public List<ChoiceInfo> Choices { get; init; } = [];
}

public class ChoiceInfo
{
    public int Index { get; init; }
    public string Label { get; init; } = "";
    public string? Preview { get; init; }
    public bool Locked { get; init; }
    public string? Requires { get; init; }
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
    public string Kind { get; init; } = "check";
    public string Skill { get; init; } = "";
    public bool Passed { get; init; }
    public int Rolled { get; init; }
    public int Target { get; init; }
    public int Modifier { get; init; }
    public string? RollMode { get; init; }
}

public class MechanicResultInfo
{
    public string Type { get; init; } = "";
    public string Description { get; init; } = "";
    public ResistCheckInfo? ResistCheck { get; init; }
}

public class ResistCheckInfo
{
    public string ConditionId { get; init; } = "";
    public string ConditionName { get; init; } = "";
    public bool Passed { get; init; }
    public int Rolled { get; init; }
    public int Target { get; init; }
    public int Modifier { get; init; }
    public string? RollMode { get; init; }
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
    public string Type { get; init; } = "";
    public int? Cost { get; init; }
    public Dictionary<string, int> SkillModifiers { get; init; } = [];
    public Dictionary<string, int> ResistModifiers { get; init; } = [];
    public int ForagingBonus { get; init; }
    public List<string> Cures { get; init; } = [];
    public bool IsEquippable { get; init; }
    public string? DestinationName { get; init; }
    public string? DestinationHint { get; init; }
    public int? Payout { get; init; }
    public string? HaulOfferId { get; init; }
}

public class MechanicsInfo
{
    public List<MechanicLine> Resistances { get; init; } = [];
    public List<MechanicLine> EncounterChecks { get; init; } = [];
    public List<MechanicLine> Other { get; init; } = [];
}

public class MechanicLine
{
    public string Label { get; init; } = "";
    public string Value { get; init; } = "";
    public string Source { get; init; } = "";
}

public class EquipmentInfo
{
    public ItemInfo? Weapon { get; init; }
    public ItemInfo? Armor { get; init; }
    public ItemInfo? Boots { get; init; }
}

// Request DTOs

public class CampInfo
{
    public bool HasSevereCondition { get; init; }
    public int HealthBefore { get; init; }
    public int HealthAfter { get; init; }
    public List<ConditionRowInfo> ConditionRows { get; init; } = [];
    public List<CampThreatInfo> Threats { get; init; } = [];
    public List<CampEventInfo> Events { get; init; } = [];
}

public class ConditionRowInfo
{
    public string ConditionId { get; init; } = "";
    public string Name { get; init; } = "";
    public int Stacks { get; init; }
    public string? CureItem { get; init; }
    public string? CureMessage { get; init; }
    public int StacksAfter { get; init; }
    public int HealthLost { get; init; }
    public int SpiritsLost { get; init; }
}

public class CampThreatInfo
{
    public string ConditionId { get; init; } = "";
    public string Name { get; init; } = "";
    public string Warning { get; init; } = "";
}

public class CampEventInfo
{
    public string Type { get; init; } = "";
    public string Description { get; init; } = "";
}

public class DungeonHubInfo
{
    public string DungeonId { get; init; } = "";
    public string Name { get; init; } = "";
    public List<EncounterSummary> Encounters { get; init; } = [];
}

public class EncounterSummary
{
    public string Id { get; init; } = "";
    public string Title { get; init; } = "";
}

// ── Tactical encounter DTOs ──────────────────────────────────

public record TacticalInfo
{
    public string Phase { get; init; } = "";  // approach, turn, finished
    public string Title { get; init; } = "";
    public string Body { get; init; } = "";
    public string Variant { get; init; } = "";
    public string? Intent { get; init; }

    // approach phase
    public List<TacticalApproachInfo>? Approaches { get; init; }

    // turn phase
    public TacticalTurnInfo? Turn { get; init; }

    // finished phase
    public string? FinishReason { get; init; }
    public string? FailureText { get; init; }
    public string? SuccessText { get; init; }
    public List<MechanicResultInfo>? FailureMechanics { get; init; }
    public List<MechanicResultInfo>? SuccessMechanics { get; init; }
    public List<MechanicResultInfo>? ConditionResults { get; init; }
}

public class TacticalApproachInfo
{
    public string Kind { get; init; } = "";
    public int Momentum { get; init; }
    public int TimerCount { get; init; }
    public int BonusOpenings { get; init; }
}

public class TacticalTurnInfo
{
    public int Turn { get; init; }
    public int Resistance { get; init; }
    public int ResistanceMax { get; init; }
    public int Momentum { get; init; }
    public int Spirits { get; init; }
    public List<TacticalTimerInfo> Timers { get; init; } = [];
    public List<TacticalOpeningInfo> Openings { get; init; } = [];
    public List<TacticalOpeningInfo>? Queue { get; init; }
}

public class TacticalTimerInfo
{
    public string Name { get; init; } = "";
    public string? CounterName { get; init; }
    public string Effect { get; init; } = "";
    public int Amount { get; init; }
    public int Countdown { get; init; }
    public int Current { get; init; }
    public bool Stopped { get; init; }
    public string? ConditionId { get; init; }
}

public class TacticalOpeningInfo
{
    public string Name { get; init; } = "";
    public string CostKind { get; init; } = "";
    public int CostAmount { get; init; }
    public string EffectKind { get; init; } = "";
    public int EffectAmount { get; init; }
    public int? StopsTimerIndex { get; init; }
}

public class ActionRequest
{
    public string Action { get; set; } = "";
    public string? Direction { get; set; }
    public int? ChoiceIndex { get; set; }
    public string? ItemId { get; set; }
    public string? EncounterId { get; set; }
    public string? Slot { get; set; }
    public int? Quantity { get; set; }
    public MarketOrderRequest? Order { get; set; }
    public string? Source { get; set; }
    public int? BankIndex { get; set; }
    public int? OfferIndex { get; set; }
    public string? OfferId { get; set; }
    public string? Approach { get; set; }
    public string? TacticalAction { get; set; }
    public int? OpeningIndex { get; set; }
}

public class DebugConditionRequest
{
    public string Condition { get; set; } = "";
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
