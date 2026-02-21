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
    <div className="h-full flex flex-col bg-stone-900 text-stone-100">
      <StatusBar status={status} />

      <div className="flex-1 flex items-start justify-center overflow-y-auto p-6">
        <div className="max-w-md w-full space-y-6">
          <div className="text-center">
            <h2 className="text-2xl font-bold text-amber-200">
              {settlement.name}
            </h2>
            <div className="text-stone-400 text-sm mt-1">
              Tier {settlement.tier} Settlement
            </div>
          </div>

          <div className="space-y-2">
            {settlement.services.includes("market") && (
              <button
                onClick={() => setShowMarket(true)}
                className="w-full py-3 bg-stone-800 hover:bg-stone-700
                           border border-stone-700 hover:border-amber-700
                           transition-colors text-left px-4"
              >
                <span className="text-amber-300">Market</span>
                <span className="text-stone-400 text-sm block">
                  Buy and sell goods
                </span>
              </button>
            )}

            <button
              onClick={() => setShowInventory(true)}
              className="w-full py-3 bg-stone-800 hover:bg-stone-700
                         border border-stone-700 transition-colors text-left px-4"
            >
              <span className="text-stone-200">Inventory</span>
            </button>
          </div>

          <button
            onClick={() => doAction({ action: "leave_settlement" })}
            disabled={loading}
            className="w-full py-3 bg-stone-700 hover:bg-stone-600 disabled:bg-stone-800
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
