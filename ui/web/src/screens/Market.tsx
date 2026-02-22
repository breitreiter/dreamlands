import { useState, useEffect, useMemo } from "react";
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
  const { gameId, doAction, loading } = useGame();
  const [stock, setStock] = useState<MarketItem[]>([]);
  const [sellPrices, setSellPrices] = useState<Record<string, number>>({});
  const [loadingStock, setLoadingStock] = useState(true);
  const [message, setMessage] = useState<string | null>(null);

  // Local order state
  const [pendingBuys, setPendingBuys] = useState<Map<string, number>>(new Map());
  const [pendingSells, setPendingSells] = useState<string[]>([]);

  useEffect(() => {
    if (!gameId) return;
    setLoadingStock(true);
    api
      .getMarketStock(gameId)
      .then((res) => { setStock(res.stock); setSellPrices(res.sellPrices); })
      .catch((e) => setMessage(e.message))
      .finally(() => setLoadingStock(false));
  }, [gameId]);

  const inventory = state.inventory;

  // Projected state derived from pending order
  const projected = useMemo(() => {
    let gold = state.status.gold;

    // Revenue from sells
    for (const defId of pendingSells) {
      gold += sellPrices[defId] ?? 0;
    }

    // Cost of buys
    let buyCost = 0;
    for (const [itemId, qty] of pendingBuys) {
      const item = stock.find((s) => s.id === itemId);
      if (item) buyCost += item.buyPrice * qty;
    }
    gold -= buyCost;

    // Projected stock quantities
    const projectedStock = new Map<string, number>();
    for (const item of stock) {
      projectedStock.set(item.id, item.quantity - (pendingBuys.get(item.id) ?? 0));
    }

    // Projected inventory: remove sells
    const remainingSells = [...pendingSells];
    const projectedPack = (inventory?.pack ?? []).filter((item) => {
      const idx = remainingSells.indexOf(item.defId);
      if (idx >= 0) { remainingSells.splice(idx, 1); return false; }
      return true;
    });
    const projectedHaversack = (inventory?.haversack ?? []).filter((item) => {
      const idx = remainingSells.indexOf(item.defId);
      if (idx >= 0) { remainingSells.splice(idx, 1); return false; }
      return true;
    });

    // Count buys going to pack vs haversack
    let packBuys = 0;
    let haversackBuys = 0;
    for (const [itemId, qty] of pendingBuys) {
      const item = stock.find((s) => s.id === itemId);
      if (!item) continue;
      if (item.type === "weapon" || item.type === "armor" || item.type === "boots") {
        packBuys += qty;
      } else {
        haversackBuys += qty;
      }
    }

    const packCount = projectedPack.length + packBuys;
    const haversackCount = projectedHaversack.length + haversackBuys;
    const packCapacity = inventory?.packCapacity ?? 0;
    const haversackCapacity = inventory?.haversackCapacity ?? 0;

    return { gold, projectedStock, projectedPack, projectedHaversack, packCount, haversackCount, packCapacity, haversackCapacity, buyCost };
  }, [state.status.gold, pendingBuys, pendingSells, stock, sellPrices, inventory]);

  function addBuy(itemId: string) {
    setPendingBuys((prev) => {
      const next = new Map(prev);
      next.set(itemId, (next.get(itemId) ?? 0) + 1);
      return next;
    });
  }

  function removeBuy(itemId: string) {
    setPendingBuys((prev) => {
      const next = new Map(prev);
      const qty = next.get(itemId) ?? 0;
      if (qty <= 1) next.delete(itemId);
      else next.set(itemId, qty - 1);
      return next;
    });
  }

  function addSell(defId: string) {
    setPendingSells((prev) => [...prev, defId]);
  }

  function removeSell(index: number) {
    setPendingSells((prev) => prev.filter((_, i) => i !== index));
  }

  function canBuy(item: MarketItem): boolean {
    const pendingQty = pendingBuys.get(item.id) ?? 0;
    if (pendingQty >= item.quantity) return false;
    if (projected.gold < item.buyPrice) return false;
    // Check space
    const isPackItem = item.type === "weapon" || item.type === "armor" || item.type === "boots";
    if (isPackItem && projected.packCount >= projected.packCapacity) return false;
    if (!isPackItem && projected.haversackCount >= projected.haversackCapacity) return false;
    return true;
  }

  function canSell(defId: string): boolean {
    // Count how many of this item remain after pending sells
    const allItems = [...(inventory?.pack ?? []), ...(inventory?.haversack ?? [])];
    const totalOwned = allItems.filter((i) => i.defId === defId).length;
    const alreadySelling = pendingSells.filter((id) => id === defId).length;
    return alreadySelling < totalOwned;
  }

  const hasOrder = pendingBuys.size > 0 || pendingSells.length > 0;

  const sellRevenue = pendingSells.reduce((sum, defId) => sum + (sellPrices[defId] ?? 0), 0);

  async function submitOrder() {
    if (!gameId || !hasOrder) return;

    const order = {
      buys: [...pendingBuys.entries()].map(([itemId, quantity]) => ({ itemId, quantity })),
      sells: pendingSells.map((itemDefId) => ({ itemDefId })),
    };

    const result = await doAction({ action: "market_order", order });
    if (result) {
      const failures = result.marketResult?.results.filter((r) => !r.success) ?? [];
      if (failures.length > 0) {
        setMessage(failures.map((f) => f.message).join("; "));
      } else {
        setMessage("Order completed");
      }
      setPendingBuys(new Map());
      setPendingSells([]);
      // Refresh stock to reflect changes
      api.getMarketStock(gameId).then((res) => { setStock(res.stock); setSellPrices(res.sellPrices); }).catch(() => {});
      setTimeout(() => setMessage(null), 3000);
    }
  }

  function cancelOrder() {
    setPendingBuys(new Map());
    setPendingSells([]);
    onBack();
  }

  return (
    <div className="h-full flex flex-col bg-stone-900 text-stone-100">
      <StatusBar status={state.status} />

      <div className="flex-1 flex overflow-hidden">
        {/* Buy panel */}
        <div className="flex-1 flex flex-col border-r border-stone-700">
          <div className="p-3 border-b border-stone-700">
            <h3 className="text-amber-200 font-medium">Buy</h3>
            <div className="text-xs text-stone-400">
              Gold: {state.status.gold}
              {projected.gold !== state.status.gold && (
                <span className="text-amber-400"> → {projected.gold}</span>
              )}
            </div>
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
                    <th className="p-2 text-right">Stock</th>
                    <th className="p-2 text-right">Price</th>
                    <th className="p-2"></th>
                  </tr>
                </thead>
                <tbody>
                  {stock.map((item) => {
                    const projQty = projected.projectedStock.get(item.id) ?? item.quantity;
                    const pendingQty = pendingBuys.get(item.id) ?? 0;
                    return (
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
                          {projQty !== item.quantity ? (
                            <><span className="text-stone-500">{item.quantity}</span> → {projQty}</>
                          ) : (
                            item.quantity
                          )}
                        </td>
                        <td className="p-2 text-right text-amber-400">
                          {item.buyPrice}g
                        </td>
                        <td className="p-2 flex gap-1">
                          <button
                            onClick={() => addBuy(item.id)}
                            disabled={!canBuy(item)}
                            className="px-3 py-1 bg-amber-700 hover:bg-amber-600
                                       disabled:bg-stone-700 disabled:text-stone-500
                                       text-xs transition-colors"
                          >
                            +
                          </button>
                          {pendingQty > 0 && (
                            <>
                              <span className="px-1 text-xs text-amber-300 self-center">{pendingQty}</span>
                              <button
                                onClick={() => removeBuy(item.id)}
                                className="px-2 py-1 bg-stone-700 hover:bg-stone-600 text-xs transition-colors"
                              >
                                −
                              </button>
                            </>
                          )}
                        </td>
                      </tr>
                    );
                  })}
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
                  const soldOut = !canSell(item.defId);
                  return (
                    <div
                      key={i}
                      className={`flex items-center justify-between p-2 hover:bg-stone-800 ${
                        soldOut ? "opacity-40" : ""
                      }`}
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
                        onClick={() => addSell(item.defId)}
                        disabled={soldOut}
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

      {/* Order summary + footer */}
      <div className="border-t border-stone-700 bg-stone-800">
        {hasOrder && (
          <div className="px-3 pt-2 text-xs text-stone-400 space-y-1">
            {[...pendingBuys.entries()].map(([itemId, qty]) => {
              const item = stock.find((s) => s.id === itemId);
              return (
                <div key={`buy-${itemId}`} className="flex justify-between">
                  <span className="text-amber-300">Buy {qty}x {item?.name ?? itemId}</span>
                  <span className="text-amber-400">-{(item?.buyPrice ?? 0) * qty}g</span>
                </div>
              );
            })}
            {pendingSells.map((defId, i) => {
              const name = [...(inventory?.pack ?? []), ...(inventory?.haversack ?? [])].find((it) => it.defId === defId)?.name ?? defId;
              return (
                <div key={`sell-${i}`} className="flex justify-between items-center">
                  <span className="text-stone-300">Sell {name}</span>
                  <div className="flex items-center gap-2">
                    <span className="text-green-400">+{sellPrices[defId] ?? 0}g</span>
                    <button onClick={() => removeSell(i)} className="text-stone-500 hover:text-stone-300">×</button>
                  </div>
                </div>
              );
            })}
            <div className="flex justify-between pt-1 border-t border-stone-700 text-sm">
              <span>Net</span>
              <span className={sellRevenue - projected.buyCost >= 0 ? "text-green-400" : "text-red-400"}>
                {sellRevenue - projected.buyCost >= 0 ? "+" : ""}{sellRevenue - projected.buyCost}g
              </span>
            </div>
          </div>
        )}
        <div className="flex items-center justify-between p-3">
          {message && <span className="text-sm text-stone-300">{message}</span>}
          <div className="flex-1" />
          <div className="flex gap-2">
            {hasOrder && (
              <button
                onClick={submitOrder}
                disabled={loading}
                className="px-4 py-2 bg-amber-700 hover:bg-amber-600 disabled:bg-stone-700 text-sm transition-colors"
              >
                Confirm Order
              </button>
            )}
            <button
              onClick={cancelOrder}
              className="px-4 py-2 bg-stone-700 hover:bg-stone-600 text-sm transition-colors"
            >
              {hasOrder ? "Cancel" : "Back"}
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}
