// Deterministic market name / proprietor generation from settlement name + terrain

function seededRng(seed: number) {
  let s = seed | 0;
  return () => {
    s = (Math.imul(s, 1664525) + 1013904223) | 0;
    return (s >>> 0) / 0x100000000;
  };
}

function hashStr(str: string): number {
  let h = 2166136261;
  for (let i = 0; i < str.length; i++) {
    h = Math.imul(h ^ str.charCodeAt(i), 16777619);
  }
  return h >>> 0;
}

function pick<T>(rng: () => number, arr: readonly T[]): T {
  return arr[Math.floor(rng() * arr.length)];
}

const GRAMMARS: Record<string, { stalls: string[]; givenNames: string[]; epithets: string[] }> = {
  plains: {
    stalls: ["Grand Bazaar", "Corner Market", "Weigh-House", "Cloth Yard", "Customs Hall", "Caravan Stall", "Toll-House"],
    givenNames: ["Ilesa", "Bren", "Mira", "Savin", "Orith", "Dunna", "Corvith", "Petra"],
    epithets: ["the Factor", "the Weigher", "the Long-Tongue", "the Broker", "the Appraiser", "the Tallier", "the Ledger-Keeper"],
  },
  mountains: {
    stalls: ["Trading Post", "Factor's Hall", "Exchange", "Toll-House", "Gear-House", "Pack-House"],
    givenNames: ["Harlen", "Darvish", "Sorine", "Mord", "Kavan", "Theria", "Brix"],
    epithets: ["the Appraiser", "the Broker", "the Exchanger", "the Factor", "the Bargainer", "the Weigher"],
  },
  forest: {
    stalls: ["Corner Market", "Factor's Hall", "Wood-Yard", "Bark & Bale", "Timber Market", "Toll-House"],
    givenNames: ["Wren", "Faur", "Elset", "Briva", "Toven", "Ceth", "Osna"],
    epithets: ["the Tallier", "the Wood-Factor", "the Trader", "the Bark-Broker", "the Ledger-Keeper", "the Weigher"],
  },
  scrub: {
    stalls: ["Caravan Stall", "Dust Market", "Way-Stall", "Exchange", "Drover's Market", "Toll-House"],
    givenNames: ["Kess", "Vanda", "Rim", "Sarith", "Deln", "Fora", "Besk"],
    epithets: ["the Drifter-Factor", "the Way-Broker", "the Exchanger", "the Drover", "the Haul-Keeper", "the Tallier"],
  },
  swamp: {
    stalls: ["Dock Market", "Fish Steps", "Wharfside Market", "Factor's Hall", "Tallow Yard", "Reed Market"],
    givenNames: ["Torren", "Maise", "Oll", "Fenna", "Drak", "Veth", "Losse"],
    epithets: ["the Dockman", "the Wharfinger", "the Fish-Factor", "the Bargeman", "the Reed-Broker", "the Tallier"],
  },
};

function grammar(terrain: string | null) {
  const key = (terrain ?? "").toLowerCase();
  return GRAMMARS[key] ?? GRAMMARS.plains;
}

export function getMarketName(settlementName: string, terrain: string | null): string {
  const rng = seededRng(hashStr(settlementName + ":market"));
  const stall = pick(rng, grammar(terrain).stalls);
  return `${settlementName} ${stall}`;
}

export function getProprietorName(settlementName: string, terrain: string | null): string {
  const rng = seededRng(hashStr(settlementName + ":prop"));
  const g = grammar(terrain);
  const name = pick(rng, g.givenNames);
  const epithet = pick(rng, g.epithets);
  // ~20% chance of "Old {name}" regardless of terrain
  if (rng() < 0.2) return `Old ${name}`;
  return `${name} ${epithet}`;
}

// ── Wax seal derivation for haul contracts ────────────────────────────────────

export type SealVariant = "merchant" | "guild" | "crown";

const SEAL_VARIANTS: SealVariant[] = ["merchant", "guild", "crown"];
const SYMBOL_COUNT = 13;

export function getSealVariant(haulId: string): SealVariant {
  return SEAL_VARIANTS[hashStr(haulId) % SEAL_VARIANTS.length];
}

export function getSealSymbolIndex(haulId: string): number {
  // Use a different hash mix so variant and symbol are independent
  return hashStr(haulId + ":sym") % SYMBOL_COUNT;
}
