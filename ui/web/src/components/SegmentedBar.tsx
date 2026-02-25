export default function SegmentedBar({
  label,
  value,
  max,
  color,
}: {
  label: string;
  value: number;
  max: number;
  color: string;
}) {
  return (
    <div>
      <div className="flex gap-[2px]">
        {Array.from({ length: max }, (_, i) => {
          const isFirst = i === 0;
          const isLast = i === max - 1;
          const isEnd = isFirst || isLast;
          return (
            <div
              key={i}
              className="h-5 border border-dim"
              style={{
                width: isEnd ? 19 : 14,
                backgroundColor: i < value ? `var(${color})` : "#0d0d0d",
                borderRadius: isFirst
                  ? "999px 0 0 999px"
                  : isLast
                    ? "0 999px 999px 0"
                    : 0,
              }}
            />
          );
        })}
      </div>
      <div className="flex justify-between mt-0.5 text-sm">
        <span className="text-dim">{label}</span>
        <span className="text-primary/80">{value}/{max}</span>
      </div>
    </div>
  );
}
