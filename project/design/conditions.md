# Conditions System — Design Inventory

## Overview

Conditions are the primary damage vector in the game. Rather than encounters directly reducing Health or Spirits (which is reserved for rare, dramatic moments), most fictional consequences manifest as **conditions** — persistent debuffs that drain stats nightly during rest resolution.

Each condition is **binary** (on/off). A player either has Diseased or they don't. Multiple *different* conditions stack freely — a player can be Diseased, Exhausted, and Cold simultaneously — but acquiring the same condition twice has no additional effect.

---

## Stats Reference

| Stat | Represents | Damaged By |
|------|-----------|------------|
| **Health** | Physical wellbeing, bodily integrity | Physical conditions + rare direct misfortune + combat |
| **Spirits** | Mental wellbeing, will to continue | Exhaustion + direct supernatural terror |

### Drain Rate Philosophy

Condition severity scales with distance tier. Early-game conditions are **gentle and well-signposted** — low drain, generous foreshadowing, obvious cures. Late-game conditions are **punishing and opaque** — higher drain, cryptic foreshadowing, specialized cures.

| Tier | Drain Rate | Foreshadowing | Example |
|------|-----------|---------------|---------|
| **Tier 1 (Safe Zone)** | Low | Generous, obvious | "The swamp air feels thick and wrong." |
| **Tier 2 (Frontier)** | Moderate | Clear but less hand-holding | "You wake slick with sweat despite the cold." |
| **Tier 3 (Deep Wild)** | High | Cryptic or absent | "Your skin is... warm. Too warm." |

This means early-game conditions teach the system (you learn what Diseased means, how Medicine works) while late-game conditions test mastery (you've got Mummy Rot — figure it out).

---

## Condition Inventory

### Physical Conditions (Drain Health)

#### Diseased
> *Fever, chills, something foul in the blood.*

| | |
|-|-|
| **Drains** | Health |
| **Sources** | Swamp/jungle biome exposure, contaminated water, encounter results |
| **Resisted by** | Mosquito Netting (environmental), Medicine (prophylactic use?) |
| **Cured by** | Medicine (consumed) |
| **Variants** | See *Disease Variants* below — only one active at a time |

#### Injured
> *A wound that needs tending before it gets worse.*

| | |
|-|-|
| **Drains** | Health |
| **Sources** | Misfortune events, equipment failures, animal attacks, environmental hazards |
| **Resisted by** | Nothing — injury is reactive, not preventable through gear |
| **Cured by** | Bandages (consumed) |
| **Notes** | Injury comes from misfortune, not combat. Combat deals direct Health damage. You can't "resist" bad luck, only patch yourself up after. |

#### Thirsty
> *The land itself drinks you dry.*

| | |
|-|-|
| **Drains** | Health |
| **Sources** | Entering/remaining in Scrub biome without protection |
| **Resisted by** | Canteen (equipment — passive, not consumed?) |
| **Cured by** | Departing the Scrub biome |
| **Notes** | Environmental condition — automatically acquired, automatically cleared. This makes the Scrub a slow-drain hazard rather than a hard barrier. |

#### Cold
> *The mountain air cuts through everything.*

| | |
|-|-|
| **Drains** | Health |
| **Sources** | Entering/remaining in Mountain biome without protection |
| **Resisted by** | Warm Clothing (equipment — passive) |
| **Cured by** | Departing the Mountain biome |
| **Notes** | Mirror of Thirsty — mountains tax the unprepared. |

---

### Mental Conditions (Drain Spirits)

#### Exhausted
> *Every step costs more than the last.*

| | |
|-|-|
| **Drains** | Spirits |
| **Sources** | Extended travel without rest, forced marches, overloaded inventory? |
| **Resisted by** | Quality Boots (equipment — passive), Balanced Meal (food triad — during rest) |
| **Cured by** | Resting at a settlement |
| **Notes** | The most common Spirits drain and likely the first condition new players encounter. Punishes players who push too far between towns. Good Boots + Balanced Meal (the food triad) should make Tier 1 exhaustion trivially ignorable for mid-game players. This is the first incentive to pack properly — three categories of food, not just the cheapest thing available. |

> **Note on Spirits damage:** Supernatural terrors (Deep Wild encounters, cursed places) deal **direct damage to Spirits** rather than applying a condition. This keeps the supernatural rare and frightening — there's no "Shaken" condition to manage or resist, just a hit that lands. You don't prepare for the supernatural; you survive it or you don't.

---

### Candidates / Open Questions

#### Hungry
> *Your pack is lighter than it should be.*

| | |
|-|-|
| **Drains** | Spirits |
| **Sources** | Having 0 food at rest resolution |
| **Resisted by** | N/A (you either have food or you don't) |
| **Cured by** | Eating (consuming any food) |
| **Notes** | Applied automatically when the player has nothing to eat at rest. Drains Spirits — hunger is demoralizing before it's physically dangerous. This stacks with the implicit punishment of losing all meal bonuses and food-based prophylactics. A hungry player is vulnerable to *everything*: no triad bonus, no special food resistances, and a Spirits drain on top. The condition clears the moment they eat. |

#### Lost
> *The trail ended two days ago.*

| | |
|-|-|
| **Drains** | Spirits |
| **Sources** | Deep Wild exploration, bad encounter outcomes |
| **Resisted by** | Map / Compass (equipment) |
| **Cured by** | Reaching a known landmark or settlement |
| **Notes** | On a node-based map, "lost" might mean forced random movement rather than a draining condition. Could work as a Tier 3 mechanic where the map itself becomes unreliable. |

#### Tier 2 Biome-Specific Conditions

Each Tier 2 biome introduces its own signature condition requiring targeted preparation.

##### Infested (Forest)
> *Something burrowed in during the night. You can feel it moving.*

| | |
|-|-|
| **Drains** | Health |
| **Sources** | Sleeping in Tier 2 forest without protection |
| **Resisted by** | ??? (Treated bedroll? Herbal repellent? Specific food ingredient?) |
| **Cured by** | Medicine (consumed) — mechanically a disease variant (Evil Parasites) |
| **Notes** | Occupies the disease slot. Ticks, leeches, burrowing things — the forest gets inside you. Foreshadowing should be skin-crawling: "You feel something on your ankle. Then your neck. Then everywhere." |

##### Haunted (Plains)
> *You keep seeing them out of the corner of your eye. Or maybe you just can't stop thinking about what happened here.*

| | |
|-|-|
| **Drains** | Spirits |
| **Sources** | Sleeping in the Tier 2 plains (ancient battlefields) |
| **Resisted by** | Warding Talisman (equipment — purchased from settlements with temples) |
| **Cured by** | Visiting a temple in a settlement |
| **Notes** | Deliberately ambiguous whether this is literal ghosts or psychological trauma from witnessing mass death — and the game never clarifies. Foreshadowing should support both readings: "Shapes in the mist that might be men. The wind sounds almost like voices." / "You can't stop counting the bones." The talisman might ward off spirits, or it might just be a comforting ritual object. The temple might perform a cleansing rite, or you might just need to sit somewhere quiet and talk to someone kind. Both are true. Neither is confirmed. |

**Design note on Haunted:** This is the second Spirits-draining condition alongside Exhausted, but the sources and cures are completely different. Exhausted is physical — your body is tired, rest at a town. Haunted is environmental/psychological/supernatural — the place itself is wrong, and the cure involves spiritual cleansing at a temple. This gives Spirits a richer threat landscape than just "walked too far."

The resist/cure pair (Talisman / Temple) also creates interesting settlement economics. Not every settlement has a temple, so players need to know where they are — and settlements near the plains have a unique trade good to sell (talismans). This gives the plains a regional economic identity beyond just being dangerous to cross.

| Biome | Condition | Drains | Resist | Cure |
|-------|-----------|--------|--------|------|
| Swamp | Diseased (Swamp Fever?) | Health | Mosquito Netting | Medicine |
| Mountains | Cold | Health | Warm Clothing | Depart biome |
| Scrub | Thirsty | Health | Canteen | Depart biome |
| Forest | Infested (Evil Parasites) | Health | ??? | Medicine |
| Plains | Haunted | Spirits | Warding Talisman | Temple visit |

#### Tier 3 Exotic Conditions (TBD)

These should be rare, punishing, and surprising. Players encountering them for the first time should not immediately know the cure. Examples for flavor — actual list depends on Deep Wild encounter design:

- **Mummy Rot** — Health drain, high severity, cure unclear
- **Radiation Sickness** — Health drain, very high severity, exotic cure
- **Petrification** — slow-acting, countdown to game over if uncured
- **??? Haunting variant** — if the plains ghosts are melancholy, Deep Wild spirits could be *angry*. But this might overlap with direct Spirits damage from supernatural encounters.

---

## Disease Variants

**Diseased** is the mechanical condition (one slot, drains Health, cured by Medicine). The *specific* disease is flavor attached to that slot — but the variant determines **drain severity and foreshadowing clarity** based on the tier where it was acquired.

**No Tier 1 diseases.** This is a deliberate design choice to prevent slot-locking abuse. If mild diseases existed, savvy players would deliberately contract them in safe areas to occupy the disease slot, immunizing themselves against worse diseases in dangerous territory. ("I'm going into the Halls of Pestilence — better catch the Pnaketen Sniffles first.") Instead, disease is a Tier 2+ threat that players encounter only when they're already committed to risky territory.

### Tier 2 Diseases — Moderate, Targeted
| Variant | Source | Foreshadowing | Drain |
|---------|--------|---------------|-------|
| Evil Parasites | Forest biome | (see Forest condition below) | Moderate |
| Rot Lung | Ruins, underground | "You wake slick with sweat, coughing dust." | Moderate |
| River Flux | Contaminated water | "Your stomach turns. Something in the water." | Moderate |

Players need to have learned the system by now. Specific food ingredients help resist. Medicine cures.

### Tier 3 Diseases — Punishing, Cryptic
| Variant | Source | Foreshadowing | Drain |
|---------|--------|---------------|-------|
| Mummy Rot | Deep Wild tombs | "Your skin is... warm. Too warm." | High |
| ??? | ??? | ??? | High |

Cryptic foreshadowing. Standard Medicine may or may not work. This is the "good luck, bro" tier.

---

## Resistance vs. Cure — Design Pattern

The system creates a clean **preparation loop**:

```
RESIST = "I brought the right gear, so I don't get the condition"
CURE   = "I got the condition, but I brought supplies to fix it"
```

This gives the player three states per hazard:

1. **Prepared (resist item):** Condition never applies. Smart packing rewarded.
2. **Partially prepared (cure item):** Condition applies but can be resolved. Costs a consumable.
3. **Unprepared:** Condition applies and persists. Must retreat or endure the drain.

### Resist/Cure Item Mapping

| Condition | Resist (Equipment/Food) | Cure (Consumable/Action) |
|-----------|------------------------|--------------------------|
| Hungry | *None — you either have food or you don't* | Eating (any food) |
| Diseased | Mosquito Netting (equip), Prophylactic foods (special) | Medicine |
| Injured | *None — bad luck can't be resisted* | Bandages |
| Thirsty | Canteen (equip) | Depart Scrub biome |
| Cold | Warm Clothing (equip), Ember Spice (food) | Depart Mountain biome |
| Exhausted | Quality Boots (equip), Balanced Meal (triad), Stillwater Biscuit (food) | Rest at settlement |
| Haunted | Warding Talisman (equip) | Temple visit at settlement |

### Food as Resistance — The Triad and Prophylactics

Food operates on two layers: the **triad** (category coverage) and **prophylactics** (special foods with condition resistance).



**Prophylactics:** Special foods belong to a category (they count toward the triad) but also provide a weak resist bonus (+2) against a specific condition. They don't cure — they improve your odds of not contracting something overnight. Think garlic soup, not penicillin.

| Special Food | Category | Prophylactic Against | Availability |
|-------------|----------|---------------------|-------------|
| Jorgo Root | Carbs | Swamp Fever | Swamp-adjacent settlements |
| Pine Resin Tea | Sweets | Rot Lung | Mountain settlements |
| Ember Spice | Sweets | Cold | Mountain settlements |
| Smoked Marsh Eel | Protein | River Flux | Swamp-adjacent settlements |
| Witchwood Bark | Carbs | Infested | Forest settlements |
| Stillwater Biscuit | Carbs | Exhausted | Common |

**Foraging:** Bushcraft check, biome-modified. You get 1 random-category unit per success, up to 2 attempts per day. Forests are generous; scrub is barren. Because category is random, foraging alone can't guarantee a balanced meal — it's survival food, not journey prep. The triad rewards players who *buy* provisions before departing.

**Tier 1 areas:** Biome conditions (Cold, Thirsty) are completely trivializable with mid-game gear. Exhaustion handled by Balanced Meal + Boots. No diseases at this tier. A prepared player barely notices.

**Tier 2 areas:** Each biome introduces a specific condition that requires targeted preparation. The packing problem starts to matter — you need the right prophylactic food *and* the right gear for this route. A player heading into the swamp should stock Jorgo Root; heading into the mountains, Ember Spice.

**Tier 3 areas:** Dangerous, exotic conditions. Specialized ingredients required. Foreshadowing is cryptic. The packing problem is severe — you can't carry everything, so you're making bets about what you'll face.

---

## Nightly Resolution Flow

Rest resolution is a multi-phase sequence where the order matters — food can prevent conditions that would otherwise apply, and damage from existing conditions lands before new ones are checked.

1. **Secret condition check** — evaluate the tile where the player is sleeping for potential new conditions (biome, distance tier, etc.). Result is held, not yet applied.
2. **Foreshadow & remind** — hint at environmental threats ("the air bites through your cloak"), remind the player of active conditions ("your wound still throbs").
3. **Prep meal** — player consumes 3 food units. Category spread determines meal quality: balanced (1 protein + 1 carb + 1 sweet) grants +1 Health/+1 Spirits; partial or monotone meals grant nothing; 0 food applies the Hungry condition. Special prophylactic foods also apply their resist bonus here.
4. **Consume medicine** — player uses cure items (bandages, medicine, etc.) if available.
5. **Sleep & heal** — base healing applied to Health and/or Spirits.
6. **Mark damage from conditions** — existing conditions drain their target stats. This happens *after* healing, so conditions erode recovery rather than stacking on top of yesterday's damage.
7. **Apply food resistances** — check whether the meal consumed provides resistance to any of the conditions flagged in step 1.
8. **Re-check for new conditions** — re-evaluate step 1's results, now adjusted for food resistances. Conditions that were resisted by the meal are discarded.
9. **Apply & notify** — any surviving new conditions are applied. Player is informed of what they've acquired.

### Key Design Implications

- **Food operates on two axes.** The triad (category coverage) gives a mild stat bonus for planning ahead. Prophylactic special foods give targeted condition resistance. Both reward thoughtful provisioning — do you spend more on Ember Spice for the mountains, or save gold and risk the cold?
- **Medicine is consumed before damage**, so a player who uses their last bandage tonight heals the Injured condition before it drains Health this rest cycle.
- **Damage lands after healing**, which means conditions slow your recovery rather than creating a death spiral. A player with one condition can still tread water; two or more and they're losing ground.
- **The two-pass condition check** (steps 1 and 8) means the player doesn't know exactly what threats they dodged. They ate a warm meal and slept fine — was that skill or luck? This supports the "stories to tell" philosophy.

### Foreshadowing as Teaching Tool

Discovery is part of the challenge — players don't get a tooltip saying "this biome causes Cold." Instead, the foreshadowing step during nightly resolution does double duty:

- **If the condition applies:** The foreshadowing is the first signal something went wrong. "The mountain air cuts through your blanket."
- **If the condition was resisted:** The foreshadowing *still fires*, so the player knows the threat existed. "The mountain air bites, but your heavy cloak holds." They learn what could have happened and how to prep for it.
- **If the player was never at risk:** No foreshadowing. Silence is information too.

This means even lucky players build knowledge over time. The exception is Tier 3 — foreshadowing is cryptic enough that first exposure is genuinely surprising. "Radiation poisoning, huh? Haven't seen that before." The community discussion about what to pack for the Deep Wild is a feature, not a bug.

---

## Remaining Open Questions

1. **Specific drain values:** The tier model defines relative severity (low/moderate/high) but not actual numbers. What's the Health/Spirits scale — 10? 20? 100? Drain values depend on this.
2. **Tier 3 disease cures:** Does standard Medicine work on exotic diseases, or do they require specialized cures? If specialized, this creates a "you didn't know to pack the right thing" problem that's either thrilling or frustrating depending on how findable the cures are.
3. **Infested resist item:** What keeps forest parasites out? Treated bedroll? Herbal repellent? Smoke? This determines what forest-adjacent settlements stock.
4. **Temple distribution:** How common are temples? If every settlement has one, Haunted is easy to cure. If only a few do, players need to plan routes around temple access — which makes the plains genuinely threatening rather than just annoying.
5. **Exhaustion sources beyond travel:** Is overloaded inventory a source? Forced marches? Or is Exhausted purely a function of distance from last settlement rest?
6. **Hungry as condition vs. implicit punishment:** ~~Resolved.~~ Both. Running out of food means no triad bonus AND no prophylactics AND the Hungry condition (drains Spirits). Triple punishment — being hungry is bad. The condition clears the moment you eat anything.
7. **Swamp at Tier 2:** Is Swamp Fever a Tier 2 disease, or does swamp operate differently? If there are no Tier 1 diseases, swamp in the Safe Zone would only cause... what? Just the environmental unpleasantness without mechanical consequence until Tier 2?