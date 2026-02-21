import type { InventoryInfo } from "../api/types";

export default function Inventory({
  inventory,
  onClose,
}: {
  inventory: InventoryInfo;
  onClose: () => void;
}) {
  return (
    <div className="absolute inset-0 z-[1000] bg-black/60 flex items-center justify-center">
      <div className="bg-stone-800 border border-stone-600 w-[500px] max-h-[80vh] overflow-y-auto">
        <div className="flex items-center justify-between p-3 border-b border-stone-700">
          <h3 className="text-amber-200 font-medium">Inventory</h3>
          <button
            onClick={onClose}
            className="text-stone-400 hover:text-stone-100 px-2"
          >
            X
          </button>
        </div>

        {/* Equipment */}
        <div className="p-3 border-b border-stone-700">
          <div className="text-xs text-stone-400 mb-2">Equipment</div>
          <div className="space-y-1 text-sm">
            <EquipSlot label="Weapon" item={inventory.equipment.weapon} />
            <EquipSlot label="Armor" item={inventory.equipment.armor} />
            <EquipSlot label="Boots" item={inventory.equipment.boots} />
          </div>
        </div>

        {/* Pack */}
        <div className="p-3 border-b border-stone-700">
          <div className="text-xs text-stone-400 mb-2">
            Pack ({inventory.pack.length}/{inventory.packCapacity})
          </div>
          {inventory.pack.length === 0 ? (
            <div className="text-stone-500 text-sm">Empty</div>
          ) : (
            <div className="space-y-1">
              {inventory.pack.map((item, i) => (
                <div key={i} className="text-sm text-stone-300">
                  {item.name}
                  {item.description && (
                    <span className="text-stone-500 text-xs ml-2">
                      {item.description}
                    </span>
                  )}
                </div>
              ))}
            </div>
          )}
        </div>

        {/* Haversack */}
        <div className="p-3">
          <div className="text-xs text-stone-400 mb-2">
            Haversack ({inventory.haversack.length}/
            {inventory.haversackCapacity})
          </div>
          {inventory.haversack.length === 0 ? (
            <div className="text-stone-500 text-sm">Empty</div>
          ) : (
            <div className="space-y-1">
              {inventory.haversack.map((item, i) => (
                <div key={i} className="text-sm text-stone-300">
                  {item.name}
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
}: {
  label: string;
  item: { defId: string; name: string } | null;
}) {
  return (
    <div className="flex justify-between">
      <span className="text-stone-400">{label}:</span>
      <span className={item ? "text-stone-200" : "text-stone-600"}>
        {item?.name || "None"}
      </span>
    </div>
  );
}
