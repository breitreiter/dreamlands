import { useState, useMemo, useEffect, useCallback } from "react";
import { MapContainer, TileLayer, Marker, Tooltip, useMap, useMapEvents } from "react-leaflet";
import { CRS, LatLngBounds, DivIcon, Icon, type LatLngExpression } from "leaflet";
import "leaflet/dist/leaflet.css";
import { useGame } from "../GameContext";
import Inventory from "./Inventory";
import MarketScreen from "./Market";
import BankScreen from "./Bank";
import Inn from "./Inn";
import MaskedIcon from "../components/MaskedIcon";
import { Button } from "@/components/ui/button";
import {
  AlertDialog,
  AlertDialogContent,
  AlertDialogHeader,
  AlertDialogTitle,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogAction,
} from "@/components/ui/alert-dialog";
import { formatDateTime } from "../calendar";
import { getDiscoveries } from "../api/client";
import type { GameResponse, DeliveryInfo, DiscoveryInfo } from "../api/types";

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
      '<svg xmlns="http://www.w3.org/2000/svg" width="36" height="36" viewBox="0 0 24 24">' +
        '<circle cx="12" cy="12" r="10" fill="#d0925d" stroke="#232d46" stroke-width="2"/>' +
        '<circle cx="12" cy="12" r="4" fill="#232d46"/>' +
        "</svg>"
    ),
  iconSize: [36, 36],
  iconAnchor: [18, 18],
});

function MapFollower({ position }: { position: LatLngExpression }) {
  const map = useMap();
  useEffect(() => {
    map.setView(position, map.getZoom(), { animate: true });
  }, [map, position]);
  return null;
}

const DIR_VECTORS: Record<string, { dx: number; dy: number; rotation: number }> = {
  east:  { dx: 1,  dy: 0,  rotation: 0 },
  south: { dx: 0,  dy: 1,  rotation: 90 },
  west:  { dx: -1, dy: 0,  rotation: 180 },
  north: { dx: 0,  dy: -1, rotation: 270 },
};

function DirectionIndicator({
  playerX,
  playerY,
  exits,
  loading,
  hidden,
  onMove,
}: {
  playerX: number;
  playerY: number;
  exits: string[];
  loading: boolean;
  hidden: boolean;
  onMove: (dir: string) => void;
}) {
  const [direction, setDirection] = useState<string | null>(null);

  const computeDirection = useCallback(
    (latlng: { lat: number; lng: number }) => {
      const playerPos = gridToLatLng(playerX, playerY) as [number, number];
      const dlat = latlng.lat - playerPos[0];
      const dlng = latlng.lng - playerPos[1];
      if (Math.abs(dlat) > Math.abs(dlng)) {
        return dlat > 0 ? "north" : "south";
      } else {
        return dlng > 0 ? "east" : "west";
      }
    },
    [playerX, playerY]
  );

  useMapEvents({
    mousemove(e) {
      if (loading || hidden) { setDirection(null); return; }
      const dir = computeDirection(e.latlng);
      setDirection(exits.includes(dir) ? dir : null);
    },
    mouseout() {
      setDirection(null);
    },
    click(e) {
      if (loading) return;
      const dir = computeDirection(e.latlng);
      if (exits.includes(dir)) onMove(dir);
    },
  });

  if (!direction) return null;

  const vec = DIR_VECTORS[direction];
  const pos = gridToLatLng(playerX + vec.dx, playerY + vec.dy);
  const icon = new DivIcon({
    html: `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 512 512" style="width:32px;height:32px;transform:rotate(${vec.rotation}deg);pointer-events:none;"><path d="M106.854 106.002a26.003 26.003 0 0 0-25.64 29.326c16 124 16 117.344 0 241.344a26.003 26.003 0 0 0 35.776 27.332l298-124a26.003 26.003 0 0 0 0-48.008l-298-124a26.003 26.003 0 0 0-10.136-1.994z" fill="#6bffae" stroke="#232d46" stroke-width="80" paint-order="stroke"/></svg>`,
    className: "",
    iconSize: [32, 32],
    iconAnchor: [16, 16],
  });

  return <Marker position={pos} icon={icon} interactive={false} />;
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

const poiPinIcon = new DivIcon({
  html: `<div style="width:24px;height:24px;display:flex;align-items:center;justify-content:center;background:#1a1a2e;border:2px solid #D0BD62;border-radius:50%;"><div style="width:14px;height:14px;background:#D0BD62;-webkit-mask:url(/world/assets/icons/black-flag.svg) center/contain no-repeat;mask:url(/world/assets/icons/black-flag.svg) center/contain no-repeat;"></div></div>`,
  className: "",
  iconSize: [24, 24],
  iconAnchor: [12, 12],
});

function gridToLatLngTopRight(x: number, y: number): LatLngExpression {
  const tileLng = TILE_PX / SCALE; // 2 latlng units per tile
  const [lat, lng] = gridToLatLng(x, y) as [number, number];
  return [lat + tileLng * 0.35, lng + tileLng * 0.35];
}

const IMPLEMENTED_SERVICES = new Set(["market", "bank", "inn", "chapterhouse"]);

const SERVICE_ICONS: Record<string, { icon: string; label: string }> = {
  market: { icon: "two-coins.svg", label: "Market" },
  bank: { icon: "locked-chest.svg", label: "Bank" },
  inn: { icon: "wood-cabin.svg", label: "Inn" },
  chapterhouse: { icon: "wood-cabin.svg", label: "Chapterhouse" },
};

export default function Explore({ state }: { state: GameResponse }) {
  const { doAction, loading, gameId } = useGame();
  const [showInventory, setShowInventory] = useState(false);
  const [vignetteError, setVignetteError] = useState(false);
  const [activeService, setActiveService] = useState<string | null>(null);
  const [pendingDeliveries, setPendingDeliveries] = useState<DeliveryInfo[]>([]);
  const [discoveries, setDiscoveries] = useState<DiscoveryInfo[]>([]);

  // Fetch discoveries on mount
  useEffect(() => {
    if (!gameId) return;
    getDiscoveries(gameId).then(setDiscoveries).catch(() => {});
  }, [gameId]);

  const move = useCallback(async (dir: string) => {
    const result = await doAction({ action: "move", direction: dir });
    if (result?.deliveries?.length) {
      setPendingDeliveries(result.deliveries);
    }
    // Optimistically add current node's POI to discoveries
    if (result?.node?.poi && (result.node.poi.kind === "settlement" || result.node.poi.kind === "dungeon")) {
      setDiscoveries(prev => {
        if (prev.some(d => d.x === result.node!.x && d.y === result.node!.y)) return prev;
        return [...prev, { x: result.node!.x, y: result.node!.y, kind: result.node!.poi!.kind, name: result.node!.poi!.name ?? result.node!.poi!.kind }];
      });
    }
  }, [doAction]);

  const position = useMemo(
    () => (state.node ? gridToLatLng(state.node.x, state.node.y) : [0, 0] as LatLngExpression),
    [state.node]
  );

  useEffect(() => {
    function handleKey(e: KeyboardEvent) {
      if (loading || activeService != null || pendingDeliveries.length > 0) return;
      const target = e.target as HTMLElement;
      if (target.tagName === "INPUT" || target.tagName === "TEXTAREA") return;

      for (const [dir, key] of Object.entries(DIR_KEYS)) {
        if (e.key === key) {
          e.preventDefault();
          move(dir);
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
  }, [move, loading, activeService, pendingDeliveries.length]);

  // Reset vignette error when terrain or tier changes
  const terrain = state.node?.terrain;
  const tier = state.node?.regionTier;
  useEffect(() => setVignetteError(false), [terrain, tier]);

  function openService(service: string) {
    setActiveService(service);
  }

  function closeService() {
    setActiveService(null);
  }

  if (!state.node || !state.exits) return null;

  const { node, exits, status } = state;
  const poi = node.poi;
  const isSettlement = poi?.kind === "settlement";
  const isLost = status.conditions.some((c) => c.id === "lost");

  // Show Market as full-screen
  if (activeService === "market") {
    return <MarketScreen state={state} onBack={closeService} />;
  }

  // Show Bank as full-screen
  if (activeService === "bank") {
    return <BankScreen state={state} onBack={closeService} />;
  }

  // Show Inn / Chapterhouse as full-screen
  if (activeService === "inn" || activeService === "chapterhouse") {
    return <Inn state={state} isChapterhouse={activeService === "chapterhouse"} onBack={closeService} />;
  }

  // Show Inventory as full-screen
  if (showInventory) {
    return <Inventory state={state} onClose={() => setShowInventory(false)} />;
  }

  return (
    <div className="h-full flex bg-page text-primary">
      {/* Map panel */}
      <div className="flex-1 relative isolate">
        <MapContainer
          crs={CRS.Simple}
          center={position}
          zoom={MAX_ZOOM}
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
          {!isLost && <Marker position={position} icon={playerIcon} />}
          <DirectionIndicator
            playerX={node.x}
            playerY={node.y}
            exits={exits.map((e) => e.direction)}
            loading={loading}
            hidden={isLost}
            onMove={move}
          />
          {discoveries.map((d) => (
            <Marker key={`${d.x},${d.y}`} position={gridToLatLngTopRight(d.x, d.y)} icon={poiPinIcon}>
              <Tooltip direction="top" offset={[0, -12]}>{d.name}</Tooltip>
            </Marker>
          ))}
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

        <div className="flex-1 flex flex-col gap-3 p-3 min-h-0">
          {/* Region name + inventory */}
          <div className="flex items-center justify-between">
            <div className="font-header text-accent text-lg">
              {node.region || node.terrain}
            </div>
            <Button variant="secondary" size="icon-sm" onClick={() => setShowInventory((v) => !v)} disabled={loading} title="Inventory (I)">
              <img src="/world/assets/icons/backpack.svg" alt="Inventory" className="w-5 h-5 opacity-80" />
            </Button>
          </div>

          {/* Date + coordinates */}
          <div className="text-dim -mt-2">
            {formatDateTime(status.day, status.time)}
            <span className="ml-2 text-muted">({node.x}, {node.y})</span>
          </div>

          {/* POI controls — fixed-height slot so compass never shifts */}
          <div className="h-[84px] flex items-center">
            {isSettlement && (
              <div className="w-full rounded-xl p-3 flex items-center gap-3" style={{ backgroundColor: "rgba(0, 0, 0, 0.4)" }}>
                <div className="flex-1 min-w-0">
                  <div className="text-accent text-base truncate">
                    {poi.name || "Settlement"}
                  </div>
                  <div className="text-dim text-sm">
                    {TIER_LABELS[node.regionTier ?? 0] || "Settlement"}
                  </div>
                </div>
                <div className="flex gap-2">
                  {(poi.services ?? []).filter((s) => IMPLEMENTED_SERVICES.has(s)).map((service) => {
                    const info = SERVICE_ICONS[service];
                    if (!info) return null;
                    return (
                      <Button key={service} variant="secondary" size="icon-lg" onClick={() => openService(service)} disabled={loading} title={info.label}>
                        <img
                          src={`/world/assets/icons/${info.icon}`}
                          alt={info.label}
                          className="w-6 h-6 opacity-80"
                        />
                      </Button>
                    );
                  })}
                </div>
              </div>
            )}
            {poi?.kind === "dungeon" && !poi.dungeonCompleted && (
              <Button variant="destructive" className="w-full" onClick={() => doAction({ action: "enter_dungeon" })} disabled={loading}>
                <img src="/world/assets/icons/dungeon-gate.svg" alt="" className="w-5 h-5" />
                <span>Enter {poi.name || "Dungeon"}</span>
              </Button>
            )}
            {poi?.kind === "dungeon" && poi.dungeonCompleted && (
              <div className="text-muted text-center w-full">
                {poi.name || "Dungeon"} (completed)
              </div>
            )}
          </div>

          {/* Conditions — flex-1 fills remaining space, items align to bottom */}
          <div className="flex-1 flex flex-col justify-end gap-1">
            {status.conditions.map((c) => (
              <div key={c.id} className="flex items-center gap-2 text-negative">
                <img
                  src={`/world/assets/icons/${CONDITION_ICONS[c.id] || "sun.svg"}`}
                  alt=""
                  className="w-5 h-5"
                />
                <span>{c.name}{c.stacks > 1 ? ` x${c.stacks}` : ""}</span>
              </div>
            ))}
          </div>

          {/* Health & Spirits KPI blocks */}
          <div className="flex gap-2">
            <div className="flex-1 rounded-lg px-3 py-2" style={{ backgroundColor: "rgba(0, 0, 0, 0.4)" }}>
              <div className="flex items-center gap-2">
                <MaskedIcon icon="heart-plus.svg" className="w-7 h-7" color="#d4c9a8" />
                <span className="text-2xl font-bold text-primary">{status.health}</span>
                <span className="text-xl text-dim">/ {status.maxHealth}</span>
              </div>
              <div className="text-xs text-muted mt-0.5">Health</div>
            </div>
            <div className="flex-1 rounded-lg px-3 py-2" style={{ backgroundColor: "rgba(0, 0, 0, 0.4)" }}>
              <div className="flex items-center gap-2">
                <MaskedIcon icon="sensuousness.svg" className="w-7 h-7" color="#d4c9a8" />
                <span className={`text-2xl font-bold ${status.conditions.some(c => c.id === "disheartened") ? "text-negative" : "text-primary"}`}>{status.spirits}</span>
                <span className="text-xl text-dim">/ {status.maxSpirits}</span>
              </div>
              <div className="text-xs text-muted mt-0.5">Spirits</div>
            </div>
          </div>
        </div>
      </div>

      {/* Haul delivery dialog */}
      <AlertDialog open={pendingDeliveries.length > 0}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle className="font-header text-accent text-[32px]">
              Delivery Complete
            </AlertDialogTitle>
          </AlertDialogHeader>
          <div className="flex flex-col gap-4">
            {pendingDeliveries.map((d, i) => (
              <div key={i} className="flex flex-col gap-2">
                <div className="flex items-center gap-2">
                  <MaskedIcon icon="wooden-crate.svg" className="w-6 h-6" color="#D0BD62" />
                  <span className="text-accent font-bold">{d.name}</span>
                </div>
                {d.flavor && (
                  <AlertDialogDescription className="text-primary leading-relaxed">
                    {d.flavor}
                  </AlertDialogDescription>
                )}
                <div className="flex items-center gap-2 text-accent">
                  <MaskedIcon icon="two-coins.svg" className="w-5 h-5" color="#D0BD62" />
                  <span>+{d.payout} gold</span>
                </div>
              </div>
            ))}
          </div>
          <AlertDialogFooter>
            <AlertDialogAction onClick={() => setPendingDeliveries([])}>
              Close
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>

    </div>
  );
}
