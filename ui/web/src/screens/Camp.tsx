import { useState, useEffect, useRef } from "react";
import { useGame, type ToastLine } from "../GameContext";
import type { GameResponse, CampEventInfo } from "../api/types";
import MaskedIcon from "../components/MaskedIcon";
import parchment from "../assets/parchment.png";

const roadIntros = [
  "It is dark, you make camp.",
  "Night falls, you set up camp.",
  "Another night, another campsite.",
  "The day is done, you make camp.",
];

const innIntro = "The interior of the inn is warm and inviting.";

const chapterhouseIntro =
  "The lobby silently attests the Merchant Guild's might. Marble floors veined with gold, walls hung with maps of trade routes that span three continents, and above it all a vaulted dome painted with a rising sun. The air smells of woodsmoke and roasting meat.";

function pickRoadIntro(day: number) {
  return roadIntros[day % roadIntros.length];
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

export default function Camp({ state }: { state: GameResponse }) {
  const { doAction, refreshState, loading, showToast } = useGame();
  const camp = state.camp;
  const node = state.node;
  const resolved = state.mode === "camp_resolved";
  const [vignetteError, setVignetteError] = useState(false);
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
  // Small delay before refreshState so Explore can mount before the toast slides in,
  // avoiding the flash from simultaneous map remount + toast render.
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

  const title = isChapterhouse
    ? "The Chapterhouse"
    : isInn
      ? "A Quiet Inn"
      : "Night on the Road";

  const intro = isChapterhouse
    ? chapterhouseIntro
    : isInn
      ? innIntro
      : pickRoadIntro(state.status.day);

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

      {/* Right panel — narrative content */}
      <div className="flex-1 overflow-y-auto p-8 md:p-12 flex flex-col">
        <div className="max-w-2xl space-y-6">
          <h2 className="text-3xl md:text-4xl font-header text-accent uppercase">
            {title}
          </h2>

          <div className="text-primary/80 leading-loose">{intro}</div>

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

          {/* Resolution events */}
          {resolved && camp && camp.events.length > 0 && (
            <div className="space-y-1 border-t border-edge pt-3">
              {camp.events.map((e, i) => (
                <div key={i} className="text-primary/80">
                  {e.description}
                </div>
              ))}
            </div>
          )}

          {/* Continue button — only after resolution */}
          {resolved && (
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
          )}
        </div>
      </div>
    </div>
  );
}
