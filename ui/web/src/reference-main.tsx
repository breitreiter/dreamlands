import { createRoot } from "react-dom/client";
import Reference from "./screens/Reference";
import "./App.css";

document.documentElement.style.overflow = "auto";
document.body.style.overflow = "auto";

createRoot(document.getElementById("reference-root")!).render(<Reference />);
