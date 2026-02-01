equipment and gear mostly exist to apply stacking resistances to conditions.

you might resist exhaustion via:
- a nice pair of boots
- a comfy bedroll
- a walking stick

you might resist freezing via:
- a warm bedroll
- a down jacket

you might resist thirsty via:
- a canteen

resist swamp fever via
- mosquito netting (while owned)
- eating jorgo root (one day)

the exception is weapons and armor, which increase your combat effectiveness

weapons have three layers:
- **quality** (crude / good / fine) — the mechanical tier, determines combat_bonus
- **class** (sword, axe, club, spear, dagger, staff) — determines narrative text: readying sentences, damage descriptions per creature type. defined in weapon_classes.yaml
- **flavor name** (chipped cutlass, bone shiv, etc) — the specific name the player sees, varies by biome. each flavor maps to a class

encounter templates use:
- `{weapon_name}` — the flavor name
- `{weapon_ready}` — a readying sentence drawn from the weapon's class
- `{weapon_damage_light_<creature>}` / `{weapon_damage_heavy_<creature>}` — damage sentence drawn from class + creature type (beast, monster, humanoid, undead)