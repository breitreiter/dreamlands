import { useGame } from "../GameContext";
import StatusBar from "./StatusBar";
import type { GameResponse } from "../api/types";

export default function Encounter({ state }: { state: GameResponse }) {
  const { doAction, loading } = useGame();
  const { encounter, status } = state;

  if (!encounter) return null;

  return (
    <div className="h-full flex flex-col bg-stone-900 text-stone-100">
      <StatusBar status={status} />

      <div className="flex-1 flex items-start justify-center overflow-y-auto p-6">
        <div className="max-w-2xl w-full space-y-6">
          <h2 className="text-2xl font-bold text-amber-200">
            {encounter.title}
          </h2>

          {encounter.body && (
            <div className="text-stone-300 leading-relaxed whitespace-pre-wrap">
              {encounter.body}
            </div>
          )}

          <div className="space-y-2 pt-2">
            {encounter.choices.map((choice) => (
              <button
                key={choice.index}
                onClick={() =>
                  doAction({ action: "choose", choiceIndex: choice.index })
                }
                disabled={loading}
                className="w-full text-left p-3 bg-stone-800 hover:bg-stone-700
                           disabled:bg-stone-800 disabled:text-stone-500
                           border border-stone-700 hover:border-amber-700
                           transition-colors group"
              >
                <span className="text-amber-300 group-hover:text-amber-200">
                  {choice.index + 1}.{" "}
                </span>
                <span>{choice.label}</span>
                {choice.preview && (
                  <span className="block text-xs text-stone-400 mt-1 ml-5">
                    {choice.preview}
                  </span>
                )}
              </button>
            ))}

            {encounter.choices.length === 0 && (
              <div className="text-center text-stone-500 py-4">
                <p>No choices available.</p>
                <button
                  onClick={() => doAction({ action: "end_encounter" })}
                  className="mt-4 px-6 py-2 bg-stone-700 hover:bg-stone-600 transition-colors"
                >
                  Continue
                </button>
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
