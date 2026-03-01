import { useState, useEffect } from "react";
import { useGame } from "../GameContext";
import { getInnQuote } from "../api/client";
import type { GameResponse, InnQuoteResponse, InnRecoveryInfo } from "../api/types";
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

  // Recovery summary phase
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
                  <button
                    onClick={handleFullRecovery}
                    disabled={loading}
                    className="px-4 py-2 bg-btn hover:bg-btn-hover rounded-lg text-action
                               hover:text-action-hover disabled:opacity-50 transition-colors"
                  >
                    Recover in the Chapterhouse
                  </button>
                )}
                <button
                  onClick={onBack}
                  disabled={loading}
                  className="px-4 py-2 bg-btn hover:bg-btn-hover rounded-lg text-action
                             hover:text-action-hover disabled:opacity-50 transition-colors"
                >
                  Cancel
                </button>
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
                <button
                  onClick={handleOneNight}
                  disabled={loading}
                  className="self-start px-4 py-2 bg-btn hover:bg-btn-hover rounded-lg text-action
                             hover:text-action-hover disabled:opacity-50 transition-colors"
                >
                  Spend One Night
                </button>

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
                    <button
                      onClick={handleFullRecovery}
                      disabled={loading || !quote.canAfford}
                      className="self-start px-4 py-2 bg-btn hover:bg-btn-hover rounded-lg text-action
                                 hover:text-action-hover disabled:opacity-50 transition-colors"
                    >
                      Fully Recover
                      {quote.quote.goldCost > 0 && ` (${quote.quote.goldCost} gold)`}
                    </button>
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
                      {quote.disqualifyingConditions.map((c) => (
                        <div key={c} className="text-negative">
                          You don't have enough medicine to treat your {c}.
                        </div>
                      ))}
                      <div className="text-dim">
                        You'll only get worse if you stay here. You need to
                        find treatment instead.
                      </div>
                    </div>
                  )}
              </div>

              <button
                onClick={onBack}
                disabled={loading}
                className="text-action hover:text-action-hover transition-colors"
              >
                Cancel
              </button>
            </>
          )}
        </div>
      </div>
    </div>
  );
}
