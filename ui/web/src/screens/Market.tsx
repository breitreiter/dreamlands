import { useState, useEffect, useMemo } from "react";
import { useGame } from "../GameContext";
import type { GameResponse, MarketItem, HaulOffer, ItemInfo } from "../api/types";
import * as api from "../api/client";
import MaskedIcon, { itemTypeIcon, TabButton } from "../components/MaskedIcon";
import HaulItem from "../components/HaulItem";
import WaxSeal from "../components/WaxSeal";
import TopBar from "../components/TopBar";
import { Button } from "@/components/ui/button";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  AlertDialogTrigger,
} from "@/components/ui/alert-dialog";
import { getMarketName, getProprietorName, getSealVariant, getSealSymbolIndex } from "../marketNaming";
import { getMarketDayNote } from "../calendar";

const PACK_TYPES = new Set(["weapon", "armor", "boots", "tool", "haul"]);
function isPackType(type: string) { return PACK_TYPES.has(type); }

type BuyTab = "hauls" | "supplies" | "equipment";
type SellTab = "pack" | "equipped";

function matchesBuyTab(item: MarketItem, tab: BuyTab): boolean {
  switch (tab) {
    case "hauls": return false; // hauls are not MarketItems
    case "supplies": return item.type === "consumable";
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
  const [hauls, setHauls] = useState<HaulOffer[]>([]);
  const [sellPrices, setSellPrices] = useState<Record<string, number>>({});
  const [rationCost, setRationCost] = useState(0);
  const [loadingStock, setLoadingStock] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Local order state
  const [pendingBuys, setPendingBuys] = useState<Map<string, number>>(new Map());
  const [pendingSells, setPendingSells] = useState<string[]>([]);

  // Tab state
  const [buyTab, setBuyTab] = useState<BuyTab>("hauls");
  const [sellTab, setSellTab] = useState<SellTab>("pack");

  function switchBuyTab(tab: BuyTab) {
    setBuyTab(tab);
    const linked: Record<BuyTab, SellTab> = { hauls: "pack", supplies: "pack", equipment: "equipped" };
    setSellTab(linked[tab]);
  }

  useEffect(() => {
    function onKeyDown(e: KeyboardEvent) {
      if (e.key === "Escape") {
        setPendingBuys(new Map());
        setPendingSells([]);
        onBack();
      }
    }
    window.addEventListener("keydown", onKeyDown);
    return () => window.removeEventListener("keydown", onKeyDown);
  }, [onBack]);

  useEffect(() => {
    if (!gameId) return;
    setLoadingStock(true);
    api
      .getMarketStock(gameId)
      .then((res) => {
        setStock(res.stock);
        setHauls(res.hauls ?? []);
        setSellPrices(res.sellPrices ?? {});
        setRationCost(res.rationCost ?? 0);
      })
      .catch((e) => setError(e.message))
      .finally(() => setLoadingStock(false));
  }, [gameId]);

  const inventory = state.inventory;

  // Projected state derived from pending order.
  // Projection tracks pack items (with isEquipped flags) after applying pending sells/buys.
  const projected = useMemo(() => {
    let gold = state.status.gold;

    // Revenue from sells
    let sellRevenue = 0;
    for (const defId of pendingSells) {
      sellRevenue += sellPrices[defId] ?? 0;
    }
    gold += sellRevenue;

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

    // Start with current pack, simulate sells (removing items + clearing equipped flags)
    const remainingPack = [...(inventory?.pack ?? [])].map(i => ({ ...i }));
    let packSells = 0;
    for (const defId of pendingSells) {
      const idx = remainingPack.findIndex(i => i.defId === defId);
      if (idx >= 0) { remainingPack.splice(idx, 1); packSells++; }
    }

    // For each buy, check if auto-equip would trigger (no existing equipped of that type)
    // and count slots consumed
    let packBuys = 0;
    const projectedEquippedTypes = new Set(remainingPack.filter(i => i.isEquipped).map(i => i.type));

    for (const [itemId, qty] of pendingBuys) {
      const item = stock.find((s) => s.id === itemId);
      if (!item) continue;
      if (isPackType(item.type)) {
        // First unit may auto-equip (into an empty slot) — still consumes a pack slot
        packBuys += qty;
        // Track that this type is now "equipped" for capacity gating
        if ((item.type === "weapon" || item.type === "armor" || item.type === "boots")
            && !projectedEquippedTypes.has(item.type)) {
          projectedEquippedTypes.add(item.type);
        }
      }
    }

    const packCount = remainingPack.length + packBuys;
    const packCapacity = inventory?.packCapacity ?? 0;

    // Projected equipment: what's equipped after order
    const projectedEquipment = {
      weapon: remainingPack.find(i => i.type === "weapon" && i.isEquipped) ?? null,
      armor: remainingPack.find(i => i.type === "armor" && i.isEquipped) ?? null,
      boots: remainingPack.find(i => i.type === "boots" && i.isEquipped) ?? null,
    };

    return { gold, projectedStock, packCount, packCapacity, buyCost, sellRevenue, projectedEquipment };
  }, [state.status.gold, pendingBuys, pendingSells, stock, inventory, sellPrices]);

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

  function removeSell(defId: string) {
    setPendingSells((prev) => {
      const idx = prev.indexOf(defId);
      if (idx < 0) return prev;
      const next = [...prev];
      next.splice(idx, 1);
      return next;
    });
  }

  function canBuy(item: MarketItem): boolean {
    const pendingQty = pendingBuys.get(item.id) ?? 0;
    if (pendingQty >= item.quantity) return false;
    if (projected.gold < item.buyPrice) return false;
    if (isPackType(item.type) && projected.packCount >= projected.packCapacity) return false;
    return true;
  }

  function canSell(item: ItemInfo): boolean {
    if (item.type === "haul") return false;
    if (!(item.defId in sellPrices)) return false;
    return true;
  }

  const packFull = projected.packCount >= projected.packCapacity;

  // Restock projection: fill empty pack slots with rations (up to packCapacity, capped by gold)
  const currentRationCount = (inventory?.pack ?? []).filter(i => i.defId === "food_ration").length;
  const pendingRationBuys = pendingBuys.get("food_ration") ?? 0;
  const projectedRationCount = currentRationCount + pendingRationBuys;
  const freePackSlots = Math.max(0, projected.packCapacity - projected.packCount);
  const restockSlots = freePackSlots;
  const restockAffordable = rationCost > 0 ? Math.floor(projected.gold / rationCost) : restockSlots;
  const restockCount = Math.min(restockSlots, restockAffordable);
  const restockCost = restockCount * rationCost;
  // Suppress restock if we already have plenty
  const shouldRestock = restockCount > 0 && projectedRationCount < 4;

  async function restockAndLeave() {
    await doAction({ action: "restock_rations" });
    onBack();
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
      setPendingBuys(new Map());
      setPendingSells([]);
      if (failures.length > 0) {
        setError(failures.map((f) => f.message).join("; "));
        if (gameId) {
          api.getMarketStock(gameId).then((res) => {
            setStock(res.stock);
            setHauls(res.hauls ?? []);
            setSellPrices(res.sellPrices ?? {});
            setRationCost(res.rationCost ?? 0);
          });
        }
      } else {
        onBack();
      }
    }
  }

  function cancelOrder() {
    setPendingBuys(new Map());
    setPendingSells([]);
    onBack();
  }

  async function claimHaul(offerId: string) {
    const result = await doAction({ action: "claim_haul", offerId });
    if (result) {
      setHauls((prev) => prev.filter((h) => h.id !== offerId));
    }
  }

  const filteredStock = stock.filter((item) => matchesBuyTab(item, buyTab));

  // Sell items for right panel — subtract items already staged for sell
  const sellItems = useMemo((): { item: ItemInfo; source: string }[] => {
    if (!inventory) return [];

    const remainingSells = [...pendingSells];

    function consumeSell(defId: string): boolean {
      const idx = remainingSells.indexOf(defId);
      if (idx >= 0) { remainingSells.splice(idx, 1); return true; }
      return false;
    }

    const items: { item: ItemInfo; source: string; sold: boolean }[] = [];

    switch (sellTab) {
      case "pack":
        for (const item of inventory.pack.filter(i => !i.isEquipped)) {
          const sold = consumeSell(item.defId);
          items.push({ item, source: "pack", sold });
        }
        break;
      case "equipped":
        for (const item of inventory.pack.filter(i => i.isEquipped)) {
          const sold = consumeSell(item.defId);
          items.push({ item, source: item.type, sold });
        }
        break;
    }

    return items.filter(i => !i.sold);
  }, [inventory, sellTab, pendingSells]);

  const settlementName = state.node?.poi?.name ?? "Market";
  const terrain = state.node?.terrain ?? null;
  const marketName = getMarketName(settlementName, terrain);
  const proprietorName = getProprietorName(settlementName, terrain);
  const dayNote = getMarketDayNote(state.status.day, state.status.time);

  return (
    <div className="h-full flex flex-col bg-page text-primary">

      {error && (
        <div className="px-4 py-2 text-center text-negative bg-panel border-b border-edge">
          {error}
        </div>
      )}

      <TopBar
        status={state.status}
        gold={projected.gold}
        goldAnnotation={hasOrder && projected.gold !== state.status.gold
          ? <span className="text-parchment-text/60">(was {state.status.gold}g)</span>
          : undefined}
        onBack={onBack}
      >
        {hasOrder ? (
          <div className="flex gap-2">
            <Button variant="secondary" size="sm" onClick={submitOrder} disabled={loading}>
              <MaskedIcon icon="shaking-hands.svg" className="w-4 h-4" color="currentColor" />
              Confirm
            </Button>
            <Button variant="secondary" size="sm" onClick={cancelOrder}>
              <MaskedIcon icon="cancel.svg" className="w-4 h-4" color="currentColor" />
              Cancel
            </Button>
          </div>
        ) : (
          <div className="flex gap-2">
            <Button variant="secondary" size="sm" onClick={restockAndLeave} disabled={loading || !shouldRestock}>
              <MaskedIcon icon="knapsack.svg" className="w-4 h-4" color="currentColor" />
              {shouldRestock
                ? `Restock Food and Leave (${restockCost}g)`
                : "Restock Food and Leave"}
            </Button>
            <Button variant="secondary" size="sm" onClick={onBack}>
              <MaskedIcon icon="cancel.svg" className="w-4 h-4" color="currentColor" />
              Return to Map
            </Button>
          </div>
        )}
      </TopBar>

      <div className="flex-1 flex overflow-hidden">
        {/* BUY column */}
        <div className="flex-1 flex flex-col border-r border-edge min-w-0">
          <div className="p-3 pt-5">
            {/* Market sign */}
            <div
              className="flex items-center gap-3"
              style={{ borderBottom: "1px dashed rgba(208,189,98,.35)", paddingBottom: "0.5rem" }}
            >
              <MaskedIcon icon="two-coins.svg" className="w-6 h-6 flex-shrink-0" color="#D0BD62" />
              <h2
                className="font-header text-accent flex-shrink-0"
                style={{ fontSize: 32, letterSpacing: ".01em", lineHeight: 1, margin: 0 }}
              >
                {marketName}
              </h2>
              <div style={{ flex: 1, height: 1, background: "linear-gradient(to right, rgba(208,189,98,.4), transparent)" }} />
              <div
                style={{
                  position: "relative",
                  width: 18,
                  height: 18,
                  borderRadius: "50%",
                  border: "1px solid rgba(208,189,98,.55)",
                  flexShrink: 0,
                }}
              >
                <div style={{ position: "absolute", top: "100%", left: "50%", transform: "translateX(-50%)", width: 1, height: 12, backgroundColor: "rgba(208,189,98,.55)" }} />
              </div>
            </div>
            {/* Byline */}
            <div className="flex items-center mt-2 mb-2" style={{ gap: "0.9rem" }}>
              <span className="text-muted">Proprietor: <span className="text-accent">{proprietorName}</span></span>
              <div style={{ width: 3, height: 3, borderRadius: "50%", backgroundColor: "rgba(139,139,139,.5)", flexShrink: 0 }} />
              <span className="text-muted">{dayNote}</span>
            </div>
            <div className="flex gap-1">
              <TabButton id="hauls" active={buyTab === "hauls"} onClick={() => switchBuyTab("hauls")}>Contracts</TabButton>
              <TabButton id="supplies" active={buyTab === "supplies"} onClick={() => switchBuyTab("supplies")}>Supplies</TabButton>
              <TabButton id="equipment" active={buyTab === "equipment"} onClick={() => switchBuyTab("equipment")}>Equipment</TabButton>
            </div>
          </div>
          <div className="flex-1 overflow-y-auto p-2 space-y-2">
            {loadingStock ? (
              <div className="p-4 text-muted">Loading stock...</div>
            ) : buyTab === "hauls" ? (
              hauls.length === 0 ? (
                <div className="p-4 text-muted">No contracts available</div>
              ) : (
                hauls.map((haul) => (
                  <div key={haul.id} className="flex items-start gap-3 p-3 rounded-lg" style={{ backgroundColor: "rgba(0, 0, 0, 0.35)" }}>
                    <WaxSeal variant={getSealVariant(haul.id)} symbolIndex={getSealSymbolIndex(haul.id)} />
                    <div className="flex-1 min-w-0">
                      <HaulItem
                        name={haul.name}
                        destinationName={haul.destinationName}
                        destinationHint={haul.destinationHint}
                        payout={haul.payout}
                        flavor={haul.originFlavor}
                      />
                    </div>
                    <Button variant="secondary" size="sm" onClick={() => claimHaul(haul.id)} disabled={loading || packFull} className="flex-shrink-0">
                      <MaskedIcon icon="receive-money.svg" className="w-4 h-4" color="currentColor" />
                      Claim
                    </Button>
                  </div>
                ))
              )
            ) : filteredStock.length === 0 ? (
              <div className="p-4 text-muted">Nothing available</div>
            ) : (
              <>
                {filteredStock.map((item) => {
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
                          <Button variant="secondary" size="sm" onClick={() => removeBuy(item.id)}>
                            <span className="text-accent">{pendingQty}x</span>
                            <MaskedIcon icon="cancel.svg" className="w-3 h-3" color="currentColor" />
                          </Button>
                        )}
                        <Button variant="secondary" size="sm" onClick={() => addBuy(item.id)} disabled={!canBuy(item)}>
                          <MaskedIcon icon="pay-money.svg" className="w-4 h-4" color="currentColor" />
                          {item.buyPrice}g
                        </Button>
                      </div>
                    </div>
                  );
                })}
              </>
            )}
          </div>
        </div>

        {/* SELL column */}
        <div className="flex-1 flex flex-col border-l border-edge min-w-0">
          <div className="p-3 pt-5">
            <h3 className="font-header text-accent text-[32px] leading-tight">Sell</h3>
            <p className="text-muted mt-0.5">The factor will take things off your hands. Click to stage.</p>
            <div className="flex gap-1 mt-2">
              <TabButton id="pack" active={sellTab === "pack"} onClick={() => setSellTab("pack")}>Pack</TabButton>
              <TabButton id="equipped" active={sellTab === "equipped"} onClick={() => setSellTab("equipped")}>Equipped</TabButton>
            </div>
          </div>

          {/* Staged sells chip bar */}
          {pendingSells.length > 0 && (
            <div className="px-3 pb-2 flex flex-wrap gap-1">
              {(() => {
                const counts = new Map<string, { name: string; count: number }>();
                for (const defId of pendingSells) {
                  const existing = counts.get(defId);
                  if (existing) { existing.count++; continue; }
                  const item = inventory?.pack.find(i => i.defId === defId);
                  counts.set(defId, { name: item?.name ?? defId, count: 1 });
                }
                return [...counts.entries()].map(([defId, { name, count }]) => (
                  <Button key={defId} variant="secondary" size="sm" onClick={() => removeSell(defId)}>
                    <span className="text-accent">{count > 1 ? `${count}x ` : ""}{name}</span>
                    <span className="text-positive">+{(sellPrices[defId] ?? 0) * count}g</span>
                    <MaskedIcon icon="cancel.svg" className="w-3 h-3" color="currentColor" />
                  </Button>
                ));
              })()}
            </div>
          )}

          <div className="flex-1 overflow-y-auto p-2 space-y-2">
            {sellItems.length === 0 ? (
              <div className="p-4 text-muted">Nothing here</div>
            ) : (
              <>
                {sellItems.map(({ item, source }, i) => {
                  const price = sellPrices[item.defId];
                  const sellable = canSell(item);
                  return (
                    <div
                      key={`${source}-${item.defId}-${i}`}
                      className="flex items-start gap-3 p-3 rounded-lg"
                      style={{ backgroundColor: "rgba(0, 0, 0, 0.35)" }}
                    >
                      {item.type === "haul" ? (
                        <WaxSeal
                          variant={getSealVariant(item.haulOfferId ?? item.defId)}
                          symbolIndex={getSealSymbolIndex(item.haulOfferId ?? item.defId)}
                        />
                      ) : (
                        <div className="w-8 h-8 flex-shrink-0 flex items-center justify-center">
                          <MaskedIcon icon={itemTypeIcon(item.type)} className="w-5 h-5" color="#D0BD62" />
                        </div>
                      )}
                      <div className="flex-1 min-w-0">
                        {item.type === "haul" ? (
                          <HaulItem
                            name={item.name}
                            destinationName={item.destinationName}
                            destinationHint={item.destinationHint}
                            payout={item.payout}
                            flavor={item.description}
                          />
                        ) : (
                          <>
                            <div className="text-primary">{item.name}</div>
                            {item.description && (
                              <div className="text-muted mt-0.5 truncate">{item.description}</div>
                            )}
                            {sellTab === "equipped" && (
                              <div className="text-dim mt-0.5">equipped ({source})</div>
                            )}
                          </>
                        )}
                      </div>
                      {sellable && (
                        <Button variant="secondary" size="sm" onClick={() => addSell(item.defId)} className="flex-shrink-0">
                          <MaskedIcon icon="pay-money.svg" className="w-4 h-4" color="currentColor" />
                          Sell
                          <span className="text-positive">+{price}g</span>
                        </Button>
                      )}
                      {item.type === "haul" && item.haulOfferId && (
                        <AlertDialog>
                          <AlertDialogTrigger asChild>
                            <Button variant="secondary" size="icon" disabled={loading} title="Abandon" className="flex-shrink-0">
                              <MaskedIcon icon="cancel.svg" className="w-5 h-5" color="currentColor" />
                            </Button>
                          </AlertDialogTrigger>
                          <AlertDialogContent size="sm">
                            <AlertDialogHeader>
                              <AlertDialogTitle>Abandon {item.name}?</AlertDialogTitle>
                              <AlertDialogDescription>This contract will be lost. It will not be returned to the market.</AlertDialogDescription>
                            </AlertDialogHeader>
                            <AlertDialogFooter>
                              <AlertDialogCancel>
                                <MaskedIcon icon="cancel.svg" className="w-4 h-4" color="currentColor" />
                                Keep
                              </AlertDialogCancel>
                              <AlertDialogAction variant="destructive" onClick={() => doAction({ action: "abandon_haul", offerId: item.haulOfferId!})}>
                                <MaskedIcon icon="trash-can.svg" className="w-4 h-4" color="currentColor" />
                                Abandon
                              </AlertDialogAction>
                            </AlertDialogFooter>
                          </AlertDialogContent>
                        </AlertDialog>
                      )}
                    </div>
                  );
                })}
                {sellTab === "pack" && inventory && Array.from(
                  { length: Math.max(0, inventory.packCapacity - inventory.pack.filter(i => !i.isEquipped).length - sellItems.length) },
                  (_, i) => (
                    <div key={`empty-${i}`} className="flex items-center justify-center bg-btn/50 p-4 border border-dashed border-edge text-muted">
                      Empty slot
                    </div>
                  )
                )}
              </>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
