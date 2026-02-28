import { GameProvider, useGame } from "./GameContext";
import Splash from "./screens/Splash";
import Explore from "./screens/Explore";
import Encounter from "./screens/Encounter";

import GameOver from "./screens/GameOver";
import Camp from "./screens/Camp";

function GameRouter() {
  const { response, error, clearError } = useGame();

  if (!response) return <Splash />;

  return (
    <>
      {error && (
        <div className="fixed top-0 left-0 right-0 z-[2000] bg-negative/90 text-contrast px-4 py-2 text-sm flex justify-between items-center">
          <span>{error}</span>
          <button onClick={clearError} className="text-contrast/70 hover:text-contrast ml-4">
            Dismiss
          </button>
        </div>
      )}
      {(response.mode === "exploring" || response.mode === "at_settlement") && <Explore state={response} />}
      {(response.mode === "encounter" || response.mode === "outcome") && <Encounter state={response} />}
      {response.mode === "game_over" && <GameOver state={response} />}
      {(response.mode === "camp" || response.mode === "camp_resolved") && <Camp state={response} />}
    </>
  );
}

export default function App() {
  return (
    <GameProvider>
      <GameRouter />
    </GameProvider>
  );
}
