import { useEffect, useRef, useState } from "react";
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

// ── Move helpers ───────────────────────────────────────────────────────────

type MoveBase = "attack" | "defend" | "recover" | "read" | "skipped";

function moveBase(encoded: string): MoveBase {
  const last = encoded.split(" ").pop()?.toLowerCase() ?? "";
  if (last === "attack" || last === "defend" || last === "recover" || last === "read" || last === "skipped") {
    return last;
  }
  return "skipped";
}

const COOLDOWN_MUTATORS = new Set(["mythic", "rare", "slow", "power"]);

const DISPLAY_OVERRIDES: Record<string, string> = {
  Defend: "Block",
  Recover: "Recover",
  Read: "Read intent",
  "Big Defend": "Brace",
  "Big Shielding Defend": "Shield wall",
  "Big Rare Shielding Defend": "Shield wall",
  "Big Recover": "Super recover",
  "Big Mythic Recover": "Super recover",
  "Big Rare Recover": "Super recover",
  "Big Rare Defend": "Brace",
};

function moveDisplayName(encoded: string): string {
  if (DISPLAY_OVERRIDES[encoded]) return DISPLAY_OVERRIDES[encoded];
  const tokens = encoded.split(" ").filter(t => !COOLDOWN_MUTATORS.has(t.toLowerCase()));
  if (tokens.length === 0) return encoded;
  const joined = tokens.join(" ");
  return joined.charAt(0).toUpperCase() + joined.slice(1).toLowerCase();
}

// Per Figma: Attack → knockout, Recover → nested-hearts, Defend → checked-shield,
// Read → one-eyed (no Figma example, kept from prior pass).
const BASE_ICON: Record<MoveBase, string | null> = {
  attack: "knockout.svg",
  defend: "checked-shield.svg",
  recover: "nested-hearts.svg",
  read: "one-eyed.svg",
  skipped: null,
};

function moveAvailability(move: string, combat: CombatInfo, selections: (string | null)[]): { disabled: boolean; reason: string | null } {
  const tokens = move.split(" ").map(s => s.toLowerCase());
  const isOncePer = tokens.includes("rare") || tokens.includes("power");
  const isOnceEvery = tokens.includes("mythic") || tokens.includes("slow");
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
const SPLAT_RED = "#AC0000";
const BTN_BG = "rgba(13, 13, 13, 0.8)";
const BTN_BG_DISABLED = "#292929";

/**
 * Combat screen — three-slot RPS.
 *
 *   ┌─ vignette + monster ─┬─ controls (280 px) ─┬─ log ─┐
 *
 * Each turn the player picks an action for slots 1/2/3; selections fill
 * left-to-right and auto-commit when the third lands. Number keys 1–9 map to
 * the action menu. Cooldowns and once-per-turn moves are shown disabled, not
 * hidden. The slot grid mirrors the AI's per-slot intent (revealed when the
 * player committed Read on the prior turn).
 *
 * Layout dimensions traced from Figma export at assets/UI/svg_export.txt and
 * css_export.txt — slot circles 60 px @ 119 px center spacing, action rows
 * 248×48 with rgba(13,13,13,.8) fill, etc.
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

  const [healthSplat, setHealthSplat] = useState(0);
  const [spiritsSplat, setSpiritsSplat] = useState(0);
  const prevHealthRef = useRef(combat?.playerHealth ?? 0);
  const prevSpiritsRef = useRef(combat?.playerSpirits ?? 0);

  useEffect(() => {
    if (!combat) return;
    if (combat.events === lastSeenRef.current) return;

    const fresh = combat.encounterId !== lastEncounterRef.current;
    if (fresh) {
      setAllEvents(combat.events);
      lastEncounterRef.current = combat.encounterId;
      setSelections([null, null, null]);
      prevHealthRef.current = combat.playerHealth;
      prevSpiritsRef.current = combat.playerSpirits;
    } else {
      setAllEvents(prev => [...prev, ...combat.events]);
      setSelections([null, null, null]);
    }
    lastSeenRef.current = combat.events;

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
    if (!combat) return;
    if (combat.playerHealth < prevHealthRef.current) setHealthSplat(s => s + 1);
    prevHealthRef.current = combat.playerHealth;
  }, [combat?.playerHealth]);

  useEffect(() => {
    if (!combat) return;
    if (combat.playerSpirits < prevSpiritsRef.current) setSpiritsSplat(s => s + 1);
    prevSpiritsRef.current = combat.playerSpirits;
  }, [combat?.playerSpirits]);

  useEffect(() => {
    if (logRef.current) logRef.current.scrollTop = logRef.current.scrollHeight;
  }, [allEvents.length]);

  const handlePick = (move: string) => {
    if (!combat || combat.resolved || loading) return;
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
      if (!combat || combat.resolved) return;
      if (e.target instanceof HTMLInputElement || e.target instanceof HTMLTextAreaElement) return;
      if (e.key >= "1" && e.key <= "9") {
        const idx = parseInt(e.key) - 1;
        const pool = combat.playerMovePool;
        if (idx >= pool.length) return;
        const move = pool[idx];
        if (moveAvailability(move, combat, selections).disabled) return;
        handlePick(move);
      }
    }
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, [combat, selections]);

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
      {/* ─── LEFT: vignette + monster ─── */}
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

      {/* ─── CENTER: combat controls (Figma: 280 px, padding 16, gap 10, bg #191919) ─── */}
      <div
        className="shrink-0 flex flex-col"
        style={{
          width: 280,
          padding: 16,
          gap: 10,
          background: "#191919",
          boxShadow: "0 4px 4px 8px rgba(0,0,0,0.25)",
        }}
      >
        <Vitals
          spirits={combat.playerSpirits}
          maxSpirits={combat.playerMaxSpirits}
          health={combat.playerHealth}
          maxHealth={combat.playerMaxHealth}
          spiritsSplatKey={spiritsSplat}
          healthSplatKey={healthSplat}
        />

        <div style={{ flex: 1 }} />

        {!combat.resolved && (
          <>
            <BannerLine text={combat.tell} />

            <SlotGrid combat={combat} selections={selections} planVisible={planVisible} />

            <BannerLine text="Your plan" />

            {combat.playerMovePool.map((move, idx) => {
              const { disabled } = moveAvailability(move, combat, selections);
              return (
                <ActionButton
                  key={move}
                  number={idx + 1}
                  label={moveDisplayName(move)}
                  disabled={disabled}
                  onClick={() => handlePick(move)}
                />
              );
            })}
          </>
        )}

        <div style={{ flex: 1 }} />

        {!combat.resolved && (
          <FleeButton onClick={onFlee} disabled={loading} />
        )}
        {combat.resolved && (
          <Button size="lg" onClick={() => refreshState()} disabled={loading} className="w-full">
            Continue
          </Button>
        )}
      </div>

      {/* ─── RIGHT: combat log ─── */}
      <div className="flex-1 min-w-[320px] bg-page flex flex-col">
        <div ref={logRef} className="flex-1 overflow-y-auto px-8 py-6 flex flex-col gap-2">
          <div className="mb-2">
            <h2 className="font-header text-[32px] text-accent leading-none">{combat.title}</h2>
            {combat.introText && <div className="text-dim mt-1.5">{combat.introText}</div>}
          </div>

          {allEvents.map((entry, i) => <LogEntry key={i} entry={entry} />)}

          {combat.resolved && <OutcomePanel combat={combat} />}
        </div>
      </div>
    </div>
  );
}

// ── Vitals ────────────────────────────────────────────────────────────────
// Figma: 248×114 row, padding 0 48px, two 60-wide stat groups via space-between.
// Each stat group is 60×114, contains a 64 px Goudy numeral (#FF6B6B if ≤0
// else #F3F3F3) and a two-line 20 px label (#ACA377). Damage splat is a ~118×
// 112 #AC0000 blob positioned absolutely behind the health value, allowed to
// spill out of the stat group's 60 px width.

function Vitals({ spirits, maxSpirits, health, maxHealth, spiritsSplatKey, healthSplatKey }: {
  spirits: number; maxSpirits: number;
  health: number; maxHealth: number;
  spiritsSplatKey: number;
  healthSplatKey: number;
}) {
  return (
    <div
      className="flex justify-between items-start"
      style={{ height: 114, padding: "0 48px" }}
    >
      <StatGroup value={spirits} max={maxSpirits} label="spirits" damaged={spirits < maxSpirits} splatKey={spiritsSplatKey} showSplat={false} />
      <StatGroup value={health} max={maxHealth} label="health" damaged={health < maxHealth} splatKey={healthSplatKey} showSplat={true} />
    </div>
  );
}

function StatGroup({ value, max, label, damaged, splatKey, showSplat }: {
  value: number;
  max: number;
  label: string;
  damaged: boolean;
  splatKey: number;
  showSplat: boolean;
}) {
  const color = value <= 0 ? "#FF6B6B" : "#F3F3F3";
  return (
    <div
      className="relative flex flex-col items-center justify-center"
      style={{ width: 60, height: 114, gap: 10 }}
    >
      {showSplat && damaged && <DamageSplat key={splatKey} />}
      <span
        className="relative font-header"
        style={{ fontSize: 64, lineHeight: "64px", color, zIndex: 1 }}
      >
        {value}
      </span>
      <div
        className="relative text-center"
        style={{ fontSize: 20, lineHeight: "20px", color: DIM, zIndex: 1 }}
      >
        of {max}<br />{label}
      </div>
    </div>
  );
}

function DamageSplat() {
  // Variant + angle pinned at mount; remount via key change re-rolls them.
  // Sized 118×112 per Figma, centered on the 60-wide stat group with
  // negative offsets so the blob spills out of bounds.
  const [variant] = useState(() => 1 + Math.floor(Math.random() * 8));
  const [angle] = useState(() => Math.random() * 360);
  return (
    <div
      className="absolute pointer-events-none"
      style={{
        width: 118,
        height: 112,
        left: (60 - 118) / 2,
        top: -8,
        backgroundColor: SPLAT_RED,
        maskImage: `url(/world/assets/effects/splat/splat${variant}.svg)`,
        WebkitMaskImage: `url(/world/assets/effects/splat/splat${variant}.svg)`,
        maskSize: "contain",
        WebkitMaskSize: "contain",
        maskRepeat: "no-repeat",
        WebkitMaskRepeat: "no-repeat",
        maskPosition: "center",
        WebkitMaskPosition: "center",
        transform: `rotate(${angle}deg)`,
      }}
    />
  );
}

// ── Banner / "Your plan" header ───────────────────────────────────────────
// Both are 20 px text, color #ACA377, line-height 40 px (= 40 px tall block).

function BannerLine({ text }: { text: string }) {
  return (
    <div
      style={{
        fontSize: 20,
        lineHeight: "40px",
        color: DIM,
        fontStyle: "italic",
      }}
    >
      {text}
    </div>
  );
}

// ── Slot grid ─────────────────────────────────────────────────────────────
// Figma: 248×179, three 60-wide columns via space-between (16 px L/R padding).
// Each column has top circle (cy=30), connector lines at y=60-70 and
// y=109-119, bottom circle (cy=149.5). Border 2 px solid #ACA377, fill #191919.
// Player circle gets opacity 0.5 when no move is selected; the bottom
// connector line fades to 0.5 to match.

function SlotGrid({ combat, selections, planVisible }: {
  combat: CombatInfo;
  selections: (string | null)[];
  planVisible: boolean;
}) {
  return (
    <div
      className="flex justify-between"
      style={{ height: 179, padding: "0 16px" }}
    >
      {[0, 1, 2].map(i => {
        const monsterMove = planVisible ? combat.plan![i] : null;
        const monsterIcon = monsterMove ? BASE_ICON[moveBase(monsterMove)] : null;
        const playerMove = selections[i];
        const playerIcon = playerMove ? BASE_ICON[moveBase(playerMove)] : null;
        const stunned = combat.playerCarryStun[i];
        const playerFaded = playerMove == null && !stunned;
        return (
          <SlotColumn
            key={i}
            monsterIcon={monsterIcon}
            monsterUnknown={!planVisible}
            playerIcon={playerIcon}
            playerFaded={playerFaded}
            stunned={stunned}
          />
        );
      })}
    </div>
  );
}

function SlotColumn({ monsterIcon, monsterUnknown, playerIcon, playerFaded, stunned }: {
  monsterIcon: string | null;
  monsterUnknown: boolean;
  playerIcon: string | null;
  playerFaded: boolean;
  stunned: boolean;
}) {
  return (
    <div
      className="flex flex-col items-center"
      style={{ width: 60, height: 179 }}
    >
      <SlotCircle icon={monsterIcon} unknown={monsterUnknown} />
      <ConnectorLine />
      <div className="flex-1 flex items-center justify-center">
        <PreviewGlyph monsterUnknown={monsterUnknown} playerFilled={!playerFaded} />
      </div>
      <ConnectorLine faded={playerFaded} />
      <SlotCircle icon={playerIcon} faded={playerFaded} stunnedLabel={stunned ? "skip" : null} />
    </div>
  );
}

function SlotCircle({ icon, faded, unknown, stunnedLabel }: {
  icon?: string | null;
  faded?: boolean;
  unknown?: boolean;
  stunnedLabel?: string | null;
}) {
  return (
    <div
      className="flex items-center justify-center"
      style={{
        width: 60,
        height: 60,
        boxSizing: "border-box",
        border: `2px solid ${DIM}`,
        borderRadius: "50%",
        background: "#191919",
        opacity: faded ? 0.5 : 1,
      }}
    >
      {unknown ? (
        <span
          className="font-header"
          style={{ fontSize: 30, lineHeight: 1, color: "#F3F3F3" }}
        >?</span>
      ) : stunnedLabel ? (
        <span style={{ fontSize: 16, color: DIM, lineHeight: 1 }}>{stunnedLabel}</span>
      ) : icon ? (
        <MaskedIcon icon={icon} color="#F3F3F3" className="w-[24px] h-[24px]" />
      ) : null}
    </div>
  );
}

function ConnectorLine({ faded }: { faded?: boolean }) {
  return (
    <span
      style={{
        display: "block",
        width: 2,
        height: 10,
        background: DIM,
        opacity: faded ? 0.5 : 1,
      }}
    />
  );
}

function PreviewGlyph({ monsterUnknown, playerFilled }: {
  monsterUnknown: boolean;
  playerFilled: boolean;
}) {
  if (monsterUnknown) {
    return (
      <span
        className="font-header"
        style={{ fontSize: 30, lineHeight: 1, color: DIM, opacity: playerFilled ? 1 : 0.6 }}
      >?</span>
    );
  }
  // Both visible OR player-empty + monster-visible: show a small dot. Brighter
  // when the slot is locked in, dim when still pending.
  return (
    <span
      style={{
        display: "block",
        width: 12,
        height: 12,
        borderRadius: "50%",
        background: DIM,
        opacity: playerFilled ? 1 : 0.5,
      }}
    />
  );
}

// ── Action / Flee buttons ─────────────────────────────────────────────────
// Figma .numberButton: 248×48, bg rgba(13,13,13,0.8), rounded 8, padding
// 12 16, gap 10. Number badge 24×24 (1 px border #D0925D, rounded 6),
// 20 px digit. Disabled: bg #292929, badge+label opacity 0.4.

function ActionButton({ number, label, disabled, onClick }: {
  number: number; label: string; disabled?: boolean; onClick: () => void;
}) {
  return (
    <button
      onClick={onClick}
      disabled={disabled}
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
      }}
      className={disabled ? "cursor-not-allowed" : "hover:brightness-125"}
    >
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
      <span
        style={{
          color: ACTION,
          fontSize: 20,
          lineHeight: "24px",
          fontFamily: "var(--font-body)",
          opacity: disabled ? 0.4 : 1,
        }}
      >
        {label}
      </span>
    </button>
  );
}

function FleeButton({ onClick, disabled }: { onClick: () => void; disabled: boolean }) {
  return (
    <button
      onClick={onClick}
      disabled={disabled}
      style={{
        height: 48,
        padding: "12px 16px",
        gap: 10,
        background: BTN_BG,
        borderRadius: 8,
        display: "flex",
        flexDirection: "row",
        alignItems: "center",
        textAlign: "left",
        boxSizing: "border-box",
        opacity: disabled ? 0.5 : 1,
      }}
      className={disabled ? "cursor-not-allowed" : "hover:brightness-125"}
    >
      <MaskedIcon icon="cancel.svg" color={ACTION} className="w-[24px] h-[24px]" />
      <span
        style={{
          color: ACTION,
          fontSize: 20,
          lineHeight: "24px",
          fontFamily: "var(--font-body)",
        }}
      >
        Flee
      </span>
    </button>
  );
}

// ── Log entries ───────────────────────────────────────────────────────────

function LogEntry({ entry }: { entry: CombatLogEntry }) {
  const line = entry.text;

  if (line.startsWith("— Turn")) {
    return (
      <div className="flex items-center gap-3 text-muted mt-3">
        <div className="flex-1 h-px bg-white/[0.08]" />
        <span>{line.replace(/^—\s*|\s*—$/g, "").split(/\s+—\s+/)[0]}</span>
        <div className="flex-1 h-px bg-white/[0.08]" />
      </div>
    );
  }

  if (line.startsWith("===")) {
    return <div className="font-header text-accent text-center mt-3">{line.replace(/=/g, "").trim()}</div>;
  }

  if (line.startsWith("  Slot ")) {
    const [head, narration] = line.split("\n    ");
    return (
      <div className="leading-relaxed">
        <div className="font-mono text-primary">{head.replace(/^ {2}/, "")}</div>
        {narration && <div className="text-dim ml-6 italic">{narration}</div>}
      </div>
    );
  }

  return <div className="leading-relaxed text-primary">{line}</div>;
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
