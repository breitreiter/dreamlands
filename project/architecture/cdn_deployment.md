# CDN Deployment Plan — Cloudflare R2

## Goal

Serve all static game assets (map tiles, icons, portraits, equipment, vignettes) from
Cloudflare R2 so the web client doesn't depend on a local dev server for images.

## Why R2

- S3-compatible object storage with **zero egress fees**
- Free tier covers us easily: 10 GB storage, 10M reads/month
- Public bucket gets a CDN-backed URL automatically via Cloudflare's edge network
- Custom domain support (`assets.dreamlands.whatever`)
- Existing `worlds/production/push.sh` already has a working R2 sync skeleton

Pages has a hard 20,000-file limit per deployment — a tile pyramid blows past that.
Azure Blob behind CF CDN adds egress costs and extra moving parts for no benefit.

## Architecture

```
React app (CF Pages or wherever)
  ├── /world/map.json         ← bundled with app or fetched from server
  └── tile/asset URLs point to ──→  assets.dreamlands.com  (CF custom domain)
                                        │
                                   Cloudflare R2 bucket: "game-assets"
                                        │
                                   worlds/production/tiles/{z}/{x}/{y}.png
                                   worlds/production/assets/icons/*.svg
                                   worlds/production/assets/portraits/*.png
                                   worlds/production/assets/equipment/*.png
                                   worlds/production/assets/vignettes/*.png
```

## Bucket Setup

1. Create R2 bucket `game-assets` in Cloudflare dashboard
2. Enable **Public Access** on the bucket
3. Attach custom domain (e.g. `assets.dreamlands.com`) — domain must be in the same CF account
4. Generate R2 API token with read/write on the bucket

## Upload Tooling

The existing `worlds/production/push.sh` uses `aws s3 sync`. This works fine.
Alternative: `rclone sync` (better progress output, same S3 protocol underneath).

### Environment Variables

```bash
CF_ACCOUNT_ID=...
R2_ACCESS_KEY_ID=...
R2_SECRET_ACCESS_KEY=...
```

### Sync Command (existing push.sh approach)

```bash
aws s3 sync worlds/production/ s3://game-assets/worlds/production/ \
  --endpoint-url "https://${CF_ACCOUNT_ID}.r2.cloudflarestorage.com" \
  --delete \
  --cache-control "public, max-age=31536000, immutable"
```

### What Gets Synced

- `tiles/` — Leaflet tile pyramid (thousands of PNGs, ~2-4 GB)
- `assets/icons/` — UI icons (SVGs/PNGs, small)
- `assets/portraits/` — character portraits
- `assets/equipment/` — gear images
- `assets/vignettes/` — encounter vignettes
- `map.png` — full map render (optional, large)

**Not synced**: `map.json` (served by GameServer or bundled with app),
`encounters/` (served by GameServer).

## Client Changes

Replace the current `/world/` relative URLs with the CDN base URL in production.

Option A — environment variable:
```typescript
const ASSET_BASE = import.meta.env.VITE_ASSET_BASE ?? "/world";
// tile URL: `${ASSET_BASE}/tiles/{z}/{x}/{y}.png`
// icon URL: `${ASSET_BASE}/assets/icons/sword.svg`
```

Option B — just hardcode the CDN URL in the production build config. It's one string.

The dev server keeps working as-is via the `/world` symlink. No changes needed for local dev.

## Cache Strategy

All assets are immutable at a given path — tiles don't change without a full map regeneration.
Set `Cache-Control: public, max-age=31536000, immutable` on upload (already in push.sh).

If the map is regenerated, the tile content changes but paths stay the same. Options:
- **Version prefix**: `worlds/v17/tiles/...` — cleanest, cache-bust by changing the prefix
- **Cache purge**: use CF API to purge after a regen — simpler but slower propagation
- **Just wait**: tiles are regenerated rarely enough that TTL expiry is fine for a hobby project

The push.sh snippet already has a comment suggesting version prefixes. That's the right call
for production but overkill until we actually have users.

## Deployment Flow

1. Build world: `worlds/production/build.sh`
2. Push assets: `worlds/production/push.sh`
3. Deploy app (separately — Pages, Azure Static Web Apps, whatever)

## Cost

Free. 5 GB of assets with hobby traffic stays well within R2's free tier
(10 GB storage, 10M reads/month, $0 egress).

## Open Questions

- Where does the React app itself get hosted? CF Pages is the natural companion
  (free tier, git-connected deploys, same CF account). But that's a separate decision.
- GameServer hosting (Azure Functions? Azure App Service? Fly.io?) — separate from asset CDN.
- Custom domain — need to pick one and add it to Cloudflare.
