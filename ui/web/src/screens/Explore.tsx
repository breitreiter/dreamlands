import { useState, useMemo, useEffect, useCallback, useRef } from "react";
import { MapContainer, TileLayer, Marker, Tooltip, Polyline, useMap, useMapEvents } from "react-leaflet";
import L, { CRS, LatLngBounds, DivIcon, Icon, type LatLngExpression } from "leaflet";
import "leaflet/dist/leaflet.css";
import { loadGrid, findPath, type MapData } from "../pathfinding";
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

const FLY_DURATION = 0.6; // seconds — synced with DayNightComplication transition

function MapFollower({ position, onFlyEnd }: { position: LatLngExpression; onFlyEnd: () => void }) {
  const map = useMap();
  useEffect(() => {
    map.once("moveend", onFlyEnd);
    map.flyTo(position, map.getZoom(), { duration: FLY_DURATION });
    return () => { map.off("moveend", onFlyEnd); };
  }, [map, position]);
  return null;
}

/** Forwards map clicks to a handler. No visual output. */
function MapClickHandler({ onClick }: { onClick: (lat: number, lng: number) => void }) {
  useMapEvents({
    click(e) { onClick(e.latlng.lat, e.latlng.lng); },
  });
  return null;
}

// ── Travel animation ──────────────────────────────────

type TravelPhase = "idle" | "preview" | "animating";

const TILE_MS = 300; // milliseconds per tile during animation
const TILES_PER_DAY = 5;
const FOOD_PER_DAY = 1;
const FOOD_IDS = ["food_ration"];

function tripEstimate(tiles: number) {
  const remainder = tiles % TILES_PER_DAY;
  if (remainder === 0) return { days: tiles / TILES_PER_DAY, modifier: "" };
  if (remainder <= 2) return { days: Math.floor(tiles / TILES_PER_DAY), modifier: "just over" };
  return { days: Math.ceil(tiles / TILES_PER_DAY), modifier: "nearly" };
}

function countFood(haversack: { defId: string }[]): number {
  return haversack.filter(i => FOOD_IDS.includes(i.defId)).length;
}

/** Freeze all Leaflet interactions. */
function freezeMap(map: L.Map) {
  map.dragging.disable();
  map.touchZoom.disable();
  map.scrollWheelZoom.disable();
  map.boxZoom.disable();
  map.keyboard.disable();
  map.doubleClickZoom.disable();
}

/** Restore all Leaflet interactions. */
function unfreezeMap(map: L.Map) {
  map.dragging.enable();
  map.touchZoom.enable();
  map.scrollWheelZoom.enable();
  map.boxZoom.enable();
  map.keyboard.enable();
  map.doubleClickZoom.enable();
}

/**
 * Catmull-Rom spline interpolation through control points.
 * Returns a point on the spline for parameter t in [0, 1] within
 * the segment between p1 and p2, given neighbors p0 and p3.
 */
function catmullRom(
  p0: [number, number], p1: [number, number],
  p2: [number, number], p3: [number, number],
  t: number
): [number, number] {
  const t2 = t * t, t3 = t2 * t;
  return [
    0.5 * ((2 * p1[0]) + (-p0[0] + p2[0]) * t + (2 * p0[0] - 5 * p1[0] + 4 * p2[0] - p3[0]) * t2 + (-p0[0] + 3 * p1[0] - 3 * p2[0] + p3[0]) * t3),
    0.5 * ((2 * p1[1]) + (-p0[1] + p2[1]) * t + (2 * p0[1] - 5 * p1[1] + 4 * p2[1] - p3[1]) * t2 + (-p0[1] + 3 * p1[1] - 3 * p2[1] + p3[1]) * t3),
  ];
}

const SPLINE_SAMPLES = 8; // points per segment
const JITTER_AMOUNT = 0.35; // max perpendicular offset in latlng units (~1/3 tile)

/** Simple deterministic hash for a coordinate pair. */
function coordHash(lat: number, lng: number): number {
  let h = (lat * 73856093) ^ (lng * 19349663);
  h = ((h >>> 16) ^ h) * 0x45d9f3b;
  return (((h >>> 16) ^ h) >>> 0) / 0xffffffff; // 0..1
}

/**
 * Nudge interior points perpendicular to their travel direction.
 * Gives straight runs an organic wobble; diagonals barely change
 * since they already produce nice spline curves.
 */
function jitterPath(points: [number, number][]): [number, number][] {
  if (points.length <= 2) return points;

  const result: [number, number][] = [points[0]];
  for (let i = 1; i < points.length - 1; i++) {
    const prev = points[i - 1];
    const cur = points[i];
    const next = points[i + 1];
    // Direction through this point (lat, lng)
    const dlat = next[0] - prev[0];
    const dlng = next[1] - prev[1];
    const len = Math.sqrt(dlat * dlat + dlng * dlng);
    if (len === 0) { result.push(cur); continue; }
    // Perpendicular unit vector: rotate direction 90 degrees
    const plat = -dlng / len;
    const plng = dlat / len;
    // Deterministic signed offset from coordinate hash
    const t = coordHash(cur[0], cur[1]) * 2 - 1; // -1..1
    result.push([cur[0] + plat * t * JITTER_AMOUNT, cur[1] + plng * t * JITTER_AMOUNT]);
  }
  result.push(points[points.length - 1]);
  return result;
}

/** Generate a smooth spline through the given control points, with jitter. */
function splinePath(points: [number, number][]): [number, number][] {
  if (points.length < 2) return points;
  if (points.length === 2) return points;

  const jittered = jitterPath(points);
  const result: [number, number][] = [jittered[0]];
  for (let i = 0; i < jittered.length - 1; i++) {
    const p0 = jittered[Math.max(0, i - 1)];
    const p1 = jittered[i];
    const p2 = jittered[i + 1];
    const p3 = jittered[Math.min(jittered.length - 1, i + 2)];
    for (let s = 1; s <= SPLINE_SAMPLES; s++) {
      result.push(catmullRom(p0, p1, p2, p3, s / SPLINE_SAMPLES));
    }
  }
  return result;
}

/** Linearly interpolate marker position between path nodes. progress is 0..pathLen-1. */
function interpolateLinear(
  pts: [number, number][],
  progress: number
): [number, number] {
  if (pts.length < 2) return pts[0] ?? [0, 0];
  const idx = Math.floor(progress);
  if (idx >= pts.length - 1) return pts[pts.length - 1];
  const frac = progress - idx;
  const [lat1, lng1] = pts[idx];
  const [lat2, lng2] = pts[idx + 1];
  return [lat1 + (lat2 - lat1) * frac, lng1 + (lng2 - lng1) * frac];
}

/** Renders the travel path preview as a smooth spline. */
function PathPreview({ path }: { path: { x: number; y: number }[] }) {
  const curvePoints = useMemo(
    () => splinePath(path.map((p) => gridToLatLng(p.x, p.y) as [number, number])),
    [path]
  );
  return (
    <Polyline
      positions={curvePoints}
      pathOptions={{ color: "#D0925D", weight: 4, dashArray: "8 6", opacity: 0.7 }}
    />
  );
}

/** Pure render component — no effects, no lifecycle, no StrictMode issues. */
function TravelOverlay({
  spline,
  markerPos,
}: {
  spline: [number, number][];
  markerPos: [number, number];
}) {
  return (
    <>
      {spline.length > 1 && (
        <Polyline
          positions={spline}
          pathOptions={{ color: "#D0925D", weight: 4, dashArray: "8 6", opacity: 0.4 }}
        />
      )}
      <Marker position={markerPos} icon={playerIcon} interactive={false} zIndexOffset={2000} />
    </>
  );
}

/** Freezes the map, zooms to fit, runs the rAF animation, then resolves. */
function runTravelAnimation(
  map: L.Map,
  pts: [number, number][],
  setMarkerPos: (pos: [number, number]) => void,
): Promise<void> {
  return new Promise((resolve) => {
    freezeMap(map);
    map.fitBounds(new LatLngBounds(pts).pad(0.4), { animate: true, duration: 0.4 });

    const totalSteps = pts.length - 1;
    if (totalSteps <= 0) {
      unfreezeMap(map);
      resolve();
      return;
    }

    const totalMs = totalSteps * TILE_MS;

    // Wait for zoom to settle, then animate
    setTimeout(() => {
      const startTime = performance.now();

      function tick(now: number) {
        const elapsed = now - startTime;
        const progress = Math.max(0, Math.min((elapsed / totalMs) * totalSteps, totalSteps));

        setMarkerPos(interpolateLinear(pts, progress));

        if (progress >= totalSteps) {
          unfreezeMap(map);
          resolve();
        } else {
          requestAnimationFrame(tick);
        }
      }

      requestAnimationFrame(tick);
    }, 500);
  });
}

/** Grabs the Leaflet map instance and exposes it via ref. */
function MapRef({ mapRef }: { mapRef: React.MutableRefObject<L.Map | null> }) {
  const map = useMap();
  mapRef.current = map;
  return null;
}

const CONDITION_ICONS: Record<string, string> = {
  freezing: "mountains.svg",
  thirsty: "water-drop.svg",
  lattice_sickness: "foamy-disc.svg",
  irradiated: "foamy-disc.svg",
  poisoned: "foamy-disc.svg",
  injured: "bloody-stash.svg",
  exhausted: "tread.svg",
  lost: "compass.svg",
};

const poiPinIcon = new DivIcon({
  html: `<div style="width:24px;height:24px;display:flex;align-items:center;justify-content:center;background:#1a1a2e;border:2px solid #D0BD62;border-radius:50%;"><div style="width:14px;height:14px;background:#D0BD62;-webkit-mask:url(/world/assets/icons/black-flag.svg) center/contain no-repeat;mask:url(/world/assets/icons/black-flag.svg) center/contain no-repeat;"></div></div>`,
  className: "",
  iconSize: [24, 24],
  iconAnchor: [12, 12],
});

function latLngToGrid(lat: number, lng: number): { x: number; y: number } {
  const gx = Math.floor((lng * SCALE) / TILE_PX);
  const gy = Math.floor((-lat * SCALE) / TILE_PX);
  return { x: Math.max(0, Math.min(GRID - 1, gx)), y: Math.max(0, Math.min(GRID - 1, gy)) };
}

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
  const foodCount = (state.inventory?.haversack ?? []).filter(i => i.defId.startsWith("food_")).length;

  const vignetteSrc = node.terrain
    ? isSettlement
      ? `/world/assets/vignettes/${node.terrain}/${node.terrain}_settlement.webp`
      : node.regionTier != null && node.regionTier > 0
        ? `/world/assets/vignettes/${node.terrain}/${node.terrain}_tier_${node.regionTier}_1.webp`
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
                  <div className="absolute bottom-full right-0 mb-1 px-2 py-1 bg-black/80 text-primary text-sm rounded whitespace-nowrap opacity-0 group-hover:opacity-100 transition-opacity pointer-events-none">
                    <div>{c.name}{c.stacks > 1 ? ` x${c.stacks}` : ""}</div>
                    {c.effect && <div className="text-dim">{c.effect}</div>}
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
            <a href="/reference.html" target="_blank" rel="noopener noreferrer">
              <Button variant="secondary" size="icon-sm" title="Player Reference">
                <img src="/world/assets/icons/rule-book.svg" alt="Reference" className="w-5 h-5 opacity-80" />
              </Button>
            </a>

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
      <div className="absolute bottom-0 left-1/2 pointer-events-auto">
        {/* Food count pill — mirrors conditions bar on left */}
        <div className="flex justify-start mb-1">
          <div className="relative bg-panel rounded-2xl pl-[96px] pr-3 py-2 flex items-center gap-1 leading-none">
            <MaskedIcon
              icon="pouch-with-beads.svg"
              className="w-5 h-5"
              color={foodCount > 0 ? "#d4c9a8" : "#ff6b6b"}
            />
            <span className={`font-bold ${foodCount > 0 ? "text-primary" : "text-negative"}`}>{foodCount}</span>
          </div>
        </div>

        {/* Services bar — always rendered for consistent height, contents hidden on the road */}
        <div className={`rounded-tr-2xl py-3 pl-[96px] pr-5 ${hasRightWing ? "bg-page" : ""}`}>
          <div className={`flex items-center gap-3 min-h-8 ${hasRightWing ? "" : "invisible"}`}>
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
      </div>

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
  const { doAction, loading, gameId, clearCampReport } = useGame();
  const [showInventory, setShowInventory] = useState(false);
  const [activeService, setActiveService] = useState<string | null>(null);
  const [pendingDeliveries, setPendingDeliveries] = useState<DeliveryInfo[]>([]);
  const [discoveries, setDiscoveries] = useState<DiscoveryInfo[]>([]);
  const [traveling, setTraveling] = useState(false);
  const [gridReady, setGridReady] = useState(false);

  // Travel state
  const [travelPhase, setTravelPhase] = useState<TravelPhase>("idle");
  const [previewPath, setPreviewPath] = useState<{ x: number; y: number }[]>([]);
  const [animSpline, setAnimSpline] = useState<[number, number][]>([]);
  const [animMarkerPos, setAnimMarkerPos] = useState<[number, number]>([0, 0]);
  const mapRef = useRef<L.Map | null>(null);

  // Load map grid for pathfinding (once)
  useEffect(() => {
    fetch("/world/map.json")
      .then((r) => r.json())
      .then((data: MapData) => {
        loadGrid(data);
        setGridReady(true);
      })
      .catch(() => {});
  }, []);

  useEffect(() => {
    if (!gameId) return;
    getDiscoveries(gameId).then(setDiscoveries).catch(() => {});
  }, [gameId, state.node]);

  // Execute travel along a path: server call, then animate
  const executeTravel = useCallback(async (path: { x: number; y: number }[]) => {
    if (path.length < 2 || traveling) return;

    const pathSnapshot = [...path];

    setTraveling(true);
    setPreviewPath([]);
    setTravelPhase("idle");
    clearCampReport();

    const result = await doAction({ action: "travel", path: pathSnapshot });

    if (!result?.travel) {
      setTraveling(false);
      return;
    }

    const deferredDeliveries = result.deliveries?.length ? result.deliveries : null;

    const effectiveLen = Math.min(result.travel.stepsCompleted + 1, pathSnapshot.length);
    const rawPts = pathSnapshot.slice(0, effectiveLen).map(
      (p) => gridToLatLng(p.x, p.y) as [number, number]
    );
    const spline = rawPts.length >= 2 ? splinePath(rawPts) : [];

    setAnimSpline(spline);
    setAnimMarkerPos(rawPts[0] ?? [0, 0]);
    setTravelPhase("animating");

    if (mapRef.current && rawPts.length >= 2) {
      await runTravelAnimation(mapRef.current, rawPts, setAnimMarkerPos);
    }

    setTravelPhase("idle");
    setAnimSpline([]);
    if (deferredDeliveries) setPendingDeliveries(deferredDeliveries);
    // MapFollower will remount and flyTo; its onFlyEnd clears traveling.
  }, [traveling, doAction, clearCampReport]);

  const cancelTravel = useCallback(() => {
    if (travelPhase === "preview") {
      setTravelPhase("idle");
      setPreviewPath([]);
    }
  }, [travelPhase]);

  // All map clicks go through pathfinding
  const handleMapClick = useCallback(
    (lat: number, lng: number) => {
      if (!state.node || !gridReady || traveling || loading) return;

      const target = latLngToGrid(lat, lng);
      if (target.x === state.node.x && target.y === state.node.y) return;

      const path = findPath(state.node.x, state.node.y, target.x, target.y);
      if (!path || path.length < 2) return;

      // Short paths: execute immediately, no confirmation
      if (path.length <= 3) {
        executeTravel(path);
        return;
      }

      // Long paths: show preview for confirmation
      setPreviewPath(path);
      setTravelPhase("preview");
    },
    [state.node, gridReady, traveling, loading, executeTravel]
  );

  const position = useMemo(
    () => (state.node ? gridToLatLng(state.node.x, state.node.y) : [0, 0] as LatLngExpression),
    [state.node]
  );

  useEffect(() => {
    function handleKey(e: KeyboardEvent) {
      if (activeService != null || pendingDeliveries.length > 0) return;
      const target = e.target as HTMLElement;
      if (target.tagName === "INPUT" || target.tagName === "TEXTAREA") return;

      if (e.key === "Escape" && travelPhase === "preview") {
        e.preventDefault();
        cancelTravel();
        return;
      }

      if (loading || traveling) return;

      if (e.key === "i") {
        e.preventDefault();
        setShowInventory((v) => !v);
      }
    }
    window.addEventListener("keydown", handleKey);
    return () => window.removeEventListener("keydown", handleKey);
  }, [loading, traveling, activeService, pendingDeliveries.length, travelPhase, cancelTravel]);

  if (!state.node || !state.exits) return null;

  const isAnimating = travelPhase === "animating";
  // Hide Leaflet's own marker and MapFollower during the entire travel flow
  // (both the execute-moves phase and the animation phase)
  const hideLeafletPlayer = isAnimating || (traveling && travelPhase !== "idle");

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
        attributionControl={false}
      >
        <TileLayer
          url="/world/tiles/{z}/{x}/{y}.webp"
          tileSize={256}
          noWrap
          maxNativeZoom={MAX_ZOOM}
          minZoom={0}
          maxZoom={MAX_ZOOM}
        />

        {/* Leaflet player marker — hidden during entire travel flow */}
        {!hideLeafletPlayer && <Marker position={position} icon={playerIcon} />}

        {/* Map click handler — disabled during travel/lost */}
        {gridReady && !hideLeafletPlayer && !loading && !traveling && (
          <MapClickHandler onClick={handleMapClick} />
        )}

        {/* Path preview polyline */}
        {travelPhase === "preview" && <PathPreview path={previewPath} />}

        {/* Travel animation overlay — pure render, no effects */}
        {isAnimating && (
          <TravelOverlay spline={animSpline} markerPos={animMarkerPos} />
        )}

        {/* Exposes the Leaflet map instance for imperative animation */}
        <MapRef mapRef={mapRef} />

        {discoveries.map((d) => (
          <Marker key={`${d.x},${d.y}`} position={gridToLatLngTopRight(d.x, d.y)} icon={poiPinIcon}>
            <Tooltip direction="top" offset={[0, -12]}>{d.name}</Tooltip>
          </Marker>
        ))}

        {/* MapFollower — disabled during entire travel flow */}
        {!hideLeafletPlayer && (
          <MapFollower position={position} onFlyEnd={() => setTraveling(false)} />
        )}
      </MapContainer>

      {/* Travel confirmation dialog */}
      {travelPhase === "preview" && (() => {
        const tiles = previewPath.length - 1;
        const { days, modifier } = tripEstimate(tiles);
        const foodOnHand = countFood(state.inventory?.haversack ?? []);
        const foodNeeded = days * FOOD_PER_DAY;
        return (
          <div className="absolute top-4 left-1/2 -translate-x-1/2 z-[1000] bg-panel rounded-2xl px-6 py-4 flex flex-col items-center gap-3 min-w-[280px]">
            <span className="font-header text-[32px] text-accent">The Road</span>
            <div className="flex flex-col items-center gap-1 text-primary">
              <span>
                This journey will take{modifier ? ` ${modifier}` : ""} <strong>{days}</strong> day{days !== 1 ? "s" : ""}
              </span>
              <span>
                You'll consume <strong>{foodNeeded}</strong> food
                {foodOnHand < foodNeeded && (
                  <span className="text-negative ml-1">({foodOnHand} on hand)</span>
                )}
              </span>
            </div>
            <div className="flex gap-3 mt-1">
              <Button variant="default" size="sm" onClick={() => executeTravel(previewPath)}>
                Venture forth
              </Button>
              <Button variant="secondary" size="sm" onClick={cancelTravel}>
                Not yet
              </Button>
            </div>
          </div>
        );
      })()}

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
