import { useState, useEffect } from "react";
import { useGame } from "../GameContext";
import StatusBar from "./StatusBar";
import type { GameResponse, MarketItem } from "../api/types";
import * as api from "../api/client";

export default function MarketScreen({
  state,
  onBack,
}: {
  state: GameResponse;
  onBack: () => void;
}) {
  const { gameId, setResponse, loading } = useGame();
  const [stock, setStock] = useState<MarketItem[]>([]);
  const [sellPrices, setSellPrices] = useState<Record<string, number>>({});
  const [loadingStock, setLoadingStock] = useState(true);
  const [message, setMessage] = useState<string | null>(null);

  const status = state.status;

  useEffect(() => {
    if (!gameId) return;
    setLoadingStock(true);
    api
      .getMarketStock(gameId)
      .then((res) => { setStock(res.stock); setSellPrices(res.sellPrices); })
      .catch((e) => setMessage(e.message))
      .finally(() => setLoadingStock(false));
  }, [gameId]);

  async function refreshStock() {
    if (!gameId) return;
    try {
      const res = await api.getMarketStock(gameId);
      setStock(res.stock);
      setSellPrices(res.sellPrices);
    } catch {}
  }

  async function handleBuy(itemId: string) {
    if (!gameId) return;
    try {
      const result = await api.marketAction(gameId, { action: "buy", itemId, quantity: 1 });
      if (result.success) {
        setMessage("Bought item");
        setResponse({ ...state, status: result.status, inventory: result.inventory });
        await refreshStock();
      } else {
        setMessage(result.message || "Purchase failed");
      }
    } catch (e) {
      setMessage(e instanceof Error ? e.message : "Purchase failed");
    }
    setTimeout(() => setMessage(null), 2000);
  }

  async function handleSell(itemDefId: string) {
    if (!gameId) return;
    try {
      const result = await api.marketAction(gameId, { action: "sell", itemId: itemDefId });
      if (result.success) {
        setMessage("Sold item");
        setResponse({ ...state, status: result.status, inventory: result.inventory });
        await refreshStock();
      } else {
        setMessage(result.message || "Sale failed");
      }
    } catch (e) {
      setMessage(e instanceof Error ? e.message : "Sale failed");
    }
    setTimeout(() => setMessage(null), 2000);
  }

  const inventory = state.inventory;

  return (
    <div className="h-full flex flex-col bg-stone-900 text-stone-100">
      <StatusBar status={status} />

      <div className="flex-1 flex overflow-hidden">
        {/* Buy panel */}
        <div className="flex-1 flex flex-col border-r border-stone-700">
          <div className="p-3 border-b border-stone-700">
            <h3 className="text-amber-200 font-medium">Buy</h3>
            <div className="text-xs text-stone-400">Gold: {status.gold}</div>
          </div>
          <div className="flex-1 overflow-y-auto">
            {loadingStock ? (
              <div className="p-4 text-stone-500">Loading stock...</div>
            ) : (
              <table className="w-full text-sm">
                <thead>
                  <tr className="text-left text-xs text-stone-400 border-b border-stone-700">
                    <th className="p-2">Item</th>
                    <th className="p-2">Type</th>
                    <th className="p-2 text-right">Qty</th>
                    <th className="p-2 text-right">Price</th>
                    <th className="p-2"></th>
                  </tr>
                </thead>
                <tbody>
                  {stock.map((item) => (
                    <tr
                      key={item.id}
                      className="border-b border-stone-800 hover:bg-stone-800"
                    >
                      <td className="p-2">
                        <div>{item.name}</div>
                        {item.description && (
                          <div className="text-xs text-stone-500">
                            {item.description}
                          </div>
                        )}
                      </td>
                      <td className="p-2 text-stone-400 capitalize">
                        {item.type}
                      </td>
                      <td className="p-2 text-right text-stone-300">
                        {item.quantity}
                      </td>
                      <td className="p-2 text-right text-amber-400">
                        {item.buyPrice}g
                      </td>
                      <td className="p-2">
                        <button
                          onClick={() => handleBuy(item.id)}
                          disabled={loading || status.gold < item.buyPrice || item.quantity <= 0}
                          className="px-3 py-1 bg-amber-700 hover:bg-amber-600
                                     disabled:bg-stone-700 disabled:text-stone-500
                                     text-xs transition-colors"
                        >
                          Buy
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
          </div>
        </div>

        {/* Sell panel */}
        <div className="w-72 flex flex-col">
          <div className="p-3 border-b border-stone-700">
            <h3 className="text-amber-200 font-medium">Sell</h3>
          </div>
          <div className="flex-1 overflow-y-auto">
            {inventory && (
              <div className="p-2 space-y-1">
                {[...inventory.pack, ...inventory.haversack].map((item, i) => {
                  const sellPrice = sellPrices[item.defId] ?? 0;
                  return (
                    <div
                      key={i}
                      className="flex items-center justify-between p-2 hover:bg-stone-800"
                    >
                      <div>
                        <div className="text-sm">{item.name}</div>
                        {sellPrice > 0 && (
                          <div className="text-xs text-amber-400/70">
                            {sellPrice}g
                          </div>
                        )}
                      </div>
                      <button
                        onClick={() => handleSell(item.defId)}
                        disabled={loading}
                        className="px-3 py-1 bg-stone-700 hover:bg-stone-600
                                   disabled:text-stone-500 text-xs transition-colors"
                      >
                        Sell
                      </button>
                    </div>
                  );
                })}
                {inventory.pack.length === 0 &&
                  inventory.haversack.length === 0 && (
                    <div className="text-stone-500 text-sm p-2">
                      Nothing to sell
                    </div>
                  )}
              </div>
            )}
          </div>
        </div>
      </div>

      {/* Footer */}
      <div className="flex items-center justify-between p-3 border-t border-stone-700 bg-stone-800">
        {message && <span className="text-sm text-stone-300">{message}</span>}
        <div className="flex-1" />
        <button
          onClick={onBack}
          className="px-4 py-2 bg-stone-700 hover:bg-stone-600 text-sm transition-colors"
        >
          Back
        </button>
      </div>
    </div>
  );
}
