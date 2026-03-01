export interface StatusInfo {
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
}

export interface ExitInfo {
  direction: string;
  terrain: string;
  poi: string | null;
}

export interface EncounterInfo {
  title: string;
  body: string;
  choices: ChoiceInfo[];
}

export interface ChoiceInfo {
  index: number;
  label: string;
  preview: string | null;
}

export interface OutcomeInfo {
  preamble: string | null;
  text: string;
  skillCheck: SkillCheckInfo | null;
  mechanics: MechanicResultInfo[];
  nextAction: string;
}

export interface SkillCheckInfo {
  skill: string;
  passed: boolean;
  rolled: number;
  target: number;
  modifier: number;
}

export interface MechanicResultInfo {
  type: string;
  description: string;
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

export interface CampInfo {
  threats: CampThreatInfo[];
  events: CampEventInfo[];
}

export interface GameResponse {
  mode: "exploring" | "encounter" | "outcome" | "game_over" | "camp" | "camp_resolved";
  status: StatusInfo;
  node?: NodeInfo;
  exits?: ExitInfo[];
  encounter?: EncounterInfo;
  outcome?: OutcomeInfo;
  reason?: string;
  camp?: CampInfo;
  inventory?: InventoryInfo;
  mechanics?: MechanicsInfo;
  marketResult?: MarketOrderResult;
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
  sellPrice: number;
  quantity: number;
  isFeaturedSell: boolean;
  skillModifiers: Record<string, number>;
  resistModifiers: Record<string, number>;
  description: string;
}

export interface MarketStockResponse {
  tier: number;
  stock: MarketItem[];
  sellPrices: Record<string, number>;
}

export interface MarketOrder {
  buys: { itemId: string; quantity: number }[];
  sells: { itemDefId: string }[];
}

export interface MarketOrderResult {
  success: boolean;
  results: { action: string; itemId: string; success: boolean; message: string }[];
}
