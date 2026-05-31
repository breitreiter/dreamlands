---
name: arc-decompose
description: Turn an arc brief (markdown sketch of a Dreamlands narrative arc — premise, characters, beats, endings) into a structurally sound set of `.enc` files with FIXME-stub prose. Produces the SKELETON only — legal syntax, no orphan tags, no infinite loops, hubs wired per established arc patterns. Prose quality is left to later pipeline passes (colorize, factual, voice). Use when the user has written or pointed at a brief like `text/encounters/arcs/<biome>/<arc>/<Name>.md` and wants the bones of the arc on disk before any writing-pass happens.
---

# Arc decompose

Turn an arc brief into a skeleton of `.enc` files: legal syntax, every
choice routed, every quality/tag setter has a reader and vice versa,
every terminal reachable, no infinite loops, hubs wired per the
established patterns. Prose is FIXME stubs throughout — quality writing
is handled by later pipeline passes (see `plans/arc_writer.md`).

You are not a writer here. You are a structural engineer with a taste
for game flow. The acceptance bar is **does this play sanely from end
to end**, not **is this prose good**.

## Inputs

Before doing anything else, read all of the following, in order:

1. **The brief.** Always a markdown file in the arc directory, usually
   named after the arc (e.g. `TheRelayPost.md`, `the_hermitage.md`,
   `README.md`). The user will name it or pass a path.
2. **`project/encounter-spec/arc_patterns.md`** — the cumulative
   inventory of structural techniques across the authored arcs. Treat
   every numbered section as a tool in the box. Decompose by *picking*
   patterns that fit the brief, not by inventing.
3. **`rules/encounter_mechanics.md`** — the canonical, living
   vocabulary spec. Every verb, every condition shape, every front-
   matter field. When this disagrees with anything else (including
   `project/encounter-spec/format.md`), this wins.
4. **The biome's locale guide.** Located at
   `text/encounters/<biome>/tier<n>/locale_guide.txt`. The brief lives
   under `text/encounters/arcs/<biome>/<arc>/`; the tier comes from the
   brief itself (it's named, e.g. "Scrub Tier 2"). Skim for register
   and the scene palette — you won't write prose, but the FIXME
   summaries should sound like they belong in this biome.
5. **One or two reference arcs.** Pick from `arc_patterns.md` §8 the
   fingerprint that's closest in shape to what you're about to build
   (linear chain → `brides_cave`; staged hubs → `the_hermitage`;
   single mid-hub with conversation spokes → `the_fugitive` or
   `wrenbury`). Read the actual `.enc` files in the matching arc
   directory. Pattern-match the shapes, not the prose.

## Process

**Plan before writing. Do not write any `.enc` file until the user has
signed off on the plan.** Writing first and fixing later costs the user
more attention than a five-minute plan review.

### Step 1 — Read everything in §Inputs.

### Step 2 — Draft the structural plan.

Produce a short plan in chat (~half a screen) covering, in order:

1. **Arc shape.** Linear chain / single mid-hub / staged hubs / hub-of-
   hubs. Cite which §8 fingerprint you're modeling on and why this
   brief warrants that shape. If the brief has more than two
   characters or more than two locations the player must visit before
   resolution, default to a mid-hub or staged-hub shape.
2. **File list.** One `.enc` per encounter, with a one-line role per
   file (Start / mid-hub / character scene / terminal). Include
   filename conventions used by the arc (`Title Case.enc` with spaces
   is normal; see existing arcs).
3. **State map.** Every arc-local tag and quality you'll introduce.
   - Tags: `<arc>.<name>` with a one-line role (scene-visited,
     earned-knowledge, commitment marker — see `arc_patterns.md` §2d).
   - Qualities only if degree is structurally required; otherwise use
     a tag. Threshold-zero gates do not work (see §9). Use
     `!quality X 1` or a tag instead.
4. **Choice gating.** For each `[requires]` you'll author, name the
   condition and the setter scene that satisfies it. Every
   `[requires tag X]` and every `[requires quality X N]` (N≥1) must
   have a reachable `+add_tag X` / `+quality X N` setter from a
   prior scene.
5. **Endings.** Each terminal route: its trigger condition, its
   `+finish_dungeon` vs. `+flee_dungeon` verb, and any per-route
   rewards (`+add_item`, `+add_level`, gold). If the brief implies
   multiple "good" endings, the same artifact reward can hang off
   several routes (see `arc_patterns.md` §1c.ii).
6. **Pickers.** Each `check <skill> correct:X wrong:Y` you intend to
   use, in which scene, with which `correct:`/`wrong:` approaches
   from the canonical pairs in `rules/encounter_mechanics.md`. Picker
   checks are terminal in their `@if` chain; `@else` is mandatory.
7. **Choice-gating pattern per hub.** Pattern 1a (recap-in-place) or
   1a.ii (hide-the-choice) per `arc_patterns.md` §1a. Default to
   1a.ii for one-shot informational beats and 1a for hubs where
   revisiting has texture. **Every hub needs at least one
   pattern-1a or unconditional choice** to satisfy §3.1; if every
   spoke would naturally be 1a.ii, convert the first
   character-intro spoke to 1a or add an always-visible exit.
8. **Staged gates for context-shifted re-reads.** When a scene's
   meaning changes across visits (e.g., a clue reread after talking
   to the character it concerns), use the §3.3 staged-gate pattern:
   `[requires !tag A || tag B && !tag C]`. First visit fires; second
   visit fires only after precursor B; the choice hides after the
   climax tag C lands.

### Step 3 — Present the plan and wait.

Send the plan to the user. Stop. Do not write files. The model is
likely to anchor wrong on any of: which character is the moral center,
which ending is the "good" one, whether the arc has a violence-shaped
ending at all, what should be quality-tracked vs. tag-tracked. The
user catches these in seconds reading the plan; catches them in
half-hours reading the files.

When the user redirects, redraft the plan. Repeat until they accept.

### Step 4 — Write the files.

Write one `.enc` per file in the file list. For each:

- First line: display title (the title as players see it).
- Front matter: `[trigger none]` for arc-internal encounters,
  `[vignette dungeons/<arc>]` for consistency. No `[tier]` on arc-
  internal encounters (the roster sets it).
- Body: prose is FIXME stubs **one line each**, using the
  `FIXME(<scene>): <one-sentence beat summary>` form that
  `EncounterCli fixme` expects. Optional `(<register>)` annotation
  hints the later voice pass — pick from `mundane | action | horror
  | dread | wonder | revelation`. Do not write multi-paragraph prose.
- Choices: full structural form. `* Link = Preview` split where the
  brief gives both a terse hook and a longer flavor. `[requires …]`
  trailers on the choice line for hide-the-choice gating. Outcome
  blocks with proper `@if` chains, mechanic verbs, and `+open` /
  `+finish_dungeon` / `+flee_dungeon` terminators.
- Edge-transit: per `arc_patterns.md` §5, edge-transit prose lives in
  the *outgoing* outcome of a choice. Drop a FIXME line for it where
  needed; the next encounter assumes the player is already there.

Write the files in dependency order (Start.enc last, since it links
forward; or write all and verify at the end — either works).

### Step 5 — Verify.

Run:

```sh
dotnet run --project text/encounter-tool/EncounterCli -- check text/encounters/arcs/<biome>/<arc>
```

If any file fails, fix the specific syntax error and re-run. Common
breakages: unclosed `@if` braces, a picker check without `@else`, a
`[requires]` condition referencing a verb not in the vocabulary.

### Step 6 — Walk the graph.

Produce a short walk-through in chat:

- **Reachability**: list every `.enc` file and confirm a path exists
  from `Start.enc` to it. Files reachable only via dead branches are
  bugs.
- **Tag/quality balance**: list every `+add_tag X` / `+quality X N`
  setter and every `[requires tag X]` / `@if tag X` / `[requires
  quality X M]` reader. Every reader must have at least one setter
  that fires before it in some play sequence. Orphan readers and
  unused setters are bugs.
- **Termination**: list every spoke and confirm each one either
  (a) advances arc state via `+add_tag` / `+quality` / `+add_item`,
  or (b) terminates via `+finish_dungeon` / `+flee_dungeon`. A spoke
  that does neither is an infinite-loop risk.
- **Endings**: list every reachable `+finish_dungeon` and
  `+flee_dungeon` and the play sequence that triggers it. Confirm
  this matches the endings the brief promised.

### Step 7 — Hand off.

Tell the user:

- File list shipped.
- Number of FIXME beats by encounter (gives them a sense of the work
  the later passes have to do).
- Any structural questions the brief left ambiguous and you resolved
  one way — they may want to flip the call.
- Any patterns you reached for that aren't in `arc_patterns.md` yet
  — those are inventory deepening opportunities.

## Output contract

The skill's deliverable is a directory of `.enc` files satisfying
**all** of the following at the moment of hand-off:

1. `EncounterCli check` passes with zero errors. Warnings are okay if
   the user accepts them.
2. Every `.enc` has the right front-matter for its role.
3. Every choice has a destination (a `+open`, a `+finish_dungeon`, a
   `+flee_dungeon`, or a clear single-outcome resolution).
4. Every `[requires]` and `@if` condition references a tag/quality
   that is set by a reachable prior scene, or a `has`/`meets`/`check`
   the player can plausibly satisfy.
5. Every spoke advances state or terminates.
6. Every terminal is reachable.
7. Every ending route promised by the brief has a path to it.
8. Prose is FIXME stubs only. No multi-paragraph generated prose. No
   improvised flavor text masquerading as final.

## Hard rules

These are violations the skill must never produce. They are the
non-negotiables from `arc_patterns.md` §9 and `rules/encounter_mechanics.md`:

- **At least one choice per encounter must be unconditional.**
  `check` hard-rejects encounters where every choice carries a
  `[requires]` trailer (see `arc_patterns.md` §3.1). Default to
  satisfying this via pattern 1a (recap-in-place) on the first
  character-introduction spoke of each hub — that gives clean
  revisit behavior *and* an always-visible exit.
- **No `@if`, `@elif`, `@else`, `+verb`, or `}` in the body.** The
  body (everything between the title/front-matter and the `choices:`
  line) renders as raw prose at runtime; flow-control tokens display
  literally. `check` rejects them. If a terminal scene needs route-
  branched prose, put the whole branched block inside the single
  closing choice's outcome.
- **No `quality X 0` as a hide-after-visit gate.** It evaluates to
  "value ≥ 0", which is always true after init. Use a tag, or
  `!quality X 1`.
- **No picker check without `@else`.** Parser rejects this.
- **No picker check mid-chain.** Picker is terminal in its `@if`
  chain; static gates (`has`, `tag`, `quality`, `meets`) may precede
  it but `@elif` may not follow it.
- **No `[requires]` referencing a tag with no setter.** Every gate
  must have a path that opens it.
- **No spoke that loops back without state advance.** A choice that
  `+open`s back to the hub must either set a tag/quality or grant an
  item; otherwise the player can spin indefinitely.
- **No em-dashes (—) in prose stubs.** `EncounterCli check` hard-rejects them.
- **No `+remove_tag` unless the brief explicitly calls for
  reversible state.** Default to monotonic accumulation; arcs rarely
  need to forget.
- **No global tags from arc-internal encounters.** Use the
  `<arc>.<flag>` namespace. The only bare-name tag an arc touches is
  the global `<arc>_known` presence tag, and that's set by the
  storylet that hooks the arc — see if the brief implies one, but
  default to not authoring it in this pass.
- **No combat encounters (.fight files).** The skill produces `.enc`
  only; `.fight` files are out of scope. If the brief calls for
  combat, leave a FIXME and let the user wire the `.fight` later.
- **No invented vocabulary.** Every verb in `+verb args` and every
  condition in `@if` / `[requires]` must exist in
  `rules/encounter_mechanics.md`. If the brief implies a mechanic
  that doesn't, flag it for the user — do not improvise.

## Interaction model

- Plan first, write second. Always.
- Briefs are partial maps, not specifications. Read them once for
  beats, once for endings, once for state. Ask before resolving any
  ambiguity that changes the arc shape (e.g. "the brief gives two
  endings but a third feels implied — should I include it?").
- The user will sometimes anchor on a structural choice that doesn't
  fit (e.g. "make every conversation a picker check"). When this
  happens, push back once with the pattern reference; if the user
  insists, comply. They know their game.
- After writing, the **user is the final reviewer**. The skill walks
  the graph; the user okays the walkthrough. Do not declare the arc
  done until the user has read the walkthrough and accepted.

## Notes

- The brief format is freeform structured markdown. Briefs vary in
  what they specify — some give explicit choice text and reward verbs
  (`brides_cave/README.md`), some give pure prose sketches
  (`TheRelayPost.md`). The skill adapts: extract structure where
  present, infer where absent, and surface anything that needed
  inference in the hand-off (Step 7).
- The skill operates inside the arc directory. The brief, the .enc
  files, any sidecar `scenes.md` or `context.md` all live together.
- The skill does not run `bundle`, `worlds/<world>/build.sh`, or any
  asset-rebuild step. That's the author's call once the arc is
  prose-complete.
- This skill is the first stage of the broader arc-writer pipeline
  documented in `plans/arc_writer.md`. The subsequent passes
  (colorize, factual, voice, adversarial) operate on the output of
  this stage. Producing a clean structural skeleton is what makes
  those passes viable.
