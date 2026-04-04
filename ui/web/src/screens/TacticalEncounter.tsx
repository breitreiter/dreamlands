import { useState } from "react";
import type { TacticalInfo, TacticalOpeningInfo, TacticalApproachInfo, MechanicResultInfo, NodeInfo } from "../api/types";
import { useGame } from "../GameContext";
import { formatProse } from "../prose";
import MaskedIcon from "../components/MaskedIcon";
import parchment from "../assets/parchment.png";

const ICONS = {
  momentum: "flamer.svg",
  progress: "dodge.svg",
  clock: "stopwatch.svg",
  draw: "card-draw.svg",
  spirits: "sensuousness.svg",
} as const;

const PRESS_COST = 2;
const FORCE_COST = 2;

function IconChip({ icon, value, color = "#aca377" }: { icon: string; value: string; color?: string }) {
  return (
    <span className="inline-flex items-center gap-0.5">
      <span className="text-dim">{value}</span>
      <MaskedIcon icon={icon} className="w-4 h-4" color={color} />
    </span>
  );
}

function NumberCircle({ n, color = "#aca377" }: { n: number; color?: string }) {
  return (
    <svg width="22" height="22" viewBox="0 0 22 22" className="shrink-0">
      <circle cx="11" cy="11" r="9.5" fill="none" stroke={color} strokeWidth="1.5" />
      <text x="11" y="11.5" textAnchor="middle" dominantBaseline="central"
        fill={color} fontSize="12" fontWeight="bold" fontFamily="sans-serif">{n}</text>
    </svg>
  );
}

function costIcon(kind: string): string | null {
  if (kind === "momentum") return ICONS.momentum;
  if (kind === "spirits") return ICONS.spirits;
  if (kind === "tick") return ICONS.clock;
  return null;
}

function effectIcon(kind: string): string | null {
  if (kind === "damage") return ICONS.progress;
  if (kind === "momentum") return ICONS.momentum;
  if (kind === "stop_timer") return ICONS.progress;
  return null;
}

function describeOpening(o: TacticalOpeningInfo): string {
  const costLabel = (kind: string, amount: number): string => {
    if (kind === "free") return "Free";
    if (kind === "tick") return "Advance threat timers";
    if (kind === "momentum") return `Pay ${amount} momentum`;
    if (kind === "spirits") return `Pay ${amount} spirits`;
    return `Pay ${amount} ${kind}`;
  };

  const effectLabel = (kind: string, amount: number): string => {
    if (kind === "damage") return `gain ${amount} progress`;
    if (kind === "momentum") return `gain ${amount} momentum`;
    if (kind === "stop_timer") return "clear the challenge";
    return `gain ${amount} ${kind}`;
  };

  const cost = costLabel(o.costKind, o.costAmount);
  const effect = effectLabel(o.effectKind, o.effectAmount);

  if (o.costKind === "free") return `${cost} — ${effect}`;
  return `${cost}, ${effect}`;
}

function CostEffect({ opening }: { opening: TacticalOpeningInfo }) {
  const cIcon = costIcon(opening.costKind);
  const eIcon = effectIcon(opening.effectKind);

  return (
    <span className="flex items-center gap-1.5 text-dim">
      {opening.costKind !== "free" && cIcon && (
        <>
          <IconChip icon={cIcon} value={opening.costKind === "tick" ? "🠻" : `-${opening.costAmount}`} />
          <span className="text-muted">➽</span>
        </>
      )}
      {opening.effectKind === "stop_timer" && eIcon ? (
        <IconChip icon={eIcon} value="×" />
      ) : eIcon ? (
        <IconChip icon={eIcon} value={`+${opening.effectAmount}`} />
      ) : null}
    </span>
  );
}

function MechanicLines({ results }: { results: MechanicResultInfo[] }) {
  return (
    <div className="space-y-2">
      {results.map((m, i) =>
        m.resistCheck ? (
          <div
            key={i}
            className={`p-3 border rounded-lg ${
              m.resistCheck.passed
                ? "border-positive bg-positive/15"
                : "border-negative bg-negative/15"
            }`}
          >
            <MaskedIcon
              icon="dice-twenty-faces-twenty.svg"
              className="w-5 h-5 inline-block align-text-bottom mr-1.5"
              color={m.resistCheck.rollMode === "disadvantage" ? "#C45656"
                : m.resistCheck.rollMode === "advantage" ? "#5B9F5B"
                : "currentColor"}
            />
            <span className="capitalize">{m.resistCheck.conditionName}</span>
            {" resist: "}
            <span className="font-medium">
              {m.resistCheck.rolled - m.resistCheck.modifier}
              {m.resistCheck.modifier !== 0 &&
                ` ${m.resistCheck.modifier >= 0 ? "+" : ""}${m.resistCheck.modifier}`}
            </span>
            {" vs "}
            <span className="font-medium">{m.resistCheck.target}</span>
            {" — "}
            <span className={m.resistCheck.passed ? "text-positive" : "text-negative"}>
              {m.resistCheck.passed ? "Resisted" : "Afflicted"}
            </span>
            {m.resistCheck.rollMode && (
              <>
                {" · "}
                <span className={m.resistCheck.rollMode === "disadvantage" ? "text-negative" : "text-positive"}>
                  {m.resistCheck.rollMode === "disadvantage" ? "Disadvantage" : "Advantage"}
                </span>
              </>
            )}
          </div>
        ) : (
          <div key={i} className="text-dim">
            {m.description}
          </div>
        )
      )}
    </div>
  );
}

function FinishedSummary({ tactical, onContinue, loading }: { tactical: TacticalInfo; onContinue: () => void; loading: boolean }) {
  const isVictory = tactical.finishReason === "resistancekill" || tactical.finishReason === "controlkill";
  const epilogue = isVictory ? tactical.successText : tactical.failureText;
  const mechanics = isVictory ? tactical.successMechanics : tactical.failureMechanics;
  const hasEffects = mechanics && mechanics.length > 0;

  return (
    <div className="space-y-6">
      {/* Victory/defeat banner */}
      <div
        className={`p-4 border rounded-lg ${
          isVictory ? "border-positive bg-positive/15" : "border-negative bg-negative/15"
        }`}
      >
        <p className="font-bold">
          {tactical.finishReason === "resistancekill"
            ? "Victory — Goal Reached"
            : tactical.finishReason === "controlkill"
              ? "Victory — Total Control"
              : tactical.finishReason === "clockexpired"
                ? "Defeated — Time Ran Out"
                : "Defeated — Spirits Depleted"}
        </p>
      </div>

      {/* Epilogue prose */}
      {epilogue && (
        <div className="text-primary/80 leading-loose whitespace-pre-wrap">
          {formatProse(epilogue)}
        </div>
      )}

      {/* Effects: conditions and mechanics */}
      {hasEffects && (
        <div className="space-y-2 border-t border-edge pt-3">
          {mechanics && mechanics.length > 0 && <MechanicLines results={mechanics} />}
        </div>
      )}

      <button
        onClick={onContinue}
        disabled={loading}
        className="w-full text-left p-4 bg-btn hover:bg-btn-hover border border-edge rounded-lg text-action font-bold cursor-pointer transition-colors"
      >
        Continue
      </button>
    </div>
  );
}

export default function TacticalEncounter({ tactical, node }: { tactical: TacticalInfo; node?: NodeInfo }) {
  const { doAction, loading } = useGame();
  const [vignetteError, setVignetteError] = useState(false);

  const chooseApproach = (a: TacticalApproachInfo) => {
    doAction({ action: "tactical_approach", approach: a.kind });
  };

  const takeOpening = (index: number) => {
    doAction({ action: "tactical_act", tacticalAction: "TakeOpening", openingIndex: index });
  };

  const pressAdvantage = () => {
    doAction({ action: "tactical_act", tacticalAction: "PressAdvantage" });
  };

  const forceOpening = () => {
    doAction({ action: "tactical_act", tacticalAction: "ForceOpening" });
  };

  const endTactical = () => {
    doAction({ action: "end_tactical" });
  };

  const canAfford = (o: TacticalOpeningInfo): boolean => {
    if (!tactical.turn) return false;
    if (o.costKind === "momentum") return tactical.turn.momentum >= o.costAmount;
    if (o.costKind === "spirits") return tactical.turn.spirits >= o.costAmount;
    return true;
  };

  const turn = tactical.turn;

  return (
    <div className="h-full flex bg-page text-primary">
      {/* Left panel — vignette */}
      {(() => {
        const vignetteSrc = node?.terrain && node.regionTier != null && node.regionTier > 0
          ? `/world/assets/vignettes/${node.terrain}/${node.terrain}_tier_${node.regionTier}_1.webp`
          : null;
        const hasVignette = !vignetteError && vignetteSrc != null;
        return (
          <div
            className="hidden md:block w-[45%] shrink-0"
            style={{ backgroundImage: `url(${parchment})`, backgroundSize: "cover", backgroundPosition: "center" }}
          >
            {hasVignette && (
              <img
                src={vignetteSrc}
                alt=""
                className="w-full h-full object-cover"
                onError={() => setVignetteError(true)}
              />
            )}
          </div>
        );
      })()}

      {/* Right panel */}
      <div className="flex-1 overflow-y-auto p-8 md:p-12 flex flex-col">
        <div className="max-w-2xl w-full space-y-6">
          <h2 className="font-header text-[32px] text-accent uppercase">{tactical.title}</h2>

          {/* Approach phase */}
          {tactical.phase === "approach" && tactical.approaches && (
            <>
              <div className="text-primary/80 leading-loose whitespace-pre-wrap">
                {formatProse(tactical.body)}
              </div>
              <div className="space-y-3 pt-2">
                <p className="text-dim font-bold">
                  {tactical.stat === "negotiation" ? "Words are your weapon." : "It's a fight."}
                </p>
                {tactical.approaches.map((a) => {
                  const isNeg = tactical.stat === "negotiation";
                  const label =
                    a.kind === "aggressive"
                      ? isNeg ? "Press them" : "Charge them"
                      : isNeg ? "Hear them out" : "Make ready";
                  const desc =
                    a.kind === "aggressive"
                      ? "Strike fast: +2 momentum/turn, draw 1 move"
                      : "Play it safe: +1 momentum/turn, draw 2 moves";
                  return (
                    <button
                      key={a.kind}
                      onClick={() => chooseApproach(a)}
                      disabled={loading}
                      className="w-full text-left p-4 bg-btn hover:bg-btn-hover border border-edge rounded-lg transition-colors group cursor-pointer"
                    >
                      <span className="font-bold text-action group-hover:text-action-hover">
                        {label}
                      </span>
                      <span className="block text-dim mt-1">{desc}</span>
                    </button>
                  );
                })}
              </div>
            </>
          )}

          {/* Turn phase */}
          {tactical.phase === "turn" && turn && (
            <>
              {/* Resources */}
              <div className="flex gap-6">
                <div className="flex-1 text-center">
                  <div className="text-dim">Clock</div>
                  <div className="font-bold text-primary text-[32px] leading-tight flex items-center justify-center gap-1.5">
                    <MaskedIcon icon={ICONS.clock} className="w-7 h-7" color="#d0bd62" />
                    {turn.clock}
                  </div>
                </div>
                <div className="flex-1 text-center">
                  <div className="text-dim">Momentum</div>
                  <div className="font-bold text-primary text-[32px] leading-tight flex items-center justify-center gap-1.5">
                    <MaskedIcon icon={ICONS.momentum} className="w-7 h-7" color="#d0bd62" />
                    {turn.momentum}
                  </div>
                </div>
                <div className="flex-1 text-center">
                  <div className="text-dim">Spirits</div>
                  <div className="font-bold text-primary text-[32px] leading-tight flex items-center justify-center gap-1.5">
                    <MaskedIcon icon={ICONS.spirits} className="w-7 h-7" color="#d0bd62" />
                    {turn.spirits}
                  </div>
                </div>
              </div>

              {/* Challenges */}
              {turn.challenges.length > 0 && (
                <div className="space-y-2">
                  <p className="text-dim font-bold">Challenges</p>
                  {turn.challenges.map((c, i) => {
                    const isCurrent = i === turn.currentChallengeIndex;
                    const color = c.cleared ? "#8b8b8b" : isCurrent ? "#d0bd62" : "#aca377";
                    const progress = c.maxResistance - c.resistance;

                    return (
                      <div
                        key={i}
                        className={`flex items-center gap-3 ${c.cleared ? "opacity-30 line-through" : ""}`}
                      >
                        <NumberCircle n={i + 1} color={color} />
                        <span className={`flex-1 ${isCurrent ? "text-accent font-bold" : ""}`}>
                          {c.name}
                        </span>
                        <span className="text-dim inline-flex items-center gap-1">
                          {progress} / {c.maxResistance} <MaskedIcon icon={ICONS.progress} className="w-4 h-4" color={color} />
                        </span>
                      </div>
                    );
                  })}
                </div>
              )}


              {/* Openings + Press/Force */}
              <div className="space-y-2">
                <p className="text-dim font-bold">
                  {turn.openings.length > 1 ? "Choose a move:" : "Your move:"}
                </p>
                {turn.openings.map((o, i) => {
                  const affordable = canAfford(o);
                  return (
                    <button
                      key={i}
                      onClick={affordable ? () => takeOpening(i) : undefined}
                      disabled={loading || !affordable}
                      title={describeOpening(o)}
                      className={`w-full text-left p-4 border rounded-lg transition-colors flex items-center justify-between ${
                        affordable
                          ? "bg-btn hover:bg-btn-hover border-edge cursor-pointer group"
                          : "bg-btn/50 border-edge/50 opacity-50 cursor-default"
                      }`}
                    >
                      <span
                        className={`font-bold ${
                          affordable
                            ? "text-action group-hover:text-action-hover"
                            : "text-muted"
                        }`}
                      >
                        {o.name}
                      </span>
                      <CostEffect opening={o} />
                    </button>
                  );
                })}
                {!turn.digUsed && (
                  <>
                    <button
                      onClick={pressAdvantage}
                      disabled={loading || turn.momentum < PRESS_COST}
                      title={`Pay ${PRESS_COST} momentum, draw 2 extra moves`}
                      className={`w-full text-left p-4 border rounded-lg transition-colors flex items-center justify-between ${
                        turn.momentum >= PRESS_COST
                          ? "bg-btn hover:bg-btn-hover border-edge cursor-pointer group"
                          : "bg-btn/50 border-edge/50 opacity-50 cursor-default"
                      }`}
                    >
                      <span className={`font-bold ${turn.momentum >= PRESS_COST ? "text-action group-hover:text-action-hover" : "text-muted"}`}>
                        Press the advantage
                      </span>
                      <span className="flex items-center gap-1.5 text-dim">
                        <IconChip icon={ICONS.momentum} value={`-${PRESS_COST}`} /> <span className="text-muted">➽</span> <IconChip icon={ICONS.draw} value="+2" />
                      </span>
                    </button>
                    <button
                      onClick={forceOpening}
                      disabled={loading || turn.spirits < FORCE_COST}
                      title={`Pay ${FORCE_COST} spirits, draw 2 extra moves`}
                      className={`w-full text-left p-4 border rounded-lg transition-colors flex items-center justify-between ${
                        turn.spirits >= FORCE_COST
                          ? "bg-btn hover:bg-btn-hover border-edge cursor-pointer group"
                          : "bg-btn/50 border-edge/50 opacity-50 cursor-default"
                      }`}
                    >
                      <span className={`font-bold ${turn.spirits >= FORCE_COST ? "text-action group-hover:text-action-hover" : "text-muted"}`}>
                        Force an opening
                      </span>
                      <span className="flex items-center gap-1.5 text-dim">
                        <IconChip icon={ICONS.spirits} value={`-${FORCE_COST}`} /> <span className="text-muted">➽</span> <IconChip icon={ICONS.draw} value="+2" />
                      </span>
                    </button>
                  </>
                )}
              </div>
            </>
          )}

          {/* Finished phase */}
          {tactical.phase === "finished" && (
            <FinishedSummary tactical={tactical} onContinue={endTactical} loading={loading} />
          )}
        </div>
      </div>
    </div>
  );
}
