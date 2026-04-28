# .tac Removal Plan

Working doc. Drives the rewrite of the 14 authored `.tac` files into either inline `.enc` checks or new `.cmb` combat encounters, ahead of deleting the tac scaffolding (`combat_pivot.md` "Cleanup" section).

## Two tracks

- **Track P (port)** — keep as a structured encounter, port to `.cmb`. Two files.
- **Track F (fold)** — collapse into the parent `.enc` as an inline skill check on the choice that opens it. Twelve files.

Track F is intentionally lossy. The `.tac` resource-management puzzle does not survive the fold; what survives is the **gating roll, win prose, lose prose, and verbs**. The `[stat <skill>]` from the `.tac` becomes the skill on the new `.enc` check; the clock/openings/challenges machinery is dropped entirely. This matches the pivot doc's view that single-check resolution is the right shape for `.enc` flavor outcomes.

## Track P — port to `.cmb`

### 1. Road Toll Fight → `plains/tier1/Combat/Road Toll Fight.cmb`

Why port: T1 introduction to combat. The first time the player pulls a sword, it should land in the new combat screen, not collapse to a single roll. Sets player expectations that `.cmb` is the format that means "real fight."

Author notes:
- T1 baseline: HP 24, AC 11, atk +4, basic 1d4, heavy 2d8 / 3-round timer.
- Single named bandit-leader stat block; the chipped-sword chief from the existing flavor is the obvious choice. Use `plains_bandit.png` (Tob Ashford, the Mask) **or** author a fresh portrait for the toll-leader if Ashford is reserved for his own encounter slot. Open question: does Ashford get this slot, or do we need a 19th sprite?
- `+repool false` — the leader is a one-and-done.
- Win verbs: keep current "scattered, you stand over the toll post" beat, drop a small gold reward (current chase-failure penalty is `-25 gold`; symmetry suggests a `+10–15 gold` win).
- Lose verbs: keep the current `+damage_spirits 2` + `+lose_random_item`. Health damage is now handled by the combat resolver itself, so the loss verbs only carry the post-fight consequence.
- Surprise: probably not. The bandits are already aware of you (toll setup); player goes first only if Bushcraft surprise check passes.

### 2. The Gauntlet → reframe as Sentinel 7-C combat

**Design call needed before authoring.** The existing `.tac` is a Cunning-stat stealth sequence past worker drones, ending in The Plaza (with `irradiated` on failure). Converting that into a turn-based combat is a meaningful narrative pivot, not a port.

Two viable shapes:

- **Shape A — Replace the gauntlet with the Sentinel fight.** The Gauntlet `.tac` deletes; `the_city/Start.enc`'s "Go with him" choice opens directly into a new `.cmb` against Sentinel 7-C. Torben's "we can thread through" prose retires. The arc becomes: Perimeter → fight Sentinel → Plaza. Cleanest mechanically, but loses the stealth-Cunning flavor and Torben's "thread the patrols" beat that defines his character.
- **Shape B — Keep the stealth, add Sentinel as the next room.** Fold The Gauntlet into Start.enc as a Cunning check (Track F treatment), then introduce a new `.cmb` against Sentinel 7-C as the next encounter after The Plaza, gating progress deeper into the arc. Preserves Torben's character beat. Adds a node, not a port. Probably the right call given the plains_boss lead-in already places Sentinel "past the gate, at the next intersection" — that's the room **after** Plaza, not the entrance puzzle.

**Recommendation: Shape B.** The stealth-past-drones moment is the better narrative use of the gauntlet `.tac`'s content; Sentinel 7-C wants its own dedicated fight node, not to be retrofitted onto the entry puzzle. This means the_city arc gets:

- Start.enc with an inline Cunning check folding in The Gauntlet (Track F).
- A new `.cmb` for Sentinel 7-C placed after The Plaza in the arc (new authoring; not strictly tac removal).

If we go Shape A instead, The Gauntlet moves to Track P and Sentinel 7-C is unaffected.

## Track F — fold into parent `.enc`

For each entry: source `.tac` → parent `.enc` choice → fold treatment.

Fold treatment template:

```
* <choice text> = <preview>
  +check <stat> <DC>
  @if check <stat> <DC>
    <success prose, 1–2 paragraphs>
    <success verbs from .tac>
  @else
    <failure prose, 1–2 paragraphs>
    <failure verbs from .tac>
```

DCs target the existing skill-check ladder (`mechanics_reference.md`). Tier 1 ≈ DC 12, Tier 2 ≈ DC 14, Tier 3 ≈ DC 16, with ±2 per encounter for variance.

### Plains tier 1

**Road Toll Chase** → `plains/tier1/Road Toll.enc`, "Make a run for it"
- Stat: Bushcraft. DC 12.
- Success prose: new (current `.tac` only has failure prose; need ~1 paragraph for "you lose them in the grass").
- Failure verbs (from `.tac`): `+damage_spirits 2`, `+rem_gold 25`, `+add_condition exhausted`.
- Note: parent already has `+repool` on this choice. Carry it through the fold.

### Plains tier 2

**Collapsed Earthwork Traverse** → `plains/tier2/Collapsed Earthwork.enc`, "Scramble down"
- Stat: from `.tac` (need to check; likely Bushcraft or Combat). DC 14.
- Success / failure prose and verbs lifted from the `.tac`.

**The Conscripts Combat** → `plains/tier2/The Conscripts.enc`, "Rescue the locals"
- Stat: Combat. DC 14.
- Success verbs: `+add_item scimitar`, `+quality faction.legion -5`, `+quality faction.scavengers +5`.
- Failure verbs: `+skip_time night`, `+add_condition injured`, `+add_condition exhausted`, `+lose_random_item`, `+quality faction.legion -5`, `+quality faction.scavengers +1`.
- Lossy: the multi-soldier formation flavor (Wynne/Taylor/Jeoffrey/Tanner) collapses to a single roll. Acceptable; the prose can keep the names.

**The Courier Conflict** → `plains/tier2/The Courier.enc`, "Talk her down"
- Stat: from `.tac`; likely Insight or Charisma equivalent. DC 14.
- Success / failure prose and verbs lifted.

**The Requisition Line Conflict** → `plains/tier2/The Requisition Line.enc`, "Talk your way out"
- Stat: same family as Courier. DC 14.
- Success / failure prose and verbs lifted.

**The Scavengers Fight** → `plains/tier2/The Scavengers.enc`, "Stand your ground"
- Stat: Combat. DC 14.
- Candidate for promotion to Track P? The Scavengers is a solid T2 set piece and we have the `plains_bandit.png` + `plains_robot_2.png` sprites unallocated to specific encounters. **Defer to authoring pass** — fold for now, promote later if a sprite finds it a home.

**The Scavengers Chase** → `plains/tier2/The Scavengers.enc`, "Make a run for it"
- Stat: Bushcraft. DC 14.
- Success / failure prose and verbs lifted.

### Plains tier 3

**The Gentle Giant Escape** → `plains/tier3/The Gentle Giant.enc`, "Sprint through the nearby streets"
- Stat: Bushcraft. DC 16.

**The Gentle Giant Sneak** → `plains/tier3/The Gentle Giant.enc`, "Sneak underneath the thing"
- Stat: Cunning. DC 16.

**The Passage Combat** → `plains/tier3/The Passage.enc`, "Stand and fight"
- Stat: Combat. DC 16.
- Success verbs: `+add_condition injured`, `+add_item mountain_regiment_armor`.
- Failure verbs: `+advance_time 3 no_sleep no_meal no_biome`, `+add_item mountain_regiment_armor`, `+add_condition injured`, `+add_condition irradiated`.
- Lossy: the long flavor pass with the malfunctioning Iron is worth preserving in the fold prose, but the per-leg targeting goes.

**The Passage Door** → `plains/tier3/The Passage.enc`, "Figure out the door"
- Stat: from `.tac`; likely Wits or Cunning. DC 16.

**The Passage Traverse** → `plains/tier3/The Passage.enc`, "Find another way"
- Stat: Bushcraft. DC 16.

### Arcs

**The Gauntlet** → `arcs/plains/the_city/Start.enc`, "Go with him" (per Shape B above)
- Stat: Cunning. DC 14 (treat as T2 since it's an arc gate, not deep wilderness).
- Success: chains to The Plaza unchanged.
- Failure: chains to The Plaza with `+add_condition irradiated` (matches current `.tac` failure).
- The Gauntlet's failure prose is a near-duplicate of its success prose padded with one extra paragraph. The fold can collapse the duplication — share the Plaza setup beat, branch only on the drone-arm flash + irradiated tag.

## Author-side workflow per fold

1. Open the parent `.enc`.
2. Read the corresponding `.tac` for stat, prose, and verbs.
3. Replace the `+open "<title>"` line on the choice with an inline `+check / @if / @else` block.
4. Lift success prose, failure prose, and both sets of verbs.
5. Run `dotnet run --project text/encounter-tool/EncounterCli -- check text/encounters` to confirm syntax.
6. Delete the `.tac` file.
7. Bundle and confirm `tactical.bundle.json` shrinks accordingly.

Don't try to do this in one pass. The fold prose is bespoke per encounter, and several `.tac` files only have failure prose authored — those need new success-branch text.

## Order of operations

1. **Folds first**, in roughly this order: Plains T1 (warmup, smallest), Plains T2 (bulk), Plains T3 (most lossy, hardest), then the_city/Start.enc (Shape B). Folds are independent of the combat resolver landing; they unblock tac-scaffolding deletion immediately.
2. **Road Toll Fight `.cmb` authoring** lands when Phase 1 of the combat landing plan is done (engine integration). Until then, the toll-fight choice can fold to a temporary inline check; promote to `.cmb` once the runtime exists.
3. **Sentinel 7-C `.cmb`** lands in Phase 4 (content authoring) of the combat landing plan, alongside the rest of the 18-monster batch.
4. **Tac scaffolding deletion** (per `combat_pivot.md` Cleanup section) waits until all 14 `.tac` files are gone. That's the trigger for ripping `lib/Tactical/`, the CLI commands, the sim tools, and the bundle wiring.

## Open questions

- **Does Tob Ashford take the Road Toll Fight slot, or do we need a fresh portrait?** Ashford's lead-in in `monster_inventory.md` reads as a separate one-off encounter, not the toll. Probably: separate encounters, separate sprites. Flag for art.
- **Shape A vs Shape B for the_city arc.** Recommendation above is Shape B, but it's a narrative call.
- **Stat lookup for several `.tac` files.** Several entries above need a quick read of the `.tac` `[stat …]` header to confirm; not done in this draft.
- **Promotion candidates from Track F.** Scavengers, Conscripts, Passage Combat, and Gentle Giant are all combat-shaped enough that they could become `.cmb` files later. Track F is the floor, not the ceiling.
