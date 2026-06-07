import { useEffect, useMemo, useRef, useState, type ReactNode } from "react";
import { useGame } from "../GameContext";
import type { CombatInfo, CombatLogEntry, GameResponse } from "../api/types";
import MaskedIcon from "../components/MaskedIcon";
import HitLens from "../components/HitLens";
import HitSplat from "../components/HitSplat";
import MissMoon from "../components/MissMoon";
import TopBar from "../components/TopBar";
import TravailStrip from "../components/TravailStrip";
import { Button } from "@/components/ui/button";

type Hit = { id: number; x: number; y: number; angle: number; splat: number; miss: boolean };

// Hit-animation anchor: roughly torso-height on a bottom-anchored monster sprite.
function pickAnchor(rect: DOMRect): { x: number; y: number } {
  const cx = rect.width / 2;
  const cy = rect.height * 0.55;
  const jx = (Math.random() - 0.5) * 100;
  const jy = (Math.random() - 0.5) * 100;
  return { x: cx + jx, y: cy + jy };
}

// ── Move helpers ───────────────────────────────────────────────────────────

type MoveBase = "attack" | "defend" | "recover" | "read" | "skipped";

function moveBase(encoded: string): MoveBase {
  const last = encoded.split(" ").pop()?.toLowerCase() ?? "";
  if (last === "attack" || last === "defend" || last === "recover" || last === "read" || last === "skipped") {
    return last;
  }
  return "skipped";
}

// Player move labels come from the server-authored displayName on each MoveOption.
// For slot tooltips reconstructed from a bare encoded string (selections, monster
// commits), look up the player's pool by encoding; fall back to the encoded form
// for moves not in the player pool (currently any monster move — monsters don't
// author display names, and their slot icons already convey family).
function lookupDisplayName(encoded: string, combat: CombatInfo): string {
  return combat.playerMovePool.find(o => o.encoding === encoded)?.displayName ?? encoded;
}

// Base-verb iconography. Stuns route through `knockout.svg` separately (see
// SLOT_STUN_ICON / PREVIEW_STUN_ICON). Telegraphed enemy attacks override
// the plain attack icon with `cross-flare.svg`.
const BASE_ICON: Record<MoveBase, string | null> = {
  attack: "sword-brandish.svg",
  defend: "checked-shield.svg",
  recover: "nested-hearts.svg",
  read: "one-eyed.svg",
  skipped: null,
};

const STUN_ICON = "knockout.svg";
const TELEGRAPHED_ATTACK_ICON = "cross-flare.svg";

/**
 * Plain-English tooltip describing what a move does, derived from its
 * base verb + mutators. Mirrors the Resolver.cs / Move.cs vocabulary so
 * tuning changes there should be reflected here.
 */
function moveTooltip(encoded: string): string {
  const tokens = encoded.split(" ").map(t => t.toLowerCase());
  const base = moveBase(encoded);
  const has = (t: string) => tokens.includes(t);
  const parts: string[] = [];

  switch (base) {
    case "attack":   parts.push("Deal damage."); break;
    case "defend":   parts.push("Block incoming damage in this slot."); break;
    case "recover":  parts.push("Heal spirits."); break;
    case "read":     parts.push("Reveal the enemy's plan for next turn."); break;
    case "skipped":  parts.push("No action."); break;
  }

  if (has("heavy")) {
    if (base === "attack")  parts.push("+4 damage");
    if (base === "defend")  parts.push("+2 prevent");
    if (base === "recover") parts.push("+2 heal");
  }
  if (has("shielding") && base === "defend") parts.push("Nullifies stuns and harmful conditions on you");
  if (has("stunning"))   parts.push("Chance to stun");
  if (has("brutal"))     parts.push("May inflict Injured");
  if (has("tainted"))    parts.push("May inflict Lattice Sickness");
  if (has("glowing"))    parts.push("May inflict Irradiated");
  if (has("venomous"))   parts.push("May inflict Poisoned");
  if (has("telegraphed")) parts.push("Heavy windup");
  if (has("exhausting"))  parts.push("Self-stun next slot");
  if (has("riposte"))     parts.push("Counters incoming attacks");
  if (has("provoking"))   parts.push("Target Berzerks (its move pool narrows next turn)");
  if (has("terrifying"))  parts.push("Target Fears (its move pool narrows next turn)");

  if (has("power")) parts.push("Once per turn");
  if (has("slow"))  parts.push("Once every other turn");

  return parts.join(". ");
}

function slotIcon(move: string | null, isMonster: boolean, stunned: boolean): string | null {
  if (stunned) return STUN_ICON;
  if (move == null) return null;
  const base = moveBase(move);
  if (base === "attack" && isMonster) {
    const tokens = move.split(" ").map(t => t.toLowerCase());
    if (tokens.includes("telegraphed")) return TELEGRAPHED_ATTACK_ICON;
  }
  return BASE_ICON[base];
}

/** Per-slot resolution outcome — what icon the third (rightmost) circle shows. */
type Outcome = "stun" | "clash" | "block" | "recover" | "read" | null;

const OUTCOME_ICON: Record<NonNullable<Outcome>, string> = {
  stun:    "knockout.svg",
  clash:   "crossed-swords.svg",
  block:   "dodge.svg",
  recover: "heart-plus.svg",
  read:    "one-eyed.svg",
};

function deriveOutcome(playerMove: string | null, monsterMove: string | null): Outcome {
  if (!playerMove || !monsterMove) return null;
  const pBase = moveBase(playerMove);
  const mBase = moveBase(monsterMove);
  if (pBase === "skipped" && mBase === "skipped") return null;

  const pTokens = playerMove.split(" ").map(t => t.toLowerCase());
  const mTokens = monsterMove.split(" ").map(t => t.toLowerCase());
  const pShielded = pBase === "defend" && pTokens.includes("shielding");
  const mShielded = mBase === "defend" && mTokens.includes("shielding");

  if (pBase === "attack" && mBase === "recover" && !mShielded) return "stun";
  if (mBase === "attack" && pBase === "recover" && !pShielded) return "stun";
  if (pBase === "attack" && mBase === "attack")  return "clash";
  if (pBase === "attack" && mBase === "defend")  return "block";
  if (mBase === "attack" && pBase === "defend")  return "block";
  if (pBase === "read"   || mBase === "read")    return "read";
  if (pBase === "recover" || mBase === "recover") return "recover";
  return null;
}

/**
 * Walk slots 0–2, applying the deterministic forward-rider rules from
 * Resolver.cs StunsTarget(): attack-vs-recover always stuns the recoverer
 * next slot, and `exhausting` attacks self-stun. `shielding` Defend
 * nullifies incoming stun. Probabilistic stuns (stunning mutator) are
 * intentionally NOT predicted — surfacing a maybe-stun would mislead.
 */
type SlotSim = {
  playerMove: string | null;
  monsterMove: string | null;
  playerStunned: boolean;
  monsterStunned: boolean;
  outcome: Outcome;
};

function simulateSlots(
  combat: CombatInfo,
  selections: (string | null)[],
  monsterMovesPerSlot: (string | null)[],
): SlotSim[] {
  const sims: SlotSim[] = [];
  let playerForwardStun = false;
  let monsterForwardStun = false;

  for (let i = 0; i < 3; i++) {
    const playerStunned = combat.playerCarryStun[i] || playerForwardStun;
    const monsterRaw = monsterMovesPerSlot[i];
    const monsterPreSkipped = monsterRaw != null && moveBase(monsterRaw) === "skipped";
    const monsterStunned = monsterForwardStun || monsterPreSkipped;

    const playerMove = playerStunned ? "Skipped" : selections[i];
    const monsterMove = monsterStunned ? "Skipped" : monsterRaw;

    playerForwardStun = false;
    monsterForwardStun = false;

    if (playerMove && monsterMove) {
      const pBase = moveBase(playerMove);
      const mBase = moveBase(monsterMove);
      const pTokens = playerMove.split(" ").map(t => t.toLowerCase());
      const mTokens = monsterMove.split(" ").map(t => t.toLowerCase());
      const pShielded = pBase === "defend" && pTokens.includes("shielding");
      const mShielded = mBase === "defend" && mTokens.includes("shielding");

      if (pBase === "attack" && mBase === "recover" && !mShielded) monsterForwardStun = true;
      else if (mBase === "attack" && pBase === "recover" && !pShielded) playerForwardStun = true;

      if (pBase === "attack" && pTokens.includes("exhausting")) playerForwardStun = true;
      if (mBase === "attack" && mTokens.includes("exhausting")) monsterForwardStun = true;
    }

    sims.push({
      playerMove,
      monsterMove,
      playerStunned,
      monsterStunned,
      outcome: deriveOutcome(playerMove, monsterMove),
    });
  }
  return sims;
}

function moveAvailability(move: string, combat: CombatInfo, selections: (string | null)[]): { disabled: boolean; reason: string | null } {
  const tokens = move.split(" ").map(s => s.toLowerCase());
  const isOncePer = tokens.includes("power");
  const isOnceEvery = tokens.includes("slow");
  if (isOncePer && selections.includes(move)) return { disabled: true, reason: "used this turn" };
  if (isOnceEvery) {
    const last = combat.playerLastUsedTurn[move];
    if (last !== undefined && combat.turn - last < 2) return { disabled: true, reason: "cooling down" };
  }
  return { disabled: false, reason: null };
}

// ── Design tokens (from Figma export) ─────────────────────────────────────
const DIM = "#ACA377";
const ACTION = "#D0925D";
const PROMPT_YELLOW = "#D0BD62";
const BTN_BG = "rgba(13, 13, 13, 0.8)";
const BTN_BG_DISABLED = "#292929";

// Roman numerals shown in empty player slot circles.
const SLOT_NUMERALS = ["I", "II", "III"];

// Match a full damage clause: "you took 5 damage" / "Tob Ashford took 0 damage".
// Subject is "you/You" or a Capitalized name (one or more capitalized words).
const DAMAGE_CLAUSE = /(?:[Yy]ou|[A-Z][\w'-]+(?:\s+[A-Z][\w'-]+)*)\s+(?:took|takes|take)\s+\d+\s+damage/;
const DAMAGE_CLAUSE_G = new RegExp(DAMAGE_CLAUSE.source, "g");
const ZERO_DAMAGE_CLAUSE = new RegExp(`^(?:${DAMAGE_CLAUSE.source.replace("\\d+", "0")})\\.?$`);
const DAMAGE_RED = "#FF6B6B";

/**
 * Drop "X took 0 damage" clauses (confusing — players read 0 as "I prevented
 * damage", not "no attack happened") and color real damage clauses red.
 *
 * Strategy: split into sentences (by ". "), then each sentence into clauses
 * (by ", "), filter out 0-damage clauses, rejoin. Far more robust than
 * regex substitution on the raw string when 0-damage clauses can appear at
 * the start, middle, or end of a sentence.
 */
function renderHead(text: string): ReactNode[] {
  const sentences = text.split(/(?<=\.)\s+/);
  const out: string[] = [];
  for (const sent of sentences) {
    const trimmed = sent.replace(/\.\s*$/, "").trim();
    if (!trimmed) continue;
    const kept = trimmed
      .split(/,\s+/)
      .map(c => c.trim())
      .filter(c => c && !ZERO_DAMAGE_CLAUSE.test(c));
    if (kept.length === 0) continue;
    let joined = kept.join(", ");
    // Capitalize a leading lowercase letter (e.g., "you" promoted to start of sentence)
    if (/^[a-z]/.test(joined)) joined = joined[0].toUpperCase() + joined.slice(1);
    out.push(joined + ".");
  }
  const cleaned = out.join(" ");

  // Color "X took N damage" red.
  const parts: ReactNode[] = [];
  let lastIndex = 0;
  let key = 0;
  let match: RegExpExecArray | null;
  DAMAGE_CLAUSE_G.lastIndex = 0;
  while ((match = DAMAGE_CLAUSE_G.exec(cleaned)) !== null) {
    if (match.index > lastIndex) parts.push(cleaned.slice(lastIndex, match.index));
    parts.push(<span key={key++} style={{ color: DAMAGE_RED }}>{match[0]}</span>);
    lastIndex = match.index + match[0].length;
  }
  if (lastIndex < cleaned.length) parts.push(cleaned.slice(lastIndex));
  return parts;
}

// Group the cumulative event log into completed-turn chunks. Each turn is
// a run of slot events 1/2/3 (slot field is 1-based, per CombatLogEntry)
// plus any non-slot trailing events that follow it. Turn divider entries
// ("— Turn N —") are dropped, and pre-combat intro events (non-slot events
// that arrive before the first slot event) are ignored — the encounter's
// `introText` is rendered separately above the card stream.
type TurnGroup = { slots: CombatLogEntry[]; trailing: CombatLogEntry[] };

function groupTurns(events: CombatLogEntry[]): TurnGroup[] {
  const turns: TurnGroup[] = [];
  let cur: TurnGroup | null = null;
  for (const e of events) {
    if (e.text.startsWith("— Turn")) continue;
    if (e.slot != null) {
      if (cur == null || e.slot === 1) {
        if (cur) turns.push(cur);
        cur = { slots: [], trailing: [] };
      }
      cur.slots.push(e);
    } else if (cur != null) {
      // Trailing non-slot events attach to the just-completed turn.
      cur.trailing.push(e);
    }
    // Pre-slot non-slot events: ignored (covered by combat.introText).
  }
  if (cur) turns.push(cur);
  return turns;
}

/**
 * Combat screen — three-slot RPS, worksheet-stream layout.
 *
 *   ┌─ vignette + monster ─┬─ worksheet stream ─┐
 *
 * Each turn is a card. Prior turns scroll upward at opacity 0.8; the bottom
 * card is the active worksheet (input → playback → prior). When combat
 * resolves, the active card is replaced by an outcome card with Continue.
 *
 * Vitals (spirits/health) are inline in the prompt text so they're right
 * where the player's eye is when planning the next turn. A persistent yellow
 * banner with duplicated stats + flee button is planned for a later pass.
 */
export default function Combat({ state }: { state: GameResponse }) {
  const { doCombatAction, refreshState, loading } = useGame();
  const { combat } = state;
  const logRef = useRef<HTMLDivElement>(null);
  const hitboxRef = useRef<HTMLDivElement>(null);

  const [allEvents, setAllEvents] = useState<CombatLogEntry[]>([]);
  const lastSeenRef = useRef<CombatLogEntry[] | null>(null);
  const lastEncounterRef = useRef<string | null>(null);

  const [selections, setSelections] = useState<(string | null)[]>([null, null, null]);

  const [hits, setHits] = useState<Hit[]>([]);
  const hitIdRef = useRef(0);
  const removeHit = (id: number) => setHits(prev => prev.filter(h => h.id !== id));

  // Staged reveal: while playback is non-null, slot events are NOT yet in
  // allEvents — they live in `playback.slotEvents` and stream into the
  // active card one at a time. They flush into allEvents on the tail timer
  // so the prior-turn list only ever shows fully-resolved turns.
  const [playback, setPlayback] = useState<{
    committedSelections: (string | null)[];
    slotEvents: CombatLogEntry[];
    revealedThrough: number;
    trailingEvents: CombatLogEntry[];
  } | null>(null);
  const SLOT_REVEAL_MS = 550;
  const PLAYBACK_TAIL_MS = 350;

  useEffect(() => {
    if (!combat) return;
    if (combat.events === lastSeenRef.current) return;

    const fresh = combat.encounterId !== lastEncounterRef.current;
    if (fresh) {
      setAllEvents(combat.events);
      lastEncounterRef.current = combat.encounterId;
      setSelections([null, null, null]);
      setPlayback(null);
      lastSeenRef.current = combat.events;
      return;
    }

    const slotEvents = combat.events.filter(e => e.slot != null);
    const trailingEvents = combat.events.filter(e => e.slot == null);

    if (slotEvents.length > 0) {
      // Hold the prior log/selections steady; the playback effect drains
      // events into the active card on a timer, then flushes to allEvents.
      setPlayback({
        committedSelections: selections,
        slotEvents,
        revealedThrough: 0,
        trailingEvents,
      });
    } else {
      setAllEvents(prev => [...prev, ...combat.events]);
      setSelections([null, null, null]);

      if (hitboxRef.current) {
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
    }

    lastSeenRef.current = combat.events;
  }, [combat]);

  useEffect(() => {
    if (!playback) return;

    if (playback.revealedThrough < playback.slotEvents.length) {
      const t = setTimeout(() => {
        const idx = playback.revealedThrough;
        const evt = playback.slotEvents[idx];
        const attack = evt.playerAttack;
        if (attack && hitboxRef.current) {
          const rect = hitboxRef.current.getBoundingClientRect();
          const { x, y } = pickAnchor(rect);
          setHits(prev => [...prev, {
            id: ++hitIdRef.current,
            x, y,
            angle: Math.random() * 360,
            splat: 1 + Math.floor(Math.random() * 8),
            miss: attack.outcome === "miss",
          }]);
        }
        setPlayback(p => p ? { ...p, revealedThrough: p.revealedThrough + 1 } : p);
      }, SLOT_REVEAL_MS);
      return () => clearTimeout(t);
    }

    // All slots revealed — hold for a beat so the player can read the
    // descriptions, then flush into allEvents (where they become a prior
    // card) and spawn a fresh active worksheet.
    const t = setTimeout(() => {
      setAllEvents(prev => [...prev, ...playback.slotEvents, ...playback.trailingEvents]);
      setSelections([null, null, null]);
      setPlayback(null);
    }, PLAYBACK_TAIL_MS);
    return () => clearTimeout(t);
  }, [playback]);

  useEffect(() => {
    if (logRef.current) logRef.current.scrollTop = logRef.current.scrollHeight;
  }, [allEvents.length, playback?.revealedThrough, playback != null, combat?.resolved]);

  const handlePick = (move: string) => {
    if (!combat || combat.resolved || loading || playback) return;
    setSelections(prev => {
      if (moveAvailability(move, combat, prev).disabled) return prev;

      const next = [...prev];
      let target = -1;
      for (let i = 0; i < 3; i++) {
        if (combat.playerCarryStun[i]) continue;
        if (next[i] == null) { target = i; break; }
      }
      if (target === -1) return prev;
      next[target] = move;

      const allDone = next.every((s, i) => combat.playerCarryStun[i] || s != null);
      if (allDone) {
        const payload = next.map((s, i) => combat.playerCarryStun[i] ? "Skipped" : (s ?? "Skipped"));
        setTimeout(() => doCombatAction({ action: "commit", slots: payload }), 0);
      }
      return next;
    });
  };

  useEffect(() => {
    if (!combat) return;
    function onKey(e: KeyboardEvent) {
      if (!combat || combat.resolved || playback) return;
      if (e.target instanceof HTMLInputElement || e.target instanceof HTMLTextAreaElement) return;
      if (e.key >= "1" && e.key <= "9") {
        const idx = parseInt(e.key) - 1;
        const pool = combat.playerMovePool;
        if (idx >= pool.length) return;
        const encoding = pool[idx].encoding;
        if (moveAvailability(encoding, combat, selections).disabled) return;
        handlePick(encoding);
      }
    }
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, [combat, selections, playback]);

  const priorTurns = useMemo(() => groupTurns(allEvents), [allEvents]);

  if (!combat) return null;

  const monsterPct = combat.monsterMaxHp > 0
    ? Math.max(0, Math.min(100, (combat.monsterHp / combat.monsterMaxHp) * 100))
    : 0;

  const onFlee = () => {
    if (combat.resolved || loading) return;
    doCombatAction({ action: "flee" });
  };

  const planVisible = combat.plan != null && combat.plan.length === 3;

  return (
    <div className="flex h-screen overflow-hidden bg-page text-primary">
      {/* ─── LEFT: vignette + monster (full height — TopBar lives in the right pane) ─── */}
      <div className="relative flex-1 min-w-[320px] bg-parchment overflow-hidden">
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
                opacity: combat.playerWon && !playback ? 0 : 1,
                transition: "opacity 900ms ease-out",
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

      {/* ─── RIGHT: top bar + worksheet stream (capped to keep cards from sprawling on wide monitors) ─── */}
      <div className="flex-1 min-w-[420px] max-w-[820px] flex flex-col bg-page">
        <TopBar status={state.status} onBack={onFlee}>
          {!combat.resolved && (
            <Button
              variant="secondary"
              size="sm"
              onClick={onFlee}
              disabled={loading}
              title="End the encounter and escape. Burns this turn — the monster's full plan resolves against you while you flee."
            >
              <MaskedIcon icon="cancel.svg" className="w-4 h-4" color="currentColor" />
              Flee
            </Button>
          )}
        </TopBar>
        <div ref={logRef} className="flex-1 overflow-y-auto px-6 py-6 flex flex-col gap-4">
          {/* Journey toll when combat cut travel short (one-shot field) */}
          <TravailStrip travails={state.travel?.travails} />
          <div>
            <h2 className="font-header text-[32px] text-accent leading-none">{combat.title}</h2>
            {combat.introText && (
              <div style={{ fontSize: 20, lineHeight: "24px", color: DIM, marginTop: 6 }}>
                {combat.introText}
              </div>
            )}
          </div>

          {priorTurns.map((turn, i) => (
            <div key={i} className="flex flex-col gap-2">
              <PriorTurnCard turn={turn} combat={combat} />
              {turn.trailing.length > 0 && (
                <div className="flex flex-col gap-1 px-2">
                  {turn.trailing.map((e, j) => (
                    <div key={j} style={{ fontSize: 20, lineHeight: "24px", color: DIM, fontStyle: "italic" }}>
                      {e.text}
                    </div>
                  ))}
                </div>
              )}
            </div>
          ))}

          {!combat.resolved && !playback && (
            <ActiveTurnCard
              combat={combat}
              selections={selections}
              planVisible={planVisible}
              onPick={handlePick}
            />
          )}

          {playback && (
            <PlaybackCard
              combat={combat}
              committedSelections={playback.committedSelections}
              slotEvents={playback.slotEvents}
              revealedThrough={playback.revealedThrough}
            />
          )}

          {combat.resolved && !playback && (
            <OutcomeCard
              combat={combat}
              onContinue={() => combat.playerLost
                ? doCombatAction({ action: "continue" })  // chain into the rescue
                : refreshState()}
              disabled={loading}
            />
          )}
        </div>
      </div>
    </div>
  );
}

// ── Worksheet primitives ─────────────────────────────────────────────────
// All three card variants share the same shell and the same per-slot row
// (monster + player + outcome circles). Only the right side differs:
//   - PriorTurn:   description text per row
//   - ActiveTurn:  action buttons in a column
//   - Playback:    description text per row, revealed progressively

function CardShell({ children, dim, greeting, status }: {
  children: ReactNode;
  /** Prior-turn cards render at 0.8 opacity with a thinner border. */
  dim?: boolean;
  /** First line — bold yellow ("What's the plan, merchant?"). */
  greeting?: string;
  /** Second line — regular white (intent + vitals readout). */
  status?: string;
}) {
  return (
    <div
      style={{
        background: "#191919",
        opacity: dim ? 0.8 : 1,
        border: `${dim ? 1 : 2}px solid ${DIM}`,
        borderRadius: 8,
        boxShadow: "0 6px 8px rgba(0,0,0,0.5)",
        padding: 16,
        display: "flex",
        flexDirection: "column",
        gap: 12,
      }}
    >
      {(greeting || status) && (
        <div className="flex flex-col" style={{ gap: 2 }}>
          {greeting && (
            <div
              style={{
                fontSize: 20,
                lineHeight: "24px",
                color: PROMPT_YELLOW,
                fontWeight: 700,
              }}
            >
              {greeting}
            </div>
          )}
          {status && (
            <div style={{ fontSize: 20, lineHeight: "24px", color: "#F3F3F3" }}>
              {status}
            </div>
          )}
        </div>
      )}
      {children}
    </div>
  );
}

/**
 * Per-slot row: [monster] + [player] = [outcome], optionally followed by a
 * description on the right. Width-flexible — the circle group is fixed at
 * 240 px and the description / action button sits in flex-1.
 */
function SlotRow({
  index,
  monsterIcon,
  monsterUnknown,
  monsterTooltip,
  playerIcon,
  playerEmpty,
  playerStunned,
  playerTooltip,
  outcome,
  outcomeUnknown,
  description,
  borderBottom,
  rightSlot,
}: {
  index: number;
  monsterIcon: string | null;
  monsterUnknown: boolean;
  monsterTooltip: string;
  playerIcon: string | null;
  /** True when the player has not selected for this slot — shows a faded roman numeral. */
  playerEmpty: boolean;
  playerStunned: boolean;
  playerTooltip: string;
  outcome: Outcome;
  outcomeUnknown: boolean;
  description?: { head: ReactNode; narration?: string };
  borderBottom?: boolean;
  /** Override the right-of-row content (used by ActiveTurnCard for a single shared button column). */
  rightSlot?: ReactNode;
}) {
  return (
    <div
      className="flex items-center"
      style={{
        gap: 16,
        paddingBottom: borderBottom ? 8 : 0,
        paddingTop: index === 0 ? 0 : 8,
        borderBottom: borderBottom ? `1px solid ${DIM}` : undefined,
      }}
    >
      <div
        className="relative shrink-0"
        style={{ width: 240, height: 60 }}
      >
        <Circle
          left={0}
          icon={monsterIcon}
          unknown={monsterUnknown}
          tooltip={monsterTooltip}
        />
        <Symbol left={67} char="+" />
        <Circle
          left={90}
          icon={playerIcon}
          stunned={playerStunned}
          numeral={playerEmpty ? SLOT_NUMERALS[index] : undefined}
          faded={playerEmpty}
          tooltip={playerTooltip}
        />
        <Symbol left={158} char="=" />
        <Circle
          left={180}
          icon={outcomeUnknown ? null : outcome ? OUTCOME_ICON[outcome] : null}
          dot={!outcomeUnknown && outcome == null}
          unknown={outcomeUnknown}
          faded={outcomeUnknown}
          tooltip={
            outcomeUnknown
              ? "Outcome unknown"
              : outcome === "stun"    ? "Stun"
              : outcome === "clash"   ? "Clash"
              : outcome === "block"   ? "Blocked"
              : outcome === "recover" ? "Recover"
              : outcome === "read"    ? "Read"
              : "Resolves"
          }
        />
      </div>

      {rightSlot != null ? (
        rightSlot
      ) : description ? (
        <div className="flex-1 min-w-0" style={{ fontSize: 20, lineHeight: "24px", color: "#F3F3F3" }}>
          {description.head}
          {description.narration && (
            <>
              {" "}
              <span style={{ color: DIM, fontStyle: "italic" }}>
                {description.narration}
              </span>
            </>
          )}
        </div>
      ) : (
        <div className="flex-1" />
      )}
    </div>
  );
}

function Circle({
  left,
  icon,
  unknown,
  faded,
  stunned,
  numeral,
  dot,
  tooltip,
}: {
  left: number;
  icon: string | null;
  unknown?: boolean;
  faded?: boolean;
  stunned?: boolean;
  numeral?: string;
  dot?: boolean;
  tooltip?: string;
}) {
  // Stun overrides everything (forced Skipped slot — show knockout icon).
  const showIcon = stunned ? STUN_ICON : icon;
  return (
    <div
      title={tooltip}
      className="absolute flex items-center justify-center"
      style={{
        left,
        top: 0,
        width: 60,
        height: 60,
        boxSizing: "border-box",
        border: `2px solid ${DIM}`,
        borderRadius: "50%",
        background: "#191919",
        opacity: faded ? 0.5 : 1,
      }}
    >
      {unknown && !showIcon ? (
        <span style={{ fontSize: 30, lineHeight: 1, color: "#F3F3F3" }}>?</span>
      ) : numeral ? (
        <span
          style={{
            fontSize: 32,
            lineHeight: 1,
            color: "#F3F3F3",
            fontWeight: 800,
          }}
        >
          {numeral}
        </span>
      ) : showIcon ? (
        <MaskedIcon icon={showIcon} color="#F3F3F3" className="w-[24px] h-[24px]" />
      ) : dot ? (
        <span
          style={{
            display: "block",
            width: 12,
            height: 12,
            borderRadius: "50%",
            background: DIM,
          }}
        />
      ) : null}
    </div>
  );
}

function Symbol({ left, char }: { left: number; char: string }) {
  return (
    <span
      style={{
        position: "absolute",
        left,
        top: 10,
        width: 16,
        height: 38,
        fontSize: 32,
        lineHeight: "38px",
        fontWeight: 700,
        color: PROMPT_YELLOW,
        textAlign: "center",
        pointerEvents: "none",
      }}
    >
      {char}
    </span>
  );
}

// ── Prior-turn card ───────────────────────────────────────────────────────
// Three resolved rows + per-row description text. Faded (opacity 0.8).
// Trailing non-slot events (condition ticks, stray narration) appended below.

function PriorTurnCard({ turn, combat }: { turn: TurnGroup; combat: CombatInfo }) {
  // Pad partial turns (unlikely outside of mid-flush races) so we always
  // render 3 rows — empty rows render as "?" everywhere.
  const slots = [...turn.slots];
  while (slots.length < 3) slots.push({ text: "" } as CombatLogEntry);

  return (
    <CardShell dim>
      <div className="flex flex-col">
        {slots.map((evt, i) => {
          const playerMove = evt.playerMove ?? null;
          const monsterMove = evt.monsterMove ?? null;
          const playerStunned = playerMove === "Skipped";
          const monsterStunned = monsterMove === "Skipped";
          const monsterUnknown = monsterMove == null && !monsterStunned;
          const outcome = deriveOutcome(playerMove, monsterMove);
          const outcomeUnknown = monsterMove == null;

          const [headText, narration] = (evt.text ?? "").split("\n    ");

          return (
            <SlotRow
              key={i}
              index={i}
              monsterIcon={monsterUnknown ? null : slotIcon(monsterMove, true, monsterStunned)}
              monsterUnknown={monsterUnknown}
              monsterTooltip={
                monsterUnknown
                  ? "Slot intent unknown"
                  : monsterStunned
                    ? "Stunned"
                    : monsterMove ?? ""
              }
              playerIcon={slotIcon(playerMove, false, playerStunned)}
              playerEmpty={playerMove == null && !playerStunned}
              playerStunned={playerStunned}
              playerTooltip={
                playerStunned
                  ? "Stunned"
                  : playerMove
                    ? lookupDisplayName(playerMove, combat)
                    : ""
              }
              outcome={outcome}
              outcomeUnknown={outcomeUnknown}
              description={headText ? { head: renderHead(headText), narration } : undefined}
              borderBottom={i < 2}
            />
          );
        })}
      </div>
    </CardShell>
  );
}

// ── Active-turn card (input phase) ───────────────────────────────────────
// Yellow prompt with vitals inline + 3 empty rows + ActionButton column on
// the right. All buttons share the column — they don't align row-by-row.

function ActiveTurnCard({
  combat,
  selections,
  planVisible,
  onPick,
}: {
  combat: CombatInfo;
  selections: (string | null)[];
  planVisible: boolean;
  onPick: (encoding: string) => void;
}) {
  const monsterMovesPerSlot = useMemo<(string | null)[]>(
    () => (planVisible && combat.plan ? combat.plan : [null, null, null]),
    [combat.plan, planVisible],
  );

  const sims = useMemo(
    () => simulateSlots(combat, selections, monsterMovesPerSlot),
    [combat, selections, monsterMovesPerSlot],
  );

  // Stable per turn (and per-encounter) so the greeting doesn't flicker, but rotates.
  const greeting = useMemo(
    () => GREETINGS[Math.floor(Math.random() * GREETINGS.length)],
    [combat.turn, combat.encounterId],
  );

  return (
    <CardShell greeting={greeting} status={buildStatus(combat)}>
      <div className="flex items-start" style={{ gap: 18 }}>
        <div className="flex flex-col shrink-0" style={{ gap: 8 }}>
          {sims.map((sim, i) => {
            const monsterUnknown = monsterMovesPerSlot[i] == null && !sim.monsterStunned;
            const playerEmpty = sim.playerMove == null && !sim.playerStunned;

            return (
              <SlotRow
                key={i}
                index={i}
                monsterIcon={
                  monsterUnknown ? null : slotIcon(sim.monsterMove, true, sim.monsterStunned)
                }
                monsterUnknown={monsterUnknown}
                monsterTooltip={
                  monsterUnknown
                    ? "Unknown — commit Read to reveal"
                    : sim.monsterStunned
                      ? "Stunned"
                      : sim.monsterMove ?? ""
                }
                playerIcon={slotIcon(sim.playerMove, false, sim.playerStunned)}
                playerEmpty={playerEmpty}
                playerStunned={sim.playerStunned}
                playerTooltip={
                  sim.playerStunned
                    ? "Stunned"
                    : sim.playerMove
                      ? lookupDisplayName(sim.playerMove, combat)
                      : `Slot ${SLOT_NUMERALS[i]} — pending`
                }
                outcome={sim.outcome}
                outcomeUnknown={monsterUnknown || playerEmpty}
                rightSlot={<></>}
              />
            );
          })}
        </div>

        <div className="flex-1 flex flex-col" style={{ gap: 10, minWidth: 0 }}>
          <div
            className="grid"
            style={{ gridTemplateColumns: "1fr 1fr", gap: 10 }}
          >
            {combat.playerMovePool.map((option, idx) => {
              const { disabled, reason } = moveAvailability(option.encoding, combat, selections);
              const tooltip = disabled
                ? `${moveTooltip(option.encoding)} (${reason})`
                : moveTooltip(option.encoding);
              return (
                <ActionButton
                  key={option.encoding}
                  number={idx + 1}
                  label={option.displayName}
                  encoding={option.encoding}
                  tooltip={tooltip}
                  disabled={disabled}
                  onClick={() => onPick(option.encoding)}
                />
              );
            })}
          </div>
        </div>
      </div>
    </CardShell>
  );
}

// ── Playback card (animation phase) ───────────────────────────────────────
// Same card the player just committed into, but with each slot revealing
// monster intent + outcome + description text on a 1.4 s timer. No buttons.

function PlaybackCard({
  combat,
  committedSelections,
  slotEvents,
  revealedThrough,
}: {
  combat: CombatInfo;
  committedSelections: (string | null)[];
  slotEvents: CombatLogEntry[];
  revealedThrough: number;
}) {
  return (
    <CardShell greeting="Resolving…">
      <div className="flex flex-col">
        {[0, 1, 2].map(i => {
          const evt = slotEvents[i];
          const revealed = i < revealedThrough;

          // Player's committed move is known from the start (we sent it).
          // Monster move + outcome only appear once that slot has been revealed.
          const playerMove = evt?.playerMove ?? committedSelections[i] ?? null;
          const monsterMove = revealed ? (evt?.monsterMove ?? null) : null;
          const playerStunned = playerMove === "Skipped";
          const monsterStunned = revealed && monsterMove === "Skipped";
          const monsterUnknown = !revealed;
          const outcomeUnknown = !revealed;
          const outcome = revealed ? deriveOutcome(playerMove, monsterMove) : null;

          const [headText, narration] = revealed && evt?.text
            ? evt.text.split("\n    ")
            : [undefined, undefined];

          return (
            <SlotRow
              key={i}
              index={i}
              monsterIcon={monsterUnknown ? null : slotIcon(monsterMove, true, monsterStunned)}
              monsterUnknown={monsterUnknown}
              monsterTooltip={monsterUnknown ? "Resolving…" : monsterMove ?? ""}
              playerIcon={slotIcon(playerMove, false, playerStunned)}
              playerEmpty={playerMove == null && !playerStunned}
              playerStunned={playerStunned}
              playerTooltip={
                playerStunned
                  ? "Stunned"
                  : playerMove
                    ? lookupDisplayName(playerMove, combat)
                    : ""
              }
              outcome={outcome}
              outcomeUnknown={outcomeUnknown}
              description={headText ? { head: renderHead(headText), narration } : undefined}
              borderBottom={i < 2}
            />
          );
        })}
      </div>
    </CardShell>
  );
}

// ── Outcome card ──────────────────────────────────────────────────────────
// Replaces the active worksheet when combat resolves. Continue button takes
// the action-buttons spot.

function OutcomeCard({
  combat,
  onContinue,
  disabled,
}: {
  combat: CombatInfo;
  onContinue: () => void;
  disabled: boolean;
}) {
  const verdict = combat.playerWon ? "Victory"
    : combat.playerLost ? "Defeat"
    : combat.playerFled ? "Escaped"
    : combat.monsterFled ? "It Flees"
    : "Resolved";

  const paragraphs = (combat.outcomeText ?? "")
    .split(/\n{2,}/)
    .map(p => p.replace(/\s*\n\s*/g, " ").trim())
    .filter(p => p.length > 0);

  return (
    <CardShell>
      <div className="font-header text-[32px] text-accent leading-none">{verdict}</div>
      {paragraphs.length > 0 && (
        <div className="flex flex-col" style={{ gap: 8, fontSize: 20, lineHeight: "24px", color: "#F3F3F3" }}>
          {paragraphs.map((p, i) => <p key={i}>{p}</p>)}
        </div>
      )}
      {combat.outcomeMechanics && combat.outcomeMechanics.length > 0 && (
        <ul className="flex flex-col" style={{ gap: 4, fontSize: 20, lineHeight: "24px", color: DIM }}>
          {combat.outcomeMechanics.map((m, i) => (
            <li key={i}>{m.description}</li>
          ))}
        </ul>
      )}
      <ContinueButton onClick={onContinue} disabled={disabled} />
    </CardShell>
  );
}

// ── Prompt assembly ───────────────────────────────────────────────────────
// Two-line prompt. First line bold yellow, second line regular white.
// Greeting rotates per turn so every fight doesn't open with the same line.

const GREETINGS = [
  "What's the plan, merchant?",
  "Eyes up.",
  "Make it count.",
  "What's it gonna be?",
  "Steady now.",
  "Pick your moves.",
  "Your call.",
];

function buildStatus(combat: CombatInfo): string {
  const tell = combat.tell?.trim() ?? "";
  const tellPart = tell ? (tell.endsWith(".") ? tell : tell + ".") : "";
  const stats = `You have ${combat.playerSpirits} spirits and ${combat.playerHealth} health left.`;
  return [tellPart, stats].filter(Boolean).join(" ");
}

// ── Buttons ───────────────────────────────────────────────────────────────

function ActionButton({ number, label, encoding, tooltip, disabled, onClick }: {
  number: number; label: string; encoding: string; tooltip?: string; disabled?: boolean; onClick: () => void;
}) {
  const classIcon = BASE_ICON[moveBase(encoding)];
  return (
    <button
      onClick={onClick}
      disabled={disabled}
      title={tooltip}
      style={{
        height: 48,
        padding: "12px 16px",
        gap: 10,
        background: disabled ? BTN_BG_DISABLED : BTN_BG,
        borderRadius: 8,
        display: "flex",
        flexDirection: "row",
        alignItems: "center",
        textAlign: "left",
        boxSizing: "border-box",
        width: "100%",
      }}
      className={disabled ? "cursor-not-allowed" : "hover:brightness-125"}
    >
      {classIcon && (
        <span style={{ opacity: disabled ? 0.4 : 1, display: "inline-flex", flex: "0 0 auto" }}>
          <MaskedIcon icon={classIcon} color={ACTION} className="w-[24px] h-[24px]" />
        </span>
      )}
      <span
        style={{
          flex: 1,
          color: ACTION,
          fontSize: 20,
          lineHeight: "24px",
          fontFamily: "var(--font-body)",
          opacity: disabled ? 0.4 : 1,
          whiteSpace: "nowrap",
          overflow: "hidden",
          textOverflow: "ellipsis",
        }}
      >
        {label}
      </span>
      <span
        style={{
          width: 24,
          height: 24,
          border: `1px solid ${ACTION}`,
          borderRadius: 6,
          color: ACTION,
          fontSize: 20,
          lineHeight: "21px",
          fontFamily: "var(--font-body)",
          display: "inline-flex",
          alignItems: "center",
          justifyContent: "center",
          flex: "0 0 auto",
          opacity: disabled ? 0.4 : 1,
          boxSizing: "border-box",
        }}
      >
        {number}
      </span>
    </button>
  );
}

function ContinueButton({ onClick, disabled }: { onClick: () => void; disabled: boolean }) {
  // Sized to its label per the house rule (no full-width buttons). Aligned to
  // the right edge of the OutcomeCard.
  return (
    <div className="flex justify-end" style={{ marginTop: 4 }}>
      <Button onClick={onClick} disabled={disabled}>
        Continue
      </Button>
    </div>
  );
}
