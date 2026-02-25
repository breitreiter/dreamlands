import { useGame } from "../GameContext";
import type { GameResponse } from "../api/types";

export default function GameOver({ state }: { state: GameResponse }) {
  const { startNewGame, loading } = useGame();

  return (
    <div className="h-full flex items-center justify-center bg-page text-primary">
      <div className="text-center space-y-6">
        <h2 className="text-3xl font-header text-negative">Game Over</h2>
        <p className="text-primary/80">
          {state.reason || "You have perished in the Dreamlands."}
        </p>
        <button
          onClick={startNewGame}
          disabled={loading}
          className="px-8 py-3 bg-action hover:bg-action-hover disabled:bg-btn
                     text-contrast transition-colors"
        >
          {loading ? "Starting..." : "New Game"}
        </button>
      </div>
    </div>
  );
}
