# Encounter
- Title
- Opening text
- Choices (label + preview) - on click remove choices and proceed to branch
- region or dungeon hero image

## Encounter branch
- choice label as title
- choice preview as subtitle
- preamble
- skill check and outcome if appropriate
- choice text
- mechanical outcomes
- confirmation button (to allow the user to finish reading the choice text)

## Mechanical outcomes

### open - branch to another encounter on close
- re-starts encounter flow with new encounter id

### add_tag/remove_tag <tag_id>
- "You will carry the weight of this choice"

### +add_item <item_id>
- "You have gained " + item_displayname
- add to inventory

### add_random_item <count> <category>
- randomly select a number of items of the appropriate category
- use tier/biome tables to select appropiate color
- if count = 1 - "You have gained: " item_displayname
- else "You have gained:\n" + unordered list + item_displayname

### lose_random_item
- randomly select an item from inventory, remove it
- potentially check for any changes to static effects associated with the item

### damage_health
- decrement health
- "You lost {n} health"

### heal
- increment health (not past maximum)
- "You recovered {actual health recovered} health"
- if no health is actually recovered, no message

### damage_spirits/heal_spirits
- similar rules maximum value rules as heal
- "Your spirits fell by {n}"
- "Your spirits were raised by {n}"

### increase_skill
- alter skill value
- "{skillname} improved by {n}"

### decrease_skill
- alter skill value
- "{skillname} lowered by {n}"

### skip_time
- "Time passed, it is now {new_time}"

### finish_dungeon
- after final choice confirmation, return PC to map
- mark dungeon as explored (no affordance to re-enter, marked on map)

### flee_dungeon
- after final choice confirmation, return PC to map
- no change to dungeon