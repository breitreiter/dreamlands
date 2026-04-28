# Checkpoint — Barn with Osric scene

Working scratch for the in-progress `Barn Osric.enc` scene. Delete when scene lands.

## Scene goal

Conversion-path lever. PC brings Osric to the barn; Osric tells a war story that reframes the hermitage for Edric without naming Aldous as the Scarecrow; PC chooses whether to shoulder the merchant problem personally.

Sets (on success path): `hermitage.edric_cracked`, optionally `hermitage.merchant_pledge`. Required edits to other files noted below.

## Structural notes

- Add gate to Tower.enc's Osric-to-barn option: `!tag hermitage.barn_with_osric` so the scene is one-shot regardless of outcome.
- New scene file: `Barn Osric.enc` (matches `+open "Barn with Osric"` from Tower.enc).
- `hermitage.merchant_pledge` is a new tag; add to scenes.md state list.
- `quiet_soldier_seen` should be taggable inside this scene too (mirror of Barn Alone) — the quiet soldier is at the back post as before.

## Stage 1 — Setup at the barn door

- Edric clocks Osric on sight, not by face but by type. Plant the tell: the place on his belt where a scabbard used to hang and still shapes the posture.
- Edric treats Osric as another civilian for a beat. Polite. Offers water.
- Osric quietly drops the opening: *"There may be a way through this that isn't the one any of us came in expecting. Let me tell you a thing, if you'll sit."*
- PC choice:
  - **A. Demand they leave.** Edric does pleasant-but-no. Osric does not intervene. Scene ends; `hermitage.barn_with_osric` set; opportunity burned.
  - **B. Let Osric speak.** Yields the floor. Story begins.

## Canon reminders (per Joseph, world background)

- Twenty years before the Scarecrow: two imperial legions attacked an abandoned city of an advanced civilization. Its automated defenses decimated them and drove the empire out of the borderlands.
- "The reconquest" is not an army vs. army war. It is clearing hostile hibernated defensive automata ("the Iron" in soldier's vernacular) and evicting scavengers/squatters from the old imperial fort network.
- The empire has little real interest in retaking the borderlands. Most soldiers are sent there to look busy.
- Aldous was an early such soldier, sent with nonsense orders (keep Aldgate's farms safe, light scouting). On his own initiative he built a small focused force and retook a network of ~20 forts, halting at the Spires (the automated defensive perimeter surrounding the abandoned city).
- He was blinded and lost many men when he tried to push past the Spires. The empire had no one to replace him; command stalled; his gains decayed.

## Stage 2 — Osric's story (draft, not final prose)

A fort on the inner edge of the reconquest. Scavengers dug in inside. Above it, on a ridge, an Iron sentinel still waking on an unknown schedule — forty years asleep, then a week awake, then down again. The squatters had worked out their own accommodation with it. They knew when to be inside. The imperial column did not. HQ's instruction was to push through; fifteen men dead in the first hour would have been the cost of doing business.

The Scarecrow said no. He said they would take the fort when they understood the sentinel. They sat on the opposite ridge for thirty-one days. No patrols. No fires the machine could see. He watched. He wrote in a little stub-of-charcoal book Osric was permitted to see once: columns of numbers, wind direction, cloud cover, time of day. The sentinel was not random. He had worked out that it measured heat against background and would not wake for a cold, slow body below a threshold he could name in yards. He had worked out it was blind into the sun for a quarter of an hour at dusk.

On the thirty-second day he sent the company across in ones and twos at that quarter-hour, crawling, wrapped in wet cloaks to dampen their heat. Three evenings to move forty-one men. Not one lost.

The scavengers were as surprised as the machine. The Scarecrow did not fight them. He walked to the door with two men and talked through the boards for an hour. They came out at dawn. Fed. Kept the fort. Squatters sent east with a cart and their kit. HQ would have preferred them put to the sword. He did not.

Osric asks afterward how he had known the sentinel would not wake. *"I did not know. I had watched. Thirty-one days of watching is harder than a fight."*

Twenty-three forts that way, over eleven years. Twenty-four if you count the one they didn't walk out of.

**Pivot — why he walked:**

Then came the Spires. He wanted to push. HQ didn't, but he convinced them. They took a tower on the near edge and something on the wall woke up that nobody had cataloged. Heat beam. Took his eyes and took nineteen men before they could fall back. He was on his back a month. When he got up he did not get up the same man. Said he had seen something. Would not say what. Told them they could go home or come with him. Eight came. Three are still alive. The old man is one.

**How identification with Aldous lands (never stated):**
- Edric's men shift. One looks away. One doesn't.
- Edric's own face doesn't move, but his eyes find the window in the direction of the chapel and come back.
- Osric's closing phrase *"the old man"* — not Aldous, not the Scarecrow. Edric already knows by then.
- Edric responds not with a question but with a small concession — *"And you walked out with him."* *"We did."* That's where `edric_cracked` sets.

## Stage 3 — The ask

Osric's pivot: the way through can't happen from inside the compound. The merchant's people need a name they can hold to account, and it cannot be the hermitage's. It has to be someone who walks in and out of the world. *Yours.*

**Decision pending:** road encounter vs. letter.

- **Recommended:** road encounter. Pledge (`hermitage.merchant_pledge`) unlocks an overworld encounter where the PC stands across from the robbed merchant and makes it real. Conversion terminal requires `merchant_vouch`, which only sets on completing that encounter. The pledge is load-bearing; the merchant encounter is the cost.
- **Softer alternative:** letter only. PC stamps a guild letter in the scene; `merchant_vouch` sets immediately. Cleaner, less demanding, turns a commitment into a promise rather than a payment.

Closing choices (either option):

- **"I'll make it right."** — sets `hermitage.merchant_pledge` + `hermitage.edric_cracked`. Osric prepares the letter stamped with the PC's ring; PC leaves the barn committed.
- **"I can't carry this."** — sets `hermitage.edric_cracked` only. Osric receives the refusal without argument: *"Then it was worth asking. Thank you for bringing me along."* Conversion's merchant prereq stays unmet.

## Open questions for Joseph

1. Road encounter or letter-only for the merchant pledge?
2. OK with the scene being one-shot (burn the Osric lever if PC takes branch A)?
3. Any objection to the fort story's specifics (one Iron sentinel, forty-one men, thirty-one days of watching, twenty-three forts over eleven years, nineteen dead at the Spires)? Fort still unnamed — can add one if you want.
4. Is "the Iron" the right soldier's collective noun for the automata, or do you already have a term in world lore?
