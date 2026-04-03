The Requisition Line Conflict
[stat Negotiation]
[tier 2]

The sergeant and Jory await some interesting tale from your travels. They've been out here for months with few visitors and they've decided it's your turn to amuse them. You consider your travels and, after a moment, recall a suitable story, something the soldiers are unlikely to have heard from another traveler.

timers:
  * The soldiers grow irritated: fatal every 9
  * The sun beats down mercilessly [counter Move the group into the shade]: spirits 1 every 3 resist 3
  * Jory fidgets with his book [counter Pull Jory into the conversation]: tick "The soldiers grow irritated" 1 every 3 resist 5
  * The sergeant is restless [counter Flatter the sergeant]: tick "The soldiers grow irritated" 1 every 3 resist 3

openings:
  * Mention the wind that never stopped: free_momentum_small
  * Describe the vitrified stone you saw: free_progress_small
  * Recount the deserter who helped you: free_momentum
  * Tell of the checkpoint with no papers: free_progress_small
  * Speak of the scavenger matriarch's maps: free_progress_small
  * Embellish the part about Grid weapons: threat_to_progress
  * Explain the sealed vault you passed: free_progress_small
  * Describe the children playing in ruins: free_progress_small
  * Mention the quartermaster's expired inventory: free_progress_small
  * Weave in the lieutenant's fading authority: momentum_to_progress
  * Claim you opened a Grid component: threat_to_progress
  * Suggest you know what broke the empire: threat_to_progress
  * Recall the night you sheltered there alone: spirits_to_momentum
  * Let the story breathe and settle: free_momentum

approaches:
  * aggressive
  * cautious

success:
  By the time you finish your story, you have the whole patrol's rapt attention. Sensing you've finished, one soldier pipes up with "that's it? But it was just getting good." The sergeant shoots him a hard look and the man frowns and falls silent. Jory nods, "best story we've heard all week. Road ahead is hard, friend. Take some provisions for the trip. Let it not be said the legion is a poor host." You collect the cloth-wrapped bundle, nod to the soldiers, and continue on your journey.
  +add_random_items 6 food

failure:
  The soldiers lose attention, despite your assurances you're nearly to the good part. After a minute or so, only Jory remains. But what optimism you had quickly fades, as you realize he's deep in his book and hasn't been paying attention for some time. You clear your throats and Jory looks up, "Oh, you're still here?" You decide now is a good time to make an exit.
  ++damage_spirits 2
