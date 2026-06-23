# The Lodge (Forest Tier 3)

A beast haunts the tier 3 deep forest, and the PC knows it personally before
the arc ever begins. `combat/forest/tier3/the_beast.fight` is a persistent
road encounter that repools until the arc is finished: wins drive it off but
never kill it ("Perhaps if you tracked it to its lair?"). By the time the PC
finds the arc trailhead they have traded blood with the thing several times.
They are hunting it, likely out of spite.

**Antagonist:** The beast. Definitely a creature — the PC has fought it.
Black jaws that split wider than they should, ichor-filled maw, a four-toed
track the size of a spread hand, a wrongness in its gait at distance. It
kills travelers and raises crude totems from the remains.

**Deeper concept:** The beast does not flee the PC so much as draw them in.
Ambushes broken off, a mauled man left across the path, a totem raised in
the night, a trail always just plain enough to follow. It wants the PC to
come down into the hollow on their own feet. Whether that is appetite,
ritual, or invitation is never resolved — see The Three Readings below.

## Flow

```
The Trail (Start)          PC cuts the beast's fresh track, chooses to hunt it
  └─ The Clearing          a tracker (Simon) mauled by the beast, hours from death
       ├─ help + Bushcraft pass ──→ Yana
       ├─ help + Bushcraft fail ──→ camp; beast takes Simon in the night; arc lost (finish)
       ├─ mercy kill ─────────────→ Yana
       └─ press on ───────────────→ Yana
  └─ Yana                  homesteader hub; loft offer; ask-about spokes
                           ("Yana" is a dev name only — she never gives her
                           name in prose; she is "the woman" throughout)
       ├─ accept loft ──→ dawn totem outside, Yana gone, blood trail → The Lair
       └─ refuse ───────→ over the ridge, cut the beast's track → The Lair
  └─ The Lair              hollow with a cleft; descend → the_beast_cornered fight
  └─ The Lodge             (chained from fight win) abandoned settler lodge + journal
                           reward: the_old_tooth (capstone dagger)
```

## The Journal (The Lodge scene)

The lodge is a generation abandoned. The journal arcs from a settler's plain
record (foundation, supplies from Hadrith) through first sightings of the
beast (four-toed track he does not know), the bodies and totems ("I think
the beast is doing something sacred here"), the gift of the tooth, and a
final entry: wordless communion in dreams, a cleft in the rock, "the final
lesson," "after that I am free."

### The Three Readings

It must remain actively unclear which of these is true. All three stay live;
no scene may confirm or kill one:

1. **He became the beast.** The lodge is decades-dust but the beast is
   current. Tells: losing words, raw meat, "after that I am free," the snare
   Yana found that "whatever set it had hands."
2. **He befriended it.** The watching at the treeline, the gift on the flat
   stone, "we spoke again, not with words," the beast leaving Yana alone.
3. **He was simply driven mad and died.** Every "communion" detail is also
   readable as isolation psychosis; the beast may be an ancient thing (the
   older race, pre-Revathi) that merely outlived him.

## The Old Tooth (reward in-road)

`the_old_tooth` is a capstone dagger (Rules/ItemDef.cs). The thread: the
journal-writer found, or was given, an ancient tooth on the offering stone
at the treeline — not the beast's own (its teeth are black; the tooth is
gray, "old past anything in these woods"). He hafted it into a blade. The
dreams started the night he brought it under his roof. He went to the cleft
without it. The PC takes it from the peg above the hearth.

This keeps the tooth's origin inside the same triple ambiguity: a gift from
a friend, a relic of the older race that corrupted him, or a found object a
madman wove into his delusion.

## Mechanics notes

- Road fight: `[persistent]`, `[requires !tag the_lodge.beast_defeated]`,
  wins give gold but the beast escapes.
- Lair fight (`the_beast_cornered.fight`): win sets
  `the_lodge.beast_defeated` (kills the road encounter for good) and chains
  to "The Lodge".
- Bushcraft picker in The Clearing (bind vs probe) is the only hard fail-out
  of the arc; the road fight keeps repooling, so the arc can be re-entered.
- Cunning (trained) read at The Lair lip reveals the drawn-in pattern.
