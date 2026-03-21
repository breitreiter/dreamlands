# The Metal Beast (Plains Tier 2)

A massive metal monster, frozen in place on the plain. One of the few
imperial kills in their war against the Grid. A makeshift rope ladder
ascends to a hatch on the side of the thing.

**Deeper concept:** The only thing the empire learned from their defeat
at the hands of the technologically-superior Grid was that the Grid had
cooler toys.

**Hook:** The PC climbs the ladder to investigate. Inside they face
automated security systems before finding imperial engineers trying to
get the monster running again. They need a key part and ask the PC to
fetch it. If they can get the thing running, they can push back the
Kesharat.

**Clutch:** The PC must decide whether to help resurrect this cursed
machine, openly sabotage their efforts, or try to rig the whole thing
to explode.

# Scene Sketch

## Start
Package delivery
- Cross the plain to the beast
	- if package = intercepted
	- if no = metal_beast.looking_for_package, holdt request
- Leave

## The Harried Trader [metal_beast.looking_for_package]
Package pickup
- yes = get package, -metal_beast.looking_for_package
- no = repool

## A Mysterious Stranger
- i'll sabotage it = The Antechamber + metal_beast.accepted_sabotage
- refuse = combat + The Antechamber + metal_beast.refused_sabotage

## The Antechamber
Meet Holdt
- ask about man = metal_beast.refused_sabotage check + explains about pyke + open The Antechamber
- ask about mission = explains mission + open The Antechamber
- take the part to Sev = Belly of the Beast

## Belly of the Beast
Meet Sev
- ask about man = metal_beast.refused_sabotage check + explains about pyke + open Belly of the Beast
- ask about mission = explains mission + open Belly of the Beast
- give part [metal_beast.accepted_sabotage] = cunning check
- give part [metal_beast.refused_sabotage] = coda + reward

## Confrontation
Sev caught ya
- fight your way out
- blame pyke
- talk your way out

## Opening

The thing is visible from a mile out. At first it looks like a hill — a
low hump on the flat plain, wrong color, wrong shape. Closer, the scale
becomes clear. It's metal. Dull, weathered plates riveted or fused
together in patterns that don't follow any construction logic the PC
recognizes. One side is cratered and blackened where imperial siege
weapons scored hits. The other side is pristine. It doesn't look
damaged so much as indifferent to the damage.

A rope ladder hangs from an open hatch near what might be a shoulder.
The ropes are new. Someone has been coming and going.

## The Climb

The hatch opens into a corridor that slopes downward at an angle that
suggests the beast fell on its side. The interior is dark, lit by
faint strips along the ceiling that pulse with a dim, sourceless light.
The air smells like hot metal and something chemical the PC can't place.

The corridors are not built for human bodies. The proportions are wrong
— too wide, too low, with handholds at the wrong height. The PC passes
what might be a control room: panels of dark glass, seats shaped for
something with a different skeleton. Everything is labeled in a script
that looks efficient and means nothing.

A security system activates — something between a trap and a reflex. A
panel in the wall slides open and a mechanical arm extends, sweeping
the corridor. It's not aggressive. It's diagnostic. It scans the PC
and retracts. But the arm is fast, and the corridor is narrow, and it
would be trivially easy for the mechanism to crush someone who moved
wrong.

This requires an agility check to navigate past. Failure means a nasty
bruise and a warning: the beast's internal systems are still partially
active, and they weren't built with human visitors in mind.

## The Engineers

Deep in the machine's gut, the PC finds a makeshift camp. Oil lamps
(the engineers don't trust the beast's own lighting), bedrolls, crates
of tools, and technical drawings pinned to every flat surface. Three
imperial engineers have been living inside the beast for weeks.

**Holdt** is the leader — a stocky woman with burn scars on her hands
and the clipped speech of someone used to giving orders in loud
environments. She's a siege engineer by training. She treats the beast
the way a farrier treats a horse: with professional respect and no
mysticism.

**Pyke** is young, nervous, and brilliant. He's the one who's been
mapping the beast's internal systems, tracing conduits, figuring out
what connects to what. He has a notebook full of diagrams that are half
engineering and half guesswork. He's excited. He's also terrified. He
doesn't sleep well in here.

**Sev** is the oldest. Retired military. He fought in the campaign
against the Grid and carries the scars. He doesn't care about the
engineering. He cares about the Kesharat, who are pushing into imperial
territory with their rail and their census and their schools. He wants
a weapon. This is the biggest weapon anyone has ever seen.

## The Problem

Holdt explains: the beast is mostly intact. Its locomotion systems, its
armor, its internal power source — all functional, just dormant. What's
missing is a control element. A crystalline component that sat in the
command array — the seat the PC passed in the control room. Without it,
the beast is inert. With it, someone could potentially activate the
machine.

The component was removed. Holdt thinks the imperials who originally
captured the beast pried it out as a trophy. She's traced it through
old campaign records to a supply cache three miles east — a buried
imperial depot from the retreat, never recovered.

She asks the PC to retrieve it. Her people can't leave the beast
unattended — scavengers would strip it in days — and the depot is in
rough terrain.

## The Supply Cache

If the PC agrees to look for the component, the trip to the depot is
a half-day round trip. The cache is buried under a collapsed earthwork.
Digging it out requires a strength check. Inside: crates of corroded
equipment, campaign records, and a locked strongbox containing the
component — a crystal the size of a fist, warm to the touch, faintly
humming. It's beautiful. It also feels wrong in the hand, like holding
something alive that isn't moving.

On the way back, the PC has time to think about what they're carrying
and what it means.

## The Argument

When the PC returns (or if the PC delays and spends more time with the
engineers first), the real tension surfaces. The three engineers don't
agree on what they're doing.

**Holdt** wants to understand the machine. She's an engineer. This is
the most sophisticated piece of technology anyone in the empire has ever
touched. She wants to study it, map it, learn from it. If they can
understand how the Grid built its weapons, they can build better
defenses. She's not interested in driving the beast into battle. She
wants to take it apart, carefully, and learn.

**Sev** wants a weapon. He's watched the Kesharat expand for years. The
empire has nothing that can stand against their infrastructure, their
organization, their alien patron. This beast killed hundreds of imperial
soldiers. If they can turn it around, it can kill hundreds of Kesharat.
He says this without relish. He says it like a man describing the only
remaining option.

**Pyke** is caught between them and increasingly frightened. His
diagrams have revealed something Holdt hasn't acknowledged: the beast's
systems aren't just mechanical. The conduits carry something that
behaves like intent. When he traces the wiring, he finds loops —
circuits that don't connect to any output, that seem to exist only to
process. The beast might not be a vehicle. It might be a creature. And
they're talking about waking it up.

## The Choice

### Help the engineers install the component

The PC hands over the crystal and helps Holdt seat it in the command
array. The beast's systems activate — slowly, like something stretching
after a long sleep. The corridor lights brighten. The air circulation
changes. Deep in the machine, something begins to hum.

It works. Holdt is exultant. Sev immediately begins talking about
targets. Pyke is backing toward the hatch.

The beast is awake. What the engineers do with it is beyond the PC's
control. The empire gets a weapon it doesn't understand, built by an
intelligence that considered imperial soldiers a pest control problem.

### Sabotage the component

The PC damages the crystal before handing it over — cracking it,
shorting it, making it look like the years in the depot ruined it.
This requires a crafting or deception check. Success means the crystal
is convincingly dead. Holdt is devastated. Sev is furious. Pyke is
quietly relieved.

The beast stays inert. The engineers have nothing to show for weeks of
work. Holdt will keep studying the dead systems, but without the
control element, she's reading a book with the last chapter torn out.
Sev packs his things and leaves. The empire's only captured Grid weapon
remains a curiosity, not an arsenal.

If the PC fails the check, Holdt spots the sabotage. The confrontation
is ugly. Sev draws a weapon. The PC must talk their way out or fight.

### Rig the beast to explode

The PC works with Pyke (who is the only engineer willing to consider
this) to overload the beast's power source. Pyke has mapped enough of
the internal systems to know where the critical junctions are.

This requires a difficult crafting check. Success means the beast's
power source destabilizes over the course of an hour, giving everyone
time to evacuate. The explosion is visible for miles — a pillar of
light and heat that fuses the surrounding plain to glass, a small echo
of the Grid's own weapons. The beast is gone. So is any chance of
learning from it.

Holdt will never forgive the PC. Sev, strangely, respects the decision.
He says: "If we can't use it, nobody should."

Failure means the overload is too fast. Everyone scrambles for the
hatch. The PC takes damage in the escape. The beast still detonates,
but the evacuation is chaotic and someone might not make it out.
