import type { InventoryInfo } from "../api/types";
import { useGame } from "../GameContext";

export default function Inventory({
  inventory,
  onClose,
}: {
  inventory: InventoryInfo;
  onClose: () => void;
}) {
  const { doAction, loading } = useGame();

  return (
    <div className="absolute inset-0 z-[1000] bg-black/60 flex items-center justify-center">
      <div className="bg-panel border border-edge w-[500px] max-h-[80vh] overflow-y-auto">
        <div className="flex items-center justify-between p-3 border-b border-edge">
          <h3 className="text-accent font-medium">Inventory</h3>
          <button
            onClick={onClose}
            className="text-dim hover:text-primary px-2"
          >
            X
          </button>
        </div>

        {/* Equipment */}
        <div className="p-3 border-b border-edge">
          <div className="text-xs text-dim mb-2">Equipment</div>
          <div className="space-y-1 text-sm">
            <EquipSlot label="Weapon" item={inventory.equipment.weapon}
              onUnequip={() => doAction({ action: "unequip", slot: "weapon" })} disabled={loading} />
            <EquipSlot label="Armor" item={inventory.equipment.armor}
              onUnequip={() => doAction({ action: "unequip", slot: "armor" })} disabled={loading} />
            <EquipSlot label="Boots" item={inventory.equipment.boots}
              onUnequip={() => doAction({ action: "unequip", slot: "boots" })} disabled={loading} />
          </div>
        </div>

        {/* Pack */}
        <div className="p-3 border-b border-edge">
          <div className="text-xs text-dim mb-2">
            Pack ({inventory.pack.length}/{inventory.packCapacity})
          </div>
          {inventory.pack.length === 0 ? (
            <div className="text-muted text-sm">Empty</div>
          ) : (
            <div className="space-y-1">
              {inventory.pack.map((item, i) => (
                <div key={i} className="flex items-center justify-between text-sm">
                  <span className="text-primary/80">
                    {item.name}
                    {item.description && (
                      <span className="text-muted text-xs ml-2">
                        {item.description}
                      </span>
                    )}
                  </span>
                  <span className="flex gap-1 ml-2 shrink-0">
                    <ActionBtn label="Equip" disabled={loading}
                      onClick={() => doAction({ action: "equip", itemId: item.defId })} />
                    <ActionBtn label="Discard" disabled={loading}
                      onClick={() => doAction({ action: "discard", itemId: item.defId })} />
                  </span>
                </div>
              ))}
            </div>
          )}
        </div>

        {/* Haversack */}
        <div className="p-3">
          <div className="text-xs text-dim mb-2">
            Haversack ({inventory.haversack.length}/
            {inventory.haversackCapacity})
          </div>
          {inventory.haversack.length === 0 ? (
            <div className="text-muted text-sm">Empty</div>
          ) : (
            <div className="space-y-1">
              {inventory.haversack.map((item, i) => (
                <div key={i} className="flex items-center justify-between text-sm">
                  <span className="text-primary/80">{item.name}</span>
                  <ActionBtn label="Discard" disabled={loading}
                    onClick={() => doAction({ action: "discard", itemId: item.defId })} />
                </div>
              ))}
            </div>
          )}
        </div>
      </div>
    </div>
  );
}

function EquipSlot({
  label,
  item,
  onUnequip,
  disabled,
}: {
  label: string;
  item: { defId: string; name: string } | null;
  onUnequip: () => void;
  disabled: boolean;
}) {
  return (
    <div className="flex justify-between items-center">
      <span>
        <span className="text-dim">{label}:</span>{" "}
        <span className={item ? "text-primary" : "text-muted"}>
          {item?.name || "None"}
        </span>
      </span>
      {item && (
        <ActionBtn label="Unequip" disabled={disabled} onClick={onUnequip} />
      )}
    </div>
  );
}

function ActionBtn({
  label,
  disabled,
  onClick,
}: {
  label: string;
  disabled: boolean;
  onClick: () => void;
}) {
  return (
    <button
      onClick={onClick}
      disabled={disabled}
      className="text-xs text-dim hover:text-primary disabled:opacity-40 disabled:hover:text-dim px-1"
    >
      {label}
    </button>
  );
}
