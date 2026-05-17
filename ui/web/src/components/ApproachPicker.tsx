import { useGame } from "../GameContext";
import type { ApproachPromptInfo } from "../api/types";
import MaskedIcon from "./MaskedIcon";

const SKILL_HEADINGS: Record<string, string> = {
  negotiation: "Your Negotiation will be tested. How will you approach this?",
  cunning: "Your Cunning will be tested. How will you approach this?",
  bushcraft: "Your Bushcraft will be tested. How will you approach this?",
  combat: "Combat is inevitable. Choose your approach.",
};

function skillHeading(skill: string): string {
  return SKILL_HEADINGS[skill.toLowerCase()] ?? `Your ${skill} will be tested. How will you approach this?`;
}

export default function ApproachPicker({ prompt }: { prompt: ApproachPromptInfo }) {
  const { doAction, loading } = useGame();

  function pick(approachId: string) {
    doAction({ action: "pick_approach", approach: approachId });
  }

  return (
    <div className="space-y-6 pt-2">
      <div>
        <h3 className="font-header text-accent text-[32px] leading-tight">
          {skillHeading(prompt.skill)}
        </h3>
        {prompt.preamble && (
          <p className="text-primary/80 leading-loose mt-3 whitespace-pre-wrap">
            {prompt.preamble}
          </p>
        )}
      </div>

      <div className="space-y-3">
        {prompt.approaches.map((approach) => (
          <button
            key={approach.id}
            onClick={() => pick(approach.id)}
            disabled={loading}
            className="w-full text-left flex items-center gap-4 p-4 rounded-lg transition-colors group cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed"
            style={{ backgroundColor: "rgba(0, 0, 0, 0.25)" }}
          >
            <div className="w-8 h-8 flex-shrink-0 flex items-center justify-center">
              <MaskedIcon
                icon={approach.iconHint || "sun.svg"}
                className="w-6 h-6 opacity-70 group-hover:opacity-100 transition-opacity"
                color="#D0925D"
              />
            </div>
            <span className="font-bold text-action group-hover:text-action-hover transition-colors">
              {approach.label}
            </span>
          </button>
        ))}
      </div>
    </div>
  );
}
