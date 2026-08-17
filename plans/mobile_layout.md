---
kind: plan
title: "Mobile layout — make the web client survive phones"
state: ready
created: 2026-06-22
updated: 2026-08-17
status: ready — shape settled (audit + Combat reflow/review-gate sketched). Launch blocker per TODO.md §1. Two prototypes gate full commit: Combat monster banner (hit-splat anchoring) + gated turn loop. 2026-08-17 — the journey's-end dialog moved from "unaudited" to confirmed broken: it soft-locks when tall (see [[alert_dialog_overflows_viewport]]), and its one-line height cap jumps the queue ahead of everything else here.
touches:
  features: [web-ui]
  files:
    - ui/web/src/screens/Encounter.tsx
    - ui/web/src/screens/Market.tsx
    - ui/web/src/screens/Bank.tsx
    - ui/web/src/screens/Inventory.tsx
    - ui/web/src/screens/Combat.tsx
    - ui/web/src/screens/Explore.tsx
    - ui/web/src/screens/Camp.tsx
    - ui/web/src/components/TopBar.tsx
provenance:
  author: claude
---

# Mobile layout

Socials traffic is mostly phones, so a desktop-first client that breaks on a
360–390px screen is the single biggest launch blocker (TODO.md §1). This plan
captures the shape of the problem after a code audit — what's already fine,
what's a shared fix, and what's genuinely hard. Not yet committed to
implementation.

## The strategic fork

Two ways to do this:

- **Responsive-reflow** — one component tree, Tailwind `md:` breakpoints
  collapse multi-pane layouts. The codebase already leans this way (Encounter
  and the arrival dialog use `md:` gates).
- **Dedicated mobile layouts** — a `useIsMobile()` hook branches to a different
  tree for the hard screens.

**Reflow wins everywhere visual.** Originally Combat looked like it might force a
dedicated tree; the worksheet audit (below) shows the *layout* reflows too — the
slot board is only 240px and already fits a phone. So all five screens are
visual reflow + `md:` breakpoints. The **one** exception is behavioral, not
visual: Combat's turn loop needs a mobile-only **review gate** (you lose the
desktop's review-while-planning simultaneity when cards stack), which is a
`useIsMobile()`-gated state — see Combat below.

**The template already exists in the codebase:** `Encounter.tsx:109` hides the
art pane with `hidden md:block w-[45%]` and lets the narrative take `flex-1`.
Copy that pattern everywhere a side pane needs to vanish on mobile.

## Audit — what the code actually does

### Already fine (little/no work)

- **Encounter** (`Encounter.tsx`) — art pane is already `hidden md:block`
  (line 109); prose is `flex-1 overflow-y-auto` (line 123). Structurally done.
  *Optional enhancement:* render the preview art as a banner above the prose on
  mobile instead of showing nothing. Polish, not a blocker.
- **Explore map** (`Explore.tsx`) — full-bleed Leaflet map with the
  InstrumentCluster as a **bottom-center overlay** (line 769), not a two-pane
  split. The map itself needs nothing. Only the cluster is a problem (below).
- **Inventory** (`Inventory.tsx`) — already `flex-1 min-w-0`, no fixed panes.
  Survives reflow. One mobile nicety: it's an *overlay*; on phones overlays
  want to be full-screen sheets, not floating panels.

### Shared fix — one pattern, two screens

- **Market** (`Market.tsx:368, 480`) and **Bank** (`Bank.tsx:116, 160`) are
  true two-pane (`flex-1 border-r` + `flex-1 border-l`, each `min-w-0`).
  `min-w-0` means they don't overflow — they squish to two unusable slivers,
  which is worse than overflow.
- Fix for both: **stack the panes below `md`**. Since the panes are Buy/Sell
  and Deposit/Withdraw, a **segmented tab toggle** on mobile is probably nicer
  than a long stacked scroll. Same component pattern solves both → one unit of
  work.

### Confirmed broken — the journey's-end dialog

Was "unaudited"; it is now a reproduced soft-lock. Full writeup in
[[alert_dialog_overflows_viewport]]; the fix lands here rather than standalone
because it is the same single-column-collapse problem this plan exists to solve.

- **The dialog cannot scroll.** `AlertDialogContent`
  (`components/ui/alert-dialog.tsx:59`) is `fixed` + `translate-y-[-50%]` with a
  width cap and **no height cap or overflow rule**. Taller-than-viewport content
  bleeds off both edges and the page scrollbar cannot reach it, so the sole
  dismiss button becomes unreachable. Hit in production at Fenwick with three
  deliveries at once.
- **The single-column collapse is what makes it tall.** `Explore.tsx:807` only
  splits into two columns at `md:`, so below that the travails list and every
  delivery (name + flavor paragraph + payout line each) stack in one column.
- Fix, in order:
  1. `max-h-[calc(100dvh-2rem)] overflow-y-auto` on `AlertDialogContent` — one
     line, un-sticks *every* dialog in the app (`Explore`, `Inventory`,
     `Market`). `dvh` not `vh`, for mobile browser chrome.
  2. Pin header + footer and scroll only the body, so "Continue" stays visible
     while deliveries scroll. Per call site, not in the component.
  3. Decide whether the delivery list wants its own capped scroll region, and
     whether two columns should kick in earlier than `md:`.
- `max-w-3xl` on a 360px screen still needs the look it always needed; step 1
  does not address width.

### Contained redesign

- **Instrument cluster** (`Explore.tsx:320` `InstrumentCluster`) — the
  bottom-center overlay with inventory/service wings is too wide for a phone.
  Self-contained; redesign the cluster, leave the map alone.

  The cluster carries two distinct kinds of thing, and the mobile split falls
  out of that:
  - **Status (glanceable):** conditions bar, spirits, health, food-count pill,
    day/night vignette circle.
  - **Actions (tappable):** inventory, reference, settlement services
    (`onOpenService`), enter-dungeon.

  **Mobile direction (chosen): a FAB that opens a menu on press, mobile only.**
  The FAB owns the *actions* only. The *status* half is already duplicated by
  TopBar (health/spirits/gold/conditions render there on every screen), so on
  mobile we **drop status from the cluster entirely** and let TopBar carry it —
  which keeps vitals glanceable instead of buried behind a press, and keeps the
  FAB menu small.

  FAB menu contents are **context-dependent**:
  - Always: Inventory, Reference.
  - At a settlement: the implemented services (market/bank/inn/etc.).
  - At a dungeon: Enter dungeon.
  - On the open road: just Inventory + Reference (light — fine).

  Open question: food-count and day/night are status, but neither is in TopBar
  today. Either surface them in TopBar on mobile or accept they're explore-only
  glance info that drops on phones. Decide during build.

### The hard one — Combat

- **Combat** (`Combat.tsx`) — monster-art pane `min-w-[320px]` (line 496) +
  worksheet pane `min-w-[420px] max-w-[820px]` (line 549). Combined min ~740px.
  Hiding the art still leaves the worksheet at 420px min — overflows a 360–390px
  phone. So **hiding art is not enough**; the worksheet itself must reflow, and
  that's exactly where the action-tracker legibility concern bites.
#### Worksheet anatomy (what's actually there)

- **Outer shell** (`Combat.tsx:494`): two panes side by side.
  - LEFT (`min-w-[320px]`): monster vignette art **+ the nameplate & HP bar**
    (`:533–545`). Art is decorative but the nameplate/HP is essential status.
    Hit-splat animations (`HitSplat`/`HitLens`) anchor to the monster image
    hitbox (`hitboxRef`, `:506`) during playback.
  - RIGHT (`min-w-[420px] max-w-[820px]`): TopBar (player HP/spirits/Flee) +
    a **vertical scroll stream** — intro, prior turns, active turn, playback.
- **The slot board** (`SlotRow:716`): a rigid `width:240, height:60` box with
  three absolutely-positioned circles reading **`[monster] + [you] = [outcome]`**
  (offsets 0 / 90 / 180). 240px **already fits a phone** (≈312px usable at
  `px-6`). Three stacked = the active board.
- **Active turn** (`ActiveTurnCard:965`): `flex items-start` row — slot board
  (`shrink-0`) on the left, **move pool** on the right as a 2-col grid of
  numbered `ActionButton`s (`:1006–1028`). Tapping a move fills the next pending
  slot.
- **Prior/playback turns** reuse `SlotRow` with its `description` region
  (`:768`, `flex-1`) — the narration sits *beside* the 240px board on desktop.

#### Mobile reflow (chosen: reflow, not a dedicated tree)

Because the 240px board already fits, the worksheet needs **reflow, not a bespoke
layout**. Four changes:

1. **Outer panes stack.** Below `md`, drop the side-by-side. Relocate the
   **monster nameplate + HP** into a compact **top banner** (art as dimmed
   background, nameplate + HP bar overlaid) above the worksheet stream. Keep the
   banner **sticky during playback** so hit-splats retain an anchor; the smaller
   banner is a smaller hitbox but still works.
2. **Active turn stacks.** `ActiveTurnCard`'s `flex items-start` → `flex-col`
   below `md`: slot board on top (full width, fits), move pool grid below it.
   Player still glances up to see which slot is pending. Keep the move pool
   **2-col** (1-col only if labels overflow — tuning point).
3. **Prior/playback descriptions wrap below.** The `SlotRow` description region
   can't sit beside a 240px board in ~312px usable. Below `md`, drop the
   narration **under** the equation instead of beside it.
4. **Relax the pane min-widths** (`min-w-[320px]`/`min-w-[420px]`) under `md` so
   nothing forces horizontal overflow.

5. **Turn-loop review gate (behavioral — the one non-CSS change).** The desktop
   layout doesn't just *fit* the prior turn and the next plan on screen — its
   tall (≤820px, full-height) right pane keeps the **resolved PriorTurnCard
   visible while you plan the next turn**, so review-while-planning is
   simultaneous. Stacked narrow on a phone, each card is taller (descriptions
   wrap below each slot), so that simultaneity is gone — you'd scroll between
   "what just happened" and "what I'm about to do." Replace the lost *spatial*
   affordance with a *temporal* one: after `PlaybackCard` finishes, **do not
   auto-render the next `ActiveTurnCard`**. Hold on the resolved turn (monster HP
   updated, outcomes shown) and present a **"What's the plan?" / Ready** button;
   tapping advances to — and scrolls to — the fresh planning card.

   ```
   plan → commit → playback → ┌ REVIEW (resolved turn held) ┐ → next plan
                              │ [ What's the plan? → ]       │
                              └──────────────────────────────┘
   ```

   This is a new turn-loop **state**, not a breakpoint — so Combat is the one
   screen that needs a genuine mobile-specific path (a `useIsMobile()`-gated
   render branch), even though the visual side is still reflow.
   **Open question:** keep the gate mobile-only (desktop keeps its continuous
   scroll, no added click), or unify both platforms on the gated loop for a
   consistent rhythm. Lean mobile-only.

Rough mobile shape:

```
┌──────────────────────────────┐
│ TopBar: ♥HP  ✦spirits   [Flee]│  player status (existing)
├──────────────────────────────┤
│ ░░ MONSTER BANNER (art bg) ░░ │  nameplate + HP bar
│  Gloomfang        [████░░] 18 │  sticky during playback (hit anchor)
├──────────────────────────────┤
│  ↓ worksheet stream (scroll)  │
│ ┌ active turn ──────────────┐ │
│ │  ?  +  Ⅰ  =  ?            │ │  240px board, fits phone width
│ │  ?  +  Ⅱ  =  ?            │ │
│ │  ?  +  Ⅲ  =  ?            │ │
│ │ ───────────────────────── │ │
│ │ [1 Strike] [2 Guard]      │ │  move pool 2-col, BELOW the board
│ │ [3 Read ]  [4 Heavy]      │ │
│ └───────────────────────────┘ │
└──────────────────────────────┘
```

The *visual* layout stays one component tree with `md:` breakpoints. The
mobile-specific pieces are: the **monster banner** (relocated nameplate/HP +
sticky hit-anchor — prototype to confirm hit-splats read at banner size) and the
**review gate** (change 5 — the one behavioral, `useIsMobile()`-gated state).
Everything else is CSS.

### Unaudited / to check

- **TopBar** (`components/TopBar.tsx`) — shared chrome on every screen
  (`flex items-center gap-4`, unbudgeted width). If it wraps/overflows on narrow
  widths it breaks *everywhere*. Cheap to check, high blast radius — eyeball
  first.
- **Camp** (`Camp.tsx`) — end-of-day (meal/medicine/threats); not yet audited
  for two-pane.

## Two global knobs (app-wide multipliers)

Set once, every screen benefits — flagged in TODO.md §1:

- **Base font** — 20px is too large for phones. Token + breakpoint.
- **Touch targets** — many icon buttons are `w-5 h-5`; below comfortable touch
  size. Token pass.

## Rough ordering by risk

1. **Cheap/global, first:** font-size + touch-target tokens; eyeball TopBar.
2. **Shared pattern, two screens:** Market + Bank → stack-or-tab below `md`.
3. **Contained:** Instrument cluster redesign.
4. **Hardest (but still reflow):** Combat — stack panes + monster banner +
   active-turn `flex-col` + wrap prior-turn descriptions. Prototype the monster
   banner (sticky hit-anchor) first.
5. **Free/polish:** Encounter art-banner; Inventory overlay → mobile sheet; Camp
   + dialogs cleanup.

Exception to the ordering: the dialog height cap (step 1 of the journey's-end
fix) is a one-line change against a live soft-lock, so it goes first regardless
of where the rest of that work sits.

## Note

The real surface is smaller than the TODO's "biggest single item" framing
implies: Encounter is already done and Explore's map needs nothing. The genuine
work is Market/Bank (shared), the cluster, Combat, plus the two global knobs.
