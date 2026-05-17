# The Hermitage — Scene Structure

High-level scene map for the encounter. Companion brief: `the_hermitage.md`.

**Replaces:** `arcs/forest/foresters_post`. The `windstrider_boots` item is renamed to `scarecrow_boots` and is now the peaceful-resolution reward here (see Terminal section).

## Flow

```
            Approach ──(leave)──▶ (revisitable; costless)
               │
             (enter)
               │
               ▼
             Cart ──────────────▶ Tower (hub)
                                    │
                                    ├──▶ Barn    (Edric, 3 variants)
                                    ├──▶ Garden  (Aldous, one-off)
                                    ├──▶ Quarters (Meilin)
                                    │
                                    └──▶ Terminal (dispatcher)
                                            │
                     ┌──────────────────────┼──────────────────────┐
                     ▼                      ▼                      ▼
                  1a Intent.           1b Chaos              2 Conversion
                                            │                      │
                                       3 Pragmatism ◀───────────────
```

No clock. Terminal fires on explicit player commitment or on "leave / give up."

---

## Scenes

### Approach
Ambient intro outside the gate. No NPC. The hermitage at a distance — smoke from the chimney, a barn with a cookfire visible through the slats, a cart in the yard that nobody is touching. Two choices:
- `enter` → commits; hands to `Cart`.
- `leave` → free, revisitable. Re-running `Approach` on a later visit gives a light state update ("the cart is still there, darker now") and the same two choices.

### Cart
First scene inside the gate. Brother Osric is arguing with one of Edric's men — the loud one, not the quiet one — over the rotting food. Neither will touch it. Neither will throw it away. Osric is patient and specific; the imperial is aggrieved and repeating himself. PC overhears, then is noticed.

Osric wants the cart moved over to the barn, says it belongs to the imperials. Edric's man insists it belongs to the monastery as a gift. Ends in a stalemate; the cart sits in the neutral ground between the barn and the tower.

Purpose: name the factions, introduce Osric, set the central dilemma. PC can intervene lightly or just listen; both exit to `Tower`.

Sets `met_osric`.

### Tower (hub)
Central hub. Osric lingers here between chores and will answer informational questions freely — a lore-dump interface more than a companion. Available exits depend on what the PC has unlocked.

Menu:
- Talk to Osric → short Q&A sub-menu (the Kesharat, Aldous, the food, the threshold rule, his own story in sketch form — never names Aldous as the Scarecrow).
- Go to the barn → `Barn` (companion picker).
- Visit the garden → `Garden` (once).
- Cross to the Kesharat quarters → `Quarters`.
- Terminal actions (appear when their prereqs are met, see below).
- Leave the hermitage → `Terminal` via the "give up" branch.

### Barn
Edric and his squad. Three variants by `companion`:

- **Alone.** Edric is dismissive. PC learns how he thinks, what he respects, what he dismisses. Sets `met_edric`.
- **With Osric.** Requires `met_osric`. Osric tells the reconquest story, names "the Scarecrow" as his old CO, never names Aldous. Edric recalibrates. Sets `edric_cracked`. Also: the quiet soldier can be noticed here and tagged `quiet_soldier_seen`.
- **With Meilin.** Requires `meilin_accompanies`. Runs the pragmatic-path scene from the brief — Meilin and Edric slide into a tactical duet, PC is sidelined. A hard Negotiation check can pull the conversation back toward a brokered deal (sets `pragmatic_summit_open`); failure sets `edric_engaged` only and leaves Meilin with better intel on Edric's posture.

### Garden
One-off. Aldous is on the porch or at a garden bed. He does not stand, does not turn toward the PC, does not introduce himself. He asks a question the PC didn't know they'd been asked and will not explain what he means:
- *"If you stop the killing tonight, what have you saved?"*
- *"When you leave this place, who do you carry with you?"*
- *"The man who eats the stolen bread and the man who starves beside it — which of them is the thief?"*

PC picks an answer or declines. Aldous responds with silence, another question, or an apparent non-sequitur that only later resolves into a reply. No quality bumps, no strategic payoff. Sets `met_aldous`. Not revisitable; re-entry gets a brief "he is not inclined to speak again today" stub.

The scene exists for atmosphere and for the moment later when the PC realizes Osric's "Scarecrow" story was about this man.

### Quarters
Meilin's lodging — the Kesharat fire team sharing two rooms at the back of the compound. First visit: she sizes the PC up, friendly but watchful. PC can ask about her, about the deserters, about what she thinks happens next.

The load-bearing choice in this scene is asking her to come to the barn. She agrees without hesitation — it's useful to her as recon. Rolled as a **blind Negotiation check** at the moment of asking:
- **Pass** → sets `meilin_plan_known`. She tells the PC the real plan: the deadline, the method, her offer to own the consequences, and her honest read that bringing the PC along lets her see Edric's setup from inside.
- **Fail** → sets `meilin_plan_hidden`. She agrees and says little. PC does not know she is scouting. The `Barn.Meilin` scene runs the same way; the difference is what the PC thinks is happening.

Either outcome sets `meilin_accompanies`.

Re-visitable. If the PC returns after completing various hub activity, Meilin's dialogue shifts to reflect what she has heard through the community.

---

## Terminal

Single dispatcher scene. Entry points:

| Trigger | Requires | Outcome |
|---|---|---|
| "Meilin, I'm in." | `meilin_plan_known` | **1a Intentional Violence** |
| "Edric, she's coming for you." | `meilin_plan_known` | **1b Chaos (rat variant)** |
| Close the conversion | `edric_cracked` + `quiet_soldier_turned` + `merchant_vouch` + `meilin_stood_down` | **2 Conversion** |
| Broker the summit | `pragmatic_summit_open` + agreement reached | **3 Pragmatism** |
| Leave / give up | (any state) | **1b Chaos (Meilin acts)** |

### 1a Intentional Violence
PC joined Meilin knowingly. Cold, deliberate, ugly. Morning: PC and Kesharat leave together. Aldous on the porch, silent. `+finish_dungeon` with flavor for a co-authored killing. **No item reward.**

### 1b Chaos
Two sub-flavors under the same terminal:
- **Rat variant** (`meilin_plan_known` + told Edric). One of Edric's men leans in: *"Hey boss, them dusties is back but they're all kitted up for battle."* Imperials meet Kesharat in the yard. Pitched fight. Both sides take casualties. Aldous emerges when the noise stops and tells the Kesharat to leave. They do.
- **Meilin-acts variant** (PC gave up, or conversion/pragmatism failed to close). Kesharat produce hedged weapons. Execution doesn't execute — someone hears a footfall too early. Same pitched fight, same wounded devoted, same dismissal from Aldous. If `meilin_plan_hidden`: the sting lands here. PC realizes they scouted for her.

**No item reward** for either chaos variant.

### 2 Conversion
Quiet soldier turned. Merchant letter sealed and out in the world. Kesharat stand down. The imperials who convert stay; others drift. Hard work ahead for everyone. `+finish_dungeon` with flavor for risk-taking transformation.

**Reward:** Aldous gifts the PC the `scarecrow_boots` directly — the last surviving piece of his old uniform, kept all these years in a chest he hasn't opened in decades. He hands them over himself, without ceremony, as the thing he can finally put down now that someone else has done the work he couldn't.

### 3 Pragmatism
Edric-Meilin summit reaches terms. Kesharat re-arm officially. Imperials absorbed on a no-banditry rule. And Aldous, the moment he understands what has been agreed to, walks out. No farewell, no blessing, no explanation. The experiment is over. The thing he spent decades building was, in the end, a hallucination held together by his presence — and now his own people have voted to keep the shell and hollow it out. He cannot stay to watch it. No one knows where he is going or what he will do next. Some devoted follow him within the hour; others stay. `+finish_dungeon` with flavor for the state being born in a single evening, and for the founder who would not witness it.

**Reward:** Osric gives the PC the `scarecrow_boots` on the way out. Aldous left them on his cot before he walked — deliberately laid out, the only piece of his old life he chose to leave behind. Osric understood the gesture: Aldous is done carrying the soldier, and he will not carry the hermitage either. The boots go to the PC because Aldous was never going to moralize at them, and this is the closest thing to a verdict he was willing to leave.

---

## State variables

Tags (boolean):
- `met_osric`, `met_edric`, `met_aldous`, `met_meilin`
- `barn_with_osric` (one-shot gate; set on all exits from Barn with Osric regardless of outcome)
- `osric_story_heard` (phase gate within Barn with Osric; set after Osric finishes the fort story)
- `meilin_plan_known`, `meilin_plan_hidden` (mutually exclusive)
- `meilin_accompanies`, `meilin_stood_down`
- `edric_cracked`, `edric_engaged`
- `quiet_soldier_seen`, `quiet_soldier_turned`
- `merchant_vouch`
- `pragmatic_summit_open`
- `ratted_to_edric`

Qualities: none required for this arc. State fits cleanly in tags.

---

## Notes

- Osric's role is split: expositor in the hub, conversion-path lever in the barn. The hub Q&A should stay informational; the tonal shift (old corporal telling war stories with intent) belongs only in the Barn.Osric scene.
- "Dusty/dusties" is an imperial slur for Kesharat and Tashkari (interchangeable to the speaker). Only use it in the mouths of unreconstructed imperial soldiers — the rank-and-file of Edric's squad. Osric is devoted and past it; Edric is educated enough to avoid it. The rat-variant line *"them dusties is back but they're all kitted up for battle"* lands because it comes from one of the underlings, not from Edric himself.
- Aldous never speaks his own history. The PC only ever hears "the Scarecrow" from Osric; the connection to Aldous is the PC's to make.
- `meilin_plan_hidden` should carry through untouched until terminal. No scene should retroactively reveal her plan to the PC in the hidden branch; the whole point is the PC not knowing.
- Conversion is the hardest path by design. Each prereq is a real piece of work.
- Pragmatism is the easiest path to reach on purpose.
