# Settlement sell prices

Every settlement has one item which is sold 15% below base price

All other items have +/-5% jitter from base.

# Settlement buy prices

Every settlement has one item which is bought 25% above base price. This item is selected randomly. This item is always from another biome. If possible, this item should be unique to this settlement (this is the one settlement that wants bog iron).

All other items have +/-5% jitter from base.

Settlements impose an additional 10% buy price penalty for items from their same biome.

# Settlement inventory by size.

Each step up is additive. An output has everything that a camp has, plus more.

## Camp
- 2 in-biome/in-tier trade goods

## Outpost
- 1 weapon, armor, or equipment (1 instance)

## Village
- 1 in-biome/in-tier trade good

## Town
- 1 out-of-biome tier 1-2 trade good
- 1 weapon, armor, or equipment (1 instance)

## City
- 1 in-biome/in-tier trade good
- 1 weapon, armor, or equipment (1 instance)

# Maximum stock count
- Camps have a maximum of 1 instance of a trade good
- Outposts have a max of 2
- Villages have a max of 3
- Towns have a max of 4
- Cities have a max of 5

# Settlement Restock

Camps, Outputs, and Villages stock one random trade good each day. No settlements restock weapons, armor, or equipment.
Towns and Cities stock two random trade goods each day.
If an item would stock past its cap, the added stock is lost.

All settlements in tier 1 start with max possible inventory.

All other settlements start with exactly one of each salable item.

# Player Mercantile Skill

Each point the player has in the Mercantile skill reduces the cost the player pays for all goods by 2%.