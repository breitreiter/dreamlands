Escape the Iron
[stat Bushcraft]
[tier 3]

 You see the hatch, heavy steel, torn open and hanging loose from a single hinge. 

Below the hatch, stairs. Concrete, utilitarian, descending into a maintenance tunnel lit by dim amber lights.

The tunnel is dry and wide enough for two abreast. Pipe runs along the ceiling. Junction markings at every intersection, the same inscrutable symbols you find everywhere here. 

Behind you there is the metal monster you saw on the street. Somehow it has fit its bulk down the stairway and is coming for you now. You'll need to nagivate the dense maintance tunnels, careful to avoid dead ends.

clock:
  11

challenges:
  * Make your way to the cistern [counter Remember the symbol for water]: 6
  * Cross the narrow walkway [counter Cross in a single fluid movement]: 4
  * Find the exit door [counter Notice the narrow ladder leading up]: 6

openings:
  * Duck through a junction as metal scrapes behind: threat_to_progress
  * Follow the pipe run toward distant echoes: free_progress_small
  * Mark your route at the next intersection: free_progress_small
  * Keep your breathing steady and quiet: free_momentum_small
  * Choose the wider tunnel at the split: free_progress_small
  * Sprint past a flickering amber light: threat_to_progress
  * Trace the junction symbols for patterns: free_momentum
  * Crawl through a collapsed section of pipe: momentum_to_progress
  * Move when the grinding sound fades left: free_progress_small
  * Read the flow arrows on the wall: free_momentum
  * Stay beneath the lowest hanging conduit: free_progress_small
  * Count intersections to track your depth: free_progress_small
  * Risk cutting through a flooded passage: threat_to_progress
  * Push through exhaustion to stay ahead: spirits_to_momentum

approaches:
  * aggressive
  * cautious

success:
  You push open the final hatch and are blinded by daylight. You move swiftly, unsure if the monster is just behind you or lost deep in the maintenance tunnels. You are safe, for now.
  +add_item mountain_regiment_armor

failure:
  You're too slow, the creature rounds the corner and barrels toward you. You try to lunge around the beast, but the thing is ready for you. The blow hammers into your back and you collapse. Everything goes black. You're not sure how much time has passed when you awake. You crawl out of the tunnels and back to the surface.
  +advance_time 3 no_sleep no_meal no_biome
  +add_item mountain_regiment_armor
  +add_condition injured
  +add_condition irradiated
