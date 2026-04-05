Navigating the Collapse
[stat Bushcraft]
[tier 2]

The route looks treacherous. You'll need to navigate down a steep bank of loose scree, then hop from boulder to boulder to reach the sealed crate. Once you've secured the crate to your pack, you've got a hard climb ahed on the far bank. At any moment you could lose your footing and tumble into the rocks below.

clock:
  10

challenges:
  * Navigate down the bank [counter Find a safe path]: 2
  * Get to the crate [counter Jump to the crate]: 4
  * Climb the far slope [counter Find solid footing]: 7

openings:
  * Test each foothold before committing weight: free_momentum_small
  * Slide down on your heels: free_progress_small
  * Use the cliff face to steady yourself: free_momentum
  * Pick a careful line between the boulders: free_momentum
  * Anchor yourself: free_progress_small
  * Wedge yourself between two rocks to rest: free_progress_small
  * Push forward: momentum_to_progress
  * Ignore the pain and scramble ahead: spirits_to_momentum
  * Find handholds in the crumbling earth: free_progress_small
  * Pull yourself onto the level ground: free_progress_small
  * Catch your breath on a stable ledge: free_momentum
  * Use exposed roots as a makeshift ladder: free_progress_small
  * Leap across the widest gap between stones: threat_to_progress
  * Shake loose dirt from your boots: free_momentum_small

failure:
  Your feet slip out from beneath you, and you tumble head over foot, landing hard against a sharp cluster of rocks. You recover the crate. It looks to be an old Trade Guild package, bound for some guild cartographer a dozen years ago and lost here. The contents are yours now.
  +add_item cartographers_cloak
  +add_condition injured

success:
  You hoist yourself and your baggage to the top of the far bank. Weary but successful. The crate looks to be an old Trade Guild package, bound for some guild cartographer a dozen years ago and lost here. The contents are yours now.
  +add_item cartographers_cloak
  +heal_spirits 2
