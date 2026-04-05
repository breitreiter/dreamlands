Unlock the Door
[stat Cunning]
[tier 3]

You crack your knuckles. You need to work fast, but more importantly you'll need to work quietly. Every sound you make attracts the Iron monster.

clock:
  12

challenges:
  * Create a map of the symbols [counter Recall your other notes on the glyphs]: 4
  * The buttons activate lights on the door, learn the pattern [counter Notice a slight flicker in the lights]: 4
  * Unlock the heavy security bolts [counter Notice a similar code in the solider's carving]: 6
  * Activate the automatic door [counter Use an opening code you've seen elsewhere]: 6

openings:
  * Tap the door with your weapon looking for lock bolts: threat_to_progress
  * Start making your own notes on the all: free_progress_small
  * Map the symbol patterns to find repetition: momentum_to_progress
  * Study the hatch mechanism for weaknesses: free_momentum
  * Remember which symbols you've seen on other hatches: free_progress_small
  * Check the buttons for wear patterns: free_progress_small
  * Test each symbol gently for response: free_momentum
  * Listen at the sealed hatch for mechanism sounds: free_momentum_small
  * Pry the panel open with your knife: threat_to_progress
  * Match tunnel markings to panel symbols: free_progress_small
  * Sketch out a diagram of the system: free_progress_small
  * Just stop for a moment and try to think: free_progress_small
  * Press symbols until your fingers ache: spirits_to_momentum
  * Strike the panel hard: threat_to_progress

approaches:
  * aggressive

success:
  You walk through the open door, finally safe. But the Iron beast skitters off the wall down the passage and then turns to face you. As it starts to gallop, you furiously work the controls. The door shudders and starts to slowly close. The creature slams hard into the closing door, then tries to pry it open. But the monster is no match for the automatic door; it closes with a horrible crunch as it severs one of the creature's limbs. You traverse the passage; the remaind is quiet and orderly, covered with a layer of dust. At last you emerge back above ground.
  +add_item mountain_regiment_armor

failure:
  You're too slow, the creature rounds the corner and barrels toward you. You pound uselessly on the control panel, then the monster strikes. The blows hammer and crush you. Everything goes black. You awake next to the fallen soldiers. Inch by inch, you drag your ruined body back to the surface.
  +advance_time 3 no_sleep no_meal no_biome
  +add_item mountain_regiment_armor
  +add_condition injured
  +add_condition irradiated
