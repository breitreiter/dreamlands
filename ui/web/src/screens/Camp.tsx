import { useState } from "react";
import { useGame } from "../GameContext";
import StatusBar from "./StatusBar";
import type { GameResponse } from "../api/types";

export default function Camp({ state }: { state: GameResponse }) {
  const { doAction, refreshState, loading } = useGame();
  const camp = state.camp;
  const resolved = state.mode === "camp_resolved";

  // Pull food and medicine from haversack by defId prefix
  const haversack = state.inventory?.haversack ?? [];
  const foodItems = haversack.filter((i) => i.defId.startsWith("food_"));
  const medicineItems = haversack.filter(
    (i) => i.defId === "bandages" || i.defId === "antivenom" || i.defId === "tonic"
  );

  // Auto-select balanced meal (1 protein + 1 grain + 1 sweets) or nothing
  const [selectedFood] = useState<Set<number>>(() => {
    const picked = new Set<number>();
    const seen = new Set<string>();
    for (let i = 0; i < foodItems.length; i++) {
      const id = foodItems[i].defId;
      if (!seen.has(id)) {
        seen.add(id);
        picked.add(i);
      }
    }
    if (seen.size < 3) return new Set<number>();
    return picked;
  });

  // Auto-select all medicine
  const selectedMedicine = new Set(medicineItems.map((_, i) => i));

  function resolve() {
    const food = foodItems
      .filter((_, i) => selectedFood.has(i))
      .map((i) => i.defId);
    const medicine = medicineItems
      .filter((_, i) => selectedMedicine.has(i))
      .map((i) => i.defId);
    doAction({ action: "camp_resolve", campChoices: { food, medicine } });
  }

  function continueJourney() {
    refreshState();
  }

  const mealSummary = selectedFood.size > 0
    ? foodItems.filter((_, i) => selectedFood.has(i)).map((i) => i.name).join(", ")
    : null;

  return (
    <div className="h-full flex flex-col bg-page text-primary">
      <StatusBar status={state.status} />

      <div className="flex-1 flex items-start justify-center overflow-y-auto p-6">
        <div className="max-w-2xl w-full space-y-6">
          <h2 className="text-2xl font-header text-accent">
            {resolved ? "Dawn Breaks" : "Make Camp"}
          </h2>

          <p className="text-primary/80 leading-relaxed">
            {resolved
              ? "You pack up your camp as the new day begins."
              : "Night falls. You set up camp and prepare to rest."}
          </p>

          {/* Threats */}
          {camp && camp.threats.length > 0 && !resolved && (
            <div className="space-y-2">
              <div className="text-xs text-dim uppercase tracking-wide">
                Threats
              </div>
              {camp.threats.map((t, i) => (
                <div
                  key={i}
                  className="p-2 border border-negative/40 bg-negative/10 text-sm"
                >
                  <span className="text-negative font-medium">{t.name}</span>
                  <span className="text-dim"> — {t.warning}</span>
                </div>
              ))}
            </div>
          )}

          {/* Meal summary */}
          {!resolved && (
            <div className="text-sm">
              {mealSummary ? (
                <span className="text-primary/80">Supper: {mealSummary}</span>
              ) : foodItems.length > 0 ? (
                <span className="text-muted">
                  No balanced meal available (need protein, grain, and sweets).
                  You will go hungry tonight.
                </span>
              ) : (
                <span className="text-muted">
                  No food in your haversack. You will go hungry tonight.
                </span>
              )}
            </div>
          )}

          {/* Medicine summary */}
          {!resolved && medicineItems.length > 0 && (
            <div className="text-sm text-primary/80">
              Medicine: {medicineItems.map((i) => i.name).join(", ")}
            </div>
          )}

          {/* Resolution events */}
          {resolved && camp && camp.events.length > 0 && (
            <div className="space-y-1 border-t border-edge pt-3">
              {camp.events.map((e, i) => (
                <div key={i} className="text-sm text-primary/80">
                  {e.description}
                </div>
              ))}
            </div>
          )}

          {/* Action button */}
          {!resolved ? (
            <button
              onClick={resolve}
              disabled={loading}
              className="px-6 py-2 bg-action hover:bg-action-hover disabled:bg-btn
                         text-contrast transition-colors"
            >
              Rest for the Night
            </button>
          ) : (
            <button
              onClick={continueJourney}
              disabled={loading}
              className="px-6 py-2 bg-action hover:bg-action-hover disabled:bg-btn
                         text-contrast transition-colors"
            >
              Continue
            </button>
          )}
        </div>
      </div>
    </div>
  );
}
