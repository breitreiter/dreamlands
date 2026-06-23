# Scenes — The Relay Post

Per-scene **intent, register, and resulting state** for the prose stages.
This is deliberately *not* a topology doc: the branch wiring lives in the
`.enc` files (and factual reads the beats directly), so it is not repeated
here. What this file carries is the dramatic job of each scene and the state
it leaves behind, which the beats alone do not make explicit.

Why this matters for factual specifically: `factual` reads the bibles (this
file included) but **not** the per-scene `*.lens.md` files (those are
colorize-only). So this is factual's one window into each scene's intent and
key. Keep it factual-facing: what the scene is *for* and what the player walks
away with, not how the choices route.

## Arc shape (the one structural fact the beats don't show)

A **timed linear spine with two small hubs**, marching evening → midnight →
dawn under the storm. Not a browsable journal; the night moves forward and the
hubs only let the PC gather knowledge and hear both sides before each gate.

`Start` → `The Relay Post` (evening hub) → `Midnight` (the two-asks hub) → one
of three Dawn terminals (`Dawn Devra` / `Dawn Ossal` / `Dawn Refuse`).

The gating that matters dramatically (so factual understands why beats assume
what they assume): at Midnight the PC must **hear both asks before they can
act** — `relay_post.devra_asked` gates writing the letter, `relay_post.
ossal_asked` gates helping Ossal, and hearing Ossal out itself requires having
already heard Devra (`devra_asked && !ossal_asked`). So by the time the PC
commits, they have always heard both pleas. Hearing Devra also sets
`relay_post.kept_copies` (the PC now knows the duplicate sleeve exists), which
later decides whether the help-Ossal route is a clean confiscation or a
fruitless search.

Tag ledger (all `relay_post.`-namespaced): `met_devra`, `saw_wire` (Start);
`met_ossal`, `predicted_arrival` (evening hub); `devra_asked`, `kept_copies`,
`ossal_asked` (Midnight). No qualities; monotonic accumulation only.

## Scene 1 — The Dust Storm (`Start.enc`)

- **Intent:** the mundane hook tipping quietly wrong. A killing dust storm
  drives a lone guild traveller off the mesa road into the only shelter for
  miles; an isolated operator's warm, slightly manic hospitality (tea, company,
  shelter) turns into her showing the PC the anomalous wire messages and her
  private log. Establish Devra, the post, the wire, and the arc's premise — the
  horror is paperwork that looks correct and asks for the impossible. Ends with
  an old man coming in the back door out of the storm and the room stiffening.
- **Register:** baseline warm, relief after a hard road, the off-key note faint
  and living in the files, not the people. Not horror.
- **Resulting state:** `met_devra`, `saw_wire`, advance to the evening hub — or,
  on the "ride it out" choice, the PC waits the storm out and never knocks
  (`+flee_dungeon`, no reward: they declined the arc before entering it).

## Scene 2 — The Relay Post (`The Relay Post.enc`, evening hub)

- **Intent:** the debate, worn smooth by long repetition. Meet Ossal and hear
  the Lattice case stated calmly and reasonably; optionally re-read the file
  with his framing in mind and find the maintenance notice that carries the
  post's own coordinates, timestamped before the PC arrived, with no origin
  header (the cold seam the evening turns on). Two intelligent, honest people
  talking past each other — she asks "what do these orders mean?", he asks
  "what kind of system produces them?". Ends when the PC settles in to wait for
  midnight: Devra takes the desk for the night shift (the strange messages come
  in late), Ossal takes the storeroom cot.
- **Register:** social, conversational, unease not horror. Ossal's cosmic
  claims are his sincere belief, stated plainly; the narrator never confirms
  them. The predicted-arrival message is the one fact that does not close.
- **Resulting state:** `met_ossal` (on the Ossal spoke); optionally
  `predicted_arrival` (on the re-read, which is gated to require `met_ossal`
  first); the "wait for midnight" choice advances to `Midnight`.

## Scene 3 — Midnight (`Midnight.enc`, the two-asks hub)

- **Intent:** the clutch. Two private pleas, staged so neither speaker hears
  the other (Devra crouched low at the PC's chair while Ossal supposedly sleeps;
  Ossal behind the closed storeroom door, fully dressed, never asleep). Devra:
  a sealed guild letter to the Aldgate chapter so she can cross the gate as
  something other than a deserter — plus the confession that she has kept
  duplicate copies of the anomalous transcriptions, evidence for someone
  outside the Kesharat system. Ossal: help him detain her until a patrol
  arrives, framed entirely in secular terms (a disloyal employee, classified
  copies, the settlements that lose the relay), plus an insinuation that she is
  isolation-addled and a reputational risk. The PC hears both, then commits.
- **Register:** hushed, conspiratorial, the moral weight landing; human strain
  under a clock toward dawn. The cosmic note is offstage entirely — this is two
  frightened people and a small, heavy decision.
- **Resulting state:** hearing Devra sets `devra_asked` + `kept_copies`;
  hearing Ossal sets `ossal_asked`. The three commit-choices route to the three
  Dawn terminals: write the letter → `Dawn Devra`; help Ossal → `Dawn Ossal`;
  decline both → `Dawn Refuse`.

## Scene 4a — Dawn, Devra Leaves (`Dawn Devra.enc`)

- **Intent:** the help-Devra ending. The PC's kindness is small (a letter, five
  minutes of guild form) and its cost is concrete: the relay goes dark behind
  her. Devra walks west toward Aldgate without a goodbye and without looking
  back at the wire. Ossal does not take it well — he breaks into curses (by
  name, family, office), swears patrols and chains, and turns insinuation and a
  warning about the Trade Guild on the PC. Then the wire comes alive in a long
  sequence neither of them understands — the arc's one openly supernatural
  beat — and the old man falls silent.
- **Register:** aftermath and rupture, ending on the cold supernatural note (the
  machine speaking at length with no hand at the key). Keep that beat faint but
  unmistakable; it silences rather than threatens.
- **Resulting state:** terminal. `+finish_dungeon` + `+add_level` — the PC saw
  the arc's central situation through and made a real call.

## Scene 4b — Dawn, Securing the Post (`Dawn Ossal.enc`)

- **Intent:** the help-Ossal ending, the bleak one. The relay stays open; the
  cost is Devra, broken into compliance — a careful, accurate woman kept at a
  post she no longer believes in. Three routes:
  - **Confront her openly.** She argues, line by line from her log, then goes
    flat and sits; Ossal confiscates the wax-paper sleeve. On the road out the
    PC meets a pair of Kesharat warriors looking for Ossal and directs them to
    the post. Clean resolution → `+finish_dungeon` + `+add_level`.
  - **Take the copies by scheme** (requires `kept_copies` — the PC must have
    heard the confession). A single **Cunning picker** (`correct:scheme
    wrong:bluff`): on success the PC draws Devra's attention while Ossal lifts
    the sleeve from her pack, the originals stay filed, the relay stays open →
    `+give_gold 15` + `+finish_dungeon` + `+add_level`. On failure the PC's eyes
    give the plan away, Devra burns the duplicates in the brazier, the evidence
    is gone → `+give_gold 15` + `+finish_dungeon`, **no level** (a failed gamble
    that destroyed the thing the PC came to preserve).
  - **Search the post on a hunch** (requires `!kept_copies` — the PC never heard
    the confession, so does not know the sleeve exists). An hour turning the
    place over and finding nothing; Ossal cannot even say what would prove what,
    because he cannot operate the wire. Humiliating, inconclusive →
    `+give_gold 15` + `+finish_dungeon`, **no level**.
- **Register:** cold, procedural, transactional shame; no supernatural note.
  Ossal is obeyed, not vindicated; his eventual report will be accurate in
  every particular and false in every way that matters.
- **Resulting state:** terminal (see per-route verbs above).

## Scene 4c — Dawn, Walking Away (`Dawn Refuse.enc`)

- **Intent:** the refuse-both ending, the bailout. The PC declines both asks
  and leaves; what happens between Devra and Ossal after the door closes is no
  longer their business. The one thing the PC carries out is the
  predicted-arrival knowledge — a message named them before they decided to
  shelter, sent from no origin anyone can name.
- **Register:** detachment, the cold comfort of staying outside a thing; the
  single unresolved fact carried out like grit under a collar, held plainly
  rather than dramatized (the system knew they were coming; the system knows
  nothing; both are true).
- **Resulting state:** terminal. `+flee_dungeon`, no reward — the PC faced the
  arc's central choice and declined to engage it.

## Reward logic (not factual's job, but the shape it should respect)

The level rides on **resolving the arc's central situation**, not on moral
correctness: helping Devra escape (4a) and securing the post cleanly (4b
confront / 4b scheme-success) all earn it. The two outcomes that do **not**
earn it are a failed obvious gamble (4b scheme-failure burns the evidence) and
the inconclusive search (4b no-copies); the bail (4c) and the never-entered
"ride it out" (Scene 1) flee without reward.
