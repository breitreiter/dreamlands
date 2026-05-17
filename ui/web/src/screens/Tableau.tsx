import { useGame } from "../GameContext";
import type { GameResponse, TableauSlotInfo } from "../api/types";
import parchment from "../assets/parchment.webp";

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
      className="flex items-center justify-between gap-4 w-full text-left px-4 py-3 border border-edge rounded bg-parchment/30 hover:bg-parchment/60 transition-colors group cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed"
    >
      <div className="flex flex-col gap-0.5">
        <span className="font-bold text-action group-hover:text-action-hover transition-colors">
          {slot.label}
        </span>
        <span className="text-primary/70">{slot.pickEffect}</span>
      </div>
      <span className="text-primary/50 text-sm shrink-0">
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
    <div className="absolute inset-0 z-[1200] flex bg-page text-primary">
      {/* Left panel — parchment background */}
      <div
        className="hidden md:block w-[45%] shrink-0"
        style={{
          backgroundImage: `url(${parchment})`,
          backgroundSize: "cover",
          backgroundPosition: "center",
        }}
      />

      {/* Right panel — tableau picker */}
      <div className="flex-1 overflow-y-auto p-8 md:p-12 flex flex-col">
        <div className="max-w-lg space-y-6">
          {/* Header */}
          <div className="space-y-1">
            <h2 className="font-hand text-[20px] text-action">Arc Complete</h2>
            <h1 className="font-header text-[32px]">Level Up</h1>
            {tableau.pendingLevels > 1 && (
              <p className="text-primary/60">
                {tableau.pendingLevels} picks remaining
              </p>
            )}
          </div>

          <p className="text-primary/80 leading-loose">
            Choose one permanent upgrade. Each option is capped at two picks
            total across all arcs.
          </p>

          {/* Slot buttons */}
          <div className="space-y-2">
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
    </div>
  );
}
