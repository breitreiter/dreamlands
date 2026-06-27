# Encounter-tool skills

Claude Code skills that drive the arc-writer pipeline. These are checked-in
plain-text instruction files; Claude Code discovers them via `~/.claude/skills/`
(user-level) or `.claude/skills/` (project-level). The repo's `.gitignore`
excludes `.claude/`, so the canonical copies live here and you install them
into your Claude config by symlink (or copy).

## Available skills

| Skill | What it does |
|---|---|
| `arc-sketch` | Author or revise an arc's Stage-0 narrative sketch — premise, Canon (NPC interiority, motivations, themes, surface-vs-reserved facts), and scene-by-scene in PC point-of-view. The substrate that LEADS, before decompose. Stage 0 of the arc-writer pipeline. |
| `arc-decompose` | Turn an arc brief (markdown sketch — premise, characters, beats, endings) into a structurally sound set of `.enc` files with FIXME-stub prose. Stage 1 of the arc-writer pipeline (see `plans/arc_writer.md`). |
| `forge-run` | Run one substrate-complete arc's skeleton `.enc` through the Forge prose pipeline (parse → categorize → color → weave → integrate → promote). Centerpiece: the human-judgment PRE-KIMI color-review gate — hand-critique the gemma color bank for referent/identity/medium/invented-entity misreads before the paid weave. Companion to `plans/scrub_arc_processing.md`. |

> **Retired:** `arc-colorize` was the Stage-2 texture-generation skill for the
> EncounterCli colorize/factual/voice pipeline. That pipeline is superseded by
> the **forge** project (`~/repos/narr/forge`, being ported to a .NET `Forge`
> project per `plans/forge_dotnet_port.md`), so the skill was retired. Recover
> it from git history if needed.

## Installation

Pick one — user-level or project-level. Symlink is recommended over copy
so that edits to the canonical copy in this repo flow into Claude
immediately.

### User-level (available in every Claude session, any project)

```bash
mkdir -p ~/.claude/skills
ln -s "$(pwd)/text/encounter-tool/skills/arc-decompose" ~/.claude/skills/arc-decompose
```

Run from the repo root.

### Project-level (available only when Claude runs in this repo)

```bash
mkdir -p .claude/skills
ln -s "../../text/encounter-tool/skills/arc-decompose" .claude/skills/arc-decompose
```

The relative symlink target keeps the link valid if the repo is moved.
`.claude/` is gitignored so this lives in your working tree only.

### Verifying

Start a Claude Code session and check the available-skills list (the
system reminder lists them). `arc-decompose` should appear with the
description from the SKILL.md frontmatter.

## Authoring conventions

Each skill is a directory containing at minimum `SKILL.md` with this
frontmatter:

```yaml
---
name: <skill-slug>
description: <one-sentence summary of when to invoke this skill>
---
```

The body is the procedure. Be specific about inputs, output contract,
and hard rules. See `arc-decompose/SKILL.md` for the established shape.

## Related

- `plans/arc_writer.md` — the broader pipeline design these skills feed
  into.
- `project/encounter-spec/arc_patterns.md` — the structural-technique
  reference the `arc-decompose` skill consults.
- `rules/encounter_mechanics.md` — the living `.enc` vocabulary spec.
