import { useState, useMemo, useEffect } from "react";
import { MapContainer, TileLayer, Marker, useMap } from "react-leaflet";
import { CRS, LatLngBounds, Icon, type LatLngExpression } from "leaflet";
import "leaflet/dist/leaflet.css";
import { useGame } from "../GameContext";
import Inventory from "./Inventory";
import MarketScreen from "./Market";
import CompassRose from "../components/CompassRose";
import SegmentedBar from "../components/SegmentedBar";
import { formatDateTime } from "../calendar";
import type { GameResponse } from "../api/types";

// Map constants — 100x100 grid at 128px/tile = 12800px source.
// At max zoom 6: 1 latlng = 64px, so 12800/64 = 200 units.
const TILE_PX = 128;
const GRID = 100;
const MAX_ZOOM = 6;
const SCALE = Math.pow(2, MAX_ZOOM); // 64
const MAP_SIZE = (GRID * TILE_PX) / SCALE; // 200
const bounds = new LatLngBounds([0, 0], [-MAP_SIZE, MAP_SIZE]);

function gridToLatLng(x: number, y: number): LatLngExpression {
  return [-(y * TILE_PX + TILE_PX / 2) / SCALE, (x * TILE_PX + TILE_PX / 2) / SCALE];
}

const playerIcon = new Icon({
  iconUrl:
    "data:image/svg+xml," +
    encodeURIComponent(
      '<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24">' +
        '<circle cx="12" cy="12" r="10" fill="#d0925d" stroke="#232d46" stroke-width="2"/>' +
        '<circle cx="12" cy="12" r="4" fill="#232d46"/>' +
        "</svg>"
    ),
  iconSize: [24, 24],
  iconAnchor: [12, 12],
});

function MapFollower({ position }: { position: LatLngExpression }) {
  const map = useMap();
  useEffect(() => {
    map.setView(position, map.getZoom(), { animate: true });
  }, [map, position]);
  return null;
}

const DIR_KEYS: Record<string, string> = {
  north: "w",
  south: "s",
  east: "d",
  west: "a",
};

const CONDITION_ICONS: Record<string, string> = {
  lost: "sextant.svg",
  hungry: "water-drop.svg",
  exhausted: "camping-tent.svg",
  injured: "nested-hearts.svg",
  poisoned: "caduceus.svg",
  cursed: "foamy-disc.svg",
};

const TIER_LABELS: Record<number, string> = { 1: "Village", 2: "Town", 3: "City" };

const IMPLEMENTED_SERVICES = new Set(["market"]);

const SERVICE_ICONS: Record<string, { icon: string; label: string }> = {
  market: { icon: "two-coins.svg", label: "Market" },
  healer: { icon: "caduceus.svg", label: "Healer" },
  temple: { icon: "tied-scroll.svg", label: "Temple" },
  inn: { icon: "camping-tent.svg", label: "Inn" },
  guild: { icon: "shaking-hands.svg", label: "Guild" },
};

export default function Explore({ state }: { state: GameResponse }) {
  const { doAction, loading } = useGame();
  const [showInventory, setShowInventory] = useState(false);
  const [vignetteError, setVignetteError] = useState(false);
  const [activeService, setActiveService] = useState<string | null>(null);

  const position = useMemo(
    () => (state.node ? gridToLatLng(state.node.x, state.node.y) : [0, 0] as LatLngExpression),
    [state.node]
  );

  const atSettlement = state.mode === "at_settlement";

  useEffect(() => {
    function handleKey(e: KeyboardEvent) {
      if (loading || atSettlement) return;
      const target = e.target as HTMLElement;
      if (target.tagName === "INPUT" || target.tagName === "TEXTAREA") return;

      for (const [dir, key] of Object.entries(DIR_KEYS)) {
        if (e.key === key) {
          e.preventDefault();
          doAction({ action: "move", direction: dir });
          return;
        }
      }
      if (e.key === "i") {
        e.preventDefault();
        setShowInventory((v) => !v);
      }
    }
    window.addEventListener("keydown", handleKey);
    return () => window.removeEventListener("keydown", handleKey);
  }, [doAction, loading, atSettlement]);

  // Reset vignette error when terrain or tier changes
  const terrain = state.node?.terrain;
  const tier = state.node?.regionTier;
  useEffect(() => setVignetteError(false), [terrain, tier]);

  async function openService(service: string) {
    if (!atSettlement) {
      await doAction({ action: "enter_settlement" });
    }
    setActiveService(service);
  }

  async function closeService() {
    setActiveService(null);
    if (atSettlement) {
      await doAction({ action: "leave_settlement" });
    }
  }

  if (!state.node || !state.exits) return null;

  const { node, exits, status } = state;
  const poi = node.poi;
  const isSettlement = poi?.kind === "settlement";

  // Show Market as full-screen overlay
  if (activeService === "market" && atSettlement) {
    return <MarketScreen state={state} onBack={closeService} />;
  }

  return (
    <div className="h-full flex bg-page text-primary">
      {/* Map panel */}
      <div className="flex-1 relative">
        <MapContainer
          crs={CRS.Simple}
          center={position}
          zoom={5}
          maxBounds={bounds.pad(0.1)}
          minZoom={0}
          maxZoom={MAX_ZOOM}
          style={{ height: "100%", width: "100%" }}
          zoomSnap={1}
        >
          <TileLayer
            url="/world/tiles/{z}/{x}/{y}.png"
            tileSize={256}
            noWrap
            maxNativeZoom={MAX_ZOOM}
            minZoom={0}
            maxZoom={MAX_ZOOM}
          />
          {!status.conditions["lost"] && <Marker position={position} icon={playerIcon} />}
          <MapFollower position={position} />
        </MapContainer>
      </div>

      {/* Side panel */}
      <div className="w-[360px] flex flex-col bg-page border-l border-edge overflow-y-auto">
        {/* Vignette */}
        {!vignetteError && node.terrain && node.regionTier != null && node.regionTier > 0 && (
          <img
            src={`/world/assets/vignettes/${node.terrain}/${node.terrain}_tier_${node.regionTier}_1.png`}
            alt=""
            className="w-full h-40 object-cover"
            onError={() => setVignetteError(true)}
          />
        )}

        <div className="flex flex-col gap-3 p-3">
          {/* Region name */}
          <div className="font-header text-accent text-lg">
            {node.region || node.terrain}
          </div>

          {/* Date + coordinates */}
          <div className="text-dim -mt-2">
            {formatDateTime(status.day, status.time)}
            <span className="ml-2 text-muted">({node.x}, {node.y})</span>
          </div>

          {/* Flavor text */}
          {node.description && (
            <p className="text-primary/70">{node.description}</p>
          )}

          {/* Settlement bar */}
          {isSettlement && (
            <div className="rounded-xl p-3 flex items-center gap-3" style={{ backgroundColor: "rgba(0, 0, 0, 0.4)" }}>
              <div className="flex-1 min-w-0">
                <div className="text-accent text-base truncate">
                  {poi.name || "Settlement"}
                </div>
                <div className="text-dim text-sm">
                  {TIER_LABELS[node.regionTier ?? 0] || "Settlement"}
                </div>
              </div>
              <div className="flex gap-2">
                {(state.settlement?.services ?? ["market"]).filter((s) => IMPLEMENTED_SERVICES.has(s)).map((service) => {
                  const info = SERVICE_ICONS[service];
                  if (!info) return null;
                  return (
                    <button
                      key={service}
                      onClick={() => openService(service)}
                      disabled={loading}
                      title={info.label}
                      className="w-11 h-11 bg-btn rounded-lg flex items-center justify-center
                                 hover:bg-btn-hover disabled:opacity-50 transition-colors"
                    >
                      <img
                        src={`/world/assets/icons/${info.icon}`}
                        alt={info.label}
                        className="w-6 h-6 opacity-80"
                      />
                    </button>
                  );
                })}
              </div>
            </div>
          )}

          {/* Dungeon button */}
          {poi?.kind === "dungeon" && !poi.dungeonCompleted && (
            <button
              onClick={() => doAction({ action: "enter_dungeon" })}
              disabled={loading}
              className="w-full py-2 px-3 bg-btn hover:bg-btn-hover disabled:opacity-50 text-negative rounded-lg flex items-center gap-2 transition-colors"
            >
              <img src="/world/assets/icons/dungeon-gate.svg" alt="" className="w-5 h-5" />
              <span>Enter {poi.name || "Dungeon"}</span>
            </button>
          )}

          {/* Dungeon completed */}
          {poi?.kind === "dungeon" && poi.dungeonCompleted && (
            <div className="text-muted text-center">
              {poi.name || "Dungeon"} (completed)
            </div>
          )}

          {/* Compass Rose */}
          {!atSettlement && (
            <CompassRose
              exits={exits}
              onMove={(dir) => doAction({ action: "move", direction: dir })}
              onInventory={() => setShowInventory((v) => !v)}
              disabled={loading}
            />
          )}

          {/* Conditions */}
          {Object.keys(status.conditions).length > 0 && (
            <div className="flex flex-col gap-1">
              {Object.entries(status.conditions).map(([name, stacks]) => (
                <div key={name} className="flex items-center gap-2 text-negative">
                  <img
                    src={`/world/assets/icons/${CONDITION_ICONS[name] || "sun.svg"}`}
                    alt=""
                    className="w-5 h-5"
                  />
                  <span className="capitalize">{name}{stacks > 1 ? ` x${stacks}` : ""}</span>
                </div>
              ))}
            </div>
          )}

          {/* Health bar */}
          <SegmentedBar
            label="Health"
            value={status.health}
            max={status.maxHealth}
            color="--color-stat-health"
          />

          {/* Spirits bar */}
          <SegmentedBar
            label="Spirits"
            value={status.spirits}
            max={status.maxSpirits}
            color="--color-stat-spirits"
          />
        </div>
      </div>

      {/* Inventory overlay */}
      {showInventory && state.inventory && (
        <Inventory
          inventory={state.inventory}
          onClose={() => setShowInventory(false)}
        />
      )}
    </div>
  );
}
