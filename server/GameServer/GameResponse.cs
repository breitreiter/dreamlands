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

    // Camp
    public CampInfo? Camp { get; init; }

    // Inn recovery summary
    public InnRecoveryInfo? InnRecovery { get; init; }

    // Haul deliveries (populated on move when arriving at a settlement)
    public List<DeliveryInfo>? Deliveries { get; init; }

    // Settlement arrival summary (populated when travel ends at a settlement
    // with cleared conditions, lost health, or lost spirits to report)
    public ArrivalInfo? Arrival { get; init; }

    // Always include inventory for client state
    public InventoryInfo? Inventory { get; init; }

    // Combat encounter
    public CombatInfo? Combat { get; init; }

    // Picker approach prompt (emitted when runner suspends on AwaitApproach)
    public ApproachPromptInfo? ApproachPrompt { get; init; }

    // Tableau level-up prompt (emitted when runner suspends on AwaitTableauPick)
    public TableauPromptInfo? TableauPrompt { get; init; }

    // Computed mechanics summary for inventory screen
    public MechanicsInfo? Mechanics { get; init; }

    // Travel (click-to-move journey results)
    public TravelInfo? Travel { get; init; }
}

public class TravelInfo
{
    /// <summary>Full path from start to destination (grid coordinates).</summary>
    public List<TravelPoint> Path { get; init; } = [];

    /// <summary>How many tiles of the path were actually traversed (1-based, includes start).</summary>
    public int StepsCompleted { get; init; }

    /// <summary>Why the journey ended: "arrived", "encounter", "rescued".</summary>
    public string StopReason { get; init; } = "arrived";
}

public class TravelPoint
{
    public int X { get; init; }
    public int Y { get; init; }
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

public class ArrivalInfo
{
    public string SettlementName { get; init; } = "";
    public int DaysElapsed { get; init; }
    public List<ArrivalLossInfo> Losses { get; init; } = [];
}

public class ArrivalLossInfo
{
    public string Cause { get; init; } = "";
    public int Health { get; init; }
    public int Spirits { get; init; }
}

public class ClearedConditionInfo
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
}

public class InnRecoveryInfo
{
    public int NightsStayed { get; init; }
    public int GoldSpent { get; init; }
    public int HealthRecovered { get; init; }
    public int SpiritsRecovered { get; init; }
    public List<string> ConditionsCleared { get; init; } = [];
    public List<string> MedicinesApplied { get; init; } = [];
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
    public string Tier { get; init; } = "";
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
}

public class ItemInfo
{
    public string DefId { get; init; } = "";
    public string Name { get; init; } = "";
    public string? Description { get; init; }
    public string Type { get; init; } = "";
    public int? Cost { get; init; }
    public Dictionary<string, int> SkillModifiers { get; init; } = [];
    public List<string> Cures { get; init; } = [];
    public List<string> Immunities { get; init; } = [];
    public List<string> Moves { get; init; } = [];
    public bool IsEquippable { get; init; }
    public bool IsEquipped { get; init; }
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

// Approach picker prompt — emitted when the runner suspends on AwaitApproach.
// The client renders the 3-approach picker UI; which approach is correct/wrong is
// intentionally withheld (that's authored data the player shouldn't see).

public class ApproachPromptInfo
{
    /// <summary>Lowercase skill id, e.g. "negotiation".</summary>
    public string Skill { get; init; } = "";

    /// <summary>Preamble prose already rendered before the picker (may be null).</summary>
    public string? Preamble { get; init; }

    /// <summary>The three approaches in their canonical roster order.</summary>
    public List<ApproachInfo> Approaches { get; init; } = [];
}

public class ApproachInfo
{
    /// <summary>Lowercase token id used as the pick_approach request value, e.g. "charm".</summary>
    public string Id { get; init; } = "";

    /// <summary>Player-facing label, e.g. "Charm".</summary>
    public string Label { get; init; } = "";

    /// <summary>SVG file hint for the approach icon (Phase 6 renders this).</summary>
    public string IconHint { get; init; } = "";
}

// Tableau level-up prompt — emitted when the runner suspends on AwaitTableauPick.
// The client renders the reward picker; correct/incorrect info is not applicable here.

public class TableauPromptInfo
{
    public int PendingLevels { get; init; }
    public List<TableauSlotInfo> Slots { get; init; } = [];
}

public class TableauSlotInfo
{
    public string Id { get; init; } = "";
    public string Label { get; init; } = "";
    public string Kind { get; init; } = "";
    public int CurrentCount { get; init; }
    public int Cap { get; init; }
    public bool IsPickable { get; init; }
    public string Tier1Description { get; init; } = "";
    public string Tier2Description { get; init; } = "";
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

public class EncounterSummary
{
    public string Id { get; init; } = "";
    public string Title { get; init; } = "";
}

public class ActionRequest
{
    public string Action { get; set; } = "";
    public string? Direction { get; set; }
    public string? InnService { get; set; } // bed | bath | full
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
    public string? RewardSlotId { get; set; }
    public List<TravelPoint>? Path { get; set; }
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

// Combat

public class CombatInfo
{
    public string EncounterId { get; init; } = "";
    public string Title { get; init; } = "";
    public string? Image { get; init; }
    public string? BiomeImage { get; init; }
    public string BloodColor { get; init; } = "#7a0a0a";
    public string IntroText { get; init; } = "";

    public int MonsterHp { get; init; }
    public int MonsterMaxHp { get; init; }

    public int PlayerSpirits { get; init; }
    public int PlayerMaxSpirits { get; init; }
    public int PlayerHealth { get; init; }
    public int PlayerMaxHealth { get; init; }
    public string PlayerWeaponClass { get; init; } = "";
    public string PlayerArmorClass { get; init; } = "";

    /// <summary>Every move the player can pick from this turn. <c>encoding</c> is the
    /// canonical mechanical form (used as the move identifier for cooldown gating and
    /// commit submission); <c>displayName</c> is the player-facing label rendered on
    /// the action button. Cooldown filtering is the client's job — pair with
    /// <see cref="PlayerLastUsedTurn"/> and <see cref="Turn"/> to gate Power/Slow moves.</summary>
    public List<MoveOption> PlayerMovePool { get; init; } = new();

    /// <summary>For each slot 1..3, true means it's locked to Skipped this turn (carry-stun).</summary>
    public List<bool> PlayerCarryStun { get; init; } = new();

    /// <summary>Encoded move → turn it was last used. Lets the client gate Power ("== current turn")
    /// and Slow ("current - 1 == last used") moves.</summary>
    public Dictionary<string, int> PlayerLastUsedTurn { get; init; } = new();

    public int Turn { get; init; }
    public string Tell { get; init; } = "";

    /// <summary>The AI's three-slot plan, surfaced when the player committed Read on
    /// the previous turn. Null otherwise.</summary>
    public List<string>? Plan { get; init; }

    public bool Resolved { get; init; }
    public bool PlayerWon { get; init; }
    public bool PlayerLost { get; init; }
    public bool PlayerFled { get; init; }
    public bool MonsterFled { get; init; }
    public string? OutcomeText { get; init; }
    public List<MechanicResultInfo>? OutcomeMechanics { get; init; }

    public List<CombatLogEntry> Events { get; init; } = [];
}

/// <summary>One option in the player's move pool: <c>Encoding</c> is the canonical
/// mechanical form used as the move identifier (cooldown gating, commit submission);
/// <c>DisplayName</c> is the authored label shown on the action button.</summary>
public sealed record MoveOption(string Encoding, string DisplayName);

/// <summary>
/// One line of combat narration. <see cref="Text"/> is the rendered form
/// (used as-is for non-roll lines); <see cref="Roll"/>, when present, lets
/// the UI render the line as the standard die-roll panel.
/// </summary>
public class CombatLogEntry
{
    public string Text { get; init; } = "";
    public CombatRollInfo? Roll { get; init; }
    public CombatNarrationInfo? Narration { get; init; }
    public PlayerAttackInfo? PlayerAttack { get; init; }

    /// <summary>1-based slot index when this entry corresponds to a SlotResolved
    /// event; null otherwise. Lets the client stage per-slot resolution playback.</summary>
    public int? Slot { get; init; }

    /// <summary>Encoded player + monster moves resolved in this slot. Populated
    /// alongside <see cref="Slot"/> so the client can reveal monster intent
    /// without re-parsing prose.</summary>
    public string? PlayerMove { get; init; }
    public string? MonsterMove { get; init; }
}

/// <summary>
/// Structured outcome of a player attack, surfaced so the client can fire
/// the appropriate hitbox animation without parsing prose. Outcome values
/// are "miss" (incl. fumble), "hit", "crit", "super_crit" (dagger-only).
/// </summary>
public class PlayerAttackInfo
{
    public string Outcome { get; init; } = "miss";
    public int? Damage { get; init; }
}

/// <summary>
/// Narrative form for a monster move that resolves into an attack: the
/// move's flavor text is the lead, then a verdict word (hit/missed) and
/// optional damage detail. UI renders verdict and detail in bold; the
/// lead is plain prose.
/// </summary>
public class CombatNarrationInfo
{
    public string Lead { get; init; } = "";       // "He drives the butt of the staff at your ribs"
    public string Verdict { get; init; } = "";    // "hit", "missed"
    public bool Hit { get; init; }                 // colors the verdict
    public string? Detail { get; init; }           // "2 damage", or null on miss
}

public class CombatRollInfo
{
    public string Label { get; init; } = "";          // "Player attack", "Cunning save"
    public string Verb { get; init; } = "";           // "attack", "save", "resist"; "" suppresses verb
    public string TargetPrefix { get; init; } = "";   // "AC ", "DC ", ""
    public int Rolled { get; init; }                  // raw d20 face
    public int Modifier { get; init; }                // total - rolled
    public int Target { get; init; }                  // AC or DC
    public bool Passed { get; init; }
    public string PassLabel { get; init; } = "Success";
    public string FailLabel { get; init; } = "Failure";
    public string? Detail { get; init; }              // trailing tag, e.g. "9 damage", "crit"
}

public class CombatIntentInfo
{
    public string MoveId { get; init; } = "";
    public string Class { get; init; } = "";
    public string Text { get; init; } = "";
}

public class CombatEncounterSummary
{
    public string Id { get; init; } = "";
    public string Title { get; init; } = "";
    public string Category { get; init; } = "";
    public int? Tier { get; init; }
    public int Hp { get; init; }
}

public class CombatListResponse
{
    public List<CombatEncounterSummary> Encounters { get; init; } = new();
}

public class CombatActionRequest
{
    /// <summary>"commit" | "flee".</summary>
    public string Action { get; set; } = "";

    /// <summary>For action=commit: exactly three move encodings, e.g. ["Big Attack", "Defend", "Read"].</summary>
    public List<string>? Slots { get; set; }
}

public class CombatBeginRequest
{
    public string EncounterId { get; set; } = "";
}
