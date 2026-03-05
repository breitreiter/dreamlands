export function iconUrl(file: string): string {
  return `/world/assets/icons/${file}`;
}

const ITEM_TYPE_ICONS: Record<string, string> = {
  weapon: "sword-brandish.svg",
  armor: "chain-mail.svg",
  boots: "boots.svg",
  tool: "knapsack.svg",
  consumable: "pouch-with-beads.svg",
  tradegood: "two-coins.svg",
};

export function itemTypeIcon(type: string): string {
  return ITEM_TYPE_ICONS[type] || "wooden-crate.svg";
}

const TAB_ICONS: Record<string, string> = {
  trade: "two-coins.svg",
  foods: "pouch-with-beads.svg",
  equipment: "sword-brandish.svg",
  pack: "backpack.svg",
  haversack: "knapsack.svg",
  equipped: "sword-brandish.svg",
};

export function TabButton({ id, active, onClick, children }: { id: string; active: boolean; onClick: () => void; children: React.ReactNode }) {
  const icon = TAB_ICONS[id];
  return (
    <button
      onClick={onClick}
      className={`h-12 px-4 flex items-center gap-2 transition-colors ${
        active
          ? "bg-btn border border-accent text-accent"
          : "bg-transparent border border-transparent text-action-dim hover:text-action"
      }`}
      style={{ borderRadius: "999px" }}
    >
      {icon && <MaskedIcon icon={icon} className="w-5 h-5" color={active ? "#D0BD62" : "currentColor"} />}
      {children}
    </button>
  );
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
