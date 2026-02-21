import { useGame } from "../GameContext";

export default function Splash() {
  const { startNewGame, loading, error } = useGame();

  return (
    <div className="h-full flex items-center justify-center bg-stone-900 text-stone-100">
      <div className="text-center space-y-8">
        <h1 className="text-5xl font-bold tracking-wider text-amber-200">
          DREAMLANDS
        </h1>
        <p className="text-stone-400 text-sm">
          A journey into the unknown
        </p>
        <button
          onClick={startNewGame}
          disabled={loading}
          className="px-8 py-3 bg-amber-700 hover:bg-amber-600 disabled:bg-stone-700
                     text-stone-100 font-medium tracking-wide transition-colors"
        >
          {loading ? "Starting..." : "New Game"}
        </button>
        {error && (
          <p className="text-red-400 text-sm">{error}</p>
        )}
      </div>
    </div>
  );
}
