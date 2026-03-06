import { useState } from "react";
import { useGame } from "../GameContext";
import { Button } from "@/components/ui/button";
import MaskedIcon from "@/components/MaskedIcon";
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
        <h1 className="font-header text-[32px] tracking-wider text-accent">
          DREAMLANDS
        </h1>
        <p className="text-dim">
          A journey into the unknown
        </p>
        <div className="space-y-3">
          {hasSavedGame && (
            <Button size="lg" className="w-full tracking-wide" onClick={resumeGame} disabled={loading}>
              <MaskedIcon icon="play-button.svg" className="w-5 h-5" color="currentColor" />
              {loading ? "Loading..." : "Continue"}
            </Button>
          )}
          <Button size="lg" className="w-full tracking-wide" onClick={startNewGame} disabled={loading}>
            <MaskedIcon icon="play-button.svg" className="w-5 h-5" color="currentColor" />
            {loading ? "Starting..." : "New Game"}
          </Button>
        </div>
        {error && (
          <p className="text-negative">{error}</p>
        )}
        <div>
          <Button variant="ghost" className="tracking-wide" onClick={() => setShowCredits(!showCredits)}>
            <MaskedIcon icon="tied-scroll.svg" className="w-5 h-5" color="currentColor" />
            {showCredits ? "Close Credits" : "Credits"}
          </Button>
          {showCredits && (
            <div className="mt-4 text-left mx-auto max-w-xs space-y-2">
              <p className="text-dim font-bold tracking-wide mb-3">
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
