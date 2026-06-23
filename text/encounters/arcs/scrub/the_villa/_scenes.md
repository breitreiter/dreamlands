# Scenes — The Villa

Per-scene **intent, register, and resulting state** for the prose stages.
This is deliberately *not* a topology doc: the branch wiring lives in the
`.enc` files (and factual reads the beats directly), so it is not repeated
here. What this file carries is the dramatic job of each scene and the
state it leaves behind, which the beats alone do not make explicit.

Why this matters for factual specifically: `factual` reads the bibles (this
file included) but **not** the per-scene `*.lens.md` files (those are
colorize-only). So this is factual's one window into each scene's intent and
key. Keep it factual-facing: what the scene is *for* and what the player
walks away with, not how the choices route.

## Arc shape (the one structural fact the beats don't show)

The middle of the arc is a **non-linear journal hub**: three pages (Early /
Middle / Last) that the PC reads **in any order and to any depth**, looping
between them and out to the Decision. Do not write them as a linear sequence
(no "first you read... then..."); each page stands alone as a thing read at
an unknown point in the reading. The Decision only ever **reads** the tags
the journal pages set; it never sets discovery state itself.

**Each page is itself a browsable hub, not one block of prose.** The
experience to build is *flipping through a book, scanning for interesting
bits* — never a linear exposition dump. So each page is:
- a terse hub framing (the PC, scanning: what this part of the book covers);
- **2–3 topic-read choices**, each one journal entry / topic, written in
  Solan's first-person hand (per the lens). Each is an **infinitely
  revisitable loop** (read it, return to the hub, read it again or another);
- **the page's one optional discovery spoke**, now **gated on having read the
  specific topic that reveals that activity** (read-gate via a `read_*` tag),
  so the player earns the mini-vignette by reading the entry that points to
  it. The discovery itself stays one-shot.
- the navigation choices (to the other pages, and to the Decision).

Read-tag wiring (the only new tags this restructure adds; each has a reader =
its discovery spoke's `[requires]`):
- `villa.read_turn` (Early, "the turn" entry that mentions his coded books)
  gates the **ledger hunt**.
- `villa.read_girl` (Middle, the murder entry naming the cabinet) gates the
  **kitchen cabinet**.
- `villa.read_lastwords` (Last, "I stay below now…") gates the **cave descent**.
Non-gating topic-reads set no tag; their revisit loop is intentional (the hub
always offers the unconditional "set the journal down" exit, so no softlock).

## Scene 1 — The Mesa Road (`Start.enc`)

- **Intent:** the mundane hook turning quietly wrong. A guild errand
  (slightly furtive, nothing a merchant hasn't done) becomes a solitary
  walk through a dead colleague's house. Establish the villa as a portrait
  of its owner (see `_set`, the three ages) and end at the desk with the
  journal open. Restraint and absence, not menace.
- **Resulting state:** the PC is in the journal (or has walked away).

## Scene 2 — The Journal (`The Early Pages` / `The Middle Pages` / `The Last Pages`)

The body of the arc: a dead man's descent, read as a fellow merchant reads
another's books. Each page is a browsable hub (see arc-shape note above):
terse framing, 2–3 revisitable first-person topic-reads, one gated discovery
spoke, navigation.

- **The Early Pages — intent:** the dry, professional, faintly enviable
  beginning before it sours. Chill under business, not yet horror.
  - **Topics:** *the windfall* (the crystal taken on a bad debt, the dry
    research notes pricing an unknown); *the good days* (the absurd Aldgate
    sum, word spreading, short supply, refusable offers, sudden fascinating
    invitations); *the turn* (sellers quiet, Kesharat clamping down on
    foreign sales, the first belief he was followed — and the entry mentions
    more than once the careful coded record he keeps of every sale → sets
    `villa.read_turn`).
  - **Discovery (gated on `villa.read_turn`):** the ledger hunt finds the
    hidden formation-goods column → sets `villa.economic_value` (the PC
    understands what a cave of these is worth).
- **The Middle Pages — intent:** the descent, and the arc's moral floor.
  Render the body (in the discovery) with restraint and pity.
  - **Topics:** *the acquisition* (the broker's tip, the villa bought
    begged-borrowed-stolen, the cave all his); *the hoarding* (stopped
    selling, started sketching, contracts lapsed, could not part with one);
    *the servant girl* (paranoia with cause and past it, the girl hired
    after a bad night, the flat murder entry naming the kitchen cabinet →
    sets `villa.read_girl`).
  - **Discovery (gated on `villa.read_girl`):** the kitchen cabinet holds
    the girl, weeks dead, nothing to suggest she poisoned anyone → sets
    `villa.danger`. The crystals stop being merely worth killing for and
    become a thing that makes a man do this.
- **The Last Pages — intent:** the writing fails, and the arc's one
  supernatural beat. Wonder shading at once into wrongness; faint, isolated,
  Tier-1, never lush.
  - **Topics:** *the last words* (the hand shrunk too fine, "I stay below
    now. The cave gives me what I need. I am not hungry anymore." → sets
    `villa.read_lastwords`); *the final page* (not text but the geometric
    diagrams, "Not finished. Not close.").
  - **Discovery (gated on `villa.read_lastwords`):** the low door behind the
    desk, the steps down, the cave and its nameless faint light → costs
    `+damage_spirits 2` and sets `villa.saw_cave` (the PC has felt the pull
    Solan felt).

## Scene 3 — The Decision (`The Decision.enc`)

- **Intent:** the clutch, with the horror now in the choice rather than the
  dark. A careful conversation with a careful man under a clock. What the PC
  learned gates which branches are open, so each ending is *earned* by the
  reading. The four endings, by register: give the journal away (genuinely
  dangerous, because Vastand is intelligent and not immune); tell him what
  it cost (practical arithmetic); burn it clean (a scheme racing the
  functionary, the one skill check); keep it (the only branch that lets the
  cosmic note back in, faint, in the flash-forward of the ordered pack).
  Vastand is practical before he is moral: neither villain nor ally.
- **Resulting state:** terminal. `villa_kept_journal` set on the keep ending
  (a cross-arc hook, read nowhere in this arc; leave it set).
