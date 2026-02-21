export interface StatusInfo {
  health: number;
  maxHealth: number;
  spirits: number;
  maxSpirits: number;
  gold: number;
  time: string;
  day: number;
  conditions: Record<string, number>;
  skills: Record<string, number>;
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

export interface SettlementInfo {
  name: string;
  tier: number;
  services: string[];
}

export interface ItemInfo {
  defId: string;
  name: string;
  description: string | null;
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

export interface GameResponse {
  mode: "exploring" | "encounter" | "outcome" | "at_settlement" | "game_over";
  status: StatusInfo;
  node?: NodeInfo;
  exits?: ExitInfo[];
  encounter?: EncounterInfo;
  outcome?: OutcomeInfo;
  reason?: string;
  settlement?: SettlementInfo;
  inventory?: InventoryInfo;
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
  skillModifiers: Record<string, number>;
  resistModifiers: Record<string, number>;
  description: string;
}

export interface MarketStockResponse {
  tier: number;
  stock: MarketItem[];
}

export interface MarketActionResponse {
  success: boolean;
  message: string;
  status: StatusInfo;
  inventory: InventoryInfo;
}
