# Encounter format

Encounter files use a token-driven format. Four sigils identify the role of each line; everything else is prose. Indent is cosmetic — braces carry block structure.

**File extension:** `.enc`

**Reference files:** `intro/00_Intro.enc`, `swamp/tier2/The Hermit of Sallow Fen.enc`

---

## 1. Document structure

| Part | Definition |
|------|------------|
| **Title** | First line of the file. |
| **Front-matter** | Zero or more `[trigger ...]`, `[tier ...]`, `[vignette ...]`, and `[requires ...]` lines immediately after the title. |
| **Body** | All lines after title/front-matter until `choices:`. Prose, blank lines, and inline markdown are valid. |
| **Choices block** | From the line `choices:` to end of file. Parsed per section 3. |

The `choices:` delimiter must appear at column 0, on its own line.

### Front-matter

Optional metadata lines between the title and the body. Order doesn't matter; blank lines between them are ignored. Supported fields:

**`[trigger <value>]`** — Where this encounter fires: `road`, `settlement`, or `none`. Required. At most one per encounter. `road` = picked by overworld cadence system, `settlement` = stocked as a storylet at settlements, `none` = only reachable by explicit navigation (arc chains, intro sequence, etc.).

**`[tier <value>]`** — Which tier this encounter belongs to: `1`, `2`, or `3`. At most one per encounter. No `[tier]` = any tier.

**`[vignette <path>]`** — Override the vignette image shown during this encounter. The value is a path relative to `assets/vignettes/`, without the `.png` extension (e.g. `intro/00_Intro`, `dungeons/brides_cave`). At most one per encounter. No `[vignette]` = the UI falls back to the standard terrain/tier image for the current node.

**`[requires <condition>]`** — Gate the entire encounter. The encounter selection system skips any encounter whose prerequisites are not met. Multiple `[requires]` lines are AND-ed. Same condition syntax as choice-level `[requires]` and `@if` (see section 5). No `[requires]` = no gating.

```
The Old Man's Story
[trigger settlement]
[vignette dungeons/brides_cave]
[tier 1]
[requires tag brides_cave_known]

The market square is winding down...
```

Encounter-level `[requires]` also controls on-demand encounter visibility at POIs: dungeon hubs and settlement notices only show encounters whose requirements the player meets.

**Encoding:** UTF-8. Normalize line endings to `\n` before parsing.

---

## 2. Line roles (choices block)

After `choices:`, every line's role is determined by its first non-whitespace characters:

| First chars | Role | Example |
|-------------|------|---------|
| `* ` (asterisk + space) | **Choice boundary** | `* Accept her hospitality = Sit and eat...` |
| `@keyword` | **Flow control** | `@if check negotiation medium {` |
| `} ...` | **Block close / transition** | `}`, `} @else {`, `} @elif has torch {` |
| `+verb` (+ immediately followed by a letter) | **Game command** | `+damage_health 2` |
| anything else | **Prose** | `You trade tales of the world...` |

**Indent is cosmetic.** The parser uses sigils and matched braces to determine structure. Authors may indent for readability.

---

## 3. Choices block grammar

### 3.1 Choice boundary

A new choice starts when the parser encounters `* ` (asterisk, space) outside a brace block.

The text after `* ` is the **option text**. If it contains `=`, the part before `=` is the **link** (terse, clickable) and the part after is the **preview** (verbose, secondary).

```
* Link text = Longer preview shown in UI
* Full option text with no link/preview split
```

A trailing `[requires <condition>]` gates the choice — the runtime hides it unless the condition is met:

```
* Open the sealed door [requires has ancient_key]
* Reveal Thorvin's affair = Expose him [requires has thorvins_journal]
```

The `[requires ...]` tag is stripped from option text before link/preview splitting.

### 3.2 Outcomes

After a choice boundary, the parser collects lines until the next `* ` or end of file. These lines form the choice's outcome, which is one of:

**A) Single outcome** — prose and `+commands`, no braces:

```
* Press on into the swamp = Thank her and refuse.
  You barely make it a dozen yards...
  +add_condition lost
  +skip_time evening
```

**B) Conditional outcome** — an `@if` block with one or more branches:

```
* Accept her hospitality = Sit, eat, and trade what information you can.
  @if check negotiation correct:reason wrong:threaten {
    You trade tales of the world beyond the fen...
    +add_random_items 3 food
  } @else {
    You wake retching in cold water...
    +damage_health 2
    +skip_time morning
  }
```

**C) Multi-branch conditional** — `@if` with `@elif` branches. Static conditions (`tag`, `has`, `quality`, `meets`) stack freely. A picker `check` may only appear as the **terminal** branch, paired with `@else` as its fail body (see §3.3):

```
* Pick the lock
  @if has rusted_key {
    The key turns with a click...
  } @elif meets cunning trained {
    You work the tumblers with practiced ease...
  } @elif check cunning correct:scheme wrong:hide {
    You outthink the mechanism...
  } @else {
    The lock defeats you...
  }
```

**D) Mixed** — prose before an `@if` block (the prose always renders, the conditional determines what follows):

```
* Demand to know what is in the shack
  The hermit's smile doesn't waver as the thing begins to move...
  @if check combat hard {
    The sailcloth tears away. You fight and win.
    +gold 30
  } @else {
    You flee into the muck.
    +lose_random_item
  }
```

**E) Choice-level mechanics** — `+commands` written outside the `@if`/`@else`, either
before the block or after it. They run **in addition to** whichever branch fires, so a
shared outcome (most often a hub return) is written once instead of repeated in every
arm:

```
* Ask about the knife = The wrapped blade on the table
  @if tag fugitive.knife_truth {
    "The mark." She glances at the cloth. "Yes."
  } @else {
    "A punishment. Something that's been coming a long time."
  }
  +open "Mareen"
```

Equivalent to putting `+open "Mareen"` inside both arms, and preferred when every
branch ends the same way — a hub with eight spokes reads far better this way, and there
is no risk of one arm silently missing the return.

Ordering: the branch's own mechanics run first, then the choice-level ones, matching
reading order. Mechanics written *before* the `@if` behave identically — position
outside the block is what matters, not which side.

> Both positions were **silently discarded** by the parser until 2026-08-04, which
> dead-ended 18 choices across three arcs. Fixed, with `check` now hard-failing on any
> mechanic it would drop. See `bugs/choice_mechanics_after_conditional_dropped.md`.

### 3.3 Block structure

- `@if <condition> {` opens the first conditional branch.
- `} @elif <condition> {` closes the current branch and opens the next one. May also be written as `}` then `@elif <condition> {` on the next line.
- `} @else {` closes the current branch and opens the fallback branch. May also be written as `}` then `@else {` on the next line.
- `}` alone closes the current block.
- Braces must be matched. Unclosed `{` is an error.
- Only one `@if` per choice.
- `@elif` and `@else` are optional. A bare `@if ... { } ` with no else is valid for static conditions.
- Branches are evaluated top-to-bottom at runtime. The first matching condition wins.

#### Terminal-check rule (picker `check` only)

A picker `check` condition (`check <skill> correct:X wrong:Y`) is **only legal as the terminal branch** of a chain, and that chain **must end with `@else`**. The `@else` body is the authored fail beat.

Legal:
```
@if tag bribed {
  ...
} @elif check negotiation correct:reason wrong:threaten {
  ...
} @else {
  ...
}
```

Illegal — parse error:
- `check` mid-chain (another `@elif` follows it)
- `check` with no `@else` fallback
- `@elif` appearing after a picker check
- Lone `@if check ... {}` with no `@else`

Static conditions (`tag`, `has`, `quality`, `meets`) are unaffected and may appear anywhere in a chain.

**Why**: a picker check requires the player to commit to an approach (UI interaction). Silently falling through to the next `@elif` after the player has committed would ignore their choice. The `@else` body is the explicit fail path.

### 3.4 Game commands

Lines starting with `+` immediately followed by a letter are game commands. The `+` is stripped; the rest is the action string (`verb arg1 arg2 ...`). Commands are validated against the action vocabulary (section 5).

### 3.5 Parser output

For each choice, the parser produces:

- **OptionText** (string): full option line after `* `, with `[requires ...]` stripped.
- **OptionLink** (string, optional): text before `=`, if present.
- **OptionPreview** (string, optional): text after `=`, if present.
- **Requires** (string, optional): condition from `[requires ...]`, e.g. `"has ancient_key"`.
- Either:
  - **Conditional:** Preamble (prose before `@if`), Branches (ordered list of condition + outcome), Fallback (outcome from `@else`, optional), and **Mechanics** (choice-level commands written outside the block — see §3.2 E — which run after whichever branch fires).
  - **Single:** prose + commands.

`check` reconciles the two sides: every `+verb` line in the source must appear
somewhere in the parsed model, and a shortfall is an error. A mechanic the parser
discards is otherwise invisible — the file reads correctly and the game quietly loses
a navigation or a tag.

---

## 4. Prose formatting

Prose appears in the body and in outcome blocks. The parser stores it as-is; the renderer handles formatting.

### 4.1 Inline markdown

- **Bold:** `**text**`
- **Italic:** `*text*`

These are safe because `*text*` (no space after `*`) is always prose, never a choice marker.

### 4.2 Inscription / found document block

- **Start:** a line that is exactly `"""` (three double quotes).
- **End:** the next line that is exactly `"""`.
- **Content:** all lines between, rendered as a distinct block (e.g. different font, indented).

No other block formatting (headers, code blocks, lists, tables) is defined.

---

## 5. Action vocabulary

### Conditions (used in `@if` / `@elif` / `[requires]`)

| Verb | Arguments | Description |
|------|-----------|-------------|
| `check <skill> correct:<approach> wrong:<approach>` | skill, two approach verbs | Picker check — renders the 3-approach picker UI, resolves by tier + player pick. Terminal-branch only; requires `@else`. |
| `check <skill> <difficulty>` | skill, difficulty | **DEPRECATED** — legacy d20 roll. Parseable until Phase 4 sweep completes; emits a deprecation warning. |
| `meets <skill> <tier>` | skill, tier name | Gate on whether the player's skill tier is ≥ the target. Tier names: `untrained`, `trained`, `expert`. Deterministic, no UI. |
| `has <item_id>` | item id | Branch on whether player has an item |
| `tag <tag_id>` | tag id | Branch on whether a world-state tag is set |
| `quality <id> <threshold>` | quality id, signed int | Branch on a numeric quality. Positive threshold: value ≥ n. Negative threshold: value ≤ n. Unset qualities default to 0. |

### Game commands (used with `+`)

| Verb | Arguments | Description |
|------|-----------|-------------|
| `open <id>` | encounter id | Navigate to another encounter |
| `add_tag <id>` | tag id | Set a world-state flag |
| `remove_tag <id>` | tag id | Clear a world-state flag |
| `quality <id> <amount>` | quality id, signed int | Adjust a numeric quality by a signed amount (positive or negative) |
| `add_item <id>` | item id | Give player a specific item |
| `add_random_items <count> <category>` | int, category | Give random items from a category |
| `lose_random_item` | (none) | Player loses a random item |
| `give_gold <amount>` | int | Give player gold |
| `rem_gold <amount>` | int | Take player's gold |
| `damage_health <amount>` | int | Reduce player health |
| `heal <amount>` | int | Restore player health |
| `damage_spirits <amount>` | int | Reduce player spirits |
| `heal_spirits <amount>` | int | Restore player spirits |
| `increase_skill <skill> <amount>` | skill, int | Boost a skill |
| `decrease_skill <skill> <amount>` | skill, int | Reduce a skill |
| `add_condition <id>` | condition id | Apply a status condition |
| `skip_time <period> [flags...]` | time period + optional flags | Advance to a time of day |
| `finish_dungeon` | (none) | Mark current dungeon as completed |
| `flee_dungeon` | (none) | Exit dungeon without completing it |

### Argument types

| Type | Valid values |
|------|-------------|
| **skill** | `combat`, `negotiation`, `bushcraft`, `cunning` |
| **tier** | `untrained`, `trained`, `expert` (used with `meets`) |
| **approach** | Lowercase identifier — one of the 3 approaches for the skill (e.g. `reason`, `threaten`, `flatter` for negotiation) |
| **difficulty** | `trivial`, `easy`, `medium`, `hard`, `very_hard`, `epic` — **deprecated**, legacy DC form only |
| **time period** | `morning`, `afternoon`, `evening`, `night` |
| **id** | Free-form string (item, tag, encounter, or condition identifier) |
| **int** | Positive integer |
| **signed int** | Integer, positive or negative (e.g. `2`, `-1`) |
| **category** | Item category name (e.g. `food`) |
| **skip_time flags** | `no_sleep`, `no_meal`, `no_biome` — suppress daily-rest accounting when time transit crosses a rest period |

The canonical definitions for these types live in `lib/Rules/`: `Skill.cs`, `Difficulty.cs`, `TimePeriod.cs`, `ActionVocabulary.cs`.

---

## 6. Edge cases

- `*text*` (no space after first `*`) is always prose (italic markdown), never a choice.
- `**text**` is always prose (bold markdown).
- `+1` or `+ text` (digit or space after `+`) is prose, not a command. Commands require `+` immediately followed by a letter.
- `}` is only a block close when it is the entire trimmed content of a line (or starts a `} @else`/`} @elif` transition). `}` embedded in prose is harmless.
- `@` at the start of a line is only flow control when followed by a known keyword (`if`, `elif`, `else`). Other `@` usage is prose. The old `@check` keyword is no longer supported and produces a parse error.
- Inside `{ }` blocks, `* ` is prose, not a choice boundary. Choices only start outside blocks.
- `[requires ...]` is only parsed at the end of a `* ` choice line, not in prose or outcome text.
