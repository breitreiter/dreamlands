export const HEALTH_GRADIENT = ["#7F0000", "#E50000"] as const;
export const SPIRITS_GRADIENT = ["#460AA7", "#843FF3"] as const;

export default function StatBar({
  label,
  value,
  max,
  gradient,
}: {
  label: string;
  value: number;
  max: number;
  gradient: readonly [string, string];
}) {
  const pct = max > 0 ? Math.round((value / max) * 100) : 0;
  return (
    <div>
      <div className="h-[18px] rounded-tl-full rounded-br-full bg-panel-alt">
        {pct > 0 && (
          <div
            className="h-full rounded-tl-full rounded-br-full border border-panel-alt"
            style={{
              width: `${pct}%`,
              background: `linear-gradient(170deg, ${gradient[0]}, ${gradient[1]})`,
            }}
          />
        )}
      </div>
      <div className="flex justify-center gap-1.5 mt-0.5 text-sm">
        <span className="text-dim">{label}</span>
        <span className="text-primary/80">{value}/{max}</span>
      </div>
    </div>
  );
}
