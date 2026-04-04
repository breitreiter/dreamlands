The Conscripts Combat
[stat Combat]
[tier 2]

You draw your weapon. The officer's gaze tracks the motion; his mouth opens slightly, brows lifting as though you have spoken some jest he does not yet comprehend. Then his eyes find yours. Whatever he sees there hardens his jaw.

"Form up, on me," he barks, the conversational ease stripped from his voice. "Jeoffrey, Tanner, on my left. Wynne, Taylor, move to flank." The soldiers descend from the wall-walk in a clatter of boots on timber. Steel hisses from scabbards. They spread across the yard, boots scuffing dirt, their shadows lengthening in the low sun.

The scavenger woman watches you. Her eyes shift between you and her bound allies. She does not move, not yet.

clock:
  12

challenges:
  * Jeoffry and Tanner press you [counter Break their ranks]: 4
  * The officer maneuvers his small troop [counter Cut the officer down]: 3
  * Wynn and Taylor circle behind you [counter Hobble Wynn]: 8

openings:
  * Feint toward the soldier's weak side: free_progress_small
  * Kick dust into the nearest conscript's eyes: free_progress_small
  * Step inside Jeoffrey's guard before he sets: free_progress_small
  * Shove Tanner back into the wall-walk ladder: free_progress_small
  * Rush the officer before they fully form up: threat_to_progress
  * Catch Wynne's blade on your crossguard: free_progress_small
  * Break through their flank toward the gate: threat_to_progress
  * Force Taylor wide with a low sweep: free_progress_small
  * Read their formation and shift your stance: free_momentum
  * Keep moving between their positions: free_momentum_small
  * Draw the soldiers apart: free_momentum
  * Tell the scavenger woman to free her friends: spirits_to_momentum
  * Grab a conscript as a living shield: threat_to_progress
  * Drive straight through their center line: momentum_to_progress

approaches:
  * aggressive
  * cautious

success:
  With two of their fellows fallen and their ranks broken, the three soldiers turn and flee. The woman clutches you in a desperate hug, then, thinking better of it, steps back. "The families here have long memories, trader. We will not soon forget what you have done for us today. Your name will be spoken of with honor."

  You nudge the officer with your boot. Dead. You take his sword, then return to the road.
  +add_item scimitar
  +quality faction.legion -5
  +quality faction.scavengers +5

failure:
  You sense the moment you lose the initiative and are pressed on your back foot. You hold them back for a few moments before the pain burns across your back. You spin to see Taylor with with rusty little sword, now wet with your blood. Someone kicks you in the back of your right knee and you go down. You remember little after that. It is dark when you awake.
  +skip_time night
  +add_condition injured
  +add_condition exhausted
  +lose_random_item
  +quality faction.legion -5
  +quality faction.scavengers +1
