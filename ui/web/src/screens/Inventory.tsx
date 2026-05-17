import { useEffect } from "react";
import type { GameResponse, SkillInfoDto, InventoryInfo, ItemInfo } from "../api/types";
import { useGame } from "../GameContext";
import MaskedIcon, { iconUrl, itemTypeIcon } from "../components/MaskedIcon";
import HaulItem from "../components/HaulItem";
import WaxSeal from "../components/WaxSeal";
import { getSealVariant, getSealSymbolIndex } from "../marketNaming";
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

const CONDITION_ICONS: Record<string, string> = {
  freezing: "mountains.svg",
  thirsty: "water-drop.svg",
  lattice_sickness: "foamy-disc.svg",
  poisoned: "foamy-disc.svg",
  irradiated: "foamy-disc.svg",
  exhausted: "tread.svg",
  lost: "compass.svg",
  injured: "bloody-stash.svg",
};

// Gear types — weapon, armor
const GEAR_TYPES = new Set(["weapon", "armor"]);
const GEAR_ORDER = ["weapon", "armor"];

// Supply defIds — food and medical kit treated as supplies
const SUPPLY_DEFS = new Set(["food_ration", "medical_kit"]);

function classifyItem(item: ItemInfo): "equipped_gear" | "unequipped_gear" | "supplies" | "tools" | "hauls" {
  if (item.type === "haul") return "hauls";
  if (GEAR_TYPES.has(item.type)) return item.isEquipped ? "equipped_gear" : "unequipped_gear";
  if (item.type === "tool") return SUPPLY_DEFS.has(item.defId) ? "supplies" : "tools";
  // consumable or anything else defaults to supplies
  return "supplies";
}

function gearSortOrder(type: string): number {
  return GEAR_ORDER.indexOf(type) >= 0 ? GEAR_ORDER.indexOf(type) : 99;
}

export default function Inventory({
  state,
  onClose,
}: {
  state: GameResponse;
  onClose: () => void;
}) {
  const { status } = state;
  const inventory = state.inventory;

  useEffect(() => {
    function onKeyDown(e: KeyboardEvent) {
      if (e.key === "Escape") onClose();
    }
    window.addEventListener("keydown", onKeyDown);
    return () => window.removeEventListener("keydown", onKeyDown);
  }, [onClose]);

  return (
    <div className="h-full flex flex-col bg-page text-primary">
      <TopBar status={status} onBack={onClose} />

      {/* Two-column layout */}
      <div className="flex-1 flex overflow-hidden">
        {/* Left: Character Panel */}
        <div className="flex-1 flex flex-col border-r border-edge overflow-y-auto">
          <CharacterPanel
            name={status.name}
            skills={status.skills}
            conditions={status.conditions}
          />
        </div>

        {/* Right: Inventory */}
        <div className="flex-1 flex flex-col min-w-0 overflow-hidden">
          {inventory ? (
            <InventoryPanel inventory={inventory} />
          ) : (
            <div className="p-4 text-muted">No inventory data</div>
          )}
        </div>
      </div>

    </div>
  );
}

function CharacterPanel({
  name,
  skills,
  conditions,
}: {
  name: string;
  skills: SkillInfoDto[];
  conditions: GameResponse["status"]["conditions"];
}) {
  return (
    <div className="flex flex-col h-full p-4">
      <h2 className="font-header text-accent text-[32px] leading-tight mb-4">
        {name || "The Merchant"}
      </h2>

      {/* Skills */}
      <div className="space-y-3">
        {skills.map((skill) => (
          <div key={skill.id} className="flex items-start gap-3">
            <div
              className="w-10 h-10 rounded-full border-2 flex items-center justify-center font-bold flex-shrink-0 mt-0.5 text-accent border-accent"
              title={skill.formatted}
            >
              {skill.formatted.charAt(0)}
            </div>
            <div className="min-w-0">
              <div className="text-primary">
                {skill.name}
                <span className="text-dim ml-2 font-normal">{skill.formatted}</span>
              </div>
              {skill.flavor && (
                <div className="text-dim leading-snug mt-0.5">{skill.flavor}</div>
              )}
            </div>
          </div>
        ))}
      </div>

      {/* Conditions */}
      {conditions.length > 0 && (
        <div className="mt-6 space-y-2">
          {conditions.map((c) => (
            <div key={c.id} className="flex items-start gap-2">
              <img
                src={iconUrl(CONDITION_ICONS[c.id] || "sun.svg")}
                alt=""
                className="w-5 h-5 flex-shrink-0 mt-0.5"
              />
              <div>
                <span className="text-negative">
                  {c.name}
                  {c.stacks > 1 && <span className="text-muted ml-1">x{c.stacks}</span>}
                </span>
                {c.effect && <div className="text-dim leading-snug">{c.effect}</div>}
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}

function InventoryPanel({ inventory }: { inventory: InventoryInfo }) {
  const { doAction, loading } = useGame();
  const pack = inventory.pack;

  // Classify and sort items into groups
  const groups: { label: string; key: string; items: ItemInfo[] }[] = [
    { label: "Equipped Gear", key: "equipped_gear", items: [] },
    { label: "Unequipped Gear", key: "unequipped_gear", items: [] },
    { label: "Supplies", key: "supplies", items: [] },
    { label: "Tools", key: "tools", items: [] },
    { label: "Contracts", key: "hauls", items: [] },
  ];

  const groupMap = Object.fromEntries(groups.map(g => [g.key, g.items]));

  for (const item of pack) {
    const cat = classifyItem(item);
    groupMap[cat].push(item);
  }

  // Sort equipped/unequipped gear by weapon→armor→boots
  groupMap["equipped_gear"].sort((a, b) => gearSortOrder(a.type) - gearSortOrder(b.type));
  groupMap["unequipped_gear"].sort((a, b) => gearSortOrder(a.type) - gearSortOrder(b.type));

  const packCount = pack.length;
  const packCapacity = inventory.packCapacity;

  return (
    <div className="flex flex-col h-full">
      <div className="p-4 pb-2">
        <h2 className="font-header text-accent text-[32px] leading-tight">
          Inventory
        </h2>
        <div className="text-muted mt-1">
          {packCount} / {packCapacity} slots
        </div>
      </div>

      <div className="flex-1 overflow-y-auto p-4 space-y-2">
        {groups.map(({ label, key, items }) => {
          if (items.length === 0) return null;
          return (
            <div key={key}>
              {/* Sticky group header */}
              <div className="sticky top-0 z-10 py-1 mb-1 text-muted font-bold text-[14px] uppercase tracking-wide bg-page/90 border-b border-edge/40">
                {label}
              </div>
              <div className="space-y-2">
                {items.map((item, i) => (
                  <ItemCard
                    key={`${key}-${item.defId}-${i}`}
                    item={item}
                    actions={
                      <>
                        {item.isEquipped && (
                          <Button variant="secondary" size="icon" disabled={loading} title="Unequip"
                            onClick={() => doAction({ action: "unequip", slot: item.type })}>
                            <MaskedIcon icon="cancel.svg" className="w-5 h-5" color="currentColor" />
                          </Button>
                        )}
                        {!item.isEquipped && item.isEquippable && (
                          <Button variant="secondary" size="icon" disabled={loading} title="Equip"
                            onClick={() => doAction({ action: "equip", itemId: item.defId })}>
                            <MaskedIcon icon="barbute.svg" className="w-5 h-5" color="currentColor" />
                          </Button>
                        )}
                        <DiscardButton item={item} doAction={doAction} loading={loading} />
                      </>
                    }
                  />
                ))}
              </div>
            </div>
          );
        })}

        {pack.length === 0 && (
          <div className="p-4 text-muted">Pack is empty.</div>
        )}
      </div>
    </div>
  );
}

function itemModifierSummary(item: ItemInfo): string {
  const parts: string[] = [];
  for (const [skill, val] of Object.entries(item.skillModifiers)) {
    if (val !== 0) parts.push(`${val > 0 ? "+" : ""}${val} ${skill}`);
  }
  if (item.cures.length > 0) parts.push(`cures ${item.cures.join(", ")}`);
  return parts.join(", ");
}

function ItemCard({
  item,
  actions,
}: {
  item: ItemInfo;
  actions: React.ReactNode;
}) {
  const mods = itemModifierSummary(item);
  return (
    <div className="flex items-start gap-3 p-3 rounded-lg" style={{ backgroundColor: "rgba(0, 0, 0, 0.35)" }}>
      {item.type === "haul" ? (
        <WaxSeal
          variant={getSealVariant(item.haulOfferId ?? item.defId)}
          symbolIndex={getSealSymbolIndex(item.haulOfferId ?? item.defId)}
        />
      ) : (
        <div className="w-10 h-10 flex-shrink-0 flex items-center justify-center">
          <MaskedIcon icon={itemTypeIcon(item.type)} className="w-6 h-6" color="#D0BD62" />
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
            <div className="text-primary flex items-center gap-2">
              {item.name}
              {item.isEquipped && (
                <span className="text-accent text-[12px] font-bold uppercase tracking-wide border border-accent/40 px-1 rounded">
                  equipped
                </span>
              )}
              {item.cost != null && item.cost > 0 && (
                <span className="text-accent ml-1">{item.cost}g</span>
              )}
            </div>
            {mods && (
              <div className="text-dim mt-0.5 truncate" title={mods}>{mods}</div>
            )}
            {item.description && (
              <div className="text-muted mt-0.5 truncate" title={item.description}>{item.description}</div>
            )}
            {item.moves.length > 0 && (
              <div className="text-dim mt-0.5">
                Moves: {item.moves.join(" · ")}
              </div>
            )}
          </>
        )}
      </div>
      <div className="flex gap-1 flex-shrink-0 items-center">
        {actions}
      </div>
    </div>
  );
}

function DiscardButton({ item, doAction, loading }: { item: ItemInfo; doAction: (body: { action: string; itemId?: string }) => void; loading: boolean }) {
  return (
    <AlertDialog>
      <AlertDialogTrigger asChild>
        <Button variant="secondary" size="icon" disabled={loading} title="Discard">
          <MaskedIcon icon="trash-can.svg" className="w-5 h-5" color="currentColor" />
        </Button>
      </AlertDialogTrigger>
      <AlertDialogContent size="sm">
        <AlertDialogHeader>
          <AlertDialogTitle>Destroy {item.name}?</AlertDialogTitle>
          <AlertDialogDescription>This item will be lost forever.</AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel>
            <MaskedIcon icon="cancel.svg" className="w-4 h-4" color="currentColor" />
            Keep
          </AlertDialogCancel>
          <AlertDialogAction variant="destructive" onClick={() => doAction({ action: "discard", itemId: item.defId })}>
            <MaskedIcon icon="trash-can.svg" className="w-4 h-4" color="currentColor" />
            Destroy
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  );
}
