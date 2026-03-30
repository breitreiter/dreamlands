# Journey Hazard Blocks — Design Sketch

Working document. Companion to `journey_hazards.md`.

---

## 1. Proposed Skill System

### Current State

Six skills: Combat, Cunning, Negotiation, Bushcraft, Luck, Mercantile.

### Problem with Mercantile

Mercantile is too narrow. It applies to deliveries and shop interactions — both of which happen in settlements, not during journey hazards. It has almost no blocks where it's the primary currency. Drop it.

### Proposed: Five Skills

| Skill | Domain | Journey Role | What it buys |
|---|---|---|---|
| **Combat** | Fighting, physical force, intimidation | Deal with hostile creatures and people through violence or threat of violence | Reweight confrontation outcomes, reveal ambush details, force past physical threats |
| **Cunning** | Awareness, trickery, reading situations | Spot traps, recognize anomalies, find hidden options | Reveal hidden options, reveal hidden outcomes, spot confounds before they bite |
| **Bushcraft** | Wilderness survival, terrain reading, pathfinding | Navigate environmental hazards, find alternate routes | Reweight terrain/weather outcomes, reveal safe passages, read environmental threats |
| **Negotiation** | Persuasion, social reading, deception, authority | Deal with sentient gatekeepers, defuse social confrontations | Reweight social outcomes, reveal NPC motivations, unlock social bypass options |
| **Luck** | Fortune, improbable outcomes | Freeform reroll safety net | Works differently: % chance to trigger a reroll on bad outcomes (current system). Not a point-budget skill. |

### Why Five Is Right

- **Combat** covers all hostile confrontation blocks (5-6 blocks).
- **Cunning** covers all perception/trap/hidden-information blocks (5-6 blocks).
- **Bushcraft** covers all terrain/environmental/navigation blocks (5-6 blocks).
- **Negotiation** covers all social/gatekeeper/stranger blocks (4-5 blocks).
- **Luck** stays special — it's the safety net, not a budget currency.

Each of the four budget skills applies to roughly a quarter of all blocks, with overlap zones where two skills both apply (e.g., a trapped door accepts both Cunning and Bushcraft). This means a character with one strong skill always has options, but a balanced character has more flexibility.

### Skill as Budget

Each encounter, a character gets N points to spend, where N = skill level. Base levels range from -2 to +4. A character with Combat 3 and Bushcraft 1 entering an encounter with a Hostile Wildlife block and a Dense Terrain block has 3 coins for the wildlife problem and 1 coin for the terrain problem. Gear adds bonus coins to specific block types.

---

## 2. Proposed Gear/Tool Categories

### Design Principle

Each tool occupies 1 pack slot. Each declares: "I provide N coins of type X for blocks tagged Y." Coins from gear stack 1:1 with skill coins. The specialist tax is pack space.

### Medical Kits (from journey_hazards.md Part 1)

These are a distinct gear category — they don't provide block coins. They enable end-of-day treatment rolls for severe conditions.

| Item | Id | Cures | Biome | ShopTier | Slots |
|---|---|---|---|---|---|
| Bandage Roll | `bandage_roll` | injured | any | any | 1 |
| Siphon Lens | `siphon_lens` | lattice_sickness | scrub | 2 | 1 |
| Tonic Kit | `tonic_kit` | irradiated | plains | 2 | 1 |
| Antivenom Pouch | `antivenom_pouch` | poisoned | forest | 2 | 1 |

### Journey Tools (block currency providers)

| Item | Id | Coins | Applies To | Biome | ShopTier | Cost |
|---|---|---|---|---|---|---|
| Lockpicks | `lockpicks` | 2 | blocks tagged `lock` | any | 1 | 15 |
| Rope & Pitons | `rope_pitons` | 2 | blocks tagged `climb` | mountains | 1 | 15 |
| Trap Kit | `trap_kit` | 2 | blocks tagged `trap` | forest | 1 | 15 |
| Signal Mirror | `signal_mirror` | 2 | blocks tagged `exposure` | scrub | 1 | 15 |
| Breathing Mask | `breathing_mask` | 2 | blocks tagged `radiation` | plains | 2 | 40 |
| Lattice Filter | `lattice_filter` | 2 | blocks tagged `lattice` | scrub | 2 | 40 |
| Field Guidebook | `field_guidebook` | 2 | blocks tagged `flora_fauna` | any | 1 | 15 |
| Lantern | `brass_lantern` | 2 | blocks tagged `darkness` | any | 1 | 15 |
| Forgery Kit | `forgery_kit` | 2 | blocks tagged `papers` | mountains | 2 | 40 |
| Surveyor's Lens | `surveyors_lens` | 2 | blocks tagged `ruin` | plains | 2 | 40 |

### Existing Gear Reframing

Equipped weapons, armor, and boots already provide skill modifiers. Under the block system, these function as broad coins (they add to the relevant skill budget for any block that accepts that skill). No changes needed — a sword with Combat +3 gives you +3 coins on any block that accepts Combat.

Tools like Cartographer's Kit, Sleeping Kit, etc. retain their resist modifier functions for end-of-day resolution. The journey tools above are new and specifically feed the block system.

### Pack Budget

7 max pack slots. Endgame loadout might look like:

| Slot | Item | Purpose |
|---|---|---|
| 1 | Weapon | Broad combat coins + foraging |
| 2 | Bandage Roll | Treats injured |
| 3 | Biome-specific med kit | Treats biome severe condition |
| 4 | Journey tool #1 | Narrow coins for expected blocks |
| 5 | Journey tool #2 | Narrow coins for expected blocks |
| 6 | Cartographer's Kit / Sleeping Kit | Resist modifier |
| 7 | Journey tool #3 or second med kit | Flex slot |

Armor and boots are equipped, not packed. Veterans who know the route pack precisely (2-3 matching tools). New players are better off with broad skills.

---

## 3. Lego Blocks

### Block Structure

Every block adds 1-2 **options** to an encounter. Each option:
- Has an action label and brief description
- Accepts specific currency types (broad skills and/or narrow gear tags)
- Has an outcome table with weighted results
- May have a resource cost (food, time, spirits) to attempt

Spending coins on an option shifts weight from bad outcomes toward good ones. Base weights assume 0 investment.

### Outcome Weight Convention

All outcomes use a 3-tier weight system: **Good / Neutral / Bad**. Base weights (0 coins invested) are listed per block. Each coin spent shifts 10% from Bad to Good (i.e., each coin spent: Good +10, Bad -10, Neutral unchanged). At high investment, bad outcomes become very unlikely but never impossible.

---

### Category: Barriers

Physical, social, or environmental obstacles that block forward progress. The encounter's "core problem."

---

#### B1: Locked Passage
**Tags:** `lock`
**Accepts:** Cunning, `lock` gear
**Options:**
- **Pick the lock** — Cunning/lockpick coins. Base: 30/40/30.
  - Good: passage opens cleanly.
  - Neutral: passage opens but it takes time (+skip_time).
  - Bad: lock jams, must find another way (forces alternate option or retreat).
- **Force it open** — Combat coins. Base: 20/30/50.
  - Good: smash through.
  - Neutral: through, but injured (minor spirits loss — noise, exertion).
  - Bad: stuck, injured (-spirits), still locked.

**Biome fit:** Universal.
**Skins:**
- Stift: "The archive door is sealed with an imperial combination lock."
- Forest: "A hunter's cache, bound with knotted rope and a crude padlock."
- Plains: "A grid-era blast door, partially retracted. The control panel is dark."

---

#### B2: Vertical Obstacle
**Tags:** `climb`
**Accepts:** Bushcraft, `climb` gear
**Options:**
- **Climb over** — Bushcraft/rope coins. Base: 30/40/30.
  - Good: clean ascent.
  - Neutral: over but slow (+skip_time).
  - Bad: fall. Injured (condition: injured, 1 stack) or -spirits.
- **Find a way around** — costs time (+skip_time guaranteed), no check.

**Biome fit:** Mountains (primary), Forest, Plains (ruins).
**Skins:**
- Mountains: "A rockslide has buried the trail under loose scree."
- Forest: "A ravine cuts across the path, choked with fallen timber."
- Plains: "A collapsed overpass from the old imperial highway."

---

#### B3: Gatekeeper
**Tags:** `papers`, `social`
**Accepts:** Negotiation, `papers` gear
**Options:**
- **Talk your way through** — Negotiation/papers coins. Base: 30/30/40.
  - Good: passage granted, maybe a tip or minor benefit.
  - Neutral: passage granted but costs gold.
  - Bad: refused. Must try alternate approach or retreat (-time).
- **Bribe** — costs gold (guaranteed), auto-success. No check.
- **Sneak past** — Cunning coins. Base: 20/40/40.
  - Good: slip by unnoticed.
  - Neutral: spotted but escape. Lose time.
  - Bad: caught. Confrontation escalates (spirits loss, possible injury).

**Biome fit:** Mountains/Stift (primary), Scrub/Kesharat, Plains (military checkpoints).
**Skins:**
- Stift: "A clerk at the transit office insists your papers are out of order."
- Scrub: "A Kesharat checkpoint. An officer reviews your transit documents."
- Plains: "A legionnaire at a half-collapsed garrison demands identification."

---

#### B4: Dense Terrain
**Tags:** `terrain`
**Accepts:** Bushcraft
**Options:**
- **Push through** — Bushcraft coins. Base: 30/40/30.
  - Good: find a clear route.
  - Neutral: get through but slow (+skip_time).
  - Bad: lost (-spirits), possible minor condition (exhausted).
- **Go around** — guaranteed +skip_time. No check.

**Biome fit:** Forest (primary), Swamp, Scrub.
**Skins:**
- Forest: "The undergrowth thickens to a wall of thorns and deadfall."
- Swamp: "The channel narrows to a reed-choked passage."
- Scrub: "Wind-carved rock formations create a maze of narrow passages."

---

#### B5: Flooded Ground
**Tags:** `terrain`, `water`
**Accepts:** Bushcraft
**Options:**
- **Wade through** — Bushcraft coins. Base: 20/40/40.
  - Good: find solid footing, cross quickly.
  - Neutral: cross but soaked. -Spirits.
  - Bad: lose footing. -Spirits, lose random item from haversack (food swept away).
- **Wait for conditions to change** — +skip_time (long), auto-success.

**Biome fit:** Swamp (primary), Forest, Plains (seasonal).
**Skins:**
- Swamp: "The path disappears into knee-deep black water."
- Forest: "Spring melt has turned the trail into a stream."
- Plains: "A sunken stretch of old road, pooled with foul runoff."

---

#### B6: Barricade
**Tags:** `barrier`
**Accepts:** Combat, Bushcraft
**Options:**
- **Break through** — Combat coins. Base: 30/30/40.
  - Good: smash through quickly.
  - Neutral: through, but exhausting (-spirits).
  - Bad: hurt yourself on debris. -Spirits, possible injured (1 stack).
- **Disassemble carefully** — Bushcraft coins. Base: 40/40/20.
  - Good: clear the way, maybe salvage something useful.
  - Neutral: clear the way, takes time (+skip_time).
  - Bad: takes time and exhausting (+skip_time, -spirits).

**Biome fit:** Universal.
**Skins:**
- Plains: "Scavengers have piled wreckage across the road."
- Forest: "Fallen trees block the trail — recently cut, not storm-felled."
- Mountains: "A mine tunnel is shored up with rotting timber that's partially collapsed."

---

### Category: Threats

Active dangers. Something is trying to hurt you or the environment is actively hostile.

---

#### T1: Hostile Wildlife
**Tags:** `combat`, `flora_fauna`
**Accepts:** Combat, `flora_fauna` gear
**Options:**
- **Fight** — Combat coins. Base: 30/30/40.
  - Good: drive it off, maybe gain food (foraged meat).
  - Neutral: it retreats, you're shaken (-spirits).
  - Bad: injured (1 stack).
- **Back away slowly** — Bushcraft coins. Base: 40/40/20.
  - Good: disengage cleanly.
  - Neutral: it follows for a while, lose time (+skip_time).
  - Bad: it charges. Falls to fight outcome (reroll on Fight table).
- **Scare it off** — Cunning coins. Base: 30/50/20.
  - Good: it flees immediately.
  - Neutral: standoff, lose time.
  - Bad: enraged. Falls to fight outcome.

**Biome fit:** Forest (primary), Mountains, Swamp, Plains (wild dogs, boar).
**Skins:**
- Forest: "A boar blocks the trail, tusks lowered, breath steaming."
- Mountains: "A ridge cat watches you from the rocks above."
- Swamp: "Something large disturbs the water ahead."

---

#### T2: Ambush
**Tags:** `combat`, `trap`
**Accepts:** Cunning (to detect), Combat (to survive), `trap` gear
**Options:**
- **Fight through** — Combat coins. Base: 20/30/50.
  - Good: repel attackers, possible loot.
  - Neutral: escape but battered (-spirits).
  - Bad: injured (1 stack), lose gold or item.
- **Spotted it first** (requires Cunning investment or trap gear to reveal) — Cunning/trap coins. Base: 40/40/20.
  - Good: avoid entirely, possibly set your own counter-ambush.
  - Neutral: avoid the ambush, but lose time circling around.
  - Bad: they spot you spotting them. Falls to fight.

**Biome fit:** Forest (exiles), Scrub (clan raiders), Plains (scavengers).
**Skins:**
- Forest: "The trail narrows between two deadfalls. Too convenient."
- Scrub: "Loose stones arranged on the ridge above. Someone set this approach."
- Plains: "A burnt-out wagon in the road. A little too perfectly placed."

---

#### T3: Environmental Hazard — Radiation
**Tags:** `radiation`, `ruin`
**Accepts:** Bushcraft, `radiation` gear, `ruin` gear
**Options:**
- **Navigate through** — Bushcraft/radiation coins. Base: 20/30/50.
  - Good: find a clean path. No exposure.
  - Neutral: minor exposure. -Spirits (nausea, headache).
  - Bad: significant exposure. Irradiated (1 stack).
- **Detour wide** — +skip_time (long). Auto-success, no exposure.
- **Read the Grid markers** (requires `ruin` gear or high Cunning) — Cunning/ruin coins. Base: 50/30/20.
  - Good: markers reveal the safe corridor perfectly.
  - Neutral: partial reading. Reduces Navigate's bad weight by half.
  - Bad: misread the markers. False confidence (worsens Navigate weights).

**Biome fit:** Plains T2-T3 only.
**Skins:**
- Plains: "The stone here is glassy. The air tastes of metal and ozone."
- Plains (ruin): "Grid warning markers — corroded imperial signage, half-buried."

---

#### T4: Environmental Hazard — Lattice
**Tags:** `lattice`
**Accepts:** Cunning, `lattice` gear
**Options:**
- **Cut through the lattice field** — Cunning/lattice coins. Base: 20/30/50.
  - Good: thread through without exposure.
  - Neutral: brief exposure. -Spirits (disorientation, the Color).
  - Bad: lattice_sickness (1 stack).
- **Wide detour** — +skip_time (long). Auto-success.
- **Follow the geometry** (requires Color Lens token or high Cunning) — Cunning coins. Base: 50/30/20.
  - Good: the patterns reveal safe channels.
  - Neutral: partial reading. Halves bad weight on cut-through.
  - Bad: the geometry is fractal. Disorientation (-spirits).

**Biome fit:** Scrub T2-T3 only.
**Skins:**
- Scrub: "The air vibrates. Colors that shouldn't exist bloom in your peripheral vision."
- Scrub: "Lattice formations rise from the ground like frozen lightning."

---

#### T5: Environmental Hazard — Traps/Poison
**Tags:** `trap`, `flora_fauna`
**Accepts:** Cunning, `trap` gear, `flora_fauna` gear
**Options:**
- **Proceed carefully** — Cunning/trap coins. Base: 30/30/40.
  - Good: spot and avoid all hazards.
  - Neutral: clip a trigger. Minor spirits loss (shock/adrenaline).
  - Bad: poisoned (1 stack).
- **Bushcraft detour** — Bushcraft coins. Base: 40/40/20.
  - Good: find a clean trail around.
  - Neutral: get around, costs time.
  - Bad: the detour has its own hazards. -Spirits.
- **Identify the trap system** (requires trap gear or Cunning 3+) — Cunning/trap coins to reveal. Base: 60/30/10.
  - Good: full map of the trap line. Neutralize or bypass trivially.
  - Neutral: partial map. Improves Proceed weights.
  - Bad: missed one.

**Biome fit:** Forest T2-T3 (the Hunter's traps).
**Skins:**
- Forest: "Thin cord at ankle height, nearly invisible. A deadfall mechanism."
- Forest: "The berries here are wrong — too bright, placed too neatly on the trail."

---

#### T6: Hostile NPC
**Tags:** `combat`, `social`
**Accepts:** Combat, Negotiation
**Options:**
- **Fight** — Combat coins. Base: 20/30/50.
  - Good: defeat them. Possible loot.
  - Neutral: drive them off, you're hurt (-spirits).
  - Bad: injured (1 stack), lose gold.
- **Negotiate** — Negotiation coins. Base: 30/40/30.
  - Good: talk them down. Possible information or trade.
  - Neutral: tense standoff, they let you pass.
  - Bad: they attack anyway (falls to Fight table).
- **Flee** — Bushcraft coins. Base: 30/40/30.
  - Good: escape cleanly.
  - Neutral: escape but drop something (lose random item).
  - Bad: can't outrun them. Falls to Fight table.

**Biome fit:** Universal.
**Skins:**
- Plains: "Three scavengers emerge from the ruins, weapons drawn but not raised."
- Scrub: "A Kesharat patrol. The officer's hand rests on their weapon."
- Forest: "An exile steps from behind a tree. They're not smiling."

---

#### T7: Exposure
**Tags:** `exposure`
**Accepts:** Bushcraft, `exposure` gear
**Options:**
- **Press on** — Bushcraft/exposure coins. Base: 20/40/40.
  - Good: weather breaks or you find shelter.
  - Neutral: endure it, -spirits.
  - Bad: serious exposure. Relevant minor condition (freezing, thirsty, exhausted) or -spirits (large).
- **Make camp and wait** — +skip_time, auto-resolves. Costs food.

**Biome fit:** Mountains (storms, cold), Scrub (heat, dehydration), universal (bad weather).
**Skins:**
- Mountains: "Cloud comes down fast. Visibility drops to arm's length."
- Scrub: "The sun is a white furnace. There's no shade for miles."
- Swamp: "Rain hammers the channel. The water is rising."

---

### Category: Opportunities

Positive blocks. Finding resources, shelter, or advantage.

---

#### O1: Cache/Salvage
**Tags:** `ruin`, `lock`
**Accepts:** Cunning, `ruin` gear, `lock` gear
**Options:**
- **Search thoroughly** — Cunning/ruin coins. Base: 40/40/20.
  - Good: find valuable items (gold, food, or useful gear).
  - Neutral: find something minor (small gold, 1 food).
  - Bad: nothing useful, wasted time (+skip_time minor).
- **Quick grab** — no coins needed, auto-resolve. Small gold or 1 food guaranteed. No risk.

**Biome fit:** Plains (Grid ruins), Mountains (abandoned mines), universal.
**Skins:**
- Plains: "An intact supply cache in a Grid-era bunker."
- Mountains: "An abandoned prospector's camp. Some gear left behind."
- Forest: "A hollow tree with supplies cached inside — someone's stash."

---

#### O2: Sheltered Camp
**Tags:** `camp`
**Accepts:** Bushcraft
**Options:**
- **Make camp here** — Bushcraft coins. Base: 50/30/20.
  - Good: excellent rest. +Spirits (bonus), food consumption reduced (shelter + fire).
  - Neutral: decent rest. Normal end-of-day.
  - Bad: camp is exposed after all. Normal end-of-day but -spirits from poor sleep.
- **Note it and press on** — no action. Decline the opportunity.

**Biome fit:** Universal.
**Skins:**
- Mountains: "An overhang in the rock, out of the wind. Room for a fire."
- Forest: "A dry clearing with a firepit. Someone camped here before."
- Swamp: "Solid ground above the waterline. Dry reeds for bedding."

---

#### O3: Forageable Ground
**Tags:** `flora_fauna`
**Accepts:** Bushcraft, `flora_fauna` gear
**Options:**
- **Forage** — Bushcraft/flora_fauna coins. Base: 40/40/20.
  - Good: abundant harvest (2-3 food, possibly special food).
  - Neutral: modest harvest (1 food).
  - Bad: slim pickings, wasted time (+skip_time).
- **Pass by** — no action.

**Biome fit:** Forest (primary), Swamp, Mountains.
**Skins:**
- Forest: "A stand of nut trees, heavy with autumn mast."
- Swamp: "Edible tubers in the shallows, if you know what to look for."
- Mountains: "Wild herbs and berries on the south-facing slope."

---

#### O4: Friendly Stranger
**Tags:** `social`
**Accepts:** Negotiation
**Options:**
- **Trade** — Negotiation coins. Base: 50/30/20.
  - Good: favorable trade (items at discount, rare item available).
  - Neutral: standard trade (market prices).
  - Bad: bad deal or nothing you need.
- **Ask for information** — Negotiation coins. Base: 50/30/20.
  - Good: reveals a confound in the current encounter or upcoming terrain intel.
  - Neutral: vague but useful hint.
  - Bad: useless gossip.
- **Share a meal** — costs 1 food. +Spirits (small).

**Biome fit:** Universal.
**Skins:**
- Plains: "A grain merchant resting by the road, cart loaded."
- Mountains: "A charcoal burner tending their kiln."
- Scrub: "A clan trader traveling with a small goat herd."

---

### Category: Biome-Signature Blocks

Blocks that appear only in their home biome. These create the distinctive feel.

---

#### S1: Grid Machinery (Plains)
**Tags:** `radiation`, `ruin`
**Accepts:** Cunning, `ruin` gear
**Options:**
- **Investigate the mechanism** — Cunning/ruin coins. Base: 30/30/40.
  - Good: deactivate or bypass. Possible Grid Cipher clue, salvage value (gold).
  - Neutral: understand it well enough to avoid. No benefit, no harm.
  - Bad: trigger it. Irradiated (1 stack) or injured (1 stack).
- **Give it a wide berth** — auto-success. May cost time if it's blocking the route.

**Biome fit:** Plains T2-T3 only.
**Skins:**
- "A golem stands motionless at a crossroads, part numbers visible on its chassis."
- "Pipes run along the ground, still humming. A grid junction."

---

#### S2: Kesharat Checkpoint (Scrub)
**Tags:** `papers`, `social`, `lattice`
**Accepts:** Negotiation, Cunning, `papers` gear
**Options:**
- **Submit to processing** — Negotiation/papers coins. Base: 30/40/30.
  - Good: processed quickly. May learn useful Kesharat intel.
  - Neutral: delayed. +Skip_time.
  - Bad: detained. +Skip_time (long), -spirits, possible confiscation (lose gold).
- **Claim clan exemption** — Negotiation coins. Requires quality `faction.clans >= 1`. Base: 60/30/10.
  - Good: waved through with respect.
  - Neutral: grudging acceptance, mild suspicion.
  - Bad: they don't believe you. Falls to processing.
- **Slip past at shift change** — Cunning coins. Base: 30/40/30.
  - Good: undetected.
  - Neutral: seen but no pursuit.
  - Bad: caught. Confrontation (-spirits, -gold fine).

**Biome fit:** Scrub T2-T3 only.
**Skins:**
- "The rail checkpoint. An officer with a clipboard and two armed guards."
- "A Kesharat census station. Forms in triplicate."

---

#### S3: Hunter's Territory (Forest)
**Tags:** `trap`, `flora_fauna`
**Accepts:** Cunning, Bushcraft, `trap` gear
**Options:**
- **Read the signs** — Cunning/trap coins. Base: 30/40/30.
  - Good: identify the Hunter's boundary markers. Route around safely.
  - Neutral: partial reading. Know you're in territory but not the trap lines.
  - Bad: misread the signs. Walk deeper into the controlled area.
- **Bushcraft through** — Bushcraft coins. Base: 30/30/40.
  - Good: find a game trail that avoids the traps.
  - Neutral: slow going but safe (+skip_time).
  - Bad: trigger a trap. Poisoned (1 stack) or injured (1 stack).

**Biome fit:** Forest T2-T3 only.
**Skins:**
- "Reversed cairns. Blazes carved backward on the trees. This trail was designed to confuse."
- "A friction trap — nearly invisible cord connected to a bent sapling."

---

#### S4: Bureaucratic Maze (Mountains/Stift)
**Tags:** `papers`, `social`
**Accepts:** Negotiation, Cunning, `papers` gear
**Options:**
- **Navigate the procedure** — Negotiation/papers coins. Base: 30/40/30.
  - Good: find the right office, get what you need. Maybe useful contact.
  - Neutral: processed eventually. +Skip_time.
  - Bad: sent to the wrong office, forms rejected. +Skip_time (long), -spirits.
- **Find someone who knows the system** — Negotiation coins. Base: 40/30/30.
  - Good: local guide talks you through it. Fast resolution.
  - Neutral: helpful but confused. Marginal improvement.
  - Bad: they're a petitioner too. Both stuck.
- **Forge/falsify documents** — Cunning/papers coins. Base: 30/30/40.
  - Good: clean forgery, immediate passage.
  - Neutral: forgery passes inspection but arouses suspicion (-faction.scholars).
  - Bad: caught. Detained, -spirits, -gold (fine), -faction.scholars.

**Biome fit:** Mountains T2-T3 (Stift) only.
**Skins:**
- "The Wandelgang. Your petition has been assigned to a committee."
- "A clerk examines your travel authorization and finds three irregularities."

---

#### S5: Swamp Navigation (Swamp)
**Tags:** `terrain`, `water`
**Accepts:** Bushcraft
**Options:**
- **Read the water** — Bushcraft coins. Base: 30/40/30.
  - Good: find the channel. Direct passage.
  - Neutral: roundabout but passable route.
  - Bad: dead end. Must backtrack (+skip_time, -spirits).
- **Follow the markers** — Cunning coins. Base: 30/30/40.
  - Good: Revathi path markers still legible. Quick passage.
  - Neutral: markers half-submerged. Slow going.
  - Bad: markers have been moved or are wrong. Lost (-spirits, +skip_time).
- **Hire a guide** (only near settlements) — costs gold. Auto-success.

**Biome fit:** Swamp T2-T3 only.
**Skins:**
- "The channels split. Reed beds obscure every direction."
- "Revathi stakes mark a route — or did, before the water rose."

---

#### S6: Cosmic Disturbance (Mountains T3)
**Tags:** `darkness`
**Accepts:** Cunning, `darkness` gear
**Options:**
- **Investigate** — Cunning coins. Base: 20/30/50.
  - Good: understand the phenomenon. +Spirits (awe, wonder), useful knowledge.
  - Neutral: unsettling but no harm. -Spirits (minor).
  - Bad: deeply disturbing. -Spirits (large). Possible disheartened.
- **Avert your eyes and keep moving** — auto-success. -Spirits (small, can't unsee it).

**Biome fit:** Mountains T3 only.
**Skins:**
- "The stars are wrong. You recognize constellations, but they're in positions they won't hold for centuries."
- "Sound arrives before its source. You hear your own footsteps a moment too early."

---

#### S7: Dissolution Zone (Swamp T3)
**Tags:** `terrain`, `lattice`
**Accepts:** Bushcraft, Cunning, `lattice` gear
**Options:**
- **Push through** — Bushcraft coins. Base: 20/30/50.
  - Good: maintain coherence. Pass through intact.
  - Neutral: disorienting. -Spirits, +skip_time.
  - Bad: partial absorption. Lattice_sickness (1 stack), -spirits.
- **Anchor yourself** — requires specific item or high Cunning. Cunning/lattice coins. Base: 40/40/20.
  - Good: maintain full awareness. Possible insight.
  - Neutral: endure it. Minor discomfort.
  - Bad: anchor fails. Falls to push-through outcome.

**Biome fit:** Swamp T3 only.
**Skins:**
- "The edges of things soften. Your hand looks unfamiliar."
- "Sound becomes texture. Light has weight. The categories are dissolving."

---

### Block Summary Table

| # | Name | Category | Tags | Primary Currency | Biome |
|---|---|---|---|---|---|
| B1 | Locked Passage | Barrier | `lock` | Cunning | Universal |
| B2 | Vertical Obstacle | Barrier | `climb` | Bushcraft | Mountains+ |
| B3 | Gatekeeper | Barrier | `papers`, `social` | Negotiation | Mountains/Scrub/Plains |
| B4 | Dense Terrain | Barrier | `terrain` | Bushcraft | Forest/Swamp/Scrub |
| B5 | Flooded Ground | Barrier | `terrain`, `water` | Bushcraft | Swamp/Forest |
| B6 | Barricade | Barrier | `barrier` | Combat/Bushcraft | Universal |
| T1 | Hostile Wildlife | Threat | `combat`, `flora_fauna` | Combat | Universal (esp. Forest) |
| T2 | Ambush | Threat | `combat`, `trap` | Cunning/Combat | Forest/Scrub/Plains |
| T3 | Radiation Hazard | Threat | `radiation`, `ruin` | Bushcraft | Plains T2-T3 |
| T4 | Lattice Field | Threat | `lattice` | Cunning | Scrub T2-T3 |
| T5 | Trap Line | Threat | `trap`, `flora_fauna` | Cunning | Forest T2-T3 |
| T6 | Hostile NPC | Threat | `combat`, `social` | Combat/Negotiation | Universal |
| T7 | Exposure | Threat | `exposure` | Bushcraft | Mountains/Scrub |
| O1 | Cache/Salvage | Opportunity | `ruin`, `lock` | Cunning | Plains/Mountains |
| O2 | Sheltered Camp | Opportunity | `camp` | Bushcraft | Universal |
| O3 | Forageable Ground | Opportunity | `flora_fauna` | Bushcraft | Forest/Swamp/Mountains |
| O4 | Friendly Stranger | Opportunity | `social` | Negotiation | Universal |
| S1 | Grid Machinery | Signature | `radiation`, `ruin` | Cunning | Plains |
| S2 | Kesharat Checkpoint | Signature | `papers`, `social`, `lattice` | Negotiation/Cunning | Scrub |
| S3 | Hunter's Territory | Signature | `trap`, `flora_fauna` | Cunning/Bushcraft | Forest |
| S4 | Bureaucratic Maze | Signature | `papers`, `social` | Negotiation/Cunning | Mountains |
| S5 | Swamp Navigation | Signature | `terrain`, `water` | Bushcraft | Swamp |
| S6 | Cosmic Disturbance | Signature | `darkness` | Cunning | Mountains T3 |
| S7 | Dissolution Zone | Signature | `terrain`, `lattice` | Bushcraft/Cunning | Swamp T3 |

**Total: 24 blocks.** With 2-3 per encounter, that's 276-2024 unique mechanical combinations (before confounds).

---

## 4. Confound System

Confounds modify the math on blocks already in the encounter. They don't add options — they shift weights, add costs, or remove options.

### State Confounds (Free, Derived from Player State)

These fire automatically based on PlayerState. No authoring needed.

| Confound | Trigger | Effect |
|---|---|---|
| **Disheartened** | Spirits < 10 | All blocks: Bad weight +10. (Current disadvantage rule, applied to block system.) |
| **Low Food** | < 3 food in haversack | Blocks with time-cost options: time costs +1 (you're slower when hungry). "Wait" options become more expensive. |
| **Injured** | Has injured condition | Climb/terrain blocks: Bad weight +10. Physical activity is harder hurt. |
| **Exhausted** | Has exhausted condition | All blocks with time-cost options: time costs +1. Everything takes longer when you're tired. |
| **Overloaded** | Pack full (7/7) | Climb/terrain blocks: Bad weight +5. Flee/run options: Bad weight +10. |

### Situational Confounds (Rolled from Oracle)

Rolled at encounter generation. 0-3 per encounter based on tier.

| # | Confound | Effect | Biome |
|---|---|---|---|
| C1 | **Night** | All blocks: Cunning required to reveal options costs +1 coin. Darkness gear offsets this. Stealth options (Cunning) get +10 Good weight. | Universal |
| C2 | **Rain/Storm** | Climb blocks: Bad weight +15. Exposure block auto-included if not already present. Fire-based options unavailable. | Universal |
| C3 | **Fog** | Social/gatekeeper blocks: Cunning stealth options get +15 Good weight. Navigation blocks: Bad weight +10. Ambush detection costs +1 coin. | Universal |
| C4 | **Extreme Cold** | All blocks: time-cost options cost +1 (you can't linger). If no warm gear: -spirits per option attempted. | Mountains |
| C5 | **Dust Storm** | Navigation/terrain blocks: Bad weight +15. Exposure block auto-included. Visibility-dependent options unavailable. | Scrub/Plains |
| C6 | **Lattice Drift** | Any block in the encounter: Bad outcomes may include lattice_sickness (1 stack) as an additional consequence. Lattice gear negates this rider. | Scrub T3 |
| C7 | **Radiation Spike** | Any block in the encounter: Bad outcomes may include irradiated (1 stack) as additional consequence. Radiation gear negates. | Plains T3 |
| C8 | **Recent Activity** | Social blocks: NPCs are on edge. Negotiation costs +1 coin. Combat blocks: enemies are alert. Ambush detection costs +1 coin. | Universal |
| C9 | **Unstable Ground** | Terrain/climb blocks: Bad weight +10. Noise-sensitive options (stealth) cost +1 coin — the ground shifts and crunches. | Mountains/Swamp |
| C10 | **Time Pressure** | A background clock: if the encounter isn't resolved within 2 options (counting retreats), add exhausted condition. Makes "wait it out" strategies expensive. | Universal |
| C11 | **Companion Animal** | Positive confound. Flora_fauna blocks: Good weight +10 (the animal helps). Social blocks: +1 free Negotiation coin (people like animals). Stealth options cost +1 (the animal is noisy). | Universal |
| C12 | **Rival Party** | Another group is trying to get through the same obstacle. Race dynamic: "quick" options get +10 Good weight (you beat them to it). "Slow" options get -10 Good weight (they get there first and complicate things). | Universal |

---

## 5. Encounter Composition

### Block Count by Tier

| Tier | Blocks | Confounds | Total Complexity |
|---|---|---|---|
| T1 | 1-2 | 0 | Simple. Learning the system. |
| T2 | 2-3 | 0-1 | Standard puzzle. Core gameplay. |
| T3 | 2-3 | 1-3 | Hard. Multiple axes of pressure. |

### Pool Filtering

1. **Biome filter**: only blocks whose biome list includes the current biome (or "universal").
2. **Tier filter**: signature blocks (S-series) only appear at their minimum tier. Universal blocks appear at any tier.
3. **Category balance**: each encounter must have at least 1 barrier or threat block. Opportunity blocks are optional (rolled separately, ~30% chance per encounter).
4. **No duplicates**: can't pull the same block twice.

### Composition Algorithm

```
1. Roll encounter type from taxonomy (confrontation, entrapment, hazard, windfall, favorable_ground, stranger)
2. Pick 1 block matching the type as the core problem:
   - confrontation → T1, T2, T6
   - entrapment → B1, B2, B4, B5, S5
   - hazard → T3, T4, T5, T7, S1, S6, S7
   - windfall → O1, O3
   - favorable_ground → O2
   - stranger → O4, B3, S2, S4
3. Pick 0-2 additional blocks (weighted by tier):
   - T1: 0-1 additional
   - T2: 1 additional
   - T3: 1-2 additional
4. Filter by biome and tier
5. Roll 0-N confounds (N by tier)
6. Apply state confounds from player state
7. Concatenate fiction skins for all blocks into encounter text
```

### Exclusion Rules (Minimal)

- No two blocks from the same narrow tag (e.g., can't pull two `radiation` blocks — they'd be redundant).
- If a positive block (O-series) is present, at least one negative block must also be present (an encounter with only opportunities is boring).
- Signature blocks (S-series) are limited to 1 per encounter (they're the flavor anchor, not the whole meal).

---

## 6. Coverage Analysis

### Skill Coverage

| Skill | Primary blocks | Secondary blocks | Total |
|---|---|---|---|
| **Combat** | T1, T2, T6, B6 | B1 (force option) | 5 |
| **Cunning** | B1, T2, T4, T5, S1, S6 | T3 (read markers), S2, S4, S7, O1 | 11 |
| **Bushcraft** | B2, B4, B5, T3, T7, S5, O2, O3 | B6, T1 (disengage), T6 (flee), S3, S7 | 13 |
| **Negotiation** | B3, S2, S4, O4 | T6 (negotiate), S5 (hire guide) | 6 |

**Assessment:** Bushcraft is the most versatile skill (expected — it's the journey skill). Cunning is second (the "see more options" skill). Combat and Negotiation are narrower but powerful in their domain. This matches the design intent: Bushcraft is the generalist's friend for overland travel, but specialists in Combat or Negotiation dominate when their blocks come up.

**Concern:** Negotiation has only 6 blocks. This is acceptable because social blocks tend to have higher-value outcomes (information, contacts, faction standing) and because the Stranger encounter type is common. But worth monitoring — if Negotiation feels weak in play, add 1-2 more social blocks.

### Gear Coverage

| Gear Tag | Blocks where it applies |
|---|---|
| `lock` | B1, O1 |
| `climb` | B2 |
| `trap` | T2, T5, S3 |
| `radiation` | T3, S1 (plus confound C7) |
| `lattice` | T4, S7 (plus confound C6) |
| `papers` | B3, S2, S4 |
| `flora_fauna` | T1, T5, S3, O3 |
| `darkness` | S6 (plus confound C1) |
| `ruin` | T3, S1, O1 |
| `exposure` | T7 |

**Assessment:** Most gear tags apply to 2-4 blocks, which is the sweet spot. `climb` and `exposure` are narrow (1 block each) — they're cheap situational tools, not build-defining. `trap` and `papers` are the most valuable journey tools (3 blocks each, all in their home biome).

**Concern:** `lock` only hits 2 blocks. Consider adding a "secured container" variant to more blocks, or combining lockpicks with another function to justify the pack slot.

### Biome Coverage

| Biome | Signature blocks | Universal blocks applicable | Total unique blocks |
|---|---|---|---|
| Plains | T3, S1 | B1, B2, B3, B4, B6, T1, T2, T6, T7, O1-O4 | 15 |
| Scrub | T4, S2 | B1, B3, B4, B6, T1, T2, T6, T7, O1-O4 | 14 |
| Forest | T5, S3 | B1, B2, B4, B5, B6, T1, T2, T6, O1-O4 | 14 |
| Mountains | S4, S6 | B1, B2, B3, B6, T1, T6, T7, O1-O4 | 13 |
| Swamp | S5, S7 | B1, B4, B5, B6, T1, T6, O1-O4 | 12 |

**Assessment:** Good spread. No biome has fewer than 12 available blocks. Plains has the most variety (the Grid ruins create both threat and opportunity blocks). Swamp has the fewest — expected, since it's the least developed biome. If swamp feels thin, add a "Bog Hazard" or "Submerged Ruin" block.

### Combinatorial Space

With 24 blocks and 2-3 per encounter:
- **2-block encounters:** C(24, 2) = 276 combinations (before biome filtering)
- **3-block encounters:** C(24, 3) = 2024 combinations

After biome filtering (roughly 12-15 blocks available per biome):
- **2-block:** ~78-105 per biome
- **3-block:** ~220-455 per biome

With 12 situational confounds at 0-3 per encounter, effective variety multiplies further. A player would need to see 200+ journey encounters before mechanical repetition becomes obvious — and the fiction skins provide additional variety on top of the mechanical layer.

### Open Questions

1. **Coin economy tuning**: How many coins does it take to meaningfully shift outcomes? If a 4-skill character plus 2-coin gear = 6 coins, and each coin shifts 10%, that's a 60% swing. Might be too generous. Consider 5% per coin, or diminishing returns.
2. **Retreat option**: Every encounter should have a "turn back" option that costs time but guarantees safety. This prevents softlocks. Not every block needs a retreat — the encounter as a whole offers it.
3. **Multi-block interaction**: Blocks are self-contained, but the player's coin budget is shared across the encounter. Spending 3 Combat coins on the Hostile NPC block means fewer coins for the Barricade block. This is the core strategic tension and needs no special rules — it emerges naturally from shared budget.
4. **Luck integration**: Luck doesn't provide coins. Instead, it triggers on bad outcomes as a reroll chance (current system). This makes Luck the "I didn't prepare for this" insurance — valuable for new players who don't know what to pack.
5. **Information asymmetry**: On first encounter with a block type, some options and most outcomes are hidden. After experiencing them, they're revealed permanently (per player, not per character). This is the player-mastery loop.
