import { useState, useEffect, useRef } from "react";
import { useGame } from "../GameContext";
import { formatDateTime } from "../calendar";
import type { GameResponse, OutcomeInfo } from "../api/types";
import parchment from "../assets/parchment.webp";
import { formatProse } from "../prose";
import DieRoll, { MeetsCheck } from "../components/DieRoll";

type Segment =
  | { kind: "outcome"; data: OutcomeInfo }
  | { kind: "body"; text: string }
  | { kind: "chosen"; label: string; preview?: string };

export default function Encounter({ state }: { state: GameResponse }) {
  const { doAction, loading } = useGame();
  const { encounter, outcome, node, status } = state;
  const [vignetteError, setVignetteError] = useState(false);
  const scrollRef = useRef<HTMLDivElement>(null);

  const [segments, setSegments] = useState<Segment[]>([]);
  const [baseTitle, setBaseTitle] = useState("");
  const [baseBody, setBaseBody] = useState("");
  const [baseVignette, setBaseVignette] = useState<string | undefined>(undefined);
  const processedOutcome = useRef<OutcomeInfo | undefined>(undefined);

  // Detect fresh encounter vs continuation vs terminal outcome
  useEffect(() => {
    if (encounter && !outcome) {
      // Fresh encounter — reset everything
      setSegments([]);
      setBaseTitle(encounter.title);
      setBaseBody(encounter.body);
      setBaseVignette(encounter.vignette);
      setVignetteError(false);
      processedOutcome.current = undefined;
    } else if (outcome && processedOutcome.current !== outcome) {
      const isNavigation = state.mode === "encounter" && encounter && baseTitle !== "";
      if (isNavigation) {
        // Scene transition — append outcome from previous choice + new encounter's body
        processedOutcome.current = outcome;
        setSegments(prev => [
          ...prev,
          { kind: "outcome", data: outcome },
          ...(encounter.body ? [{ kind: "body" as const, text: encounter.body }] : []),
        ]);
        setBaseTitle(encounter.title);
        setBaseVignette(encounter.vignette);
      } else {
        // Terminal outcome (same encounter or standalone outcome mode)
        processedOutcome.current = outcome;
        setSegments(prev => [...prev, { kind: "outcome", data: outcome }]);
      }
    } else if (outcome && encounter && baseTitle === "") {
      // First load is already a continuation (e.g. page refresh during dungeon)
      setBaseTitle(encounter.title);
      setBaseBody(encounter.body);
      processedOutcome.current = undefined;
    }
  }, [encounter, outcome, state.mode]);

  // Index of the last "chosen" segment — scroll target
  const scrollTargetIndex = useRef(-1);
  const scrollTargetEl = useRef<HTMLDivElement | null>(null);

  // When a new "chosen" segment appears, record its index
  useEffect(() => {
    for (let i = segments.length - 1; i >= 0; i--) {
      if (segments[i].kind === "chosen") {
        scrollTargetIndex.current = i;
        break;
      }
    }
  }, [segments.length]);

  // Scroll to the chosen marker when it mounts (or when outcome arrives after it)
  useEffect(() => {
    if (scrollTargetEl.current && scrollRef.current) {
      const container = scrollRef.current;
      const target = scrollTargetEl.current;
      const top = target.offsetTop - container.offsetTop;
      container.scrollTo({ top, behavior: "smooth" });
    }
  }, [segments.length]);

  if (!encounter && !outcome) return null;

  const isSettlement = node?.poi?.kind === "settlement";
  const vignette = encounter?.vignette ?? baseVignette;
  const vignetteSrc = vignette
    ? `/world/assets/vignettes/${vignette}.webp`
    : isSettlement
      ? `/world/assets/vignettes/${node!.terrain}/${node!.terrain}_settlement.webp`
      : node?.terrain && node.regionTier != null && node.regionTier > 0
        ? `/world/assets/vignettes/${node.terrain}/${node.terrain}_tier_${node.regionTier}_1.webp`
        : null;
  const hasVignette = !vignetteError && vignetteSrc != null;

  // Is this a terminal outcome (no more choices to show)?
  const isTerminalOutcome = state.mode === "outcome" && segments.some(s => s.kind === "outcome");
  const terminalOutcome = isTerminalOutcome ? segments[segments.length - 1] : null;

  return (
    <div className="h-full flex bg-page text-primary">
      {/* Left panel — vignette over parchment */}
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

      {/* Right panel — narrative content */}
      <div ref={scrollRef} className="flex-1 overflow-y-auto p-8 md:p-12 flex flex-col">
        <div className="max-w-2xl space-y-6">
          {/* Base title + body (shown once) */}
          <div>
            <h2 className="text-3xl md:text-4xl font-header text-accent">
              {baseTitle || encounter?.title}
            </h2>
            {node?.region && status && (
              <p className="text-dim text-sm mt-1">
                {node.region}, {formatDateTime(status.day, status.time)}
              </p>
            )}
          </div>

          {(baseBody || encounter?.body) && segments.length === 0 && !isTerminalOutcome && (
            <div className="text-primary/80 leading-loose whitespace-pre-wrap">
              {formatProse((baseBody || encounter?.body || "").trim())}
            </div>
          )}

          {baseBody && segments.length > 0 && (
            <div className="text-primary/80 leading-loose whitespace-pre-wrap">
              {formatProse(baseBody.trim())}
            </div>
          )}

          {/* Accumulated segments */}
          {segments.map((seg, i) => (
            <div key={i} ref={i === scrollTargetIndex.current ? scrollTargetEl : undefined}>
              {seg.kind === "outcome" ? (
                <OutcomeSegment outcome={seg.data} />
              ) : seg.kind === "chosen" ? (
                <div className="flex items-start gap-3 opacity-50">
                  <img
                    src="/world/assets/icons/sun.svg"
                    alt=""
                    className="w-4 h-4 mt-1 shrink-0"
                  />
                  <span>
                    <span className="font-bold text-dim">
                      {seg.label}
                    </span>
                    {seg.preview && (
                      <span className="block text-dim mt-0.5">
                        {seg.preview}
                      </span>
                    )}
                  </span>
                </div>
              ) : (
                <div className="text-primary/80 leading-loose whitespace-pre-wrap">
                  {formatProse(seg.text.trim())}
                </div>
              )}
            </div>
          ))}

          {/* Current choices (if not terminal) */}
          {!isTerminalOutcome && encounter && (
            <div className="space-y-4 pt-2">
              {encounter.choices.map((choice) => (
                <button
                  key={choice.index}
                  onClick={choice.locked ? undefined : () => {
                    setSegments(prev => [...prev, { kind: "chosen", label: choice.label, preview: choice.preview ?? undefined }]);
                    doAction({ action: "choose", choiceIndex: choice.index });
                  }}
                  disabled={loading || choice.locked}
                  className={`w-full text-left flex items-start gap-3 transition-colors ${
                    choice.locked
                      ? "opacity-40 cursor-default"
                      : "group cursor-pointer"
                  }`}
                >
                  <img
                    src="/world/assets/icons/sun.svg"
                    alt=""
                    className={`w-4 h-4 mt-1 shrink-0 ${
                      choice.locked
                        ? "opacity-30"
                        : "opacity-70 group-hover:opacity-100 transition-opacity"
                    }`}
                  />
                  <span>
                    <span className={choice.locked
                      ? "font-bold text-muted"
                      : "font-bold text-action group-hover:text-action-hover transition-colors"
                    }>
                      {choice.label}
                    </span>
                    {choice.locked && choice.requires && (
                      <span className="block text-dim mt-0.5">
                        (requires {choice.requires})
                      </span>
                    )}
                    {!choice.locked && choice.preview && (
                      <span className="block text-sm text-action-dim mt-0.5">
                        {choice.preview}
                      </span>
                    )}
                  </span>
                </button>
              ))}

              {encounter.choices.length === 0 && (
                <button
                  onClick={() => doAction({ action: "end_encounter" })}
                  className="flex items-start gap-3 transition-colors group cursor-pointer"
                >
                  <img
                    src="/world/assets/icons/sun.svg"
                    alt=""
                    className="w-4 h-4 mt-1 shrink-0 opacity-70 group-hover:opacity-100
                               transition-opacity"
                  />
                  <span className="font-bold text-action group-hover:text-action-hover transition-colors">
                    Continue
                  </span>
                </button>
              )}
            </div>
          )}

          {/* Terminal outcome — show continue/return button */}
          {isTerminalOutcome && terminalOutcome?.kind === "outcome" && (
            <button
              onClick={() => doAction({ action: terminalOutcome.data.nextAction || "end_encounter" })}
              disabled={loading}
              className="flex items-start gap-3 transition-colors group cursor-pointer"
            >
              <img
                src="/world/assets/icons/sun.svg"
                alt=""
                className="w-4 h-4 mt-1 shrink-0 opacity-70 group-hover:opacity-100 transition-opacity"
              />
              <span className="font-bold text-action group-hover:text-action-hover transition-colors">
                {terminalOutcome.data.nextAction === "end_dungeon" ? "Return to your journey" : "Continue"}
              </span>
            </button>
          )}
        </div>
      </div>
    </div>
  );
}

function OutcomeSegment({ outcome }: { outcome: OutcomeInfo }) {
  return (
    <div className="space-y-4">
      {outcome.preamble && (
        <div className="text-primary/80 leading-loose whitespace-pre-wrap">
          {formatProse(outcome.preamble)}
        </div>
      )}

      {outcome.skillCheck && (
        outcome.skillCheck.kind === "meets" ? (
          <MeetsCheck
            label={outcome.skillCheck.skill}
            modifier={outcome.skillCheck.modifier}
            target={outcome.skillCheck.target}
            passed={outcome.skillCheck.passed}
          />
        ) : (
          <DieRoll
            label={outcome.skillCheck.skill}
            rolled={outcome.skillCheck.rolled}
            modifier={outcome.skillCheck.modifier}
            target={outcome.skillCheck.target}
            passed={outcome.skillCheck.passed}
            rollMode={outcome.skillCheck.rollMode}
          />
        )
      )}

      <div className="text-primary/80 leading-loose whitespace-pre-wrap">
        {formatProse(outcome.text)}
      </div>

      {outcome.mechanics.length > 0 && (
        <div className="space-y-2 border-t border-edge pt-3">
          {outcome.mechanics.map((m, i) =>
            m.resistCheck ? (
              <DieRoll
                key={i}
                label={m.resistCheck.conditionName}
                verb="resist"
                rolled={m.resistCheck.rolled}
                modifier={m.resistCheck.modifier}
                target={m.resistCheck.target}
                passed={m.resistCheck.passed}
                passLabel="Resisted"
                failLabel="Afflicted"
                rollMode={m.resistCheck.rollMode}
              />
            ) : (
              <div key={i} className="text-xs text-dim">
                {m.description}
              </div>
            )
          )}
        </div>
      )}
    </div>
  );
}
