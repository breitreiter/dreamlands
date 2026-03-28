The Washed-Out Ford
[variant traverse]
[intent exploration]
[tier 1]

The ford marked on your map is gone -- three days of rain have turned
the creek into a surging brown torrent. Broken fence posts and uprooted
shrubs tumble past in the current. The far bank is only twenty yards
away, but the water looks waist-deep and fast.

stats:
  resistance 6
  queue_depth 5

timers:
  draw 1
  * Rising Water: resistance 1 every 4
  * Debris: spirits 1 every 3

openings:
  * Wade Carefully: free -> damage 1
  * Brace and Push: momentum 1 -> damage 2
  * Rope Crossing: tick -> damage 3 [requires has climbing_rope]
  * Find Footing: free -> momentum 1
  * Strong Stroke: spirits 1 -> damage 3

failure:
  The current takes your legs out. You wash up downstream, soaked
  and bruised, missing something from your pack.
  +damage_spirits 2
  +lose_random_item
  +add_condition exhausted
