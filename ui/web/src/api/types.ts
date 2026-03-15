export interface StatusInfo {
  name: string;
  bio: string;
  health: number;
  maxHealth: number;
  spirits: number;
  maxSpirits: number;
  gold: number;
  time: string;
  day: number;
  conditions: ConditionInfo[];
  skills: SkillInfoDto[];
}

export interface ConditionInfo {
  id: string;
  name: string;
  stacks: number;
  description: string;
  effect: string;
}

export interface SkillInfoDto {
  id: string;
  name: string;
  level: number;
  formatted: string;
  flavor: string;
}

export interface NodeInfo {
  x: number;
  y: number;
  terrain: string;
  region: string | null;
  regionTier: number | null;
  description: string | null;
  poi: PoiInfo | null;
}

export interface PoiInfo {
  kind: string;
  name: string | null;
  dungeonId: string | null;
  dungeonCompleted: boolean | null;
  services?: string[];
}

export interface ExitInfo {
  direction: string;
  terrain: string;
  poi: string | null;
}

export interface EncounterInfo {
  id?: string;
  category?: string;
  vignette?: string;
  title: string;
  body: string;
  choices: ChoiceInfo[];
}

export interface ChoiceInfo {
  index: number;
  label: string;
  preview: string | null;
  locked: boolean;
  requires: string | null;
}

export interface OutcomeInfo {
  preamble: string | null;
  text: string;
  skillCheck: SkillCheckInfo | null;
  mechanics: MechanicResultInfo[];
  nextAction: string;
}

export interface SkillCheckInfo {
  kind: "check" | "meets";
  skill: string;
  passed: boolean;
  rolled: number;
  target: number;
  modifier: number;
  rollMode?: "advantage" | "disadvantage";
}

export interface MechanicResultInfo {
  type: string;
  description: string;
  resistCheck?: ResistCheckInfo;
}

export interface ResistCheckInfo {
  conditionId: string;
  conditionName: string;
  passed: boolean;
  rolled: number;
  target: number;
  modifier: number;
  rollMode?: "advantage" | "disadvantage";
}

export interface ItemInfo {
  defId: string;
  name: string;
  description: string | null;
  type: string;
  cost: number | null;
  skillModifiers: Record<string, number>;
  resistModifiers: Record<string, number>;
  foragingBonus: number;
  cures: string[];
  isEquippable: boolean;
  destinationName: string | null;
  destinationHint: string | null;
  payout: number | null;
  haulOfferId: string | null;
}

export interface EquipmentInfo {
  weapon: ItemInfo | null;
  armor: ItemInfo | null;
  boots: ItemInfo | null;
}

export interface InventoryInfo {
  pack: ItemInfo[];
  packCapacity: number;
  haversack: ItemInfo[];
  haversackCapacity: number;
  equipment: EquipmentInfo;
}

export interface CampThreatInfo {
  conditionId: string;
  name: string;
  warning: string;
}

export interface CampEventInfo {
  type: string;
  description: string;
}

export interface ConditionRowInfo {
  conditionId: string;
  name: string;
  stacks: number;
  cureItem: string | null;
  cureMessage: string | null;
  stacksAfter: number;
  healthLost: number;
  spiritsLost: number;
}

export interface CampInfo {
  hasSevereCondition: boolean;
  healthBefore: number;
  healthAfter: number;
  conditionRows: ConditionRowInfo[];
  threats: CampThreatInfo[];
  events: CampEventInfo[];
}

export interface InnRecoveryInfo {
  nightsStayed: number;
  goldSpent: number;
  healthRecovered: number;
  spiritsRecovered: number;
  conditionsCleared: string[];
  medicinesConsumed: string[];
}

export interface InnQuoteResponse {
  isChapterhouse: boolean;
  canFullRecover: boolean;
  disqualifyingConditions: string[];
  quote: {
    nights: number;
    goldCost: number;
    healthRecovered: number;
    spiritsRecovered: number;
  };
  needsRecovery: boolean;
  canAfford: boolean;
}

export interface DiscoveryInfo {
  x: number;
  y: number;
  kind: string;
  name: string;
}

export interface DeliveryInfo {
  name: string;
  payout: number;
  flavor: string | null;
}

export interface DungeonHubInfo {
  dungeonId: string;
  name: string;
  encounters: EncounterSummaryInfo[];
}

export interface EncounterSummaryInfo {
  id: string;
  title: string;
}

export interface NoticesResponse {
  encounters: EncounterSummaryInfo[];
}

export interface RescueInfo {
  lostItems: string[];
  goldLost: number;
}

export interface GameResponse {
  mode: "exploring" | "encounter" | "outcome" | "camp" | "camp_resolved" | "rescued";
  status: StatusInfo;
  node?: NodeInfo;
  exits?: ExitInfo[];
  encounter?: EncounterInfo;
  outcome?: OutcomeInfo;
  rescue?: RescueInfo;
  dungeonHub?: DungeonHubInfo;
  camp?: CampInfo;
  inventory?: InventoryInfo;
  mechanics?: MechanicsInfo;
  marketResult?: MarketOrderResult;
  innRecovery?: InnRecoveryInfo;
  deliveries?: DeliveryInfo[];
}

export interface MechanicsInfo {
  resistances: MechanicLine[];
  encounterChecks: MechanicLine[];
  other: MechanicLine[];
}

export interface MechanicLine {
  label: string;
  value: string;
  source: string;
}

export interface NewGameResponse {
  gameId: string;
  state: GameResponse;
}

export interface MarketItem {
  id: string;
  name: string;
  type: string;
  buyPrice: number;
  quantity: number;
  skillModifiers: Record<string, number>;
  resistModifiers: Record<string, number>;
  description: string;
}

export interface HaulOffer {
  id: string;
  name: string;
  destinationName: string | null;
  destinationHint: string;
  payout: number;
  originFlavor: string;
}

export interface MarketStockResponse {
  tier: number;
  stock: MarketItem[];
  hauls: HaulOffer[];
  sellPrices: Record<string, number>;
}

export interface BankResponse {
  settlementName: string;
  items: ItemInfo[];
  capacity: number;
  packFull: boolean;
  haversackFull: boolean;
}

export interface MarketOrder {
  buys: { itemId: string; quantity: number }[];
  sells: { itemDefId: string }[];
}

export interface MarketOrderResult {
  success: boolean;
  results: { action: string; itemId: string; success: boolean; message: string }[];
}
