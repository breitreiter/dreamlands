# Skill Check System

## Core Randomizer

Roll **1d20**, add modifiers, compare to **Difficulty Class (DC)**. Result ≥ DC is a pass. Result < DC is a fail. Outcomes are binary — pass/fail with no partial success.

**Natural 1** always fails regardless of modifiers. **Natural 20** always succeeds regardless of DC. Both provide 5% floor/ceiling on every check.

## Difficulty Classes

DCs are properties of the world, not the character. A hard lock is hard for everyone.

| Difficulty | DC  |
|------------|-----|
| trivial    | 5   |
| easy       | 8   |
| medium     | 12  |
| hard       | 15  |
| very_hard  | 18  |
| epic       | 22  |

## Mutator Split

Modifiers come from three independent sources that serve different design purposes.

### Skill Bonus (identity)

Set at character creation. Never changes. Represents who the character is. This is a modest, permanent thumbprint — not the primary driver of success.

| Skill Ranking | Bonus |
|---------------|-------|
| inept         | -2    |
| untrained     | +0    |
| trained       | +2    |
| expert        | +4    |

### Gear/Consumable Bonus (progression)

Acquired through play. Equipped or expended. This is where character growth lives. Finding better gear is the level-up moment. Burning a rare consumable on a hard check is a meaningful resource decision.

| Gear Quality | Bonus |
|--------------|-------|
| poor         | +1    |
| standard     | +2    |
| fine         | +4    |
| superior     | +6    |


### Advantage/Disadvantage (situational)

Sometimes effects grant advantage or disadvantage to a roll.

- **Advantage:** Roll 2d20, keep the higher result.
- **Disadvantage:** Roll 2d20, keep the lower result.
- **Non-stacking:** Multiple sources of advantage/disadvantage do not compound. You either have it or you don't.
- Advantage and disadvantage cancel each other out if both apply.

## Check Type Ownership
Total bonuses must never sum to greater than +10. That means checks must be one of:
- Stat + Gear + token
- Stat + Consumable + token
- Big gear + small gear + token

The following items are "big gear" and can scale up to +5:
- Weapon
- Armor
- Boots

The following items are "small gear" and can scale up to +3:
- Pack held bonus items

Critically, that means there can only be one +3 small gear for any situation and one +2 small gear for the same situation.

The following items are "tokens" can can scale up to +1:
- Haversack small bonus items

### Equipment and consumables
Items should clearly state the circumstances under which they provide bonuses
- Combat: "+X to Combat"
- Encounter: "+X in Negotiation Encounters"
- Foraging: "+X when foraging"
- Conditions: "+X to resist ConditionName"

### Encounter Checks
- Combat: Add weapon bonus + token
- Negotiation: Add two highest small gear bonuses + token
- Bushcraft: Add two highest small gear bonuses + token
- Cunning: Add armor bonus + token
- Mercantile: Add two highest small gear bonuses + token

### Injury resist
A side effect of every combat is the risk of gaining the Injured condition. Mechanics:
- Combat + Armor bonus + token vs Medium DC
- Advantage if you won the combat
- Disadvantage if you lost the combat

### Foraging
- Bushcraft + weapon bonus + token

### Freezing, Thirsty resist
- Bushcraft + two highest small gear bonuses + token

### Hungry resist
- N/A - alternate system based entirely on food volume consumed

### Swamp Fever, Gut Worms, Irradiated resist
- just-eaten (nightly maintenance) consumable bonus (big) + single highest pack-held equipment bonus (small) + bonus

### Poison resist
- Bushcraft + armor bonus + token

### Exhausted resist
- Boots bonus (big) + single highest pack-held equipment bonus (small) + token

## Check Formula

```
d20 + skill bonus (-2 to +4) + gear bonus (0 to +6) vs DC (5 to 22)
```

With possible advantage or disadvantage on the d20 roll.

**Maximum combined modifier:** +10 (expert skill + superior gear).

## Probability Reference

Approximate success chance (d20 + modifier ≥ DC):

| Modifier | trivial (5) | easy (8) | medium (12) | hard (15) | very_hard (18) | epic (22) |
|----------|-------------|----------|-------------|-----------|----------------|-----------|
| -2       |             |          | 35%         | 20%       | 10%            | 5%*       |
| +0       | 80%         | 65%      | 45%         | 30%       | 15%            | 5%*       |
| +2       | 90%         | 75%      | 55%         | 40%       | 25%            | 5%*       |
| +4       | 95%*        | 85%      | 65%         | 50%       | 35%            | 15%       |
| +6       | 95%*        | 95%*     | 75%         | 60%       | 45%            | 25%       |
| +8       | 95%*        | 95%*     | 85%         | 70%       | 55%            | 35%       |
| +10      | 95%*        | 95%*     | 95%*        | 80%       | 65%            | 45%       |

*Capped at 95% (nat 1 always fails) and floored at 5% (nat 20 always succeeds).

Advantage adds roughly +3.5 on average but the effect is nonlinear — strongest in the middle of the probability curve, weakest at extremes.

## Condition Recovery

Condition recovery is **deterministic, not random.** No skill check is made.

Recovery requires three things:
1. **The right item** (bandage, antidote, remedy, etc.)
2. **Time** to apply it
3. **Opportunity** (a safe enough situation to rest/treat)

If all three are met, the character recovers a stack of the condition. Period.

## Luck

On a failure, roll a percentile (or just a second d20 against a threshold) to see if luck triggers a reroll. Luck magnitude sets the trigger chance.

| Luck | Trigger Chance |
|------|---------------|
| none | 0% |
| low | 5% |
| medium | 10% |
| high | 15% |

Those numbers are deliberately low. At 15% on the high end, your lucky idiot catches a break roughly once every six or seven failures. That's enough to get a "wait, what?" moment a few times per session without devaluing the check system. It also means luck *never helps you when you're already succeeding* — it only matters when things go wrong, which is exactly the fantasy. The lucky idiot doesn't do things well, they just inexplicably don't suffer the consequences.

**Luck rerolls don't chain.** If the reroll also fails, that's it. No rolling until you get lucky. One shot at a second chance.

### Rationale

A skill check asks "can you do this?" Recovery with the right supplies is not a question of ability. The interesting decisions were made before this moment: did you pack bandages, can you afford to stop here, is this location safe enough? Those are preparation and resource management decisions, which is where the game's core tension lives.

Condition **resistance** (avoiding onset) remains a standard skill check: d20 + skill + gear vs DC. The uncertainty is in whether you get sick, not whether a bandage works.

## Design Lineage

This system draws from **5th Edition D&D's bounded accuracy** philosophy: total modifiers are constrained so the d20 roll always matters, and the advantage/disadvantage mechanic provides a second axis of modification that cannot stack into auto-success. The key inversion from 5e is that skill bonuses (identity) are the small, static component while gear bonuses (progression) are the large, dynamic component — reflecting a game about preparation and resourcefulness rather than XP accumulation.
