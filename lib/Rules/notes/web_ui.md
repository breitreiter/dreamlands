spa

ambivalent on ui tech stack

probably azure function backend
db tbd

need to ensure we include all licenses.  likely will be using Google Material Icons - https://fonts.google.com/icons?icon.set=Material+Symbols&selected=Material+Symbols+Outlined:flag:FILL@0;wght@400;GRAD@0;opsz@20&icon.query=flag&icon.size=20&icon.color=%23e3e3e3


Here’s a “cheap until proven fun” backend architecture for a single-player, turn-based web RPG on Azure Functions, optimized for: **fast load → mutate → save within one API call**, **low idle cost**, and **simple ops**.

## Goals & constraints

* **Single player**: no realtime sync, no multi-writer concurrency (mostly).
* **Save code access**: player can resume from any device.
* **Azure Functions**: stateless compute, short-lived execution.
* **One request = one turn**: fetch state, apply action(s), persist, return result.
* **Keep costs low**: pay-per-use, minimal always-on services.

---

## High-level shape

**API Gateway (Function App HTTP triggers)** → **Game Service (pure deterministic logic)** → **State Store (cheap + fast)** → **Telemetry**.

### Key design decision

Treat the backend like a **turn transaction processor**:

* Inputs: `saveCode`, `clientAction`, and a `clientTurnToken` (or `expectedVersion`)
* Output: `newStateDelta` + `newTurnToken`

This pushes you toward *idempotency*, *optimistic concurrency*, and *small writes*.

---

## Services (minimal set)

### 1) Azure Functions (Consumption plan)

* HTTP endpoints:

  * `POST /v1/new` → create save, return `saveCode`
  * `GET /v1/load?code=...` → fetch state snapshot + turn token
  * `POST /v1/turn` → apply one (or a small batch) of actions
  * `POST /v1/rename` / `POST /v1/settings` etc. (optional)
* Keep your game logic as a **pure library** referenced by the Functions project.
* Always validate and clamp input; never trust the client.

**Cost posture:** near-zero when idle; pay per execution.

### 2) State store: Cosmos DB (serverless) *or* Azure Table Storage

Pick based on how fancy your queries need to be:

**Option A: Azure Table Storage (cheapest, simplest)**

* Great for key-value “load this save by code”.
* Limited query patterns but you probably don’t need them.
* Super economical.

**Option B: Cosmos DB Serverless (still cheap if traffic is low)**

* Easier JSON document handling, TTL, indexing, and conditional writes.
* Costs more than Tables at tiny scale but stays manageable.

For *your* described pattern (load by code, write back), **Tables is likely enough**.

### 3) Blob Storage (optional but recommended)

Use blob for:

* Larger payloads (if save games get big)
* Snapshots / rollbacks
* Asset-like generated content (maps, thumbnails) if needed

Pattern: DB row contains a pointer to the latest blob + metadata.

---

## Data model: make writes tiny and safe

### Save code

* `saveCode` = short random token (not guessable), e.g. 10–16 chars base32/base64url.
* Store a **hashed** form as the key if you want “if DB leaks, save codes aren’t immediately usable”.

### Record structure (works for Tables or Cosmos)

**Save metadata row/document** (hot path)

* `saveId` (GUID)
* `saveCodeHash` (lookup key)
* `version` (int, optimistic concurrency)
* `etag` (storage-provided concurrency token; Tables/Cosmos both support this concept)
* `updatedAt`
* `state` (compressed JSON) **or** pointer to blob

**Optional action log** (append-only, cold path)

* `saveId`, `version`, `timestamp`, `action`, `rngSeedDelta`, etc.
* Useful for debugging, replays, and rollback
* Only enable if/when you need it (or sample it)

### Compression

If state is JSON:

* Compress before storing (e.g., gzip or zstd).
  Turn-based state often compresses extremely well.

---

## Turn processing flow (the “one API call” transaction)

`POST /v1/turn`

1. **Load** save by `saveCodeHash`
2. **Validate**:

   * check expected `version` (or compare ETag)
   * verify action schema
3. **Apply** game logic:

   * `newState = Reduce(oldState, action)`
   * ensure deterministic RNG: store RNG state/seed in save, advance it server-side
4. **Write back** with optimistic concurrency:

   * If using Tables: `UpdateEntity` with `If-Match: etag`
   * If Cosmos: conditional replace on `_etag` or use transactional batch if partitioned properly
5. **Return**:

   * minimal payload (delta, new version, any revealed info)

If concurrency fails (client double-submits or uses stale token):

* return **409 Conflict** with latest `version` and maybe a “reload required” hint.

---

## Idempotency (so you don’t double-apply a turn)

Mobile networks and browsers retry. Protect yourself:

* Client sends `actionId` (UUID).
* Save record stores `lastActionId` (or a small LRU set).
* If the same `actionId` arrives again at the same `version`, return the **same result** without reapplying.

This one change prevents “duplicate turn” bugs and makes retries safe.

---

## Performance tricks that keep it cheap

* **Keep the save payload small**:

  * Separate “world seed + derived data” from “mutable state”.
  * Store procedural generation inputs, not generated outputs.
* **Use deltas**:

  * Client doesn’t need the whole state each turn; send only what changed + a small view model.
* **Partition/keys**:

  * Your dominant access pattern is key lookup; design for single-row reads/writes.
* **Avoid chatty APIs**:

  * One call per turn. Don’t do “load → validate → commit” as separate calls.

---

## Security model (minimal but sane)

* Treat `saveCode` as a bearer secret.
* Rate limit per IP + per save code (Functions + Front Door/APIM if needed later).
* Optional: allow the user to set a secondary PIN/passphrase; store a verifier hash.

---

## What I’d build first (lowest complexity)

**Azure Functions (Consumption) + Azure Table Storage + Blob Storage**

* Table row contains:

  * `stateBlobUri` (or inline compressed state if small)
  * `etag/version`
  * `updatedAt`
* Blob contains compressed JSON state

Why this is nice:

* Table update gives you **ETag concurrency**.
* Blob is cheap for bigger states.
* You only pay meaningful money when people actually play.

---

## Suggested endpoints (concrete)

* `POST /v1/new`

  * response: `{ saveCode, version, initialView }`
* `GET /v1/load?code=...`

  * response: `{ version, view, worldMeta }`
* `POST /v1/turn`

  * body: `{ code, version, actionId, action }`
  * response: `{ newVersion, result, viewDelta }`

---

## Implementation notes that prevent pain later

* Put all logic in a `Game.Core` library with **no Azure dependencies**.
* Version your state schema (e.g., `stateSchemaVersion`) and write migrators.
* Add lightweight telemetry:

  * `turnDurationMs`, `stateSizeBytes`, `compressionRatio`, `conflictRate`, `errorsByType`
* Add a “panic button” admin function to invalidate a save or roll back to last snapshot (optional).

---

If you want, paste:

* your rough state shape (what’s in the save),
* your expected max size (KB/MB),
* and whether you want “save code” to be guess-resistant vs. user-friendly,

…and I’ll sketch a more exact data model (Tables vs Cosmos, partition keys, and a turn handler pseudo-code) tuned to your game.


Wait—**but put in the two or three “bill-preventers” now.** Full OTel + Grafana can come when you have real traffic or real bugs.

### Do *now* (cheap, high leverage)

1. **Hard budgets + alerts**

   * Set an **Azure Cost Management budget** for the subscription (and/or resource group) with alerts at like **$5 / $20 / $50**.
   * This is the single best “no surprise bill” move.

2. **Tight guardrails on the expensive knobs**

   * If you use **Application Insights** / Azure Monitor:

     * Set **daily cap** (or at least sampling) and keep retention low.
   * If you use **Cosmos**:

     * Prefer **serverless** at low scale; watch RU spikes.
   * If you use **Blob / egress**:

     * Watch bandwidth and public downloads.

3. **Minimal structured logging**

   * Log *just* enough to debug:

     * request id / save code hash prefix (not the code), save version, actionId, turn duration, state size, status code, exception type.
   * Write logs to whatever you already have (App Insights if you’re on Functions). Don’t build a whole observability stack yet.

4. **One metric that matters**

   * Emit a counter/timer for `turn_latency_ms` and `turn_error_rate`.
   * You can do this with built-in Azure monitoring without going full OTel.

### Defer (until you have signal)

* **End-to-end traces across services**, rich spans, custom baggage, exemplars, Grafana dashboards, etc.
* **Logs to Grafana Loki / OTLP pipelines** unless you already run Grafana Cloud and know you’ll stick with it.
* Anything that adds multiple new components (collector, agents, sidecars) before you’ve proven the game loop.

### When to “turn on” OTel/Grafana for real

Do it when *any* of these happen:

* You get enough users that debugging “one weird save” is painful.
* Your p95 turn latency starts drifting and you can’t tell why.
* Costs become non-trivial and you need per-endpoint / per-save / per-action visibility.
* You add more moving pieces (queue, blob snapshots, multiple stores).

### A pragmatic compromise

If you *already* live in Grafana land (sounds like you might):

* Add **OTel API instrumentation in code** (minimal, behind a flag), but **don’t deploy an OTel Collector** yet.
* Keep export to **Azure Application Insights** (or just console logs) initially.
* Later, switch the exporter to OTLP → collector → Grafana when it’s worth it.

If you tell me what you’re using today for Functions monitoring (App Insights enabled? Grafana Cloud? self-hosted?), I’ll give you the exact “minimum config” that keeps costs predictable.


If you design your save format like a **document DB from day 1**, the “loose files → Cosmos” move is mostly wiring, not surgery.

## 1) Treat the save as one authoritative document

Have a single **SaveRoot** object that contains everything needed to resume:

* `saveId` (stable GUID)
* `schemaVersion` (int)
* `updatedAt`
* `rng` state (seed + position/state)
* `world` (mostly immutable: seed, gen params)
* `entities` (mutable)
* `quests/flags`
* `inventory/economy`
* `location/party`
* `history` (optional)

On disk: `saves/<saveId>/save.json` (or `.json.gz`).
In Cosmos: one document in a `Saves` container.

**Rule:** assume you will load + write the whole document per turn. Optimize later.

## 2) Use stable IDs everywhere (never rely on array index)

Cosmos loves “map of things by id.” So should you.

Prefer:

```json
"actors": {
  "a_001": { ... },
  "a_002": { ... }
}
```

over:

```json
"actors": [ { ... }, { ... } ]
```

Arrays are fine for ordered lists (combat turn order, log), but *identity* should be key-based.

**Practical payoff:** fewer merge/migration bugs, easy targeted updates later if you ever need patches.

## 3) Split “immutable world definition” from “mutable state”

Most RPG data doesn’t change every turn (biome definitions, item templates, encounter tables).

* **Static content**: JSON files shipped with the game (or embedded resources)
* **Save state**: only references static content by ID

Example:

* Save stores `itemInstance { templateId: "itm.iron_sword", durability: 12 }`
* The sword’s name/description/stats live in content data.

**Payoff:** your save stays small and stable; schema migrations are simpler.

## 4) Make “one turn = one deterministic reduce()”

Design your backend/CLI loop as:

`(oldState, action) -> (newState, result)`

Where:

* RNG is part of state (or derived from a seed + counter)
* No hidden globals
* No ambient clock access (or it’s injected)

This matches Cosmos perfectly: read doc → apply → write doc.

## 5) Add versioning and migration hooks immediately

Put these at the top of the save:

* `schemaVersion`
* `gameBuild` (optional)
* `createdAt`, `updatedAt`

And keep a simple migration pipeline:

`v3 -> v4 -> v5`

Even if migrations are trivial at first. You’ll thank past-you.

## 6) Plan for optimistic concurrency now (even on disk)

Cosmos will want ETag/If-Match. On disk you can simulate with:

* `version` integer increment per write
* (optional) file hash / last-write timestamp

Client sends `expectedVersion`; if mismatch, force reload.
This prevents “double-apply turn” bugs and makes the Cosmos transition nearly drop-in.

## 7) Make save files “content-addressable-ish” (optional but nice)

If you anticipate big saves later, structure with **subdocuments** that can become separate blobs/files:

* `save.json` (root + pointers)
* `entities.json` (or `entities/*.json`)
* `log.json`

On disk: multiple files.
In Cosmos: keep one doc until you must split; if you split, you already have the seams.

Don’t overdo this early—just ensure the root model *could* point elsewhere.

## 8) Use a “Dictionary of components” style for entities (ECS-lite)

For mutable entities, a flexible pattern is:

```json
"entities": {
  "e_1001": {
    "type": "npc",
    "components": {
      "position": { "mapId": "m1", "x": 12, "y": 9 },
      "stats": { "hp": 10, "str": 7 },
      "ai": { "state": "patrol", "target": null }
    }
  }
}
```

Why this is Cosmos-friendly:

* JSON-document native
* You can add components without reshaping the whole schema
* Easy migrations (component-by-component)

If you prefer strong typing in C#, you can still implement this with:

* a typed facade around a dictionary
* or polymorphic component serialization

## 9) Keep a strict separation: “save model” vs “runtime model”

Runtime objects can be rich (caches, derived fields, precomputed pathing), but your save should be:

* boring
* explicit
* serialization-friendly
* no references/pointers
* no cycles

Runtime can rebuild caches after load.

## 10) Choose serialization habits that match Cosmos

Cosmos is JSON-first. So even locally:

* JSON (optionally gzipped)
* camelCase field names
* avoid custom binary formats unless you love pain
* keep numbers and enums stable (store enums as strings if you expect changes)

---

### A simple “Cosmos-ready” save root skeleton

Fields worth having from day one:

* `id` (Cosmos wants `id`)
* `saveCodeHash` (later; local can omit or keep null)
* `version`
* `schemaVersion`
* `updatedAt`
* `rng`
* `worldSeed` + generation params
* `entities` as dictionary
* `flags` as dictionary/set

---

If you paste (1) your rough state categories and (2) whether you’re already leaning ECS-ish or more “classic OO,” I’ll propose a concrete C# model layout (records/classes) that serializes cleanly to JSON and ports to Cosmos without you rewriting half your game.
