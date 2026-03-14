import { useState, useEffect } from "react";
import { useGame } from "../GameContext";
import { getInnQuote } from "../api/client";
import type { GameResponse, InnQuoteResponse, InnRecoveryInfo } from "../api/types";
import { Button } from "@/components/ui/button";
import MaskedIcon from "@/components/MaskedIcon";
import parchment from "../assets/parchment.png";

export default function Inn({
  isChapterhouse,
  onBack,
}: {
  state: GameResponse;
  isChapterhouse: boolean;
  onBack: () => void;
}) {
  const { doAction, loading, gameId } = useGame();
  const [quote, setQuote] = useState<InnQuoteResponse | null>(null);
  const [quoteError, setQuoteError] = useState<string | null>(null);
  const [recovery, setRecovery] = useState<InnRecoveryInfo | null>(null);
  const [vignetteError, setVignetteError] = useState(false);

  useEffect(() => {
    if (!gameId) return;
    getInnQuote(gameId)
      .then(setQuote)
      .catch((e) => setQuoteError(e.message));
  }, [gameId]);

  const vignetteSrc = isChapterhouse
    ? "/world/assets/vignettes/chapterhouse_camp.png"
    : "/world/assets/vignettes/inn_camp.png";

  const title = isChapterhouse ? "The Chapterhouse" : "A Quiet Inn";

  async function handleOneNight() {
    await doAction({ action: "rest_at_inn" });
    // rest_at_inn returns camp_resolved mode — App router will show Camp.tsx
  }

  async function handleFullRecovery() {
    const action = isChapterhouse ? "chapterhouse_recover" : "inn_full_recovery";
    const result = await doAction({ action });
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
              {recovery.nightsStayed === 1
                ? "After a night's rest, you feel restored."
                : `After ${recovery.nightsStayed} nights, you feel fully restored.`}
            </div>

            <div className="space-y-1 border-t border-edge pt-3">
              {recovery.healthRecovered > 0 && (
                <div className="text-positive">
                  Health restored: +{recovery.healthRecovered}
                </div>
              )}
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
              {recovery.conditionsCleared.map((c) => (
                <div key={c} className="text-positive">
                  {c} cured
                </div>
              ))}
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

          {quoteError && (
            <div className="text-negative">{quoteError}</div>
          )}

          {!quote && !quoteError && (
            <div className="text-dim">Loading...</div>
          )}

          {quote && isChapterhouse && (
            <>
              <div className="text-primary/80 leading-loose">
                Food and lodging is free for guild members in good standing.
                An on-site physician will tend to any injuries or illnesses
                you've encountered on the road.
              </div>

              {quote.needsRecovery && (
                <div className="text-dim">
                  Given your current state, recovery will take{" "}
                  {quote.quote.nights === 1
                    ? "one night"
                    : `${quote.quote.nights} nights`}
                  .
                </div>
              )}

              <div className="flex gap-3">
                {quote.needsRecovery && (
                  <Button variant="secondary" onClick={handleFullRecovery} disabled={loading}>
                    <MaskedIcon icon="heart-plus.svg" className="w-5 h-5" color="currentColor" />
                    Recover in the Chapterhouse
                  </Button>
                )}
                <Button variant="secondary" onClick={onBack} disabled={loading}>
                  <MaskedIcon icon="cancel.svg" className="w-5 h-5" color="currentColor" />
                  Depart
                </Button>
              </div>
            </>
          )}

          {quote && !isChapterhouse && (
            <>
              <div className="text-primary/80 leading-loose">
                You may stay one night at no charge, though you'll need to
                buy your own food in the market.
              </div>

              <div className="flex flex-col gap-3">
                <Button variant="secondary" className="self-start" onClick={handleOneNight} disabled={loading}>
                  <MaskedIcon icon="wood-cabin.svg" className="w-5 h-5" color="currentColor" />
                  Spend One Night
                </Button>

                {quote.canFullRecover && quote.needsRecovery && (
                  <>
                    <div className="text-primary/80 leading-loose">
                      You may rest here until fully recovered. Given your
                      current state, that will take{" "}
                      {quote.quote.nights === 1
                        ? "one night"
                        : `${quote.quote.nights} nights`}
                      {quote.quote.goldCost > 0
                        ? ` and cost ${quote.quote.goldCost} gold`
                        : ""}
                      .
                    </div>
                    <Button variant="secondary" className="self-start" onClick={handleFullRecovery} disabled={loading || !quote.canAfford}>
                      <MaskedIcon icon="heart-plus.svg" className="w-5 h-5" color="currentColor" />
                      Fully Recover
                      {quote.quote.goldCost > 0 && ` (${quote.quote.goldCost} gold)`}
                    </Button>
                    {!quote.canAfford && (
                      <div className="text-negative">
                        You don't have enough gold.
                      </div>
                    )}
                  </>
                )}

                {!quote.canFullRecover &&
                  quote.disqualifyingConditions.length > 0 && (
                    <div className="space-y-1 border-t border-edge pt-3">
                      {quote.disqualifyingConditions.length === 1 ? (
                        <div className="text-negative">
                          You don't have the medical supplies to treat your{" "}
                          {quote.disqualifyingConditions[0]}.
                        </div>
                      ) : (
                        <div className="text-negative">
                          You don't have the medical supplies to treat your
                          conditions:{" "}
                          {quote.disqualifyingConditions.join(", ")}.
                        </div>
                      )}
                      <div className="text-dim">
                        You'll only get worse if you stay here. You need to
                        buy medical supplies in the market.
                      </div>
                    </div>
                  )}
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
