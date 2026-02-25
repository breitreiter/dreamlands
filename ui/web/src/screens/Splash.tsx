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
    <div className="h-full flex items-center justify-center bg-page text-primary">
      <div className="text-center space-y-8">
        <h1 className="text-5xl font-header tracking-wider text-accent">
          DREAMLANDS
        </h1>
        <p className="text-dim text-sm">
          A journey into the unknown
        </p>
        <button
          onClick={startNewGame}
          disabled={loading}
          className="px-8 py-3 bg-action hover:bg-action-hover disabled:bg-btn
                     text-contrast font-medium tracking-wide transition-colors"
        >
          {loading ? "Starting..." : "New Game"}
        </button>
        {error && (
          <p className="text-negative text-sm">{error}</p>
        )}
        <div>
          <button
            onClick={() => setShowCredits(!showCredits)}
            className="text-muted hover:text-dim text-xs tracking-wide transition-colors"
          >
            {showCredits ? "Close Credits" : "Credits"}
          </button>
          {showCredits && (
            <div className="mt-4 text-left mx-auto max-w-xs space-y-2">
              <p className="text-dim text-xs font-medium tracking-wide mb-3">
                Third-party libraries
              </p>
              {credits.map((c) => (
                <div key={c.name} className="text-xs text-muted">
                  <span className="text-primary/80">{c.name}</span>
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
