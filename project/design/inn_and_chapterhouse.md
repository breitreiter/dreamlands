## The Chapterhouse

The Chapterhouse replaces the inn in Aldgate. There is only one.

The Chapterhouse costs no gold.

Using the Chapterhouse clears all conditions. The Chapterhouse does not disqualify you for any condition.

The Chapterhouse otherwise function the same as an inn, except you are only quoted time cost.

### Description

The lobby silently attests the Merchant Guild's might. Marble floors veined with gold, walls hung with maps of trade routes that span three continents, and above it all a vaulted dome painted with a rising sun. The air smells of woodsmoke and roasting meat. The polished oak desk at the center of the lobby is manned by a wizened guild clerk who looks up from his ledger. "Welcome colleague. How long will you be staying with us?"

## Inns

Inns are largely a QoL feature.

They exist in all settlements.

They cannot be used if:
- You have any conditions which require medicine/bandages to treat AND
- You do not have enough medicine/bandages on-hand to treat all current conditions

This is because serious conditions damage health daily. The inn recovers 1 health daily. Your stay would be infinite (or you would die in the inn).

The inn has two modes:
- Spend one night
- Spend enough nights to heal to full

The inn has two pricing structures:
- One night - free
- Multiple nights - each night at an inn costs the same as buying 3 food

Using the inn:
- Checks if you have disqualifying conditions
- If you do, warns you to seek medicine immediately
- Checks the lower of your Health or Spirits
- Calculates the number of nights required to restore you fully (1 health + 1 spirits per day)
- Presents you with a quote of time + money for full recovery
- Presents you with an alternate option to only stay one night

Depending on your choice:
- If you approve the full recovery quote: in a single step, recovers you fully and advances time
- If you stay only one night: perform normal end-of-day

## End of Day Speedrun

Players will be seeing this screen A LOT. 

The contents are extremely important. Bad/noisy presentation will lead to players missing core game mechanics and being surprised or frustrated. Extra choices add clicks.

To support this, big changes:
- No more condition resist consumables/medicines
- Auto food/medicine consumption
- Simplified, one-step condition acquisition resolution

We can lean on the vignette to set the mood, so we can keep descriptive text to a bare minimum.

Items in [brackets] represent a placeholder for UI components or data.

### Screen structure

#### Title
- Night on the Road
- The Chapterhouse
- A Quiet Inn

No date, no region info, no settlement name

### Intro
If you can rest and you are outside a settlement:
- It is dark, you make camp.
- Night falls, you set up camp.
- Another night, another campsite.
- The day is done, you make camp.

If you are inside a settlement or using an inn explicitly:
- The interior of the inn is warm and inviting.

If you are inside Aldgate or using the Chapterhous explicitly:
- The lobby silently attests the Merchant Guild's might. Marble floors veined with gold, walls hung with maps of trade routes that span three continents, and above it all a vaulted dome painted with a rising sun. The air smells of woodsmoke and roasting meat.

If you are unable to rest (encounter-triggered time skip):
- A night passed, though you found no rest.

### Offer Building-specifc Services

#### Chapterhouse

"Food and lodging is free for guild members in good standing. An on-site physician will tend to any injuries or illnesses you've encountered on the road."
[Recover in the Chapterhouse] [Cancel]

#### Inn

"You may stay one night at no charge, though you'll need to buy your own food in the market."
[Spend One Night]

- If the player has enough medicine on hand to treat all stacks of each of their medicine-curable conditions
    - You may rest here until fully recovered. Given your current state, that will cost you [days * 9]gp.
    - [Fully Recover]
- Otherwise
    - For each condition which the player does not have enough medicine:
        - You don't have enough [medicine] to cure your [condition].
    - You'll only get worse if you stay here. You need to find treatment instead.
    - We still allow the PC to spend one night, though it's probably not a great idea.

### Resolve acquired conditions and telegraph
If the player is on the road, for each at-risk condition, silently check resist. DO NOT apply new conditions yet.

### Consume supplies
No supplies are consumed in the Chapterhouse.

**Food** Always try to optimize for the best outcome. The downside is this cheats players who are trying to optimize for some sort of weird starvation edging strategy. But given how often this screen is used, it's better to just speedrun this.
- You eat a full and balanced meal. You feel refreshed.
- You eat a full meal.
- You eat what little food you have.
- You have no food to eat.

**Medicine** Review existing conditions. If the player has cure medicine for that condition, remove 1 instance of that medicine from their inventory. Note treatment has been applied:
- You consume [medicine] for your [condition]

### Rest Recovery
+1 health, +1 spirits base
+1 health, +1 spirits with a full meal

### Report Condition Changes

If the PC consumed a full meal (3 of any food) treat that as curing medicine for hunger. Do not show a "You consumed food for your Hungry" message, just do this silently. If the PC consumed anything other than a full meal, show the Ongoing message as normal for an untreated condition.

project/design/conditions_list.md

For each condition the PC started end-of-day with:
- If no curing medicine was consumed, show the Ongoing message. 
- If curing medicine was consumed
    - If the PC failed a resist check for THAT EXACT CONDITION, show the HealFailure message.
    - Otherwise remove 1 stack then
        - If no stacks remain, show the HealComplete message
        - If stacks remains, show the HealProgress message

Then, for each condition the PC was at-risk of acquiring and does not currently have:
- If the player resists the condition, show the Resist message. Do not show a resist message for Hungry, Lost, or Exhausted.
- If the player succumbs to the condition, show the Succumb message. Apply condition stacks. Show mechanical penalty for condition.

Finally, resolve Disheartened with the PC's final updated Spirits.
- If the PC has the Disheartened condition
    - If the final value of Spirits was above 9, remove the condition and show HealComplete
    - Otherwise, show Ongoing
- Otherwise if the final value of Spirits was below 10, add the condition and show Succumb



