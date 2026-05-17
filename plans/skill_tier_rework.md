---
kind: plan
title: Skill system rework — collapse to untrained/trained/expert tiers, retire d20, adopt RPS approach picker for encounter checks
state: shipped
created: 2026-05-15
updated: 2026-05-17
related:
  - inventory_consolidation.md
  - inventory_slot_refactor.md
  - arc_leveling.md
  - picker_check_chain_semantics.md
touches:
  files:
    - lib/Rules/ItemDef.cs
    - lib/Rules/CharacterBalance.cs
    - lib/Game/SkillChecks.cs
    - lib/Game/Mechanics.cs
    - lib/Game/Conditions.cs
    - lib/Game/EndOfDay.cs
    - lib/Game/Market.cs
    - lib/Encounter/
    - lib/Orchestration/EncounterRunner.cs
    - lib/Orchestration/SettlementRunner.cs
    - server/GameServer/GameResponse.cs
    - text/encounter-tool/EncounterCli/
    - text/encounters/  (content pass)
    - project/encounter-spec/format.md
    - project/encounter-spec/mechanics_reference.md
  features: [skills, encounters, balance, food, market]
---

# Skill System Rework

## Design Intent

The RPS combat pivot replaced d20 combat resolution with an interactive prediction game.
This plan finishes the job: d20 is retired across the board. Encounter `check` mechanics adopt
a **rock-paper-scissors approach picker** that mirrors the combat pivot's "predict and commit"
feel. Resist rolls and foraging rolls collapse into immunity / passive unlocks. Skill levels
become **tiers** (untrained / trained / expert), not integers.

This means:
- Encounter checks become a 3-way approach picker resolved by the player's tier in the skill.
  Difficulty lives in the encounter's *prose telegraphing*, not in a DC number.
- Gear bonuses to skill checks become irrelevant. Gear immunities and combat move sets are
  the gear system going forward (see `inventory_consolidation.md`).
- The skill number on `PlayerState.Skills` becomes a tier enum, not an integer.
- `SkillChecks.Roll()` is removed entirely. No d20 path remains in the engine.

Randomness stays where it's most interactive: in RPS combat, in encounter selection (which
encounter fires from the pool), and in the Untrained-correct coinflip (small surface, narrative
flavor).

---

## RPS Approach System

Each `check` in the encounter corpus has a **skill domain**, a **correct approach**, and a
**wrong approach**. The third approach in the domain is implicitly **neutral**. The player
picks one of the three; resolution is deterministic given the player's tier.

### Approaches per skill

| Skill        | Approach 1            | Approach 2                   | Approach 3                |
|--------------|-----------------------|------------------------------|---------------------------|
| Negotiation  | **Charm** (flatter, praise, or demur) | **Reason** (facts, argument) | **Threaten** (highlight risk) |
| Cunning      | **Hide** (conceal)    | **Bluff** (pretend upper hand) | **Scheme** (trick, outwit) |
| Bushcraft    | **Push** (grit through) | **Plan** (analyze first)   | **Reroute** (go around)   |
| Combat       | **Rush** (head-on)    | **Strategize** (wait, observe) | **Outlast** (wear down)   |

Combat approaches apply to **narrative `check combat`** in .enc files — they do not replace or
duplicate the RPS card system in `.fight` files. A .enc combat check is a single-beat narrative
clash ("the bar fight is brewing — how do you handle it?"). A .fight encounter is the full
multi-turn card game.

### Resolution table

| Tier        | Pick = correct  | Pick = neutral  | Pick = wrong |
|-------------|-----------------|-----------------|--------------|
| **Expert**  | succeed         | succeed         | fail         |
| **Trained** | succeed         | fail            | fail         |
| **Untrained** | coinflip      | fail            | fail         |

The Untrained-on-correct coinflip is base 50%, nudged by Luck (see Open Decisions for whether
Luck survives as a scalar for exactly this purpose).

### Resolution UX — Untrained streamlining (2026-05-17)

The table above describes the *mechanical* outcome distribution. The *experience* of
resolution differs by tier:

- **Expert / Trained**: the player picks one of three approaches. Their pick determines
  outcome per the table.
- **Untrained**: the engine pre-rolls the coinflip at the moment the player commits to
  the top-level choice (before the picker UI renders).
  - **Pre-roll fails** → skip the picker entirely; emit the connector + fail body
    directly. The pick was never going to matter.
  - **Pre-roll passes** → render the picker; the player gets the Trained-tier
    experience (best-of-three; correct succeeds, neutral/wrong fail). Their pick is
    consequential.

This keeps the success rate identical (50% on correct from Untrained), but it spares
the Untrained player from the "I picked the right thing and got told it didn't matter"
moment. It also means a successful Untrained outcome is always paired with an actually-
good pick — the lucky-but-bad-pick branch never plays.

**Engine implication**: pre-roll happens in the runner when the picker `check` branch is
entered, *before* emitting the picker payload to the client. The picker payload only
fires on a passing pre-roll. On a failing pre-roll, the engine jumps straight to the
fail body + `Direct` connector.

**Authoring implication**: none. Authors still write success/fail bodies as if the pick
mattered; the streamlining is invisible at the content layer.

### Difficulty by prose, not by number

The "difficulty" of a check is purely an authoring property of the surrounding prose — how
clearly it telegraphs the correct approach. No DC number lives on the check.

- **Tier 1 encounters** — the cue is obvious from a surface read.
- **Tier 2 encounters** — close read, or memory of something seen *earlier in the same arc*
  (within ~5 minutes of play). No journal needed; the prior scene is recent.
- **Tier 3 encounters** — requires world-knowledge plus scene evidence. Lore drops feeding
  tier-3 checks must be reachable earlier in the same run (NPC dialogue, prior encounters).

### Connector framing (engine-emitted)

When the player's pick and the realized outcome diverge, the engine prepends a one-line
connector to the authored success/fail body. Three cells need connectors; two are direct.

| Pick \ Outcome | Succeeded                                                | Failed                                                          |
|----------------|----------------------------------------------------------|-----------------------------------------------------------------|
| Correct        | — (direct success body)                                  | **"You try to {correct}, but find yourself {wrong.gerund}."**   |
| Neutral        | **"You work the angles and the better play is to {correct}."** | **"Your {neutral.noun} doesn't pan out, and you end up {wrong.gerund}."** |
| Wrong          | (unreachable — wrong always fails)                       | — (direct fail body)                                            |

Each skill domain provides its own connector phrasings (terse, situation-neutral) and verb
forms (noun / gerund) for each approach. Connectors are author-static, written carefully so
they read naturally regardless of which `wrong` the encounter authored.

**Authoring impact**: the success/fail bodies should be written as if the picked approach
worked or failed cleanly. The connector handles the "you ended up doing something else"
pivot before the body fires.

---

## .enc Syntax Extension

Two attribute slots on the existing `check` predicate. No new block constructs.

```
@if check Bushcraft correct:reroute wrong:push {
  +advance_time 1
  You're past it.
} @else {
  +damage_spirits 2
  The terrain wins.
}
```

**Semantics.** When the runtime enters a `check` block with `correct:`/`wrong:` attributes, it:
1. Renders the standard 3-approach picker for the skill (using the static approach roster).
2. Resolves the outcome from `(tier, pick, correct, wrong)` per the table above.
3. Emits the appropriate connector (if any) from the per-skill connector tables.
4. Executes the success or fail body.

**Parser dispatch.** Today `check <skill> <difficulty>` resolves immediately as a predicate.
The new form is recognized by the presence of `:` in the args (`correct:X wrong:Y`). The
parser learns one new arg-shape on one existing predicate; existing parse paths are unchanged.

**Chain semantics.** A picker `check` must be the **terminal** branch of any
`@if`/`@elif` chain it appears in, paired with `@else` as its fail body. See
[[picker_check_chain_semantics]] for the rule, rationale, and parser-enforcement
details.

**Legacy `check` uses** (in `[requires]` blocks, compound conditions, etc.) are retired
because the corresponding rolls no longer exist. The `[requires]` form retains `has` / `tag` /
`quality` and `meets <skill> <tier>` (the latter replacing `check` for gate-style usage — see
§Open Decisions on requires/meets terminology).

**Choice prose for the approach picker.** Static per skill. The picker UI shows the three
verbs ("Charm / Reason / Threaten") with icons. Per-check custom choice prose would be nice
but is not in scope — encounters telegraph through the *preamble* prose, and the picker is
standard. (Revisit if playtest reveals it feels too gamey.)

---

## Skill Roster

| Skill | Verdict |
|---|---|
| Combat | Already tier-gated post-RPS pivot. Narrative `check combat` adopts new RPS approach picker. |
| Bushcraft | RPS approach picker + Travel Condition resist (40%/80%). |
| Cunning | RPS approach picker + Serious Condition resist (40%/80%). |
| Negotiation | RPS approach picker + contract/price benefits. Absorbs Mercantile. |
| Mercantile | Folded into Negotiation. Removed. |
| Luck | Dropped. See Open Decisions #2. |

---

## Skill Definitions

### Combat (combat system unchanged; narrative checks adopt RPS approach)
- Untrained: daggers + light armor (RPS combat gear gate)
- Trained: axes + medium armor
- Expert: swords + heavy armor

Tier gate for the RPS combat system is unchanged. Narrative `check combat` in .enc files
uses the new approach picker (Rush / Strategize / Outlast).

### Bushcraft

**Condition type — Travel Conditions**: exhausted, freezing, thirsty. All drain spirits daily.

- Untrained: eat every night; biome conditions apply normally
- Trained: eat every other night; correct approach always succeeds on a Bushcraft check; 40% chance to resist Travel Conditions
- Expert: eat every other night; only the wrong approach fails on a Bushcraft check; 80% chance to resist Travel Conditions

The d20 foraging check in `EndOfDay.cs` is removed. `check bushcraft` in .enc files uses the
new approach picker (Push / Plan / Reroute).

### Cunning

**Condition type — Serious Conditions**: injured, poisoned, irradiated, lattice poisoned. All drain health daily.

- Untrained: standard RPS approach resolution (coinflip on correct pick)
- Trained: correct approach always succeeds on a Cunning check; 40% chance to resist Serious Conditions
- Expert: only the wrong approach fails on a Cunning check; 80% chance to resist Serious Conditions

### Negotiation

- Untrained: standard RPS approach resolution (coinflip on correct pick); standard market and contract prices
- Trained: correct approach always succeeds on a Negotiation check; contracts pay +20%; better market prices (absorbs Mercantile Trained benefit)
- Expert: only the wrong approach fails on a Negotiation check; contracts pay +40%; better market prices + access to rare stock (absorbs Mercantile Expert benefit)

Negotiation absorbs Mercantile. The numerical multipliers in `Market.cs` and haul delivery in
`HaulDelivery.cs` become tier lookups on Negotiation.

### Mercantile
**Removed** — folded into Negotiation. Price and stock benefits reassigned to Negotiation tiers above.

### Luck
**Removed** — see Open Decisions #2.

---

## d20 Retirement Scope

d20 is retired wholesale. Every roll path comes out.

### What goes away
- **Environmental resist rolls** in `EndOfDay.cs` — replaced by binary immunity gear / Bushcraft expert.
- **Foraging roll** in `EndOfDay.cs` — replaced by Bushcraft trained passive.
- **Encounter `check` rolls** in `EncounterRunner.cs` — replaced by RPS approach resolution.
- **Resist overload** of `SkillChecks.Roll()` — gone.
- **Check overload** of `SkillChecks.Roll()` — gone.
- **`SkillChecks.Roll()` itself** — file deleted (or gutted to a `ResolveCheck(tier, pick, correct, wrong) → CheckResult` helper, depending on where the new logic lives).

### What stays random
- **Encounter selection** — which encounter fires from the pool.
- **RPS combat** — opponent move prediction.
- **Untrained-correct coinflip** — the one remaining roll in the encounter check path.

---

## Engine Changes

### `lib/Rules/`
- Replace integer skill level with `SkillTier` enum: `Untrained = 0, Trained = 1, Expert = 2`
- `PlayerState.Skills` becomes `Dictionary<string, SkillTier>`
- Add static approach roster per skill (Negotiation / Cunning / Bushcraft / Combat × 3 approaches)
- Add static connector tables per skill (verb forms + 3 connector templates)
- `ActionVocabulary`: extend `check` predicate to accept `correct:` / `wrong:` attributes

### `lib/Game/SkillChecks.cs`
- Delete `Roll()` (both overloads). No d20 path remains.
- Delete `GetTokenBonus()` / `GetTokenResist()` (tokens gone — see `inventory_consolidation.md §3`).
- Add `ResolveApproach(tier, pick, correct, wrong, rng) → CheckResult` returning `(succeeded, connector)`.
  - `connector` is a `ConnectorKind` enum (`None | CorrectToFail | NeutralToFail | NeutralToPass`).
  - The actual connector string is rendered at presentation time by looking up the skill's connector table.

### `lib/Game/Conditions.cs`
- `check <skill> correct:<approach> wrong:<approach>` parses to a new `CheckApproachCondition` shape.
- The old `check <skill> <difficulty>` parser path is removed; any remaining call sites are migrated as part of the content pass.

### `lib/Game/EndOfDay.cs`
- Remove foraging d20 path; replace with Bushcraft tier check.
- Remove resist rolls; immunity check replaces them.
- Remove `ResolveFood` balanced-meal path (slated in `inventory_consolidation.md`).

### `lib/Game/Market.cs`
- Replace `mercantile * multiplier` with `switch(mercantileTier)` price lookup.

### `lib/Encounter/`
- Parser learns attribute syntax on `check` predicate: `correct:X wrong:Y`.
- Bundle JSON for an `@if check` block with attributes carries `{ skill, correct, wrong }` instead of `{ skill, dc }`.
- Validation: `correct` and `wrong` must be valid approaches for the skill; they must be different from each other.

### `lib/Orchestration/EncounterRunner.cs`
- New flow for check blocks: render approach picker → take player input → call `ResolveApproach` → emit connector → run success/fail body.
- The picker is a new screen state (`AwaitingApproachPick`) alongside the existing `AwaitingChoice`.

### `server/GameServer/GameResponse.cs`
- Add response shape for the approach-picker state (skill name, 3 approach verbs, icon refs).
- Connector text lands in the post-pick response alongside the body prose.

### `text/encounter-tool/EncounterCli/`
- `check` command validates new attribute syntax.
- Optional: `migrate` subcommand for the content pass (mechanical: convert `check skill <DC>` → prompt for `correct:` / `wrong:`). LLM-assisted batch with manual review is the likely workflow.
- Bundle output updated for new JSON shape.

### `project/encounter-spec/format.md` and `mechanics_reference.md`
- Document new `check` attribute syntax.
- Document approach rosters per skill.
- Document connector framing (so authors understand what the engine emits before their bodies).

---

## Content Pass

~20 .enc files use `check skill <DC>` and need conversion. Mechanical steps:

1. For each `@if check <skill> <difficulty>` block, decide `correct:` and `wrong:` from the scene fiction.
2. Existing success body → wrap in `@if succeeded` branch (or keep as the direct body).
3. Existing failure body → `@else` branch.
4. Verify the preamble prose telegraphs the correct approach at an appropriate tier.

This is LLM-assistable but each conversion is a small authoring decision. Plan on a manual
review pass.

---

## Blast Radius

| Area | Scope | Risk |
|---|---|---|
| `SkillTier` enum + `PlayerState.Skills` | Type change throughout; Mercantile key removed | Medium — many read sites |
| `SkillChecks.Roll()` deleted | Entire file gutted | Low — fewer callers post-removal |
| `EndOfDay.cs` foraging + resist | Replace d20 paths with tier probability lookup (40%/80%) | Medium |
| `HaulDelivery.cs` | `mercantile * 0.10` → `switch(negotiationTier)` lookup (+20%/+40%) | Low |
| `Market.cs` | Mercantile tier lookup → Negotiation tier lookup | Low |
| `lib/Encounter/` parser | New attribute syntax on `check` | Low — additive |
| `lib/Orchestration/EncounterRunner.cs` | New approach-picker screen state | Medium |
| `server/GameServer/GameResponse.cs` | New response shape | Low |
| `.enc` files | ~20 files convert | Medium — authoring work, not code risk |
| `Luck` removed | Delete from `PlayerState.Skills`, items, encounter rewards | Low |
| Boots equip slot removed | `PlayerState` boots field gone; equip/unequip verb retired; boots become a plain inventory item with exhaustion immunity; UI equipment panel loses boots slot | Medium — touches server, UI, saves |
| Serialization / Cosmos docs | `Skills` dict value type changes; Mercantile key gone; boots slot gone | Medium — existing saves break |

Existing saves will break when `Skills` changes from `Dictionary<string, int>` to
`Dictionary<string, SkillTier>`. Decide: accept breakage (simple) or write a migration
deserializer (maps 0→Untrained, 1-3→Trained, 4+→Expert).

---

## UI / Icon Work

Three new icons per skill (4 skills × 3 approaches = 12 icons) for the approach picker.
**Owned by the user** — not in scope for the engine work, but the picker UI can't ship without them.

Negotiation:
They might be open to Negotiation. How do you want to play this?
- charm.svg Charm — Flatter, praise, or demur
- brain.svg Reason - Present facts and careful arguments
- barbute.svg Threaten - Highlight the danger of their position

Cunning:
You'll need to rely on your Cunning here. What's the plan?
- cloak-dagger.svg Hide - Misdirect, conceal, or avoid
- crown.svg Bluff - Pretend you are in a position of strength or authority
- one-eyed.svg Scheme - Trick or outwit your opponent

Bushcraft:
Your Bushcraft will be tested. How will you approach this?
- dodge.svg Push - power through with grit and determination
- compass.svg Plan - analyze the situation before acting
- treasure-map.svg Reroute - find a way around or another approach

Combat:
It looks like Combat. What's your opening move?
- sword-brandish.svg Rush - attack head on, relying on momentum and surprise
- one-eyed.svg Strategize - hold back, observe, wait for an opening
- checked-shield.svg Outlast - attempt to wear the opponent down 

Picker UI design lives in `project/screens/` (TBD which screen file).

---

## Open Discussions (mini-todo)

These are the topics that still need design conversation before code work begins.
Each one is a live thread; not blocking the design philosophy but blocking some part
of the implementation.

### A. Skill advancement during play
**Resolved (2026-05-16) — arc-completion leveling.** See [[arc_leveling]].
Advancement happens via per-arc tableau picks, cap 2 per reward slot. Cap aligns
cleanly with the tier model: 0 picks = Untrained, 1 pick = Trained, 2 picks = Expert.
Background dilution concern is moot — backgrounds no longer grant skill points
(see B), so progression is fully arc-driven.

### B. Background → starting skill state
**Resolved (2026-05-16) — start at zero.** Player begins with all skills at Untrained.
- Rewrite the background picker so backgrounds grant identity-flavored starter
  state (gear, items, flags, possibly a starting condition) instead of skill points.
- Remove the final choice from the intro line (the +8-point allocation step).
- All skill advancement flows through arc completion per A.

### C. Cunning save mechanic
**Resolved (2026-05-16).** Cunning grants a probabilistic resist against Serious Conditions
(injured, poisoned, irradiated, lattice poisoned — all drain health daily): 40% at Trained,
80% at Expert. Not a picker — a direct tier-to-probability lookup applied at the condition
application site. Replaces the old "save against unavoidable damage" framing.

### D. `@if`/`@elif` chain semantics with picker checks
**Status: real syntax gap.**
Today the chain works because `check` failure falls through to the next branch.
Example today: `@if check negotiation 12 { let by } @elif tag bribed { let by } @else { fight }`.
With the picker, silent fall-through to `@elif` after the player has already
committed to an approach feels wrong — they invested in a choice, the engine should
honor success/fail explicitly.
Candidate resolutions:
- Constrain picker `check` to terminal `@if`/`@else` only. No `@elif` after a check.
- Or introduce a dedicated construct for picker checks, separate from `@if`.
- Audit the existing corpus for how mixed/chained current usage is — guides which
  resolution costs less authoring rework.
- Also folds in: a single `.enc` can contain multiple choice paths each with their
  own check, which is fine; the question is specifically about `@elif` chains within
  one path.

### E. Exhaustion model
**Status: needs decision.**
Exhaustion stays as a condition. The d20 resist roll is going away. Two candidate
replacements:
- **Auto-apply unless immune**: PC gains `exhausted` at end-of-day unless
  gear/Bushcraft tier grants immunity. Simple, binary, aligns with d20 retirement.
- **Fixed random chance**: PC has N% chance per day to gain `exhausted`; gear/skill
  reduces the chance.
Lean toward auto-unless-immune; preserves the design ethos ("randomness only where
it's interactive") and matches the cold/thirst pattern. But worth a direct call.

---

## Open Decisions

1. **Untrained-correct coinflip %**: base 50%? Calibrate to feel right — too low makes
   Untrained pointless, too high makes the Trained tier feel flat. Suggest 50% as starting
   point, tune after playtest.

2. **Luck's fate**: **Resolved (2026-05-16) — drop entirely.** Tried to save it but Luck
   is hard to communicate in the current game structure. The Untrained-correct coinflip
   stays flat at 50% (modulo the tuning in #1). Remove Luck items, Luck encounter rewards,
   Luck from `PlayerState.Skills`.

3. **`[requires]` semantics for skill gating**: **Resolved (2026-05-16) — confirmed.**
   Replace `[requires check <skill> <DC>]` with `[requires meets <skill> <tier>]`
   (e.g. `[requires meets cunning trained]`).

4. **Per-encounter approach prose**: the picker uses static verbs ("Charm / Reason /
   Threaten"). Allowing custom prose per check is appealing but doubles authoring cost. Hold
   off; revisit if playtest reveals the static picker feels gamey.

5. **Connector phrasings — terse vs varied**: one phrasing per (skill, cell) is the starting
   point. If playtests show repetition fatigue, expand to 2-3 variations per cell with random
   selection.

6. **Approach roster as data vs hardcoded**: ship as hardcoded static tables in `lib/Rules/`.
   YAML / dynamic loading is over-engineering for a 12-entry roster.

7. **Bushcraft trained food cadence**: every other night = consume on days 2, 4, 6... Track a
   `daysSinceAte` counter on `PlayerState`, or just toggle at end of day based on parity of
   `DaysElapsed`?

8. **Save migration**: `Skills` type change breaks existing saves. Accept breakage during
   active development; write a deserializer (0→Untrained, 1-3→Trained, 4+→Expert) only when
   there are live players to protect.

9. **`[requires]` UX — hidden vs greyed-out**: **Resolved (2026-05-16) — hide.**
   Tag-gated choices read weirdly when shown. Greyed-out is acceptable as a debug-only
   affordance if simpler to implement, but the shipping behavior is hidden.

10. **Negotiation benefits beyond price**: **Resolved (2026-05-16).** Contract delivery payout
    +20% at Trained, +40% at Expert (matches old Mercantile 2 and 4 feel). Market pricing
    absorbs from Mercantile. Expert adds rare stock access.

11. **Merge Cunning + Negotiation?** **Resolved (2026-05-16) — keep separate.** Cunning resists
    Serious Conditions; Negotiation improves prices and contract payouts. Distinct passive effects
    give each a clear identity beyond the shared approach mechanic.
