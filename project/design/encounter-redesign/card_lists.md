# Card Lists by Skill

Complete card-source inventory for each encounter skill. Every item that contributes a skill modifier is a card source. Skill levels 1-4 also contribute cards. Tokens contribute 1 card each.

Archetype notation: see card_archetypes.md for cost/effect definitions.

---

## Combat (Fight encounters)

### Combat skill (4 cards)

No cancels in the skill pool — cancel only comes from weapons and tokens.

- Combat 1: "Zornhau - wrathful strike" = momentum_to_progress
- Combat 2: "Nachdrängen - take initiative" = spirits_to_momentum
- Combat 3: "Überlaufen - fearless strike" = threat_to_progress
- Combat 4: "Scheitelhau - inevitable end" = momentum_to_progress_large

### Weapon types

All three weapon types scale Combat +1 to +5 (5 weapons each). Cards are **additive** — a +3 weapon has the same cards as the +2, plus one new card. All weapons of the same type share the same card pool.

Cancel card names are never shown to the player (overwritten by the encounter's timer counterName at draw time).

### Daggers (Combat +1 to +5, cancel-focused)

Design: the expert's weapon. Find openings, neutralize threats. Cancel cards appear at level 2, making even cheap daggers control-viable. Pairs naturally with cautious approach.

**Dagger pool:**
1. "Lunge forward and stab at their guard" = momentum_to_progress
2. (cancel) = momentum_to_cancel
3. "Circle your opponent, looking for a gap" = free_momentum
4. (cancel) = spirits_to_cancel
5. (cancel) = free_cancel

- **bodkin** (plains T1, 15gp, Combat +1) — 1 card (level 1)
- **jambiya** (scrub T1, 15gp, Combat +2) — 2 cards (levels 1–2)
- **kukri** (scrub T2, 40gp, Combat +3) — 3 cards (levels 1–3)
- **hunting_knife** (mountains T2, 80gp, Combat +4) — 4 cards (levels 1–4)
- **the_old_tooth** (unique, Combat +5) — 5 cards (levels 1–5)

NOTE: A +5 dagger has 3 cancel cards (momentum, spirits, free), 1 progress, and 1 free_momentum. Very control-heavy — 3 of 5 cards neutralize threats.

### Axes (Combat +1 to +5, aggro-focused, zero cancels)

Design: the berserker's weapon. Big momentum dumps, overwhelm before timers fire. No cancels at all — pair with armor or skill-based cancel if you want control. Pairs naturally with aggressive approach.

**Axe pool:**
1. "Swing the axe into their defense" = momentum_to_progress
2. "Shift your grip and ready a heavy swing" = free_momentum
3. "Put your weight behind a brutal chop" = momentum_to_progress_large
4. "Charge forward swinging wildly" = threat_to_progress_large
5. "Bring the axe down with everything you have" = momentum_to_progress_huge

- **hatchet** (forest T1, 15gp, Combat +1) — 1 card (level 1)
- **tomahawk** (forest T1, 15gp, Combat +2) — 2 cards (levels 1–2)
- **war_axe** (forest T2, 40gp, Combat +3) — 3 cards (levels 1–3)
- **broadaxe** (mountains T2, 80gp, Combat +4) — 4 cards (levels 1–4)
- **revathi_labrys** (unique, Combat +5) — 5 cards (levels 1–5)

NOTE: A +5 axe has 3 progress cards (regular, large, huge), 1 threat_to_progress_large, and 1 free_momentum. Pure damage ramp — free_momentum at level 2 fuels the big spends at 3+.

### Swords (Combat +1 to +5, hybrid)

Design: the generalist's weapon. Some progress, some cancel, best at neither. Gets cancel at level 3 (later than dagger's level 2). Works with either approach.

**Sword pool:**
1. "Test their guard with a quick cut" = momentum_to_progress
2. "Feint high and step back to recover" = free_momentum
3. (cancel) = momentum_to_cancel
4. "Commit to a powerful driving thrust" = momentum_to_progress_large
5. (cancel) = free_cancel

- **falchion** (plains T1, 15gp, Combat +1) — 1 card (level 1)
- **short_sword** (plains T1, 15gp, Combat +2) — 2 cards (levels 1–2)
- **tulwar** (scrub T2, 40gp, Combat +3) — 3 cards (levels 1–3)
- **scimitar** (scrub T2, 80gp, Combat +4) — 4 cards (levels 1–4)
- **shimmering_blade** (unique, Combat +5) — 5 cards (levels 1–5)

NOTE: A +5 sword has 2 progress cards, 2 cancel cards, and 1 free_momentum. True hybrid — jack of all trades, master of none.

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
