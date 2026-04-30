type Props = {
  x: number;          // px, relative to positioned parent
  y: number;          // px, relative to positioned parent
  angle: number;      // deg
  size?: number;      // outer diameter, px
  thickness?: number; // crescent thickness on the thick side, px
  duration?: number;  // ms
  color?: string;
  onDone?: () => void;
};

// Outer circle minus an interior circle, offset so the cutout is internally
// tangent to the outer's edge. Uses fill-rule="evenodd" so the inner subpath
// punches a hole. d = R - r places the cutout flush against +x; the visible
// thick side faces -x and is rotated to taste.
export default function MissMoon({
  x, y, angle,
  size = 84,
  thickness = 18,
  duration = 300,
  color = "#ffffff",
  onDone,
}: Props) {
  const R = size / 2;
  const r = R - thickness / 2;
  const d = R - r;
  const pad = 2;
  const vb = size + pad * 2;

  return (
    <svg
      className="miss-moon"
      width={vb}
      height={vb}
      viewBox={`${-R - pad} ${-R - pad} ${vb} ${vb}`}
      style={{
        left: x - vb / 2,
        top: y - vb / 2,
        ["--miss-angle" as string]: `${angle}deg`,
        animationDuration: `${duration}ms`,
      }}
      onAnimationEnd={onDone}
    >
      <path
        fillRule="evenodd"
        fill={color}
        d={`M ${-R},0 a ${R},${R} 0 1,0 ${2 * R},0 a ${R},${R} 0 1,0 ${-2 * R},0 Z
            M ${d - r},0 a ${r},${r} 0 1,0 ${2 * r},0 a ${r},${r} 0 1,0 ${-2 * r},0 Z`}
      />
    </svg>
  );
}
