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

          {services && (
            <>
              <div className="text-primary/80 leading-loose">
                {isChapterhouse
                  ? "Choose your accommodation. Guild patrons keep the lights on, but the rates are the same as anywhere else."
                  : "Choose your accommodation."}
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
        {name} — {cost} gold, {spiritsLabel}
        {!canAfford && " (can't afford)"}
      </span>
    </Button>
  );
}
