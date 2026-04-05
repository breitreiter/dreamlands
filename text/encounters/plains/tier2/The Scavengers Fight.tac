Fight the Monster
[stat Combat]
[tier 2]

The monster lumbers toward you, slow, clumsy, but inevitable.

clock:
  12

challenges:
  * Avoid the burning eye [counter Smash the flame-casting orb]: 6
  * Disable the creature's leg [counter Hack into its strange sinews]: 5
  * Defeat the monster's armor [counter Pry open its carapace]: 4
  * Smash the internals [counter Destroy the black metal box]: 2

openings:
  * Kick debris into its glass cluster: free_progress_small
  * Yank the canvas over its eye: free_momentum
  * Wrench its damaged leg until it buckles: spirits_to_momentum
  * Dodge between its searching sweeps: threat_to_progress
  * Loop rope around the hitching limb: free_progress_small
  * Circle wide of its grinding head: free_momentum_small
  * Slam its leg with your full weight: momentum_to_progress
  * Put the cart between you and the monster: free_progress_small
  * Stand firm as it lurches toward you: threat_to_progress
  * Roll a wheel into its path: free_progress_small
  * Let it charge and sidestep late: threat_to_progress
  * Hurl a stone at it: free_momentum
  * Tangle rope in its joints: free_progress_small
  * Keep the thing off-balance: free_progress_small

approaches:
  * aggressive
  * cautious

success:
  As you hammer away at the exposed internals of the creature, it quiets and ceases its struggle. The tiny points of light inside the thing's body dim, then flicker back to life for a moment, then a final darkness. You have defeated one of the Iron in single combat, no small thing. 

failure:
  The creature's leg swats you aside like a doll, sending you flying. You land on the road with a hard thump, bouncing and skidding. Your breath comes in ragged gasps, you think you might have broken a rib. You wait for the thing to finish you, but it doesn't. It scrapes and shudders away down the road.
  +add_condition injured
  +add_condition exhausted