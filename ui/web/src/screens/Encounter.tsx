import { useState, useEffect, useRef } from "react";
import { useGame } from "../GameContext";
import { formatDateTime } from "../calendar";
import type { GameResponse, OutcomeInfo } from "../api/types";
import parchment from "../assets/parchment.png";

type Segment =
  | { kind: "outcome"; data: OutcomeInfo }
  | { kind: "body"; text: string };

export default function Encounter({ state }: { state: GameResponse }) {
  const { doAction, loading } = useGame();
  const { encounter, outcome, node, status } = state;
  const [vignetteError, setVignetteError] = useState(false);
  const scrollRef = useRef<HTMLDivElement>(null);

  const [segments, setSegments] = useState<Segment[]>([]);
  const [baseTitle, setBaseTitle] = useState("");
  const [baseBody, setBaseBody] = useState("");
  const processedOutcome = useRef<OutcomeInfo | undefined>(undefined);

  // Detect fresh encounter vs continuation vs terminal outcome
  useEffect(() => {
    if (encounter && !outcome) {
      // Fresh encounter — reset everything
      setSegments([]);
      setBaseTitle(encounter.title);
      setBaseBody(encounter.body);
      processedOutcome.current = undefined;
    } else if (outcome && processedOutcome.current !== outcome) {
      if (encounter && encounter.title !== baseTitle && baseTitle !== "") {
        // Continuation: scene transition — append outcome + new body
        processedOutcome.current = outcome;
        setSegments(prev => [
          ...prev,
          { kind: "outcome", data: outcome },
          ...(encounter.body ? [{ kind: "body" as const, text: encounter.body }] : []),
        ]);
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
  }, [encounter, outcome]);

  // Auto-scroll on new segments or outcome
  useEffect(() => {
    if (segments.length > 0 && scrollRef.current) {
      scrollRef.current.scrollTo({ top: scrollRef.current.scrollHeight, behavior: "smooth" });
    }
  }, [segments.length]);

  if (!encounter && !outcome) return null;

  const dungeonId = node?.poi?.dungeonId;
  const vignetteSrc = dungeonId
    ? `/world/assets/vignettes/dungeons/${dungeonId}.png`
    : `/world/assets/vignettes/${node!.terrain}/${node!.terrain}_tier_${node!.regionTier}_1.png`;
  const hasVignette =
    !vignetteError &&
    (dungeonId || (node?.terrain && node.regionTier != null && node.regionTier > 0));

  // Is this a terminal outcome (no more choices to show)?
  const isTerminalOutcome = outcome && (!encounter || encounter.title === baseTitle) && segments.some(s => s.kind === "outcome");
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
            <h2 className="text-3xl md:text-4xl font-header text-accent uppercase">
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
              {baseBody || encounter?.body}
            </div>
          )}

          {baseBody && segments.length > 0 && (
            <div className="text-primary/80 leading-loose whitespace-pre-wrap">
              {baseBody}
            </div>
          )}

          {/* Accumulated segments */}
          {segments.map((seg, i) => (
            <div key={i}>
              {seg.kind === "outcome" ? (
                <OutcomeSegment outcome={seg.data} />
              ) : (
                <div className="text-primary/80 leading-loose whitespace-pre-wrap">
                  {seg.text}
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
                  onClick={() =>
                    doAction({ action: "choose", choiceIndex: choice.index })
                  }
                  disabled={loading}
                  className="w-full text-left flex items-start gap-3
                             disabled:text-muted transition-colors group cursor-pointer"
                >
                  <img
                    src="/world/assets/icons/sun.svg"
                    alt=""
                    className="w-4 h-4 mt-1 shrink-0 opacity-70 group-hover:opacity-100
                               transition-opacity"
                  />
                  <span>
                    <span className="font-bold text-action group-hover:text-action-hover transition-colors">
                      {choice.label}
                    </span>
                    {choice.preview && (
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
          {outcome.preamble}
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
          <span className="capitalize">{outcome.skillCheck.skill}</span>
          {" check: "}
          <span className="font-medium">
            {outcome.skillCheck.rolled}
            {outcome.skillCheck.modifier !== 0 &&
              ` ${outcome.skillCheck.modifier >= 0 ? "+" : ""}${outcome.skillCheck.modifier}`}
          </span>
          {" vs "}
          <span className="font-medium">{outcome.skillCheck.target}</span>
          {" — "}
          <span className={outcome.skillCheck.passed ? "text-positive" : "text-negative"}>
            {outcome.skillCheck.passed ? "Success" : "Failure"}
          </span>
        </div>
      )}

      <div className="text-primary/80 leading-loose whitespace-pre-wrap">
        {outcome.text}
      </div>

      {outcome.mechanics.length > 0 && (
        <div className="space-y-1 border-t border-edge pt-3">
          {outcome.mechanics.map((m, i) => (
            <div key={i} className="text-xs text-dim">
              {m.description}
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
