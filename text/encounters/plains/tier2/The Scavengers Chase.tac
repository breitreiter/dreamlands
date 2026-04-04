The Scavengers Chase
[stat Cunning]
[tier 2]

There is no fighting this thing. Your best hope is to hide and flee. There is an abaondoned imperial watchtower in the distance; you might be safe if you can make it there.

clock:
  11

challenges:
  * Distract the monster [counter Pull the canvas tarp over it]: 5
  * Make it to the abandoned watchtower [counter Slip into an old trenchline]: 7
  * Wait silently until it loses interest [counter Crawl into an old basement]: 4

openings:
  * Sprint across the open ground: free_progress_small
  * Dodge around an old artillery pit: free_progress_small
  * Follow the military road toward the tower: free_progress_small
  * Vault a collapsed trench wall: momentum_to_progress
  * Keep your breathing steady: free_momentum
  * Cut through a gap in the earthworks: free_progress_small
  * Force yourself past exhaustion: spirits_to_momentum
  * Risk the exposed ridge for speed: threat_to_progress
  * Scramble through vitrified rubble: threat_to_progress
  * Slip between rusted supply crates: free_progress_small
  * Duck into an abandoned foxhole: free_progress_small
  * Catch your breath behind a marker stone: free_momentum_small
  * Leap across a crater's crumbling edge: threat_to_progress
  * Focus on the tower ahead: free_momentum

success:
  You're not sure how long you spend hidden in the tower, but at long last the thing horrible scraping and thumping noises outside fade to silence. After a time, you dare a peek and find that the thing has moved on. You make your way back to the road, quick as you can, and return to your journey.

failure:
  You hear nothing but the wind and your own ragged breathing. The beast must have grown weary of the chase and shuffled off to menace some other unfortunate. You dare a quick peek; nothing. With some relief, you stand and immediately see it. It has spotted you as well. You dive for cover, but you are too slow. There is a high whine and a pop, and your side floods with agony. There is no singed flesh, only a surface itching and a deeper pain.
  The creature decides it is done with you, and clanks off.
  +add_condition irradiated
