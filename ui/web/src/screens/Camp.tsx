import { useState, useEffect, useRef } from "react";
import { useGame } from "../GameContext";
import type { GameResponse } from "../api/types";
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

export default function Camp({ state }: { state: GameResponse }) {
  const { doAction, refreshState, loading } = useGame();
  const camp = state.camp;
  const node = state.node;
  const resolved = state.mode === "camp_resolved";
  const [vignetteError, setVignetteError] = useState(false);
  const didResolve = useRef(false);

  // Auto-resolve on mount — no preamble screen
  useEffect(() => {
    if (!resolved && !didResolve.current) {
      didResolve.current = true;
      doAction({ action: "camp_resolve" });
    }
  }, [resolved, doAction]);

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

  return (
    <div className="h-full flex bg-page text-primary">
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
