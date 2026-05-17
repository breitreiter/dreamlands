import { GameProvider, useGame } from "./GameContext";
import Splash from "./screens/Splash";
import Explore from "./screens/Explore";
import Encounter from "./screens/Encounter";
import Rescue from "./screens/Rescue";
import Camp from "./screens/Camp";
import Combat from "./screens/Combat";
import Tableau from "./screens/Tableau";

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
      {/* Keep Explore mounted during camp/rescue so Leaflet map stays alive */}
      {["exploring", "camp", "camp_resolved", "rescued"].includes(response.mode) && (
        <Explore state={response} />
      )}
      {(response.mode === "encounter" || response.mode === "outcome" || response.mode === "approach_prompt" || response.mode === "tableau_prompt") && <Encounter state={response} />}
      {(response.mode === "combat" || response.mode === "combat_resolved") && <Combat state={response} />}
      {response.mode === "rescued" && <Rescue state={response} />}
      {(response.mode === "camp" || response.mode === "camp_resolved") && <Camp state={response} />}
      {response.mode === "tableau_prompt" && <Tableau state={response} />}
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
