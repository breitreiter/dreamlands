import { useGame } from "../GameContext";
import type { ApproachPromptInfo } from "../api/types";

const SKILL_HEADINGS: Record<string, string> = {
  negotiation: "They might be open to Negotiation. How do you want to play this?",
  cunning: "You'll need to rely on your Cunning here. What's the plan?",
  bushcraft: "Your Bushcraft will be tested. How will you approach this?",
  combat: "It looks like Combat. What's your opening move?",
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
    <div className="space-y-4 pt-2">
      <p>{skillHeading(prompt.skill)}</p>

      {prompt.preamble && (
        <p className="whitespace-pre-wrap">{prompt.preamble}</p>
      )}

      <div className="space-y-3">
        {prompt.approaches.map((approach) => (
          <button
            key={approach.id}
            onClick={() => pick(approach.id)}
            disabled={loading}
            className="w-full text-left flex items-start gap-3 transition-colors group cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed"
          >
            <img
              src={`/world/assets/icons/${approach.iconHint || "sun.svg"}`}
              alt=""
              className="w-4 h-4 mt-1 shrink-0 opacity-70 group-hover:opacity-100 transition-opacity"
            />
            <span className="font-bold text-action group-hover:text-action-hover transition-colors">
              {approach.label}
            </span>
          </button>
        ))}
      </div>
    </div>
  );
}
