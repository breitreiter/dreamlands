import fs from "fs";
import path from "path";
import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import tailwindcss from "@tailwindcss/vite";

const apiVersion = fs.readFileSync(path.resolve(__dirname, "../../api-version"), "utf-8").trim();

export default defineConfig({
  plugins: [react(), tailwindcss()],
  define: {
    __API_VERSION__: JSON.stringify(apiVersion),
    __API_BASE__: JSON.stringify(process.env.VITE_API_BASE || ""),
  },
  resolve: {
    alias: {
      "@": path.resolve(__dirname, "./src"),
    },
  },
  build: {
    rollupOptions: {
      input: {
        main: path.resolve(__dirname, "index.html"),
        reference: path.resolve(__dirname, "reference.html"),
      },
    },
  },
  server: {
    port: 3000,
    proxy: {
      "/api": "http://localhost:7071",
    },
  },
});
