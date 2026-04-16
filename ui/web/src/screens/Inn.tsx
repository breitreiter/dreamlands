import { useState, useEffect } from "react";
import { useGame } from "../GameContext";
import { getInnServices } from "../api/client";
import type { GameResponse, InnServicesResponse, InnRecoveryInfo } from "../api/types";
import { Button } from "@/components/ui/button";
import MaskedIcon from "@/components/MaskedIcon";
import parchment from "../assets/parchment.webp";

export default function Inn({
  isChapterhouse,
  onBack,
}: {
  state: GameResponse;
  isChapterhouse: boolean;
  onBack: () => void;
}) {
  const { doAction, loading, gameId } = useGame();
  const [services, setServices] = useState<InnServicesResponse | null>(null);
  const [servicesError, setServicesError] = useState<string | null>(null);
  const [recovery, setRecovery] = useState<InnRecoveryInfo | null>(null);
  const [vignetteError, setVignetteError] = useState(false);

  useEffect(() => {
    if (!gameId) return;
    getInnServices(gameId)
      .then(setServices)
      .catch((e) => setServicesError(e.message));
  }, [gameId]);

  const vignetteSrc = isChapterhouse
    ? "/world/assets/vignettes/chapterhouse_camp.webp"
    : "/world/assets/vignettes/inn_camp.webp";

  const title = isChapterhouse ? "The Chapterhouse" : "A Quiet Inn";

  async function handleBook(serviceId: "bed" | "bath" | "full") {
    const result = await doAction({ action: "inn_book", innService: serviceId });
    if (result?.innRecovery) {
      setRecovery(result.innRecovery);
    }
  }

  // Recovery summary — close with Enter key
  useEffect(() => {
    if (!recovery) return;
    const handler = (e: KeyboardEvent) => {
      if (e.key === "Enter") onBack();
    };
    window.addEventListener("keydown", handler);
    return () => window.removeEventListener("keydown", handler);
  }, [recovery, onBack]);

  if (recovery) {
    return (
      <div className="h-full flex bg-page text-primary">
        <div
          className="hidden md:block w-[45%] shrink-0"
          style={{
            backgroundImage: `url(${parchment})`,
            backgroundSize: "cover",
            backgroundPosition: "center",
          }}
        >
          {!vignetteError && (
            <img
              src={vignetteSrc}
              alt=""
              className="w-full h-full object-cover"
              onError={() => setVignetteError(true)}
            />
          )}
        </div>

        <div className="flex-1 overflow-y-auto p-8 md:p-12 flex flex-col">
          <div className="max-w-2xl space-y-6">
            <h2 className="font-header text-accent text-[32px]">
              {title}
            </h2>

            <div className="text-primary/80 leading-loose">
              After a night's rest, you feel restored.
            </div>

            <div className="space-y-1 border-t border-edge pt-3">
              {recovery.spiritsRecovered > 0 && (
                <div className="text-positive">
                  Spirits restored: +{recovery.spiritsRecovered}
                </div>
              )}
              {recovery.goldSpent > 0 && (
                <div className="text-dim">
                  Gold spent: {recovery.goldSpent}
                </div>
              )}
              {recovery.medicinesConsumed.map((m, i) => (
                <div key={i} className="text-dim">
                  {m} consumed
                </div>
              ))}
            </div>

            <button
              onClick={onBack}
              disabled={loading}
              className="flex items-start gap-3 transition-colors group cursor-pointer"
            >
              <img
                src="/world/assets/icons/sun.svg"
                alt=""
                className="w-4 h-4 mt-1 shrink-0 opacity-70 group-hover:opacity-100 transition-opacity"
              />
              <span className="font-bold text-action group-hover:text-action-hover transition-colors">
                Continue
              </span>
            </button>
          </div>
        </div>
      </div>
    );
  }

  // Offer phase
  return (
    <div className="h-full flex bg-page text-primary">
      <div
        className="hidden md:block w-[45%] shrink-0"
        style={{
          backgroundImage: `url(${parchment})`,
          backgroundSize: "cover",
          backgroundPosition: "center",
        }}
      >
        {!vignetteError && (
          <img
            src={vignetteSrc}
            alt=""
            className="w-full h-full object-cover"
            onError={() => setVignetteError(true)}
          />
        )}
      </div>

      <div className="flex-1 overflow-y-auto p-8 md:p-12 flex flex-col">
        <div className="max-w-2xl space-y-6">
          <h2 className="font-header text-accent text-[32px]">
            {title}
          </h2>

          {servicesError && (
            <div className="text-negative">{servicesError}</div>
          )}

          {!services && !servicesError && (
            <div className="text-dim">Loading...</div>
          )}

          {services && isChapterhouse && (
            <>
              <div className="text-primary/80 leading-loose space-y-4">
                <p>
                  The hall unfolds before you — blue columns rising to a
                  vaulted ceiling, their surfaces traced with arabesque figures
                  that shift and resolve as you move past. The capitals are
                  gilded, warm against the stone. Lamps hang deep between the
                  columns and the light they cast pools amber on floors worn
                  smooth by generations of feet. The air smells of incense and
                  spice.
                </p>
                <p>
                  Food and lodging is free for guild members in good standing.
                  An on-site physician will tend to any injuries or illnesses
                  you've encountered on the road.
                </p>
              </div>

              <div className="flex flex-col gap-3 items-start">
                <Button variant="secondary" onClick={() => handleBook("full")} disabled={loading}>
                  <MaskedIcon icon="heart-plus.svg" className="w-5 h-5" color="currentColor" />
                  Recover in the Chapterhouse
                </Button>
                <Button variant="secondary" onClick={onBack} disabled={loading}>
                  <MaskedIcon icon="cancel.svg" className="w-5 h-5" color="currentColor" />
                  Depart
                </Button>
              </div>
            </>
          )}

          {services && !isChapterhouse && (
            <>
              <div className="text-primary/80 leading-loose space-y-4">
                <p>
                  The ceiling hangs low enough that you duck without needing
                  to. The room smells of woodsmoke and something savory
                  that's been on the hearth since morning. A few mismatched
                  tables crowd what was once a front room, the mantel still
                  lined with painted clay figures no one bothered to move.
                </p>
                <p>
                  The floor is uneven planking, creaking underfoot, and the
                  walls are rough plaster, gone amber with smoke. The
                  innkeeper looks up when you enter, always glad to see a
                  traveler.
                </p>
              </div>

              <div className="flex flex-col gap-3">
                {services.services.map((svc) => (
                  <ServiceRow
                    key={svc.id}
                    name={svc.name}
                    cost={svc.cost}
                    spiritsLabel={svc.restoresFull ? "full spirits" : `+${svc.spirits} spirits`}
                    canAfford={svc.canAfford}
                    disabled={loading}
                    onClick={() => handleBook(svc.id)}
                  />
                ))}
              </div>

              <Button variant="secondary" onClick={onBack} disabled={loading}>
                <MaskedIcon icon="cancel.svg" className="w-5 h-5" color="currentColor" />
                Depart
              </Button>
            </>
          )}
        </div>
      </div>
    </div>
  );
}

function ServiceRow({
  name,
  cost,
  spiritsLabel,
  canAfford,
  disabled,
  onClick,
}: {
  name: string;
  cost: number;
  spiritsLabel: string;
  canAfford: boolean;
  disabled: boolean;
  onClick: () => void;
}) {
  return (
    <Button
      variant="secondary"
      className="self-start"
      onClick={onClick}
      disabled={disabled || !canAfford}
    >
      <MaskedIcon icon="heart-plus.svg" className="w-5 h-5" color="currentColor" />
      <span>
        {name} — {cost === 0 ? "free" : `${cost} gold`}, {spiritsLabel}
        {!canAfford && " (can't afford)"}
      </span>
    </Button>
  );
}
