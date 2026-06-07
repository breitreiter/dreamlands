import type { TravailLineInfo } from "../api/types";
import MaskedIcon from "./MaskedIcon";

/**
 * Compact journey-toll summary shown when travel was cut short by an
 * encounter, combat, or a camp interruption. Lines come pre-written from
 * the server (Travails.Summarize); zero-cost exposure lines are dropped.
 */
export default function TravailStrip({ travails }: { travails?: TravailLineInfo[] }) {
  const lines = travails?.filter((t) => t.spiritsLost > 0 || t.sparedByGear) ?? [];
  if (lines.length === 0) return null;
  return (
    <div className="border border-edge rounded-lg bg-panel px-4 py-2.5 flex flex-col gap-1">
      <div className="text-muted text-[15px] tracking-wide">The road so far</div>
      {lines.map((t, i) => (
        <div key={i} className="flex items-center gap-2 text-dim">
          <MaskedIcon
            icon={t.sparedByGear ? "checked-shield.svg" : "sensuousness.svg"}
            className="w-4 h-4 shrink-0"
            color={t.sparedByGear ? "#D0BD62" : "#d4c9a8"}
          />
          <span>{t.text}</span>
        </div>
      ))}
    </div>
  );
}
