import { useGame } from "../GameContext";
import StatusBar from "./StatusBar";
import type { GameResponse } from "../api/types";

export default function Outcome({ state }: { state: GameResponse }) {
  const { doAction, loading } = useGame();
  const { outcome, status } = state;

  if (!outcome) return null;

  return (
    <div className="h-full flex flex-col bg-page text-primary">
      <StatusBar status={status} />

      <div className="flex-1 flex items-start justify-center overflow-y-auto p-6">
        <div className="max-w-2xl w-full space-y-6">
          {outcome.preamble && (
            <div className="text-primary/80 leading-relaxed whitespace-pre-wrap">
              {outcome.preamble}
            </div>
          )}

          {outcome.skillCheck && (
            <div
              className={`p-3 border ${
                outcome.skillCheck.passed
                  ? "border-positive bg-positive/15"
                  : "border-negative bg-negative/15"
              }`}
            >
              <span className="capitalize">{outcome.skillCheck.skill}</span>
              {" check: "}
              <span className="font-medium">
                {outcome.skillCheck.rolled}
                {outcome.skillCheck.modifier !== 0 &&
                  ` ${outcome.skillCheck.modifier >= 0 ? "+" : ""}${outcome.skillCheck.modifier}`}
              </span>
              {" vs "}
              <span className="font-medium">{outcome.skillCheck.target}</span>
              {" \u2014 "}
              <span
                className={
                  outcome.skillCheck.passed ? "text-positive" : "text-negative"
                }
              >
                {outcome.skillCheck.passed ? "Success" : "Failure"}
              </span>
            </div>
          )}

          <div className="text-primary/80 leading-relaxed whitespace-pre-wrap">
            {outcome.text}
          </div>

          {outcome.mechanics.length > 0 && (
            <div className="space-y-1 border-t border-edge pt-3">
              {outcome.mechanics.map((m, i) => (
                <div key={i} className="text-xs text-dim">
                  {m.description}
                </div>
              ))}
            </div>
          )}

          <button
            onClick={() => doAction({ action: outcome.nextAction || "end_encounter" })}
            disabled={loading}
            className="px-6 py-2 bg-action hover:bg-action-hover disabled:bg-btn
                       text-contrast transition-colors"
          >
            {outcome.nextAction === "end_dungeon" ? "Return to your journey" : "Continue"}
          </button>
        </div>
      </div>
    </div>
  );
}
