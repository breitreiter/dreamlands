import { useGame } from "../GameContext";
import type { TableauPromptInfo, TableauSlotInfo } from "../api/types";

type TierState = "pickable" | "owned" | "locked";

function tierState(slot: TableauSlotInfo, tier: 1 | 2): TierState {
  if (tier === 1) {
    if (slot.currentCount >= 1) return "owned";
    return slot.isPickable ? "pickable" : "locked";
  } else {
    if (slot.currentCount >= 2) return "owned";
    if (slot.currentCount >= 1 && slot.isPickable) return "pickable";
    return "locked";
  }
}

function TierCard({
  label,
  description,
  state,
  onClick,
}: {
  label: string;
  description: string;
  state: TierState;
  onClick?: () => void;
}) {
  const isClickable = state === "pickable" && !!onClick;

  const borderClass =
    state === "pickable"
      ? "border-action-dim"
      : state === "owned"
      ? "border-accent opacity-50"
      : "border-dim border-dashed opacity-50";

  const labelClass = state === "pickable" ? "text-action" : "text-accent";

  return (
    <button
      onClick={isClickable ? onClick : undefined}
      disabled={!isClickable}
      className={`flex flex-col gap-1.5 p-2 w-[240px] min-h-[94px] rounded border-2 text-left transition-colors ${borderClass} ${
        isClickable ? "cursor-pointer hover:border-action" : "cursor-default"
      }`}
    >
      <span className={`text-base leading-6 font-body ${labelClass}`}>{label}</span>
      <span className="text-primary text-base leading-6 font-body">{description}</span>
    </button>
  );
}

function ConnectorLine({ active, dashed }: { active: boolean; dashed?: boolean }) {
  return (
    <div
      className={`w-[30px] h-0 border-t-2 shrink-0 ${
        active
          ? "border-accent"
          : dashed
          ? "border-dim border-dashed opacity-50"
          : "border-dim opacity-50"
      }`}
    />
  );
}

function SlotRow({
  slot,
  onPick,
  disabled,
}: {
  slot: TableauSlotInfo;
  onPick: (id: string) => void;
  disabled: boolean;
}) {
  const t1 = tierState(slot, 1);
  const t2 = tierState(slot, 2);

  const tier1Label = t1 === "owned" ? "✓ Trained" : `Train ${slot.label}`;
  const tier2Label = t2 === "owned" ? "✓ Mastered" : `Master ${slot.label}`;

  const leftLineActive = slot.isPickable;
  const rightLineDashed = t2 === "locked";
  const rightLineActive = t2 === "pickable" || t2 === "owned";

  return (
    <div className="flex items-center">
      <div className="w-[60px] h-[60px] rounded-full bg-panel-alt border-2 border-accent shrink-0 flex items-center justify-center">
        <span className="text-accent font-body text-base">{slot.label.charAt(0)}</span>
      </div>
      <ConnectorLine active={leftLineActive} />
      <TierCard
        label={tier1Label}
        description={slot.tier1Description}
        state={t1}
        onClick={t1 === "pickable" && !disabled ? () => onPick(slot.id) : undefined}
      />
      <ConnectorLine active={rightLineActive} dashed={rightLineDashed} />
      <TierCard
        label={tier2Label}
        description={slot.tier2Description}
        state={t2}
        onClick={t2 === "pickable" && !disabled ? () => onPick(slot.id) : undefined}
      />
    </div>
  );
}

export default function Tableau({ tableau }: { tableau: TableauPromptInfo }) {
  const { doAction, loading } = useGame();

  const handlePick = (slotId: string) => {
    doAction({ action: "pick_reward", rewardSlotId: slotId } as Parameters<typeof doAction>[0]);
  };

  return (
    <div className="space-y-5 pt-2">
      <div className="space-y-1">
        <h2 className="font-header text-accent text-[32px] leading-tight">You have leveled up.</h2>
        {tableau.pendingLevels > 1 && (
          <p className="text-dim text-base">{tableau.pendingLevels} picks remaining — choose one</p>
        )}
      </div>
      <div className="flex flex-col gap-3 overflow-x-auto">
        {tableau.slots.map((slot) => (
          <SlotRow key={slot.id} slot={slot} onPick={handlePick} disabled={loading} />
        ))}
      </div>
    </div>
  );
}
