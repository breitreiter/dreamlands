import { useEffect, useState } from "react";
import { useGame } from "../GameContext";
import { combatList } from "../api/client";
import type { CombatEncounterSummary } from "../api/types";
import { Button } from "@/components/ui/button";

/**
 * Debug overlay for picking any loaded .fight encounter to fight. Mirrors the
 * Notices storylet picker style. Lives in components/ rather than screens/
 * because it's a developer affordance, not part of the play loop — gated by
 * `import.meta.env.DEV` at the call site.
 *
 * Selecting an encounter calls combatBegin, which transitions the global
 * game state into combat mode; App.tsx then routes to the Combat screen.
 */
export default function CombatPicker({ onClose }: { onClose: () => void }) {
  const { gameId, doCombatBegin, loading } = useGame();
  const [encounters, setEncounters] = useState<CombatEncounterSummary[]>([]);
  const [fetching, setFetching] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!gameId) return;
    combatList(gameId)
      .then((r) => setEncounters(r.encounters))
      .catch((e) => setError(e instanceof Error ? e.message : "Failed to load combat list"))
      .finally(() => setFetching(false));
  }, [gameId]);

  return (
    <div
      className="fixed inset-0 z-[1500] bg-black/80 flex items-center justify-center p-6"
      onClick={onClose}
    >
      <div
        className="bg-page border border-white/10 rounded-lg w-full max-w-[560px] max-h-[80vh] flex flex-col"
        onClick={(e) => e.stopPropagation()}
      >
        <div className="px-6 pt-6 pb-3 border-b border-white/5">
          <h1 className="font-header text-[32px] text-accent leading-none">Pick a fight</h1>
          <div className="text-dim mt-1">Debug: any loaded .fight encounter</div>
        </div>

        <div className="flex-1 overflow-y-auto px-6 py-4">
          {fetching ? (
            <p className="text-dim">Loading...</p>
          ) : error ? (
            <p className="text-negative">{error}</p>
          ) : encounters.length === 0 ? (
            <p className="text-dim">
              No .fight encounters loaded. Drop files into <code className="text-action">worlds/&lt;name&gt;/combat/</code> or
              ensure the fallback <code className="text-action">tools/combat-prototype/Monsters/</code> exists.
            </p>
          ) : (
            <div className="space-y-3">
              {encounters.map((enc) => (
                <button
                  key={enc.id}
                  onClick={async () => {
                    await doCombatBegin(enc.id);
                    onClose();
                  }}
                  disabled={loading}
                  className="w-full text-left flex items-start gap-3 group cursor-pointer disabled:text-muted disabled:cursor-not-allowed"
                >
                  <img
                    src="/world/assets/icons/sword-brandish.svg"
                    alt=""
                    className="w-4 h-4 mt-1.5 shrink-0 opacity-70 group-hover:opacity-100 transition-opacity"
                  />
                  <div className="flex-1">
                    <div className="font-bold text-action group-hover:text-action-hover transition-colors">
                      {enc.title || enc.id}
                    </div>
                    <div className="text-dim text-[16px] flex gap-3 mt-0.5">
                      {enc.tier != null && <span>T{enc.tier}</span>}
                      {enc.category && <span className="truncate">{enc.category}</span>}
                      <span className="ml-auto text-muted">HP {enc.hp}</span>
                    </div>
                  </div>
                </button>
              ))}
            </div>
          )}
        </div>

        <div className="px-6 py-4 border-t border-white/5 flex justify-end">
          <Button variant="secondary" onClick={onClose}>Close</Button>
        </div>
      </div>
    </div>
  );
}
