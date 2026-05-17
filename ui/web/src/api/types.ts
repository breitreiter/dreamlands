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
  tier: string;
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
  cures: string[];
  immunities: string[];
  moves: string[];
  isEquippable: boolean;
  isEquipped: boolean;
  destinationName: string | null;
  destinationHint: string | null;
  payout: number | null;
  haulOfferId: string | null;
}

export interface InventoryInfo {
  pack: ItemInfo[];
  packCapacity: number;
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
  medicinesApplied: string[];
}

export interface InnServiceInfo {
  id: "bed" | "bath" | "full";
  name: string;
  cost: number;
  spirits: number;
  restoresFull: boolean;
  canAfford: boolean;
}

export interface InnServicesResponse {
  isChapterhouse: boolean;
  needsRecovery: boolean;
  services: InnServiceInfo[];
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

export interface ClearedConditionInfo {
  id: string;
  name: string;
}

export interface ArrivalInfo {
  settlementName: string;
  daysElapsed: number;
  conditionsCleared: ClearedConditionInfo[];
  healthBefore: number;
  healthAfter: number;
  spiritsBefore: number;
  spiritsAfter: number;
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

export interface ApproachPromptInfo {
  skill: string;
  preamble: string | null;
  approaches: Array<{ id: string; label: string; iconHint: string }>;
}

export interface TableauSlotInfo {
  id: string;
  label: string;
  kind: "skill" | "health" | "inventory";
  currentCount: number;
  cap: number;
  isPickable: boolean;
  tier1Description: string;
  tier2Description: string;
}

export interface TableauPromptInfo {
  pendingLevels: number;
  slots: TableauSlotInfo[];
}

export interface GameResponse {
  mode: "exploring" | "encounter" | "outcome" | "camp" | "camp_resolved" | "rescued" | "combat" | "combat_resolved" | "approach_prompt" | "tableau_prompt";
  status: StatusInfo;
  node?: NodeInfo;
  exits?: ExitInfo[];
  encounter?: EncounterInfo;
  outcome?: OutcomeInfo;
  rescue?: RescueInfo;
  camp?: CampInfo;
  inventory?: InventoryInfo;
  mechanics?: MechanicsInfo;
  marketResult?: MarketOrderResult;
  innRecovery?: InnRecoveryInfo;
  deliveries?: DeliveryInfo[];
  arrival?: ArrivalInfo;
  combat?: CombatInfo;
  travel?: TravelInfo;
  approachPrompt?: ApproachPromptInfo;
  tableauPrompt?: TableauPromptInfo;
}

export interface CombatEncounterSummary {
  id: string;
  title: string;
  category: string;
  tier: number | null;
  hp: number;
}

export interface CombatListResponse {
  encounters: CombatEncounterSummary[];
}

export interface CombatInfo {
  encounterId: string;
  title: string;
  image: string | null;
  biomeImage: string | null;
  bloodColor: string;
  introText: string;

  monsterHp: number;
  monsterMaxHp: number;

  playerSpirits: number;
  playerMaxSpirits: number;
  playerHealth: number;
  playerMaxHealth: number;
  playerWeaponClass: string;
  playerArmorClass: string;

  /** Every move the player can pick from. encoding is the canonical mechanical form
   *  (used as the move identifier — selections, cooldown keys); displayName is the
   *  authored label shown on the action button. Cooldowns filtered client-side. */
  playerMovePool: { encoding: string; displayName: string }[];
  /** Per-slot lockout: if true, that slot is forced to Skipped (carry-stun). */
  playerCarryStun: boolean[];
  /** encoded move → turn number when last used. Lets us gate Power/Slow. */
  playerLastUsedTurn: Record<string, number>;

  turn: number;
  tell: string;
  /** AI's three-slot commit, surfaced when the player committed Read on the prior turn. */
  plan: string[] | null;

  resolved: boolean;
  playerWon: boolean;
  playerLost: boolean;
  playerFled: boolean;
  monsterFled: boolean;
  outcomeText: string | null;
  outcomeMechanics: MechanicResultInfo[] | null;

  events: CombatLogEntry[];
}

export interface CombatLogEntry {
  text: string;
  roll?: CombatRollInfo;
  narration?: CombatNarrationInfo;
  playerAttack?: PlayerAttackInfo;
  /** 1-based slot index for SlotResolved entries; null for everything else. */
  slot?: number;
  playerMove?: string;
  monsterMove?: string;
}

export interface PlayerAttackInfo {
  outcome: "miss" | "hit" | "crit" | "super_crit";
  damage?: number;
}

export interface CombatNarrationInfo {
  lead: string;
  verdict: string;
  hit: boolean;
  detail?: string;
}

export interface CombatRollInfo {
  label: string;
  verb: string;
  targetPrefix: string;
  rolled: number;
  modifier: number;
  target: number;
  passed: boolean;
  passLabel: string;
  failLabel: string;
  detail?: string;
}

export interface TravelInfo {
  path: { x: number; y: number }[];
  stepsCompleted: number;
  stopReason: "arrived" | "encounter" | "rescued";
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
  requiredCombat: number;
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
  rationCost: number;
}

export interface BankResponse {
  settlementName: string;
  items: ItemInfo[];
  capacity: number;
  packFull: boolean;
}

export interface MarketOrder {
  buys: { itemId: string; quantity: number }[];
  sells: { itemDefId: string }[];
}

export interface MarketOrderResult {
  success: boolean;
  results: { action: string; itemId: string; success: boolean; message: string }[];
}

