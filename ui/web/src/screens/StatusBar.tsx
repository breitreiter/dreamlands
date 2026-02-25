import type { StatusInfo } from "../api/types";

function Bar({
  label,
  value,
  max,
  color,
}: {
  label: string;
  value: number;
  max: number;
  color: string;
}) {
  const pct = max > 0 ? Math.round((value / max) * 100) : 0;
  return (
    <div className="flex items-center gap-2">
      <span className="text-dim text-xs w-14">{label}</span>
      <div className="w-24 h-2 bg-panel rounded-full overflow-hidden">
        <div className={`h-full ${color} transition-all`} style={{ width: `${pct}%` }} />
      </div>
      <span className="text-xs text-primary/80">
        {value}/{max}
      </span>
    </div>
  );
}

export default function StatusBar({ status }: { status: StatusInfo }) {
  return (
    <div className="flex flex-wrap items-center gap-x-6 gap-y-1 px-3 py-2 bg-panel-alt border-b border-edge text-xs">
      <Bar label="Health" value={status.health} max={status.maxHealth} color="bg-negative" />
      <Bar label="Spirits" value={status.spirits} max={status.maxSpirits} color="bg-blue-400" />
      <div className="flex items-center gap-1">
        <span className="text-accent">{status.gold}g</span>
      </div>
      <div className="text-dim">
        Day {status.day}, {status.time}
      </div>
      {Object.keys(status.conditions).length > 0 && (
        <div className="text-negative">
          {Object.entries(status.conditions)
            .map(([name, stacks]) => stacks > 1 ? `${name} x${stacks}` : name)
            .join(", ")}
        </div>
      )}
    </div>
  );
}
