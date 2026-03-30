import { useState } from "react";
import { useGame } from "../GameContext";
import { Button } from "@/components/ui/button";
import splashStars from "../assets/splash_stars.jpg";
import splashTower from "../assets/splash_tower.png";

const credits = [
  { name: "Leaflet", license: "BSD 2-Clause", author: "Vladimir Agafonkin" },
  { name: "React", license: "MIT", author: "Meta" },
  { name: "react-leaflet", license: "MIT", author: "Paul Le Cam" },
  { name: "Tailwind CSS", license: "MIT", author: "Tailwind Labs" },
  { name: "Vite", license: "MIT", author: "Evan You" },
];

export default function Splash() {
  const { startNewGame, resumeGame, hasSavedGame, loading, error } = useGame();
  const [showCredits, setShowCredits] = useState(false);

  return (
    <div
      className="h-full flex items-center justify-center text-primary overflow-hidden relative"
      style={{ background: `url(${splashStars}) repeat center center / 1400px 800px` }}
    >
      <img
        src={splashTower}
        alt=""
        className="absolute left-0 top-0 pointer-events-none"
        width={715}
        height={1554}
      />

      <div className="text-center space-y-8 relative z-10">
        <h1
          className="font-header text-[72px] tracking-wider text-accent"
          style={{
            WebkitTextStroke: "4px #000113",
            paintOrder: "stroke fill",
            textShadow: "0 0 8px #000113, 0 0 16px #000113",
          }}
        >
          The Merchant
        </h1>
        <div className="space-x-4">
          {hasSavedGame && (
            <Button size="lg" className="" onClick={resumeGame} disabled={loading}>
              {loading ? "Loading..." : "Continue"}
            </Button>
          )}
          <Button size="lg" className="" onClick={startNewGame} disabled={loading}>
            {loading ? "Starting..." : "New Game"}
          </Button>
        </div>
        {error && (
          <p className="text-negative">{error}</p>
        )}
        <div>
          <Button variant="ghost" className="tracking-wide" onClick={() => setShowCredits(!showCredits)}>
            {showCredits ? "Close Credits" : "Credits"}
          </Button>
          {showCredits && (
            <div className="mt-4 text-left mx-auto max-w-xs space-y-2">
              <p className="text-dim font-bold tracking-wide mb-3">
                Assets
              </p>
              <div className="text-muted">
                Icons by{" "}
                <a href="https://game-icons.net/" target="_blank" rel="noopener noreferrer" className="text-action underline">
                  game-icons.net
                </a>
                {" — "}CC BY 3.0
              </div>
              <p className="text-dim font-bold tracking-wide mb-3 mt-4">
                Third-party libraries
              </p>
              {credits.map((c) => (
                <div key={c.name} className="text-muted">
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
