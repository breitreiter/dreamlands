# Encounter format

Encounter files use a token-driven format. Four sigils identify the role of each line; everything else is prose. Indent is cosmetic — braces carry block structure.

**File extension:** `.enc`

**Reference files:** `intro/00_Intro.enc`, `swamp/tier2/The Hermit of Sallow Fen.enc`

---

## 1. Document structure

| Part | Definition |
|------|------------|
| **Title** | First line of the file. |
| **Body** | All lines from line 2 until `choices:`. Prose, blank lines, and inline markdown are valid. |
| **Choices block** | From the line `choices:` to end of file. Parsed per section 3. |

The `choices:` delimiter must appear at column 0, on its own line.

**Encoding:** UTF-8. Normalize line endings to `\n` before parsing.

---

## 2. Line roles (choices block)

After `choices:`, every line's role is determined by its first non-whitespace characters:

| First chars | Role | Example |
|-------------|------|---------|
| `* ` (asterisk + space) | **Choice boundary** | `* Accept her hospitality = Sit and eat...` |
| `@keyword` | **Flow control** | `@check negotiation medium {` |
| `} ...` | **Block close** | `}` or `} @else {` |
| `+verb` (+ immediately followed by a letter) | **Game command** | `+damage_health small` |
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

### 3.2 Outcomes

After a choice boundary, the parser collects lines until the next `* ` or end of file. These lines form the choice's outcome, which is one of:

**A) Single outcome** — prose and `+commands`, no braces:

```
* Press on into the swamp = Thank her and refuse.
  You barely make it a dozen yards...
  +add_condition lost
  +skip_time evening
```

**B) Branched outcome** — a `@check` block with success and failure branches:

```
* Accept her hospitality = Sit, eat, and trade what information you can.
  @check negotiation medium {
    You trade tales of the world beyond the fen...
    +add_random_items 3 food
  } @else {
    You wake retching in cold water...
    +damage_health small
    +skip_time morning
  }
```

**C) Mixed** — prose before a `@check` block (the prose always renders, the check determines what follows):

```
* Demand to know what is in the shack
  The hermit's smile doesn't waver as the thing begins to move...
  @check combat hard {
    The sailcloth tears away. You fight and win.
    +get_random_treasure
  } @else {
    You flee into the muck.
    +lose_random_item
  }
```

### 3.3 Block structure

- `@check <skill> <difficulty> {` opens a success branch.
- `} @else {` closes the success branch and opens the failure branch. May also be written as `}` then `@else {` on the next line.
- `}` alone closes the current block.
- Braces must be matched. Unclosed `{` is an error.

### 3.4 Game commands

Lines starting with `+` immediately followed by a letter are game commands. The `+` is stripped; the rest is the action string (`verb arg1 arg2 ...`). Commands are validated against the action vocabulary (section 5).

### 3.5 Parser output

For each choice, the parser produces:

- **OptionText** (string): full option line after `* `.
- **OptionLink** (string, optional): text before `=`, if present.
- **OptionPreview** (string, optional): text after `=`, if present.
- Either:
  - **Branched:** ConditionAction (string from `@check`), Success (prose + commands), Failure (prose + commands).
  - **Single:** prose + commands.

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

### Conditions (used in `@check`)

| Verb | Arguments | Description |
|------|-----------|-------------|
| `check <skill> <difficulty>` | skill, difficulty | Branch on a skill check |

### Game commands (used with `+`)

| Verb | Arguments | Description |
|------|-----------|-------------|
| `open <id>` | encounter id | Navigate to another encounter |
| `add_tag <id>` | tag id | Set a world-state flag |
| `remove_tag <id>` | tag id | Clear a world-state flag |
| `add_item <id>` | item id | Give player a specific item |
| `add_random_items <count> <category>` | int, category | Give random items from a category |
| `lose_random_item` | (none) | Player loses a random item |
| `get_random_treasure` | (none) | Player gets a random valuable |
| `give_gold <amount>` | int | Give player gold |
| `rem_gold <amount>` | int | Take player's gold |
| `damage_health <magnitude>` | magnitude | Reduce player health |
| `heal <magnitude>` | magnitude | Restore player health |
| `damage_spirits <magnitude>` | magnitude | Reduce player spirits |
| `heal_spirits <magnitude>` | magnitude | Restore player spirits |
| `increase_skill <skill> <magnitude>` | skill, magnitude | Boost a skill |
| `decrease_skill <skill> <magnitude>` | skill, magnitude | Reduce a skill |
| `add_condition <id>` | condition id | Apply a status condition |
| `skip_time <period>` | time period | Advance to a time of day |
| `finish_dungeon` | (none) | Mark current dungeon as completed |
| `fail_dungeon` | (none) | Mark current dungeon as failed |
| `flee_dungeon` | (none) | Exit dungeon without completing it |

### Argument types

| Type | Valid values |
|------|-------------|
| **skill** | `combat`, `negotiation`, `bushcraft`, `stealth`, `perception`, `luck`, `mercantile` |
| **difficulty** | `trivial`, `easy`, `medium`, `hard`, `very_hard`, `heroic` |
| **magnitude** | `trivial`, `small`, `medium`, `large`, `huge` |
| **time period** | `morning`, `afternoon`, `evening`, `night` |
| **id** | Free-form string (item, tag, encounter, or condition identifier) |
| **int** | Positive integer |
| **category** | Item category name (e.g. `food`) |

The canonical definitions for these types live in `lib/Rules/`: `Skill.cs`, `Difficulty.cs`, `Magnitude.cs`, `TimePeriod.cs`, `ActionVocabulary.cs`.

---

## 6. Edge cases

- `*text*` (no space after first `*`) is always prose (italic markdown), never a choice.
- `**text**` is always prose (bold markdown).
- `+1` or `+ text` (digit or space after `+`) is prose, not a command. Commands require `+` immediately followed by a letter.
- `}` is only a block close when it is the entire trimmed content of a line. `}` embedded in prose is harmless.
- `@` at the start of a line is only flow control when followed by a known keyword (`check`, `else`). Other `@` usage is prose.
- Inside `{ }` blocks, `* ` is prose, not a choice boundary. Choices only start outside blocks.
