import { useState, useCallback, useRef, useEffect } from "react";
import type {
  TacticalEncounterData,
  TacticalApproachDef,
} from "../api/types";
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

// ── Local engine state ─────────────────────────────────

interface ActiveTimer {
  name: string;
  counterName: string; // name of the opening that stops this timer
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
  stopsTimerIndex?: number; // if stop_timer, which specific timer
}

interface EngineState {
  resistance: number;
  resistanceMax: number;
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
    { name: "Flanking Maneuver", counterName: "Block the flank", effect: "spirits", amount: 2, countdown: 4 },
    { name: "Pack Howl", counterName: "Silence the alpha", effect: "resistance", amount: 1, countdown: 5 },
    { name: "Closing Circle", counterName: "Break the circle", effect: "spirits", amount: 1, countdown: 3 },
  ],
  openings: [
    { name: "Lunge", costKind: "momentum", costAmount: 2, effectKind: "damage", effectAmount: 3 },
    { name: "Feint", costKind: "momentum", costAmount: 1, effectKind: "damage", effectAmount: 1 },
    { name: "Hold Ground", costKind: "free", costAmount: 0, effectKind: "momentum", effectAmount: 2 },
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

const SAMPLE_TRAVERSE: TacticalEncounterData = {
  id: "plains/tier1/The Washed-Out Ford",
  title: "The Washed-Out Ford",
  body: "The ford marked on your map is gone -- three days of rain have turned the creek into a surging brown torrent. Broken fence posts and uprooted shrubs tumble past in the current. The far bank is only twenty yards away, but the water looks waist-deep and fast.",
  variant: "traverse",
  intent: "exploration",
  resistance: 6,
  queueDepth: 5,
  timerDraw: 1,
  timers: [
    { name: "Rising Water", counterName: "Find high ground", effect: "resistance", amount: 1, countdown: 4 },
    { name: "Debris", counterName: "Clear the path", effect: "spirits", amount: 1, countdown: 3 },
  ],
  openings: [
    { name: "Wade Carefully", costKind: "free", costAmount: 0, effectKind: "damage", effectAmount: 1 },
    { name: "Brace and Push", costKind: "momentum", costAmount: 1, effectKind: "damage", effectAmount: 2 },
    { name: "Find Footing", costKind: "free", costAmount: 0, effectKind: "momentum", effectAmount: 1 },
    { name: "Strong Stroke", costKind: "spirits", costAmount: 1, effectKind: "damage", effectAmount: 3 },
  ],
  approaches: [],
  failure: {
    text: "The current takes your legs out. You wash up downstream, soaked and bruised, missing something from your pack.",
    mechanics: ["damage_spirits 2", "lose_random_item", "add_condition exhausted"],
  },
};

const SAMPLES = { combat: SAMPLE, traverse: SAMPLE_TRAVERSE };

// ── Helpers ────────────────────────────────────────────

function buildPool(enc: TacticalEncounterData, timers: ActiveTimer[]): Opening[] {
  const pool: Opening[] = enc.openings
    .filter((o) => !o.requires && o.effectKind !== "stop_timer")
    .map((o, i) => ({
      poolIndex: i,
      name: o.name,
      costKind: o.costKind,
      costAmount: o.costAmount,
      effectKind: o.effectKind,
      effectAmount: o.effectAmount,
    }));

  // Generate a counter opening for each active timer
  timers.forEach((t, timerIdx) => {
    pool.push({
      poolIndex: 1000 + timerIdx,
      name: t.counterName,
      costKind: "tick",
      costAmount: 0,
      effectKind: "stop_timer",
      effectAmount: 0,
      stopsTimerIndex: timerIdx,
    });
  });

  return pool;
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
      counterName: def.counterName,
      effect: def.effect,
      amount: def.amount,
      countdown: def.countdown,
      current: def.countdown,
      stopped: false,
    });
  }
  return drawn;
}

function generateOpenings(pool: Opening[], count: number, timers?: ActiveTimer[]): Opening[] {
  const available = timers
    ? pool.filter((o) => o.stopsTimerIndex == null || !timers[o.stopsTimerIndex].stopped)
    : pool;
  if (available.length === 0) return [];
  return Array.from({ length: count }, () => randomFrom(available));
}

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

function CostEffect({ opening }: { opening: Opening }) {
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

// ── Component ──────────────────────────────────────────

export default function TacticalEncounter() {
  const [sampleKey, setSampleKey] = useState<"combat" | "traverse">("combat");
  const [enc, setEnc] = useState<TacticalEncounterData>(SAMPLES.combat);
  const [phase, setPhase] = useState<Phase>({ kind: "approach" });
  const [engine, setEngine] = useState<EngineState | null>(null);
  const [log, setLog] = useState<LogEntry[]>([]);
  const logRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (logRef.current) {
      logRef.current.scrollTop = logRef.current.scrollHeight;
    }
  }, [log.length]);

  // ── Start encounter (traverse skips approach) ──────

  const startEncounter = useCallback(
    (encounter: TacticalEncounterData) => {
      if (encounter.variant === "combat" && encounter.approaches.length > 0) {
        setPhase({ kind: "approach" });
        return;
      }

      // Traverse (or combat with no approaches): go straight to turn
      const timers = drawTimers(encounter, encounter.timerDraw);
      const pool = buildPool(encounter, timers);
      const queueDepth = encounter.queueDepth ?? 5;
      const queue = Array.from({ length: queueDepth }, () => randomFrom(
        pool.filter((o) => o.stopsTimerIndex == null || !timers[o.stopsTimerIndex]?.stopped)
      ));
      const openings = [queue[0]];

      const s: EngineState = {
        resistance: encounter.resistance,
        resistanceMax: encounter.resistance,
        momentum: encounter.momentum ?? 0,
        spirits: 20,
        turn: 1,
        timers,
        pool,
        openings,
        queue,
        bonusNextTurn: false,
      };

      setEngine(s);
      setLog([{ turn: 0, text: `${encounter.variant === "traverse" ? "Traverse" : "Combat"} begins. ${timers.length} threat(s).`, type: "info" }]);
      setPhase({ kind: "turn" });
    },
    []
  );

  // ── Approach selection (combat only) ───────────────

  const chooseApproach = useCallback(
    (approach: TacticalApproachDef) => {
      const timers = drawTimers(enc, approach.timerCount);
      const pool = buildPool(enc, timers);
      const bonus = approach.bonusOpenings > 0;
      const openings = generateOpenings(pool, bonus ? BONUS_COUNT : 1, timers);

      const s: EngineState = {
        resistance: enc.resistance,
        resistanceMax: enc.resistance,
        momentum: approach.momentum,
        spirits: 20,
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
          text: `Approach: ${approach.kind}. Starting momentum ${approach.momentum}, ${timers.length} threat(s).`,
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
            logs.push({ turn: s.turn, text: `${t.name} fires: -${t.amount} Progress`, type: "timer_fire" });
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

      if (s.queue) {
        // Traverse: front of queue is the opening
        if (s.queue.length > 0) {
          s.openings = [s.queue[0]];
          // Bonus openings are ephemeral extras alongside the queue front
          for (let i = 1; i < count; i++)
            s.openings.push(randomFrom(
              s.pool.filter((o) => o.stopsTimerIndex == null || !s.timers[o.stopsTimerIndex]?.stopped)
            ));
        } else {
          s.openings = generateOpenings(s.pool, count, s.timers);
        }
      } else {
        // Combat: random openings
        s.openings = generateOpenings(s.pool, count, s.timers);
      }

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
            text: `${opening.name}: +${opening.effectAmount} Progress`,
            type: "positive",
          });
          break;
        case "stop_timer": {
          const targetIdx = opening.stopsTimerIndex;
          const target = targetIdx != null ? s.timers[targetIdx] : null;
          if (target && !target.stopped) {
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

      // Traverse: advance the queue
      if (s.queue && s.queue.length > 0) {
        s.queue = [...s.queue.slice(1)];
        // Replenish to target depth
        const targetDepth = enc.queueDepth ?? 5;
        const availablePool = s.pool.filter((o) => o.stopsTimerIndex == null || !s.timers[o.stopsTimerIndex]?.stopped);
        while (s.queue.length < targetDepth && availablePool.length > 0) {
          s.queue.push(randomFrom(availablePool));
        }
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

          {/* Encounter picker (dev only) */}
          {phase.kind === "approach" && !engine && (
            <div className="flex gap-2">
              {(Object.keys(SAMPLES) as Array<keyof typeof SAMPLES>).map((key) => (
                <button
                  key={key}
                  onClick={() => { setSampleKey(key); setEnc(SAMPLES[key]); }}
                  className={`px-4 py-2 rounded-lg border transition-colors cursor-pointer ${
                    sampleKey === key
                      ? "bg-btn border-accent text-accent"
                      : "bg-btn border-edge text-action-dim hover:text-action"
                  }`}
                >
                  {key}
                </button>
              ))}
            </div>
          )}

          {/* Approach phase (combat) */}
          {phase.kind === "approach" && enc.variant === "combat" && enc.approaches.length > 0 && (
            <>
              <div className="text-primary/80 leading-loose whitespace-pre-wrap">
                {formatProse(enc.body)}
              </div>

              <div className="space-y-3 pt-2">
                <p className="text-dim font-bold">It's a fight.</p>
                {enc.approaches.map((a) => {
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

          {/* Approach phase (traverse — auto-start with intro) */}
          {phase.kind === "approach" && (enc.variant === "traverse" || enc.approaches.length === 0) && (
            <>
              <div className="text-primary/80 leading-loose whitespace-pre-wrap">
                {formatProse(enc.body)}
              </div>
              <button
                onClick={() => startEncounter(enc)}
                className="w-full text-left p-4 bg-btn hover:bg-btn-hover border border-edge rounded-lg transition-colors group cursor-pointer"
              >
                <span className="font-bold text-action group-hover:text-action-hover">
                  Begin the crossing
                </span>
              </button>
            </>
          )}

          {/* Turn phase */}
          {phase.kind === "turn" && engine && (
            <>
              {/* Resources */}
              <div className="flex gap-6">
                <div className="flex-1 text-center">
                  <div className="text-dim">Progress</div>
                  <div className="font-bold text-primary text-[32px] leading-tight flex items-center justify-center gap-1.5">
                    <MaskedIcon icon={ICONS.progress} className="w-7 h-7" color="#d0bd62" />
                    {engine.resistanceMax - engine.resistance}
                    <span className="text-dim font-normal"> of {engine.resistanceMax}</span>
                  </div>
                </div>
                <div className="flex-1 text-center">
                  <div className="text-dim">Momentum</div>
                  <div className="font-bold text-primary text-[32px] leading-tight flex items-center justify-center gap-1.5">
                    <MaskedIcon icon={ICONS.momentum} className="w-7 h-7" color="#d0bd62" />
                    {engine.momentum}
                  </div>
                </div>
                <div className="flex-1 text-center">
                  <div className="text-dim">Spirits</div>
                  <div className="font-bold text-primary text-[32px] leading-tight flex items-center justify-center gap-1.5">
                    <MaskedIcon icon={ICONS.spirits} className="w-7 h-7" color="#d0bd62" />
                    {engine.spirits}
                  </div>
                </div>
              </div>

              {/* Timers */}
              {engine.timers.length > 0 && (
                <div className="space-y-2">
                  <p className="text-dim font-bold">Threats</p>
                  {engine.timers.map((t, i) => (
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

              {/* Queue (traverse only) */}
              {engine.queue && engine.queue.length > 1 && (
                <div className="space-y-2">
                  <p className="text-dim font-bold">Ahead</p>
                  <div className="flex gap-2 items-center overflow-x-auto">
                    {engine.queue.slice(1).map((q, i) => (
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

              {/* Openings */}
              <div className="space-y-2">
                <p className="text-dim font-bold">
                  {engine.openings.length > 1 ? "Choose a move:" : "Your move:"}
                </p>
                {engine.openings.map((o, i) => {
                  const affordable = canAfford(o);
                  return (
                    <button
                      key={i}
                      onClick={affordable ? () => takeOpening(i) : undefined}
                      disabled={!affordable}
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
                  disabled={engine.momentum < PRESS_COST}
                  className={`w-full text-left p-4 border rounded-lg transition-colors flex items-center justify-between ${
                    engine.momentum >= PRESS_COST
                      ? "bg-btn hover:bg-btn-hover border-edge cursor-pointer group"
                      : "bg-btn/50 border-edge/50 opacity-50 cursor-default"
                  }`}
                >
                  <span className={`font-bold ${engine.momentum >= PRESS_COST ? "text-action group-hover:text-action-hover" : "text-muted"}`}>
                    Press the Advantage
                  </span>
                  <span className="flex items-center gap-1.5 text-dim">
                    <IconChip icon={ICONS.momentum} value={`-${PRESS_COST}`} /> <span className="text-muted">➽</span> <IconChip icon={ICONS.draw} value="+3" />
                  </span>
                </button>
                <button
                  onClick={forceOpening}
                  disabled={engine.spirits < FORCE_COST}
                  className={`w-full text-left p-4 border rounded-lg transition-colors flex items-center justify-between ${
                    engine.spirits >= FORCE_COST
                      ? "bg-btn hover:bg-btn-hover border-edge cursor-pointer group"
                      : "bg-btn/50 border-edge/50 opacity-50 cursor-default"
                  }`}
                >
                  <span className={`font-bold ${engine.spirits >= FORCE_COST ? "text-action group-hover:text-action-hover" : "text-muted"}`}>
                    Force an Opening
                  </span>
                  <span className="flex items-center gap-1.5 text-dim">
                    <IconChip icon={ICONS.spirits} value={`-${FORCE_COST}`} /> <span className="text-muted">➽</span> <IconChip icon={ICONS.draw} value="+3" />
                  </span>
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
                    ? "Victory — Goal Reached"
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
                className="w-full text-left p-4 bg-btn hover:bg-btn-hover border border-edge rounded-lg text-action font-bold cursor-pointer transition-colors"
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

