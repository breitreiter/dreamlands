type Props = {
  x: number;             // px, relative to positioned parent
  y: number;             // px, relative to positioned parent
  angle: number;         // deg
  length?: number;       // anchor-to-anchor distance, px
  thickness?: number;    // full waist, px
  duration?: number;     // ms
  color?: string;
  strokeWidth?: number;
  growTo?: number;       // final scale
  onDone?: () => void;
};

export default function HitLens({
  x, y, angle,
  length = 224,
  thickness = 28,
  duration = 320,
  color = "#fffbe6",
  strokeWidth = 0,
  growTo = 2.6,
  onDone,
}: Props) {
  const half = length / 2;
  const halfT = thickness / 2;
  const pad = 4;
  const vbW = length + pad * 2;
  const vbH = thickness + pad * 2;

  return (
    <svg
      className="hit-lens"
      width={vbW}
      height={vbH}
      viewBox={`${-half - pad} ${-halfT - pad} ${vbW} ${vbH}`}
      style={{
        left: x - vbW / 2,
        top: y - vbH / 2,
        ["--lens-angle" as string]: `${angle}deg`,
        ["--lens-grow" as string]: `${growTo}`,
        animationDuration: `${duration}ms`,
      }}
      onAnimationEnd={onDone}
    >
      <path
        d={`M ${-half} 0 Q 0 ${-halfT} ${half} 0 Q 0 ${halfT} ${-half} 0 Z`}
        fill={color}
        stroke={strokeWidth > 0 ? color : "none"}
        strokeWidth={strokeWidth}
        strokeLinejoin="round"
        vectorEffect="non-scaling-stroke"
      />
    </svg>
  );
}
