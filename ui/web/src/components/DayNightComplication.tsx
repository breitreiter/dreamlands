import { useRef, useEffect, useState } from "react";

const ROTATION: Record<string, number> = {
  Morning: 252,
  Midday: 324,
  Afternoon: 36,
  Evening: 108,
  Night: 180,
};

export default function DayNightComplication({ time }: { time: string }) {
  const target = ROTATION[time] ?? 0;
  const [cumulative, setCumulative] = useState(target);
  const prevTarget = useRef(target);

  useEffect(() => {
    if (target === prevTarget.current) return;
    setCumulative(prev => {
      // Always advance clockwise: if the new target is behind, add 360
      let delta = target - (prev % 360);
      if (delta <= 0) delta += 360;
      return prev + delta;
    });
    prevTarget.current = target;
  }, [target]);

  return (
    <div className="absolute left-1/2 -translate-x-1/2 top-full -translate-y-[35%] w-[140px] h-[140px] z-10">
      <div className="absolute inset-0 rounded-full border-3 border-page bg-black/60 overflow-hidden">
        <img
          src="/world/assets/ui/daynight_complication.png"
          alt=""
          className="absolute inset-0 w-full h-full"
          style={{
            transform: `rotate(${cumulative}deg)`,
            transition: "transform 3s cubic-bezier(0.4, 0, 0.2, 1)",
          }}
        />
      </div>
    </div>
  );
}
