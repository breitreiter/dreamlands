# POI Map Markers

## Goal

Show name plaques for discovered POIs on the Leaflet map so players can plan
trips — especially important for the trade system where you need to see distant
settlements.

## Current State

- Client receives POI data **only for the current node** (`GameResponse.node.poi`).
- `PlayerState.VisitedNodes` tracks every tile the player has stepped on (encoded x,y pairs).
- `map.json` has full POI data for all ~10,000 nodes, but the client never loads it.
- The map is tile-only — no Leaflet markers except the player pin and direction indicator.

## Plan

### 1. Server: include visited POIs in GameResponse

Add a `visitedPois` field to the game response. On every response, the server
filters the map's POI list against `PlayerState.VisitedNodes` and returns
matching entries.

```
VisitedPoiInfo {
  x: number
  y: number
  kind: string        // "settlement" | "dungeon" | "landmark"
  name: string | null
}
```

- Skip `encounter` kind — those are anonymous slots, no name to show.
- Skip nodes where `poi.name` is null.
- Sent on every response alongside `node` and `exits`. Lightweight — there are
  maybe 30-40 named POIs on the whole map; the visited subset is smaller.

Server change: `BuildVisitedPois(map, playerState)` in `Program.cs`, added to
the response builder. One LINQ pass over `VisitedNodes`, map lookup, filter.

### 2. Client types

Add `VisitedPoiInfo` interface to `types.ts`. Add `visitedPois?: VisitedPoiInfo[]`
to `GameResponse`.

### 3. Client rendering

In `Explore.tsx`, render a Leaflet `Marker` for each visited POI using a
`DivIcon` with the POI name as a text label. Use `Tooltip` set to `permanent`
for always-visible name plaques, or regular hover tooltips if permanent feels
too noisy.

- Position via existing `gridToLatLng(poi.x, poi.y)`.
- Style by kind: settlements get one look, dungeons another.
- The player's current node POI is already shown in the sidebar — the map marker
  should still appear for consistency but doesn't need special treatment.
- Markers should have low `zIndexOffset` so they don't cover the player pin.
- Consider hiding markers at low zoom levels to avoid clutter on the full map view.

### 4. GameContext merge handling

`visitedPois` should use the same partial-merge pattern as other response fields —
only overwrite when the server sends it, don't clobber with null on
inventory-only responses.

## Not in scope (yet)

- **Fog of war / unexplored overlay** — would be nice but separate feature.
- **Nearby-but-unvisited POIs** — could show "?" markers for POIs within N tiles
  of visited nodes. Deferred.
- **POI icons on the map** — just name text for now. Icons can come later.

## Files touched

- `server/GameServer/Program.cs` — response builder
- `ui/web/src/api/types.ts` — new interface
- `ui/web/src/screens/Explore.tsx` — marker rendering
- `ui/web/src/GameContext.tsx` — merge logic (if needed)
