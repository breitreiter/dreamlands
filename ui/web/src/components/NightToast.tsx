import { useEffect, useState } from "react";
import { useGame } from "../GameContext";

export default function NightToast() {
  const { toast, clearToast } = useGame();
  const [visible, setVisible] = useState(false);

  useEffect(() => {
    if (toast) {
      // Trigger fade-in on next frame
      requestAnimationFrame(() => setVisible(true));
    } else {
      setVisible(false);
    }
  }, [toast]);

  if (!toast) return null;

  return (
    <div
      className={`fixed top-6 left-1/2 -translate-x-1/2 z-[1500] transition-opacity duration-500 cursor-pointer ${visible ? "opacity-100" : "opacity-0"}`}
      onClick={clearToast}
    >
      <div className="bg-panel/90 rounded-lg px-5 py-3 space-y-1 shadow-lg backdrop-blur-sm">
        {toast.lines.map((line, i) => (
          <div
            key={i}
            className={
              line.color === "negative" ? "text-negative" :
              line.color === "positive" ? "text-positive" :
              "text-primary"
            }
          >
            {line.text}
          </div>
        ))}
      </div>
    </div>
  );
}
