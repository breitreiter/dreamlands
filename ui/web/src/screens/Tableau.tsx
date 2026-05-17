import { useGame } from "../GameContext";
import type { GameResponse, TableauSlotInfo } from "../api/types";

function SlotButton({
  slot,
  onPick,
  disabled,
}: {
  slot: TableauSlotInfo;
  onPick: (id: string) => void;
  disabled: boolean;
}) {
  return (
    <button
      onClick={() => onPick(slot.id)}
      disabled={disabled}
      className="flex items-center justify-between gap-4 w-full text-left transition-colors group cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed"
    >
      <div className="flex flex-col gap-0.5">
        <span className="font-bold text-action group-hover:text-action-hover transition-colors">
          {slot.label}
        </span>
        <span className="text-primary/70">{slot.pickEffect}</span>
      </div>
      <span className="text-primary/50 shrink-0">
        {slot.currentCount}/{slot.cap}
      </span>
    </button>
  );
}

export default function Tableau({ state }: { state: GameResponse }) {
  const { doAction, loading } = useGame();
  const tableau = state.tableauPrompt;

  if (!tableau) return null;

  const handlePick = (slotId: string) => {
    doAction({ action: "pick_reward", rewardSlotId: slotId } as Parameters<typeof doAction>[0]);
  };

  return (
    <div className="absolute inset-0 z-[1200] flex items-center justify-center bg-black/60 p-6">
      <div className="bg-page border border-edge rounded-lg shadow-xl max-w-md w-full p-8 space-y-5">
        <div className="space-y-1">
          <h2 className="font-header text-accent text-[32px] leading-tight">
            You have gained a level
          </h2>
          {tableau.pendingLevels > 1 && (
            <p className="text-primary/60">
              {tableau.pendingLevels} picks remaining
            </p>
          )}
        </div>

        <div className="space-y-3">
          {tableau.slots.map((slot) => (
            <SlotButton
              key={slot.id}
              slot={slot}
              onPick={handlePick}
              disabled={loading}
            />
          ))}
        </div>

        {tableau.slots.length === 0 && (
          <p className="text-primary/50 italic">
            No upgrade slots available — all rewards have been taken.
          </p>
        )}
      </div>
    </div>
  );
}
