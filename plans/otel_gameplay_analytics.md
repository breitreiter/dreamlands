---
kind: plan
title: "Gameplay analytics telemetry — combat outcomes and arc engagement"
state: exploring
created: 2026-08-16
updated: 2026-08-16
status: Not started. Design settled for combat (§2); arc disposition has one open question that needs tracing before it can be specced (§3). Depends on plans/otel_appsignal.md phase 2 being deployed — the metrics below are inert in production until production app settings are set.
touches:
  files:
    - server/GameServer/Telemetry.cs
    - server/GameServer/GameFunctions.cs
  features: [observability, combat, arcs, balance]
related:
  - plans/otel_appsignal.md → §3 (this deliberately relaxes its "no game analytics in metrics" exclusion — see §4)
  - plans/otel_appsignal.md → §7 event-forensics log (the alternative home for per-player detail)
---

# Gameplay analytics telemetry

Track how players actually engage with combat and arcs: are they fleeing,
fighting and losing, or fighting and winning; and how far into each arc do they
get. The motivating question was "is Tob Ashford clapping noobs, or is he
reasonably tuned" — but the same shape answers it for every fight and arc.

## 0. What already exists

Phase 2 pass 2 of `plans/otel_appsignal.md` already shipped:

| Metric | Type | Tags |
|---|---|---|
| `dreamlands.combat.started` | `Counter<long>` | none |
| `dreamlands.combat.ended` | `Counter<long>` | `result` |
| `dreamlands.encounter.started` | `Counter<long>` | `kind` |

`result` already carries exactly the three states we care about:
`won` / `lost` / `fled` (plus `monster_fled` / `other`).

**The gap is dimensioning, not plumbing.** `combat.ended` has no fight
dimension, so every fight in the game aggregates into one bucket — you cannot
see an individual monster. `encounter.started` buckets to the leading path
segment, so every arc in the game collapses into `kind="arcs"`.

## 1. Where the code goes, and why

Dependencies point **server → lib**, never the reverse
(`GameServer.csproj:26`; `lib/Orchestration` references only Game/Map/Encounter/
Combat/Rules/Flavor). `Telemetry` is a GameServer-local static class.

This is deliberate, not incidental — see the comment at
`GameFunctions.cs:2283-2288`: combat metrics live at the server boundary so the
engine stays a pure `(state, args, balance, rng) -> (state, results)` library
drivable headless by `ui/cli` and sim harnesses.

**No `lib/` changes in this plan.** Everything lands in `Telemetry.cs` and
`GameFunctions.cs`.

## 2. Combat outcomes

### The seam

There is already exactly one translation point:
`RecordCombatOutcome(CombatTurn)` at `GameFunctions.cs:2289`. It pulls
`CombatEvent.Outcome` out of `turn.Events`, maps to the result string, and calls
`Telemetry.RecordCombatEnded`. Its two callers cover both entry paths:

- `GameFunctions.cs:2279` in `BeginCombat` — instant-resolve fights
- `GameFunctions.cs:2251` after `CombatOrchestrator.Step` — the normal path

Exactly-once per fight is guaranteed by the double-submit guard at
`GameFunctions.cs:2247-2248`.

**Keep the seam here.** `GameFunctions.cs:96-117` rebuilds a synthetic outcome
event on every GetGame refresh during the defeat coda; instrumenting anything
downstream of that (e.g. in `BuildCombatResponse`) would count a single loss
once per page reload.

### Changes

Widen the helper to `RecordCombatOutcome(GameSession session, CombatTurn turn)`
— both call sites already have `session` in scope.

| Metric | Type | Tags | Notes |
|---|---|---|---|
| `dreamlands.combat.ended` | `Counter<long>` (exists) | **add** `fight_id`, `tier` | `fight_id` from `turn.EncounterId`; `tier` from `session.CombatBundle.GetById(id).Tier` |
| `dreamlands.combat.rounds` | `Histogram<int>` (new) | `fight_id`, `result` | rounds from `outcome.Turns` |

**Explicitly NOT tracking final PC health.** Starting HP is far too noisy to
interpret — travel grind means two players entering the same fight are in wildly
different states, so the number measures the journey, not the fight.

`dreamlands.combat.rounds` earns its place by separating failure modes that the
win rate alone conflates: short losses mean overwhelmed, long losses mean a
grind the player lost on attrition, and long *wins* mean a spongy fight that
isn't fun even when you win.

### Histogram buckets

Set **explicit coarse boundaries** (suggest `1, 2, 3, 5, 8, 13, 21`). Every
histogram bucket is its own time series, and `plans/otel_appsignal.md` §4 warns
that series count is expected to hit the free-plan cap before anything else.
Default OTel boundaries would be ~15 buckets × 20 fights × 5 results.

### Series budget

- `combat.ended`: 20 fights × 5 results = **100 series**
- `combat.rounds`: 20 fights × 5 results × ~8 buckets = **~800 series**

The histogram dominates. If that's too expensive, drop `result` from the
histogram (→ ~160 series) and keep the full breakdown on the counter.

## 3. Arc engagement

### Counts (straightforward)

Add `dreamlands.arc.started{arc}` — a bounded set of 20 arcs from
`mapgen/content/dungeons_roster.yaml`. Derive the arc slug from the encounter
id's second path segment (`arcs/<biome>/<arc>/...`) in the same shape as the
existing `RecordEncounterStarted` bucketing at `Telemetry.cs:84-88`.

### Disposition (OPEN QUESTION — trace before speccing)

**Unverified: there may be no single "arc ended" event to hang a counter on.**

The one worked example, `the_lodge`, ends by setting a tag
(`the_lodge.beast_defeated`) and `+chain`-ing a follow-up encounter. That
suggests completion would have to be *inferred* from arc-scoped tag grants
rather than read off one event, which is a materially different (and more
fragile) design than the combat metric.

Before writing any code:

1. Establish whether arcs have a uniform completion signal, or whether each arc
   ends idiosyncratically via its own tags.
2. If idiosyncratic — decide whether to introduce an explicit arc-completion
   mechanic in the `.enc` vocabulary (cleaner, but touches `lib/Rules` and every
   arc's final encounter) or infer from tags (no content changes, but every new
   arc needs its terminal tag registered somewhere or it silently never reports).

**Ship arc counts first; treat disposition as a follow-up** gated on that
decision. Counts alone already answer "which arcs do players actually enter",
which is the bigger unknown today.

## 4. This relaxes a stated convention — do it deliberately

`plans/otel_appsignal.md` §3 explicitly excludes "anything that is game
analytics rather than ops — gold balances, encounter-choice distributions,
per-player progression" from metrics, routing it to the event-forensics log
(§7). And `RecordEncounterStarted` deliberately buckets ids away rather than
emitting them.

This plan is game analytics in metrics, on purpose. The justification:

- `fight_id` and `arc` are **closed, authored sets** (20 each), fixed at build
  time. They are categorically unlike encounter ids, game ids, or coordinates —
  they cannot grow with traffic, only with content we write.
- The question is a balance question with a small, stable answer space, and
  metrics are the right tool for a rate over a bounded vocabulary.

**Action:** amend `plans/otel_appsignal.md` §3 with a carve-out naming these two
dimensions, so this reads as a decision rather than drift.

**Standing constraint that does NOT relax:** never dimension by game id, player
id, coordinates, or any user-supplied string. Per-player detail goes on spans
via `Telemetry.Source` (see `GameFunctions.cs:193`), which has no cardinality
cost, or to the event-forensics log.

## 5. Order of work

1. Combat: widen `RecordCombatOutcome`, add tags + rounds histogram. (§2)
2. Amend `plans/otel_appsignal.md` §3 with the carve-out. (§4)
3. Arc counts: `dreamlands.arc.started{arc}`. (§3)
4. Trace arc completion; spec disposition separately. (§3)

## 6. Caveats

**This is inert until phase 2 is deployed.** `plans/otel_appsignal.md` status
says production app settings are still unset. No metric here reports anything
from production until that lands.

**Telemetry answers a different question than the sim.** A sim gives the tuning
curve today at arbitrary N; telemetry tells you what real players with messy
gear actually do — including fleeing, which no sim policy models. They are
complements. For "is this monster tuned", sim first, telemetry as the
confirmation loop.

**Deaths are the tail, not the distribution.** Flee rate is likely the more
sensitive signal of a badly-tuned fight than death rate — players bail before
dying, so an overtuned monster may show a modest death rate and a huge flee
rate. Read `result=fled` as a first-class outcome, not an error case.
