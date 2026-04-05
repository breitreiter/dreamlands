Fight the Machine
[stat Combat]
[tier 3]

You ready your weapon. This will be a hard fight.

clock:
  12

challenges:
  * There's a pattern in its movement, find it [counter Notice how its head scrapes the ceiling]: 5
  * Find the flaw in the damaged leg casing [counter Step inside a blow and examine the thing]: 6
  * Shatter another leg [counter Smash the casing in the unarmored belly]: 5
  * Maneuver behind it as it struggles to right itself [counter Vault over the flailing legs]: 4

openings:
  * Charge through its erratic patrol path: threat_to_progress
  * Dodge as it careens past: free_progress_small
  * Strike when it stalls against the wall: momentum_to_progress
  * Duck beneath its flailing limb: free_momentum
  * Sidestep its crooked lunge: free_progress_small
  * Slip past as it reverses direction: free_progress_small
  * Block with your blade raised: free_momentum
  * Anchor your footing to brace the blows: free_momentum_small
  * Meet its rush head-on: threat_to_progress
  * Weave between its jerking strikes: free_progress_small
  * Pivot away from its shoulder charge: free_progress_small
  * Let it crash past you: free_progress_small
  * Throw yourself into the damaged leg: spirits_to_momentum
  * Grab one of the soldier's shields: threat_to_progress

approaches:
  * aggressive
  * cautious

success:
  The horse-sized thing struggles to turn in the tight corridor. You have a few moments to finish it, and your blows hammer hard, smashing the glass, and then the strange organs within. It screams, or sings, you're not sure which, but the warbling tone is awful to hear. Something fluid sprays, not blood, not oil, something that smells sweet and wrong. The tone stops, but your ears ring. You make your way back to the surface, wiping away the ichor as best as you can.
  +add_condition injured
  +add_item mountain_regiment_armor

failure:
  The blows from the creatures limbs hammer and crush you. You, stumble and it's on you. Everything goes black. You awake next to the fallen soldiers. Inch by inch, you drag your ruined body back to the surface.
  +advance_time 3 no_sleep no_meal no_biome
  +add_item mountain_regiment_armor
  +add_condition injured
  +add_condition irradiated
