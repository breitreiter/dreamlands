export function iconUrl(file: string): string {
  return `/world/assets/icons/${file}`;
}

export default function MaskedIcon({ icon, className, color }: { icon: string; className?: string; color: string }) {
  return (
    <div
      className={`inline-block flex-shrink-0 ${className || ""}`}
      style={{
        backgroundColor: color,
        maskImage: `url(${iconUrl(icon)})`,
        maskSize: "contain",
        maskRepeat: "no-repeat",
        maskPosition: "center",
        WebkitMaskImage: `url(${iconUrl(icon)})`,
        WebkitMaskSize: "contain",
        WebkitMaskRepeat: "no-repeat",
        WebkitMaskPosition: "center",
      }}
    />
  );
}
