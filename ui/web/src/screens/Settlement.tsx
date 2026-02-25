import { useState } from "react";
import { useGame } from "../GameContext";
import StatusBar from "./StatusBar";
import MarketScreen from "./Market";
import Inventory from "./Inventory";
import type { GameResponse } from "../api/types";

export default function Settlement({ state }: { state: GameResponse }) {
  const { doAction, loading } = useGame();
  const [showMarket, setShowMarket] = useState(false);
  const [showInventory, setShowInventory] = useState(false);

  if (!state.settlement) return null;

  const { settlement, status } = state;

  if (showMarket) {
    return <MarketScreen state={state} onBack={() => setShowMarket(false)} />;
  }

  return (
    <div className="h-full flex flex-col bg-page text-primary">
      <StatusBar status={status} />

      <div className="flex-1 flex items-start justify-center overflow-y-auto p-6">
        <div className="max-w-md w-full space-y-6">
          <div className="text-center">
            <h2 className="text-2xl font-header text-accent">
              {settlement.name}
            </h2>
            <div className="text-dim text-sm mt-1">
              Tier {settlement.tier} Settlement
            </div>
          </div>

          <div className="space-y-2">
            {settlement.services.includes("market") && (
              <button
                onClick={() => setShowMarket(true)}
                className="w-full py-3 bg-btn hover:bg-btn-hover
                           border border-edge hover:border-action
                           transition-colors text-left px-4"
              >
                <span className="text-accent">Market</span>
                <span className="text-dim text-sm block">
                  Buy and sell goods
                </span>
              </button>
            )}

            <button
              onClick={() => setShowInventory(true)}
              className="w-full py-3 bg-btn hover:bg-btn-hover
                         border border-edge transition-colors text-left px-4"
            >
              <span className="text-primary">Inventory</span>
            </button>
          </div>

          <button
            onClick={() => doAction({ action: "leave_settlement" })}
            disabled={loading}
            className="w-full py-3 bg-btn hover:bg-btn-hover disabled:bg-panel-alt
                       transition-colors"
          >
            Leave Settlement
          </button>
        </div>
      </div>

      {showInventory && state.inventory && (
        <Inventory
          inventory={state.inventory}
          onClose={() => setShowInventory(false)}
        />
      )}
    </div>
  );
}
