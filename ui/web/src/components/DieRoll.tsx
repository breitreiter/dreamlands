import type { ReactNode } from "react";
import MaskedIcon from "./MaskedIcon";

export type RollMode = "advantage" | "disadvantage" | null | undefined;

function Panel({
  passed, rollMode, children,
}: {
  passed: boolean;
  rollMode?: RollMode;
  children: ReactNode;
}) {
  return (
    <div>
      <MaskedIcon
        icon="dice-twenty-faces-twenty.svg"
        className="w-5 h-5 inline-block align-text-bottom mr-1.5"
        color={passed ? "var(--color-positive)" : "var(--color-negative)"}
      />
      {children}
      {rollMode && (
        <>
          {" · "}
          <span className={rollMode === "disadvantage" ? "text-negative" : "text-positive"}>
            {rollMode === "disadvantage" ? "Disadvantage" : "Advantage"}
          </span>
        </>
      )}
    </div>
  );
}

export interface DieRollProps {
  rolled: number;
  modifier: number;
  target: number;
  passed: boolean;
  label?: string;
  verb?: string;             // joined as " <verb>:"  — default "check"
  targetPrefix?: string;     // "" (default), "AC ", "DC "
  passLabel?: string;        // default "Success"
  failLabel?: string;        // default "Failure"
  rollMode?: RollMode;
  detail?: ReactNode;        // optional trailing text after the pass/fail tag
}

/**
 * Standard d20-vs-DC roll line: d20 icon tinted by pass/fail, then
 * "<label> <verb>: rolled+mod vs <prefix><target> · pass/fail". Used for
 * skill checks, resist checks, combat attack rolls, and Cunning saves.
 */
export default function DieRoll({
  rolled, modifier, target, passed,
  label, verb = "check",
  targetPrefix = "",
  passLabel = "Success",
  failLabel = "Failure",
  rollMode,
  detail,
}: DieRollProps) {
  const modText = modifier !== 0 ? ` ${modifier >= 0 ? "+" : ""}${modifier}` : "";
  return (
    <Panel passed={passed} rollMode={rollMode}>
      {label && <><span className="capitalize">{label}</span>{verb ? ` ${verb}:` : ":"} </>}
      <span className="font-medium">{rolled - modifier}{modText}</span>
      {" vs "}{targetPrefix}<span className="font-medium">{target}</span>
      {" · "}
      <span className={passed ? "text-positive" : "text-negative"}>
        {passed ? passLabel : failLabel}
      </span>
      {detail && <> · {detail}</>}
    </Panel>
  );
}

export interface MeetsCheckProps {
  label: string;
  modifier: number;
  target: number;
  passed: boolean;
  passLabel?: string;        // default "Qualified"
  failLabel?: string;        // default "Unqualified"
}

/**
 * Static-comparison panel for "meets" checks (no roll): "<label> <mod> meets
 * <target>" or "doesn't meet". Same panel shell as DieRoll so the two read
 * as variants of the same control.
 */
export function MeetsCheck({
  label, modifier, target, passed,
  passLabel = "Qualified",
  failLabel = "Unqualified",
}: MeetsCheckProps) {
  return (
    <Panel passed={passed}>
      <span className="capitalize">{label}</span>
      {" "}
      <span className="font-medium">{modifier}</span>
      {passed ? " meets " : " doesn't meet "}
      <span className="font-medium">{target}</span>
      {" · "}
      <span className={passed ? "text-positive" : "text-negative"}>
        {passed ? passLabel : failLabel}
      </span>
    </Panel>
  );
}
