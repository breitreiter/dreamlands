The Ghost Story
[stat Negotiation]
[tier 2]

The sergeant and Jory await some interesting tale from your travels. They've been out here for months with few visitors and they've decided it's your turn to amuse them. You consider your travels and, after a moment, decide on a ghost story. Everyone loves a ghost story.

clock:
  9

challenges:
  * The soldiers are unimpressed [counter "The locals say the cave is haunted, but I knew better"]: 3
  * Jory is fidgeting with his book [counter "What would you have done, Jory?"]: 5
  * The sergeant is restless [counter "But I'm sure your sergeant would be fearless"]: 3

openings:
  * "The cave's slow, mineral cold settled into my bones": free_momentum_small
  * "Pale lichen painted the walls in sickly greens": free_momentum_small
  * "In the dark, water dripped like patient counting": free_momentum
  * "I was certain someone, or something, was walking just behind me": momentum_to_progress
  * "On the ground, rusted pitons and a length of frayed rope": free_progress_small
  * "A narrow passage forced me sideways, stone pressing close": free_progress_small
  * "I saw movement up ahead, but when I reached it, nothing": momentum_to_progress
  * "There were strange scratches on the rock at shoulder height": free_progress_small
  * "My lantern guttered without wind, nearly going out": spirits_to_momentum
  * "I found a pool of still liquid, black, reflecting nothing at all": free_progress_small
  * "I heard laughter in the distance, a woman's, and then silence": momentum_to_progress_large
  * "There was a sudden icy chill in the air": momentum_to_progress
  * "The darkness ahead gathered into shapes, blurring and twisted": momentum_to_cancel
  * "Every path seemed to slope inward, gently guiding me": free_momentum

approaches:
  * aggressive
  * cautious

success:
  By the time you finish, the whole patrol has drawn in close. One soldier breaks the silence: "That's it? But what happened after?" The sergeant shoots him a look and the man falls quiet. Jory closes his book. "Best story we've heard all month. Road ahead is hard, friend. Take some provisions for the trip. Let it not be said the legion is a poor host." You collect the cloth-wrapped bundle, nod to the soldiers, and continue on your way.
  +add_random_items 6 food

failure:
  You lose the thread somewhere in the middle and the soldiers' eyes glaze over. You try to recover, but the details come out muddled and the ending falls flat. After a moment, only Jory remains, though you quickly realize he's deep in his book and hasn't been listening for some time. He looks up. "Oh, you're still here?" You decide now is a good time to move on.
  +damage_spirits 2
