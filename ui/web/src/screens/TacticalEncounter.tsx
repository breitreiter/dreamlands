import type { TacticalInfo, TacticalOpeningInfo, TacticalApproachInfo } from "../api/types";
import { useGame } from "../GameContext";
import { formatProse } from "../prose";
import MaskedIcon from "../components/MaskedIcon";
import parchment from "../assets/parchment.png";

const ICONS = {
  momentum: "flamer.svg",
  progress: "dodge.svg",
  threat: "stopwatch.svg",
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

function costIcon(kind: string): string | null {
  if (kind === "momentum") return ICONS.momentum;
  if (kind === "spirits") return ICONS.spirits;
  if (kind === "tick") return ICONS.threat;
  return null;
}

function effectIcon(kind: string): string | null {
  if (kind === "damage") return ICONS.progress;
  if (kind === "momentum") return ICONS.momentum;
  if (kind === "stop_timer") return ICONS.threat;
  return null;
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

export default function TacticalEncounter({ tactical }: { tactical: TacticalInfo }) {
  const { doAction, loading } = useGame();

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
      <div
        className="hidden md:flex w-[45%] shrink-0 items-center justify-center"
        style={{ backgroundImage: `url(${parchment})`, backgroundSize: "cover", backgroundPosition: "center" }}
      >
        <div className="text-contrast/30 font-header text-[32px] uppercase tracking-wider">
          {tactical.intent ?? tactical.variant}
        </div>
      </div>

      {/* Right panel */}
      <div className="flex-1 overflow-y-auto p-8 md:p-12 flex flex-col">
        <div className="max-w-2xl w-full space-y-6">
          <h2 className="font-header text-[32px] text-accent uppercase">{tactical.title}</h2>

          {/* Approach phase (combat) */}
          {tactical.phase === "approach" && tactical.approaches && (
            <>
              <div className="text-primary/80 leading-loose whitespace-pre-wrap">
                {formatProse(tactical.body)}
              </div>
              <div className="space-y-3 pt-2">
                <p className="text-dim font-bold">It's a fight.</p>
                {tactical.approaches.map((a) => {
                  const label =
                    a.kind === "scout" ? "Let them come"
                    : a.kind === "direct" ? "Make ready"
                    : "Charge them";
                  const desc =
                    a.kind === "scout" ? "Watch for tells, learn their patterns"
                    : a.kind === "direct" ? "Find a position of strength"
                    : "Strike fast and hard";
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
                  <div className="text-dim">Progress</div>
                  <div className="font-bold text-primary text-[32px] leading-tight flex items-center justify-center gap-1.5">
                    <MaskedIcon icon={ICONS.progress} className="w-7 h-7" color="#d0bd62" />
                    {turn.resistanceMax - turn.resistance}
                    <span className="text-dim font-normal"> of {turn.resistanceMax}</span>
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

              {/* Threats */}
              {turn.timers.length > 0 && (
                <div className="space-y-2">
                  <p className="text-dim font-bold">Threats</p>
                  {turn.timers.map((t, i) => (
                    <div
                      key={i}
                      className={`flex items-center gap-3 ${t.stopped ? "opacity-30 line-through" : ""}`}
                    >
                      <MaskedIcon icon={ICONS.threat} className="w-4 h-4" color={t.stopped ? "#8b8b8b" : "#aca377"} />
                      <span className="flex-1">{t.name}</span>
                      <span className="text-dim">
                        {t.effect === "spirits" ? `Lose ${t.amount} Spirits` : `Lose ${t.amount} Progress`}
                      </span>
                      <div className="flex gap-1">
                        {Array.from({ length: t.countdown }, (_, j) => (
                          <span
                            key={j}
                            className={`w-2.5 h-2.5 rounded-full ${
                              j < t.current
                                ? t.effect === "spirits"
                                  ? "bg-negative"
                                  : "bg-accent"
                                : "bg-edge"
                            }`}
                          />
                        ))}
                      </div>
                    </div>
                  ))}
                </div>
              )}

              {/* Queue (traverse) */}
              {turn.queue && turn.queue.length > 1 && (
                <div className="space-y-2">
                  <p className="text-dim font-bold">Ahead</p>
                  <div className="flex gap-2 items-center overflow-x-auto">
                    {turn.queue.slice(1).map((q, i) => (
                      <div
                        key={i}
                        className="shrink-0 px-3 py-2 rounded-lg border border-edge/50 bg-btn/50 text-dim"
                      >
                        <div>{q.name}</div>
                        <CostEffect opening={q} />
                      </div>
                    ))}
                  </div>
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
                <button
                  onClick={pressAdvantage}
                  disabled={loading || turn.momentum < PRESS_COST}
                  className={`w-full text-left p-4 border rounded-lg transition-colors flex items-center justify-between ${
                    turn.momentum >= PRESS_COST
                      ? "bg-btn hover:bg-btn-hover border-edge cursor-pointer group"
                      : "bg-btn/50 border-edge/50 opacity-50 cursor-default"
                  }`}
                >
                  <span className={`font-bold ${turn.momentum >= PRESS_COST ? "text-action group-hover:text-action-hover" : "text-muted"}`}>
                    Press the Advantage
                  </span>
                  <span className="flex items-center gap-1.5 text-dim">
                    <IconChip icon={ICONS.momentum} value={`-${PRESS_COST}`} /> <span className="text-muted">➽</span> <IconChip icon={ICONS.draw} value="+3" />
                  </span>
                </button>
                <button
                  onClick={forceOpening}
                  disabled={loading || turn.spirits < FORCE_COST}
                  className={`w-full text-left p-4 border rounded-lg transition-colors flex items-center justify-between ${
                    turn.spirits >= FORCE_COST
                      ? "bg-btn hover:bg-btn-hover border-edge cursor-pointer group"
                      : "bg-btn/50 border-edge/50 opacity-50 cursor-default"
                  }`}
                >
                  <span className={`font-bold ${turn.spirits >= FORCE_COST ? "text-action group-hover:text-action-hover" : "text-muted"}`}>
                    Force an Opening
                  </span>
                  <span className="flex items-center gap-1.5 text-dim">
                    <IconChip icon={ICONS.spirits} value={`-${FORCE_COST}`} /> <span className="text-muted">➽</span> <IconChip icon={ICONS.draw} value="+3" />
                  </span>
                </button>
              </div>
            </>
          )}

          {/* Finished phase */}
          {tactical.phase === "finished" && (
            <div className="space-y-6">
              <div
                className={`p-4 border rounded-lg ${
                  tactical.finishReason === "spiritsloss"
                    ? "border-negative bg-negative/15"
                    : "border-positive bg-positive/15"
                }`}
              >
                <p className="font-bold">
                  {tactical.finishReason === "resistancekill"
                    ? "Victory — Goal Reached"
                    : tactical.finishReason === "controlkill"
                      ? "Victory — Total Control"
                      : "Defeated — Spirits Depleted"}
                </p>
                {tactical.finishReason === "spiritsloss" && tactical.failureText && (
                  <p className="mt-2 text-primary/80 leading-loose whitespace-pre-wrap">
                    {formatProse(tactical.failureText)}
                  </p>
                )}
              </div>

              <button
                onClick={endTactical}
                disabled={loading}
                className="w-full text-left p-4 bg-btn hover:bg-btn-hover border border-edge rounded-lg text-action font-bold cursor-pointer transition-colors"
              >
                Continue
              </button>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
