The Gentle Giant Escape
[stat Bushcraft]
[tier 3]

You tighten the straps of your pack and check your boots. It's now or never.

clock:
  9

challenges:
  * Reach the edge of the orchard [counter Take a shortcut to a side gate]: 7
  * Cross the open courtyard while the beast stands [counter Push yourself to cross faster]: 3
  * Evade the monster as it chases you [counter Slip into a ventillation shaft]: 5

openings:
  * Narrowly dodge a patch of glassy terrain: free_progress_small
  * Vault a low wall at full sprint: threat_to_progress
  * Follow the curve of the walkway: free_progress_small
  * Steady your breathing as you move: free_momentum
  * Cut straight across unstable ground: threat_to_progress
  * Leap over a gap in the path: threat_to_progress
  * Duck under a hanging irrigation frame: free_progress_small
  * Keep to the smooth ceramic path: free_progress_small
  * Break into open ground at speed: momentum_to_progress
  * Check the Iron's stance behind you: free_momentum_small
  * Push through burning lungs and fear: spirits_to_momentum
  * Angle toward the widest gap: free_progress_small
  * Time your steps to the walkway rhythm: free_momentum
  * Weave through the bleached stump maze: free_progress_small

approaches:
  * aggressive
  * cautious

success:
  Slowly the Iron's bone-shaking footsteps fade as the monster abandons the search. Perhaps the creature had a heart once, but you are no resident of this place. You are vermin to be exterminated, and you've narrowly avoided that fate.

failure:
  The monster is upon you. You wedge yourself between two walls, but the creature crushes them effortlessly. Huge chunks of concrete batter down on you. Satisfied you are no longer a threat, the creature returns to its vigil. Eventually, painfully, you pull yourself free of the debris.
  +add_condition injured
  +add_condition exhausted
  +advance_time 3 no_sleep no_meal
