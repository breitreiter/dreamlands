# Alternate taxonomy/structure

1. Generate a rationale

2. Generate a pair of options, both as:
- Shape
- Threat scale
- Payoff type
- Visibility

# Rationale - why mess with this?
Each rationale has four bullet points:
- the move required to learn more about this 
- the moves available to resolve this. the last one is always available
- special costs
- special rewards

## Path Blocked - you can't continue until you resolve this somehow

### Dangerous/challenging terrain (per-biome/tier)
- examine terrain
- navigate it quickly, navigate it carefully, tough it out
- +time, +exhausted, +injury
- -time

### An unexpected dead end (per-biome/tier)
- examine terrain
- navigate it quickly, navigate it carefully, tough it out
- +time, +lost

### Bandits - Someone who blocks passage and will stab you
- measure their skill
- fight them, trick them, sneak around, pay the price
- +injury, -3 spirits
- random loot bundle

## Imminent Danger - you need to respond to avoid consequences
### Weather
- examine terrain
- find shelter, tough it out, wait it out
- +time, +exhausted

### Costly route
- examine terrain
- navigate it skillfully, tough it out
- +time, +lost

### Someone or something that wants to kill you
- measure their skill
- sneak around, fight them
- +injury, -3 spirits
- random loot bundle

## Pure Opportunity - there is a chance to gain something of value or some other boon

### Dangerous/challenging terrain (per-biome/tier)
- examine terrain
- navigate it quickly, navigate it carefully, tough it out, ignore
- +time, +exhausted, +injury
- random loot bundle

### Explore a structure (per-biome/tier)
- examine terrain
- delve deep, take shelter
- +time
- random loot bundle

### Someone or something that wants to kill you
- measure their skill
- sneak around, fight them, ignore
- +injury, -3 spirits
- random loot bundle

# Option challenge scaling
Random per option. Each option may be 1 step higher or lower than median
- obvious choice, safe
- manageable, can be overcome with prep and skill
- serious, real costs, a risk for even a ready explorer

# Visibility levels
0 - shown only as "Play it by ear"
1 - full description (There are 4 bandits on the road ahead. They are lightly armed.)
2 - expert description (There are 4 bandits on the road ahead. Military gait, disciplined. Deserters. Not an easy fight.)

# Resolution Phases

## Recon
I think we do this step automatically.

### bushcraft
- read the terrain
### combat
- measure their skill

## Decision
It's time to act. Pick your approach.
### combat
- fight them
### bushcraft
- navigate it carefully
- navigate it quickly
### cunning
- sneak around
- trick them
### no stat
- wait it out
- tough it out
- pay the price

## Outcome
See how things turn out.
If things went poorly, you have a chance to blunt the impact.

# Outcomes
What's at stake here? What happens if you choose poorly or things go wrong? What's to be gained from risks?

## Resource
- Time - Move time forward or back (reverse 1 max)
- Spirits - Become more or less optimistic
- Health - Recover or lose
- Money
- Food
- Items
- Story quality
- Status condition

## Loot
Roll 2 times on the table below:
- +2 Spirits
- +1-20 money
- +3 random food
- 1 random purchasable weapon
- 1 random purchasable armor
- 1 random purchasable tool

# Move list

## Examine Terrain
When you take stock of the terrain and what it affords, roll+Bushcraft
- On a miss, you misread the threat, believing it more or less dangerous, you unlock no extra options
- On a mixed, you unlock options, but no threat information
- On a full, you accurately read the threat and unlock all options

## Measure Their Skill
As above, except for combat encounters. Roll+Combat

## Fight Them
When you engage in combat with an enemy, roll+Combat
- On a miss, you suffer the worst of the Rationale outcome and gain no benefits
- On a mixed, you are injured, but receive full benefits
- On a full, you suffer no injury and are fully successful

## Navigate it Carefully
When you cautiously navigate a tricky situation or dangerous terrain, roll+Bushcraft
- On a miss, you suffer the worst of the Rationale outcome and gain no benefits
- On a mixed, time moves forward 4, but you are otherwise successful
- On a full, you are fully successful

## Navigate it Quickly
When you boldly navigate a tricky situation or dangerous terrain, roll+Bushcraft
- On a miss, you suffer the worst of the Rationale outcome and gain no benefits
- On a mixed, you are injured, but otherwise successful
- On a full, you are fully successful

## Sneak Around
When you stealthily evade an enemy, roll+Cunning
- On a miss, you suffer the worst of the Rationale outcome and gain no benefits
- On a mixed, you avoid any negative outcome, but also receive no positive outcome
- On a full, you are fully successful

## Trick Them
When you attempt to distract or deceive an enemy, roll+Cunning
- On a miss, you suffer the worst of the Rationale outcome and gain no benefits
- On a mixed, you are injured, but otherwise successful
- On a full, you are fully successful

## Wait it Out
When you wait for a threat to blow over, roll without modifiers
- On a miss, time moves forward 4, and further suffer the worst of the Rationale outcome
- On a mixed, time moves forward 4, you avoid any negative outcome, but also receive no positive outcome
- On a full, time moves forward 1, you avoid any negative outcome, but also receive no positive outcome

## Tough it out
When you just try to power through something, roll without modifiers
- On a miss, you suffer the worst of the Rationale outcome and gain no benefits
- On a mixed, you are injured, you avoid any further negative outcome, but also receive no positive outcome
- On a full, you avoid any negative outcome, but also receive no positive outcome

## Pay the Price
When you pay someone off, roll without modifiers
- On a miss, you lose 50g * threat scale
- On a mixed, you lose 20g * threat scale
- On a full, you lose 10g * threat scale

## Delve Deep
When you explore the full depths of a mysterious structure, roll without modifiers
- On a miss, you suffer the worst of the Rationale outcome and gain no benefits
- On a mixed, you are injured, but otherwise successful
- On a full, you are fully successful

## Take Shelter
When you take shelter in a mysterious structure, roll without modifiers
- On a miss, time moves forward 4, you avoid any negative outcome, but also receive no positive outcome
- On a mixed, time moves forward 2, you recover 2 spirits, you avoid any negative outcome, but also receive no positive outcome
- On a full, time moves forward 2, you recover 2 spirits, you cure the Exhausted condition, you avoid any negative outcome, but also receive no positive outcome

---

# Prop resolver

Concept - Certain gear represents a prop. Certain items in a scene represent props. Players can combine props to produce a single action. The action is coded with an encounter resolution and success/mixed/fail bands.

The recipe always consists of:
- "rationale"/threat
- complidator, if applicable
- player approach
- Player aspects
- Scene elements

Every recipe is hard-coded.

Rationales:
1 lattice incursion
2 forest trap
3 grid defenses
4 bandits
5 weather
6 killer
7 structure

Approach/confound:
1 clever/exhausted
2 stealthy/detected
3 violent/injured
4 cautious/hurried
5 direct

Player aspects:
1 lattice manipulator
2 grid security viewer
3 trap disarm kit
4 canvas tarp
5 rope
6 hammer and spikes
7 brass lantern

Scene elements:
1 boulder
2 dead tree
3 wagon
4 tall grass
5 loose stones
6 fog/dust
7 ridge
8 gully