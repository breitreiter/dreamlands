# Hiker Ranges and Settlement Spacing Reference

## Overview
This document establishes realistic travel ranges for different character types across various terrain, used to calculate proper settlement spacing and difficulty scaling in map generation.

**Node scale:** 3 miles per node

---

## Hiker Profiles

### Normal Person (Novice Merchant)

**Carrying capacity:** ~40-50 lbs total

**Typical loadout:**
- Food: 2 lbs/day (dried rations, hardtack)
- Water: 4-8 lbs/day depending on climate (1-2 gallons)
- Gear/weapons/trade goods: 15-25 lbs baseline

**Starting expedition loadout:**
- 5-7 days food (10-14 lbs)
- 2 days water (8-16 lbs)
- Gear (20 lbs)
- **Total: 38-50 lbs**

**Daily travel distances:**
- **Plains/roads:** 10-12 miles (2-3 mph pace for 4-5 hours)
- **Forest/hills:** 6-8 miles (slower pace, rougher terrain)
- **Swamps/mountains:** 3-5 miles (exhausting, requires frequent rest)

**Critical constraint: Water**
- Must find water every **2 days maximum** or die
- In hot/arid conditions: every **1 day**
- This is the hard limit on range

**Safe range from settlements:**
- **Plains:** 40-50 miles (5 days out × 10 miles/day, assuming water sources)
- **Forest:** 30-40 miles (5 days × 6-8 miles/day)
- **Mountains/swamps:** 15-25 miles (5 days × 3-5 miles/day)

---

### Veteran (Expert with Bushcraft)

**Carrying capacity:** 30-40 lbs (lighter, more efficient packing)

**Loadout:**
- Food: 1 lb/day rations (supplements with foraging/hunting)
- Water: 1 day supply (finds sources reliably)
- Gear: 15-20 lbs (multi-purpose tools, lightweight)

**Key skills:**
- **Foraging:** Can find 1-2 lbs food/day in forest/plains (takes 2-3 hours)
- **Hunting:** Unreliable but high payoff (can get 5-10 days meat if successful)
- **Water finding:** Can locate springs, catch rain, purify sketchy sources
- **Navigation:** Takes efficient routes, avoids dead ends

**Daily travel distances:**
- **Plains/roads:** 15-18 miles (faster pace, lighter load)
- **Forest/hills:** 10-12 miles (knows game trails, efficient routing)
- **Swamps/mountains:** 6-8 miles (still challenging but experienced)

**Range from settlements:**
- **Theoretically unlimited** with good foraging/hunting
- **Practically limited by:**
    - Time cost of foraging (reduces daily distance)
    - Hunt failure streaks (can't rely on it every day)
    - Water scarcity in deserts/mountains
    - Injury/exhaustion accumulation

**Realistic expert range:**
- **Plains/forest:** 100+ miles (foraging reliable)
- **Mountains:** 50-70 miles (harder foraging, must plan water sources)
- **Desert/swamps:** 30-40 miles (water is critical, foraging unreliable)

---

## Game Design Numbers (3-mile nodes)

### Early Game (Novice in Safe Zones)

**Settlement spacing in plains/scrub:**
- **Maximum:** 15-20 nodes apart (45-60 miles)
- **Comfortable:** 10-12 nodes apart (30-36 miles)
- **Safe:** 6-8 nodes apart (18-24 miles)

**What this enables:**
- Novice can travel 3-4 days from town with food
- Still have 2-3 days to get back or reach next town
- Water sources needed every **6-8 nodes** (18-24 miles, ~2 days travel)

**Forest/hills settlement spacing:**
- **Maximum:** 10-12 nodes (slower travel, same food limit)
- **Comfortable:** 6-8 nodes
- **Safe:** 4-5 nodes

### Late Game (Expert in Dangerous Zones)

**Mountain/swamp settlement spacing:**
- **Veteran range:** 15-25 nodes (45-75 miles)
- Needs **strategic water sources** every 5-8 nodes
- Needs **food opportunities** (game trails, fishing spots, berry patches) every 3-5 nodes

**Expert gameplay characteristics:**
- **Route planning** - chain together known water and foraging sites
- **Weight optimization** - bring climbing gear OR extra food, not both
- **Risk management** - push hard and risk injury, or go slow and burn more food

---

## Recommended POI Spacing by Zone

### Tier 1: Safe Starting Zone (Plains/Roads)
- **Towns:** every 8-12 nodes
- **Inns/waystations:** every 4-6 nodes
- **Water sources:** every 2-3 nodes (wells, streams, rivers)
- **Total safe range for novice:** 12-15 nodes from starting city

### Tier 2: Frontier (Forest/Hills)
- **Towns:** every 10-15 nodes
- **Small settlements:** every 6-8 nodes
- **Water sources:** every 3-4 nodes
- **Foraging sites:** every 2-3 nodes
- **Expert can reach:** 20-25 nodes out from safe zones

### Tier 3: Wildlands (Mountains/Swamps)
- **Settlements:** every 15-20 nodes (sparse, dangerous)
- **Critical resources (water, shelter):** every 4-6 nodes
- **Expert must chain these together carefully**
- **Novice will die trying** - these zones are endgame

### Tier 4: The Dark Places (Remote/Hostile)
- **No settlements,** only ruins
- **Water sources:** every 5-8 nodes (hard to find, must be discovered)
- **Foraging:** unreliable, takes significant time
- **Expert needs detailed knowledge to survive**
- **Creates "exploration as conquest"** - mapping these resources IS the achievement

---

## Key Design Principles

### Water is the Hard Limiter
Food can be stretched with foraging/hunting, but water sources are non-negotiable. POI placement should prioritize:

1. **Water source distribution** defines survivable routes
2. **Settlement placement** at natural water intersections (river crossings, oases, springs)
3. **Difficulty scaling** by increasing distance between water sources
4. **Expert gameplay** emerges from knowing secret water sources and efficient routes

### Player Experience by Skill Level

**Novice player looks at map and sees:**
- Safe town-to-town roads
- Clear water sources marked
- Dangerous areas obviously marked

**Expert player sees:**
- Hidden spring at node X
- Cave with stream at node Y
- "I can cut through those mountains if I hit these waypoints"
- Secret routes that novices can't survive

### Progression Through Geography

**Early game (Novice, Tier 1 zones):**
- Settlements close together
- Water abundant
- Foraging unnecessary
- Can explore without dying

**Mid game (Skilled, Tier 2 zones):**
- Wider spacing between settlements
- Must start thinking about water
- Foraging becomes useful
- Route planning matters

**Late game (Expert, Tier 3-4 zones):**
- Sparse settlements or none
- Water sources far apart and hard to find
- Must forage/hunt to extend range
- Detailed route planning required
- Knowledge of terrain is power

---

## Travel Time Examples

### Example 1: Novice crosses plains
- **Objective:** Travel from Town A to Town B
- **Distance:** 10 nodes (30 miles)
- **Terrain:** Plains/roads
- **Travel time:** 3 days (10 miles/day)
- **Water needed:** 2 sources (day 1, day 2, arrival day 3)
- **Food needed:** 6 lbs (3 days × 2 lbs/day)
- **Risk level:** Low if waystations/water sources are placed every 3-4 nodes

### Example 2: Expert explores mountains
- **Objective:** Reach remote ruin
- **Distance:** 20 nodes (60 miles) from nearest settlement
- **Terrain:** Mountains
- **Travel time:** 10+ days (6 miles/day average due to rough terrain)
- **Strategy:**
    - Day 1-2: Travel through foothills, resupply at known stream
    - Day 3-5: Push into mountains, hunting along the way
    - Day 6-7: Reach hidden spring (must know it exists)
    - Day 8-9: Final push to ruin
    - Day 10+: Return journey
- **Requires:** Knowledge of 3-4 water sources, ability to forage/hunt, climbing gear
- **Risk level:** High - one missed water source or bad hunting streak = death

### Example 3: Novice attempts wilderness too early
- **Mistake:** Tries to reach forest settlement 15 nodes away
- **Terrain:** Dense forest
- **Travel speed:** 6 miles/day = 2 nodes/day
- **Time needed:** 7-8 days
- **Food carried:** 5-7 days worth
- **Outcome:** Runs out of food, must turn back or forage (untrained = slow, risky)
- **Lesson learned:** Need to be closer to intermediate resupply points OR need foraging skills

---

## Implementation Notes

### For Map Generation

When placing settlements and POIs:
1. Calculate node's distance from starting city (Dijkstra depth)
2. Determine zone tier based on depth and terrain
3. Apply appropriate spacing rules from the tables above
4. Ensure water source coverage meets requirements
5. Place specialty resources (foraging, hunting) in appropriate density

### For Difficulty Scaling

**Easy mode (beginner-friendly):**
- Reduce spacing between water sources by 25%
- Add more waystations
- Increase foraging success rates

**Normal mode (as designed):**
- Use spacing guidelines above

**Hard mode (veteran challenge):**
- Increase water source spacing by 25-50%
- Remove some settlements
- Make foraging/hunting less reliable
- Add more hostile encounters

### For Player Feedback

Signal difficulty through environment:
- **"The road is well-traveled and you pass several other merchants"** = Safe zone
- **"The path grows faint and you see no recent tracks"** = Frontier
- **"You find no sign that anyone has passed this way in years"** = Wildlands
- **"The very air feels wrong here"** = The Dark Places

This helps players self-assess whether they're prepared for what lies ahead.
