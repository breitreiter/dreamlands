import { useState, useEffect, useRef } from "react";
import { useGame, type ToastLine } from "../GameContext";
import type { GameResponse, CampEventInfo, ConditionRowInfo } from "../api/types";
import MaskedIcon, { iconUrl } from "../components/MaskedIcon";
import parchment from "../assets/parchment.png";

const CONDITION_ICONS: Record<string, string> = {
  freezing: "mountains.svg",
  thirsty: "water-drop.svg",
  lattice_sickness: "foamy-disc.svg",
  poisoned: "foamy-disc.svg",
  irradiated: "foamy-disc.svg",
  exhausted: "tread.svg",
  lost: "compass.svg",
  injured: "bloody-stash.svg",
  disheartened: "sensuousness.svg",
};

function crisisTitle(health: number): string {
  if (health <= 1) return "Things Are Dire";
  if (health === 2) return "You Are Dying";
  if (health === 3) return "Another Grim Night";
  return "A Serious Problem";
}

function crisisSubtitle(rows: ConditionRowInfo[]): string {
  const untreated = rows.filter(r => r.stacksAfter > 0);
  if (untreated.length === 0)
    return "Your conditions have been treated. You will recover.";
  return "You have a serious condition. You will lose health every night until the condition is treated.";
}

function buildToastLines(events: CampEventInfo[]): ToastLine[] {
  const lines: ToastLine[] = [];
  for (const e of events) {
    if (e.type === "FoodConsumed" || e.type === "Starving")
      lines.push({ text: e.description, color: e.type === "Starving" ? "negative" : undefined });
    else if (e.type === "ConditionDrain")
      lines.push({ text: e.description, color: "negative" });
    else if (e.type === "RestRecovery")
      lines.push({ text: e.description, color: "positive" });
  }
  return lines;
}

/** Minor events: food, foraging, rest recovery — not condition-related */
function getMinorEvents(events: CampEventInfo[]): CampEventInfo[] {
  const minorTypes = new Set([
    "FoodConsumed", "Starving", "Foraged", "RestRecovery",
    "ResistPassed", "ResistFailed", "ConditionAcquired",
    "DisheartendGained", "DisheartendCleared",
  ]);
  return events.filter(e => minorTypes.has(e.type));
}

export default function Camp({ state }: { state: GameResponse }) {
  const { doAction, refreshState, loading, showToast } = useGame();
  const camp = state.camp;
  const node = state.node;
  const resolved = state.mode === "camp_resolved";
  const [vignetteError, setVignetteError] = useState(false);
  const [minorExpanded, setMinorExpanded] = useState(false);
  const didResolve = useRef(false);
  const didToast = useRef(false);

  // Auto-resolve on mount — no preamble screen
  useEffect(() => {
    if (!resolved && !didResolve.current) {
      didResolve.current = true;
      doAction({ action: "camp_resolve" });
    }
  }, [resolved, doAction]);

  // Toast path: minor conditions only — skip crisis screen.
  useEffect(() => {
    if (resolved && camp && !camp.hasSevereCondition && !didToast.current) {
      didToast.current = true;
      const lines = buildToastLines(camp.events);
      if (lines.length > 0) showToast({ lines });
      setTimeout(() => refreshState(), 50);
    }
  }, [resolved, camp, showToast, refreshState]);

  const isSettlement = node?.poi?.kind === "settlement";
  const isChapterhouse = isSettlement && node?.poi?.services?.includes("chapterhouse");
  const isInn = isSettlement && !isChapterhouse;

  const terrain = node?.terrain ?? "plains";
  const vignetteSrc = isChapterhouse
    ? "/world/assets/vignettes/chapterhouse_camp.png"
    : isInn
      ? "/world/assets/vignettes/inn_camp.png"
      : `/world/assets/vignettes/${terrain}/${terrain}_camp.png`;

  // Close with Enter key once resolved
  useEffect(() => {
    if (!resolved) return;
    const handler = (e: KeyboardEvent) => {
      if (e.key === "Enter") refreshState();
    };
    window.addEventListener("keydown", handler);
    return () => window.removeEventListener("keydown", handler);
  }, [resolved, refreshState]);

  // Pre-resolve or toast path — Explore is visible underneath, render nothing
  if (!resolved || didToast.current) return null;

  const conditionRows = camp?.conditionRows ?? [];
  const healthBefore = camp?.healthBefore ?? state.status.health;
  const healthAfter = camp?.healthAfter ?? state.status.health;
  const healthDelta = healthAfter - healthBefore;
  const minorEvents = camp ? getMinorEvents(camp.events) : [];

  return (
    <div className="absolute inset-0 z-[1100] flex bg-page text-primary">
      {/* Left panel — vignette over parchment */}
      <div
        className="hidden md:block w-[45%] shrink-0"
        style={{
          backgroundImage: `url(${parchment})`,
          backgroundSize: "cover",
          backgroundPosition: "center",
        }}
      >
        {!vignetteError && (
          <img
            src={vignetteSrc}
            alt=""
            className="w-full h-full object-cover"
            onError={() => setVignetteError(true)}
          />
        )}
      </div>

      {/* Right panel — crisis content */}
      <div className="flex-1 overflow-y-auto p-8 md:p-12 flex flex-col">
        <div className="max-w-2xl space-y-6">
          {/* Title */}
          <h2 className="text-3xl md:text-4xl font-header text-negative uppercase">
            {crisisTitle(healthAfter)}
          </h2>

          {/* Subtitle */}
          <div className="text-primary/80 leading-loose">
            {crisisSubtitle(conditionRows)}
          </div>

          {/* Haul deliveries */}
          {state.deliveries && state.deliveries.length > 0 && (
            <div className="space-y-3 border-t border-edge pt-3">
              <div className="text-accent font-bold">Delivery Complete</div>
              {state.deliveries.map((d, i) => (
                <div key={i} className="flex flex-col gap-1">
                  <div className="flex items-center gap-2">
                    <MaskedIcon icon="wooden-crate.svg" className="w-5 h-5" color="#D0BD62" />
                    <span className="text-accent font-bold">{d.name}</span>
                  </div>
                  {d.flavor && (
                    <div className="text-primary/80 leading-relaxed ml-7">{d.flavor}</div>
                  )}
                  <div className="flex items-center gap-2 text-accent ml-7">
                    <MaskedIcon icon="two-coins.svg" className="w-4 h-4" color="#D0BD62" />
                    <span>+{d.payout} gold</span>
                  </div>
                </div>
              ))}
            </div>
          )}

          {/* Situation table */}
          <div className="border border-edge rounded-lg overflow-hidden">
            {/* Health in the evening */}
            <div className="flex items-center justify-between px-4 py-3 bg-btn">
              <span className="text-dim">Health in the evening</span>
              <span className="font-bold">{healthBefore}</span>
            </div>

            {/* Condition rows */}
            {conditionRows.map((row) => (
              <div key={row.conditionId} className="border-t border-edge px-4 py-3 space-y-1 bg-panel">
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-2">
                    <img
                      src={iconUrl(CONDITION_ICONS[row.conditionId] || "sun.svg")}
                      alt=""
                      className="w-5 h-5"
                    />
                    <span className="text-negative font-bold">
                      {row.name} x {row.stacks}
                      {row.cureItem && <span> → x {row.stacksAfter}</span>}
                    </span>
                  </div>
                  <span className="text-negative font-bold">
                    {row.healthLost > 0 ? `−${row.healthLost}` : ""}
                    {row.spiritsLost > 0 ? `${row.healthLost > 0 ? ", " : ""}−${row.spiritsLost} spirits` : ""}
                    {row.healthLost === 0 && row.spiritsLost === 0 ? "—" : ""}
                  </span>
                </div>
                <div className="text-dim ml-7">
                  {row.cureMessage}
                </div>
              </div>
            ))}

            {/* Health by morning */}
            <div className="flex items-center justify-between px-4 py-3 border-t border-edge bg-btn">
              <span className="text-dim">Health by morning</span>
              <span className={`font-bold ${healthDelta < 0 ? "text-negative" : ""}`}>
                {healthAfter}
              </span>
            </div>
          </div>

          {/* Minor events — collapsed, dimmed */}
          {minorEvents.length > 0 && (
            <div className="border-t border-edge pt-3">
              <button
                onClick={() => setMinorExpanded(!minorExpanded)}
                className="text-muted cursor-pointer hover:text-dim transition-colors"
              >
                {minorExpanded ? "Hide details" : "Show details"} ({minorEvents.length})
              </button>
              {minorExpanded && (
                <div className="mt-2 space-y-1">
                  {minorEvents.map((e, i) => (
                    <div key={i} className="text-muted">{e.description}</div>
                  ))}
                </div>
              )}
            </div>
          )}

          {/* Continue button */}
          <button
            onClick={() => refreshState()}
            disabled={loading}
            className="flex items-start gap-3 transition-colors group cursor-pointer"
          >
            <img
              src="/world/assets/icons/sun.svg"
              alt=""
              className="w-4 h-4 mt-1 shrink-0 opacity-70 group-hover:opacity-100 transition-opacity"
            />
            <span className="font-bold text-action group-hover:text-action-hover transition-colors">
              Continue
            </span>
          </button>
        </div>
      </div>
    </div>
  );
}
