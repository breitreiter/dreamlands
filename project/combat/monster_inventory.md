# Monster Art Inventory

Source: `assets/monsters/`. Static PNGs, intended for the combat-screen left
panel. Each one is a candidate for a single named, hand-authored `.cmb` set
piece (one sprite = one monster, per `combat_pivot.md`).

## Forest (3)

| File | Subject |
|------|---------|
| `forest_bear.png`   | Enormous grizzled bear with glowing red eyes |
| `forest_knight.png` | Knight in a green cloak with a sword — likely an exile, possibly a hunter sent after one |
| `forest_ranger.png` | Old ranger with a wooden staff, likely an exile himself |

## Plains (4)

| File | Subject |
|------|---------|
| `plains_bandit.png`  | Scavenger bandit in a leather cloak and robot mask, no weapons visible |
| `plains_boss.png`    | Giant robot covered in sensors and beam-projectors |
| `plains_robot_1.png` | Large blocky robot, many antennae sprouting from the head |
| `plains_robot_2.png` | Large blocky robot, many antennae sprouting from the head |

## Scrub (5)

| File | Subject |
|------|---------|
| `scrub_giant.png`     | Giant walking color-factory creature |
| `scrub_boss.png`      | Mutated humanoid, saturated with strange purple coloration |
| `scrub_mage.png`      | Hovering robed figure, face hidden behind a shimmering purple disk |
| `scrub_warrior_1.png` | Warrior with sword, purple-saturated |
| `scrub_warrior_2.png` | Warrior with sword, purple-saturated |

## Swamp (6)

| File | Subject |
|------|---------|
| `swamp_boss.png`    | Enormous hulking biped flesh mutant |
| `swamp_child.png`   | Small crouched flesh mutant with exposed bone ribs |
| `swamp_mage.png`    | Shadowy figure in flesh robes |
| `swamp_beast_1.png` | Horse-like flesh mutant with a humanoid skull for a head |
| `swamp_beast_3.png` | Hulking quadruped flesh mutant with no discernable eyes |
| `swamp_beast_4.png` | Predatory flesh mutant, poised to attack |

> Numbering gap: no `swamp_beast_2.png`. Either retired or never produced.

## Counts

| Biome     | Sprites |
|-----------|---------|
| Forest    | 3       |
| Plains    | 4       |
| Scrub     | 5       |
| Swamp     | 6       |
| Mountains | 0       |
| **Total** | **18**  |

Mountains has no monster art yet; encounters there will need to fall back to
single-check combat in `.enc` files until sprites land.

## Roster

Tier assignment: every `_boss` sprite is a T3 set piece. Everything else is
T2. No combat encounters in T1 yet.

Each entry is the player's lead-in: second person, present tense, ending at
the beat before engagement.

### Forest

**Old Redleaf** — `forest_bear.png` *(T2)*
You hear it first — the snap of brush thirty paces upslope, then the wet
huff of breath. It comes down through the trees without hurry. Massive,
grizzled, the fur at its shoulders silvered. When it stops to look at you,
its eyes catch the canopy-light wrong, an ember-glow that holds steady when
it shouldn't. The forest has gone quiet around you both. It rolls its
shoulders, lowers its head, and starts to move.

**Halen Morick** — `forest_knight.png` *(T2)*
You see a man in heavy imperial plate walking down the road toward you.
Fine gear, not the rusted cast-offs you've seen in the nearby garrisons.
He halts some ten paces away and you do the same. "Didn't think I'd find
you walking the road, but here you are. You've left a fair few bodies in
your wake." He does a slight bow. "Halen Morick. I'm the one they've sent
to put you down." He draws his blade. "Take a moment. When you're ready,
we'll begin."

**Old Bram** — `forest_ranger.png` *(T2)*
You took a shortcut off the marked trail and found his cabin in a hollow
you would never have found on purpose — low timber, moss-weighted
shingles, smoke curling out of the chimney. The old man at the door was
congenial. Said he didn't get many visitors, and would you sit a while.
You traded news for a bowl of stew and stories for another, and the
afternoon went easier than you'd expected. Now you're on the threshold
on your way out, pack settled, and you hear the floorboard behind you go
quiet.

"Sorry, friend." His voice is the same congenial voice it was an hour
ago. The staff is in both hands now. "Can't let it get out where my
cabin is. I've got enemies. Nothing personal, trader."

### Plains

**Tob Ashford, the Mask** — `plains_bandit.png` *(T2)*
Three of his crew are visible behind the wagon-husk — one with a crossbow,
two pretending not to be paying attention. The man who steps out wears a
brown leather long-coat and the visor of a dead Pylon strapped over his
face like a helm. He spreads his hands, friendly. "Old Survey Road,
friend. South of Forward Depot Three. That's Ashford's. That's me." He
cocks his head. "Now you can pay the toll, you can walk back the way you
came, or — third option — you can be why my morning got interesting. Pick
fast."

**Pylon Unit V-3** — `plains_robot_1.png` *(T2)*
You curse your foul luck and this old imperial survey map and the fact
that you are, for the third time this hour, lost again. The road is
gone or was never here. The sun is lower than it should be. The only
landmark in any direction is a squat tower out across the grass — pale,
blocky, taller than a man, standing alone in open country. A guard
post, you hope. A relay station with a roof and a kettle. You set out
toward it.

It is not a guard post. You realize this somewhere in the long approach
across the grass: that the antennae at the crown are turning in a slow
sweep, that the indicator lights along the chest have woken amber, that
the head has finished tracking and is locked on you. The voice comes
flat and synthetic in clean imperial Common. "PYLON UNIT V-3 ACTIVE.
PERIMETER VIOLATION ACKNOWLEDGED. ENGAGEMENT AUTHORIZED." The lights
go red. Its arms unfold. The plains around you are very open, and there
is no road back the way you came.

**Pylon Unit V-7** — `plains_robot_2.png` *(T2)*
The Old Satara Road runs straight through scrub pine and blackened
stumps, paving stones still fitted tight after centuries of neglect. Two
men have a handcart blocking it — a sorry, mule-drawn affair, one wheel
threatening to part company — and they are trying to lever something
heavy onto it with a plank and a length of rope. The something is
wrapped in canvas that's slipped loose at one end to reveal smooth dark
iron and a row of antennae crushed flat against the bed. The shorter
man sees you first and drops the rope. "Private enterprise. Salvage
rights. We were here first, and I'll thank you to —" His partner's
voice cuts him off, gone tight and careful. "Ged." Ged keeps talking.
The canvas is moving.

The thing stands up. Not the way a living creature stands; it lurches
upright all at once, like a table flipped onto its legs, antennae
snapping erect at the crown as the cart cracks and splinters beneath
it. A row of indicator lights along the chest wakes amber, then red. A
flat synthetic voice speaks in clean imperial Common: "PYLON UNIT V-7
ACTIVE. PERIMETER VIOLATION ACKNOWLEDGED. ENGAGEMENT AUTHORIZED." There
is a sound like a whip-crack and the smell of a forge, and Ged is on
the ground and not moving and the front of his coat is on fire. Mickey
runs. The recessed face-panel turns to you. One leg drags badly as it
steps off the wreckage of the cart — something inside it is broken,
maybe many things — but it is coming for you.

**Sentinel 7-C** — `plains_boss.png` *(T3)*
Past the gate, the abandoned city opens around you in clean imperial
geometry — granaries closed, market stalls intact, doors shut on quiet
rooms. Something larger than a wagon stands at the next intersection,
beam projectors still warm. It raises one of its long arms toward you in
something like a salute, or a scan. The voice from its chest plate is
calm, conversational, addressed to no one in particular. "SENTINEL 7-C.
INNER PERIMETER. UNAUTHORIZED PRESENCE NOTED. COMMENCING ENFORCEMENT."
The Color in the projector lenses begins to wake.

### Scrub

**Vessel IV-N** — `scrub_giant.png` *(T2)*
You smell it first — hot metal and something sweeter, like fruit gone
wrong. The thing comes around the rail-cut on three legs and one limb
that is not quite a leg, taller than a guardhouse, plated in Kesharat
industrial gray. The Color leaks from a vent on its side in pulses you
cannot quite see directly. A small man in administrator's whites is
jogging behind it with a tablet, shouting calmly: "Stand clear of the
unit. Stand clear of the unit." The unit does not stand clear of you.

**Civil Priest Darun Velyr** — `scrub_mage.png` *(T2)*
He is several inches above the road. You notice this the way you notice
something while you are noticing something else, and by then he is
already speaking. His face is hidden behind a polished violet disk, and
his voice is gentle, almost relieved. "I am so glad you stopped. There
has been a misunderstanding about your function, and I am authorized to
correct it. Please listen carefully." The disk turns toward you. The
Color behind it begins to thicken. "We will begin with alignment."

**Stray Vessel** — `scrub_warrior_1.png` *(T2)*
You see it first as a wrong color on the hillside above the dry
riverbed — a saturated violet the sun does not explain. As you watch,
it stands. The shape is vaguely a man's shape, taller than a man,
naked, the skin pulsing in slow waves the way a cuttlefish pulses, hues
you cannot quite settle on rolling across it from one side of the body
to the other. There are no eyes you can find. There is no expression to
find them with. In one hand it is holding a curved Tashkari blade,
gripped correctly, and that is the only thing about it that still
remembers being human. It steps down off the slope toward you. It does
not speak. Whatever walks out of the great factory does not speak.

**Stray Vessel** — `scrub_warrior_2.png` *(T2)*
The Tashkari camp at the bottom of the wash is empty. Not abandoned —
the cookfire is still smoking and a goat is still tied — but empty, and
the standing figure at the far side of the clearing is not Tashkari. It
is naked, vaguely man-shaped, taller than a man, and the skin ripples
in long slow pulses of a violet that hurts to look at directly,
shading to colors you do not have the eyes for. A jambiya hangs in its
grip, point down, blood not yet dry on the blade. The head turns toward
you as you enter the clearing. It does not raise the blade. It does
not need to. It begins to walk.

**Prefect Velor Qastor** — `scrub_boss.png` *(T3)*
The hall is geometrically too perfect — angles a fraction sharper than
your eye can hold, walls that brighten on no schedule. The man who rises
to greet you wears the formal pleating of a senior interior administrator,
and the Color is deep in him, in his eyes and the breath he speaks with.
"Ah. The traveler. I have read your function. There is a small
irregularity, and I have the authority to address it personally." He
smiles, briefly, the way a polite host smiles. "Please. Set down what you
are carrying. The correction is brief."

### Swamp

**Orel-Who-Did-Not-Grow** — `swamp_child.png` *(T2)*
It is on the plank walkway ahead, crouched, watching. The shape is a
child's shape, mostly. The ribs show through the wet skin in places they
should not, and one arm ends a finger short. It does not move when you
approach. It does not move when you stop. Then it stands — all wrong, the
joints in the wrong order — and begins to come toward you in the gait of
something that has never been told it is not a child. You remember, too
late to matter now, hearing that you should offer it food.

**The Hooded** — `swamp_mage.png` *(T2)*
You see it first as a darker patch in the bioluminescent moss along the
stone — a robed shape, or what wants to be a robed shape, edges blurring
into the wet light around it. Up close the impression does not resolve.
It is the suggestion of a woman in a deep hood. It is the suggestion of
a hand. It is the suggestion of a face turning toward you and not
finding the geometry quite right and trying again. This is not the
witch in the Shrine Quarter and her hedge-pharmacology. This is what
Revënakh has been remembering, every time it has remembered a witch,
all of them at once. Your skull goes warm. Behind your eyes, very
gently, the bog begins to push. If you let it push long enough, you
understand, you will be one more thing the swamp remembers.

**The Drayman's Horse** — `swamp_beast_1.png` *(T2)*
You hear the wheels first. There are no wheels. The thing comes up the
old hauling track at a pace too steady to be alive — horse-bodied, the
harness rotted on its flanks, the head a human skull set wrong on the
long neck. There is nothing pulling it. There is nothing it is pulling.
It does not slow when it sees you. It has been on this run since before
the village was built, and it does not yield the road.

**The Listener** — `swamp_beast_3.png` *(T2)*
You hear yourself stop talking. Not a decision. The thing is already
coming — four-legged, pale as wet bone, the head a smooth ruin where eyes
should be. It moves toward sound the way iron moves toward a lodestone,
and you have spent the last hour speaking. Your boot creaks on the plank.
Its head snaps to you. Whatever proverb the Revathi have about silence on
the deep paths, you understand it now.

**The Bog Stalker** — `swamp_beast_4.png` *(T2)*
It has been watching you for some time. You realize this in the
half-second before it comes out of the reeds — too low in the hip, too
long in the shoulder, the wet skin patterned like nothing that ever
lived. It does not roar. It does not posture. It crosses the open water
between you in a single coiled stride and is on its second when you raise
your weapon.

**Sothëkavath, the Compounded Soldier** — `swamp_boss.png` *(T3)*
The clearing was a battle once and is again. The thing in the middle of
it is the size of two men and built from more — armor from a dozen eras
layered into the skin, the seams of it healed and reopened a thousand
times. It turns toward you, and its mouth moves, and many voices come
out at once — all of them angry, none of them at you, all of them
speaking your name. Not the name on your papers. The name your
grandmother carried. It plants its feet in the mud where it has planted
its feet for a thousand years, and it lifts the blade.
