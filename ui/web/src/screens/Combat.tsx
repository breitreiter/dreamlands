import { useEffect, useMemo, useRef, useState } from "react";
import { useGame } from "../GameContext";
import type { CombatInfo, CombatLogEntry, GameResponse } from "../api/types";
import { Button } from "@/components/ui/button";
import MaskedIcon from "../components/MaskedIcon";
import HitLens from "../components/HitLens";
import HitSplat from "../components/HitSplat";
import MissMoon from "../components/MissMoon";

type Hit = { id: number; x: number; y: number; angle: number; splat: number; miss: boolean };

// Hit-animation anchor: roughly torso-height on a bottom-anchored monster sprite.
function pickAnchor(rect: DOMRect): { x: number; y: number } {
  const cx = rect.width / 2;
  const cy = rect.height * 0.55;
  const jx = (Math.random() - 0.5) * 100;
  const jy = (Math.random() - 0.5) * 100;
  return { x: cx + jx, y: cy + jy };
}

/**
 * Combat screen — three-slot RPS.
 *
 * Each turn the player picks a Move for slots 1/2/3, then commits. The server
 * resolves all three slots in order, applies forward-rider stuns, and rolls the
 * AI's next commitment. The Tell banner shows what the AI is broadcasting; if
 * the player committed Read on the previous turn, the Plan banner shows the
 * AI's full three-slot commitment for the upcoming turn.
 *
 * Move pool comes from the server (CombatInfo.playerMovePool); cooldowns and
 * carry-stun are filtered locally using PlayerLastUsedTurn + PlayerCarryStun.
 *
 * This is a Phase-4 functional UI — clean enough to playtest, not a polish pass.
 */
export default function Combat({ state }: { state: GameResponse }) {
  const { doCombatAction, refreshState, loading } = useGame();
  const { combat } = state;
  const logRef = useRef<HTMLDivElement>(null);
  const hitboxRef = useRef<HTMLDivElement>(null);

  // Accumulate events for the lifetime of the combat session (reset on encounter change).
  const [allEvents, setAllEvents] = useState<CombatLogEntry[]>([]);
  const lastSeenRef = useRef<CombatLogEntry[] | null>(null);
  const lastEncounterRef = useRef<string | null>(null);

  // Three-slot picker selections. Reset every turn after a successful commit.
  const [selections, setSelections] = useState<(string | null)[]>([null, null, null]);

  const [hits, setHits] = useState<Hit[]>([]);
  const hitIdRef = useRef(0);
  const removeHit = (id: number) => setHits(prev => prev.filter(h => h.id !== id));

  useEffect(() => {
    if (!combat) return;
    if (combat.events === lastSeenRef.current) return;

    const fresh = combat.encounterId !== lastEncounterRef.current;
    if (fresh) {
      setAllEvents(combat.events);
      lastEncounterRef.current = combat.encounterId;
      setSelections([null, null, null]);
    } else {
      setAllEvents(prev => [...prev, ...combat.events]);
      // After a turn lands, reset slot picks.
      setSelections([null, null, null]);
    }
    lastSeenRef.current = combat.events;

    // Spawn hit animations for player-attack outcomes in this batch.
    if (!fresh && hitboxRef.current) {
      const rect = hitboxRef.current.getBoundingClientRect();
      const newHits: Hit[] = [];
      for (const e of combat.events) {
        if (!e.playerAttack) continue;
        const { x, y } = pickAnchor(rect);
        newHits.push({
          id: ++hitIdRef.current,
          x, y,
          angle: Math.random() * 360,
          splat: 1 + Math.floor(Math.random() * 8),
          miss: e.playerAttack.outcome === "miss",
        });
      }
      if (newHits.length) setHits(prev => [...prev, ...newHits]);
    }
  }, [combat]);

  useEffect(() => {
    if (logRef.current) logRef.current.scrollTop = logRef.current.scrollHeight;
  }, [allEvents.length]);

  if (!combat) return null;

  const monsterPct = combat.monsterMaxHp > 0
    ? Math.max(0, Math.min(100, (combat.monsterHp / combat.monsterMaxHp) * 100))
    : 0;

  // For each slot, what move is effectively chosen — accounting for carry-stun.
  const effectiveSelections = selections.map((s, i) =>
    combat.playerCarryStun[i] ? "Skipped" : s
  );
  const allSlotsPicked = effectiveSelections.every(s => s !== null && s !== "");
  const canCommit = allSlotsPicked && !combat.resolved && !loading;

  const onCommit = () => {
    if (!canCommit) return;
    doCombatAction({
      action: "commit",
      slots: effectiveSelections.map(s => s ?? "Skipped"),
    });
  };

  const onFlee = () => {
    if (combat.resolved || loading) return;
    doCombatAction({ action: "flee" });
  };

  return (
    <div className="flex h-screen overflow-hidden bg-page text-primary">
      {/* ─── Left: vignette + monster ─── */}
      <div className="relative w-[45%] shrink-0 bg-parchment overflow-hidden">
        {combat.biomeImage && (
          <img
            className="absolute inset-0 w-full h-full object-cover"
            style={{ filter: "brightness(0.35) saturate(0.7)" }}
            src={`/world/assets/vignettes/${combat.biomeImage}.webp`}
            alt=""
          />
        )}
        <div
          ref={hitboxRef}
          className="absolute inset-x-0 bottom-0 top-32 flex items-end justify-center"
        >
          {combat.image && (
            <img
              className="w-full h-full object-contain object-bottom pointer-events-none"
              style={{
                filter:
                  "drop-shadow(0 0 6px rgba(0,0,0,0.95)) drop-shadow(0 0 18px rgba(0,0,0,0.85)) drop-shadow(0 0 40px rgba(0,0,0,0.65)) drop-shadow(0 12px 24px rgba(0,0,0,0.6))",
              }}
              src={`/world/assets/${combat.image}`}
              alt={combat.title}
              onError={(e) => { (e.target as HTMLImageElement).style.display = "none"; }}
            />
          )}
          {hits.map(h => (
            <span key={h.id}>
              {h.miss
                ? <MissMoon x={h.x} y={h.y} angle={h.angle} />
                : <HitSplat x={h.x} y={h.y} angle={h.angle} variant={h.splat} color={combat.bloodColor} />}
              <HitLens x={h.x} y={h.y} angle={h.angle} onDone={() => removeHit(h.id)} />
            </span>
          ))}
        </div>

        {/* Monster nameplate */}
        <div className="absolute top-6 left-6 right-6 bg-black/[0.78] border border-[#3a3a3a] rounded-lg px-4 py-3">
          <div className="font-header text-[32px] text-accent leading-none mb-2">{combat.title}</div>
          <div className="relative h-[18px] bg-black/50 border border-[#3a3a3a] rounded overflow-hidden">
            <div
              className="h-full transition-all"
              style={{ width: `${monsterPct}%`, background: "linear-gradient(90deg, #800000, #c44a4a)" }}
            />
            <div className="absolute inset-0 flex items-center justify-center text-primary leading-none" style={{ textShadow: "0 0 4px rgba(0,0,0,0.9)" }}>
              {combat.monsterHp} / {combat.monsterMaxHp}
            </div>
          </div>
        </div>
      </div>

      {/* ─── Right: vitals + tell + log + slot picker ─── */}
      <div className="flex-1 flex flex-col bg-page min-w-0">
        {/* Header: player vitals + turn + weapon/armor */}
        <div className="px-10 py-5 border-b border-white/5 bg-panel-alt flex items-center gap-7">
          <StatPair color="#d96565" icon="heart-plus.svg" value={combat.playerHealth} max={combat.playerMaxHealth} />
          <StatPair color="#9b75d6" icon="sensuousness.svg" value={combat.playerSpirits} max={combat.playerMaxSpirits} />
          <div className="flex-1" />
          <Pill label="Turn" value={combat.turn.toString()} />
          <Pill label={combat.playerWeaponClass || "Unarmed"} value={combat.playerArmorClass || "Unarmored"} />
        </div>

        {/* Tell + plan banner */}
        {!combat.resolved && (
          <div className="px-10 py-3 border-b border-white/5 bg-panel-alt text-primary">
            <div>
              <em className="text-dim">{combat.tell}</em>
            </div>
            {combat.plan && combat.plan.length > 0 && (
              <div className="mt-1 text-action">
                <span className="text-muted">Plan:</span>{" "}
                {combat.plan.map((m, i) => (
                  <span key={i}>
                    <strong>{m}</strong>
                    {i < combat.plan!.length - 1 && <span className="text-muted"> / </span>}
                  </span>
                ))}
              </div>
            )}
          </div>
        )}

        {/* Log (scrollable) */}
        <div ref={logRef} className="flex-1 overflow-y-auto px-10 py-6 flex flex-col gap-2">
          <div className="mb-2">
            <h2 className="font-header text-[32px] text-accent leading-none">{combat.title}</h2>
            {combat.introText && <div className="text-dim mt-1.5">{combat.introText}</div>}
          </div>

          {allEvents.map((entry, i) => <LogEntry key={i} entry={entry} />)}

          {combat.resolved && <OutcomePanel combat={combat} />}
        </div>

        {/* Slot picker + actions */}
        {!combat.resolved && (
          <div className="px-10 pt-4 pb-7 border-t border-white/5 flex flex-col gap-4">
            <div className="grid grid-cols-3 gap-3">
              {[0, 1, 2].map(i => (
                <SlotPicker
                  key={i}
                  slot={i}
                  selected={selections[i]}
                  pool={combat.playerMovePool}
                  combat={combat}
                  otherSelections={selections.filter((_, j) => j !== i)}
                  onChange={v => setSelections(prev => {
                    const next = [...prev];
                    next[i] = v;
                    return next;
                  })}
                />
              ))}
            </div>
            <div className="grid grid-cols-2 gap-3">
              <Button size="lg" onClick={onCommit} disabled={!canCommit} className="w-full">
                Commit
              </Button>
              <Button size="lg" variant="secondary" onClick={onFlee} disabled={loading} className="w-full">
                Flee
              </Button>
            </div>
          </div>
        )}

        {combat.resolved && (
          <div className="px-10 pt-4 pb-7 border-t border-white/5">
            <Button size="lg" onClick={() => refreshState()} disabled={loading} className="w-full">
              Continue
            </Button>
          </div>
        )}
      </div>
    </div>
  );
}

// ── Slot picker ───────────────────────────────────────────────────────────

function SlotPicker({
  slot, selected, pool, combat, otherSelections, onChange,
}: {
  slot: number;
  selected: string | null;
  pool: string[];
  combat: CombatInfo;
  otherSelections: (string | null)[];
  onChange: (v: string | null) => void;
}) {
  const stunned = combat.playerCarryStun[slot];

  // Filter the pool for this slot. Cooldown gates: Rare/Power = once per turn
  // (mutually exclusive across slots in the same commit); Mythic/Slow = once
  // every other turn (current turn vs PlayerLastUsedTurn).
  const options = useMemo(() => {
    return pool.map(move => {
      const mutators = move.split(" ").map(s => s.toLowerCase()).slice(0, -1);
      const isOncePer = mutators.includes("rare") || mutators.includes("power");
      const isOnceEvery = mutators.includes("mythic") || mutators.includes("slow");

      let disabled = false;
      let reason: string | null = null;

      if (isOncePer && otherSelections.includes(move)) {
        disabled = true;
        reason = "once per turn";
      }
      if (isOnceEvery) {
        const last = combat.playerLastUsedTurn[move];
        if (last !== undefined && combat.turn - last < 2) {
          disabled = true;
          reason = "cooling down";
        }
      }

      return { move, disabled, reason };
    });
  }, [pool, otherSelections, combat.playerLastUsedTurn, combat.turn]);

  if (stunned) {
    return (
      <div className="flex flex-col gap-2">
        <div className="text-muted">Slot {slot + 1}</div>
        <div className="px-3 py-2.5 border border-white/10 rounded-lg bg-btn text-muted text-center">
          Stunned — Skipped
        </div>
      </div>
    );
  }

  return (
    <div className="flex flex-col gap-2">
      <div className="text-muted">Slot {slot + 1}</div>
      <select
        value={selected ?? ""}
        onChange={e => onChange(e.target.value || null)}
        className="px-3 py-2.5 border border-white/10 rounded-lg bg-btn text-action hover:bg-btn-hover focus:outline-none focus:border-action transition"
      >
        <option value="">— pick —</option>
        {options.map(({ move, disabled, reason }) => (
          <option key={move} value={move} disabled={disabled}>
            {move}{reason ? ` (${reason})` : ""}
          </option>
        ))}
      </select>
    </div>
  );
}

// ── Stat header bits ──────────────────────────────────────────────────────

function StatPair({ color, icon, value, max }: { color: string; icon: string; value: number; max: number }) {
  return (
    <div className="flex items-center gap-2 leading-none" style={{ color }}>
      <MaskedIcon icon={icon} color="currentColor" className="w-[26px] h-[26px]" />
      <div className="text-primary"><span style={{ color: "var(--color-primary)" }}>{value}</span> / {max}</div>
    </div>
  );
}

function Pill({ label, value }: { label: string; value: string }) {
  return (
    <div className="inline-flex items-baseline gap-1.5 px-3 py-1 border border-white/10 rounded-md text-dim leading-tight">
      {label} <span className="text-primary">{value}</span>
    </div>
  );
}

// ── Log entries ───────────────────────────────────────────────────────────

function LogEntry({ entry }: { entry: CombatLogEntry }) {
  const line = entry.text;

  // Turn header: "— Turn N — tell text [plan: ...]"
  if (line.startsWith("— Turn")) {
    return (
      <div className="flex items-center gap-3 text-muted mt-3">
        <div className="flex-1 h-px bg-white/[0.08]" />
        <span>{line.replace(/^—\s*|\s*—$/g, "").split(/\s+—\s+/)[0]}</span>
        <div className="flex-1 h-px bg-white/[0.08]" />
      </div>
    );
  }

  // Outcome banner
  if (line.startsWith("===")) {
    return <div className="font-header text-accent text-center mt-3">{line.replace(/=/g, "").trim()}</div>;
  }

  // Slot resolution: "  Slot N: you Attack | them Defend → -2 / 0\n    narration"
  if (line.startsWith("  Slot ")) {
    const [head, narration] = line.split("\n    ");
    return (
      <div className="leading-relaxed">
        <div className="font-mono text-primary">{head.replace(/^ {2}/, "")}</div>
        {narration && <div className="text-dim ml-6 italic">{narration}</div>}
      </div>
    );
  }

  // Plain prose (intro, flee message, etc.)
  return (
    <div className="leading-relaxed text-primary">{line}</div>
  );
}

// ── Outcome panel ─────────────────────────────────────────────────────────

function OutcomePanel({ combat }: { combat: CombatInfo }) {
  const verdict = combat.playerWon ? "Victory"
    : combat.playerLost ? "Defeat"
    : combat.playerFled ? "Escaped"
    : combat.monsterFled ? "It Flees"
    : "Resolved";

  return (
    <div className="mt-4 p-5 border border-white/10 rounded-lg bg-panel-alt">
      <div className="font-header text-[32px] text-accent leading-none mb-3">{verdict}</div>
      {combat.outcomeText && <div className="text-primary leading-relaxed whitespace-pre-line">{combat.outcomeText}</div>}
      {combat.outcomeMechanics && combat.outcomeMechanics.length > 0 && (
        <ul className="mt-3 space-y-1 text-dim">
          {combat.outcomeMechanics.map((m, i) => (
            <li key={i}>{m.description}</li>
          ))}
        </ul>
      )}
    </div>
  );
}
