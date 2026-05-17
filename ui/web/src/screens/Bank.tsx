import { useState, useEffect, useMemo } from "react";
import { useGame } from "../GameContext";
import type { GameResponse, ItemInfo, BankResponse } from "../api/types";
import * as api from "../api/client";
import MaskedIcon, { itemTypeIcon } from "../components/MaskedIcon";
import WaxSeal from "../components/WaxSeal";
import { getSealVariant, getSealSymbolIndex } from "../marketNaming";
import TopBar from "../components/TopBar";
import { Button } from "@/components/ui/button";

const GEAR_TYPES = new Set(["weapon", "armor"]);
const SUPPLY_DEFS = new Set(["food_ration", "medical_kit"]);

type CarriedGroup = "equipped_gear" | "unequipped_gear" | "supplies" | "tools" | "hauls";

function classifyCarriedItem(item: ItemInfo): CarriedGroup {
  if (item.type === "haul") return "hauls";
  if (GEAR_TYPES.has(item.type)) return item.isEquipped ? "equipped_gear" : "unequipped_gear";
  if (item.type === "tool") return SUPPLY_DEFS.has(item.defId) ? "supplies" : "tools";
  return "supplies";
}

const CARRIED_GROUP_LABELS: Record<CarriedGroup, string> = {
  equipped_gear: "Equipped Gear",
  unequipped_gear: "Unequipped Gear",
  supplies: "Supplies",
  tools: "Tools",
  hauls: "Contracts",
};
const CARRIED_GROUP_ORDER: CarriedGroup[] = ["equipped_gear", "unequipped_gear", "supplies", "tools", "hauls"];

export default function BankScreen({
  state,
  onBack,
}: {
  state: GameResponse;
  onBack: () => void;
}) {
  const { gameId, doAction, loading } = useGame();
  const [bankData, setBankData] = useState<BankResponse | null>(null);
  const [loadingBank, setLoadingBank] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    function onKeyDown(e: KeyboardEvent) {
      if (e.key === "Escape") onBack();
    }
    window.addEventListener("keydown", onKeyDown);
    return () => window.removeEventListener("keydown", onKeyDown);
  }, [onBack]);

  useEffect(() => {
    if (!gameId) return;
    setLoadingBank(true);
    api
      .getBank(gameId)
      .then(setBankData)
      .catch((e) => setError(e.message))
      .finally(() => setLoadingBank(false));
  }, [gameId]);

  const inventory = state.inventory;

  const carriedGroups = useMemo((): { group: CarriedGroup; label: string; items: ItemInfo[] }[] => {
    if (!inventory) return [];
    const grouped = new Map<CarriedGroup, ItemInfo[]>();
    for (const g of CARRIED_GROUP_ORDER) grouped.set(g, []);
    for (const item of inventory.pack) {
      const g = classifyCarriedItem(item);
      grouped.get(g)!.push(item);
    }
    return CARRIED_GROUP_ORDER
      .map(g => ({ group: g, label: CARRIED_GROUP_LABELS[g], items: grouped.get(g)! }))
      .filter(g => g.items.length > 0);
  }, [inventory]);

  const bankFull = (bankData?.items.length ?? 0) >= (bankData?.capacity ?? 10);
  const packFull = (inventory?.pack.length ?? 0) >= (inventory?.packCapacity ?? 0);

  async function deposit(defId: string, source: string) {
    if (!gameId) return;
    setError(null);
    const result = await doAction({ action: "bank_deposit", itemId: defId, source });
    if (result && gameId) {
      const updated = await api.getBank(gameId);
      setBankData(updated);
    }
  }

  async function withdraw(bankIndex: number) {
    if (!gameId) return;
    setError(null);
    const result = await doAction({ action: "bank_withdraw", bankIndex });
    if (result && gameId) {
      const updated = await api.getBank(gameId);
      setBankData(updated);
    }
  }

  function canWithdraw(_item: ItemInfo): boolean {
    return !packFull;
  }

  return (
    <div className="h-full flex flex-col bg-page text-primary">
      {error && (
        <div className="px-4 py-2 text-sm text-center text-negative bg-panel border-b border-edge">
          {error}
        </div>
      )}

      <TopBar status={state.status} onBack={onBack} />

      <div className="flex-1 flex overflow-hidden">
        {/* STORED column (bank contents) */}
        <div className="flex-1 flex flex-col border-r border-edge min-w-0">
          <div className="p-3">
            <h3 className="font-header text-accent text-[32px] leading-tight">Stored</h3>
            {bankData && (
              <div className="text-muted mt-1">
                {bankData.items.length}/{bankData.capacity} slots
              </div>
            )}
          </div>
          <div className="flex-1 overflow-y-auto p-2 space-y-2">
            {loadingBank ? (
              <div className="p-4 text-muted">Loading...</div>
            ) : !bankData || bankData.items.length === 0 ? (
              <div className="p-4 text-muted">No items stored</div>
            ) : (
              bankData.items.map((item, i) => (
                <div key={`bank-${i}`} className="flex items-start gap-3 p-3 rounded-lg" style={{ backgroundColor: "rgba(0, 0, 0, 0.35)" }}>
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
                    <div className="text-primary">{item.name}</div>
                    {item.description && (
                      <div className="text-muted mt-0.5 truncate">{item.description}</div>
                    )}
                  </div>
                  <Button variant="secondary" size="sm" onClick={() => withdraw(i)} disabled={loading || !canWithdraw(item)} className="flex-shrink-0">
                    <MaskedIcon icon="receive-money.svg" className="w-4 h-4" color="currentColor" />
                    Withdraw
                  </Button>
                </div>
              ))
            )}
          </div>
        </div>

        {/* CARRIED column (player inventory, grouped) */}
        <div className="flex-1 flex flex-col min-w-0">
          <div className="p-3">
            <h3 className="font-header text-accent text-[32px] leading-tight">Carried</h3>
            {inventory && (
              <div className="text-muted mt-1">
                {inventory.pack.length}/{inventory.packCapacity} slots
              </div>
            )}
          </div>
          <div className="flex-1 overflow-y-auto p-2 space-y-2">
            {carriedGroups.length === 0 ? (
              <div className="p-4 text-muted">Nothing here</div>
            ) : (
              carriedGroups.map(({ group, label, items }) => (
                <div key={group}>
                  <div className="sticky top-0 z-10 py-1 mb-1 text-muted font-bold text-[14px] uppercase tracking-wide bg-page/90 border-b border-edge/40">
                    {label}
                  </div>
                  <div className="space-y-2">
                    {items.map((item, i) => (
                      <div
                        key={`${group}-${item.defId}-${i}`}
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
                          <div className="text-primary flex items-center gap-2">
                            {item.name}
                            {item.isEquipped && (
                              <span className="text-accent text-[12px] font-bold uppercase tracking-wide border border-accent/40 px-1 rounded">
                                equipped
                              </span>
                            )}
                          </div>
                          {item.description && (
                            <div className="text-muted mt-0.5 truncate">{item.description}</div>
                          )}
                        </div>
                        {item.defId !== "food_ration" && (
                          <Button variant="secondary" size="sm" onClick={() => deposit(item.defId, item.isEquipped ? item.type : "pack")} disabled={loading || bankFull} className="flex-shrink-0">
                            <MaskedIcon icon="pay-money.svg" className="w-4 h-4" color="currentColor" />
                            Deposit
                          </Button>
                        )}
                      </div>
                    ))}
                  </div>
                </div>
              ))
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
