Road Toll Chase
[variant traverse]
[stat bushcraft]
[tier 1]

You bolt off the road and into the tall grass. Behind you, the thieves curse and give chase. They're road bandits, not trackers. If you can stay ahead through the fields and find cover, they'll give up soon enough.

stats:
  resistance 7

timers:
  draw 2
  * They're gaining on you [counter Slide down a gully]: spirits 1 every 3
  * They can see you [counter Duck into an old barn]: spirits 1 every 3

openings:
  * Keep a steady pace: free_momentum_small
  * Cut through the brush: free_momentum_small
  * Double back on them: threat_to_momentum
  * Run in a zig-zag pattern: free_momentum_small
  * Shout for help: free_momentum_small
  * Vault over the fence: free_momentum_small
  * Push yourself harder: spirits_to_momentum
  * Pull your pack tighter: free_momentum_small
  * Slide down a hill: free_momentum
  * Taunt the bandits: threat_to_momentum
  * Smash through the briar patch: spirits_to_momentum

path:
  * Fence line (vault over): momentum_to_progress
  * Low hill (scramble up): momentum_to_progress_large
  * Dense thicket (vanish into): momentum_to_progress

failure:
  They corner you in a drainage ditch. Winded and outnumbered, you hand over your coin purse.
  +damage_spirits 2
  +rem_gold 25
  +add_condition exhausted
