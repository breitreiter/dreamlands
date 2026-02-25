import { useGame } from "../GameContext";
import StatusBar from "./StatusBar";
import type { GameResponse } from "../api/types";

export default function Encounter({ state }: { state: GameResponse }) {
  const { doAction, loading } = useGame();
  const { encounter, status } = state;

  if (!encounter) return null;

  return (
    <div className="h-full flex flex-col bg-page text-primary">
      <StatusBar status={status} />

      <div className="flex-1 flex items-start justify-center overflow-y-auto p-6">
        <div className="max-w-2xl w-full space-y-6">
          <h2 className="text-2xl font-header text-accent">
            {encounter.title}
          </h2>

          {encounter.body && (
            <div className="text-primary/80 leading-relaxed whitespace-pre-wrap">
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
                className="w-full text-left p-3 bg-btn hover:bg-btn-hover
                           disabled:bg-btn disabled:text-muted
                           border border-edge hover:border-action
                           transition-colors group"
              >
                <span className="text-accent group-hover:text-accent">
                  {choice.index + 1}.{" "}
                </span>
                <span>{choice.label}</span>
                {choice.preview && (
                  <span className="block text-xs text-dim mt-1 ml-5">
                    {choice.preview}
                  </span>
                )}
              </button>
            ))}

            {encounter.choices.length === 0 && (
              <div className="text-center text-muted py-4">
                <p>No choices available.</p>
                <button
                  onClick={() => doAction({ action: "end_encounter" })}
                  className="mt-4 px-6 py-2 bg-btn hover:bg-btn-hover transition-colors"
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
