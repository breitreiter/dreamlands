# The Fugitive — Scene Inventory

## Structure

Three acts. Act 1 is chronological (on-rails). Act 2 is conversational
(hub with people to visit). Act 3 is determined by what the PC learned
in Act 2 — three boolean variables produce four distinct outcomes. The
arc pivots on the gap between what the camp believes (execution) and
what the knife actually means (ritual scarification), and whether the
PC does anything about it.

---

## ACT 1 — The Arrival (chronological)

### 1. Start
- **Location:** Forest road, then the exile camp.
- PC overtakes a group of travelers from the high mesa — Daveth,
  Haneth, Reska. Friendly, competent, clearly not from around here.
  They're heading to a nearby exile camp. Natural travel conversation:
  recent news, the forest, trade routes.
- Arrival at camp. Maren comes out to greet the visitors. Daveth
  addresses her by her full Tashkari name — Mareen Khavar of Tarsin.
  The camp goes quiet.
- Daveth lays out the accusation. Not performative — factual, heavy,
  twelve years compressed into a few sentences. Maren does not deny
  it. "I know."
- Daveth says they'll leave in the morning. He produces the ceremonial
  knife. Maren takes it. She walks to her cabin. The camp watches.
- Night falls. The outlanders make camp at the edge of the clearing.
  The camp is awake and uneasy.
- **Choices:** Continue into the camp (-> The Camp), or walk away now
  (+flee_dungeon). Walk-away is the "not my problem" exit.
- **Communicate:** The confrontation should land without melodrama.
  Daveth is stating facts. Maren is unsurprised. But in the forest,
  everyone takes a new name. No one uses their old name. No one speaks
  anyone else's old name. It's not custom — it's the rule. Safety
  depends on it. Speaking someone's given name aloud, in front of
  others, is universally understood as unforgivable. Daveth doesn't
  know this — he's using the formal Tashkari honorific because that's
  how you address a clan debtor. But to every exile in earshot, he
  just did the one thing you never do. The camp's fury starts here,
  before anyone even processes the accusation. The sheltering side
  gets an emotional head start that has nothing to do with the merits.

---

## ACT 2 — The Fracture (conversational hub)

Hub scene. The camp at night. PC can visit people in any order. The
hub has two states — before and after the PC corrects the suicide
rumor — which changes the available conversations and the camp's mood.

Three pieces are mechanically significant (tracked via tags): the
knife truth, Maren's resolve, and Haneth's loophole. But learning the
truth isn't enough — the PC must also *tell the camp*, which is a
separate action in the hub.

### 2. The Camp (hub)
- **Location:** The camp clearing, nighttime. Fires, tension, clusters
  of people arguing.
- Short body — one or two sentences about the atmosphere. Must read
  well on repeat visits.

**Before rumor is corrected** (`fugitive.rumor_corrected` not set):
- The camp is volatile. People think Maren is going to be killed.
- Self-linking: **Gault** — overheard by the fire. Loud, posturing,
  talking about what happens to outsiders who think they can march
  into a forest camp and take people. Getting nods from people who
  wouldn't normally listen. Sets `fugitive.saw_gault`.
- Edda and Silla are not available — the mood is too hot for nuanced
  arguments. People are reacting, not reasoning.
- Wings: The Outlanders, Maren's Cabin — always available.

**Correcting the rumor** (requires `fugitive.knife_truth`):
- Self-linking choice: "Tell the camp what the knife is really for."
  The PC explains the oathbreaker's mark — scarification, not
  execution. The temperature drops. Gault loses his audience. Sets
  `fugitive.rumor_corrected`.

**After rumor is corrected** (`fugitive.rumor_corrected` set):
- The camp is tense but thinking. The crisis has shifted from panic
  to genuine disagreement.
- Self-linking choices:
  - **Edda** — principled sheltering argument. Quiet, serious. "If you
    let them take her, every person in this camp just learned their
    past can walk through the door." Sets `fugitive.talked_edda`.
  - **Silla** — personal surrendering argument. Not political. She
    watched Daveth say Maren's name and it hit somewhere she wasn't
    ready for. Family comes first. Sets `fugitive.talked_silla`.
- Wings: The Outlanders, Maren's Cabin — still available.

**Dawn gate:**
- Dawn (-> Dawn) available after at least one wing visit, gated by
  `[requires tag fugitive.night_passed]`. Always available as "wait
  for morning" regardless of other progress.

### 3. The Outlanders (wing)
- **Location:** Edge of camp. Three people around a small fire,
  keeping to themselves.
- Daveth is guarded but not hostile. He'll talk if approached
  respectfully. Reska is angry and young. Haneth is tired.
- **Key beats:**
  - Daveth or Haneth, if asked about the knife, will explain: it's for
    the oathbreaker's mark. Ritual scarification — a cut on the
    forearm. Not a death sentence. Sets `fugitive.knife_truth`.
  - Haneth alone (separate choice, gated by `fugitive.knife_truth`)
    tells the outrider story — a man who took the mark himself, and it
    meant something different. She doesn't frame it as a solution. The
    PC has to connect the dots. Sets `fugitive.loophole`.
  - Reska, if talked to, is a neo-traditionalist. Back home, the
    Kesharat are encroaching on Tashkari territory — not with violence
    but with schools, clinics, infrastructure. Some of her generation
    have sided with the Kesharat. Reska's faction is retreating
    aggressively into the old ways to preserve cultural identity. She's
    here because this case is a test: can the clan enforce its own
    justice, or is that another thing the old ways can't do anymore?
    She wants blood and pageantry more than closure. The self-given
    mark (best outcome) frustrates her — a quiet act of personal
    atonement isn't the public reckoning she needs to take home.
- **Communicate:** These are people at the end of a two-year search.
  They are not villains. Daveth's anger is compacted and patient.
  Haneth wants to go home. Reska wants something she can't name.
- Returns to hub.

### 4. Maren (wing)
- **Location:** Maren's cabin. She's sitting with the knife.
- She knew immediately what the knife was for. She's known for twelve
  years that this was owed.
- **Maren's state:** She is doom-spiraling. Not contemplating the
  knife, not weighing options — she's already past the crisis and
  mentally planning life as a forest hermit. Which roots are edible,
  how far from camp she'd need to go, whether she can build a shelter
  before the cold sets in. She is not engaging with the situation
  outside because she's focused entirely on how awful she is, and she
  thinks that focus is honest and noble. It is neither. It's the same
  pattern that caused the original crime: she feels trapped, locks up,
  and can only see ways things get worse. Twelve years ago she panicked
  and ran. Tonight she's panicking and freezing. Different expression,
  same dysfunction. She's mistaking her spiral for accountability.
- **Key beats:**
  - She'll tell the full story of the crime if asked. Bad trade,
    panic, cascading mistakes, flight. Presented flatly, without
    self-pity. She does not ask to be saved.
  - She knows the camp probably thinks the knife is for killing. She
    could go outside and correct this in thirty seconds. She won't,
    because acting on behalf of the camp would mean claiming a role
    she's decided she doesn't deserve anymore. This feels principled
    to her. It's actually indulgent — she's prioritizing her own
    guilt narrative over the safety of thirty people.
  - If the PC pushes back — not a pep talk, but pattern recognition,
    "you've been here before, last time you saw no way out and ran,
    this time you see no way out and you're sitting still, the common
    factor isn't the situation" — she can be moved. Two paths:
    - **Without loophole:** Hard negotiation check. Pure force of
      argument. The PC has to break the spiral with nothing to offer
      except the observation that freezing is just running in place.
    - **With loophole** (`fugitive.loophole` set): Medium negotiation
      check. The PC has something concrete — a way to answer for what
      she did without abandoning what she's built. The argument has a
      shape Maren can hold onto, which is what she needs to break out
      of the doom loop.
    - Sets `fugitive.maren_resolve` on success.
  - She confirms what the knife is if asked (alternate path to
    `fugitive.knife_truth`).
- **Communicate:** The PC's job is not to tell Maren she's a good
  person. It's to show her that "sitting here accepting my fate" is
  the same impulse as "taking the money and running" — she's
  abandoning people who depend on her because the alternative
  requires her to act under pressure, and she locks up under pressure.
  The nobility she thinks she's performing is actually the flaw
  repeating.
- Returns to hub.

---

## ACT 3 — The Crisis (branching terminal)

### 5. Dawn (terminal, branching)
- **Location:** The camp clearing, first light.
- Daveth stands. "It's time."
- Outcome is determined by three tags:
  - `fugitive.rumor_corrected` — PC learned the truth AND told the camp
  - `fugitive.maren_resolve` — PC convinced Maren to stand up
  - `fugitive.loophole` — PC learned about the self-given mark

**Branches:**

- **Chaos** (no `rumor_corrected`): The camp attacks the outlanders.
  Gault is the spark. Maren walks away. No winners. This is the
  default — any path where the suicide rumor stays live ends here,
  regardless of what else the PC has done.
  `+finish_dungeon`

- **Daveth does the mark** (`rumor_corrected`, no `maren_resolve`):
  Maren submits. Daveth performs the rite. Technically justice. The
  camp watches their leader branded by an outsider. She stays,
  diminished.
  `+finish_dungeon`

- **Unified refusal** (`rumor_corrected`, `maren_resolve`, no
  `loophole`): Maren refuses to go. The camp backs her. Daveth
  leaves. The clan's wound stays open. Maren is stronger. The
  outlanders carry grief she could have answered.
  `+finish_dungeon`

- **Maren takes the knife** (`rumor_corrected`, `maren_resolve`,
  `loophole`): Best outcome. Maren speaks her full name, tells the
  truth, cuts the mark herself. Everyone gets something. Nobody gets
  everything. The cost is real.
  `+finish_dungeon`

- **Walk away** (available throughout): PC leaves. Without
  intervention, chaos is the likely result. Brief coda — the argument
  fading behind them.
  `+flee_dungeon`

---

## Tag Inventory

| Tag | Set by | Used by |
|-----|--------|---------|
| `fugitive.knife_truth` | The Outlanders (Daveth/Haneth) or Maren | The Camp (gates rumor correction choice), The Outlanders (gates Haneth loophole) |
| `fugitive.rumor_corrected` | The Camp (self-link, requires knife_truth) | The Camp (unlocks Edda/Silla, deflates Gault), Dawn (gates non-chaos outcomes) |
| `fugitive.maren_resolve` | Maren (negotiation check) | Dawn (gates refusal/best outcomes) |
| `fugitive.loophole` | The Outlanders (Haneth, requires knife_truth) | Maren (lowers convince difficulty), Dawn (gates best outcome) |
| `fugitive.talked_edda` | The Camp (self-link, requires rumor_corrected) | Dawn (flavor) |
| `fugitive.talked_silla` | The Camp (self-link, requires rumor_corrected) | Dawn (flavor) |
| `fugitive.saw_gault` | The Camp (self-link) | Dawn (chaos flavor) |
| `fugitive.night_passed` | Any wing visit | The Camp (gates Dawn choice) |

---

## File Inventory

| File | Type | Scene |
|------|------|-------|
| Start.enc | Entry | The trail + arrival + confrontation |
| The Camp.enc | Hub | Nighttime camp, faction conversations |
| The Outlanders.enc | Wing | Daveth, Haneth, Reska conversations |
| Maren.enc | Wing | Maren's cabin, the crime, the knife |
| Dawn.enc | Terminal | Morning resolution, all branches |
