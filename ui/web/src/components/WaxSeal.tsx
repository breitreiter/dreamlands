import type { SealVariant } from "../marketNaming";

const SEAL_COLORS: Record<SealVariant, { highlight: string; mid: string; shadow: string }> = {
  merchant: { highlight: "#d4816c", mid: "#8a3a2a", shadow: "#5a2419" },
  guild:    { highlight: "#6c81d4", mid: "#2a3a8a", shadow: "#192454" },
  crown:    { highlight: "#d4c06c", mid: "#8a7a2a", shadow: "#5a5019" },
};

const SYMBOLS = [
  "crown.svg",
  "hell-crosses.svg",
  "heraldic-sun.svg",
  "holy-symbol.svg",
  "linked-rings.svg",
  "lion.svg",
  "magic-palm.svg",
  "orbital.svg",
  "rose.svg",
  "sunrise.svg",
  "triorb.svg",
  "triple-beak.svg",
  "wyvern.svg",
];

interface WaxSealProps {
  variant?: SealVariant;
  symbolIndex?: number;
  compact?: boolean;
}

export default function WaxSeal({ variant = "merchant", symbolIndex = 0, compact = false }: WaxSealProps) {
  const c = SEAL_COLORS[variant];
  const symbol = SYMBOLS[symbolIndex % SYMBOLS.length];
  const sealUrl = "/world/assets/icons/seals/wax-seal.svg";
  const symbolUrl = `/world/assets/icons/seals/${symbol}`;

  if (compact) {
    return (
      <div style={{ width: 7, height: 7, borderRadius: "50%", backgroundColor: c.mid, flexShrink: 0 }} />
    );
  }

  const size = 52;

  return (
    <div style={{ position: "relative", width: size, height: size, flexShrink: 0 }}>
      {/* Seal blob */}
      <div
        style={{
          position: "absolute",
          inset: 0,
          backgroundColor: c.mid,
          maskImage: `url(${sealUrl})`,
          maskSize: "contain",
          maskRepeat: "no-repeat",
          maskPosition: "center",
          WebkitMaskImage: `url(${sealUrl})`,
          WebkitMaskSize: "contain",
          WebkitMaskRepeat: "no-repeat",
          WebkitMaskPosition: "center",
          filter: `drop-shadow(0 1px 3px rgba(0,0,0,.5))`,
        }}
      />
      {/* Highlight layer — lighter centre to give wax depth */}
      <div
        style={{
          position: "absolute",
          inset: 0,
          background: `radial-gradient(circle at 38% 32%, ${c.highlight}88 0%, transparent 58%)`,
          maskImage: `url(${sealUrl})`,
          maskSize: "contain",
          maskRepeat: "no-repeat",
          maskPosition: "center",
          WebkitMaskImage: `url(${sealUrl})`,
          WebkitMaskSize: "contain",
          WebkitMaskRepeat: "no-repeat",
          WebkitMaskPosition: "center",
        }}
      />
      {/* Impressed symbol */}
      <div
        style={{
          position: "absolute",
          inset: "22%",
          backgroundColor: "rgba(0,0,0,0.6)",
          maskImage: `url(${symbolUrl})`,
          maskSize: "contain",
          maskRepeat: "no-repeat",
          maskPosition: "center",
          WebkitMaskImage: `url(${symbolUrl})`,
          WebkitMaskSize: "contain",
          WebkitMaskRepeat: "no-repeat",
          WebkitMaskPosition: "center",
        }}
      />
    </div>
  );
}
