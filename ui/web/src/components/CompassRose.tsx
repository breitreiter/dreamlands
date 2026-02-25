import type { ExitInfo } from "../api/types";

const DIR_LETTER: Record<string, string> = { north: "N", south: "S", east: "E", west: "W" };

export default function CompassRose({
  exits,
  onMove,
  onInventory,
  disabled,
}: {
  exits: ExitInfo[];
  onMove: (dir: string) => void;
  onInventory: () => void;
  disabled: boolean;
}) {
  const exitSet = new Set(exits.map((e) => e.direction));

  function arm(dir: string, gridArea: string) {
    const available = exitSet.has(dir);
    return (
      <button
        key={dir}
        style={{ gridArea }}
        onClick={() => onMove(dir)}
        disabled={!available || disabled}
        className={`flex items-center justify-center rounded-lg font-medium transition-colors
          ${available ? "bg-btn hover:bg-btn-hover text-primary" : "bg-btn/40 text-muted cursor-not-allowed"}`}
      >
        {DIR_LETTER[dir]}
      </button>
    );
  }

  return (
    <div
      className="grid gap-1 w-32 h-32 mx-auto"
      style={{
        gridTemplateColumns: "1fr 1fr 1fr",
        gridTemplateRows: "1fr 1fr 1fr",
        gridTemplateAreas: `". n ." "w c e" ". s ."`,
      }}
    >
      {arm("north", "n")}
      {arm("west", "w")}
      <button
        style={{ gridArea: "c" }}
        onClick={onInventory}
        disabled={disabled}
        className="flex items-center justify-center rounded-lg bg-btn hover:bg-btn-hover text-primary transition-colors p-2"
        title="Inventory (I)"
      >
        <img src="/world/assets/icons/backpack.svg" alt="Inventory" className="w-6 h-6" />
      </button>
      {arm("east", "e")}
      {arm("south", "s")}
    </div>
  );
}
