Road Toll Fight
[variant combat]
[stat combat]
[tier 1]

The pretense drops. You draw your weapon and the thieves rush in. No formation, no discipline, just desperation and numbers. The leader hangs back, shouting orders.

stats:
  resistance 8

timers:
  draw 2
  * The thieves surround you [counter Throw them into disarray]: spirits 1 every 4
  * The bandit leader rallies his men [counter Cut the leader down]: resistance 1 every 3
  * You take another cudgel blow [counter Put you back to the mile marker]: add_condition injury every 5

openings:
  * Shove one aside: free_progress_small
  * Kick a bandit in the groin: momentum_to_progress
  * Should a vile curse: free_momentum
  * Smash a bandit into the mile marker: momentum_to_progress_large
  * Headbutt the closest one: spirits_to_progress
  * Wait for a gap: free_momentum_small
  * Duck a bandit's wild blow: free_progress_small
  * Kill one of them dead: momentum_to_progress_huge
  * Charge the closest bandit: threat_to_momentum
  * Cut the leader down: momentum_to_cancel
  * Kick road dust into them: free_momentum_small
  * Shout and wildly attack one: threat_to_progress
  * Slam into the nearest thief: momentum_to_progress
  * Fight through your exhaustion: spirits_to_momentum

approaches:
  * scout: momentum 0, timers 1, openings 3
  * direct: momentum 2, timers 2
  * wild: momentum 4, timers 3

failure:
  They beat you down and rifle through your pack, taking what they please.
  +damage_health 3
  +damage_spirits 2
  +lose_random_item
