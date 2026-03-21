import { useState, useEffect, useMemo } from "react";
import { useGame } from "../GameContext";
import type { GameResponse, MarketItem, HaulOffer, ItemInfo } from "../api/types";
import * as api from "../api/client";
import MaskedIcon, { itemTypeIcon, TabButton } from "../components/MaskedIcon";
import HaulItem from "../components/HaulItem";
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

const PACK_TYPES = new Set(["weapon", "armor", "boots", "tool", "haul"]);
function isPackType(type: string) { return PACK_TYPES.has(type); }

const FOOD_IDS = ["food_protein", "food_grain", "food_sweets"];
function isFoodItem(id: string) { return FOOD_IDS.includes(id); }

type BuyTab = "hauls" | "supplies" | "equipment";
type SellTab = "pack" | "haversack" | "equipped";

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
    const linked: Record<BuyTab, SellTab> = { hauls: "pack", supplies: "haversack", equipment: "equipped" };
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
      })
      .catch((e) => setError(e.message))
      .finally(() => setLoadingStock(false));
  }, [gameId]);

  const inventory = state.inventory;

  // Projected state derived from pending order
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

    const projectedEquipment = {
      weapon: inventory?.equipment.weapon ?? null,
      armor: inventory?.equipment.armor ?? null,
      boots: inventory?.equipment.boots ?? null,
    };

    // Track which equipment slots are freed by sells
    for (const defId of pendingSells) {
      if (projectedEquipment.weapon?.defId === defId) projectedEquipment.weapon = null;
      else if (projectedEquipment.armor?.defId === defId) projectedEquipment.armor = null;
      else if (projectedEquipment.boots?.defId === defId) projectedEquipment.boots = null;
    }

    // Identify "floating" buys: equippable items that auto-equip into empty slots
    const claimedSlots = new Set<string>();
    const floatingBuys = new Set<string>();
    for (const [itemId] of pendingBuys) {
      const item = stock.find((s) => s.id === itemId);
      if (!item) continue;
      const slot = item.type as string;
      if ((slot === "weapon" || slot === "armor" || slot === "boots")
          && !claimedSlots.has(slot)
          && projectedEquipment[slot] === null) {
        claimedSlots.add(slot);
        floatingBuys.add(itemId);
      }
    }

    // Count sells from pack/haversack
    let packSells = 0;
    let haversackSells = 0;
    const remainingPack = [...(inventory?.pack ?? [])];
    const remainingHaversack = [...(inventory?.haversack ?? [])];
    for (const defId of pendingSells) {
      const packIdx = remainingPack.findIndex(i => i.defId === defId);
      if (packIdx >= 0) { remainingPack.splice(packIdx, 1); packSells++; continue; }
      const havIdx = remainingHaversack.findIndex(i => i.defId === defId);
      if (havIdx >= 0) { remainingHaversack.splice(havIdx, 1); haversackSells++; }
    }

    // Count buys going to pack vs haversack (subtract 1 for floating buys)
    let packBuys = 0;
    let haversackBuys = 0;
    for (const [itemId, qty] of pendingBuys) {
      const item = stock.find((s) => s.id === itemId);
      if (!item) continue;
      const floatCount = floatingBuys.has(itemId) ? 1 : 0;
      if (isPackType(item.type)) {
        packBuys += qty - floatCount;
      } else {
        haversackBuys += qty;
      }
    }

    const packCount = (inventory?.pack ?? []).length - packSells + packBuys;
    const haversackCount = (inventory?.haversack ?? []).length - haversackSells + haversackBuys;
    const packCapacity = inventory?.packCapacity ?? 0;
    const haversackCapacity = inventory?.haversackCapacity ?? 0;

    return { gold, projectedStock, packCount, haversackCount, packCapacity, haversackCapacity, buyCost, sellRevenue, claimedSlots, projectedEquipment };
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
    if (isPackType(item.type) && projected.packCount >= projected.packCapacity) {
      // Allow if this item would float (auto-equip into an empty, unclaimed slot)
      const slot = item.type as string;
      if ((slot === "weapon" || slot === "armor" || slot === "boots")
          && !projected.claimedSlots.has(slot)
          && projected.projectedEquipment[slot] === null) {
        return true;
      }
      return false;
    }
    if (!isPackType(item.type) && projected.haversackCount >= projected.haversackCapacity) return false;
    return true;
  }

  function canSell(item: ItemInfo): boolean {
    if (item.type === "haul") return false;
    if (!(item.defId in sellPrices)) return false;
    return true;
  }

  // ── Food helpers ──
  // Count food by type in the player's haversack (minus pending sells)
  const foodCounts = useMemo(() => {
    const counts: Record<string, number> = {};
    for (const id of FOOD_IDS) counts[id] = 0;
    if (!inventory) return counts;
    const remainingSells = [...pendingSells];
    for (const item of inventory.haversack) {
      if (!isFoodItem(item.defId)) continue;
      const sellIdx = remainingSells.indexOf(item.defId);
      if (sellIdx >= 0) { remainingSells.splice(sellIdx, 1); continue; }
      counts[item.defId] = (counts[item.defId] ?? 0) + 1;
    }
    return counts;
  }, [inventory, pendingSells]);

  // Remove one food buy — remove from the type with the most pending
  function removeFoodBuy() {
    setPendingBuys(prev => {
      const next = new Map(prev);
      let best = "";
      let bestQty = 0;
      for (const id of FOOD_IDS) {
        const qty = next.get(id) ?? 0;
        if (qty > bestQty) { bestQty = qty; best = id; }
      }
      if (!best) return prev;
      if (bestQty <= 1) next.delete(best);
      else next.set(best, bestQty - 1);
      return next;
    });
  }

  const pendingFoodTotal = FOOD_IDS.reduce((sum, id) => sum + (pendingBuys.get(id) ?? 0), 0);

  const foodPrice = useMemo(() => {
    const item = stock.find(s => isFoodItem(s.id));
    return item?.buyPrice ?? 3;
  }, [stock]);

  function canBuyFood(): boolean {
    if (projected.gold < foodPrice) return false;
    if (projected.haversackCount >= projected.haversackCapacity) return false;
    return true;
  }

  function addFoodBuy(count: number = 1) {
    setPendingBuys(prev => {
      const next = new Map(prev);
      for (let i = 0; i < count; i++) {
        // Recalculate best type each iteration
        let best = FOOD_IDS[0];
        let bestCount = Infinity;
        for (const id of FOOD_IDS) {
          const total = (foodCounts[id] ?? 0) + (next.get(id) ?? 0);
          if (total < bestCount) { bestCount = total; best = id; }
        }
        next.set(best, (next.get(best) ?? 0) + 1);
      }
      return next;
    });
  }

  function maxFoodBuyable(): number {
    const slotsLeft = projected.haversackCapacity - projected.haversackCount;
    const affordable = Math.floor(projected.gold / foodPrice);
    return Math.max(0, Math.min(slotsLeft, affordable));
  }

  const packFull = projected.packCount >= projected.packCapacity;

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
        // Refresh stock to reflect partial order
        if (gameId) {
          api.getMarketStock(gameId).then((res) => {
            setStock(res.stock);
            setHauls(res.hauls ?? []);
            setSellPrices(res.sellPrices ?? {});
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

  // Filtered stock for buy panel — exclude individual food items from supplies (merged into one row)
  const filteredStock = stock.filter((item) => matchesBuyTab(item, buyTab) && !isFoodItem(item.id));
  const hasFood = stock.some(s => isFoodItem(s.id));

  // Sell items for right panel — subtract items already staged for sell
  const sellItems = useMemo((): { item: ItemInfo; source: string }[] => {
    if (!inventory) return [];

    // Track which pending sells have been "consumed" by earlier items
    const remainingSells = [...pendingSells];

    function consumeSell(defId: string): boolean {
      const idx = remainingSells.indexOf(defId);
      if (idx >= 0) { remainingSells.splice(idx, 1); return true; }
      return false;
    }

    const items: { item: ItemInfo; source: string; sold: boolean }[] = [];

    switch (sellTab) {
      case "pack":
        for (const item of inventory.pack) {
          const sold = consumeSell(item.defId);
          items.push({ item, source: "pack", sold });
        }
        break;
      case "haversack":
        for (const item of [...inventory.haversack].sort((a, b) => a.name.localeCompare(b.name))) {
          const sold = consumeSell(item.defId);
          items.push({ item, source: "haversack", sold });
        }
        break;
      case "equipped":
        if (inventory.equipment.weapon) {
          const sold = consumeSell(inventory.equipment.weapon.defId);
          items.push({ item: inventory.equipment.weapon, source: "weapon", sold });
        }
        if (inventory.equipment.armor) {
          const sold = consumeSell(inventory.equipment.armor.defId);
          items.push({ item: inventory.equipment.armor, source: "armor", sold });
        }
        if (inventory.equipment.boots) {
          const sold = consumeSell(inventory.equipment.boots.defId);
          items.push({ item: inventory.equipment.boots, source: "boots", sold });
        }
        break;
    }

    return items.filter(i => !i.sold);
  }, [inventory, sellTab, pendingSells]);

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
        ) : undefined}
      </TopBar>

      <div className="flex-1 flex overflow-hidden">
        {/* BUY column */}
        <div className="flex-1 flex flex-col border-r border-edge min-w-0">
          <div className="p-3">
            <h3 className="font-header text-accent text-[32px] leading-tight">Buy</h3>
            <div className="flex gap-1 mt-2">
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
                    <div className="w-8 h-8 flex-shrink-0 flex items-center justify-center">
                      <MaskedIcon icon="wooden-crate.svg" className="w-5 h-5" color="#D0BD62" />
                    </div>
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
            ) : filteredStock.length === 0 && !(buyTab === "supplies" && hasFood) ? (
              <div className="p-4 text-muted">Nothing available</div>
            ) : (
              <>
                {/* Merged food row on supplies tab */}
                {buyTab === "supplies" && hasFood && (
                  <div className="flex items-start gap-3 p-3 rounded-lg" style={{ backgroundColor: "rgba(0, 0, 0, 0.35)" }}>
                    <div className="w-8 h-8 flex-shrink-0 flex items-center justify-center">
                      <MaskedIcon icon={itemTypeIcon("consumable")} className="w-5 h-5" color="#D0BD62" />
                    </div>
                    <div className="flex-1 min-w-0">
                      <div className="text-primary">Food</div>
                      <div className="text-muted mt-0.5">Provisions for the road</div>
                    </div>
                    <div className="flex items-center gap-1 flex-shrink-0">
                      {pendingFoodTotal > 0 && (
                        <Button variant="secondary" size="sm" onClick={removeFoodBuy}>
                          <span className="text-accent">{pendingFoodTotal}x</span>
                          <MaskedIcon icon="cancel.svg" className="w-3 h-3" color="currentColor" />
                        </Button>
                      )}
                      {maxFoodBuyable() > 0 && (
                        <Button variant="secondary" size="sm" onClick={() => addFoodBuy(maxFoodBuyable())}>
                          Max
                        </Button>
                      )}
                      <Button variant="secondary" size="sm" onClick={() => addFoodBuy()} disabled={!canBuyFood()}>
                        <MaskedIcon icon="pay-money.svg" className="w-4 h-4" color="currentColor" />
                        {foodPrice}g
                      </Button>
                    </div>
                  </div>
                )}
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
          <div className="p-3">
            <h3 className="font-header text-accent text-[32px] leading-tight">Sell</h3>
            <div className="flex gap-1 mt-2">
              <TabButton id="pack" active={sellTab === "pack"} onClick={() => setSellTab("pack")}>Pack</TabButton>
              <TabButton id="haversack" active={sellTab === "haversack"} onClick={() => setSellTab("haversack")}>Haversack</TabButton>
              <TabButton id="equipped" active={sellTab === "equipped"} onClick={() => setSellTab("equipped")}>Equipped</TabButton>
            </div>
          </div>

          {/* Staged sells chip bar */}
          {pendingSells.length > 0 && (
            <div className="px-3 pb-2 flex flex-wrap gap-1">
              {(() => {
                // Group by defId for compact display
                const counts = new Map<string, { name: string; count: number }>();
                for (const defId of pendingSells) {
                  const existing = counts.get(defId);
                  if (existing) { existing.count++; continue; }
                  // Find name from inventory
                  const item = inventory?.pack.find(i => i.defId === defId)
                    ?? inventory?.haversack.find(i => i.defId === defId)
                    ?? inventory?.equipment.weapon?.defId === defId ? inventory?.equipment.weapon
                    : inventory?.equipment.armor?.defId === defId ? inventory?.equipment.armor
                    : inventory?.equipment.boots?.defId === defId ? inventory?.equipment.boots
                    : null;
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
            {sellItems.length === 0 && sellTab === "equipped" ? (
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
                      <div className="w-8 h-8 flex-shrink-0 flex items-center justify-center">
                        <MaskedIcon icon={itemTypeIcon(item.type)} className="w-5 h-5" color="#D0BD62" />
                      </div>
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
                  { length: inventory.packCapacity - sellItems.length },
                  (_, i) => (
                    <div key={`empty-${i}`} className="flex items-center justify-center bg-btn/50 p-4 border border-dashed border-edge text-muted">
                      Empty slot
                    </div>
                  )
                )}
                {sellTab === "haversack" && inventory && Array.from(
                  { length: inventory.haversackCapacity - sellItems.length },
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
