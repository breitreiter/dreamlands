import { useState, useCallback, useRef, useEffect } from "react";
import type {
  TacticalEncounterData,
  TacticalApproachDef,
} from "../api/types";
import { formatProse } from "../prose";
import parchment from "../assets/parchment.png";

// ── Local engine state ─────────────────────────────────

interface ActiveTimer {
  name: string;
  effect: "spirits" | "resistance";
  amount: number;
  countdown: number;
  current: number;
  stopped: boolean;
}

interface Opening {
  poolIndex: number;
  name: string;
  costKind: string;
  costAmount: number;
  effectKind: string;
  effectAmount: number;
}

interface EngineState {
  resistance: number;
  maxResistance: number;
  momentum: number;
  spirits: number;
  turn: number;
  timers: ActiveTimer[];
  pool: Opening[];
  openings: Opening[];
  queue: Opening[] | null;
  bonusNextTurn: boolean;
}

type Phase =
  | { kind: "approach" }
  | { kind: "turn" }
  | { kind: "finished"; reason: "resistance_kill" | "control_kill" | "spirits_loss" };

interface LogEntry {
  turn: number;
  text: string;
  type: "info" | "damage" | "positive" | "timer_fire";
}

const PRESS_COST = 2;
const FORCE_COST = 2;
const BONUS_COUNT = 3;

// ── Sample encounter for prototyping ───────────────────

const SAMPLE: TacticalEncounterData = {
  id: "plains/tier1/Wolves on the Grain Road",
  title: "Wolves on the Grain Road",
  body: "Three wolves emerge from the tall grass along the roadside ditch. The largest blocks the road ahead, hackles raised, while the others fan out to either side. These aren't strays -- they move together, practiced and patient.",
  variant: "combat",
  intent: "violence",
  resistance: 8,
  momentum: 3,
  timerDraw: 2,
  timers: [
    { name: "Flanking Maneuver", effect: "spirits", amount: 2, countdown: 4 },
    { name: "Pack Howl", effect: "resistance", amount: 1, countdown: 5 },
    { name: "Closing Circle", effect: "spirits", amount: 1, countdown: 3 },
  ],
  openings: [
    { name: "Lunge", costKind: "momentum", costAmount: 2, effectKind: "damage", effectAmount: 3 },
    { name: "Feint", costKind: "momentum", costAmount: 1, effectKind: "damage", effectAmount: 1 },
    { name: "Hold Ground", costKind: "free", costAmount: 0, effectKind: "momentum", effectAmount: 2 },
    { name: "Break the Circle", costKind: "tick", costAmount: 0, effectKind: "stop_timer", effectAmount: 0 },
    { name: "Trap Line", costKind: "free", costAmount: 0, effectKind: "damage", effectAmount: 4, requires: "has bear_trap" },
  ],
  approaches: [
    { kind: "scout", momentum: 0, timerCount: 2, bonusOpenings: 3 },
    { kind: "direct", momentum: 3, timerCount: 2, bonusOpenings: 0 },
    { kind: "wild", momentum: 5, timerCount: 3, bonusOpenings: 0 },
  ],
  failure: {
    text: "The pack drags you down. You fight free but leave blood and gear behind in the road dust.",
    mechanics: ["damage_spirits 3", "lose_random_item"],
  },
};

// ── Helpers ────────────────────────────────────────────

function buildPool(enc: TacticalEncounterData): Opening[] {
  return enc.openings
    .filter((o) => !o.requires) // skip gear-gated for prototype
    .map((o, i) => ({
      poolIndex: i,
      name: o.name,
      costKind: o.costKind,
      costAmount: o.costAmount,
      effectKind: o.effectKind,
      effectAmount: o.effectAmount,
    }));
}

function randomFrom<T>(arr: T[]): T {
  return arr[Math.floor(Math.random() * arr.length)];
}

function drawTimers(enc: TacticalEncounterData, count: number): ActiveTimer[] {
  const available = [...enc.timers];
  const drawn: ActiveTimer[] = [];
  for (let i = 0; i < count && available.length > 0; i++) {
    const idx = Math.floor(Math.random() * available.length);
    const def = available.splice(idx, 1)[0];
    drawn.push({
      name: def.name,
      effect: def.effect,
      amount: def.amount,
      countdown: def.countdown,
      current: def.countdown,
      stopped: false,
    });
  }
  return drawn;
}

function generateOpenings(pool: Opening[], count: number): Opening[] {
  if (pool.length === 0) return [];
  return Array.from({ length: count }, () => randomFrom(pool));
}

function formatCost(kind: string, amount: number): string {
  if (kind === "free") return "Free";
  if (kind === "tick") return "Tick timer";
  return `${amount} ${kind}`;
}

function formatEffect(kind: string, amount: number): string {
  if (kind === "stop_timer") return "Stop timer";
  if (kind === "damage") return `${amount} damage`;
  return `+${amount} ${kind}`;
}

// ── Component ──────────────────────────────────────────

export default function TacticalEncounter() {
  const enc = SAMPLE;
  const [phase, setPhase] = useState<Phase>({ kind: "approach" });
  const [engine, setEngine] = useState<EngineState | null>(null);
  const [log, setLog] = useState<LogEntry[]>([]);
  const logRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (logRef.current) {
      logRef.current.scrollTop = logRef.current.scrollHeight;
    }
  }, [log.length]);

  // ── Approach selection ─────────────────────────────

  const chooseApproach = useCallback(
    (approach: TacticalApproachDef) => {
      const pool = buildPool(enc);
      const timers = drawTimers(enc, approach.timerCount);
      const bonus = approach.bonusOpenings > 0;
      const openings = generateOpenings(pool, bonus ? BONUS_COUNT : 1);

      const s: EngineState = {
        resistance: enc.resistance,
        maxResistance: enc.resistance,
        momentum: approach.momentum,
        spirits: 20, // simulated starting spirits
        turn: 1,
        timers,
        pool,
        openings,
        queue: null,
        bonusNextTurn: false,
      };

      setEngine(s);
      setLog([
        {
          turn: 0,
          text: `Approach: ${approach.kind}. Starting momentum ${approach.momentum}, ${timers.length} timer(s).`,
          type: "info",
        },
      ]);
      setPhase({ kind: "turn" });
    },
    [enc]
  );

  // ── Turn actions ───────────────────────────────────

  const advanceTurn = useCallback(
    (s: EngineState): { state: EngineState; finished: Phase | null; newLogs: LogEntry[] } => {
      const logs: LogEntry[] = [];
      s.turn++;

      // Tick timers
      for (const t of s.timers) {
        if (t.stopped) continue;
        t.current--;
        if (t.current <= 0) {
          if (t.effect === "spirits") {
            s.spirits = Math.max(0, s.spirits - t.amount);
            logs.push({ turn: s.turn, text: `${t.name} fires: -${t.amount} Spirits`, type: "timer_fire" });
          } else {
            s.resistance += t.amount;
            logs.push({ turn: s.turn, text: `${t.name} fires: +${t.amount} Resistance`, type: "timer_fire" });
          }
          t.current = t.countdown;
        }
      }

      // Spirits loss check
      if (s.spirits <= 0) {
        logs.push({ turn: s.turn, text: "Spirits depleted. You are overwhelmed.", type: "damage" });
        return { state: s, finished: { kind: "finished", reason: "spirits_loss" }, newLogs: logs };
      }

      // Passive momentum
      s.momentum++;
      logs.push({ turn: s.turn, text: `Turn ${s.turn}: +1 Momentum (${s.momentum})`, type: "info" });

      // Generate openings
      const count = s.bonusNextTurn ? BONUS_COUNT : 1;
      s.bonusNextTurn = false;
      s.openings = generateOpenings(s.pool, count);

      return { state: s, finished: null, newLogs: logs };
    },
    []
  );

  const takeOpening = useCallback(
    (index: number) => {
      if (!engine) return;
      const s = { ...engine, timers: engine.timers.map((t) => ({ ...t })) };
      const opening = s.openings[index];
      const newLogs: LogEntry[] = [];

      // Pay cost
      switch (opening.costKind) {
        case "momentum":
          if (s.momentum < opening.costAmount) return;
          s.momentum -= opening.costAmount;
          break;
        case "spirits":
          if (s.spirits < opening.costAmount) return;
          s.spirits -= opening.costAmount;
          break;
        case "tick": {
          const active = s.timers.filter((t) => !t.stopped);
          if (active.length > 0) {
            const target = active[Math.floor(Math.random() * active.length)];
            target.current--;
            newLogs.push({ turn: s.turn, text: `Tick: ${target.name} advanced`, type: "info" });
          }
          break;
        }
      }

      // Apply effect
      switch (opening.effectKind) {
        case "damage":
          s.resistance = Math.max(0, s.resistance - opening.effectAmount);
          newLogs.push({
            turn: s.turn,
            text: `${opening.name}: -${opening.effectAmount} Resistance (${s.resistance} remaining)`,
            type: "positive",
          });
          break;
        case "stop_timer": {
          const target = s.timers
            .filter((t) => !t.stopped)
            .sort((a, b) => a.current - b.current)[0];
          if (target) {
            target.stopped = true;
            newLogs.push({ turn: s.turn, text: `${opening.name}: Stopped ${target.name}`, type: "positive" });
          }
          break;
        }
        case "momentum":
          s.momentum += opening.effectAmount;
          newLogs.push({
            turn: s.turn,
            text: `${opening.name}: +${opening.effectAmount} Momentum (${s.momentum})`,
            type: "positive",
          });
          break;
      }

      // Check win conditions
      if (s.resistance <= 0) {
        setLog((prev) => [...prev, ...newLogs]);
        setEngine(s);
        setPhase({ kind: "finished", reason: "resistance_kill" });
        return;
      }
      if (s.timers.every((t) => t.stopped)) {
        setLog((prev) => [...prev, ...newLogs]);
        setEngine(s);
        setPhase({ kind: "finished", reason: "control_kill" });
        return;
      }

      // Advance turn
      const result = advanceTurn(s);
      setLog((prev) => [...prev, ...newLogs, ...result.newLogs]);
      setEngine(result.state);
      if (result.finished) setPhase(result.finished);
    },
    [engine, advanceTurn]
  );

  const pressAdvantage = useCallback(() => {
    if (!engine || engine.momentum < PRESS_COST) return;
    const s = { ...engine, timers: engine.timers.map((t) => ({ ...t })) };
    s.momentum -= PRESS_COST;
    s.bonusNextTurn = true;

    const result = advanceTurn(s);
    setLog((prev) => [
      ...prev,
      { turn: s.turn, text: `Press the Advantage: -${PRESS_COST} Momentum`, type: "info" },
      ...result.newLogs,
    ]);
    setEngine(result.state);
    if (result.finished) setPhase(result.finished);
  }, [engine, advanceTurn]);

  const forceOpening = useCallback(() => {
    if (!engine || engine.spirits < FORCE_COST) return;
    const s = { ...engine, timers: engine.timers.map((t) => ({ ...t })) };
    s.spirits -= FORCE_COST;
    s.bonusNextTurn = true;

    const result = advanceTurn(s);
    setLog((prev) => [
      ...prev,
      { turn: s.turn, text: `Force an Opening: -${FORCE_COST} Spirits`, type: "damage" },
      ...result.newLogs,
    ]);
    setEngine(result.state);
    if (result.finished) setPhase(result.finished);
  }, [engine, advanceTurn]);

  const canAfford = (opening: Opening): boolean => {
    if (!engine) return false;
    if (opening.costKind === "momentum") return engine.momentum >= opening.costAmount;
    if (opening.costKind === "spirits") return engine.spirits >= opening.costAmount;
    return true;
  };

  // ── Render ─────────────────────────────────────────

  return (
    <div className="h-full flex bg-page text-primary">
      {/* Left panel — vignette */}
      <div
        className="hidden md:flex w-[45%] shrink-0 items-center justify-center"
        style={{ backgroundImage: `url(${parchment})`, backgroundSize: "cover", backgroundPosition: "center" }}
      >
        <div className="text-contrast/30 font-header text-[32px] uppercase tracking-wider">
          {enc.intent ?? enc.variant}
        </div>
      </div>

      {/* Right panel */}
      <div className="flex-1 overflow-y-auto p-8 md:p-12 flex flex-col">
        <div className="max-w-2xl w-full space-y-6">
          {/* Title */}
          <h2 className="font-header text-[32px] text-accent uppercase">{enc.title}</h2>

          {/* Approach phase */}
          {phase.kind === "approach" && (
            <>
              <div className="text-primary/80 leading-loose whitespace-pre-wrap">
                {formatProse(enc.body)}
              </div>

              <div className="space-y-3 pt-2">
                <p className="text-dim font-bold">Choose your approach:</p>
                {enc.approaches.map((a) => (
                  <button
                    key={a.kind}
                    onClick={() => chooseApproach(a)}
                    className="w-full text-left p-4 bg-btn hover:bg-btn-hover border border-edge rounded-lg transition-colors group cursor-pointer"
                  >
                    <span className="font-bold text-action group-hover:text-action-hover capitalize">
                      {a.kind}
                    </span>
                    <span className="block text-dim mt-1">
                      {a.kind === "scout"
                        ? `No momentum, but ${a.bonusOpenings} openings on turn 1. Study the situation.`
                        : a.kind === "direct"
                          ? `Start with ${a.momentum} momentum. Balanced and reliable.`
                          : `Start with ${a.momentum} momentum, but ${a.timerCount} timers. High risk, high reward.`}
                    </span>
                  </button>
                ))}
              </div>
            </>
          )}

          {/* Turn phase */}
          {phase.kind === "turn" && engine && (
            <>
              {/* Resource bars */}
              <div className="space-y-3">
                <ResourceBar
                  label="Resistance"
                  current={engine.resistance}
                  max={engine.maxResistance}
                  color="bg-accent"
                />
                <div className="flex gap-6">
                  <div className="flex-1">
                    <span className="text-dim">Momentum</span>
                    <span className="ml-2 font-bold text-primary">{engine.momentum}</span>
                  </div>
                  <div className="flex-1">
                    <span className="text-dim">Spirits</span>
                    <span className="ml-2 font-bold text-primary">{engine.spirits}</span>
                  </div>
                  <div className="text-dim">
                    Turn {engine.turn}
                  </div>
                </div>
              </div>

              {/* Timers */}
              {engine.timers.length > 0 && (
                <div className="space-y-2">
                  <p className="text-dim font-bold">Timers</p>
                  {engine.timers.map((t, i) => (
                    <div
                      key={i}
                      className={`flex items-center gap-3 ${t.stopped ? "opacity-30 line-through" : ""}`}
                    >
                      <span className="flex-1">{t.name}</span>
                      <span className="text-dim">
                        {t.effect === "spirits" ? `-${t.amount} Spr` : `+${t.amount} Res`}
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

              {/* Openings */}
              <div className="space-y-2">
                <p className="text-dim font-bold">
                  {engine.openings.length > 1 ? "Choose an opening:" : "Opening:"}
                </p>
                {engine.openings.map((o, i) => {
                  const affordable = canAfford(o);
                  return (
                    <button
                      key={i}
                      onClick={affordable ? () => takeOpening(i) : undefined}
                      disabled={!affordable}
                      className={`w-full text-left p-4 border rounded-lg transition-colors ${
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
                      <span className="block text-dim mt-1">
                        {formatCost(o.costKind, o.costAmount)}
                        {" → "}
                        {formatEffect(o.effectKind, o.effectAmount)}
                      </span>
                    </button>
                  );
                })}
              </div>

              {/* Press / Force buttons */}
              <div className="flex gap-3">
                <button
                  onClick={pressAdvantage}
                  disabled={engine.momentum < PRESS_COST}
                  className={`flex-1 p-3 rounded-lg border transition-colors ${
                    engine.momentum >= PRESS_COST
                      ? "bg-btn hover:bg-btn-hover border-edge text-action cursor-pointer"
                      : "bg-btn/50 border-edge/50 text-muted cursor-default"
                  }`}
                >
                  <span className="font-bold">Press the Advantage</span>
                  <span className="block text-dim mt-1">-{PRESS_COST} Momentum, 3 openings next turn</span>
                </button>
                <button
                  onClick={forceOpening}
                  disabled={engine.spirits < FORCE_COST}
                  className={`flex-1 p-3 rounded-lg border transition-colors ${
                    engine.spirits >= FORCE_COST
                      ? "bg-btn hover:bg-btn-hover border-edge text-action cursor-pointer"
                      : "bg-btn/50 border-edge/50 text-muted cursor-default"
                  }`}
                >
                  <span className="font-bold">Force an Opening</span>
                  <span className="block text-dim mt-1">-{FORCE_COST} Spirits, 3 openings next turn</span>
                </button>
              </div>

              {/* Turn log */}
              {log.length > 0 && (
                <div
                  ref={logRef}
                  className="max-h-48 overflow-y-auto border-t border-edge pt-3 space-y-1"
                >
                  {log.map((entry, i) => (
                    <div
                      key={i}
                      className={
                        entry.type === "damage"
                          ? "text-negative"
                          : entry.type === "positive"
                            ? "text-positive"
                            : entry.type === "timer_fire"
                              ? "text-negative"
                              : "text-dim"
                      }
                    >
                      {entry.text}
                    </div>
                  ))}
                </div>
              )}
            </>
          )}

          {/* Finished phase */}
          {phase.kind === "finished" && engine && (
            <div className="space-y-6">
              <div
                className={`p-4 border rounded-lg ${
                  phase.reason === "spirits_loss"
                    ? "border-negative bg-negative/15"
                    : "border-positive bg-positive/15"
                }`}
              >
                <p className="font-bold">
                  {phase.reason === "resistance_kill"
                    ? "Victory — Resistance Broken"
                    : phase.reason === "control_kill"
                      ? "Victory — Total Control"
                      : "Defeated — Spirits Depleted"}
                </p>
                {phase.reason === "spirits_loss" && (
                  <p className="mt-2 text-primary/80 leading-loose whitespace-pre-wrap">
                    {formatProse(enc.failure.text)}
                  </p>
                )}
              </div>

              {/* Turn log */}
              {log.length > 0 && (
                <div className="max-h-64 overflow-y-auto border-t border-edge pt-3 space-y-1">
                  {log.map((entry, i) => (
                    <div
                      key={i}
                      className={
                        entry.type === "damage" || entry.type === "timer_fire"
                          ? "text-negative"
                          : entry.type === "positive"
                            ? "text-positive"
                            : "text-dim"
                      }
                    >
                      {entry.text}
                    </div>
                  ))}
                </div>
              )}

              <button
                onClick={() => {
                  setPhase({ kind: "approach" });
                  setEngine(null);
                  setLog([]);
                }}
                className="p-3 bg-btn hover:bg-btn-hover border border-edge rounded-lg text-action font-bold cursor-pointer transition-colors"
              >
                Play Again
              </button>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}

// ── Sub-components ─────────────────────────────────────

function ResourceBar({
  label,
  current,
  max,
  color,
}: {
  label: string;
  current: number;
  max: number;
  color: string;
}) {
  const pct = max > 0 ? Math.max(0, Math.min(100, (current / max) * 100)) : 0;
  return (
    <div>
      <div className="flex justify-between mb-1">
        <span className="text-dim">{label}</span>
        <span className="font-bold">
          {current} / {max}
        </span>
      </div>
      <div className="h-3 bg-edge/30 rounded-full overflow-hidden">
        <div
          className={`h-full ${color} rounded-full transition-all duration-300`}
          style={{ width: `${pct}%` }}
        />
      </div>
    </div>
  );
}
