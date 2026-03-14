import { useState, useEffect } from "react";
import type { GameResponse, SkillInfoDto, InventoryInfo, ItemInfo, MechanicsInfo, MechanicLine } from "../api/types";
import { useGame } from "../GameContext";
import MaskedIcon, { iconUrl, itemTypeIcon, TabButton } from "../components/MaskedIcon";
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

const CONDITION_ICONS: Record<string, string> = {
  freezing: "mountains.svg",
  thirsty: "water-drop.svg",
  lattice_sickness: "foamy-disc.svg",
  poisoned: "foamy-disc.svg",
  irradiated: "foamy-disc.svg",
  exhausted: "tread.svg",
  lost: "compass.svg",
  injured: "bloody-stash.svg",
  disheartened: "sensuousness.svg",
};


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

      {/* Three-column layout */}
      <div className="flex-1 flex overflow-hidden">
        {/* Left: Character Panel */}
        <div className="flex-1 flex flex-col border-r border-edge overflow-y-auto">
          <CharacterPanel
            name={status.name}
            skills={status.skills}
            conditions={status.conditions}
          />
        </div>

        {/* Middle: Inventory */}
        <div className="flex-1 flex flex-col min-w-0 overflow-hidden border-r border-edge">
          {inventory ? (
            <InventoryPanel inventory={inventory} />
          ) : (
            <div className="p-4 text-muted">No inventory data</div>
          )}
        </div>

        {/* Right: Mechanics — fixed 420px */}
        <div className="w-[420px] flex flex-col overflow-y-auto flex-shrink-0">
          {state.mechanics ? (
            <MechanicsPanel mechanics={state.mechanics} />
          ) : (
            <div className="p-4 text-muted">No mechanics data</div>
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
            >
              {skill.formatted}
            </div>
            <div className="min-w-0">
              <div className="text-primary">{skill.name}</div>
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

function MechanicsPanel({ mechanics }: { mechanics: MechanicsInfo }) {
  return (
    <div className="p-4">
      <h2 className="font-header text-accent text-[32px] leading-tight mb-4">
        Mechanics
      </h2>

      {mechanics.resistances.length > 0 && (
        <MechanicsSection title="Resistances" lines={mechanics.resistances} />
      )}

      {mechanics.encounterChecks.length > 0 && (
        <MechanicsSection title="Encounter Checks" lines={mechanics.encounterChecks} />
      )}

      {mechanics.other.length > 0 && (
        <MechanicsSection title="Other" lines={mechanics.other} />
      )}
    </div>
  );
}

function MechanicsSection({ title, lines }: { title: string; lines: MechanicLine[] }) {
  return (
    <div className="mb-3">
      <div className="font-bold mb-1 flex justify-between">
        <span>{title}</span>
        <span className="text-muted font-normal">Source</span>
      </div>
      <table className="w-full">
        <tbody>
          {lines.map((line, i) => (
            <tr key={i}>
              <td className="py-0.5 pr-2 whitespace-nowrap">
                {line.label} <span className="font-bold text-accent">{line.value}</span>
              </td>
              <td className="py-0.5 pl-2 text-muted text-right">{line.source}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

type InventoryTab = "pack" | "haversack" | "equipped";

function InventoryPanel({ inventory }: { inventory: InventoryInfo }) {
  const { doAction, loading } = useGame();
  const [tab, setTab] = useState<InventoryTab>("pack");

  return (
    <div className="flex flex-col h-full">
      {/* Header + tabs */}
      <div className="p-4 pb-0">
        <h2 className="font-header text-accent text-[32px] leading-tight mb-3">
          Inventory
        </h2>
        <div className="flex gap-1">
          <TabButton id="pack" active={tab === "pack"} onClick={() => setTab("pack")}>
            Pack
          </TabButton>
          <TabButton id="haversack" active={tab === "haversack"} onClick={() => setTab("haversack")}>
            Haversack
          </TabButton>
          <TabButton id="equipped" active={tab === "equipped"} onClick={() => setTab("equipped")}>
            Equipped
          </TabButton>
        </div>
      </div>

      {/* Tab content */}
      <div className="flex-1 overflow-y-auto p-4 space-y-2">
        {tab === "pack" && (
          <PackTab items={inventory.pack} capacity={inventory.packCapacity} doAction={doAction} loading={loading} />
        )}
        {tab === "haversack" && (
          <HaversackTab items={inventory.haversack} capacity={inventory.haversackCapacity} doAction={doAction} loading={loading} />
        )}
        {tab === "equipped" && (
          <EquippedTab equipment={inventory.equipment} doAction={doAction} loading={loading} />
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
  for (const [cond, val] of Object.entries(item.resistModifiers)) {
    if (val !== 0) parts.push(`${val > 0 ? "+" : ""}${val} resist ${cond}`);
  }
  if (item.foragingBonus) parts.push(`+${item.foragingBonus} foraging`);
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
      <div className="w-10 h-10 flex-shrink-0 flex items-center justify-center">
        <MaskedIcon icon={itemTypeIcon(item.type)} className="w-6 h-6" color="#D0BD62" />
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
            <div className="text-primary">
              {item.name}
              {item.cost != null && item.cost > 0 && (
                <span className="text-accent ml-2">{item.cost}g</span>
              )}
            </div>
            {mods && (
              <div className="text-dim mt-0.5 truncate" title={mods}>{mods}</div>
            )}
            {item.description && !mods && (
              <div className="text-muted mt-0.5 truncate" title={item.description}>{item.description}</div>
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
          <MaskedIcon icon="cancel.svg" className="w-5 h-5" color="currentColor" />
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

function PackTab({
  items,
  capacity,
  doAction,
  loading,
}: {
  items: ItemInfo[];
  capacity: number;
  doAction: (body: { action: string; itemId?: string }) => void;
  loading: boolean;
}) {
  const emptySlots = capacity - items.length;
  return (
    <>
      {items.map((item, i) => (
        <ItemCard key={i} item={item} actions={
          <>
            {item.isEquippable && (
              <Button variant="secondary" size="icon" disabled={loading} title="Equip"
                onClick={() => doAction({ action: "equip", itemId: item.defId })}>
                <MaskedIcon icon="barbute.svg" className="w-5 h-5" color="currentColor" />
              </Button>
            )}
            <DiscardButton item={item} doAction={doAction} loading={loading} />
          </>
        } />
      ))}
      {Array.from({ length: emptySlots }, (_, i) => (
        <div key={`empty-${i}`} className="flex items-center justify-center bg-btn/50 p-4 border border-dashed border-edge text-muted">
          Empty slot
        </div>
      ))}
    </>
  );
}

function HaversackTab({
  items,
  capacity,
  doAction,
  loading,
}: {
  items: ItemInfo[];
  capacity: number;
  doAction: (body: { action: string; itemId?: string }) => void;
  loading: boolean;
}) {
  const emptySlots = capacity - items.length;
  return (
    <>
      {[...items].sort((a, b) => a.name.localeCompare(b.name)).map((item, i) => (
        <ItemCard key={i} item={item} actions={
          <DiscardButton item={item} doAction={doAction} loading={loading} />
        } />
      ))}
      {Array.from({ length: emptySlots }, (_, i) => (
        <div key={`empty-${i}`} className="flex items-center justify-center bg-btn/50 p-4 border border-dashed border-edge text-muted">
          Empty slot
        </div>
      ))}
    </>
  );
}

const EQUIP_SLOTS = ["weapon", "armor", "boots"] as const;

function EquippedTab({
  equipment,
  doAction,
  loading,
}: {
  equipment: { weapon: ItemInfo | null; armor: ItemInfo | null; boots: ItemInfo | null };
  doAction: (body: { action: string; slot?: string }) => void;
  loading: boolean;
}) {
  return (
    <>
      {EQUIP_SLOTS.map((slot) => {
        const item = equipment[slot];
        return item ? (
          <ItemCard key={slot} item={item} actions={
            <Button variant="secondary" size="icon" disabled={loading} title="Unequip"
              onClick={() => doAction({ action: "unequip", slot })}>
              <MaskedIcon icon="cancel.svg" className="w-5 h-5" color="currentColor" />
            </Button>
          } />
        ) : (
          <div key={slot} className="flex items-center justify-center bg-btn/50 p-4 border border-dashed border-edge text-muted">
            Empty {slot} slot
          </div>
        );
      })}
    </>
  );
}
