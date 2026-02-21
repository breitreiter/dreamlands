# Two part inventory system

Items do not have explicit weight. Instead, items are automatically fit to slots. All items fit in one slot.

## Pack

This represents some sort of load-bearing pack carried on the back, either by straps or carried over the shoulder.

The pack's starting capacity is 3.

The pack has a few inventory slots which hold large objects. This includes:
- Survival equipment, like cartography kits and bedrolls
- Weapons and armor
- Trade goods

The pack has a numeric number of slots. Everything is one slot. All items are scaled to be approximately the same level of bulk or weight or encumbrance. Slots are basically an abstraction for "equally inconvenient to pack and carry"

Every weapon is one slot. Every piece of armor is one slot. When buying a trade good, it fits in one slot. All tools fit in one slot.

Items equipped to the body (1 weapon, 1 armor, 1 pair of boots) do not count against pack slots. If an equipped item is removed, it must be placed in a pack slot. A player may equip an item from the pack into a slot that is currently occupied. This simply swaps the items.

Items which can be equipped to the body (weapons, armor, boots) only provide bonuses while equipped.

All other items which provide bonuses continue to provide them from the pack.

## Haversack

A bag carried along the body.

This is a container for small items. It has disjoint inventory slots. Items which can be stored in haversack cannot be stored in the pack, and vice versa.

The haversack's permanent capacity is 20.

Items which can be stored in the haversack:
- food items
- small mementos and quest items
- documents

Like the pack, the haversack has a fixed number of slots.

A serving of food is one slot. A small trinket is one slot. A document is one slot. When buying food, you buy quantities pre-measured to fit into exactly one slot.

The haversack cannot carry any equippable items.

Items in the haversack which apply bonuses provide them from the haversack.

## Upgrading storage capacity

The haversack can never be upgraded.

The pack can be upgraded by purchasing items at merchants.

These items are immediate applied to increase the capacity of the pack by 1. They cannot be stored. Once purchased, they are no longer available for sale anywhere. They cannot be found through encounters. They cannot be lost. They are essentially a one-use increment to pack size.

- Bamboo frame — a lightweight internal skeleton that distributes weight and opens up vertical space
- Fitted yoke — a carved wooden shoulder piece that transfers load to your chest, letting you carry more before fatigue forces you to drop things
- Iron reinforcements — Robust metal fittings to prevent tears
- Side lashing rings — brass rings riveted to the exterior for strapping bulky items outside the main body
- Hanging hook — a cast metal arm attachment for dangling a lantern, cage, or bundled gear
- Compression straps — external cinches that let you pack a fuller load without things shifting and jamming
- Woven carrying cloth — a large reinforced wrap panel that lets you bundle overflow onto the pack exterior
