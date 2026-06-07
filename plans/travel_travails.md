---
kind: plan
title: "Travails — nuke travel conditions, deterministic journey costs"
state: active
created: 2026-06-06
updated: 2026-06-06
status: ACTIVE — phases 1-3 DONE 2026-06-07 (engine, wiring, rules+content sweep; travel conditions fully gone); next: phase 4 UI; UX opens OQ-2/5 to settle there
touches:
  files:
    - lib/Rules/ConditionDef.cs
    - lib/Rules/BalanceData.cs
    - lib/Rules/ItemDef.cs
    - lib/Game/EndOfDay.cs
    - lib/Game/Mechanics.cs
    - lib/Game/PlayerState.cs
    - lib/Orchestration/SettlementRunner.cs
    - server/GameServer/GameFunctions.cs
    - server/GameServer/GameResponse.cs
    - ui/web/src/
    - text/encounters/
  features: [conditions, travel, end-of-day, camp, arrival]
supersedes-partially: |
  condition_rework.md (the minor/travel-condition half; severe half stands);
  spirits_economy.md ("clean travel day costs 0 spirits" principle — reversed, see Economy role)
---

# Travails — Nuke Travel Conditions

Travel conditions (freezing / thirsty / exhausted) are thanos-snapped. No state,
no nightly resist rolls, no settlement auto-clears. In their place: every journey
deterministically accrues **travail costs** from the route walked, mitigated by
gear and Bushcraft, and the player sees a narrative cost summary at the tail end
of the trip.

> "Walked through the desert for half a day — lost 1 spirit to thirst.
>  Your bedroll kept the mountain cold out. Four nights on the road — 3 spirits."

## Why

From the 2026-06-06 condition-system design brief (TODO.md Pre-Launch):

- **Invisible acquisition**: you couldn't see that you got thirsty crossing the
  desert; spirits just decremented. Travails make every loss a named, visible line.
- **Silent clears**: settlement-entry clearing was the game's only silent state
  change (the half-wired `allClearedConditions` plumbing). With no travel-condition
  state, there is nothing to clear — the dead plumbing dies with it.
- **Exhausted-cure-on-arrival felt bad**: fiction said rest should be required.
  Now fatigue is a *cost already paid*, not a state to cure. The feel-bad moment
  is structurally impossible.
- **Randomness without agency**: nightly 0/40/80% resist rolls were invisible dice.
  Deterministic costs mean route planning and gear choices are real decisions with
  legible payoffs.

## Decisions locked (2026-06-06, w/ Joseph)

1. **Spirits only.** Travails never touch HP or rations. HP stays the domain of
   combat + severe conditions; food cadence is untouched.
2. **Bushcraft = deterministic rate discount.** Replaces the 0/40/80% resist roll.
3. **Costs accrue per step** (charged at threshold crossings as you walk); the
   end-of-journey summary displays the accumulated ledger. Spirits are always
   current when an encounter interrupts mid-journey — no lump-sum moment, and a
   one-step manual `move` is just a degenerate journey using the same math.
4. **Content sweep**: the 12 `+add_condition exhausted` / 2 `freezing` mechanic
   lines in .enc files become direct `+spirits -N` losses.
5. **Fatigue charges from night 1 — no grace night.** Constant low-level spirits
   drain is the point: it's the game's money sink (see Economy role). Supersedes
   the `spirits_economy.md` "clean travel day costs 0" ceiling.
6. **scarecrow_boots keep zeroing fatigue.** They're a dungeon reward — a
   deliberate discoverable valve, not a balance bug.

## The model

### Hazard channels

A small static table in `BalanceData` (no YAML, per convention):

| Hazard  | Accrues when                       | Mitigating gear   | Replaces  |
|---------|------------------------------------|-------------------|-----------|
| Thirst  | per step in scrub                  | waterskin (zeroes)| thirsty   |
| Cold    | per step in mountains              | sleeping_kit (zeroes) | freezing  |
| Fatigue | per night camped on the road       | scarecrow_boots   | exhausted |

Each channel: `id, name, biome-or-universal, unit (step|night), mitigating item id,
flavor fragments` (for summary lines: suffered / spared-by-gear / eased-by-skill).

Roster is deliberately data-shaped so the "pad the travel conditions roster" TODO
becomes "add a row + a mitigation item" (swamp miasma → mosquito netting, etc.).
Follow-up, not this plan.

### Cost math (integer, no fractions)

Per-channel **steps-per-spirit** threshold, scaled by Bushcraft tier. Calibrated
2026-06-06 against measured production-map route lengths — see
`project/reference/map_route_lengths.md` for the data and method.

|           | Untrained | Trained | Expert |
|-----------|-----------|---------|--------|
| Thirst/Cold (steps per 1 spirit) | 2 | 4 | 8 |
| Fatigue (chargeable nights per 1 spirit) | 1 | 2 | 4 |

- A day = **5 time periods** (Morning/Midday/Afternoon/Evening/Night), so a step
  is a fifth of a day; untrained thirst ≈ 2–3 spirits per full desert day and
  "half a day" = 1 spirit (matches the target flavor line).
- **Fatigue charges from night 1** — every road night costs. No "fresh legs"
  grace. This is deliberate (decided 2026-06-06): see Economy role below.
- Mitigating gear in pack zeroes its channel entirely (and earns a "spared" line
  in the summary — gear must *visibly* do its job).
- Steps on settlement nodes don't accrue.

### Economy role — travails are the money sink

This **supersedes the `spirits_economy.md` principle that "a clean travel day
costs 0 spirits."** We now want the opposite: constant, low-level spirits drain
on every road night. Rationale (Joseph, 2026-06-06): spirits drain converts to
money drain (inn bed +5sp/5g, bath +10sp/12g), and the game currently has **no
other drain on money** — gear lasts forever, medicine lasts forever, food is
nearly free. Without a recurring cost, there's no reason to keep delivering
contracts. Travails are that recurring cost. (The alternative — raising food
prices — was considered and passed over.)

Feeds the "economy pass" item under TODO.md Rules & Balancing (cash accumulates
too fast); this plan is one of its levers, not the whole answer.

### Calibration vs the production map (spirits budget: 20, inn bed +5 / bath +10)

Round-trip costs under the table above (U = Untrained, T = Trained; "geared" =
waterskin + bedroll (sleeping_kit), no scarecrow_boots):

| Route | U no gear | U geared | T geared |
|---|---|---|---|
| T1 hop (median 6 steps, 1 night) | 1 | 1 | 0–1 |
| Median trade leg (13 steps, 2 nights) | 2–6 | 2 | 1 |
| Worst hazard leg (Kalimur→Melori: 18 steps, 15 hazard, 3 nights) | 10 | 3 | 1–2 |
| the_stift RT (18 steps, all mountain, 3 nights) | 12 | 3 | 1–2 |
| the_city RT (44 steps, hazard-free, 8 nights) | 8 | 8 | 4 |
| foundry RT (64 steps, 20 hazard steps, 12 nights) | **22 — busts** | 12 | 6 |

The shape lands where we want it:

- T1/T2 bread-and-butter costs a steady 1–2 spirits per leg — the constant
  low-level drain the economy needs (see Economy role): every few legs buys an
  inn bed. Hazard-heavy T2 legs are gear checks, not skill checks ("immunity
  gear is the replacement flavor").
- Doorstep T3 (the_lodge/the_stift/the_revenakh, 6–12 step legs from T2 towns) is
  a gear check plus a few nights.
- Expedition T3: foundry untrained-ungeared **cannot come home** (22 > 20);
  untrained-geared returns on fumes (12 spirits, ~8/20 left) with zero headroom
  for the ~4–6 encounters a 64-step round trip rolls; Trained is comfortable.
  That's the soft-require: gear is mandatory, Bushcraft Trained is what buys you
  margin for things going wrong. Matches the "bad frontier trip ends ~7/20"
  target from `spirits_economy.md`.
- scarecrow_boots (zero fatigue) would let an untrained player do the_city run
  cheap — kept deliberately: the boots are a dungeon reward, so a pro can
  bee-line them while a new player may never find them. And the_city / T3
  plains have enough snares that exhaustion is the least of your concerns.

### Ledger + charging

`PlayerState.TravailLedger`: per-channel `{ unitsAccrued, spiritsCharged }` plus
journey start day. Cosmos-serializable like everything else on PlayerState.

- **Accrual**: each travel/move step calls `Travails.AccrueStep(state, biome, balance)`;
  each on-road night rollover calls `Travails.AccrueNight(...)`. When
  `unitsAccrued / threshold` exceeds `spiritsCharged`, charge the difference to
  spirits immediately. Pure `(state, args, balance) -> results`, no RNG at all.
- **Flush** (`Travails.Summarize(state, balance) -> TravailSummary`): builds the
  narrative summary from the ledger and resets it. Called at journey termination.

### Journey boundaries / when the summary shows

A "journey" is the `travel` action's path walk. Termination = any stop reason:

- **arrived** → summary rides on `ArrivalInfo` (generalize it: travails replace
  the `journeyLosses` condition accounting; keep deliveries).
- **encounter / combat / camp interruption** → summary attaches to the existing
  `TravelInfo` in that response: "the shortened actual journey". UI shows it as
  a compact travails strip; resuming travel afterward starts a fresh ledger.
- **Manual `move` stepping**: same accrual into the same ledger; flush on
  settlement arrival. (Open question §OQ-2 on whether camp screen should show a
  running tally.)

## Kill list

- `ConditionDef`: `freezing`, `thirsty`, `exhausted` entries; `ClearedOnSettlement`,
  `ResistDifficulty`, `SpiritsDrain`, `Biome`, `Tier` fields (all only served
  travel conditions). Remaining four conditions are all Severe →
  `ConditionSeverity.Minor` and the enum itself likely collapse (OQ-3).
- `EndOfDay`: `GetThreats`, `RollResists`, `ApplyNewConditions`,
  `ClearOutOfBiomeConditions`, travel-condition branch of `ResolveSpiritsDrain`
  (starving stays), `UniversalAmbientIds` / `EncounterOnlyIds` / `TravelConditionIds`
  sets, `PendingNoBiome` flag. EndOfDay shrinks to: food, medicine, severe HP
  drain/regen, death/rescue.
- `PlayerState.ConditionsClearedThisTurn` — only existed to stop ambient re-add
  after `remove_condition`; no ambient adds, no need.
- `SettlementRunner.EnsureSettlement` cleared-conditions out-param, and the whole
  `allClearedConditions` / `ClearedConditionInfo` dead plumbing in GameFunctions +
  GameResponse (resolves the ReSharper 2026-06-05 finding).
- `Mechanics.ApplyAddCondition` travel-condition Bushcraft resist branch (Cunning
  resist for severe stays).
- Camp threats: `BuildCampThreats`, `CampThreatInfo`, threat display in Camp.tsx.
  Camp screen becomes food + severe-condition accounting only.
- `ItemDef.PassiveImmunities` → renamed/repurposed as the hazard-mitigation hook
  (waterskin/sleeping_kit/scarecrow_boots point at hazard channel ids instead of
  condition ids).
- `ConditionFlavor` entries for the three travel conditions (harvest any good
  prose into the travails flavor fragments first).
- Content: 11 .enc files swept (`+add_condition exhausted|freezing` → `+spirits -N`).

## Survives untouched

- Severe conditions end-to-end: encounter acquisition (Cunning resist), medicine
  kits, inn/chapterhouse cures, HP drain, severe-condition camp interruption
  during travel, rescue.
- Food cadence (Bushcraft eat-every-other-night), starving penalty.
- Encounter condition gating (`has`/`tag`/`quality`/`meets`) — no content gates
  on travel conditions today (verified: zero grep hits).

## Phases

1. **Engine** — `lib/Game/Travails.cs` (hazard table in BalanceData, ledger on
   PlayerState, AccrueStep/AccrueNight/Summarize). New `TravailsTests`. Old-system
   equivalence math in test comments. **DONE 2026-06-07**: `lib/Rules/HazardDef.cs`
   (3 channels, per-tier thresholds, flavor fragments), `lib/Game/Travails.cs`,
   `lib/Game/TravailSummary.cs` (TravailTally/TravailLine/TravailSummary),
   `PlayerState.TravailLedger`, `BalanceData.Hazards`; 23 tests incl. foundry-RT
   calibration cases. Note: cold mitigation is `sleeping_kit` (Wool Bedroll) —
   there is no fur_coat item.
2. **Wiring** — server `travel` + `move` cases accrue; summary on all stop reasons;
   EndOfDay gutted per kill list; settlement clear plumbing removed.
   **DONE 2026-06-07.** Notes: every travel termination flushes (settlement →
   ArrivalInfo.Travails, wilderness/interruption → TravelInfo.Travails); flush
   happens BEFORE store.Save so the cleared ledger persists; AccrueNight called
   at both EndOfDay.Resolve sites, guarded on non-settlement nodes; manual moves
   flush on settlement arrival only. EndOfDay.Resolve lost biome/tier/rng params.
   ConditionsClearedThisTurn, PendingNoBiome + the `no_biome` mechanic flag
   (zero content used it), CampThreatInfo, ClearedConditionInfo all removed.
   Interim until phase 3: encounter-applied travel conditions still drain and
   still silently clear on settlement entry (SettlementRunner keeps the loop,
   minus the dead out-param). Smoke-tested against the live server: arrival
   summary, wilderness-end summary, no double-count across journeys.
3. **Rules + content** — ConditionDef/ConditionFlavor/ItemDef cleanup; sweep the
   11 .enc files; `EncounterCli check` must pass (checker will reject the dead
   condition ids — good). **DONE 2026-06-07.** Notes: swept 14 mechanic lines
   (Lost.enc failure blocks fold exhausted into the existing hit → damage_spirits 3;
   Old Assay Station storm = flat 4; Conscripts nets +1 heartened). ConditionDef
   kept only Id/Name/Stacks/Severity/SpecialEffect; Minor severity retained for
   future (OQ-3 resolved: keep). `lost` condition survives (Hermit/Lost.enc
   mechanic, Bushcraft resist). PassiveImmunities deleted — gear tooltips now
   read hazard mitigation from HazardDef.MitigatingItemId. Settlement-entry
   clearing fully gone. SkillFlavor/ArcRewards Bushcraft text now describes the
   rate discount (and Cunning resist odds corrected 30/60 → 40/80 to match the
   engine). 157 .enc files pass check.
4. **UI** — travails summary component (arrival panel + interruption strip),
   camp screen de-threated, CLI `status`/`travel` output. Style spec applies
   (`project/screens/styles.md`).
5. **Tests + docs** — EndOfDayTests rewrite, Orchestration travel tests,
   mechanics_reference.md update, condition_rework.md supersede note, TODO.md
   edits (close the design-conversation threads this resolves; rewrite the
   "pad the roster" item as "add hazard channels").

## Open questions

- **OQ-1 (rates)**: ~~needs a pass against production route lengths~~ — done
  2026-06-06, see Calibration section + `project/reference/map_route_lengths.md`.
  Boots question also resolved: keep zeroing fatigue (dungeon-reward valve).
  Residual: playtest only.
- **OQ-2 (manual-move summary moment)**: with `move` stepping, the ledger flushes
  on settlement arrival — should the nightly camp screen also show a running
  "travails so far" tally so a long manual trek isn't a black box until arrival?
- **OQ-3 (severity enum)**: with Minor empty, collapse `ConditionSeverity` and the
  minor/severe split, or keep the enum for future minor conditions? Leaning keep
  (cheap, and arc content may want minor encounter-applied conditions later).
- **OQ-4 (fatigue scope)**: does fatigue accrue on nights when the player
  *chose* to camp mid-journey vs. only forced road nights? Current draft: every
  on-road night accrues; settlement nights never do.
- **OQ-5 (interrupted-journey UX)**: exact placement of the travails strip when an
  encounter/combat hijacks the screen mid-journey — before the encounter card,
  after resolution, or folded into the next exploring view?
