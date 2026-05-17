import { useState } from "react";
import { useGame } from "../GameContext";
import type { DungeonHubInfo } from "../api/types";
import { Button } from "@/components/ui/button";
import parchment from "../assets/parchment.webp";

export default function DungeonHub({ hub }: { hub: DungeonHubInfo }) {
  const { doAction, loading } = useGame();
  const [vignetteError, setVignetteError] = useState(false);

  const vignetteSrc = hub.vignette
    ? `/world/assets/vignettes/${hub.vignette}.webp`
    : null;
  const hasVignette = !vignetteError && vignetteSrc != null;

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
        {hasVignette && (
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
          <h2 className="text-3xl md:text-4xl font-header text-accent">
            {hub.name}
          </h2>

          {hub.encounters.length > 0 ? (
            <div className="space-y-4 pt-2">
              {hub.encounters.map((enc) => (
                <button
                  key={enc.id}
                  onClick={() => doAction({ action: "start_encounter", encounterId: enc.id })}
                  disabled={loading}
                  className="w-full text-left flex items-start gap-3 transition-colors group cursor-pointer disabled:opacity-50"
                >
                  <img
                    src="/world/assets/icons/sun.svg"
                    alt=""
                    className="w-4 h-4 mt-1 shrink-0 opacity-70 group-hover:opacity-100 transition-opacity"
                  />
                  <span className="font-bold text-action group-hover:text-action-hover transition-colors">
                    {enc.title}
                  </span>
                </button>
              ))}
            </div>
          ) : (
            <p className="text-dim italic">Nothing more to find here.</p>
          )}

          <div className="pt-4">
            <Button
              variant="secondary"
              onClick={() => doAction({ action: "leave_dungeon" })}
              disabled={loading}
            >
              Leave
            </Button>
          </div>
        </div>
      </div>
    </div>
  );
}
