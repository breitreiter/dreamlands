import { useState } from "react";
import type { GameResponse, ConditionInfo, SkillInfoDto, InventoryInfo, ItemInfo, MechanicsInfo, MechanicLine } from "../api/types";
import { useGame } from "../GameContext";
import StatBar, { HEALTH_GRADIENT, SPIRITS_GRADIENT } from "../components/StatBar";

const CONDITION_ICONS: Record<string, string> = {
  lost: "sextant.svg",
  hungry: "water-drop.svg",
  exhausted: "camping-tent.svg",
  injured: "nested-hearts.svg",
  poisoned: "caduceus.svg",
  cursed: "foamy-disc.svg",
};

const SKILL_TIER_COLORS: Record<number, string> = {
  0: "text-muted border-muted",
  2: "text-primary border-primary/60",
  4: "text-accent border-accent",
};

function skillColor(level: number): string {
  if (level >= 4) return SKILL_TIER_COLORS[4];
  if (level >= 2) return SKILL_TIER_COLORS[2];
  return SKILL_TIER_COLORS[0];
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
    <div className="h-full flex flex-col bg-page text-primary">
      {/* Three-column layout */}
      <div className="flex-1 flex overflow-hidden">
        {/* Left: Character Panel */}
        <div className="w-80 flex flex-col border-r border-edge overflow-y-auto flex-shrink-0">
          <CharacterPanel
            skills={status.skills}
            conditions={status.conditions}
            health={status.health}
            maxHealth={status.maxHealth}
            spirits={status.spirits}
            maxSpirits={status.maxSpirits}
          />
        </div>

        {/* Middle: Mechanics */}
        <div className="flex-1 flex flex-col border-r border-edge overflow-y-auto min-w-0">
          {state.mechanics ? (
            <MechanicsPanel mechanics={state.mechanics} />
          ) : (
            <div className="p-4 text-muted text-sm">No mechanics data</div>
          )}
        </div>

        {/* Right: Inventory */}
        <div className="flex-1 flex flex-col min-w-0">
          {inventory ? (
            <InventoryPanel inventory={inventory} />
          ) : (
            <div className="p-4 text-muted text-sm">No inventory data</div>
          )}
        </div>
      </div>

      {/* Bottom bar with close */}
      <div className="px-4 py-2 border-t border-edge bg-panel-alt flex justify-end">
        <button
          onClick={onClose}
          className="px-4 py-1.5 text-sm text-dim hover:text-primary transition-colors"
        >
          Close
        </button>
      </div>
    </div>
  );
}

function CharacterPanel({
  skills,
  conditions,
  health,
  maxHealth,
  spirits,
  maxSpirits,
}: {
  skills: SkillInfoDto[];
  conditions: ConditionInfo[];
  health: number;
  maxHealth: number;
  spirits: number;
  maxSpirits: number;
}) {
  return (
    <div className="flex flex-col h-full p-4">
      {/* Character title */}
      <h2 className="font-header text-accent text-lg tracking-wide uppercase mb-4">
        The Merchant
      </h2>

      {/* Vitals */}
      <div className="space-y-2 mb-6">
        <StatBar label="Health" value={health} max={maxHealth} gradient={HEALTH_GRADIENT} />
        <StatBar label="Spirits" value={spirits} max={maxSpirits} gradient={SPIRITS_GRADIENT} />
      </div>

      {/* Skills */}
      <div className="space-y-3 mb-6">
        {skills.map((skill) => (
          <div key={skill.id} className="flex items-start gap-3">
            <div
              className={`w-8 h-8 rounded-full border-2 flex items-center justify-center text-xs font-bold flex-shrink-0 mt-0.5 ${skillColor(skill.level)}`}
            >
              {skill.formatted}
            </div>
            <div className="min-w-0">
              <div className="text-primary text-sm font-medium">{skill.name}</div>
              {skill.flavor && (
                <div className="text-dim text-xs leading-snug mt-0.5">{skill.flavor}</div>
              )}
            </div>
          </div>
        ))}
      </div>

      {/* Conditions — pushed to bottom */}
      {conditions.length > 0 && (
        <div className="mt-auto pt-4 border-t border-edge space-y-2">
          {conditions.map((c) => (
            <div key={c.id} className="flex items-start gap-2">
              <img
                src={`/world/assets/icons/${CONDITION_ICONS[c.id] || "sun.svg"}`}
                alt=""
                className="w-5 h-5 flex-shrink-0 mt-0.5"
              />
              <div className="min-w-0">
                <div className="text-negative text-sm">
                  {c.name}
                  {c.stacks > 1 && <span className="text-muted ml-1">x{c.stacks}</span>}
                </div>
                {c.description && (
                  <div className="text-muted text-xs leading-snug mt-0.5">{c.description}</div>
                )}
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
      <h2 className="font-header text-accent text-sm tracking-widest uppercase mb-4">
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
    <div className="mb-5">
      <div className="text-dim text-xs uppercase tracking-wider mb-2">{title}</div>
      <table className="w-full text-sm">
        <tbody>
          {lines.map((line, i) => (
            <tr key={i} className="border-b border-edge/30 last:border-0">
              <td className="py-1 pr-2 text-primary">{line.label}</td>
              <td className="py-1 px-2 text-accent font-medium whitespace-nowrap">{line.value}</td>
              <td className="py-1 pl-2 text-muted text-xs text-right">{line.source}</td>
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
        <h2 className="font-header text-accent text-sm tracking-widest uppercase mb-3">
          Inventory
        </h2>
        <div className="flex gap-1">
          <TabButton active={tab === "pack"} onClick={() => setTab("pack")}>
            Pack
          </TabButton>
          <TabButton active={tab === "haversack"} onClick={() => setTab("haversack")}>
            Haversack
          </TabButton>
          <TabButton active={tab === "equipped"} onClick={() => setTab("equipped")}>
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

function TabButton({ active, onClick, children }: { active: boolean; onClick: () => void; children: React.ReactNode }) {
  return (
    <button
      onClick={onClick}
      className={`px-3 py-1 text-xs transition-colors ${
        active
          ? "bg-btn border border-accent text-primary"
          : "bg-transparent border border-transparent text-muted hover:text-primary"
      }`}
      style={{ borderRadius: "999px" }}
    >
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

function ItemCard({
  item,
  actions,
}: {
  item: ItemInfo;
  actions: React.ReactNode;
}) {
  const mods = itemModifierSummary(item);
  return (
    <div className="flex items-start gap-3 bg-btn p-3 border-l-2 border-accent">
      <div className="w-8 h-8 bg-btn-hover flex-shrink-0 flex items-center justify-center text-muted text-xs">
        ?
      </div>
      <div className="flex-1 min-w-0">
        <div className="text-sm text-primary">
          {item.name}
          {item.cost != null && item.cost > 0 && (
            <span className="text-accent ml-2 text-xs">{item.cost}g</span>
          )}
        </div>
        {mods && (
          <div className="text-xs text-dim mt-0.5 truncate">{mods}</div>
        )}
        {item.description && !mods && (
          <div className="text-xs text-muted mt-0.5 truncate">{item.description}</div>
        )}
      </div>
      <div className="flex gap-1 flex-shrink-0 items-center">
        {actions}
      </div>
    </div>
  );
}

function ActionBtn({ label, disabled, onClick }: { label: string; disabled: boolean; onClick: () => void }) {
  return (
    <button
      onClick={onClick}
      disabled={disabled}
      className="w-8 h-8 bg-btn-hover hover:bg-edge disabled:opacity-40
                 flex items-center justify-center text-dim hover:text-primary
                 transition-colors text-xs"
      title={label}
    >
      {label === "Equip" && "E"}
      {label === "Unequip" && "U"}
      {label === "Discard" && "D"}
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
  return (
    <>
      <div className="text-xs text-dim mb-1">{items.length}/{capacity}</div>
      {items.length === 0 ? (
        <div className="text-muted text-sm">Empty</div>
      ) : (
        items.map((item, i) => (
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
        ))
      )}
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
  return (
    <>
      <div className="text-xs text-dim mb-1">{items.length}/{capacity}</div>
      {items.length === 0 ? (
        <div className="text-muted text-sm">Empty</div>
      ) : (
        items.map((item, i) => (
          <ItemCard key={i} item={item} actions={
            <ActionBtn label="Discard" disabled={loading}
              onClick={() => doAction({ action: "discard", itemId: item.defId })} />
          } />
        ))
      )}
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
          <div key={slot} className="flex items-center justify-center bg-btn/50 p-4 border border-dashed border-edge text-muted text-sm">
            Empty {slot} slot
          </div>
        );
      })}
    </>
  );
}
