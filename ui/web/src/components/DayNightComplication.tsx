const ROTATION: Record<string, number> = {
  morning: 288,
  midday: 0,
  afternoon: 72,
  evening: 144,
  night: 216,
};

export default function DayNightComplication({ time }: { time: string }) {
  const degrees = ROTATION[time] ?? 0;

  // The complication sits below the vignette with its bottom half clipping
  // out of the viewport. The parent cluster is anchored to bottom-0, so
  // this naturally extends past the screen edge.
  return (
    <div className="absolute left-1/2 -translate-x-1/2 top-full -translate-y-1/2 w-[100px] h-[100px] z-10">
      <div className="absolute inset-0 rounded-full border-2 border-edge bg-black/60 overflow-hidden">
        <img
          src="/world/assets/ui/daynight_complication.png"
          alt=""
          className="absolute inset-0 w-full h-full"
          style={{
            transform: `rotate(${degrees}deg)`,
            transition: "transform 3s cubic-bezier(0.4, 0, 0.2, 1)",
          }}
        />
      </div>
    </div>
  );
}
