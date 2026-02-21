# Map Tiling

## Problem

Production map is 12,800 × 12,800px (100×100 nodes × 128px/tile). Decoded in-browser, that's ~655MB of GPU memory — well beyond what any browser will handle. Even desktop browsers cap around 256–512MB for a single decoded image. CSS pan/zoom is not viable at this size.

## Decision: Leaflet + slippy map tiles

**Leaflet** (~42KB) with `L.CRS.Simple` — a Cartesian coordinate system for non-geographic tiled images. Standard, well-documented, huge ecosystem.

Why Leaflet over alternatives:
- **vs. OpenSeadragon**: OpenSeadragon is purpose-built for deep zoom images and slightly cleaner for pure viewing, but Leaflet gives us markers/popups/overlays for free. We'll want those for POI annotations, the exploration graph overlay, and coordinate mapping.
- **vs. CSS pan/zoom**: No level-of-detail. Full bitmap decode is too heavy for mobile at our sizes.
- **vs. deck.gl**: ~300KB, WebGL overkill. No.

## Tile format

**256×256 PNG**, `{z}/{x}/{y}.png` directory layout (slippy map standard).

- PNG over WebP: lossless, transparency for edge tiles, universal. WebP is a drop-in upgrade later if size matters.
- PNG over JPEG: our maps have crisp lines and stipple detail. JPEG artifacts are visible and ugly.
- 256×256 is what Leaflet defaults to and what all tooling expects.

## Zoom levels

Each zoom level doubles resolution. For a 12,800 × 12,800 image:

| Zoom | Tile grid | Tiles | Approx scale |
|------|-----------|-------|--------------|
| 0    | 1×1       | 1     | Thumbnail    |
| 1    | 2×2       | 4     | ~1/25        |
| 2    | 4×4       | 16    | ~1/13        |
| 3    | 8×8       | 64    | ~1/6         |
| 4    | 16×16     | 256   | ~1/3         |
| 5    | 32×32     | 1,024 | ~2/3         |
| 6    | 64×64     | 4,096 | ~1:1 (13,056px covers 12,800) |

6 zoom levels. ~5,461 tiles total at ~30KB average ≈ ~160MB on disk. Only 12-20 tiles are loaded at any time.

## Tile generation: SkiaSharp post-processing

Already in mapgen, zero new dependencies. Add a tile slicing step after rendering:

1. Take the rendered `SKBitmap` (already in memory from rendering)
2. For each zoom level 0–5:
   - Resize to `256 × 2^z` on the long axis (use `SKFilterQuality.High` — Lanczos downscale, no aliasing)
   - Walk the grid, extract 256×256 regions
   - Encode each to PNG, write to `{outputDir}/{z}/{x}/{y}.png`
3. Edge tiles: fill remainder with transparent. Leaflet handles partial tiles fine.

~50 lines of code. PNG encoding ~5,400 tiles will take longer than the old 1,300 — expect 10-30 seconds depending on machine. Still a one-time build cost per map.

**Gotcha — memory**: a 12,800×12,800 RGBA bitmap is ~655MB. This is the big concern. The full-res source must be in memory for slicing, but avoid holding any other full-res copy simultaneously. Resize per zoom level creates progressively smaller bitmaps; dispose each immediately after slicing.

If 655MB is too much for target build environments, the alternative is **rendering tiles directly** — have the renderer target viewports per zoom level instead of producing a single huge intermediate. This requires the render pipeline to support region rendering (translate the canvas origin, clip to tile bounds). More work, but avoids the giant intermediate bitmap entirely. Worth considering if mapgen will run in constrained environments (CI, Azure Functions).

## Leaflet setup (sketch)

```tsx
import L from 'leaflet';
import 'leaflet/dist/leaflet.css';

const map = L.map('map', {
  crs: L.CRS.Simple,
  minZoom: 0,
  maxZoom: 6,
});

L.tileLayer('/tiles/{z}/{x}/{y}.png', {
  tileSize: 256,
  noWrap: true,
  maxNativeZoom: 6,
}).addTo(map);

// Constrain to image bounds (in CRS.Simple, 1 unit = 1 pixel at max zoom)
const bounds: L.LatLngBoundsExpression = [[0, 0], [12800, 12800]];
map.fitBounds(bounds);
map.setMaxBounds(bounds);
```

Note: `L.CRS.Simple` has y-up (math convention). Tile row numbering is y-down by default. If tiles look flipped, add `tms: true` to the tile layer options. In practice, slicing top-to-bottom and using the default works.

## What this enables

- **POI markers**: server provides node pixel positions → Leaflet markers with popups for settlements, dungeons, encounters
- **Exploration overlay**: `L.polyline` or `L.polygon` layers tracing visited nodes/edges
- **Current position**: animated marker showing player location
- **Mobile**: smooth pinch-zoom, only loads visible tiles, low memory

## Integration with PLAN.md

This replaces the vague "Map image: served by the server" in PLAN.md. The server serves the tile directory (static files) plus a metadata endpoint with image dimensions and node→pixel coordinate mappings. The React app mounts a Leaflet instance and overlays game state.

## Build order

1. Add tile slicing to mapgen (SkiaSharp post-process after `ImageRenderer.Render`)
2. Scaffold Leaflet component in React app
3. Static file serving for tile directory (dev: Vite proxy or direct serve; prod: blob storage or CDN)
4. POI marker layer (once server provides node coordinates)
5. Exploration graph overlay (once exploration tracking exists)
