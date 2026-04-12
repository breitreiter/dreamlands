The Gauntlet
[stat cunning]

The dome door grinds open and the air that hits you is warm, oily, and humming. This is not the entrance you expected.

The corridor opens into a vast bay, lit by strips of pale light running along the ceiling. Machines move inside — not the patrol units from outside, but smaller, specialized things. Worker drones. They shuttle between repair cradles on fixed paths, carrying components, welding, adjusting. The sound is constant: the whine of servos, the click of articulated limbs, the hiss of something being sealed or cut.

Torben pulls you behind a support column. "Workshop," he breathes. "Maintenance depot. These things are repairing each other." He watches the nearest drone complete a circuit and return to its cradle. "They're on fixed routes. Predictable. We can thread through if we time it right."

The far side of the bay opens into a corridor. Beyond it, the faint purple glow of the dome interior. You just need to get across.

clock:
  9

challenges:
  * Cross the open bay floor [counter Dart between the repair cradles]: 5
  * Pass the welding station [counter Slip through during a weld cycle]: 4
  * Reach the far corridor [counter Time the last drone's patrol loop]: 4

openings:
  * Wait for a drone to pass, then move: free_progress_small
  * Follow Torben's hand signals through a gap: free_progress_small
  * Study the drone patrol routes from cover: free_momentum
  * Crawl beneath a repair cradle: free_progress_small
  * Hold still as a drone passes close enough to touch: spirits_to_momentum
  * Sprint across an open stretch between cycles: threat_to_progress
  * Use the welding flash as cover for movement: momentum_to_progress
  * Press flat against a support column: free_progress_small
  * Time your breathing to the servo whine: free_momentum_small
  * Inch forward while a drone is occupied with repairs: free_progress_small
  * Dash past a drone mid-rotation: threat_to_progress
  * Note which cradles are active and which are empty: free_momentum
  * Duck behind a stack of components: free_progress_small
  * Squeeze through while a heavy drone blocks the sightline: threat_to_progress

approaches:
  * aggressive
  * cautious

success:
  You slip into the corridor beyond the bay. The humming fades behind you. Torben exhales, presses his back against the wall, and allows himself a grin. "Not bad. Not bad at all." He pulls out his notebook and begins sketching the bay from memory.

  Ahead, the corridor opens into light and space.
  +open "The Plaza"

failure:
  A drone pivots toward you mid-stride. No alarm, no sound — it simply adjusts course, extending a manipulator arm that crackles with the same energy it uses to cut metal. You throw yourself sideways. The arm catches your pack and the smell of scorched leather fills the bay. Torben hauls you through the far doorway by your collar.

  You crouch in the corridor beyond, hearts hammering, while the drone returns to its route as though nothing happened.
  +add_condition irradiated
  +open "The Plaza"
