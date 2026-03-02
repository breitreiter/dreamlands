import { useState, useEffect, useMemo } from "react";
import { useGame } from "../GameContext";
import type { GameResponse, ItemInfo, BankResponse } from "../api/types";
import * as api from "../api/client";

const PACK_TYPES = new Set(["weapon", "armor", "boots", "tool", "tradegood"]);

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

const TAB_ICONS: Record<string, string> = {
  pack: "backpack.svg",
  haversack: "knapsack.svg",
  equipped: "sword-brandish.svg",
};

type CarriedTab = "pack" | "haversack" | "equipped";

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
  const [carriedTab, setCarriedTab] = useState<CarriedTab>("pack");

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

  const carriedItems = useMemo((): { item: ItemInfo; source: string }[] => {
    if (!inventory) return [];
    switch (carriedTab) {
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
  }, [inventory, carriedTab]);

  const bankFull = (bankData?.items.length ?? 0) >= (bankData?.capacity ?? 10);
  const packFull = (inventory?.pack.length ?? 0) >= (inventory?.packCapacity ?? 0);
  const haversackFull = (inventory?.haversack.length ?? 0) >= (inventory?.haversackCapacity ?? 0);

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

  function canWithdraw(item: ItemInfo): boolean {
    const isPackItem = PACK_TYPES.has(item.type);
    return isPackItem ? !packFull : !haversackFull;
  }

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
      {error && (
        <div className="px-4 py-2 text-sm text-center text-negative bg-panel border-b border-edge">
          {error}
        </div>
      )}

      <div className="flex-1 flex overflow-hidden">
        {/* STORED column (bank contents) */}
        <div className="flex-1 flex flex-col border-r border-edge min-w-0">
          <div className="p-3 border-b border-edge flex items-center justify-between">
            <div>
              <h3 className="font-header text-accent text-[32px] leading-tight">Stored</h3>
              {bankData && (
                <div className="text-muted mt-1">
                  {bankData.items.length}/{bankData.capacity} slots
                </div>
              )}
            </div>
            <button
              onClick={onBack}
              className="px-4 py-2 rounded-lg bg-btn text-action hover:text-action-hover transition-colors"
            >
              Leave Bank
            </button>
          </div>
          <div className="flex-1 overflow-y-auto p-2 space-y-2">
            {loadingBank ? (
              <div className="p-4 text-muted">Loading...</div>
            ) : !bankData || bankData.items.length === 0 ? (
              <div className="p-4 text-muted">No items stored</div>
            ) : (
              bankData.items.map((item, i) => (
                <div key={`bank-${i}`} className="flex items-start gap-3 p-3 rounded-lg" style={{ backgroundColor: "rgba(0, 0, 0, 0.35)" }}>
                  <div className="w-8 h-8 flex-shrink-0 flex items-center justify-center">
                    <MaskedIcon icon={itemTypeIcon(item.type)} className="w-5 h-5" color="#D0BD62" />
                  </div>
                  <div className="flex-1 min-w-0">
                    <div className="text-primary">{item.name}</div>
                    {item.description && (
                      <div className="text-muted mt-0.5 truncate">{item.description}</div>
                    )}
                  </div>
                  <button
                    onClick={() => withdraw(i)}
                    disabled={loading || !canWithdraw(item)}
                    className="px-3 py-1 rounded-lg disabled:opacity-40
                               text-action hover:text-action-hover transition-colors flex items-center gap-1 flex-shrink-0"
                    style={{ backgroundColor: "rgba(13, 13, 13, 0.8)" }}
                  >
                    <MaskedIcon icon="receive-money.svg" className="w-4 h-4" color="currentColor" />
                    Withdraw
                  </button>
                </div>
              ))
            )}
          </div>
        </div>

        {/* CARRIED column (player inventory) */}
        <div className="flex-1 flex flex-col min-w-0">
          <div className="p-3 border-b border-edge">
            <h3 className="font-header text-accent text-[32px] leading-tight">Carried</h3>
            <div className="flex gap-1 mt-2">
              <TabBtn id="pack" active={carriedTab === "pack"} onClick={() => setCarriedTab("pack")}>Pack</TabBtn>
              <TabBtn id="haversack" active={carriedTab === "haversack"} onClick={() => setCarriedTab("haversack")}>Haversack</TabBtn>
              <TabBtn id="equipped" active={carriedTab === "equipped"} onClick={() => setCarriedTab("equipped")}>Equipped</TabBtn>
            </div>
          </div>
          <div className="flex-1 overflow-y-auto p-2 space-y-2">
            {carriedItems.length === 0 ? (
              <div className="p-4 text-muted">Nothing here</div>
            ) : (
              carriedItems.map(({ item, source }, i) => (
                <div
                  key={`${source}-${item.defId}-${i}`}
                  className="flex items-start gap-3 p-3 rounded-lg"
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
                    {carriedTab === "equipped" && (
                      <div className="text-dim mt-0.5">equipped ({source})</div>
                    )}
                  </div>
                  <button
                    onClick={() => deposit(item.defId, source)}
                    disabled={loading || bankFull}
                    className="px-3 py-1 rounded-lg disabled:opacity-40
                               text-action hover:text-action-hover transition-colors flex items-center gap-1 flex-shrink-0"
                    style={{ backgroundColor: "rgba(13, 13, 13, 0.8)" }}
                  >
                    <MaskedIcon icon="pay-money.svg" className="w-4 h-4" color="currentColor" />
                    Deposit
                  </button>
                </div>
              ))
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
