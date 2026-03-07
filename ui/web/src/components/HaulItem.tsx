import MaskedIcon from "./MaskedIcon";

interface HaulItemProps {
  name: string;
  destinationName: string | null;
  destinationHint: string | null;
  payout: number | null;
  flavor: string | null;
}

export default function HaulItem({ name, destinationName, destinationHint, payout, flavor }: HaulItemProps) {
  return (
    <>
      <div className="text-primary">{name}</div>
      <div className="text-muted mt-0.5">
        Deliver to {destinationName ?? destinationHint}
        {destinationName && destinationHint && (
          <span className="text-dim"> ({destinationHint})</span>
        )}
      </div>
      {flavor && (
        <div className="text-muted mt-0.5">{flavor}</div>
      )}
      {payout != null && (
        <div className="mt-0.5 flex items-center gap-1" style={{ color: "#D0BD62" }}>
          <MaskedIcon icon="two-coins.svg" className="w-4 h-4" color="#D0BD62" />
          Pays {payout}g on delivery
        </div>
      )}
    </>
  );
}
