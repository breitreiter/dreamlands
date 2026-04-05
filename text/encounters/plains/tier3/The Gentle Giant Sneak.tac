Sneak by the Giant
[stat Cunning]
[tier 3]

You weigh the plan.

The Iron is standing over the crèche, head bowed, stationary but not off. Servos make constant micro-corrections. It is building-scale — clearance beneath it is sufficient to move under if you stay low. The undercarriage is visible: rivets, cable runs, piston housings. Weight shifts are rhythmic but not perfectly regular. The orchard stumps (head-height) provide waypoints between its legs. The mosaic walkways ring underfoot if weighted wrong. The Iron is not alert — it has no concept of alert. It detects by threshold, not by awareness. A single human body is below the density trigger. Sound and vibration are the danger, not sight. The player's goal is to cross beneath the thing and out the far side of the orchard without generating enough noise or contact to cause it to shift stance or take a step, because one step in any direction is lethal at this range.

You flank along a nearby wall. You're only just out of the monster's vision.

clock:
  9

challenges:
  * Reach the Iron's crouched body [counter Move swiftly from stump to stump]: 6
  * Sneak along the hulking, hissing metal body [counter Press close to the warm metal frame]: 5
  * Make it past the giant metal feet [counter Slide gracefully over the left foot]: 4

openings:
  * Dash forward during a servo cycle: threat_to_progress
  * Memorize the weight-shift rhythm from cover: free_progress_small
  * Sprint forward at full speed: threat_to_progress
  * Count the piston cycles to find the pattern: free_momentum
  * Slide on your belly beneath a cable run: free_progress_small
  * Hold your breath and the thing seems to notice you: spirits_to_momentum
  * Time your crossing to its furthest weight distribution: momentum_to_progress
  * Vault a stump mid-undercarriage: threat_to_progress
  * Freeze behind a stump as servos recalibrate: free_progress_small
  * Crawl behind the ruins of a low wall: free_progress_small
  * Duck-walk under the lowest clearance point: free_progress_small
  * Watch the hydraulic lines for pressure warnings: free_momentum_small
  * Track the micro-corrections to predict safe zones: free_momentum
  * Slip through when the pistons fully extend: free_progress_small

approaches:
  * aggressive
  * cautious

success:
  You pad swiftly past the metal monster. It sits silently, absorbed in its vigil, unaware of your presence. You marvel at the enormity of the thing, but don't dare to let your gaze linger. You move quickly to the nearest cluster of low concrete buildings, breaking line of sight.

failure:
  The monster shudders to life, slowly standing back up. Uncertain what else to do, you clutch desperately to the creature's monstrous metal leg. It does not notice you, but the ride is punishing, each footfall shaking through you. You pick an opportune moment to leap from the foot, landing hard on a patch of glassed terrain. You scramble to your feet and sprint to safety.
  +add_condition injured
