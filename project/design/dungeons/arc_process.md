# Dungeon Authoring Process

How to turn 21 dungeon sketches into playable .enc encounter chains
without producing bland or incoherent fiction.

## The Problem

Each dungeon has a detailed sketch (narrative design doc, 50-300 lines).
Converting a sketch into linked .enc scenes is ~2 hours of focused work
per dungeon if done entirely by hand. AI can cut that roughly in half,
but only if you constrain it carefully. Known failure modes:

- **Character teleportation**: Sonnet loses track of where characters
  are physically located and has them appear/disappear between scenes
- **Conflict flattening**: Sonnet dials stakes down to mild procedural
  disagreements. A sketch that says "the community collapses" becomes
  "tensions remain but people are hopeful"
- **Repetitive stakes**: Sonnet gravitates toward the same handful of
  conflict shapes (misunderstanding → reconciliation, fear → courage)
- **Theme narration**: NPCs explain the dungeon's deeper concept
  in dialogue instead of living it

## Scene Types

Every scene in a dungeon should be doing one of four jobs. If a scene
doesn't fit a type, it's probably filler. Use these as a checklist
when writing scene plans and as copy-paste skeletons when drafting.

### Intro

Establish where the PC is, what they can see, what's obviously going
on. Hook the player — suggest something interesting is afoot without
explaining it. The PC just arrived; they're orienting.

**Must accomplish:**
- Ground the PC in a specific physical location
- Establish tone and atmosphere through sensory detail
- Present at least one thing that invites investigation
- Give the player a reason to go deeper instead of walking away

**Common pitfall:** Over-explaining. The intro should raise questions,
not answer them. If the PC already understands the situation, there's
no reason to explore.

```
Scene Name

[2-3 paragraphs: where the PC is, what they see/hear/smell, one
 concrete detail that signals something is wrong or interesting]

choices:

* [Engage with the hook] = [What the PC does, concretely]
  [Outcome prose, 1-2 paragraphs]
  +open "Next Scene"

* [Cautious alternative] = [A different angle of approach]
  [Outcome prose, 1-2 paragraphs]
  +open "Next Scene"

* [Leave] = [Walk away from whatever this is]
  [Brief departure prose]
  +flee_dungeon
```

**Brides Cave example:** `Start.enc` — the PC is in a cave full of
carved writing. Two choices: venture deeper (bushcraft check changes
quality of arrival) or examine the writing (leads to a side path).
Both go deeper. Neither explains the ghosts yet.

---

### Bridge

Move the PC from one place or time to another. Bridges carry
exposition — the PC learns things in transit. Few or no meaningful
choices; the player is being moved to the next decision point.

**Must accomplish:**
- Transport the PC to a new location or moment in time
- Deliver context the PC needs for the upcoming conflict
- Maintain atmosphere and pacing (don't rush, don't drag)

**Common pitfall:** Making bridges interactive when they shouldn't be.
If the choices don't change anything meaningful, the scene is a bridge
and should flow through with minimal branching. A single check that
affects *how* the PC arrives (prepared vs. rattled) is fine.

```
Scene Name

[2-4 paragraphs: the journey, what the PC sees/learns along the
 way, exposition delivered through environment or NPC conversation
 rather than narration]

choices:

* [Continue] = [What the PC does next]
  @if check <skill> <difficulty> {
    [Arrive well — better positioned for what's ahead]
    +open "Next Scene"
  } @else {
    [Arrive poorly — a cost: time, health, or spirits]
    +skip_time <period> no_sleep no_meal no_biome
    +open "Next Scene"
  }
```

**Brides Cave example:** `A Record in Stone` functions partly as a
bridge — the PC reads the wall inscriptions (exposition about who
carved them and why), then either flees or ventures deeper. The
writing delivers context for The Ghosts without anyone explaining it.

---

### Conflict

The player faces a situation they can't ignore and must make a real
choice or take a real risk. This is the dungeon's core. The player
should have multiple outs that let them express their character and
feel like their skills and gear mattered.

**Must accomplish:**
- Present a situation with genuine stakes (not just discomfort)
- Offer 2-4 choices that reflect different values or approaches
- At least one choice should involve a skill check
- Failure paths must cost something real (health, spirits, items, time)
- Success paths should feel earned, not gifted

**Common pitfall:** Making one choice obviously correct. If "negotiate
peacefully" always works and "fight" always fails, the player isn't
making a meaningful decision. Each path should have its own logic and
its own cost.

```
Scene Name

[2-3 paragraphs: the situation, who is involved, what's at stake.
 The PC should understand enough to make an informed choice but not
 so much that the choice is obvious.]

choices:

* [Bold approach] = [What the PC does, with clear risk]
  @if check <skill> <difficulty> {
    [Success: the PC achieves their goal, at some cost]
    +heal_spirits <magnitude>
    +finish_dungeon
  } @else {
    [Failure: real consequences, not just "you feel bad"]
    +damage_health <magnitude>
    +finish_dungeon
  }

* [Cautious approach] = [A different path, different tradeoffs]
  [Outcome prose — no check needed if the tradeoff is baked in]
  +finish_dungeon

* [Walk away] = [Decline to engage]
  [The PC leaves, but the situation doesn't resolve itself]
  +flee_dungeon
```

**Brides Cave example:** `The Ghosts` — three choices (fight, flee,
approach with open hands), each with its own logic. Fighting is a hard
combat check. Fleeing is safe but you get nothing. Approaching with
open hands uses a `meets negotiation 4` gate — if you have the skill,
you get the best outcome (the ghosts' story + a keepsake); if not,
they dismiss you. No choice is punished; each reflects a different
kind of character.

---

### Epilogue

Show how things changed. The dungeon is over; this scene exists to
give the player's choice weight by depicting its consequences. Short.
No new decisions — just aftermath.

**Must accomplish:**
- Show the concrete result of the player's choice
- Resist the urge to soften or hedge ("but maybe someday...")
- Give the player a moment to sit with what happened
- End with `+finish_dungeon`

**Common pitfall:** Introducing new information or choices. The
epilogue is a landing, not a launchpad. If you're tempted to add
a twist here, it probably belongs in the conflict scene instead.

```
Scene Name

[1-3 paragraphs: what the world looks like now. Concrete, specific,
 final. No new characters, no new choices.]

+finish_dungeon
```

**When to use a separate epilogue scene:** Only when the conflict has
3+ paths that each need distinct aftermath prose. If the aftermath
fits in the conflict scene's choice outcomes (as in Brides Cave),
you don't need a separate scene — just write the ending into each
branch. A standalone epilogue is for when the consequences are complex
enough to warrant their own scene, or when multiple conflict paths
converge on a shared-but-varied ending.

---

### Putting It Together

A typical 3-scene dungeon:

```
Intro → Conflict → +finish_dungeon
Intro → Bridge → Conflict → +finish_dungeon
Intro → Conflict → Epilogue → +finish_dungeon
```

A 4-scene dungeon with a side path:

```
Intro → Bridge → Conflict → +finish_dungeon
  └──────────→ Conflict → +finish_dungeon
```

Not every dungeon needs all four types. Brides Cave is Intro → Bridge
(optional side path) → Conflict, with epilogues folded into the
conflict's choice outcomes. That's fine. The taxonomy is a lens for
making sure every scene earns its place, not a formula to fill.

---

## Pipeline: 3 Phases Per Dungeon

### Phase 1 — Scene Plan (you, ~15 min)

Read the sketch. Write a scene plan that decomposes it into .enc files.
This is where the authorial decisions live: which moments become
interactive, where skill checks go, what the emotional arc of each
scene is. The AI cannot do this well.

Format — tag each scene with its type (Intro/Bridge/Conflict/Epilogue):

```
Start.enc "The Trail" [BRIDGE]
  Body: Walking with Dara, she describes the Commons
  Choices:
    - "Ask about the people" → single, more NPC detail
    - "Walk in silence" → single, time skip
  Both → +open "The Commons"

The Commons.enc [INTRO]
  Body: Empty clearing, cold fire pit, nobody greets Dara
  Choices:
    - "Help Dara unpack" → @if check negotiation medium
    - "Talk to Oskar" → single, he explains what happened
    - "Find Maren" → single, dismantling the cistern
  All → +open "The Clutch"

The Clutch.enc [CONFLICT]
  Body: Dara lays out seed stock, says she can rebuild
  Choices:
    - "Encourage her" → single, bittersweet hope
    - "Discourage her" → single, quiet dissolution
    - "Say nothing" → single, ambiguous departure
  All → +finish_dungeon
```

Include: scene names, body summary, choice text, outcome type
(single/conditional), what checks if any, where each path goes.

Don't include: prose, dialogue, mechanics details. That's Phase 2.

### Phase 2 — AI Draft (AI, you review)

Feed the AI a single prompt containing:
1. The full sketch
2. Your scene plan
3. The constraint sheet (below)
4. Brides Cave .enc files as style reference

**One prompt per dungeon, all scenes at once.** The AI must see the
complete dungeon to maintain character consistency across scenes.

Ask it to output each .enc file in sequence, separated by filename
headers. Review the output for the failure modes listed above.

### Phase 3 — Human Polish (you, ~30-60 min)

The AI draft gives you something to react against. Common fixes:

- Tighten flabby prose (AI tends to over-describe)
- Restore stakes the AI softened
- Fix character positioning (who is where, who can see what)
- Add sensory specificity (the sketch usually has better details)
- Verify .enc syntax and +open links
- Run `dotnet run --project text/encounter-tool/EncounterCli -- check`
  on the finished files

---

## Constraint Sheet

Give this to the AI with every drafting prompt.

```
DUNGEON SCENE DRAFTING CONSTRAINTS

PROSE STYLE
- Second-person present tense. Dense, atmospheric.
- Sensory detail over emotional narration. Show cold, show silence,
  show body language. Don't tell the reader how to feel.
- No weasel hedging: "perhaps," "it seems," "you sense that maybe."
- Characters speak in short, concrete sentences. No monologues.
  No one explains how they feel — they act, and the PC interprets.
- Paragraphs of 2-4 sentences. No single-sentence paragraphs for
  dramatic emphasis.

STAKES & CONFLICT (CRITICAL)
- The sketch defines the stakes. Do not soften them.
- If the sketch says someone dies, they die. If a community collapses,
  it collapses. Do not add hopeful caveats or silver linings unless
  the sketch explicitly includes them.
- Failure outcomes must COST something: health, spirits, items, time,
  or narrative consequence. "You feel uneasy" is not a failure.
- "Discourage" or "refuse" choices are not punishment paths. Write
  them with the same care and specificity as positive choices.
- Do not resolve tension prematurely. If two characters disagree,
  they still disagree at the end unless the sketch says otherwise.

CHARACTERS
- Characters stay where the sketch puts them. If Oskar is on the
  storehouse steps, he does not follow the PC around.
- Characters have consistent motivations within and across scenes.
  Don't bend their personality to accommodate a choice branch.
- NPCs do not explain the theme. They live it. No character should
  articulate the "deeper concept" from the sketch header.
- Use dialogue sparingly. When characters speak, their words should
  reveal character, not deliver information the prose could convey.

FORMAT & MECHANICS
- Follow .enc syntax exactly. Title on line 1, blank line, body
  prose, `choices:` on its own line at column 0.
- Choice format: `* Link text = Preview text`
- Link text: under 15 words. Preview text: under 30 words.
- Use `+open "Scene Name"` for links (quoted, exact filename match
  without .enc extension).
- End every terminal path with `+finish_dungeon` or `+flee_dungeon`.
- Skills: combat, negotiation, bushcraft, cunning, luck, mercantile.
- Difficulties: trivial, easy, medium, hard, very_hard, heroic.
- Keep mechanics sparse: 1-2 skill checks per dungeon total.
- Don't gate the main experience behind a check. Checks change HOW
  you experience something, not WHETHER you experience it.
- Magnitude words for damage/gold/healing: trivial, small, medium,
  large, huge.
- Use `+skip_time <period>` when significant time passes. Periods:
  morning, afternoon, evening, night. Add `no_sleep no_meal no_biome`
  flags when the PC doesn't get proper rest.
- Use `+add_tag <id>` only when a future encounter needs to know
  something happened. Don't tag for its own sake.
```

---

## Complexity Tiers

Work the simple ones first to build momentum and refine the process.

### Tier A — Compact (do first)

Short sketches, clear binary/ternary clutch, 2-3 scenes.

| Dungeon | Biome | Lines | Notes |
|---------|-------|-------|-------|
| Warrant Oak | Forest | 50 | Shortest sketch. Simple confrontation. |
| Forester's Post | Forest | 139 | Three-way clutch but straightforward scenes. |
| Briar Commons | Forest | 141 | Clean structure. Two-option clutch. |
| Inkwell Hollow | Forest | 148 | |
| Metal Beast | Plains | 195 | |
| Grainway Station | Plains | 196 | |
| Harrow Line | Plains | 191 | |

### Tier B — Standard (do second)

Longer sketches, multiple characters, 3-4 scenes.

| Dungeon | Biome | Lines | Notes |
|---------|-------|-------|-------|
| The Lodge | Forest | 212 | Complex NPC. Needs careful tone. |
| Tile House | Swamp | 215 | |
| Zahlenhaus | Mountains | 226 | |
| Halfway House | Mountains | 255 | |
| Census House | Scrub | 254 | Multiple NPCs, 4 choice paths. |
| Drowning Post | Swamp | 252 | |
| Wellhead Station | Scrub | 243 | |
| Ledgerhaus | Mountains | 237 | |

### Tier C — Involved (do last)

Dense sketches, elaborate character dynamics, 4+ scenes likely.

| Dungeon | Biome | Lines | Notes |
|---------|-------|-------|-------|
| The Revenakh | Swamp | 231 | |
| The Stift | Mountains | 257 | |
| Listening Blind | Swamp | 258 | |
| The City | Plains | 264 | |
| Foundry | Scrub | 269 | |
| Relay Post | Scrub | 296 | Longest sketch. Two-NPC dynamic. |

---

## Deliverables Per Dungeon

Each finished dungeon directory should contain:

- [ ] `descriptor.yaml` — name, preview, id, biome, tier range, decal
- [ ] `Start.enc` — entry scene (or `<Name>.enc` if single-scene)
- [ ] Additional `.enc` files as needed
- [ ] All `+open` links verified (names match filenames exactly)
- [ ] All terminal paths end with `+finish_dungeon` or `+flee_dungeon`
- [ ] Passes `EncounterCli check`

---

## To-Do

### Setup
- [ ] Refine constraint sheet after first dungeon attempt
- [ ] Write descriptor.yaml template

### Tier A — Compact
- [ ] Warrant Oak — scene plan
- [ ] Warrant Oak — AI draft + polish
- [ ] Forester's Post — scene plan
- [ ] Forester's Post — AI draft + polish
- [ ] Briar Commons — scene plan
- [ ] Briar Commons — AI draft + polish
- [ ] Inkwell Hollow — scene plan
- [ ] Inkwell Hollow — AI draft + polish
- [ ] Metal Beast — scene plan
- [ ] Metal Beast — AI draft + polish
- [ ] Grainway Station — scene plan
- [ ] Grainway Station — AI draft + polish
- [ ] Harrow Line — scene plan
- [ ] Harrow Line — AI draft + polish

### Tier B — Standard
- [ ] The Lodge — scene plan
- [ ] The Lodge — AI draft + polish
- [ ] Tile House — scene plan
- [ ] Tile House — AI draft + polish
- [ ] Zahlenhaus — scene plan
- [ ] Zahlenhaus — AI draft + polish
- [ ] Halfway House — scene plan
- [ ] Halfway House — AI draft + polish
- [ ] Census House — scene plan
- [ ] Census House — AI draft + polish
- [ ] Drowning Post — scene plan
- [ ] Drowning Post — AI draft + polish
- [ ] Wellhead Station — scene plan
- [ ] Wellhead Station — AI draft + polish
- [ ] Ledgerhaus — scene plan
- [ ] Ledgerhaus — AI draft + polish

### Tier C — Involved
- [ ] The Revenakh — scene plan
- [ ] The Revenakh — AI draft + polish
- [ ] The Stift — scene plan
- [ ] The Stift — AI draft + polish
- [ ] Listening Blind — scene plan
- [ ] Listening Blind — AI draft + polish
- [ ] The City — scene plan
- [ ] The City — AI draft + polish
- [ ] Foundry — scene plan
- [ ] Foundry — AI draft + polish
- [ ] Relay Post — scene plan
- [ ] Relay Post — AI draft + polish

### Final
- [ ] Bundle all dungeons: `EncounterCli bundle`
- [ ] Verify all descriptors match dungeons_roster.yaml
- [ ] Playtest at least one dungeon per biome
