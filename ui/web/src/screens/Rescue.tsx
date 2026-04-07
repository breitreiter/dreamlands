import { useEffect } from "react";
import { useGame } from "../GameContext";
import type { GameResponse } from "../api/types";
import MaskedIcon from "../components/MaskedIcon";
import parchment from "../assets/parchment.webp";

export default function Rescue({ state }: { state: GameResponse }) {
  const { refreshState, loading } = useGame();
  const rescue = state.rescue;

  // Close with Enter key
  useEffect(() => {
    const handler = (e: KeyboardEvent) => {
      if (e.key === "Enter") refreshState();
    };
    window.addEventListener("keydown", handler);
    return () => window.removeEventListener("keydown", handler);
  }, [refreshState]);

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
        <img
          src="/world/assets/vignettes/chapterhouse_camp.webp"
          alt=""
          className="w-full h-full object-cover"
        />
      </div>

      {/* Right panel — narrative content */}
      <div className="flex-1 overflow-y-auto p-8 md:p-12 flex flex-col">
        <div className="max-w-2xl space-y-6">
          <h2 className="text-3xl md:text-4xl font-header text-negative uppercase">
            Rescued
          </h2>

          <div className="text-primary/80 leading-loose space-y-4">
            <p className="italic">
              Your injuries were too severe to bear. Darkness took you on the road.
            </p>
            <p className="italic">
              You remember only fragments — many hands, the creak of a cart, voices
              speaking low. Somewhere along the way your belongings were taken as
              payment, though you can scarcely remember by whom.
            </p>
            <p className="italic">
              You awake in the Chapterhouse, mended and rested.
            </p>
          </div>

          {/* Lost items summary */}
          {rescue && (rescue.lostItems.length > 0 || rescue.goldLost > 0) && (
            <div className="space-y-2 border-t border-edge pt-3">
              <div className="text-negative font-bold">Lost</div>
              {rescue.lostItems.map((item, i) => (
                <div key={i} className="flex items-center gap-2 text-primary/80">
                  <span>— {item}</span>
                </div>
              ))}
              {rescue.goldLost > 0 && (
                <div className="flex items-center gap-2 text-primary/80">
                  <MaskedIcon icon="two-coins.svg" className="w-4 h-4" color="#D0BD62" />
                  <span>−{rescue.goldLost} gold</span>
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
