import { useState, useMemo, useEffect } from "react";
import { MapContainer, TileLayer, Marker, useMap } from "react-leaflet";
import { CRS, LatLngBounds, Icon, type LatLngExpression } from "leaflet";
import "leaflet/dist/leaflet.css";
import { useGame } from "../GameContext";
import StatusBar from "./StatusBar";
import Inventory from "./Inventory";
import type { GameResponse } from "../api/types";

// Map constants — 100x100 grid at 128px/tile = 12800px source.
// At max zoom 6: 1 latlng = 64px, so 12800/64 = 200 units.
const TILE_PX = 128;
const GRID = 100;
const MAX_ZOOM = 6;
const SCALE = Math.pow(2, MAX_ZOOM); // 64
const MAP_SIZE = (GRID * TILE_PX) / SCALE; // 200
const bounds = new LatLngBounds([0, 0], [-MAP_SIZE, MAP_SIZE]);

// Convert grid (x,y) to leaflet latlng. Y is inverted.
function gridToLatLng(x: number, y: number): LatLngExpression {
  return [-(y * TILE_PX + TILE_PX / 2) / SCALE, (x * TILE_PX + TILE_PX / 2) / SCALE];
}

const playerIcon = new Icon({
  iconUrl:
    "data:image/svg+xml," +
    encodeURIComponent(
      '<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24">' +
        '<circle cx="12" cy="12" r="10" fill="#f59e0b" stroke="#1c1917" stroke-width="2"/>' +
        '<circle cx="12" cy="12" r="4" fill="#1c1917"/>' +
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

const TERRAIN_EMOJI: Record<string, string> = {
  plains: "",
  forest: "",
  mountains: "",
  swamp: "",
  scrub: "",
  lake: "",
};

const DIR_KEYS: Record<string, string> = {
  north: "w",
  south: "s",
  east: "d",
  west: "a",
};

const DIR_LABELS: Record<string, string> = {
  north: "North",
  south: "South",
  east: "East",
  west: "West",
};

const DIR_ARROWS: Record<string, string> = {
  north: "\u2191",
  south: "\u2193",
  east: "\u2192",
  west: "\u2190",
};

export default function Explore({ state }: { state: GameResponse }) {
  const { doAction, loading } = useGame();
  const [showInventory, setShowInventory] = useState(false);

  const position = useMemo(
    () => (state.node ? gridToLatLng(state.node.x, state.node.y) : [0, 0] as LatLngExpression),
    [state.node]
  );

  useEffect(() => {
    function handleKey(e: KeyboardEvent) {
      if (loading) return;
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
  }, [doAction, loading]);

  if (!state.node || !state.exits) return null;

  const { node, exits, status } = state;
  const poi = node.poi;

  return (
    <div className="h-full flex flex-col bg-stone-900 text-stone-100">
      <StatusBar status={status} />

      <div className="flex-1 flex overflow-hidden">
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
            <Marker position={position} icon={playerIcon} />
            <MapFollower position={position} />
          </MapContainer>
        </div>

        {/* Info panel */}
        <div className="w-80 flex flex-col bg-stone-800 border-l border-stone-700 overflow-y-auto">
          {/* Location */}
          <div className="p-3 border-b border-stone-700">
            <div className="text-amber-200 font-medium">
              {TERRAIN_EMOJI[node.terrain] || ""}{" "}
              {node.region || node.terrain}
            </div>
            <div className="text-xs text-stone-400 mt-1">
              ({node.x}, {node.y})
              {node.regionTier && ` \u2014 Tier ${node.regionTier}`}
            </div>
            {node.description && (
              <p className="text-sm text-stone-300 mt-2">{node.description}</p>
            )}
          </div>

          {/* POI */}
          {poi && (
            <div className="p-3 border-b border-stone-700">
              {poi.kind === "settlement" && (
                <button
                  onClick={() => doAction({ action: "enter_settlement" })}
                  disabled={loading}
                  className="w-full py-2 bg-amber-700 hover:bg-amber-600 disabled:bg-stone-700 text-sm transition-colors"
                >
                  Enter {poi.name || "Settlement"}
                </button>
              )}
              {poi.kind === "dungeon" && !poi.dungeonCompleted && (
                <button
                  onClick={() => doAction({ action: "enter_dungeon" })}
                  disabled={loading}
                  className="w-full py-2 bg-red-800 hover:bg-red-700 disabled:bg-stone-700 text-sm transition-colors"
                >
                  Enter {poi.name || "Dungeon"}
                </button>
              )}
              {poi.kind === "dungeon" && poi.dungeonCompleted && (
                <div className="text-stone-500 text-sm text-center">
                  {poi.name || "Dungeon"} (completed)
                </div>
              )}
              {poi.kind === "landmark" && (
                <div className="text-stone-400 text-sm">
                  {poi.name || "Landmark"}
                </div>
              )}
              {poi.kind === "encounter" && (
                <div className="text-stone-400 text-sm">
                  Encounter zone
                </div>
              )}
            </div>
          )}

          {/* Navigation */}
          <div className="p-3 border-b border-stone-700">
            <div className="text-xs text-stone-400 mb-2">Move (WASD)</div>
            <div className="grid grid-cols-3 gap-1 w-36 mx-auto">
              <div />
              {renderDirButton("north")}
              <div />
              {renderDirButton("west")}
              <div />
              {renderDirButton("east")}
              <div />
              {renderDirButton("south")}
              <div />
            </div>
          </div>

          {/* Skills */}
          <div className="p-3 border-b border-stone-700">
            <div className="text-xs text-stone-400 mb-1">Skills</div>
            <div className="grid grid-cols-2 gap-x-4 gap-y-0.5 text-xs">
              {Object.entries(status.skills).map(([skill, level]) => (
                <div key={skill} className="flex justify-between">
                  <span className="text-stone-300 capitalize">{skill}</span>
                  <span className="text-stone-100">{level}</span>
                </div>
              ))}
            </div>
          </div>

          {/* Inventory toggle */}
          <div className="p-3">
            <button
              onClick={() => setShowInventory((v) => !v)}
              className="w-full py-2 bg-stone-700 hover:bg-stone-600 text-sm transition-colors"
            >
              Inventory (I)
            </button>
          </div>
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

  function renderDirButton(dir: string) {
    const exit = exits!.find((e) => e.direction === dir);
    return (
      <button
        key={dir}
        onClick={() => doAction({ action: "move", direction: dir })}
        disabled={!exit || loading}
        className={`py-2 text-center text-sm font-medium transition-colors
          ${
            exit
              ? "bg-stone-700 hover:bg-stone-600 text-stone-100"
              : "bg-stone-800 text-stone-600 cursor-not-allowed"
          }`}
        title={exit ? `${DIR_LABELS[dir]} (${exit.terrain}${exit.poi ? ` - ${exit.poi}` : ""})` : ""}
      >
        {DIR_ARROWS[dir]}
      </button>
    );
  }
}
