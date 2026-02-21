import { useGame } from "../GameContext";
import StatusBar from "./StatusBar";
import type { GameResponse } from "../api/types";

export default function Outcome({ state }: { state: GameResponse }) {
  const { doAction, loading } = useGame();
  const { outcome, status } = state;

  if (!outcome) return null;

  return (
    <div className="h-full flex flex-col bg-stone-900 text-stone-100">
      <StatusBar status={status} />

      <div className="flex-1 flex items-start justify-center overflow-y-auto p-6">
        <div className="max-w-2xl w-full space-y-6">
          {outcome.preamble && (
            <div className="text-stone-300 leading-relaxed whitespace-pre-wrap">
              {outcome.preamble}
            </div>
          )}

          {outcome.skillCheck && (
            <div
              className={`p-3 border ${
                outcome.skillCheck.passed
                  ? "border-green-700 bg-green-900/30"
                  : "border-red-700 bg-red-900/30"
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
                  outcome.skillCheck.passed ? "text-green-400" : "text-red-400"
                }
              >
                {outcome.skillCheck.passed ? "Success" : "Failure"}
              </span>
            </div>
          )}

          <div className="text-stone-300 leading-relaxed whitespace-pre-wrap">
            {outcome.text}
          </div>

          {outcome.mechanics.length > 0 && (
            <div className="space-y-1 border-t border-stone-700 pt-3">
              {outcome.mechanics.map((m, i) => (
                <div key={i} className="text-xs text-stone-400">
                  {m.description}
                </div>
              ))}
            </div>
          )}

          <button
            onClick={() => doAction({ action: outcome.nextAction || "end_encounter" })}
            disabled={loading}
            className="px-6 py-2 bg-amber-700 hover:bg-amber-600 disabled:bg-stone-700
                       transition-colors"
          >
            {outcome.nextAction === "end_dungeon" ? "Return to your journey" : "Continue"}
          </button>
        </div>
      </div>
    </div>
  );
}
