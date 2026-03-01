import { useState } from "react";
import type { GameResponse, SkillInfoDto, InventoryInfo, ItemInfo, MechanicsInfo, MechanicLine } from "../api/types";
import { useGame } from "../GameContext";
import StatBar, { HEALTH_GRADIENT, SPIRITS_GRADIENT } from "../components/StatBar";

const CONDITION_ICONS: Record<string, string> = {
  freezing: "mountains.svg",
  hungry: "pouch-with-beads.svg",
  thirsty: "water-drop.svg",
  poisoned: "foamy-disc.svg",
  swamp_fever: "foamy-disc.svg",
  gut_worms: "foamy-disc.svg",
  irradiated: "foamy-disc.svg",
  exhausted: "tread.svg",
  lost: "treasure-map.svg",
  injured: "bloody-stash.svg",
};

const ITEM_TYPE_ICONS: Record<string, string> = {
  weapon: "sword-brandish.svg",
  armor: "chain-mail.svg",
  boots: "boots.svg",
  tool: "knapsack.svg",
  consumable: "pouch-with-beads.svg",
  tradegood: "two-coins.svg",
};

const TAB_ICONS: Record<string, string> = {
  pack: "backpack.svg",
  haversack: "knapsack.svg",
  equipped: "sword-brandish.svg",
};

function iconUrl(file: string): string {
  return `/world/assets/icons/${file}`;
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

export default function Inventory({
  state,
  onClose,
}: {
  state: GameResponse;
  onClose: () => void;
}) {
  const { status } = state;
  const inventory = state.inventory;

  return (
    <div className="h-full flex flex-col bg-page text-primary relative">
      {/* Close button — top right */}
      <button
        onClick={onClose}
        className="absolute top-3 right-4 z-10 w-8 h-8 flex items-center justify-center
                   text-action hover:text-action-hover transition-colors"
        title="Close"
      >
        ✕
      </button>

      {/* Three-column layout */}
      <div className="flex-1 flex overflow-hidden">
        {/* Left: Character Panel */}
        <div className="flex-1 flex flex-col border-r border-edge overflow-y-auto">
          <CharacterPanel
            skills={status.skills}
            health={status.health}
            maxHealth={status.maxHealth}
            spirits={status.spirits}
            maxSpirits={status.maxSpirits}
          />
        </div>

        {/* Middle: Mechanics — parchment background, fixed 380px */}
        <div className="w-[420px] flex flex-col border-r border-edge overflow-y-auto flex-shrink-0 bg-parchment text-contrast">
          {state.mechanics ? (
            <MechanicsPanel mechanics={state.mechanics} />
          ) : (
            <div className="p-4 text-contrast/50">No mechanics data</div>
          )}
        </div>

        {/* Right: Inventory */}
        <div className="flex-1 flex flex-col">
          {inventory ? (
            <InventoryPanel inventory={inventory} />
          ) : (
            <div className="p-4 text-muted">No inventory data</div>
          )}
        </div>
      </div>

      {/* Conditions footer — full-width strip */}
      {status.conditions.length > 0 && (
        <div className="px-4 py-2 border-t border-edge bg-panel-alt flex flex-wrap gap-x-6 gap-y-1">
          {status.conditions.map((c) => (
            <div key={c.id} className="flex items-center gap-1.5">
              <img
                src={iconUrl(CONDITION_ICONS[c.id] || "sun.svg")}
                alt=""
                className="w-5 h-5 flex-shrink-0"
              />
              <span className="text-negative">
                {c.name}
                {c.stacks > 1 && <span className="text-muted ml-1">x{c.stacks}</span>}
              </span>
              {c.description && (
                <span className="text-muted">{c.description}</span>
              )}
            </div>
          ))}
        </div>
      )}
    </div>
  );
}

function CharacterPanel({
  skills,
  health,
  maxHealth,
  spirits,
  maxSpirits,
}: {
  skills: SkillInfoDto[];
  health: number;
  maxHealth: number;
  spirits: number;
  maxSpirits: number;
}) {
  return (
    <div className="flex flex-col h-full p-4">
      <h2 className="font-header text-accent text-[32px] leading-tight mb-4">
        The Merchant
      </h2>

      {/* Vitals */}
      <div className="space-y-2 mb-6">
        <StatBar label="Health" value={health} max={maxHealth} gradient={HEALTH_GRADIENT} />
        <StatBar label="Spirits" value={spirits} max={maxSpirits} gradient={SPIRITS_GRADIENT} />
      </div>

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
    </div>
  );
}

function MechanicsPanel({ mechanics }: { mechanics: MechanicsInfo }) {
  return (
    <div className="p-4">
      <h2 className="font-header text-parchment-text text-[32px] leading-tight mb-4">
        Mechanics
      </h2>

      {mechanics.resistances.length > 0 && (
        <MechanicsSection title="Resistances" lines={mechanics.resistances} iconType="resistance" />
      )}

      {mechanics.encounterChecks.length > 0 && (
        <MechanicsSection title="Encounter Checks" lines={mechanics.encounterChecks} iconType="skill" />
      )}

      {mechanics.other.length > 0 && (
        <MechanicsSection title="Other" lines={mechanics.other} iconType="other" />
      )}
    </div>
  );
}

const RESISTANCE_ICONS: Record<string, string> = {
  ...CONDITION_ICONS,
};

const OTHER_ICONS: Record<string, string> = {
  "better prices": "pay-money.svg",
  "reroll any failure": "foamy-disc.svg",
  "foraging checks": "knapsack.svg",
};

function mechanicIcon(label: string, iconType: string): string | null {
  const key = label.toLowerCase();
  if (iconType === "resistance") return RESISTANCE_ICONS[key] || null;
  if (iconType === "skill") return "sun.svg";
  for (const [pattern, icon] of Object.entries(OTHER_ICONS)) {
    if (key.includes(pattern)) return icon;
  }
  return null;
}

function MechanicsSection({ title, lines, iconType }: { title: string; lines: MechanicLine[]; iconType: string }) {
  return (
    <div className="mb-3">
      <div className="font-bold mb-1 flex justify-between">
        <span>{title}</span>
        <span className="text-contrast/50 font-normal">Source</span>
      </div>
      <table className="w-full">
        <tbody>
          {lines.map((line, i) => {
            const icon = mechanicIcon(line.label, iconType);
            return (
              <tr key={i}>
                <td className="py-0.5 pr-2 whitespace-nowrap">
                  <span className="inline-flex items-center gap-1.5">
                    {icon && (
                      <MaskedIcon icon={icon} className="w-5 h-5" color="var(--color-contrast)" />
                    )}
                    {line.label} <span className="font-bold">{line.value}</span>
                  </span>
                </td>
                <td className="py-0.5 pl-2 text-contrast/50 text-right">{line.source}</td>
              </tr>
            );
          })}
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

function TabButton({ id, active, onClick, children }: { id: string; active: boolean; onClick: () => void; children: React.ReactNode }) {
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

function itemTypeIcon(type: string): string {
  return ITEM_TYPE_ICONS[type] || "wooden-crate.svg";
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
        <div className="text-primary">
          {item.name}
          {item.cost != null && item.cost > 0 && (
            <span className="text-accent ml-2">{item.cost}g</span>
          )}
        </div>
        {mods && (
          <div className="text-dim mt-0.5 truncate">{mods}</div>
        )}
        {item.description && !mods && (
          <div className="text-muted mt-0.5 truncate">{item.description}</div>
        )}
      </div>
      <div className="flex gap-1 flex-shrink-0 items-center">
        {actions}
      </div>
    </div>
  );
}

const ACTION_ICONS: Record<string, string> = {
  Equip: "sword-brandish.svg",
  Unequip: "cancel.svg",
  Discard: "cancel.svg",
};

function ActionBtn({ label, disabled, onClick }: { label: string; disabled: boolean; onClick: () => void }) {
  return (
    <button
      onClick={onClick}
      disabled={disabled}
      className="w-10 h-10 rounded-lg disabled:opacity-40
                 flex items-center justify-center transition-colors text-action hover:text-action-hover"
      style={{ backgroundColor: "rgba(13, 13, 13, 0.8)" }}
      title={label}
    >
      <MaskedIcon icon={ACTION_ICONS[label] || "sun.svg"} className="w-5 h-5" color="currentColor" />
    </button>
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
              <ActionBtn label="Equip" disabled={loading}
                onClick={() => doAction({ action: "equip", itemId: item.defId })} />
            )}
            <ActionBtn label="Discard" disabled={loading}
              onClick={() => doAction({ action: "discard", itemId: item.defId })} />
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
      {items.map((item, i) => (
        <ItemCard key={i} item={item} actions={
          <ActionBtn label="Discard" disabled={loading}
            onClick={() => doAction({ action: "discard", itemId: item.defId })} />
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
            <ActionBtn label="Unequip" disabled={loading}
              onClick={() => doAction({ action: "unequip", slot })} />
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
