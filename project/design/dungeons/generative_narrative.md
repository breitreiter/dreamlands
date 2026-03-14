# Procedural beats
Revolve around the protagonist's external or practical goals and evoke a sense of suspense, advancing the overall plot with little personal investment from the characters. For example, the hero may be giving chase to the villain to stop his nefarious plans: in a positive resolution, he is able to intercept the bad guy; in a negative one, he loses track of him.

# Dramatic beats
Are the opposite of Procedural ones: instead of moving the external plot along, they concern personal aspirations and relationships between characters. For example, the hero may seek his father figure's approval for his actions: in a positive resolution, his father supports him; in a negative one, he is not impressed at all.

# Commentary beats
Inject the author's own voice into the narrative, e.g. as a Greek Chorus or as an Author Filibuster, and are often (but not always) a sign of bad writing.

# Anticipation beats
Create expectation of incoming awesomeness. Any "Hell, Yes!" Moment is essentially this. They resolve positively by definition.

# Gratification beats
Consist of various forms of Fanservice (not necessarily sexual, e.g. a Continuity Nod is also a form of fanservice) and/or Author Appeal and serve to resolve positively without adding anything of consequence to the story.

# Bringdown beats
Are Gratification's Evil Twin, rubbing salt into the wounds without technically making the situation any worse — for instance, an Empathy Doll Shot. Naturally, they always resolve negatively.

# Pipe beats 
Lay subtle Foreshadowing groundwork for upcoming events or reveals. Well-written pipes resolve neutrally, as the audience doesn't normally register them as important.

# Question beats
Pique the audience's curiosity about something that's happening or already happened. Because no one likes being kept in the dark, they usually resolve negatively.

# Reveal beats
Do just that: cash in the suspense built up by earlier Question and Pipe beats by revealing something surprising or important. They typically resolve positively.

**Lume** (Mason et al., FDG 2019, UC Santa Cruz) is worth reading even though it's pre-LLM. It's a system for procedural story generation using parameterized node-trees where individual scenes constitute the nodes but the selection of which scene to display next is left to the system, building on logic programming approaches for content selection. The key idea is scenes with preconditions, bindings (parameterized slots for characters, places, flags), and constraints. This is conceptually close to your oracle+template approach — authored fragments with parameterized slots that get filled contextually.

On the structural analysis side, **Carstensdottir et al.** have a body of work worth knowing about. Their **"Progression Maps"** paper (CHI 2020) proposes models to reason about, evaluate, and improve interactive narrative designs, arguing for utility in reducing iteration cycles. The companion **"Exploratory Automated Analysis of Structural Features of Interactive Narrative"** (Partlan, Carstensdottir et al., AIIDE 2018) developed a model of metrics for analyzing interactive narrative structure using a novel multi-graph representation, implemented for an authoring tool and tested on 20 student-designed scenarios. If you ever want to analyze the structural properties of your generated graphs (path diversity, bottlenecks, dead ends), this is the framework.

Finally, there's a newer Autodesk research paper, **WhatIF** (2025), about LLM-assisted branching narrative authoring. Authors can either manually create alternate events or use LLM-assisted branch recommendations to generate diverse, coherent storylines. Their user research found that authors mentioned starting from a grounding or a "hook" — a specific scene, location, or structure in mind — which maps pretty directly to your oracle's situation/forcing function/complication structure.

The **knowledge graph + story generation** thread might also be useful for your coherence problem. A recent paper (2025) proposes a knowledge graph framework integrated into a story generation pipeline where the graph acts as a central repository of narrative elements and relationships while dynamically ensuring logical continuity and factual accuracy throughout the story. For your biome bibles and world state, this could be a way to keep generated fragments consistent.

---

id
front
situation
role
stage
location_scope
actors
requires
priority
cooldown
effects
followups
text

role:
seed
hint
complication
choice
reveal
resolution
aftermath
ambient

id: caravan_rumor_aldgate_01
situation: missing_caravan
role: hint
tone: anxious
region: aldgate
requires:
  location == aldgate
  situation.missing_caravan.progress < 2
effects:
  situation.missing_caravan.visibility += 1
  situation.missing_caravan.progress = max(progress, 1)

progress — how far along the situation is
urgency — how hard it is pushing into the foreground
visibility — how much the player knows about it
heat — how dangerous / politically risky it has become
readiness — whether the player is equipped for resolution
decay — whether the opportunity is fading

World state
Stable facts about the world.

Examples:
settlement.aldgate.alert_level
road.aldgate_redmesa.safety
guild.borderlands.reputation

These are shared infrastructure. They should be rare.

Situation state
Short-lived counters for one packet.

Examples:
situation.missing_caravan.progress = 2
situation.missing_caravan.heat = 1
situation.missing_caravan.resolved = false

This is where most of your “salience meters” should live.

Relationship state
Reusable NPC/faction values.

Examples:
npc.factor_hale.trust
faction.teamsters.goodwill
Again: shared, reusable, limited.

Ephemeral tags
Cheap temporary flags for immediate gating.

Examples:
tag.heard_rumor_caravan
tag.injured
tag.arrived_after_dark

These should expire or be easy to recompute.