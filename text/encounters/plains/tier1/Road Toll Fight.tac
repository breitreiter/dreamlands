Road Toll Fight
[stat combat]
[tier 1]

The pretense drops. You draw your weapon and the thieves rush in. No formation, no discipline, just desperation and numbers. The leader hangs back, shouting orders.

clock:
  10

challenges:
  * Prevent the thieves from surrounding you [counter Throw them into disarray]: 3
  * Break their morale [counter Wound one of the bandits]: 4
  * Finish the leader [counter Cut the leader down]: 4

openings:
  * Shove one aside: free_progress_small
  * Kick a bandit in the groin: momentum_to_progress
  * Shout a vile curse: free_momentum
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
  * aggressive
  * cautious

success:
  The last of them scatters into the tall grass. You catch your breath, standing over their abandoned toll post. A few coins glint in the road dust where they dropped them.

failure:
  They beat you down and rifle through your pack, taking what they please.
  +damage_spirits 2
  +lose_random_item
