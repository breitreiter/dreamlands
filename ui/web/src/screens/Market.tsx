import { useState, useEffect, useMemo } from "react";
import { useGame } from "../GameContext";
import type { GameResponse, MarketItem, ItemInfo } from "../api/types";
import * as api from "../api/client";

const PACK_TYPES = new Set(["weapon", "armor", "boots", "tool", "tradegood"]);
function isPackType(type: string) { return PACK_TYPES.has(type); }

const ITEM_TYPE_ICONS: Record<string, string> = {
  weapon: "sword-brandish.svg",
  armor: "chain-mail.svg",
  boots: "boots.svg",
  tool: "knapsack.svg",
  consumable: "pouch-with-beads.svg",
  tradegood: "two-coins.svg",
};

function iconUrl(file: string): string {
  return `/world/assets/icons/${file}`;
}

function itemTypeIcon(type: string): string {
  return ITEM_TYPE_ICONS[type] || "wooden-crate.svg";
}

const TAB_ICONS: Record<string, string> = {
  trade: "two-coins.svg",
  foods: "pouch-with-beads.svg",
  equipment: "sword-brandish.svg",
  pack: "backpack.svg",
  haversack: "knapsack.svg",
  equipped: "sword-brandish.svg",
};

function MaskedIcon({ icon, className, color }: { icon: string; className?: string; color: string }) {
  return (
    <div
      className={`inline-block flex-shrink-0 ${className || ""}`}
      style={{
        backgroundColor: color,
        maskImage: `url(${iconUrl(icon)})`,
        maskSize: "contain",
        maskRepeat: "no-repeat",
        maskPosition: "center",
        WebkitMaskImage: `url(${iconUrl(icon)})`,
        WebkitMaskSize: "contain",
        WebkitMaskRepeat: "no-repeat",
        WebkitMaskPosition: "center",
      }}
    />
  );
}

type BuyTab = "trade" | "foods" | "equipment";
type SellTab = "pack" | "haversack" | "equipped";

function matchesBuyTab(item: MarketItem, tab: BuyTab): boolean {
  switch (tab) {
    case "trade": return item.type === "tradegood";
    case "foods": return item.type === "consumable";
    case "equipment": return item.type === "weapon" || item.type === "armor" || item.type === "boots" || item.type === "tool";
  }
}

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

  // Tab state
  const [buyTab, setBuyTab] = useState<BuyTab>("trade");
  const [sellTab, setSellTab] = useState<SellTab>("pack");

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
      if (isPackType(item.type)) {
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
    if (isPackType(item.type) && projected.packCount >= projected.packCapacity) return false;
    if (!isPackType(item.type) && projected.haversackCount >= projected.haversackCapacity) return false;
    return true;
  }

  function canSell(defId: string): boolean {
    const allItems = [...(inventory?.pack ?? []), ...(inventory?.haversack ?? [])];
    const totalOwned = allItems.filter((i) => i.defId === defId).length;
    const alreadySelling = pendingSells.filter((id) => id === defId).length;
    return alreadySelling < totalOwned;
  }

  const hasOrder = pendingBuys.size > 0 || pendingSells.length > 0;

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
      onBack();
    }
  }

  function cancelOrder() {
    setPendingBuys(new Map());
    setPendingSells([]);
    onBack();
  }

  // Filtered stock for buy panel
  const filteredStock = stock.filter((item) => matchesBuyTab(item, buyTab));

  // Sellable items for sell panel
  const sellItems = useMemo((): { item: ItemInfo; source: string }[] => {
    if (!inventory) return [];
    switch (sellTab) {
      case "pack":
        return inventory.pack.map((item) => ({ item, source: "pack" }));
      case "haversack":
        return inventory.haversack.map((item) => ({ item, source: "haversack" }));
      case "equipped": {
        const items: { item: ItemInfo; source: string }[] = [];
        if (inventory.equipment.weapon) items.push({ item: inventory.equipment.weapon, source: "weapon" });
        if (inventory.equipment.armor) items.push({ item: inventory.equipment.armor, source: "armor" });
        if (inventory.equipment.boots) items.push({ item: inventory.equipment.boots, source: "boots" });
        return items;
      }
    }
  }, [inventory, sellTab]);

  // Ledger entries for the cash book, each with an undo callback
  const ledgerEntries = useMemo(() => {
    const entries: { label: string; amount: number; type: "buy" | "sell"; undo: () => void }[] = [];
    pendingSells.forEach((defId, idx) => {
      const allItems = [...(inventory?.pack ?? []), ...(inventory?.haversack ?? [])];
      const name = allItems.find((it) => it.defId === defId)?.name ?? defId;
      entries.push({ label: `sell: ${name}`, amount: sellPrices[defId] ?? 0, type: "sell", undo: () => removeSell(idx) });
    });
    for (const [itemId, qty] of pendingBuys) {
      const item = stock.find((s) => s.id === itemId);
      const name = item?.name ?? itemId;
      for (let q = 0; q < qty; q++) {
        entries.push({ label: `buy: ${name}`, amount: item?.buyPrice ?? 0, type: "buy", undo: () => removeBuy(itemId) });
      }
    }
    return entries;
  }, [pendingBuys, pendingSells, stock, sellPrices, inventory]);

  const TabBtn = ({ id, active, onClick, children }: { id: string; active: boolean; onClick: () => void; children: React.ReactNode }) => {
    const icon = TAB_ICONS[id];
    return (
      <button
        onClick={onClick}
        className={`h-12 px-4 flex items-center gap-2 transition-colors ${
          active
            ? "bg-btn border border-accent text-accent"
            : "bg-transparent border border-transparent text-action-dim hover:text-action"
        }`}
        style={{ borderRadius: "999px" }}
      >
        {icon && <MaskedIcon icon={icon} className="w-5 h-5" color={active ? "#D0BD62" : "currentColor"} />}
        {children}
      </button>
    );
  };

  return (
    <div className="h-full flex flex-col bg-page text-primary">

      {message && (
        <div className="px-4 py-2 text-sm text-center text-primary/80 bg-panel border-b border-edge">
          {message}
        </div>
      )}

      <div className="flex-1 flex overflow-hidden">
        {/* BUY column */}
        <div className="flex-1 flex flex-col border-r border-edge min-w-0">
          <div className="p-3 border-b border-edge">
            <h3 className="font-header text-accent text-[32px] leading-tight">Buy</h3>
            <div className="flex gap-1 mt-2">
              <TabBtn id="trade" active={buyTab === "trade"} onClick={() => setBuyTab("trade")}>Trade</TabBtn>
              <TabBtn id="foods" active={buyTab === "foods"} onClick={() => setBuyTab("foods")}>Foods</TabBtn>
              <TabBtn id="equipment" active={buyTab === "equipment"} onClick={() => setBuyTab("equipment")}>Equipment</TabBtn>
            </div>
          </div>
          <div className="flex-1 overflow-y-auto p-2 space-y-2">
            {loadingStock ? (
              <div className="p-4 text-muted">Loading stock...</div>
            ) : filteredStock.length === 0 ? (
              <div className="p-4 text-muted">Nothing available</div>
            ) : (
              filteredStock.map((item) => {
                const projQty = projected.projectedStock.get(item.id) ?? item.quantity;
                const pendingQty = pendingBuys.get(item.id) ?? 0;
                return (
                  <div key={item.id} className="flex items-start gap-3 p-3 rounded-lg" style={{ backgroundColor: "rgba(0, 0, 0, 0.35)" }}>
                    <div className="w-8 h-8 flex-shrink-0 flex items-center justify-center">
                      <MaskedIcon icon={itemTypeIcon(item.type)} className="w-5 h-5" color="#D0BD62" />
                    </div>
                    <div className="flex-1 min-w-0">
                      <div className="text-primary">
                        {item.name}
                        <span className="text-muted ml-1">({projQty} available)</span>
                      </div>
                      {item.description && (
                        <div className="text-muted mt-0.5 truncate">{item.description}</div>
                      )}
                    </div>
                    <div className="flex items-center gap-1 flex-shrink-0">
                      {pendingQty > 0 && (
                        <button
                          onClick={() => removeBuy(item.id)}
                          className="px-2 py-1 rounded-lg text-action hover:text-action-hover transition-colors"
                          style={{ backgroundColor: "rgba(13, 13, 13, 0.8)" }}
                        >
                          -
                        </button>
                      )}
                      {pendingQty > 0 && (
                        <span className="text-accent w-4 text-center">{pendingQty}</span>
                      )}
                      <button
                        onClick={() => addBuy(item.id)}
                        disabled={!canBuy(item)}
                        className="px-3 py-1 rounded-lg disabled:opacity-40
                                   text-action hover:text-action-hover transition-colors flex items-center gap-1"
                        style={{ backgroundColor: "rgba(13, 13, 13, 0.8)" }}
                      >
                        {item.buyPrice}g
                      </button>
                    </div>
                  </div>
                );
              })
            )}
          </div>
        </div>

        {/* CASH BOOK column */}
        <div className="w-64 flex flex-col bg-parchment text-parchment-text flex-shrink-0">
          <div className="p-3 border-b border-parchment-text/20">
            <h3 className="font-header text-parchment-text text-[32px] leading-tight">Cash Book</h3>
          </div>
          <div className="flex-1 overflow-y-auto p-4 font-hand">
            <div className="flex justify-between mb-3">
              <span>Opening balance</span>
              <span>{state.status.gold}g</span>
            </div>

            {ledgerEntries.length > 0 && (
              <div className="space-y-1 mb-3">
                {ledgerEntries.map((entry, i) => (
                  <div
                    key={i}
                    onClick={entry.undo}
                    className="flex justify-between cursor-pointer"
                  >
                    <span className="truncate mr-2">{entry.label}</span>
                    <span className="flex-shrink-0">
                      {entry.type === "sell" ? (
                        <span className="text-green-800">+{entry.amount}g</span>
                      ) : (
                        <span className="text-red-800">-{entry.amount}g</span>
                      )}
                    </span>
                  </div>
                ))}
              </div>
            )}

            <div className="border-t border-parchment-text/30 pt-2 mt-2">
              <div className="flex justify-between font-bold">
                <span>Closing balance</span>
                <span>{projected.gold}g</span>
              </div>
            </div>
          </div>

          {hasOrder && (
            <div className="p-3 border-t border-parchment-text/20 flex gap-2">
              <button
                onClick={submitOrder}
                disabled={loading}
                className="flex-1 px-3 py-2 bg-parchment-text/80 hover:bg-parchment-text
                           disabled:opacity-50 text-parchment transition-colors flex items-center justify-center gap-1"
              >
                <span>&#x2713;</span> Accept
              </button>
              <button
                onClick={cancelOrder}
                className="flex-1 px-3 py-2 bg-parchment-text/20 hover:bg-parchment-text/30
                           text-parchment-text transition-colors flex items-center justify-center gap-1"
              >
                <span>&#x2717;</span> Cancel
              </button>
            </div>
          )}

          {!hasOrder && (
            <div className="p-3 border-t border-parchment-text/20">
              <button
                onClick={onBack}
                className="w-full px-3 py-2 bg-parchment-text/20 hover:bg-parchment-text/30
                           text-parchment-text transition-colors"
              >
                Leave Market
              </button>
            </div>
          )}
        </div>

        {/* SELL column */}
        <div className="flex-1 flex flex-col border-l border-edge min-w-0">
          <div className="p-3 border-b border-edge">
            <h3 className="font-header text-accent text-[32px] leading-tight">Sell</h3>
            <div className="flex gap-1 mt-2">
              <TabBtn id="pack" active={sellTab === "pack"} onClick={() => setSellTab("pack")}>Pack</TabBtn>
              <TabBtn id="haversack" active={sellTab === "haversack"} onClick={() => setSellTab("haversack")}>Haversack</TabBtn>
              <TabBtn id="equipped" active={sellTab === "equipped"} onClick={() => setSellTab("equipped")}>Equipped</TabBtn>
            </div>
          </div>
          <div className="flex-1 overflow-y-auto p-2 space-y-2">
            {sellItems.length === 0 ? (
              <div className="p-4 text-muted">Nothing to sell</div>
            ) : (
              sellItems.map(({ item, source }, i) => {
                const sellPrice = sellPrices[item.defId] ?? 0;
                const soldOut = !canSell(item.defId);
                return (
                  <div
                    key={`${source}-${item.defId}-${i}`}
                    className={`flex items-start gap-3 p-3 rounded-lg ${soldOut ? "opacity-40" : ""}`}
                    style={{ backgroundColor: "rgba(0, 0, 0, 0.35)" }}
                  >
                    <div className="w-8 h-8 flex-shrink-0 flex items-center justify-center">
                      <MaskedIcon icon={itemTypeIcon(item.type)} className="w-5 h-5" color="#D0BD62" />
                    </div>
                    <div className="flex-1 min-w-0">
                      <div className="text-primary">{item.name}</div>
                      {item.description && (
                        <div className="text-muted mt-0.5 truncate">{item.description}</div>
                      )}
                      {sellTab === "equipped" && (
                        <div className="text-dim mt-0.5">equipped ({source})</div>
                      )}
                    </div>
                    <button
                      onClick={() => addSell(item.defId)}
                      disabled={soldOut}
                      className="px-3 py-1 rounded-lg disabled:opacity-40
                                 text-action hover:text-action-hover transition-colors flex items-center gap-1 flex-shrink-0"
                      style={{ backgroundColor: "rgba(13, 13, 13, 0.8)" }}
                    >
                      {sellPrice}g
                    </button>
                  </div>
                );
              })
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
