---
kind: plan
title: Skill system rework — collapse to untrained/trained/expert tiers, retire d20, adopt RPS approach picker for encounter checks
state: exploring
created: 2026-05-15
updated: 2026-05-15
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
related:
  - inventory_consolidation.md
  - inventory_slot_refactor.md
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
| Negotiation  | **Flatter** (praise)  | **Reason** (facts, argument) | **Threaten** (highlight risk) |
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

**Legacy `check` uses** (in `[requires]` blocks, compound conditions, etc.) are retired
because the corresponding rolls no longer exist. The `[requires]` form retains `has` / `tag` /
`quality` and `meets <skill> <tier>` (the latter replacing `check` for gate-style usage — see
§Open Decisions on requires/meets terminology).

**Choice prose for the approach picker.** Static per skill. The picker UI shows the three
verbs ("Flatter / Reason / Threaten") with icons. Per-check custom choice prose would be nice
but is not in scope — encounters telegraph through the *preamble* prose, and the picker is
standard. (Revisit if playtest reveals it feels too gamey.)

---

## Skill Roster

| Skill | Verdict |
|---|---|
| Combat | Already tier-gated post-RPS pivot. Narrative `check combat` adopts new RPS approach picker. |
| Bushcraft | Convert to passive unlocks per tier + RPS approach picker for `check bushcraft`. |
| Cunning | RPS approach picker. |
| Negotiation | RPS approach picker. |
| Mercantile | Easy — remap price multipliers to fixed tiers. |
| Luck | Possibly retained as Untrained-coinflip nudge. See Open Decisions. |

---

## Skill Definitions

### Combat (combat system unchanged; narrative checks adopt RPS approach)
- Untrained: daggers + light armor (RPS combat gear gate)
- Trained: axes + medium armor
- Expert: swords + heavy armor

Tier gate for the RPS combat system is unchanged. Narrative `check combat` in .enc files
uses the new approach picker (Rush / Strategize / Outlast).

### Bushcraft
- Untrained: eat every night; biome conditions apply normally
- Trained: eat every other night
- Expert: immune to cold and thirst

The d20 foraging check in `EndOfDay.cs` is removed. `check bushcraft` in .enc files uses the
new approach picker (Push / Plan / Reroute).

**Interaction with immunity gear** (`inventory_consolidation.md §7`): expert Bushcraft grants
the same cold/thirst immunity as a coat and waterskin. Untrained/trained players can buy the
benefit via gear.

### Mercantile
- Untrained: standard buy/sell prices
- Trained: ~15% better prices; possibly +1 extra item visible in market stock
- Expert: ~25% better prices; access to rare/special stock tier

Numerical multiplier in `Market.cs` becomes a tier lookup. Mercantile has no `check`
encounter mechanic to convert.

### Luck
**Probably retained** as the Untrained-correct coinflip modifier (only). Removed everywhere
else — no luck encounter checks, no luck items beyond what nudges the coinflip. See Open
Decisions for the alternative (remove entirely, leave coinflip flat).

### Cunning and Negotiation
Both adopt the RPS approach picker. No `check` skill is "different" anymore — they're all the
same mechanic with different verb sets and connector phrasings.

The narrative tension argument (a failed roll is a story beat) is preserved: at Untrained tier,
even the correct approach can fail on the coinflip. At Trained tier, misreading the scene fails
the check. At Expert tier, only the actively-wrong move fails. Tension scales with character
investment; bad outcomes remain authored, not random.

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
| `SkillTier` enum + `PlayerState.Skills` | Type change throughout | Medium — many read sites |
| `SkillChecks.Roll()` deleted | Entire file gutted | Low — fewer callers post-removal |
| `EndOfDay.cs` foraging + resist | Replace d20 paths with tier check + immunity | Medium |
| `Market.cs` Mercantile pricing | Lookup table swap | Low |
| `lib/Encounter/` parser | New attribute syntax on `check` | Low — additive |
| `lib/Orchestration/EncounterRunner.cs` | New approach-picker screen state | Medium |
| `server/GameServer/GameResponse.cs` | New response shape | Low |
| `.enc` files | ~20 files convert | Medium — authoring work, not code risk |
| `Luck` semantics | Possibly retained as coinflip nudge only | Low |
| Serialization / Cosmos docs | `Skills` dict value type changes | Medium — existing saves break |

Existing saves will break when `Skills` changes from `Dictionary<string, int>` to
`Dictionary<string, SkillTier>`. Decide: accept breakage (simple) or write a migration
deserializer (maps 0→Untrained, 1-3→Trained, 4+→Expert).

---

## UI / Icon Work

Three new icons per skill (4 skills × 3 approaches = 12 icons) for the approach picker.
**Owned by the user** — not in scope for the engine work, but the picker UI can't ship without them.

Negotiation:
They might be open to Negotiation. How do you want to play this?
- charm.svg Charm - Flatter, praise, or demur
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

## Open Decisions

1. **Untrained-correct coinflip %**: base 50%? Calibrate to feel right — too low makes
   Untrained pointless, too high makes the Trained tier feel flat. Suggest 50% as starting
   point, tune after playtest.

2. **Luck's fate**: retain as ±N% nudge on the Untrained-correct coinflip, or remove entirely
   and leave the coinflip flat at 50%? Retaining gives Luck a single clear job; removing
   simplifies. Lean toward retain — the alternative is deleting Luck items / encounter rewards.

3. **`[requires]` semantics for skill gating**: the existing `[requires check <skill> <DC>]`
   syntax can't survive (no DC). Replace with `[requires meets <skill> <tier>]` for hard
   prerequisites (e.g. `[requires meets cunning trained]`). Confirm `meets` is the right verb
   or pick a different one.

4. **Per-encounter approach prose**: the picker uses static verbs ("Flatter / Reason /
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

9. **`[requires]` UX — hidden vs greyed-out**: choices with unmet `[requires meets …]` —
   hidden entirely or shown greyed-out? Greyed-out surfaces the skill system to new players
   but reveals locked content. Decide before the UI work.

10. **Negotiation benefits beyond price**: trained market unlock could be "+1 visible stock
    item" or "access to the back catalogue." What's the interesting expert-tier Negotiation
    benefit outside of market context?

11. **Merge Cunning + Negotiation?** Low stakes — keep separate unless a corpus audit shows
    they're used interchangeably. The engine cost is identical either way.
