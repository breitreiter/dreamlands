Amenities:
- Market - buy/sell anything, random prices, stock per biome/tier
- Guild banker - store stuff
- Outfitter - buy/sell gear
- Inn/tavern - rest and recover. cures exhaustion, freezing, hungry
- Temple - cures haunted
- Healer - cures injured, diseases. takes time.

Settlement tiers could be:

Aldgate (palace icon) - Has everything, rare
Town (rook) - Has most amenities (80-90%)
Outpost (house) - Basic amenities only (market, tavern, storage)
Waystation (flag) - Minimal (maybe just tavern)

Zoomed out (strategic view):
- Settlement size icons only
- Maybe biome-colored backgrounds
- Focus on geography and routing
- dyanamically generated client-side
- shows discovered poi and paths
- color path by biome

Zoomed in (tactical view):
- Size icons remain
- Amenity icons appear as smaller decorations around the settlement (if space permits)
- Full details still in tooltip
- full map png + non-diegetic overlay showing discovered poi and paths

Fully zoomed in (single settlement view):
- Could show stylized settlement illustration with amenity buildings visible

For each biome:

Look at all tiles of that biome.

Find the maximum graph distance among them.

Assign:

Tier 3 = region(s) containing that max

Tier 2 = something in the middle

Tier 1 = closest region(s)

You’ve turned tiering into a ranking problem inside each biome.