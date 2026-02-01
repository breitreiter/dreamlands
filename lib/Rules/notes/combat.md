The nodes as combat postures
Think of each node not as "what's happening" but as the relative positioning between you and your opponent. Something like:

Locked — close quarters, grappling range, neither has advantage
Pressed — opponent is driving you back, you're reacting
Pressing — you have momentum, opponent is reacting
Circling — distance, both assessing, low intensity
Exposed — you've overcommitted or been caught out
Opening — opponent has a gap you can exploit
Reeling — you just took a serious hit, trying to recover
Cornered — environmental or positional trap, limited options
Dominant — you have clear control of the fight
Desperate — last-resort territory, high risk everything

That's ten, which feels like a workable number. You could go to twelve or fifteen, but ten is enough to create varied paths without making the prose authoring unmanageable. Each node is a dramatic state — it tells you what the emotional and tactical texture of this moment is, without specifying any concrete physical details. Those come from the tags.
Choices as verbs, not tactics
At each node, the player gets 2–4 choices. Crucially, these should be posture choices, not specific actions. Not "swing your sword" but something more like:

Commit — go all in on the current dynamic, escalate
Redirect — try to change the terms of engagement
Endure — absorb what's coming, hold your ground
Withdraw — create distance, sacrifice position for safety
Exploit — press a specific advantage (only available at certain nodes)

The same verb means different things at different nodes. "Commit" when you're Pressing means pressing harder. "Commit" when you're Exposed means a desperate counterattack. The prose handles that distinction — the player just decides their intent.
This also means you're not making promises about the fiction. You never said "I pick up the dropped sword." You said "I commit." The prose tells you what that looked like.
Edge mechanics
Each choice at a node has a weighted distribution over outgoing edges. The weights are influenced by:

Base weights — the default likelihood for that choice from that node
PC stats — a strong character's "Commit" from Pressing is more likely to reach Dominant; a fast character's "Redirect" from Pressed is more likely to reach Circling
Opponent tags — fighting something fast makes Redirect harder; fighting something slow makes it easier
Environment tags — Cornered is more reachable in cramped spaces; Circling is harder in them

So the resolution for a single beat is: player chooses a verb → system evaluates weights → selects destination node → plays the prose for that transition (source node + choice + destination node + tags).
HP as accumulated damage
HP cost on edges is elegant because it means the path matters. A fight where you bounce between Pressed and Circling for four beats is a war of attrition. A fight where you go Circling → Opening → Dominant → resolution is efficient but required good choices or good stats. Some edges are free. Some are expensive. Getting Reeling → Pressed might cost 2 HP because you took hits stabilizing. Getting Exposed → Reeling is expensive because you got punished.
You could also make some edges conditionally free or cheap based on stats. A tough character pays less on defensive transitions. A skilled character pays less when exploiting openings. This makes builds feel different without changing the graph.