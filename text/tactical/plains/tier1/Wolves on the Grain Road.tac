Wolves on the Grain Road
[variant combat]
[intent violence]
[tier 1]

Three wolves emerge from the tall grass along the roadside ditch. The
largest blocks the road ahead, hackles raised, while the others fan out
to either side. These aren't strays -- they move together, practiced
and patient.

stats:
  resistance 8
  momentum 3

timers:
  draw 2
  * Flanking Maneuver: spirits 2 every 4
  * Pack Howl: resistance 1 every 5
  * Closing Circle: spirits 1 every 3

openings:
  * Lunge: momentum 2 -> damage 3
  * Feint: momentum 1 -> damage 1
  * Hold Ground: free -> momentum 2
  * Break the Circle: tick -> stop_timer
  * Trap Line: free -> damage 4 [requires has bear_trap]

approaches:
  * scout: momentum 0, timers 2, openings 3
  * direct: momentum 3, timers 2
  * wild: momentum 5, timers 3

failure:
  The pack drags you down. You fight free but leave blood and gear
  behind in the road dust.
  +damage_spirits 3
  +lose_random_item
