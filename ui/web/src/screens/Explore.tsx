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
import DayNightComplication from "../components/DayNightComplication";
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
import { getDiscoveries, getNotices } from "../api/client";
import type { GameResponse, DeliveryInfo, DiscoveryInfo, EncounterSummaryInfo } from "../api/types";

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
  iconUrl: "/world/assets/ui/player_pin.svg",
  iconSize: [31, 58],
  iconAnchor: [15, 58],
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
  freezing: "mountains.svg",
  thirsty: "water-drop.svg",
  swamp_fever: "foamy-disc.svg",
  gut_worms: "foamy-disc.svg",
  irradiated: "foamy-disc.svg",
  poisoned: "foamy-disc.svg",
  injured: "bloody-stash.svg",
  hungry: "pouch-with-beads.svg",
  exhausted: "tread.svg",
  lost: "compass.svg",
  disheartened: "sensuousness.svg",
};

const poiPinIcon = new DivIcon({
  html: `<div style="width:24px;height:24px;display:flex;align-items:center;justify-content:center;background:#1a1a2e;border:2px solid #D0BD62;border-radius:50%;"><div style="width:14px;height:14px;background:#D0BD62;-webkit-mask:url(/world/assets/icons/black-flag.svg) center/contain no-repeat;mask:url(/world/assets/icons/black-flag.svg) center/contain no-repeat;"></div></div>`,
  className: "",
  iconSize: [24, 24],
  iconAnchor: [12, 12],
});

function gridToLatLngTopRight(x: number, y: number): LatLngExpression {
  const tileLng = TILE_PX / SCALE;
  const [lat, lng] = gridToLatLng(x, y) as [number, number];
  return [lat + tileLng * 0.35, lng + tileLng * 0.35];
}

const IMPLEMENTED_SERVICES = new Set(["market", "bank", "inn", "chapterhouse", "notices"]);

const SERVICE_ICONS: Record<string, { icon: string; label: string }> = {
  market: { icon: "two-coins.svg", label: "Market" },
  bank: { icon: "locked-chest.svg", label: "Bank" },
  inn: { icon: "wood-cabin.svg", label: "Inn" },
  chapterhouse: { icon: "byzantin-temple.svg", label: "Chapterhouse" },
  notices: { icon: "tied-scroll.svg", label: "Notices" },
};

function InstrumentCluster({
  state,
  loading,
  onOpenInventory,
  onOpenService,
  onEnterDungeon,
}: {
  state: GameResponse;
  loading: boolean;
  onOpenInventory: () => void;
  onOpenService: (service: string) => void;
  onEnterDungeon: () => void;
}) {
  const { node, status } = state;
  if (!node) return null;

  const poi = node.poi;
  const isSettlement = poi?.kind === "settlement";
  const isDungeon = poi?.kind === "dungeon";
  const hasRightWing = isSettlement || isDungeon;

  const healthLow = status.health < status.maxHealth * 0.5;
  const spiritsLow = status.spirits < status.maxSpirits * 0.5;

  const vignetteSrc = node.terrain
    ? isSettlement
      ? `/world/assets/vignettes/${node.terrain}/${node.terrain}_settlement.png`
      : node.regionTier != null && node.regionTier > 0
        ? `/world/assets/vignettes/${node.terrain}/${node.terrain}_tier_${node.regionTier}_1.png`
        : null
    : null;

  return (
    <div className="absolute bottom-0 left-1/2 -translate-x-1/2 z-[1000] pointer-events-none">
      {/* Left wing */}
      <div className="absolute bottom-0 right-1/2 pointer-events-auto">
        {/* Conditions bar — above the main bar */}
        {status.conditions.length > 0 && (
          <div className="flex justify-end mb-1">
            <div className="bg-panel rounded-2xl pl-3 pr-[96px] py-2 flex items-center gap-2 leading-none">
              {status.conditions.map((c) => (
                <div key={c.id} className="relative group">
                  <MaskedIcon
                    icon={CONDITION_ICONS[c.id] || "sun.svg"}
                    className="w-5 h-5"
                    color="#ff6b6b"
                  />
                  <div className="absolute bottom-full left-1/2 -translate-x-1/2 mb-1 px-2 py-1 bg-black/80 text-primary text-sm rounded whitespace-nowrap opacity-0 group-hover:opacity-100 transition-opacity pointer-events-none">
                    {c.name}{c.stacks > 1 ? ` x${c.stacks}` : ""}
                  </div>
                </div>
              ))}
            </div>
          </div>
        )}

        {/* Stats bar */}
        <div className="bg-page rounded-tl-2xl py-3 pl-5 pr-[96px]">
          <div className="flex items-center gap-3">
            <Button variant="secondary" size="icon-sm" onClick={() => onOpenInventory()} disabled={loading} title="Inventory (I)">
              <img src="/world/assets/icons/backpack.svg" alt="Inventory" className="w-5 h-5 opacity-80" />
            </Button>

            <div className="flex items-center gap-1">
            <MaskedIcon icon="sensuousness.svg" className="w-5 h-5" color="#d4c9a8" />
            <span className={`font-bold ${spiritsLow ? "text-negative" : "text-primary"}`}>{status.spirits}</span>
            <span className="text-dim opacity-60">/{status.maxSpirits}</span>
          </div>

          <div className="flex items-center gap-1">
            <MaskedIcon icon="heart-plus.svg" className="w-5 h-5" color="#d4c9a8" />
            <span className={`font-bold ${healthLow ? "text-negative" : "text-primary"}`}>{status.health}</span>
            <span className="text-dim opacity-60">/{status.maxHealth}</span>
          </div>
          </div>
        </div>
      </div>

      {/* Right wing */}
      {hasRightWing && (
        <div className="absolute bottom-0 left-1/2 bg-page rounded-tr-2xl py-3 pl-[96px] pr-5 pointer-events-auto">
          <div className="flex items-center gap-3">
            {isSettlement && (
              <>
                <span className="text-accent font-bold whitespace-nowrap">
                  {poi!.name || "Settlement"}
                </span>
                <div className="flex gap-1">
                  {(poi!.services ?? []).filter((s) => IMPLEMENTED_SERVICES.has(s)).map((service) => {
                    const info = SERVICE_ICONS[service];
                    if (!info) return null;
                    return (
                      <Button key={service} variant="secondary" size="icon-sm" onClick={() => onOpenService(service)} disabled={loading} title={info.label}>
                        <img src={`/world/assets/icons/${info.icon}`} alt={info.label} className="w-5 h-5 opacity-80" />
                      </Button>
                    );
                  })}
                </div>
              </>
            )}
            {isDungeon && !poi!.dungeonCompleted && (
              <>
                <span className="text-accent font-bold whitespace-nowrap">
                  {poi!.name || "Dungeon"}
                </span>
                <Button variant="secondary" size="icon-sm" onClick={onEnterDungeon} disabled={loading} title="Enter dungeon">
                  <img src="/world/assets/icons/dungeon-gate.svg" alt="Enter dungeon" className="w-5 h-5 opacity-80" />
                </Button>
              </>
            )}
            {isDungeon && poi!.dungeonCompleted && (
              <span className="text-muted whitespace-nowrap">
                {poi!.name || "Dungeon"} (completed)
              </span>
            )}
          </div>
        </div>
      )}

      {/* Vignette + complication — centered, sits on top of the bar */}
      <div className="relative z-10 pointer-events-auto">
        <div className="w-[160px] h-[160px] rounded-full border-5 border-page overflow-hidden bg-black/60">
          {vignetteSrc && (
            <img src={vignetteSrc} alt="" className="w-full h-full object-cover" />
          )}
        </div>
        <DayNightComplication time={status.time} />
      </div>
    </div>
  );
}

function NoticesScreen({ gameId, onBack }: { gameId: string; onBack: () => void }) {
  const { doAction, loading } = useGame();
  const [notices, setNotices] = useState<EncounterSummaryInfo[]>([]);
  const [fetching, setFetching] = useState(true);

  useEffect(() => {
    getNotices(gameId).then(r => setNotices(r.encounters)).catch(() => {}).finally(() => setFetching(false));
  }, [gameId]);

  return (
    <div className="h-full flex items-center justify-center bg-page text-primary">
      <div className="w-full max-w-[520px] px-6">
        <h1 className="font-header text-[32px] text-accent mb-6">Notices</h1>

        {fetching ? (
          <p className="text-dim">Loading...</p>
        ) : notices.length > 0 ? (
          <div className="space-y-4">
            {notices.map((enc) => (
              <button
                key={enc.id}
                onClick={async () => {
                  await doAction({ action: "start_encounter", encounterId: enc.id });
                  onBack();
                }}
                disabled={loading}
                className="w-full text-left flex items-start gap-3
                           disabled:text-muted transition-colors group cursor-pointer"
              >
                <img
                  src="/world/assets/icons/sun.svg"
                  alt=""
                  className="w-4 h-4 mt-1 shrink-0 opacity-70 group-hover:opacity-100
                             transition-opacity"
                />
                <span className="font-bold text-action group-hover:text-action-hover transition-colors">
                  {enc.title}
                </span>
              </button>
            ))}
          </div>
        ) : (
          <p className="text-dim">No notices posted here.</p>
        )}

        <div className="mt-8">
          <Button variant="secondary" onClick={onBack}>
            Back
          </Button>
        </div>
      </div>
    </div>
  );
}

export default function Explore({ state }: { state: GameResponse }) {
  const { doAction, loading, gameId } = useGame();
  const [showInventory, setShowInventory] = useState(false);
  const [activeService, setActiveService] = useState<string | null>(null);
  const [pendingDeliveries, setPendingDeliveries] = useState<DeliveryInfo[]>([]);
  const [discoveries, setDiscoveries] = useState<DiscoveryInfo[]>([]);

  useEffect(() => {
    if (!gameId) return;
    getDiscoveries(gameId).then(setDiscoveries).catch(() => {});
  }, [gameId]);

  const move = useCallback(async (dir: string) => {
    const result = await doAction({ action: "move", direction: dir });
    if (result?.deliveries?.length) {
      setPendingDeliveries(result.deliveries);
    }
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

  if (!state.node || !state.exits) return null;

  const { node, exits } = state;
  const isLost = state.status.conditions.some((c) => c.id === "lost");

  if (activeService === "market") return <MarketScreen state={state} onBack={() => setActiveService(null)} />;
  if (activeService === "bank") return <BankScreen state={state} onBack={() => setActiveService(null)} />;
  if (activeService === "inn" || activeService === "chapterhouse")
    return <Inn state={state} isChapterhouse={activeService === "chapterhouse"} onBack={() => setActiveService(null)} />;
  if (activeService === "notices")
    return <NoticesScreen gameId={gameId!} onBack={() => setActiveService(null)} />;
  if (showInventory) return <Inventory state={state} onClose={() => setShowInventory(false)} />;

  return (
    <div className="h-full relative bg-page text-primary">
      {/* Map — full screen */}
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

      {/* Instrument cluster — bottom center overlay */}
      <InstrumentCluster
        state={state}
        loading={loading}
        onOpenInventory={() => setShowInventory(true)}
        onOpenService={setActiveService}
        onEnterDungeon={() => doAction({ action: "enter_dungeon" })}
      />

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
