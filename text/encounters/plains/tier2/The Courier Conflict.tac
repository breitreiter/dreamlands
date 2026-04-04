The Courier Conflict
[stat Negotiation]
[tier 2]

"Wait," you begin. "I am certain Captain Aldric is a good soldier, and it is right to honor his service to you. But you have a duty to the guild to deliver the letter, regardless of the contents."

clock:
  9

challenges:
  * Focus her attention [counter Kneel down beside her]: 3
  * Give her a way out [counter Offer your ring to mend the seal]: 5
  * Help her remember her duty [counter Recall the grim weight of the guild ring]: 3

openings:
  * Cite guild regulations with quiet authority: free_progress_small
  * Appeal to the courier's oath of service: free_progress_small
  * Recount your own bitter losses in transit: spirits_to_momentum
  * Trace the chain of duty on your fingers: free_momentum
  * Suggest the letter might already be compromised: threat_to_progress
  * Promise to witness whatever the letter reveals: momentum_to_progress
  * Acknowledge the captain's decorated record: free_momentum_small
  * Note that honor serves the living, not the dead: free_progress_small
  * Suggest Aldric is clever and will find a way through: free_momentum
  * Imply the guild has ways of learning refusal: threat_to_progress
  * Ask what the captain would want done: free_progress_small
  * Lean in close, menacing: threat_to_progress
  * Remind them delay breeds its own dishonor: free_progress_small
  * Point out the recipient still waits, unknowing: free_progress_small

approaches:
  * aggressive
  * cautious

success:
  You mend the seal as best you can with your own ring. A fellow Guild member would see the forgery immediately, but you imagine Captain Aldric will think nothing of it. He will be more concerned with his orders. The courier is quiet and distant, but seems to have accepted her fate. The work of the Guild is thankless, hard, but necessary.
  +heal_spirits 2
  +quality faction.tradeguild +2
  +quality faction.empire +1

failure:
  You are mid-sentence when the woman pushes the letter into the flame. You move to grasp it, but the flame burns and you draw your hand back with a curse. "You're just like the rest of them. Just another part of the great machine. I imagine I'll need to answer for this in the chapterhouse. I can only hope you, too, are called to answer for your deeds." The damage is done. You make a note of the woman to report her misdeeds next time you are in the chapterhouse in Aldgate.
  +damage_spirits 2
  +quality faction.tradeguild +1
