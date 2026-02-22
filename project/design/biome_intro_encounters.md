# Biome Intro Encounters

One-time scripted encounters that fire the first time a player enters a new biome/tier
combination. These set the scene for a new area — describing the landscape shift, hinting
at local dangers, and grounding the player in the biome's atmosphere before they hit the
regular encounter pool.

## Concept

Each biome/tier directory (e.g. `scrub/tier2/`) can contain a specially-named encounter
file (e.g. `_intro.enc`) that is shown exactly once: the first time a player steps onto
an encounter node in that biome/tier. After it fires, the player never sees it again — it
goes into `UsedEncounterIds` like any other encounter, but the selection logic guarantees
it comes first.

## Player State

Add a `HashSet<string> SeenBiomeTiers` to `PlayerState`. Entries are category strings
like `"scrub/tier2"`. When a player enters a biome/tier for the first time:

1. Check if the category is in `SeenBiomeTiers`. If yes, proceed with normal selection.
2. If no, look for the intro encounter in that category's pool.
3. If the intro encounter exists and hasn't been used, show it. Add the category to
   `SeenBiomeTiers`.
4. If no intro encounter exists for that category, add the category to `SeenBiomeTiers`
   anyway (no intro written yet — skip silently) and proceed with normal selection.

`SeenBiomeTiers` is checked on every move into a new node, not just encounter-slot nodes.
The intro should fire on the first qualifying node in that biome/tier, even if the node
isn't an encounter slot. This means the trigger lives in the movement flow, not just in
`PickOverworld`.

## Selection Logic

In `EncounterSelection`:

- Add a method like `TryPickIntro(session, node)` that checks `SeenBiomeTiers`,
  looks for the intro encounter by convention (e.g. ID starts with `_intro` or a
  dedicated naming convention), and returns it if found.
- The GameServer move handler calls `TryPickIntro` before `PickOverworld`. If it
  returns an encounter, show it. Otherwise fall through to normal logic.

## Naming Convention

Intro encounters use a `_intro` prefix (e.g. `_intro.enc`). The underscore prefix
signals "system encounter, not part of the random pool." `PickOverworld` should skip
IDs starting with `_` so intros are never selected randomly — they only fire through
`TryPickIntro`.

## Encounter Content

Intros should be short (1-2 pages), descriptive, and low-stakes. No skill checks or
combat — just atmosphere and a choice or two that lets the player absorb the setting.
Good patterns:

- Describe the landscape transition ("The scrub gives way to cracked red earth...")
- Hint at local threats without triggering them
- Offer a small flavor choice (observe something, take a memento, talk to a local)
- Optionally grant a minor item or tag that acknowledges the player's arrival

## Bundle Integration

No bundle format changes needed. Intro encounters are regular .enc files that live in
the same biome/tier directory. The `_` prefix convention and `TryPickIntro` logic handle
the special behavior. The bundler indexes them like any other encounter.

## Scope

- 15 intro encounters total (5 biomes x 3 tiers)
- Each is a short standalone .enc file
- Writing priority is low — intros are nice QoL but not required for the gameplay loop
- Can be written incrementally as biome content is authored
