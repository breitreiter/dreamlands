import { useGame } from "../GameContext";
import type { DungeonHubInfo } from "../api/types";
import { Button } from "@/components/ui/button";

export default function DungeonHub({ hub }: { hub: DungeonHubInfo }) {
  const { doAction, loading } = useGame();

  return (
    <div className="h-full flex items-center justify-center bg-page text-primary">
      <div className="w-full max-w-[520px] px-6">
        <h1 className="font-header text-[32px] text-accent mb-6">{hub.name}</h1>

        {hub.encounters.length > 0 ? (
          <div className="space-y-4">
            {hub.encounters.map((enc) => (
              <button
                key={enc.id}
                onClick={() => doAction({ action: "start_encounter", encounterId: enc.id })}
                disabled={loading}
                className="w-full text-left flex items-start gap-3
                           disabled:text-muted transition-colors group cursor-pointer"
              >
                <img
                  src="/world/assets/icons/sun.svg"
                  alt=""
                  className="w-4 h-4 mt-1 shrink-0 opacity-70 group-hover:opacity-100
                             transition-opacity"
                />
                <span className="font-bold text-action group-hover:text-action-hover transition-colors">
                  {enc.title}
                </span>
              </button>
            ))}
          </div>
        ) : (
          <p className="text-dim">Nothing more to find here.</p>
        )}

        <div className="mt-8">
          <Button
            variant="secondary"
            onClick={() => doAction({ action: "leave_dungeon" })}
            disabled={loading}
          >
            Leave
          </Button>
        </div>
      </div>
    </div>
  );
}
