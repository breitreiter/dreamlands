import type { StatusInfo } from "../api/types";
import StatBar, { HEALTH_GRADIENT, SPIRITS_GRADIENT } from "../components/StatBar";

export default function StatusBar({ status }: { status: StatusInfo }) {
  return (
    <div className="flex flex-wrap items-center gap-x-6 gap-y-1 px-3 py-2 bg-panel-alt border-b border-edge text-xs">
      <div className="flex gap-4 w-64">
        <div className="flex-1">
          <StatBar label="Health" value={status.health} max={status.maxHealth} gradient={HEALTH_GRADIENT} />
        </div>
        <div className="flex-1">
          <StatBar label="Spirits" value={status.spirits} max={status.maxSpirits} gradient={SPIRITS_GRADIENT} />
        </div>
      </div>
      <div className="flex items-center gap-1">
        <span className="text-accent">{status.gold}g</span>
      </div>
      <div className="text-dim">
        Day {status.day}, {status.time}
      </div>
      {status.conditions.length > 0 && (
        <div className="text-negative">
          {status.conditions
            .map((c) => c.stacks > 1 ? `${c.name} x${c.stacks}` : c.name)
            .join(", ")}
        </div>
      )}
    </div>
  );
}
