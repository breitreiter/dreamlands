import { useState } from "react";
import { useGame } from "../GameContext";
import { formatDateTime } from "../calendar";
import type { GameResponse } from "../api/types";
import parchment from "../assets/parchment.png";

export default function Encounter({ state }: { state: GameResponse }) {
  const { doAction, loading } = useGame();
  const { encounter, node, status } = state;
  const [vignetteError, setVignetteError] = useState(false);

  if (!encounter) return null;

  const hasVignette =
    !vignetteError &&
    node?.terrain &&
    node.regionTier != null &&
    node.regionTier > 0;

  return (
    <div className="h-full flex bg-page text-primary">
      {/* Left panel — vignette over parchment */}
      <div
        className="hidden md:block w-[45%] shrink-0"
        style={{ backgroundImage: `url(${parchment})`, backgroundSize: "cover", backgroundPosition: "center" }}
      >
        {hasVignette && (
          <img
            src={`/world/assets/vignettes/${node!.terrain}/${node!.terrain}_tier_${node!.regionTier}_1.png`}
            alt=""
            className="w-full h-full object-cover"
            onError={() => setVignetteError(true)}
          />
        )}
      </div>

      {/* Right panel — narrative content */}
      <div className="flex-1 overflow-y-auto p-8 md:p-12 flex flex-col">
        <div className="max-w-2xl space-y-6">
          <div>
            <h2 className="text-3xl md:text-4xl font-header text-accent uppercase">
              {encounter.title}
            </h2>
            {node?.region && status && (
              <p className="text-dim text-sm mt-1">
                {node.region}, {formatDateTime(status.day, status.time)}
              </p>
            )}
          </div>

          {encounter.body && (
            <div className="text-primary/80 leading-loose whitespace-pre-wrap">
              {encounter.body}
            </div>
          )}

          <div className="space-y-4 pt-2">
            {encounter.choices.map((choice) => (
              <button
                key={choice.index}
                onClick={() =>
                  doAction({ action: "choose", choiceIndex: choice.index })
                }
                disabled={loading}
                className="w-full text-left flex items-start gap-3
                           disabled:text-muted transition-colors group cursor-pointer"
              >
                <img
                  src="/world/assets/icons/sun.svg"
                  alt=""
                  className="w-4 h-4 mt-1 shrink-0 opacity-70 group-hover:opacity-100
                             transition-opacity"
                />
                <span>
                  <span className="font-bold text-action group-hover:text-action-hover transition-colors">
                    {choice.label}
                  </span>
                  {choice.preview && (
                    <span className="block text-sm text-action-dim mt-0.5">
                      {choice.preview}
                    </span>
                  )}
                </span>
              </button>
            ))}

            {encounter.choices.length === 0 && (
              <button
                onClick={() => doAction({ action: "end_encounter" })}
                className="flex items-start gap-3 transition-colors group cursor-pointer"
              >
                <img
                  src="/world/assets/icons/sun.svg"
                  alt=""
                  className="w-4 h-4 mt-1 shrink-0 opacity-70 group-hover:opacity-100
                             transition-opacity"
                />
                <span className="font-bold text-action group-hover:text-action-hover transition-colors">
                  Continue
                </span>
              </button>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
