import { useGame } from "../GameContext";
import type { GameResponse } from "../api/types";

export default function GameOver({ state }: { state: GameResponse }) {
  const { startNewGame, loading } = useGame();

  return (
    <div className="h-full flex items-center justify-center bg-stone-900 text-stone-100">
      <div className="text-center space-y-6">
        <h2 className="text-3xl font-bold text-red-400">Game Over</h2>
        <p className="text-stone-300">
          {state.reason || "You have perished in the Dreamlands."}
        </p>
        <button
          onClick={startNewGame}
          disabled={loading}
          className="px-8 py-3 bg-amber-700 hover:bg-amber-600 disabled:bg-stone-700
                     transition-colors"
        >
          {loading ? "Starting..." : "New Game"}
        </button>
      </div>
    </div>
  );
}
