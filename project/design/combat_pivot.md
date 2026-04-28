# Combat Pivot: Tac Encounters → OGL-Style Combat

Status: thinking doc, not a commitment. No code changes yet.

## Context

We invested heavily in tactical encounters:

- `.tac` format (authoring spec, parser, bundle pipeline)
- `lib/Tactical/` library (TacticalEncounter, TacticalParser, TacticalBundle)
- `text/encounter-tool/` commands: `check-tactical`, `bundle-tactical`,
  `fixme-tactical`, `generate-tactical`, `walk-tac`
- `tools/tactical-sim/` (GA testing, vibe sim, cancel sim, powerhouse sim)
- ~14 authored `.tac` files across plains tiers and one arc
- Related design docs under `project/design/encounter-redesign/` and the
  tactical UI/chaining/gear-gap project memories

The tac system was built around resource-management puzzles: per-timer counters,
progress framing, cancels, aggro, archetypes. It technically works. But it isn't
fun enough to justify the authoring cost and the screen real estate it consumes.
The decision space is dense but emotionally flat; players solve the puzzle rather
than fighting.

## Proposed Replacement

Road encounters pivot to OGL-style (d20 attack rolls, AC, HP pools, damage dice)
combat. The genre is well-understood by players, requires no onboarding, and
delivers a tight hit/miss/damage feedback loop per round. Authoring cost drops
dramatically: a combat encounter is mostly a monster stat block plus art plus
intro/win/lose prose, rather than a hand-tuned graph.

The tentative shape:

- Road encounters can resolve as combat encounters.
- Single enemy per encounter (one art piece, one name, one stat block). Each
  monster is a one-and-done "oh no it's Girzor, look out" set piece: defeat
  them once, never see them again. Authoring is a lead-in plus two exeunt
  paragraphs (win/lose) plus stats.
- Semantics carry over from `.tac`: `+repool` support, intro text, win text,
  lose text, win/loss verbs (reward/penalty mechanics).
- Side-based initiative: monsters as a side, player as a side. Alternate. No
  per-actor initiative roll.
- Surprise is a starting Bushcraft check. Pass gets you the first round; fail
  puts the monster side first.
- **Enemy intent preview (foundational).** Each turn the monster telegraphs its
  next action (Attack / Big Attack / Defend / Power Up / etc.), Slay-the-Spire
  style. This is load-bearing — sword stances, dagger exploits, and axe Block
  timing all depend on the player being able to read what's coming. Without
  it, every per-turn decision is blind.
- Player can flee, use consumables, and pick weapon moves (see below).

### Single-check combat in `.enc` stays

Combat encounters are not a replacement for single-check combat in `.enc`
files. Single-check combat is the right tool for cases where a whole run of
extended fiction hinges on one roll (Brides Cave, where you think bloody
thoughts and the ghosts recoil, is the canonical example; nothing is being
"attacked" in any mechanical sense). Those stay as they are.

The new combat screen fires only for explicit combat encounters (the new
format). `.enc` authors pick whichever fits the fiction.

## Mechanical Changes

### Player HP: 20 Spirits + 4 Health (locked)

Player HP stays at the **fixed values 20 Spirits + 4 Health**. The 20-Spirits
buffer absorbs the bulk of combat damage; once depleted, a 4-Health pool means
the player is 1–2 hits from death. Tight late-fight tension by design.

May revisit if sim shows the curve is wrong, but the shape (large Spirits
buffer, small Health meat) is locked.

**Damage flow: Spirits first, then Health.** This borrows from the old D&D
house-rule tradition where starting HP is "actual HP" and level HP is
"competence HP," which is why a sword wound was a mild inconvenience to a
name-level fighter. We map that idea onto our existing stat split:

- Incoming damage ablates **Spirits** down to zero first.
- Overflow ablates **Health**.
- Spirits represents morale, focus, stamina: the rolls, dodges, and grit that
  let you take a blow without really taking a blow. Health is meat.
- Low spirits going into a fight means a smaller buffer — effectively fewer
  HP in combat. That is the entire low-spirits penalty. No roll penalty, no
  secondary debuff. Just a thinner pad between the blade and the body.
- Inns and chapterhouses restore Spirits cheaply, as today. No special
  combat-era treatment. The daily-cadence recovery curve doesn't need to
  change.
- Conditions that drain Spirits (the existing condition machinery) get
  meaningfully scarier under this model: a condition eating 3 Spirits is now
  three fewer points of combat absorption until you rest. That's the bonus
  teeth conditions get for free once combat exists.

Knock-on effects:

- `+health` / `-health` mechanic magnitudes in existing `.enc` files probably
  hold up at 4 Health, but worth a sweep.
- End-of-day and inn/chapterhouse Health-recovery values stay roughly as-is
  (small Health pool means each point of recovery matters more).
- **Injury condition needs new semantics.** It currently stacks 1/2/3 and
  affects Health recovery — a soft penalty model that assumed combat was
  resolved by skill check. With combat now potentially fatal and Health
  pool small, the existing semantics may not be right. Open design question;
  see "Needs Follow-Up" below.
- **Death handling needs an audit.** Today the player can only die at
  end-of-day cleanup. Combat now opens new death paths (Health to 0 mid-fight,
  bleed-after-combat tick, etc.). The combat runner, save state, UI, and any
  resolution surfaces need to handle "died in combat" cleanly. See "Needs
  Follow-Up."

### Armor pivots to AC, keeps utility bonuses

Primary combat role of armor becomes Armor Class. Everyone has an AC, player
and monsters alike. **AC bands start from D&D 5e values** and we tune from
there if sim shows problems:

- Light armor: 11–12 base, with Cunning save vs incoming hits as the active
  defense (see `weapon_classes.md`).
- Medium: 13–15.
- Heavy: 16–18.

Non-combat bonuses stay:

- Injury resistance (still the out-of-combat protection stat)
- Cunning bonus (Light armor's identity, also feeds the Cunning save)
- Freezing resistance (mountains-only ambient)

This preserves the armor class flavor from `project/design/armor_classes.md`
while giving AC a real home.

**Light armor's liability is solved by the Cunning save.** Wearing silks on
the road would be a death sentence with raw AC alone, but the active dodge
mechanic (Cunning save vs DC = the attack roll on hits) gives Light a viable
defensive identity. High-Cunning characters in silks survive by rolling well;
low-Cunning characters in silks shouldn't be in silks.

### Weapons grant moves, not just flat bonuses

Today every weapon is a flat `+N` to the Combat skill. That's fine for a single
skill check but boring inside a turn-by-turn fight. Pivot: weapon class
determines the tactical options available during combat.

Working design at `project/design/weapon_classes.md`. Sword (stances) and axe
(momentum) are converging; dagger is still open. Tier scaling lands on damage
die size per class.

### Damage dice on weapons

Weapons gain a damage die (1d4, 1d6, 1d8, etc.) plus optional flat modifier.
To-hit resolution is d20 + mod vs AC (same engine as existing skill checks,
just reusing the d20 roll surface).

### Crits and fumbles

Yes, both. Nat-20 crits and nat-1 fumbles. The fight loop of "you attack, they
attack, you attack, they attack" needs occasional texture, and the 5% swing is
the cheapest way to get it. Exact effects TBD (standard double-damage crit
probably; fumble as miss + minor narrative consequence rather than anything
punishing).

### Flee: Cunning check

A flee action is always available. Resolve with a Cunning check against a DC
set by the encounter (easier in open terrain, harder if Cornered-equivalent).
Failure costs the turn and probably takes a free hit from the monster side.
Success ends the encounter with a loss-adjacent but softer consequence (the
loss verbs may need a "fled" variant, or we just reuse loss verbs).

### New combat encounter type

Parallel to `.enc` and `.tac`. Tentative name `.cmb`; file format (custom
token format vs YAML vs hybrid) is TBD — leaning toward token-format prose
sections plus a structured stat block.

Category routing mirrors `.enc`: `{biome}/tier{n}/` folders, placed by
`EncounterPlacer` into the existing encounter slots on the map.

#### Required fields

- **Title** (= monster name; one per encounter, e.g. "Gorzog the Cleaver")
- **Monster stats:** HP, AC, to-hit bonus, basic-attack damage die + mod
- **Monster image:** path to portrait/body sprite
- **Hitboxes:** rectangular sub-regions of the monster image where damage,
  deflect, and miss sprites are allowed to render. Multiple per monster
  (head/torso/limbs) so impact placement varies.
- **Monster moves:** ordered list, see schema below
- **Intro text:** prose shown when combat starts (the lead-in)
- **Win text:** prose shown on player victory
- **Lose text:** prose shown on player defeat
- **Win verbs:** mechanics applied on victory (rewards, tag, gold, etc.)
- **Loss verbs:** mechanics applied on defeat (penalty, condition, etc.)
- **+repool flag:** whether the encounter can recur in repools (default off
  for one-and-done set-piece monsters)

#### Hitboxes and render scaling

Hitboxes are defined as **percentile interior margins** of the monster
image, not absolute pixel coords. This way the monster sprite scales (mobile,
desktop, different resolutions) and the hitboxes scale with it.

```
hitboxes:
  - id: head
    region: { left: 0.4, top: 0.05, right: 0.6, bottom: 0.25 }
  - id: torso
    region: { left: 0.3, top: 0.3, right: 0.7, bottom: 0.7 }
  - id: legs
    region: { left: 0.35, top: 0.7, right: 0.65, bottom: 0.95 }
```

Damage/deflect/miss sprites pick a hitbox (random, or biased per attack
class — e.g. heavy attacks bias toward torso) and render centered inside it.

#### Monster move schema

Each move has both **mechanical content** and **presentation content**.

```
moves:
  - id: cleaver_swing
    intent_class: attack         # drives intent-preview icon/colour
    intent_text: "Gorzog raises his cleaver."
    timer: 0                     # 0 = basic move (always available)
    sprite: cleaver_swing        # visual effect on resolution
    sprite_anchor: center        # center | top | bottom | <hitbox_id>
    narration: "Gorzog hacks down with the cleaver."
    mechanics:
      - deal_damage 1d8

  - id: crushing_blow
    intent_class: heavy_attack
    intent_text: "Gorzog winds up for an overhead strike."
    timer: 3                     # cooldown in turns
    sprite: cleaver_overhead
    sprite_anchor: top
    narration: "Gorzog brings the cleaver down with both hands."
    mechanics:
      - deal_damage 2d8+2

  - id: gore
    intent_class: pierce
    intent_text: "Gorzog lowers his horns."
    timer: 5
    sprite: gore_thrust
    sprite_anchor: bottom
    narration: "Gorzog charges, horns first."
    mechanics:
      - pierce 1d8 dc 14                # Cunning save vs DC 14, full damage on fail
      - inflict_condition bleeding dc 14 25%
```

**Move sprite anchor** controls where the move's visual effect renders. The
anchor can be a fixed point (center/top/bottom of the monster image) or a
specific hitbox id. This keeps spell-effects, swings, and gore animations
positioned sensibly regardless of monster proportions.

**Intent class** drives the Slay-the-Spire-style preview shown to the player
the turn before the move fires. Initial classes:

- `attack` — light damage, default attack
- `heavy_attack` — big damage incoming
- `defend` — AC bump this turn
- `pierce` — armor-piercing, Cunning save to evade
- `condition` — about to inflict a status
- `flee` — about to disengage

The class determines the icon and colour treatment; the `intent_text` field
provides per-monster flavor on top.

#### Mechanics DSL

Monster moves are bespoke per monster but built from a shared vocabulary,
similar to the existing `.enc` mechanics language. Initial primitives:

- `deal_damage <dice>` — roll damage, apply per Spirits-then-Health flow
- `pierce <dice> dc <n>` — Cunning save vs DC; failure takes full damage
  (armor doesn't apply)
- `inflict_condition <id> dc <n> [<chance>]` — apply status, gated by the
  existing condition-resist save (DC required since all conditions have a
  resist mechanic). Optional proc chance for moves where the condition only
  sometimes triggers.
- `defend +<n> ac` — raise monster AC by N this turn
- `bleed <chance> <dice> <duration>` — convenience for a bleed status apply
- `flee` — monster ends combat by escaping; encounter repools if eligible

Multiple primitives can compose in a single move's `mechanics` list (e.g.
deal damage + inflict condition on the same swing).

#### Authoring

Each monster is hand-authored — one named set piece per `.cmb` file ("oh no
it's Girzor"), no stat-block library, no procedural generation. We have ~18
monster sprites ready.

**No LLM tooling.** No `generate-combat` / `fixme-combat` commands. The
authoring surface is small enough and the content bespoke enough that
hand-authoring is faster than prompt engineering. Revisit only if this
changes.

### New combat screen

Fresh UI. Shows:

- Monster portrait + name + HP bar
- Player HP, AC, current weapon
- Turn log (you swing, hit/miss, damage roll; monster swings, etc.)
- Actions: Attack, [Weapon moves], Use item, Flee
- Enemy intent preview (next-move telegraph, Slay-the-Spire style)

Styling per `project/screens/styles.md`. Action buttons follow the existing
`bg-action` clickable-element rule.

## Monsters

### Stat starting points

Monster AC, HP, to-hit, and damage **start from D&D 5e values** and we tune
from there. A CR-1 monster's stat block is a reasonable shape for a tier-1
encounter; CR-3 for tier-3, etc. We're not committed to D&D's CR math, just
using it as a sane baseline rather than inventing numbers from scratch.

### Turn structure: basic move + semi-hidden timers

Most turns, the monster takes its **basic move** (the workhorse default attack
for that creature). Layered on top, the monster has one or more **timers** for
special moves — deterministic countdowns, not RNG.

- Timers tick down silently. The player doesn't see the count most of the
  time.
- The turn before a timer fires, the **intent preview shows the special
  move** (one-turn lookahead, same window as the basic-move telegraph).
- After the special fires, its timer resets and starts ticking again.

Determinism is the point. Same monster, same fight, same timer sequence.
Players who replay learn the rhythm. Players who use a guide can play it
optimally. Players who don't pay attention eat the big move.

Reused monster types (visual reskins of the same base creature) **share
movesets and timer values**. Visual similarity is a mechanical cue. If two
encounters use the same monster sprite at different tiers, the tactics
carry over.

### Monster move catalog

Initial vocabulary (will grow). Each monster picks a basic move plus 0–2
special moves with timers.

- **Strike** — light damage, the default attack. Resolves vs AC.
- **Defend** — no damage; AC bonus this turn (analogous to player Block).
- **Heavy Strike** — big damage attack on a timer. Resolves vs AC.
- **Pierce** — medium damage that bypasses armor. Player rolls a Cunning
  save to evade; failure takes full damage.
- **Flee** — ends combat. The encounter repools; the monster is not dead,
  it just got away.
- **Inflict Condition** — applies a status (Bleed, etc.). Specific condition
  per monster.

The list is intentionally small. Boss-tier monsters may eventually pick up
unique moves, but the base catalog should cover most encounters.

### Design rule: no player agency negation

Monster moves **must not destroy player agency or negate player prep**. No
charm (taking control), no paralyze (skipping turns), no insta-kills (bypassing
defense and HP entirely), no disarm-style "your weapon is useless this turn."

The fantasy is: you came prepared, you read the telegraphs, you made
decisions, and you either won or you didn't. Mechanics that take the
controller out of the player's hands or invalidate the build they brought to
the fight feel cheap and break the contract.

This rule extends to conditions inflicted by Inflict Condition moves and to
any future encounter mechanics. Conditions that *bias* play (lower to-hit,
reduce damage, accelerate Spirits decay) are fine; conditions that *remove*
play (skip turn, lose action, force a specific choice) are not.

## Cleanup (deferred)

When we commit to this, the tac scaffolding comes out. Estimated surface:

- Delete `lib/Tactical/` (TacticalEncounter, TacticalParser, TacticalBundle)
- Delete `text/encounter-tool/EncounterCli/TacticalCheckCommand.cs`,
  `TacticalBundleCommand.cs`, `FixmeTacticalCommand.cs`,
  `GenerateTacticalCommand.cs`, `WalkTacticalCommand.cs`
- Remove `check-tactical`, `bundle-tactical`, `fixme-tactical`,
  `generate-tactical`, `walk-tac` from `Program.cs`
- Delete `tools/tactical-sim/` (GA_Testing.md, vibe_sim.py, cancel_sim.py,
  powerhouse_sim.py)
- Delete existing `.tac` files under `text/encounters/` (14 authored files
  across plains and arcs)
- Delete `tactical.bundle.json` loading paths in `server/GameServer/GameData.cs`
  and the tactical-bundle-first branches in `GameFunctions.cs`
- Archive `project/design/encounter-redesign/` (old tac thinking stays as
  reference, doesn't influence current work)
- Archive `memory/project_tactical_*.md` entries (or rewrite as "superseded by
  combat pivot")

Tests: `tests/Tactical.Tests/` if it's populated (currently empty dir).

## Decisions Locked In

- Single-check combat in `.enc` stays. New combat screen is only for
  explicit combat encounters.
- **Player HP: 20 Spirits + 4 Health, fixed.** May revisit if sim shows the
  curve is wrong, but the shape is locked.
- **AC bands start from D&D 5e** (Light 11–12, Medium 13–15, Heavy 16–18).
  Light armor's defense is a Cunning save vs incoming hits, not raw AC.
- **Monster stats start from D&D 5e** CR ladder. Tune from there if sim
  shows problems.
- Damage ablates Spirits first, then Health.
- Side-based initiative. Surprise is a Bushcraft check; win the check, go
  first.
- **Enemy intent preview is foundational** — every weapon class's per-turn
  decision depends on it.
- Flee action: Cunning check. Failure costs the turn and a free hit.
- Crits and fumbles: yes, standard nat-20 / nat-1.
- No LLM tooling for combat encounters. Hand-author.
- Ability scores: do not introduce STR/DEX/CON. Combat for to-hit, Bushcraft
  for surprise, Cunning for damage saves and flee.
- **Cunning is the universal damage-save stat** (see
  `memory/project_cunning_damage_saves.md`). Light armor's dodge is the
  primary application, but the pattern extends to traps, area effects, etc.
- Monster generation: hand-authored per encounter, one sprite = one named
  monster = one `.cmb` file.
- **Weapon classes locked.** Sword stances, axe momentum, dagger Edge — each
  with a distinct per-turn rhythm. See `weapon_classes.md`.
- **Monster turn design locked.** Basic move + semi-hidden deterministic
  timers; one-turn lookahead on specials. Reused sprites share movesets.
- **No player agency negation.** Monster moves and conditions can bias play
  but cannot remove it. No charm, no paralyze, no insta-kills, no
  disarm-style action lockouts.

## Still Open

- HP / damage / bleed numbers tuning pass — needs simulation once the combat
  loop exists.
- Crit / fumble effects beyond the basic "nat-20 double damage, nat-1 miss."
- How chained `.tac` → `.enc` flows
  (`memory/project_tactical_enc_chaining.md`) map onto `.cmb` → `.enc`.
  `+open` should carry over unchanged.
- Fled-encounter win/loss verb semantics. Reuse loss verbs, add fled variant,
  or let the author decide per-encounter?

## Needs Follow-Up

Engineering and design tasks queued by the pivot decisions:

- **Injured condition redesign.** Current semantics (1/2/3 stacks, soft
  Health-recovery penalty) assumed combat resolved by a single skill check.
  With combat now potentially fatal and Health pool small, the meaning needs
  reworking. Maybe Injured stacks make bleeds tick harder? Maybe Injured
  caps Spirits? Maybe Injured is vestigial once combat damages Health
  directly? Open design question, then code.
- **Death handling audit.** Today the player can only die at end-of-day
  cleanup. Combat needs to handle Health → 0 mid-fight, bleed-after-combat
  ticks (if applicable), and the resolution paths through `GameSession`,
  combat runner, save state, and UI. Audit and fix all the surfaces before
  shipping combat.
- **Bleed tuning.** Player's 25% per-hit, 1d4 × 2-turn bleed proposal in
  `weapon_classes.md` is a starting point. Monster bleed (if Inflict
  Condition uses it) needs separate tuning — 1d4 × 2 against a 4-Health
  pool can chain-kill the player.

## Encounter Tuning Baseline

Reverse-engineered via combat sim. Targets:

- Combat lasts 5–10 rounds.
- Even-match PC (Combat +2, tier-N gear, plays stance to intent): fatality < 10%.
- Overmatched PC (Combat 0, tier-1 gear, no stance read): fatality 40–70% at T1/T2.

### Default monster per tier

| Tier | HP | AC | Atk | Basic | Heavy (3-round timer) |
|------|----|----|-----|-------|-----------------------|
| T1   | 24 | 11 | +4  | 1d4   | 2d8                   |
| T2   | 30 | 11 | +5  | 1d4   | 2d8                   |
| T3   | 40 | 12 | +5  | 1d4   | 2d10                  |

These are the **baseline shape**, not the only shape. Authors can deviate
(more HP, different damage spread, different timer cadence) but should
return to the same fight-length and fatality envelope.

### Why the shape

- **HP** grows ~6–10 per tier. Long enough to feel substantial, short
  enough that a smart PC kills the monster before cumulative damage
  clears their buffer.
- **Monster AC stays 11–12 across tiers.** PC hit rate sits at 70–75%.
  Missing more than 1 in 4 attacks feels tedious; this keeps it on the
  right side of that line.
- **Monster attack bonus** rises slowly (+4 → +5 → +5). The threat
  scales via damage, not via grinding the player's AC down.
- **Basic stays 1d4.** Cumulative basic-attack damage across rounds is
  what kills high-tier PCs; keeping the basic small protects the
  24-HP buffer.
- **Heavy scales 2d8 → 2d8 → 2d10** on a 3-round timer. The heavy is
  the "intent preview matters" moment — the smart PC reads the
  telegraph, switches Defensive, eats less. The naive PC stays Balanced
  and gets hammered.

### Assumed PC profile

Spirits-then-Health buffer is locked at 24 (20sp + 4hp). The abstract
attack/damage values:

| Slot           | Even-match (T-N)  | Overmatched (any) |
|----------------|-------------------|-------------------|
| Attack bonus   | +(2 + T)          | +1                |
| Damage bonus   | +T (gear only)    | +1                |
| Damage die     | 1d8               | 1d8               |
| Bushcraft      | +2                | +0                |
| Cunning        | +2                | +0                |
| Stance         | reads intent      | always Balanced   |

### Armor scales with tier

T3 fatality only lands below 10% with a high-AC frontline build. The
expected progression:

- **T1**: light or medium, base AC 12–13.
- **T2**: medium, base AC 13–14.
- **T3**: heavy plate, base AC 18. Chainmail (AC 16) is borderline
  (~13–17% fatality), half-plate (AC 15) is too light (~15–25%).

This matches D&D's full-plate end of the curve and makes "the gear
ladder is real" a load-bearing part of the tier system.

### Tourist exception at T3

A literal "+0 Combat, tier-1 gear, AC 11" character at T3 dies ~91%,
far above the 40–70% overmatched target. **This is a feature.** An
unprepared traveller in deep wilderness has no realistic chance
against a tier-3 monster. The 40–70% overmatched target applies at
T1/T2 only; at T3 the floor is "if you weren't ready, you don't come
home."

### Cunning-build is worse in straight combat (by design)

A Cunning-built (light-armor, low-Combat) character is **objectively
worse than the plate fighter in a straight-up fight**, and the
encounter tuning does not try to give them parity. Plate fighters get
to shine when steel meets steel. The Cunning-build is paying for
something else.

What they're paying for:

- Out-of-combat options the brute doesn't have: sneaking past
  encounters, lying out of social ones, lockpicks, traps, sleights of
  hand, anything with "Cunning" on the check.
- Broader save coverage — Cunning saves against pierce, traps, area
  effects, fall damage, etc. Plate has injury resistance and that's
  it.
- Light armor's hit-negate via Cunning save (DC = attack roll) does
  reduce incoming damage some, but it's not enough to close the
  AC-12-vs-AC-18 gap. Light-armor PCs at T3 should not be standing
  toe-to-toe with the monster — that's a category error in their
  build.

Implication for the encounter ladder: the Cunning-build's answer to
T3 combat is "don't fight it like a fighter." Surprise the encounter,
flee, set up advantage, dodge the dangerous move, then close. The
encounter tuning doesn't bend to make them safe; the build pays a
real cost in straight combat and earns elsewhere.

Sim work that produced these numbers lived in the conversation; there
is no committed simulator yet. If we want to keep encounter-tuning
honest as we add monster moves and conditions, that simulator should
land in `tools/combat-prototype/` alongside the runtime.

## References

- Weapon class design (working): `project/design/weapon_classes.md`
- Prior combat thinking: `project/design/combat.md` (posture-node system, not
  adopted, kept for reference)
- Skill check foundation: `project/design/dice_mechanics.md`
- Armor classes: `project/design/armor_classes.md`
- Encounter placement: `mapgen/` content pipeline, `EncounterPlacer.cs`
- Tac-era docs: `project/design/encounter-redesign/` and the
  `project_tactical_*` memory entries
