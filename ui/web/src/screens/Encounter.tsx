import { useState, useEffect, useRef } from "react";
import { useGame } from "../GameContext";
import { formatDateTime } from "../calendar";
import type { GameResponse, OutcomeInfo } from "../api/types";
import parchment from "../assets/parchment.webp";
import { formatProse } from "../prose";
import MaskedIcon from "../components/MaskedIcon";

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
        <div
          className={`p-3 border ${
            outcome.skillCheck.passed
              ? "border-positive bg-positive/15"
              : "border-negative bg-negative/15"
          }`}
        >
          <MaskedIcon
            icon="dice-twenty-faces-twenty.svg"
            className="w-5 h-5 inline-block align-text-bottom mr-1.5"
            color={outcome.skillCheck.rollMode === "disadvantage" ? "#C45656"
              : outcome.skillCheck.rollMode === "advantage" ? "#5B9F5B"
              : "currentColor"}
          />
          {outcome.skillCheck.kind === "meets" ? (
            <>
              <span className="capitalize">{outcome.skillCheck.skill}</span>
              {" "}
              <span className="font-medium">{outcome.skillCheck.modifier}</span>
              {outcome.skillCheck.passed ? " meets " : " doesn't meet "}
              <span className="font-medium">{outcome.skillCheck.target}</span>
              {" — "}
              <span className={outcome.skillCheck.passed ? "text-positive" : "text-negative"}>
                {outcome.skillCheck.passed ? "Qualified" : "Unqualified"}
              </span>
            </>
          ) : (
            <>
              <span className="capitalize">{outcome.skillCheck.skill}</span>
              {" check: "}
              <span className="font-medium">
                {outcome.skillCheck.rolled - outcome.skillCheck.modifier}
                {outcome.skillCheck.modifier !== 0 &&
                  ` ${outcome.skillCheck.modifier >= 0 ? "+" : ""}${outcome.skillCheck.modifier}`}
              </span>
              {" vs "}
              <span className="font-medium">{outcome.skillCheck.target}</span>
              {" — "}
              <span className={outcome.skillCheck.passed ? "text-positive" : "text-negative"}>
                {outcome.skillCheck.passed ? "Success" : "Failure"}
              </span>
            </>
          )}
          {outcome.skillCheck.rollMode && (
            <>
              {" · "}
              <span className={outcome.skillCheck.rollMode === "disadvantage" ? "text-negative" : "text-positive"}>
                {outcome.skillCheck.rollMode === "disadvantage" ? "Disadvantage" : "Advantage"}
              </span>
            </>
          )}
        </div>
      )}

      <div className="text-primary/80 leading-loose whitespace-pre-wrap">
        {formatProse(outcome.text)}
      </div>

      {outcome.mechanics.length > 0 && (
        <div className="space-y-2 border-t border-edge pt-3">
          {outcome.mechanics.map((m, i) =>
            m.resistCheck ? (
              <div
                key={i}
                className={`p-3 border ${
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
