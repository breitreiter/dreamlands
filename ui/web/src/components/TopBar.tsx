import type { StatusInfo } from "../api/types";
import MaskedIcon from "./MaskedIcon";

export default function TopBar({
  status,
  gold,
  goldAnnotation,
  onBack,
  children,
}: {
  status: StatusInfo;
  gold?: number;
  goldAnnotation?: React.ReactNode;
  onBack: () => void;
  children?: React.ReactNode;
}) {
  return (
    <div className="flex items-center gap-4 px-4 py-3 bg-parchment text-parchment-text flex-shrink-0">
      <div className="flex items-center gap-2">
        <MaskedIcon icon="heart-plus.svg" className="w-5 h-5" color="#3a3520" />
        <span className="text-contrast">Health</span>
        <span className="font-bold">{status.health}/{status.maxHealth}</span>
      </div>
      <div className="flex items-center gap-2">
        <MaskedIcon icon="sensuousness.svg" className="w-5 h-5" color="#3a3520" />
        <span className="text-contrast">Spirits</span>
        <span className="font-bold">{status.spirits}/{status.maxSpirits}</span>
      </div>
      <div className="flex items-center gap-2">
        <MaskedIcon icon="two-coins.svg" className="w-5 h-5" color="#3a3520" />
        <span className="text-contrast">Gold</span>
        <span className="font-bold">{gold ?? status.gold}g</span>
        {goldAnnotation}
      </div>
      <div className="flex-1" />
      {children ?? (
        <button
          onClick={onBack}
          className="px-4 py-1 rounded-lg bg-btn text-action hover:text-action-hover
                     transition-colors flex items-center gap-2"
        >
          <MaskedIcon icon="cancel.svg" className="w-4 h-4" color="currentColor" />
          Return to Map
        </button>
      )}
    </div>
  );
}
