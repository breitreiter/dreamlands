type Props = {
  x: number;          // px, relative to positioned parent
  y: number;          // px, relative to positioned parent
  angle: number;      // deg
  variant: number;    // 1..8
  size?: number;      // px square
  duration?: number;  // ms
  color?: string;
  onDone?: () => void;
};

export default function HitSplat({
  x, y, angle, variant,
  size = 160,
  duration = 520,
  color = "#7a0a0a",
  onDone,
}: Props) {
  const url = `/world/assets/effects/splat/splat${variant}.svg`;
  return (
    <div
      className="hit-splat"
      onAnimationEnd={onDone}
      style={{
        left: x - size / 2,
        top: y - size / 2,
        width: size,
        height: size,
        backgroundColor: color,
        maskImage: `url(${url})`,
        WebkitMaskImage: `url(${url})`,
        ["--splat-angle" as string]: `${angle}deg`,
        animationDuration: `${duration}ms`,
      }}
    />
  );
}
