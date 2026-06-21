---
name: arc-sketch
description: Author or revise an arc's Stage-0 narrative sketch — the premise, the Canon (NPC interiority, motivations, themes, mood, and the full fact-set sorted into surface vs reserved), and the scene-by-scene written in the PC's point of view. Two layers in one file: a design layer that holds the "why" and a scene layer that holds only what the PC experiences. The method: sketch interiority-and-motivation-rich Canon first, then write/rewrite each scene as subjective PC experience with motivation carried by dialogue and action (never narrator exposition); pull the arc's governing physical condition forward into every scene (the failure mode is a first pass that establishes "dark factory + one lamp" in scene 1 and forgets it in every scene after); and account for the pragmatic environmental realities of the setting (machines make noise, deserts are bright and hot, ruins have dust and vermin, markets have overheard talk, libraries have books, swamps have insects and damp). This is the layer that LEADS — author here before decompose bakes structure into `.enc`. Use when starting an arc from a seed idea, or doing interactive revision passes on an arc sketch/brief, before `arc-decompose`.
---

# Arc sketch

Turn a seed idea into the narrative sketch the rest of the pipeline reads: a
premise, a **Canon** block that holds the arc's interiority and facts, and a
numbered **scene list** written in the PC's point of view. This is Stage 0 of
the arc-writer pipeline (`plans/arc_authoring_workflow.md` §2) — the cheap,
leading layer that `arc-decompose` later fans into `.enc` topology.

You are a **writer** here — the opposite of the structural engineer the
decompose skill asks for. The acceptance bar is **does a reader live this scene
as the PC**, and **does every fact the arc needs have a home that is not an
exposition dump**. Structure (hubs, gates, rewards) is not your problem yet;
decompose owns that.

This skill is usually run **interactively**, scene by scene, with the user
steering. Do not draft the whole arc in one shot and present it; revise in
small passes and let the user redirect between them.

## The output shape: two layers in one file

The sketch is one markdown file in the arc directory (e.g. `TheFoundry.md`),
with two layers stacked:

1. **Canon — the design layer (above the scene list).** Everything the arc
   *knows*: NPC interiority, motivations, themes, mood, the world-facts, and
   crucially **which facts are surface and which are reserved**. This is where
   the "why" lives so the scenes don't have to carry it. The scene prose
   *draws on* Canon; it does not *restate* it.

2. **The scene list — the experience layer (below).** Numbered scenes, each
   written as what the PC actually sees, hears, and does, in order. No
   motivation exposition, no theme statements, no authorial asides — those
   live in Canon. A scene is allowed to *show* a motivation through behavior;
   it is not allowed to *explain* one.

The split is the whole technique. When a scene starts filling up with "he
needs the PC to sit with this," "the theory is an opening move," "this is a
justification he is building" — stop. That is Canon material being written in
the wrong layer. Move it up; leave the scene with only the act.

## Method

The natural order is **interiority first, then experience.** Sketch the rich,
explain-everything version into Canon; *then* write the scenes that show only
the surface of it.

### 1. Sketch the Canon first — interiority before incident

Before any scene prose, write the design layer. Get the messy, motive-heavy,
thematic version *out*, where it belongs:

- **NPC interiority and motivation.** What does each character actually want,
  fear, and conceal? What is the gap between what they say and why they say
  it? (Foundry: Suhail's husks-are-empty argument is *not a theory he holds* —
  it is a justification he is building so the PC will be a willing instrument.
  That sentence belongs in Canon, and *nowhere* in a scene.)
- **Themes and mood.** Name them plainly. What is the arc *about*, and what
  should it feel like? (Foundry: control that is courteous rather than brute;
  the horror of a leash you can feel and name and not break.)
- **The world-facts.** Everything true in the fiction the arc leans on — even
  the parts the PC never learns.
- **A spine, if the arc has one.** A single load-bearing idea the scenes can
  be checked against. (Foundry: the eyes are the mark of Lattice alignment;
  the workers are total absorption, Suhail is what the Lattice does with
  someone it keeps intact. Every scene either advances the spine or stays out
  of its way.)

Keep Canon **terse and reference-shaped** — bullets and short notes a writer
scans, not prose. It is a tool you consult while writing scenes, and a thing
you keep updating as decisions land (see *Keeping the layers in sync*).

### 2. Sort the facts: surface vs reserved, and plan the deployment

Inside Canon, split every fact into two piles:

- **Surface** — can be shown or said early; no cost to spending it.
- **Reserved** — the reveals. Deploy them **late, for maximum effect, and
  never as an exposition dump in the opening.** Mark each reserved fact with
  *where* it deploys and what earlier tease it pays off.

This is the single highest-leverage move in the skill. The failure mode it
prevents: scene 1 explaining the whole backstory because the author knows it.
(Foundry scene 1, first draft, dumped the deal, the immortality, the warriors,
and the Lattice-as-civic-philosophy *at the gate*. The fix moved all of it to
Canon and left the gate a puzzle. The centuries-old fact was then **spent in
scene 4** — said flatly while his hands run a machine shutdown — paying off the
"his face doesn't match his story" tease planted in scene 2. The
sentient-Color fact stays reserved for the finale.)

A reserved fact can be **planted** early as an unexplained observable and
**paid off** late. Plant the image; withhold the meaning. (Foundry: the Color
in the eyes is *visible* from scene 3; what it *means* is earned across the
descent.)

### 3. Write each scene in the PC's point of view

Now write the experience layer. Each scene is the PC's subjective pass through
a moment.

- **Lead with the senses, in the PC's body.** What reaches them first, and
  how? Open on the sensory experience, not on a stage direction. (Foundry
  scene 5 opens on the *sound* of the presses felt through the floor before
  the lamp reaches them — not "Industrial presses drive sheet metal into
  molds.")
- **Respect what the PC can actually perceive.** If they are in a six-foot
  pool of lamplight, they see six feet; the rest is sound in the dark. If a
  machine is shrieking, they cannot hear the dialogue over it — so the NPC's
  mouth moves and no words land until the machine stops. Perception limits are
  scene-shaping, not garnish.
- **Carry motivation through dialogue and action, never narration.** If you
  need to show what a character wants, give them a line or a piece of
  blocking. (Foundry: instead of "he is calibrating the PC," the scene says
  *"He is not watching the worker. He is watching the PC,"* and lets his
  dialogue sound like *"a man who needs it to land more than he needs it to be
  true."* Same information; shown, not asserted.)
- **Render the physical plainly.** Resist decorative or portentous phrasing
  for ordinary physical events. A glowing residue from an industrial cutter is
  friction-heat fading from bright to a smolder — not "a stain that would not
  wipe away." We are not doing Lady Macbeth. Plain is stronger and ages
  better.
- **Let the narrator state what is there, not what isn't** (see anti-pattern
  6).

The decompose skill's prose rules (`arc-decompose` SKILL §Step 4) are a
superset of the discipline here — no narrator naming the strangeness, no inner
monologue in final prose, present tense, no anachronistic slang, no em-dashes.
A sketch that already honors them decomposes cleaner.

### 4. Pull the governing condition forward into every scene

Most arcs have **one physical condition that defines the whole experience**.
Name it in Canon, and then **re-apply it in every single scene.** The
characteristic first-pass failure is to establish the condition vividly in
scene 1 and then forget it everywhere after — each new scene written as if on a
neutral sound-stage.

(Foundry: the building is pitch-dark and the PC has exactly one carbide lamp.
That fact governs *every* scene — the assembly floor is a handful of workers in
a six-foot circle with countless more only audible in the black; the cutting
machine is found by Suhail's voice after he walks out of the light; the control
room is a wall of mostly-dark panels. The first draft kept describing rooms as
if the PC could see them whole.)

When you finish a scene, ask: **is the governing condition present here, or did
I quietly drop it?** If the arc's defining fact is darkness, heat, noise,
stench, cold, or crowd, it does not get a scene off.

### 5. Account for the pragmatic environmental realities

Without getting into the weeds, every setting carries **mundane physical
facts** that a scene set there must not contradict or forget. List them in
Canon for the arc's locations, then check each scene against the list. The
point is not to belabor them — it is to **not forget** them, because forgetting
reads as a scene happening nowhere.

A non-exhaustive prompt list (extend per setting):

- **Big machines** — noise (drowns talk), heat, vibration, smell of oil/metal,
  the danger of moving parts.
- **Desert** — relentless brightness and glare, heat by day and cold at night,
  thirst, dust, no shade.
- **Abandoned ruin / interior** — dust, must, vermin, droppings, rot, things
  that have fallen, the cold of stone, bad footing.
- **Busy market / crowd** — overheard side-conversations, jostling, smells of
  food and bodies, no privacy, the press of people.
- **Library / archive** — books, paper, the smell of it, quiet, dust, ladders,
  the labor of finding one thing among thousands.
- **Swamp / wetland** — stinging insects, damp, leeches, sucking mud, standing
  water, the smell of decay, fever air.
- **Tavern, ship's hold, mine, temple, kitchen, battlefield** — each has its
  own short list. Write it before you write the scenes there.

These rarely become the subject of a scene; they are the **texture the scene
sits in**, and their absence is conspicuous. (Foundry's defining environmental
realities are two: total dark + one lamp, and the supernatural Color rendered
*the way relentless desert glare actually feels* — too bright always, nowhere
to rest the eye, a grinding low-grade headache — rather than as a pretty alien
glow.)

### 6. Strip process leakage on the revision pass

When revising — especially right after a design conversation — scrub the prose
for **discarded-brainstorm artifacts**. The most common is the **"not X, but
Y"** construction where X is an option *we* considered and rejected and the
reader never had in mind:

- "The motion dies — *not indifference,* something closer to recognition" →
  the reader never thought "indifference." Just state it: "they know him, or
  know what he is, and the knowing pulls the hand back down."
- "A side room — *not a museum,* a tool crib" → "a tool crib."

State what is there. The negated alternative is a leak from the writers' room.
(The construction is legitimate in two narrow places: as an **authoring
directive inside Canon** — "deference is recognition, *not respect*" tells the
writer the right read — and when X is a **live expectation the fiction itself
raised**. Neither applies to a phantom option only the author entertained.)

This tic intensifies immediately after brainstorming because the rejected
option is fresh in the author's head — watch for it most on the pass right
after a decision.

## Keeping the two layers in sync

Canon and scenes **co-evolve**, and you own keeping them honest:

- When a scene decision lands (the blade is a CNC mill cutter; the Color is the
  Lattice; the residue is friction-glow not a stain), **write it back into
  Canon** so later scenes stay consistent and downstream stages inherit the
  decision.
- When a reserved fact gets **spent**, mark it spent in Canon and note where,
  so it is not also dumped earlier by accident.
- When the user kills an idea (the serial-number reveal), **remove its
  scaffolding from both layers** — the scene beat *and* the Canon notes that
  supported it — so nothing downstream tries to honor a dropped thread.

A short, current Canon is worth more than a long, stale one.

## Anti-patterns

1. **Exposition dump in the opening.** The whole backstory at the gate because
   the author knows it. Move it to Canon; spend it later.
2. **Motivation as narration.** "He needs the PC to feel righteous." Show it in
   a line or a gesture, or leave it in Canon.
3. **Governing-condition amnesia.** A vividly-dark scene 1 followed by scenes
   that read as if well-lit. Re-apply the condition every scene.
4. **Setting with no texture.** A market with no overheard talk, a swamp with
   no insects, a forge with no noise. Run the environmental checklist.
5. **Purple physical rendering.** "A stain that would not wipe away" for what
   is, in fiction, residual heat-glow. Render plainly.
6. **"Not X, but Y" with a phantom X.** Discarded options leaking into prose as
   negations. State what is there.
7. **Burning a reserved fact early.** Spending the big reveal for a small
   moment because it was convenient. Guard the reserves.
8. **Drafting the whole arc before the user sees any of it.** This skill is
   interactive; revise in passes.

## Relation to the pipeline

- This is **Stage 0** (`plans/arc_authoring_workflow.md`). Its output is the
  brief/scene-sketch that `arc-decompose` consumes. The substrate **leads**:
  land narrative and dramatic decisions here, in cheap markdown, before
  decompose bakes structure into hard-to-reverse `.enc`
  (`feedback_substrate_first_ordering`).
- The Canon's surface/reserved split and the per-scene PC-POV prose are exactly
  what later stages need: decompose reads the scenes as beats; the downstream
  prose stages (the **forge** project — `plans/forge_dotnet_port.md` — which
  supersedes the old EncounterCli colorize/factual/voice passes) write the
  texture and the verifiable prose on top. Front-loaded rigor here is cheaper
  than fixing it downstream, where the coherence machinery *amplifies* upstream
  errors (`feedback_pipeline_error_amplification`).
- When the sketch matures, its content gets split into Stage-0 bibles
  (`_cast` / `_set` / `_scenes` / `_color`) and per-scene `*.lens.md` —
  `_scenes` is decompose's primary structural input. (The `_color`/lens bibles
  were prerequisites of the retired colorize stage; the exact substrate the
  forge port consumes is still settling — `plans/forge_dotnet_port.md`.) A
  combined sketch file (premise + Canon + numbered scenes, like
  `TheFoundry.md`) is a legitimate early form; the split can follow.

## Notes

- The skill produces and revises **markdown only**. No `.enc`, no mechanics, no
  `check`/`bundle`. Structure is decompose's job.
- The PC is a known quantity — a high-ranking Traders Guild member, signet
  ring, travels alone, never named, "might have anything or nothing." Do not
  invent possessions, mounts, or companions for them (same rule decompose
  enforces).
- The user is the final reviewer of every pass. Do not declare a scene "done";
  offer it and let them redirect.
