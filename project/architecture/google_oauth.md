# Google OAuth + Session Reconnect

Analysis of what it would take to let players sign in via Google and reconnect to games in progress.

## Current State

- No auth — the 12-char `gameId` is the only identity. Whoever has it can play.
- Storage: local JSON files in `~/saves/{gameId}.json`
- Server: ASP.NET 8.0 Minimal APIs, stateless per-request reconstruction
- Client: React SPA, holds `gameId` in memory

## New Concepts

| Concept | Description |
|---------|-------------|
| **User** | Google identity (`sub` claim from JWT). Maps to one or more games. |
| **Auth token** | Google ID token, verified server-side. Sent as `Authorization: Bearer <token>`. |
| **User-game binding** | Lookup from Google `sub` → `gameId`. JSON file or DB row. |

## Server Changes

**New endpoint:**
```
POST /api/auth/google   ← receives Google ID token, verifies against Google's public keys,
                          returns or creates user record, returns active gameId (if any)
```

**Ownership guard** on all `/api/game/{id}/*` endpoints — authenticated user must own the game.

**User store** — a JSON file per Google `sub` at `~/saves/users/{sub}.json`, containing
`{ email, displayName, gameIds[] }`. With Cosmos (planned future), this becomes a partition
key on the user document.

**Endpoint tweaks:**
- `POST /api/game/new` requires auth, binds game to user
- `GET /api/game/{id}` verifies ownership
- New: `GET /api/user/games` lists saved games for a "continue" screen

**Dependencies:** `Microsoft.AspNetCore.Authentication.JwtBearer` (already in ASP.NET ecosystem),
Google's public JWKS endpoint for token verification. No new NuGet packages strictly required.

## Client Changes

**Google Sign-In** on Splash screen via Google Identity Services (`@react-oauth/google` or raw
GSI script tag). Flow:

1. User clicks "Sign in with Google"
2. Google returns an ID token (JWT)
3. Client sends to `POST /api/auth/google`
4. Server verifies, returns `{ userId, activeGameId? }`
5. Active game exists → offer "Continue" → `GET /api/game/{id}` → resume
6. No active game → "New Game" → `POST /api/game/new`

**Token storage** in `localStorage`. On page load, if token exists, re-verify and fetch active game.

**New files:** `AuthContext.tsx` (parallel to GameContext), sign-in UI on Splash screen.

## What You Don't Need

- **No session cookies** — Google ID token is stateless auth, verified per-request
- **No user database** — JSON file per `sub` is fine at single-player scale
- **No refresh token flow** — ID tokens last ~1 hour; silent re-auth on expiry
- **No authorization code flow** — simple "Sign In With Google" popup is enough (identity only, no API access)

## Scope

| Area | Files | Complexity |
|------|-------|------------|
| Server auth middleware | `Program.cs` + new `AuthService.cs` | Medium |
| User store | New `UserStore.cs` (mirrors `GameStore.cs`) | Low |
| Ownership guards | `Program.cs` endpoint checks | Low |
| Client sign-in | `Splash.tsx` + new `AuthContext.tsx` | Medium |
| Client reconnect | `App.tsx`, `GameContext.tsx` | Low |
| Google Cloud Console | OAuth consent screen + client ID | Config only |

~3-4 new server files, ~2-3 new/modified client files.

## Considerations

- **Guest mode** — keep the current no-auth path. Let people play without signing in, optionally
  "claim" their game later by linking a gameId to their account.
- **Multiple devices** — works naturally since state is server-side. No conflict resolution needed
  (single-player, sequential actions).
- **Privacy** — store Google `sub` (opaque ID) + email. Minimal PII.
- **CORS** — lock down to actual domain once auth exists.
- **Google dependency** — if Google auth is down, new games can't start. Existing games could
  work if verified user is cached locally.

## Migration Path

1. **Phase 1**: Google sign-in client + `POST /api/auth/google` server. User→game mapping.
   Existing saves are "unclaimed."
2. **Phase 2**: Ownership checks on game endpoints. Unclaimed games stay accessible by gameId
   (backwards compatible with CLI client).
3. **Phase 3** (optional): "Claim game" flow — sign in, then link an existing gameId to your account.
