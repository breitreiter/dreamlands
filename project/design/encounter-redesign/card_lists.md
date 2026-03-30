# Card Lists by Skill

Complete card-source inventory for each encounter skill. Every item that contributes a skill modifier is a card source. Skill levels 1-4 also contribute cards. Tokens contribute 1 card each.

Archetype notation: see card_archetypes.md for cost/effect definitions.

---

## Combat (Fight encounters)

### Combat skill (4 cards)

- Combat 1: "Zornhau - wrathful strike" = momentum_to_progress
- Combat 2: "Nachdrängen - take initiative" = spirits_to_momentum
- Combat 3: "Überlaufen - fearless strike" = threat_to_progress
- Combat 4: "Scheitelhau - inevitable end" = momentum_to_progress_large

### Daggers (Combat +1, all contribute 1 card)

Design: quick, efficient, low-ceiling. Good at free chips, bad at big momentum conversions.

- **bodkin** (plains T1, 15gp) — "Stab desperately" = momentum_to_progress
- **skinning_knife** (swamp T1, 15gp) — "Slash wildly with your knife" = momentum_to_progress
- **jambiya** (scrub T1, 15gp) — "Strike with your jambiya" = momentum_to_progress
- **seax** (mountains T2, 40gp) — "Hack at their defense" = spirits_to_momentum
- **kukri** (scrub T2, 40gp) — "Stab deep and twist" = momentum_to_cancel
- **hunting_knife** (mountains T2, 80gp) — "Stab under their guard" = momentum_to_progress
- **the_old_tooth** (unique) — "Wish upon the Old Tooth" = free_cancel

NOTE: All daggers are Combat +1, so they only contribute 1 card each. T1 and T2 daggers give momentum_to_progress or momentum_to_cancel (basic utility). The unique gives free_cancel. Daggers never offer huge progress — they're tools, not proper weapons.

### Axes (Combat +1 to +4, each contributes cards = Combat modifier)

Design: big, slow, top-heavy. Strong momentum_to_progress_large and _huge. Weak on control (no cancel). Wind-up dependent.

- **hatchet** (forest T1, 15gp, Combat +1) — 1 card
  - "Bury the hatchet in their guard" = momentum_to_progress

- **tomahawk** (forest T1, 15gp, Combat +2) — 2 cards
  - "Push past their defense and chop" = threat_to_progress
  - "Exploit a gap in their guard" = momentum_to_progress

- **war_axe** (forest T2, 40gp, Combat +2) — 2 cards
  - "Bring the axe down with both hands" = momentum_to_progress_large
  - "Drive the beard of the axe into their defense" = momentum_to_progress

- **broadaxe** (mountains T2, 80gp, Combat +3) — 3 cards
  - "Bring the broadaxe down in a terrible arc" = momentum_to_progress_large
  - "Wind up and put everything behind the swing" = momentum_to_progress_huge
  - "Shove them back with the axe's haft" = free_momentum

- **bardiche** (mountains T2, 80gp, Combat +3) — 3 cards
  - "Swing the bardiche in a wide, committed arc" = momentum_to_progress_large
  - "Drive the axe's spike into their foot" = free_momentum
  - "Brace the shaft and let them come to you" = threat_to_progress_large

- **revathi_labrys** (unique, Combat +4) — 4 cards
  - "Bring the labrys down like a felled tree" = momentum_to_progress_huge
  - "Let the weight carry through in a brutal arc" = momentum_to_progress_large
  - "The ground trembles where the labrys strikes" = free_momentum
  - "Call upon the Revathi to guide your arm" = spirits_to_progress_large

NOTE: Axes have no cancel cards at all. A pure axe fighter powers through threats by racing them, not by neutralizing them. This is a deliberate weakness — pair with armor or a skill-based cancel if you want control.

### Swords (Combat +2 to +5, each contributes cards = Combat modifier)

Design: balanced, the optimal martial weapon. Good at everything, best at nothing. Only swords offer cancel.

- **falchion** (plains T1, 15gp, Combat +2) — 2 cards
  - "Hack at their defense" = spirits_to_momentum
  - "Swing your blade in an arcing chop" = momentum_to_progress

- **short_sword** (plains T2, 40gp, Combat +3) — 3 cards
  - "Probe their defenses" = free_momentum
  - "Thrust your blade through an opening" = momentum_to_progress
  - "Exploit their error" = momentum_to_cancel

- **tulwar** (scrub T2, 40gp, Combat +3) — 3 cards
  - "Hack and slash" = momentum_to_progress
  - "Bring your tulwar down in a brutal chop" = momentum_to_progress_large
  - "Exploit their error" = momentum_to_cancel

- **scimitar** (scrub T2, 80gp, Combat +4) — 4 cards
  - "Hammer their defense with quick slashes" = free_momentum
  - "Feint high, then kick them off balance" = momentum_to_progress
  - "Exploit their error" = momentum_to_cancel
  - "Making a daring attack" = threat_to_progress

- **arming_sword** (plains T2, 80gp, Combat +4) — 4 cards
  - "Thrust, then pull back with a draw cut" = momentum_to_progress
  - "Grasp your blade and hammer with the pommel" = momentum_to_progress_large
  - "Step in close and trip them" = spirits_to_cancel
  - "Bind and control their weapon" = free_momentum

- **shimmering_blade** (unique, Combat +5) — 5 cards
  - "Obscure your strikes" = free_momentum
  - "Lace the air with uncolor" = momentum_to_progress
  - "Burn through iron and sinew" = momentum_to_cancel
  - "Let the Lattice guide your hand" = threat_to_progress_large
  - "Unleash the Lattice from the blade" = free_cancel

### Token: lucky_buckle (Combat +1, 1 card)

- "Trust your luck" = spirits_to_cancel

---

## Cunning (Sneak encounters)

### Cunning skill (4 cards)

Cunning cards should describe physical actions of stealth, misdirection, and evasion.

- Cunning 1: "Steady yourself" = free_momentum
- Cunning 2: "Exploit a distraction" = momentum_to_progress
- Cunning 3: "Find the hidden path" = momentum_to_cancel
- Cunning 4: "Dash directly through" = threat_to_progress_large

### Light armor (Cunning +0 to +5)

Design: light armor enables stealth through not getting in the way. Better light armor actively helps (muffled, dark-colored, flowing). Cards should describe the armor doing its job — staying quiet, staying hidden.

- **tunic** (plains T1, free, Cunning +0) — 0 cards
  (no skill modifier, no cards)

- **silks** (scrub T1, 15gp, Cunning +1) — 1 card
  - "Tread softly" = free_momentum

Note: this assumes waxed_poncho is replaced by hunters_gear

- **hunters_gear** (swamp T1, 15gp, Cunning +2) — 2 cards
  - "Blend with the terrain" = free_momentum
  - "Move while they're not looking" = momentum_to_progress

Note: this assumes traveling_cloak is replaced by cartographers_cloak

- **cartographers_cloak** (mountains T2, 40gp, Cunning +3) — 3 cards
  - "Move with conviction" = free_momentum
  - "Inch forward cautiously" = spirits_to_progress
  - "Slip by unnoticed" = momentum_to_cancel

Note: this assumes embroidered_kaftan is replaced by desert_scout_gear

- **desert_scout_gear** (scrub T2, 80gp, Cunning +4) — 4 cards
  - "Blend with the terrain" = free_momentum
  - "Hurl pocket sand" = momentum_to_progress
  - "Slip by unnoticed" = momentum_to_cancel
  - "Sprint to cover" = threat_to_progress_large

Note: this assumes magisterial_robe is replaced by robe_of_shadows

- **robe_of_twilight** (unique, Cunning +5) — 5 cards
  - "Gather shadows around you" = free_momentum
  - "Glide forward silently" = momentum_to_progress
  - "Cast terrifying shadows" = momentum_to_cancel
  - "Step between shadows" = momentum_to_progress_large
  - "Conjure a shadow beast" = free_cancel

### Medium armor (Cunning +1 to +2)

Design: medium armor gives a small Cunning bonus. These should be modest utility cards — the armor is flexible enough to not ruin your stealth, but it's not helping much.

- **leather** (forest T1, 15gp, Cunning +1) — 1 card
  - "Tread softly" = free_momentum

- **hide_armor** (mountains T1, 15gp, Cunning +1) — 1 card
  - "Blend with the terrain" = free_momentum

- **buff_coat** (forest T2, 40gp, Cunning +1) — 1 card
  - "Tread softly" = free_momentum

- **lamellar** (mountains T2, 80gp, Cunning +2) — 2 cards
  - "Move while they're not looking" = momentum_to_progress
  - "Blend with the terrain" = free_momentum

- **mountain_regiment_armor** (unique, Cunning +2) — 2 cards
  - "Move with uncanny speed" = spirits_to_momentum
  - "Put your faith in the armor" = threat_to_progress_large

NOTE: Medium armor caps at Cunning +2 and never offers cancel or big progress. This feels right — it's not stealth gear, it's armor that doesn't completely blow your cover.

### Heavy armor (Cunning +0)

Heavy armor gives no Cunning modifier. **No cards contributed.** You're loud and visible. If you're sneaking in heavy armor, you're relying entirely on skill cards and chaff.

### Token: tarnished_key (Cunning +1, 1 card)

- "Remember the key's lesson" = free_momentum

---

## Negotiation (Debate encounters)

### Negotiation skill (4 cards)

Negotiation cards should describe rhetorical actions — arguments, emotional appeals, social maneuvers.

- Negotiation 1: "Push back on that" = momentum_to_progress
- Negotiation 2: "Change the subject" = free_momentum
- Negotiation 3: "Appeal to a higher authority" = threat_to_progress
- Negotiation 4: "Present the inevitable conclusion" = momentum_to_progress_huge

### Tools

- **letters_of_introduction** (scrub T1, 40gp, Negotiation +2) — 2 cards
  - "Drop a name" = free_momentum
  - "Mention your patron" = momentum_to_progress

- **peoples_borderlands** (mountains T2, 80gp, Negotiation +3) — 3 cards
  - "Quote the book" = momentum_to_progress
  - "Cite a precedent" = momentum_to_progress_large
  - "Show you understand their ways" = free_momentum

NOTE: Both negotiation tools are knowledge/social capital items. The cards work as rhetorical techniques (you have research, connections, or context to draw on). This maps well enough — "I read about your people" or "My patron sent me" are real debate moves.

### Token: ivory_comb (Negotiation +1, 1 card)

- "Listen to the ghostly whispers" = spirits_to_momentum

---

## Bushcraft (Navigate encounters)

### Bushcraft skill (4 cards)

Bushcraft cards should describe physical actions of pathfinding, endurance, and terrain reading.

- Bushcraft 1: "Pick a path" = free_progress_small
- Bushcraft 2: "Read the terrain" = free_momentum
- Bushcraft 3: "Find your footing" = momentum_to_progress_large
- Bushcraft 4: "Push through it" = momentum_to_progress_huge

### Tools

Note: this assumes flora_monograph is replaced by cartographers_diary

- **cartographers_diary** (mountains T1, 40gp, Bushcraft +2) — 2 cards
  - "Recall a story about this place" = momentum_to_progress
  - "Check the diary" = free_momentum

- **ornate_spyglass** (scrub T2, 80gp, Bushcraft +3) — 3 cards
  - "Scout ahead" = free_momentum
  - "Spot the path" = momentum_to_progress
  - "Glass the danger" = momentum_to_cancel

### Token: knotwork_seed (Bushcraft +1, 1 card)

- "Trust the seed" = momentum_to_progress_large

---
