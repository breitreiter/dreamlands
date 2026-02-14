# Dreamlands Encounter Format (.enc) — VS Code Extension

Syntax highlighting and snippets for authoring `.enc` encounter files.

## Installation

This is a local extension, not published to the marketplace. To install:

1. Copy or symlink this folder into your VS Code extensions directory:
   ```bash
   ln -s /path/to/dreamlands/text/enc-vscode ~/.vscode/extensions/dreamlands-enc
   ```
2. Reload VS Code.

## Features

**Syntax highlighting** for `.enc` files: titles, prose, `choices:` blocks, `@check`/`@else` branches, `+command` lines, and `"""` inscription blocks.

**Snippets:**

| Prefix | Description |
|--------|-------------|
| `choice` | Choice with link = preview text |
| `@check` | Skill check with success/failure branches |
| `"""` | Triple-quoted inscription block |
| `+damage_health` | Deal health damage |
| `+heal` | Heal health |
| `+damage_spirits` | Deal spirit damage |
| `+heal_spirits` | Heal spirits |
| `+increase_skill` | Increase a skill |
| `+decrease_skill` | Decrease a skill |
| `+skip_time` | Skip to time of day |
| `+open` | Navigate to another encounter |
| `+add_tag` | Add a world state tag |
| `+remove_tag` | Remove a world state tag |
| `+add_item` | Give player an item |
| `+add_random_items` | Give random items from a category |
| `+give_gold` | Give gold |
| `+rem_gold` | Remove gold |
| `+add_condition` | Apply a condition |
| `+lose_random_item` | Lose a random item |
| `+get_random_treasure` | Find random treasure |
| `+finish_dungeon` | Mark dungeon completed |
| `+fail_dungeon` | Mark dungeon failed |
| `+flee_dungeon` | Flee dungeon |

Skill and difficulty values are offered as dropdown completions in the relevant snippets.
