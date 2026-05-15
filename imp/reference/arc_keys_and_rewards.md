---
kind: reference
title: Arc Keys and Rewards (per biome)
created: 2026-03-14
updated: 2026-05-15
status: current
touches:
  files:
    - mapgen/content/dungeons_roster.yaml
    - text/encounters/arcs/
    - lib/Rules/ItemDef.cs
  features: [arcs, dungeons, keys, rewards, gating]
provenance:
  author: migration:M-001
subject: Lookup table of arc → entry key + reward. 20 arcs across 5 biomes. The canonical roster is `mapgen/content/dungeons_roster.yaml`; this is the design-intent snapshot of gating and payoff per arc.
---

> Migration note (M-001, 2026-05-15): The arc IDs and key/reward structure
> match the live roster. Specific weapon stat lines (e.g., "Combat +1") may
> be stale after the RPS combat pivot — weapons no longer contribute to the
> Combat skill (see [[rps_combat_pivot]]). Verify against `lib/Rules/ItemDef.cs`
> before relying on a specific bonus number.

# Arc IDs by Biome

## Plains (5)
### brides_cave — The Bride's Cave (T1)
- key: brass_lantern (shop)
- reward: ivory_comb — Ivory Comb (Negotiation +1)
### grainway_station — Grainway Station (T2)
- key: Combat > 4
- reward: mountain_regiment_armor — (Medium, Cunning +2, Injury +3, Freezing +5)
### wrenbury — Wrenbury (T2)
- key: faction.scavengers > 4
- reward: lucky_buckle (+1 combat)
### metal_beast — The Metal Beast (T2)
- key: faction.legion > 4
- reward: grid_cipher — Grid Cipher (arc key)
### the_city — The City (T3)
- key: grid_cipher — Grid Cipher (arc key)
- reward: golem_armor - (Heavy, Injury +5, Freezing +2)

## Mountains (4)
### halfway_house — The Halfway House (T2)
- key: faction.miners > 4
- reward: tarnished_key (+1 cunning)
### ledgerhaus — The Ledgerhaus (T2)
- key: faction.renegadescholars > 4
- reward: hunters_journal — Hunter's Journal (arc key) 
### zahlenhaus — The Zahlenhaus (T2)
- key: faction.scholars > 4
- reward: sakharov_mask — Lead-Lined Case (Irradiated +5)  
### the_stift — The Stift (T3)
- key: Negotiation > 5
- reward: magisterial_robe — Nightveil (Light, Cunning +5, Freezing +3)

## Forest (3)
### warrant_oak — The Warrant Oak (T2)
- key: faction.exiles > 4
- reward: +2 pack size
### foresters_post — The Forester's Post (T2)
- key: Bushcraft > 4
- reward: scarecrow_boots — Scarecrow Boots (Exhaustion +5)
### the_lodge — The Lodge (T3)
- key: hunters_journal — Hunter's Journal (arc key)
- reward: the_old_tooth — Kopis (Dagger, Combat +1, Foraging +5)

## Swamp (4)
### drowning_post — The Drowning Post (T2)
- key: Cunning > 4
- reward: knotwork_seed +1 bushcraft
### listening_blind — The Listening Blind (T2)
- key: faction.revivalists > 4
- reward: antivenom_kit — Antivenom Kit (Poison +5)
### tile_house — The Tile House (T2)
- key: faction.revathi > 4
- reward: revathi_tile — Revathi Tile (arc key)
### the_revenakh — The Revënakh (T3)
- key: revathi_tile — Revathi Tile (arc key)
- reward: revathi_labrys — Labrys (Axe, Combat +4, Foraging +3)

## Scrub (4)
### census_house — The Census House (T2)
- key: Negotiation > 4
- reward: lattice_ward — Lattice Ward (Lattice Sickness +5)
### relay_post — The Relay Post (T2)
- key: faction.kesharat > 4
- reward: color_lens — Color Lens (arc key)
### wellhead_station — Wellhead Station (T2)
- key: faction.clans > 4
- reward: pack size + 2
### foundry — The Foundry (T3)
- key: color_lens — Color Lens (arc key)
- reward: shimmering_blade — Shimmering Blade (Sword, Combat +5)

# Philosophy
- Failed rolls can bring bad consequences, but failed rolls can never fail the arc
- "Incorrect" decisions can fail the arc
