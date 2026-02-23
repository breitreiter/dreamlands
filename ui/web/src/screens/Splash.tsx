import { useState } from "react";
import { useGame } from "../GameContext";

const credits = [
  { name: "Leaflet", license: "BSD 2-Clause", author: "Vladimir Agafonkin" },
  { name: "React", license: "MIT", author: "Meta" },
  { name: "react-leaflet", license: "MIT", author: "Paul Le Cam" },
  { name: "Tailwind CSS", license: "MIT", author: "Tailwind Labs" },
  { name: "Vite", license: "MIT", author: "Evan You" },
];

export default function Splash() {
  const { startNewGame, loading, error } = useGame();
  const [showCredits, setShowCredits] = useState(false);

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
        <div>
          <button
            onClick={() => setShowCredits(!showCredits)}
            className="text-stone-500 hover:text-stone-300 text-xs tracking-wide transition-colors"
          >
            {showCredits ? "Close Credits" : "Credits"}
          </button>
          {showCredits && (
            <div className="mt-4 text-left mx-auto max-w-xs space-y-2">
              <p className="text-stone-400 text-xs font-medium tracking-wide mb-3">
                Third-party libraries
              </p>
              {credits.map((c) => (
                <div key={c.name} className="text-xs text-stone-500">
                  <span className="text-stone-300">{c.name}</span>
                  {" — "}
                  {c.license} ({c.author})
                </div>
              ))}
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
