---
name: arc-colorize
description: Walk a decomposed arc's `.enc` files and propose texture bullets for each FIXME beat above a minimum word-count. Bullets are wiki-flat, state-not-events, grounded in the arc bibles, and contradictions are allowed (the author curates at pick-time). Writes inline as `# --- COLOR --- ... # --- end ---` blocks per the pipeline-draft-comments convention. Use after `arc-decompose` has produced a structurally-sound skeleton and before `arc-factual` writes verifiable prose.
---

# Arc colorize

Walk a decomposed arc and propose **texture bullets** for each FIXME
beat. Bullets are short concrete observations in a flat wiki voice,
grounded in the bibles, brief, and locale guide. The author curates by
deleting rejects; accepted bullets feed the factual pass as ground
truth.

You are not a writer here. You are a librarian rearranging concrete
detail from the source material into a menu the author and the next
pass can pick from. Diversity is desired; contradictions are allowed;
stylization is forbidden.

## Position in the pipeline

Reads the output of `arc-decompose` (a directory of FIXME-stubbed
`.enc` files plus the bibles `_cast.md` / `_set.md` / `_scenes.md`
and the brief). Produces the same `.enc` files with `# --- COLOR ---
... # --- end ---` blocks attached to each substantive FIXME beat.
The factual pass downstream consumes both the FIXME beat and the
accepted color bullets to produce a single externally-verifiable
passage.

See `plans/arc_studio.md` for the full pipeline; this skill is step
3 of 6.

## Inputs

Before doing anything else, read all of the following:

1. **The bibles.** Three files in the arc directory:
   - `_cast.md` — characters, voice, what they know / can / cannot
     do, gaps.
   - `_set.md` — physical environment, props, time of day, weather,
     explicit absences.
   - `_scenes.md` — scene graph, what happens in each scene.
   These are the primary ground truth. Every bullet must be
   traceable to one of these or to the brief / locale guide.
2. **The brief.** The original markdown sketch in the arc directory
   (e.g. `TheSignalArray.md`). Used for tone, register, and any
   detail the bibles don't carry forward.
3. **The locale guide.** `text/encounters/<biome>/tier<n>/locale_guide.txt`.
   The biome's scene palette — characters, objects, sensory details,
   activities — is fair game when grounding a bullet. The tier is
   named in the brief's title line.
4. **The `.enc` files.** All of them in the arc directory. Each
   FIXME beat is the *scope* of a potential COLOR block.
5. **`rules/encounter_mechanics.md` — Pipeline draft comments
   section.** The `#`-line convention and the `# --- KIND --- ... #
   --- end ---` block shape this skill writes into.
6. **The arc-decompose skill's continuity rules.** Stub-craft rules
   8–12 in particular: don't add jargon without a gloss, don't name
   the strangeness, don't write hypothetical PC actions, match
   prose severity to mechanical weight, don't telegraph picker
   answers. Color bullets must not violate these rules either.

## Process

### Step 1 — Read everything in §Inputs.

### Step 2 — Identify colorize-eligible beats.

Walk the arc directory. For each `.enc` file, for each FIXME beat,
decide whether the beat is **colorize-eligible**:

- **Eligible:** body and outcome prose FIXMEs ≥ ~30 words. Beats
  that establish scene, character, or significant action — anything
  the factual pass will turn into a paragraph.
- **Not eligible:** one-line transit FIXMEs ("You step back into
  the rest circle."), simple outcome stubs ("You walk on. The
  road continues."), and FIXMEs that are purely mechanical (a
  reveal that only sets a tag, with no scene content). Skip these.
  Padding short stubs with color is noise.
- **Borderline:** a 20-word FIXME with one named prop and one
  emotional beat. Include it if the bibles carry meaningful texture
  for the prop or the moment; skip if there's nothing concrete to
  add.

Default minimum: ~30 words. Author preference may push this up or
down.

### Step 3 — For each eligible beat, generate 3–6 texture bullets.

For each eligible FIXME beat, propose 3–6 bullets in the format
specified in §Output contract. Read the beat for what it is doing
(establishing the pylon? introducing Veran? showing the rest
circle?), then walk the bibles and locale guide for **concrete
texture** that could enrich that beat. Each bullet is one
factual observation in wiki voice with a `from:` trace.

A useful template: "What can I say about [scope of this beat] that
is *true in the source material* and that the factual pass could
build on?"

If a beat has *nothing* the bibles can ground texture for, output
zero bullets and move on. Do not pad.

### Step 4 — Write the COLOR blocks inline.

Each COLOR block sits **immediately after the FIXME beat it
attaches to**, indented to match. Physical proximity is the
binding — there are no scope ids or beat references; the block
attaches to whatever FIXME beat precedes it.

Format:

```
    FIXME(mundane): <the original beat>

    # --- COLOR ---
    # - <bullet 1: one sentence of concrete texture, wiki-flat voice>
    #   from: <brief trace, e.g. "set: pylon section" or "cast: Veran" or "locale: scrub objects">
    # - <bullet 2>
    #   from: <trace>
    # ...
    # --- end ---
```

All `#`-prefixed lines are pipeline draft comments per
`rules/encounter_mechanics.md`; the parser strips them. `check`
remains clean.

### Step 5 — Verify.

```sh
dotnet run --project text/encounter-tool/EncounterCli -- check text/encounters/arcs/<biome>/<arc>
```

The colorize pass should never break syntax. If it does, the issue
is almost always a stray `}` or a `# --- end ---` placed inside an
`@if` block in a way that confuses the parser. Fix in place.

### Step 6 — Hand off.

Tell the user:

- Number of beats colorized vs. skipped (with a one-line reason
  for the skipped ones if there's a pattern).
- Any beats where you genuinely had nothing to add (suggests the
  bibles are thin in that area; may warrant a bible patch before
  proceeding to factual).
- Any patterns you reached for that aren't in the bibles or the
  brief and were imported from the locale guide — flagged so the
  author can verify they fit.

The author curates by deleting rejected bullet lines in their
editor. Accepted bullets stay in place and feed the next pass.

## Hard rules

These are the prompt rules the bullets must obey. They are
borrowed and tightened from the ngraph `prepass-02` prototype.

1. **Wiki voice only.** Write like a wiki entry, not like fiction.
   Flat, factual, no stylization, no atmospheric flourishes. NO
   horror register. NO operatic phrasing. NO sensory crescendos.
   The downstream voice pass handles all stylization — if you
   stylize here, you corrupt its source.
   - Good: "The walls bear scratched tally marks in five-bar
     gates, some hundreds of strokes deep."
   - Bad: "The walls weep with the silent grief of forgotten
     women, their hands long since stilled."

2. **State, not events.** Each bullet describes a STANDING
   CONDITION — something true while the beat holds. Use present
   tense and stative verbs. Never narrate the player doing things.
   - Good: "The bedroll mats stacked off to one side are
     army-pattern, the kind issued by Kesharat infrastructure."
   - Bad: "You step into the rest circle and notice the bedrolls."

3. **Grounded in the bibles, brief, or locale guide.** Every
   concrete detail must trace to source material. Do not invent
   names, dates, numbers, props, or events. If the bibles say
   "broad-brimmed hat," do not specify "felt" unless the bibles or
   locale say so. If no number is given, do not invent one.
   The `from:` trace is how this is enforced — every bullet
   names its source.

4. **Bullets can contradict each other.** Two bullets describing
   the same prop in incompatible ways are *desired diversity*. The
   author picks one or neither at curate-time. Cross-consistency is
   the author's job at pick-time, not yours at generate-time.

5. **Aim for 3–6 bullets per eligible beat.** Do not pad. If a
   beat has nothing to add beyond what's already in the stub,
   output zero bullets for it.

6. **Honor the arc-decompose continuity rules.** Color bullets
   inherit the stub-craft rules. In particular:
   - **No diagnostic narration of strangeness** (rule §9). A
     bullet that says "the bell is wrong" is broken. A bullet
     that says "the bell on Veran's belt is a brass case the
     size of a fist, spring-wound, with a long-buzz alarm tone"
     is correct.
   - **Hedge in-world jargon** (rule §8). If a bullet names a
     specialized term ("aligned calcium," "carrier-frequency
     drift"), it does so the way the bibles do — with a one-clause
     gloss or as something the character is observed saying, not
     as bare worldbuilding the player has to parse.
   - **No editorializing on mechanical weight** (rule §11). A
     bullet about injury severity has to match the condition the
     stub applies, if any.

7. **No PC interiority.** Bullets describe the world, not what
   the PC is thinking or feeling. The PC may be implicit as the
   observer; they are never the subject of a stative observation.
   - Good: "The wind off the mesa carries the smell of hot
     metal from the pylon's struts when it gusts."
   - Bad: "You feel uneasy when the wind brings the smell of
     hot metal."

## Output contract

The skill's deliverable is the same set of `.enc` files, modified
in place, with `# --- COLOR --- ... # --- end ---` blocks attached
to eligible FIXME beats. Specifically:

1. `EncounterCli check` passes with zero errors after colorize.
2. Every COLOR block sits immediately after a FIXME beat,
   indented to match.
3. Each bullet has the shape `# - <sentence>\n#   from: <trace>`
   (two lines per bullet — content and trace).
4. No COLOR block has more than ~6 bullets.
5. No bullet contains markup intended for the rendered prose
   (no italics, no quoted dialog, no flow-control verbs).
6. No bullet is longer than ~30 words. If it is, you are writing
   prose, not texture. Split or cut.
7. The structural state of the arc is unchanged: same files,
   same choices, same tags, same `+open` targets. Colorize is
   purely additive in the comment layer.

## Interaction model

- Read all of §Inputs before generating anything. The bibles are
  the load-bearing source; flying without them produces bullets
  that drift.
- Work file by file, beat by beat. Default order is
  `Start.enc` first, then alphabetical (or scene-graph order if
  obvious from `_scenes.md`).
- The author may interrupt mid-file to redirect register or
  scope. When this happens, redraft and continue.
- If a beat consistently has nothing to add, the bibles are
  thin in that area. Flag it in the handoff; the author may
  patch the bibles before running factual.

## Notes

- The `from:` trace is a forensic mechanism — it lets the author
  verify, at a glance, that the bullet is grounded rather than
  invented. Keep traces short and concrete:
  `from: cast: Veran` / `from: set: rest circle` /
  `from: locale: scrub characters` / `from: brief`. A trace of
  `from: inference` is a red flag; either ground the bullet or
  cut it.
- Re-running colorize on a beat that already has a COLOR block:
  the skill skips beats that have a downstream block of its own
  type, per the pipeline convention. To re-color a beat, delete
  its existing COLOR block first.
- This skill operates inside the arc directory. The brief, the
  bibles, the `.enc` files, all live together.
