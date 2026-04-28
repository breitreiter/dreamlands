import { useEffect, useRef } from "react";
import { useGame } from "../GameContext";
import type { CombatInfo, GameResponse } from "../api/types";
import { Button } from "@/components/ui/button";
import MaskedIcon from "../components/MaskedIcon";

/**
 * Combat screen.
 *
 * Layout follows project/combat/combat_screen.html — 45/55 split. Left panel
 * is the monster vignette + sprite + name plate. Right panel has a vitals
 * header, the scene title + scrolling turn log, an intent banner, and a
 * weapon-class-aware action row.
 *
 * Dagger uses the timing-window model (no d20). Until the actual minigame
 * widget lands, the dagger control is four debug buttons (Miss/Hit/Crit/Super)
 * so the backend can be exercised end-to-end. The minigame widget replaces
 * the button row in a future polish pass.
 */
export default function Combat({ state }: { state: GameResponse }) {
  const { doCombatAction, refreshState, loading } = useGame();
  const { combat } = state;
  const logRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (logRef.current) logRef.current.scrollTop = logRef.current.scrollHeight;
  }, [combat?.lines.length]);

  if (!combat) return null;

  const monsterPct = combat.monsterMaxHp > 0
    ? Math.max(0, Math.min(100, (combat.monsterHp / combat.monsterMaxHp) * 100))
    : 0;

  const weaponClass = combat.playerWeaponClass.toLowerCase();
  const isDagger = weaponClass === "dagger";
  const isSword = weaponClass === "sword";

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
        <div className="absolute inset-x-0 bottom-0 top-32 flex items-end justify-center">
          {combat.image && (
            <img
              className="w-full h-full object-contain object-bottom"
              style={{
                filter:
                  "drop-shadow(0 0 6px rgba(0,0,0,0.95)) drop-shadow(0 0 18px rgba(0,0,0,0.85)) drop-shadow(0 0 40px rgba(0,0,0,0.65)) drop-shadow(0 12px 24px rgba(0,0,0,0.6))",
              }}
              src={`/world/assets/${combat.image}`}
              alt={combat.title}
              onError={(e) => { (e.target as HTMLImageElement).style.display = "none"; }}
            />
          )}
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

      {/* ─── Right: vitals + log + actions ─── */}
      <div className="flex-1 flex flex-col bg-page min-w-0">

        {/* Header: player vitals + AC + weapon */}
        <div className="px-10 py-5 border-b border-white/5 bg-panel-alt flex items-center gap-7">
          <StatPair color="#d96565" icon="heart-plus.svg" value={combat.playerHealth} max={combat.playerMaxHealth} />
          <StatPair color="#9b75d6" icon="sensuousness.svg" value={combat.playerSpirits} max={combat.playerMaxSpirits} />
          <div className="flex-1" />
          <Pill label="AC" value={combat.playerEffectiveAc.toString()} />
          <Pill label={combat.playerWeaponClass || "Unarmed"} value={isDagger ? "timing" : `+${combat.playerAttackBonus}`} />
        </div>

        {/* Log (scrollable) */}
        <div ref={logRef} className="flex-1 overflow-y-auto px-10 py-6 flex flex-col gap-4">
          <div>
            <h2 className="font-header text-[32px] text-accent leading-none">{combat.title}</h2>
            {combat.introText && <div className="text-dim mt-1.5">{combat.introText}</div>}
          </div>

          {combat.lines.map((line, i) => <LogLine key={i} line={line} />)}

          {combat.resolved && (
            <OutcomePanel combat={combat} />
          )}
        </div>

        {/* Intent preview — load-bearing per design (sword stance, axe block, dagger band choice all key off this) */}
        {!combat.resolved && combat.intent && (
          <div className="px-10 py-3 border-t border-white/5 bg-panel-alt flex items-center gap-3 text-dim">
            <span className="text-muted">Next:</span>
            <span className="text-action">{combat.intent.class}</span>
            <span className="text-primary">— {combat.intent.text}</span>
          </div>
        )}

        {/* Actions */}
        <div className="px-10 pt-4 pb-7 border-t border-white/5 flex flex-col gap-3">
          {combat.resolved ? (
            <Button
              size="lg"
              onClick={() => refreshState()}
              disabled={loading}
              className="w-full"
            >
              Continue
            </Button>
          ) : isSword ? (
            <SwordControls combat={combat} loading={loading} doCombatAction={doCombatAction} />
          ) : isDagger ? (
            <DaggerControls loading={loading} doCombatAction={doCombatAction} />
          ) : (
            <DefaultControls loading={loading} doCombatAction={doCombatAction} />
          )}
        </div>
      </div>
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

function LogLine({ line }: { line: string }) {
  // Round markers and outcome banners get a horizontal-rule treatment.
  if (line.startsWith("— Round")) {
    return (
      <div className="flex items-center gap-3 text-muted">
        <div className="flex-1 h-px bg-white/[0.08]" />
        <span>{line.replace(/^—\s*|\s*—$/g, "")}</span>
        <div className="flex-1 h-px bg-white/[0.08]" />
      </div>
    );
  }
  if (line.startsWith("===")) {
    return <div className="font-header text-accent text-center">{line.replace(/=/g, "").trim()}</div>;
  }

  // Heuristic actor color: lines that begin with "  Player" or "  Monster" are turns.
  // Strip the leading two-space indent the server renderer uses.
  const trimmed = line.replace(/^ {2}/, "");
  let actorClass = "text-dim";
  if (trimmed.startsWith("Player")) actorClass = "text-action";
  else if (trimmed.startsWith("Monster")) actorClass = "text-negative";
  else if (trimmed.startsWith("Cunning save")) actorClass = "text-dim";
  else if (trimmed.startsWith("Stance:") || trimmed.startsWith("Intent:") || trimmed.startsWith("Surprise:")) actorClass = "text-muted";

  return (
    <div className={`leading-relaxed ${actorClass === "text-dim" ? "text-primary" : ""}`}>
      <span className={actorClass}>{trimmed}</span>
    </div>
  );
}

// ── Outcome panel (rendered inline at the end of the log) ─────────────────

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

// ── Weapon-specific controls ──────────────────────────────────────────────

function SwordControls({
  combat, loading, doCombatAction,
}: {
  combat: CombatInfo;
  loading: boolean;
  doCombatAction: (b: { action: string; stance?: string; band?: string }) => Promise<unknown>;
}) {
  const stance = combat.stance.toLowerCase();
  return (
    <>
      <div className="flex items-center gap-3">
        <span className="text-dim">Stance</span>
        <div className="flex bg-btn border border-white/10 rounded-lg overflow-hidden">
          {(["aggressive", "balanced", "defensive"] as const).map(s => (
            <button
              key={s}
              onClick={() => doCombatAction({ action: "stance", stance: s })}
              disabled={loading}
              className={`px-[18px] py-2 leading-none transition ${
                stance === s
                  ? "bg-action text-contrast hover:bg-action-hover"
                  : "text-action hover:bg-btn-hover hover:text-action-hover"
              }`}
            >
              {s.charAt(0).toUpperCase() + s.slice(1)}
            </button>
          ))}
        </div>
      </div>
      <div className="grid grid-cols-3 gap-3">
        <PrimaryActionButton onClick={() => doCombatAction({ action: "attack" })} disabled={loading} icon="sword-brandish.svg" label="Attack" sub={`d20 +${combat.playerAttackBonus} vs AC ${combat.monsterAc}`} />
        <SecondaryActionButton onClick={() => {}} disabled icon="pouch-with-beads.svg" label="Use item" sub="not yet" />
        <SecondaryActionButton onClick={() => doCombatAction({ action: "flee" })} disabled={loading} icon="walk.svg" label="Flee" sub="Cunning vs DC 12" />
      </div>
    </>
  );
}

function DaggerControls({
  loading, doCombatAction,
}: {
  loading: boolean;
  doCombatAction: (b: { action: string; stance?: string; band?: string }) => Promise<unknown>;
}) {
  // Debug-mode stand-in for the actual timing minigame. Future: replace with
  // the bands widget; for now four buttons let us exercise the backend.
  const bands = [
    { band: "miss",       label: "Miss",       sub: "off-time",      tone: "dim" as const },
    { band: "hit",        label: "Hit",        sub: "outer band",    tone: "default" as const },
    { band: "crit",       label: "Crit",       sub: "inner band",    tone: "default" as const },
    { band: "super_crit", label: "Super-crit", sub: "atomic — T3+",  tone: "primary" as const },
  ];
  return (
    <>
      <div className="text-muted text-center">
        Strike timing <span className="text-dim">(debug — minigame to come)</span>
      </div>
      <div className="grid grid-cols-4 gap-3">
        {bands.map(b => (
          <SecondaryActionButton
            key={b.band}
            onClick={() => doCombatAction({ action: "dagger_attack", band: b.band })}
            disabled={loading}
            label={b.label}
            sub={b.sub}
            primary={b.tone === "primary"}
            dim={b.tone === "dim"}
          />
        ))}
      </div>
      <div className="grid grid-cols-2 gap-3">
        <SecondaryActionButton onClick={() => {}} disabled label="Use item" sub="not yet" icon="pouch-with-beads.svg" />
        <SecondaryActionButton onClick={() => doCombatAction({ action: "flee" })} disabled={loading} icon="walk.svg" label="Flee" sub="Cunning vs DC 12" />
      </div>
    </>
  );
}

function DefaultControls({
  loading, doCombatAction,
}: {
  loading: boolean;
  doCombatAction: (b: { action: string; stance?: string; band?: string }) => Promise<unknown>;
}) {
  // Axe and unarmed for now — same atomic Attack as sword without stance.
  return (
    <div className="grid grid-cols-3 gap-3">
      <PrimaryActionButton onClick={() => doCombatAction({ action: "attack" })} disabled={loading} icon="sword-brandish.svg" label="Attack" sub="d20 to hit" />
      <SecondaryActionButton onClick={() => {}} disabled label="Use item" sub="not yet" icon="pouch-with-beads.svg" />
      <SecondaryActionButton onClick={() => doCombatAction({ action: "flee" })} disabled={loading} icon="walk.svg" label="Flee" sub="Cunning vs DC 12" />
    </div>
  );
}

// ── Action button atoms ───────────────────────────────────────────────────

function PrimaryActionButton({
  onClick, disabled, icon, label, sub,
}: { onClick: () => void; disabled: boolean; icon?: string; label: string; sub?: string }) {
  return (
    <button
      onClick={onClick}
      disabled={disabled}
      className="bg-action text-contrast border border-white/10 rounded-lg px-4 py-3.5 hover:bg-action-hover transition flex items-center justify-center gap-2.5 disabled:opacity-50 disabled:cursor-not-allowed"
    >
      {icon && <MaskedIcon icon={icon} color="currentColor" className="w-[22px] h-[22px]" />}
      <div>
        <div>{label}</div>
        {sub && <div className="text-contrast/70 text-[16px] leading-none mt-1">{sub}</div>}
      </div>
    </button>
  );
}

function SecondaryActionButton({
  onClick, disabled, icon, label, sub, primary, dim,
}: { onClick: () => void; disabled: boolean; icon?: string; label: string; sub?: string; primary?: boolean; dim?: boolean }) {
  if (primary) return <PrimaryActionButton onClick={onClick} disabled={disabled} icon={icon} label={label} sub={sub} />;
  const color = dim ? "text-muted hover:text-dim" : "text-action hover:text-action-hover";
  return (
    <button
      onClick={onClick}
      disabled={disabled}
      className={`bg-btn border border-white/10 rounded-lg px-4 py-3.5 hover:bg-btn-hover transition flex items-center justify-center gap-2.5 disabled:opacity-40 disabled:cursor-not-allowed ${color}`}
    >
      {icon && <MaskedIcon icon={icon} color="currentColor" className="w-[22px] h-[22px]" />}
      <div>
        <div>{label}</div>
        {sub && <div className="text-action-dim text-[16px] leading-none mt-1">{sub}</div>}
      </div>
    </button>
  );
}
