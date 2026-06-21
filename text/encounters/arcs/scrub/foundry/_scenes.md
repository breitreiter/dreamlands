# The Foundry — scene breakdown (`_scenes`)

Structural distillation of `TheFoundry.md` (the brief). Prose, mood, the
surface-vs-reserved deploy discipline, and all NPC interiority live in the
brief and its **Canon** block; this file is the **topology + state contract**
decompose wires. When the two disagree, the brief's Canon governs intent and
this file governs structure — keep them in sync.

- **Arc:** Scrub Tier 3 capstone. Front-matter `[vignette dungeons/foundry]`,
  `[trigger none]` on arc-internal encounters.
- **Reward item:** `shimmering_blade` (T3 unique gear; arc/reward only).
  Granted mid-arc in **3b**, retained to every resolution.
- **Combat:** none. The kill is a narrative outcome, not a `.fight`.

## Shape

Short linear lead-in (**1 Gate → 2 Lamp**) → a **fully-connected three-space
triangle** (**3 The Floor**: 3a Assembly / 3b Cutting Machine / 3c Press, any
order, navigation by sound) → a single **counter-gated finale** (**4 Control
Room**) reachable only once all three spaces are seen.

Topology preference for the triangle: **lateral, not hub-and-return.** Each
space links to the other two by their sound; do not force the PC back through
a central hub between spaces (no recap pattern). Entry from the Lamp lands the
PC at a three-way choice; the route to the Control Room appears once all three
visited-tags are set. Decompose owns the exact wiring — a thin entry node is
fine; a forced return-to-hub after each space is not.

## State map

**Tags** (`foundry.*`):
- `foundry.assembly_seen` — set on first entry to 3a
- `foundry.cutter_seen` — set on first entry to 3b
- `foundry.press_seen` — set on first entry to 3c
- Control-Room route requires all three.

**Qualities:**
- `foundry.challenge` — `+quality foundry.challenge 1` each time the PC
  intervenes for a husk (available in **3a** and **3c**; optional both times).
  Range 0–2. Finale split: `quality foundry.challenge 2` = **HIGH**; the
  `@else` (0–1) = **LOW**.

**Items:**
- `shimmering_blade` — `+add_item` in **3b** (Suhail presses it on the PC).
  The finale kill is gated `has shimmering_blade`.

**Global presence tag** (`foundry_known`): set by the hooking storylet, not
authored in this pass.

## Scenes

### 1. The Gate — `Start.enc`
Role: entry / hook. Days through Color-tainted scrub; brutalist windowless
factory; PC refused at the gate despite guild standing; the vagrant Suhail
walks through and calls the PC in under his name.
- **Choice (unconditional):** follow him in → `+open` 2 Lamp.
- Optional bail ("turn back, this isn't guild business") → `+flee_dungeon`,
  no reward. *Open Q below — include or not.*
- Sets: none. Leads: → 2.

### 2. The Lamp
Role: character intro + the lamplight constraint that governs every later
space. Suhail names himself, the carbide-lamp handoff (*"You'll want this. I
won't."*), the Color in his eyes, the *"Kesharat privilege"* line. Deploys the
"his face doesn't match his story" tease (paid off in 3b).
- **Choice (unconditional):** out onto the floor → `+open` the Floor entry.
- Sets: none. Leads: → 3.

### 3. The Floor — three spaces, any order
Entry from the Lamp lands the PC where three sounds call (clicking benches /
slamming presses / shrieking cutter). Each space below sets its visited-tag on
first entry and links laterally to the other two (hide-when-visited, 1a.ii).
Once all three tags are set, the route to **4 Control Room** opens.

#### 3a. Assembly — *She can't hear you.*
Husk-regard beat, **no real cost.** Workers in the dark with Suhail's eyes;
*"SHE CAN'T HEAR YOU"*; Suhail's aimed petty cruelty (knocks a piece down,
tips a pin-bin); the PC may set the work right.
- Sets: `foundry.assembly_seen`.
- **Intervene** (optional) → `+quality foundry.challenge 1`. Free; no check,
  no condition.
- **Watch / let it be** (optional) → no quality.
- Lateral choices: → 3b, → 3c (each `[requires !tag <target>_seen]`).
- Exit to 4 once all three seen.

#### 3b. The Cutting Machine — *the offered weapon*
The blade-and-history space. House-tall cutter; Suhail shuts it down, draws
the mill-blade, wraps the tang; tells the unsealing (≈200 years, the monks,
the seal, the ordering-sickness → "the lattice"); blade-at-throat; presses
`shimmering_blade` on the PC. **No husk beat, no `foundry.challenge` here.**
- Sets: `foundry.cutter_seen`. Grants: `+add_item shimmering_blade`.
- Lateral choices: → 3a, → 3c (hide-when-visited).
- Exit to 4 once all three seen.

#### 3c. The Press — *You're grieving a machine.*
Husk-regard beat, **the PC's own risk.** Suhail lobs a tool into the press
strike-zone; a husk crawls in after it, indifferent to the die; the PC may
haul it clear.
- Sets: `foundry.press_seen`.
- **Intervene** (optional) → `+quality foundry.challenge 1`. Carries risk —
  *open Q:* picker (e.g. `check combat correct:… wrong:…` or bushcraft) with
  `injured` on the wrong branch, vs. a flat risky-but-successful pull. Either
  way intervening sets the quality.
- **Watch / let it be** (optional) → no quality.
- Lateral choices: → 3a, → 3b (hide-when-visited).
- Exit to 4 once all three seen.

### 4. Control Room — the finale
Reachable only with `foundry.assembly_seen && foundry.cutter_seen &&
foundry.press_seen`. Suhail takes the brakes off the plant (delayed,
escapable runaway), confesses the decades of provocations, poses the
unanswerable question. Then the **counter check** on `foundry.challenge`.

- **LOW** (`@else`, challenge 0–1): the act framed as a switch — *"Press it,
  or stop me."*
  - **Stop him with the blade** `[requires has shimmering_blade]` →
    kills him cleanly; the runaway proceeds regardless → `+add_level`,
    `+finish_dungeon`. (PC keeps `shimmering_blade`.)
  - **Draw your own weapon on him** (ordinary weapon) → **fails** — he can't
    be killed by ordinary steel. Folds into the let-him-finish outcome (he
    proceeds; the switch goes).
  - **Let him finish** → `+add_level`, `+finish_dungeon`; epilogue: engineers
    on the road asking, with professional interest, whether it came down
    cleanly.
- **HIGH** (`quality foundry.challenge 2`): no switch offered. He poses the
  question over the emptiest husk; the move is **refusing the terms**, not
  winning the husks-are-real argument. They walk out together, blade in hand
  → `+add_level`, `+finish_dungeon`.

## Endings (summary)

| Route | Trigger | Verb | Reward |
|---|---|---|---|
| Kill (blade) | LOW + `has shimmering_blade` + choose to stop him | `+finish_dungeon` | `+add_level` (+ keeps blade) |
| Let finish | LOW + let him / failed ordinary-weapon attempt | `+finish_dungeon` | `+add_level` |
| Refuse the terms | HIGH | `+finish_dungeon` | `+add_level` |
| Bail at the Gate | (optional) turn back | `+flee_dungeon` | none |

All non-bail routes resolve the arc's central situation → default `+add_level`.
T3 unique gear (`shimmering_blade`) is granted in 3b regardless of ending.

## Open questions for decompose

1. **Gate bail route** — include a `+flee_dungeon` "turn back" at the Gate, or
   commit the PC once they choose to follow? Brief implies commitment; a bail
   only matters for the §3.1 unconditional-choice floor, which the forward
   "follow him in" already satisfies. Lean: no bail.
2. **3c intervention cost** — picker with `injured` on the wrong branch, or a
   flat risky-success? The brief frames real risk ("the air move ahead of
   it") but doesn't mandate a check. Lean: a picker, so the risk is live.
3. **Counter granularity** — only two increment points (3a, 3c), so LOW spans
   0–1 and HIGH is exactly 2. Confirm the binary split is intended (it is, per
   the dial-down).
4. **Normal-weapon-fail wiring** — the failing ordinary-weapon attempt must be
   a *real, selectable* outcome at the finale (alongside the blade-kill), or
   the kill-gate is invisible since the PC always carries the blade. This is
   the one place the reveal depends on offering the "wrong" option on purpose.
