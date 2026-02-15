the flavor library generates text strings for any situation in which it is not useful or appropriate to formally write and store text.

# things the library will need to generate:

## Map regions (inputs: biome, tier)
- name
- per-tile descriptions

## Settlements (inputs: biome, tier)
- name
- description
- guild office description
- market description
- guild office rumors (addl inputs: price sheet)
- temple description
- inn description
- healer description

## Other elements (inputs: biome, tier)
- time of day description
- weather description (addl input: weather state)

## Loose elements
- status condition warning (condition)

The MVP version of the library can ignore inputs and return a static placeholder string. the important thing is that downstream projects are not blocked on flavor text.