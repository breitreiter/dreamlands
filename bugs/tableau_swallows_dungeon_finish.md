---
kind: bug
title: "Tableau level-up on arc completion swallows the dungeon-finish step, stranding the player inside the dungeon"
state: fixed
created: 2026-08-17
fixed: 2026-08-17
severity: critical
status: fixed — the deferred exit is parked on the player and consumed by the last pick,
  and an already-completed dungeon id self-heals on session load. See "Resolution".
touches:
  files:
    - lib/Orchestration/EncounterRunner.cs
    - server/GameServer/GameFunctions.cs
    - text/encounters/arcs/plains/brides_cave/The Ghosts.enc
  features: [tableau, arcs, dungeons, session-mode]
provenance:
  author: claude
  found_via: production play — completed brides_cave, levelled up, could not travel
---

# Levelling up on arc completion strands you in the dungeon

## What happens

Complete an arc whose terminal outcome grants a level, pick a tableau reward, and
you are returned to the map — but the dungeon was never exited. Travel is rejected.

Reproduced in production on `brides_cave`. Reported symptom was
`"Cannot travel while not exploring"`, and after a refresh the more precise
`"Cannot travel while in a dungeon"` (`travel_in_dungeon`,
`GameFunctions.cs:427`). Both are the same underlying state: the run left
`player.CurrentDungeonId` set.

The arc's closing text is not lost — `BuildTableauPromptResponse`
(`GameFunctions.cs:1976-1990`) carries the outcome onto the tableau screen. What is
missing is only the "Return to your journey" beat that would have exited the dungeon.

## Why it happens

`The Ghosts.enc:81-82` fires `+add_level` then `+finish_dungeon`, which is the
intended shape for an arc that grants a level.

In `EncounterRunner.FinishResolved` the `DungeonFinished` result is captured into
`pendingFinished` and the loop breaks (`EncounterRunner.cs:273-279`). Then the
tableau check runs (`:298-307`) and returns `AwaitTableauPick` carrying only
`pendingFinished.Outcome`. **The `FinishReason.DungeonFinished` itself is
discarded** — the comment says the suspend keeps "the dungeon exit clean", but
nothing ever completes that exit.

The handoff is then lost a second time on the way back:

- `GameFunctions.cs:947` calls `EncounterRunner.PickReward(session, slotId, null)` —
  `pendingOutcome` is hardcoded `null`, so even the outcome that `AwaitTableauPick`
  was carrying does not come back.
- `PickReward` therefore returns `Finished(FinishReason.Completed)`, and
  `GameFunctions.cs:951-955` inspects the step only for "still pending" before
  returning `BuildExploringResponse`. A `Finished` step of any reason is ignored.

So the only two places that clear `CurrentDungeonId` — the `DungeonFinished` /
`DungeonFled` cases at `GameFunctions.cs:774-782` and `:879-887` — are never
reached, and neither is the `end_dungeon` action (`:1019-1021`) that would have
called `EncounterRunner.EndEncounter`.

What *does* happen correctly: `ApplyFinishDungeon` (`Mechanics.cs:398-403`) adds
the arc to `CompletedDungeons`, and `PickReward` clears `CurrentEncounterId`. So
the arc counts as done and the level is really banked — the residue is purely the
stale `CurrentDungeonId`.

## Scope

Any arc that grants a level in the same outcome that finishes the dungeon. That is
the *normal* shape for a capstone reward, so this is expected to hit most
level-granting arcs, not just `brides_cave`. `+flee_dungeon` alongside
`+add_level` has the same defect via the `DungeonFled` branch.

## Affected saves

A save that hit this is stuck but **not lost**: everything except
`CurrentDungeonId` is correct. Clearing that one field restores it.

## Resolution (2026-08-17)

Both halves, since one is the fix and the other is the recovery.

**1. Park the deferred exit on the player.** The tableau spans a request boundary —
`pick_reward` is a separate HTTP call and rebuilds the session from scratch — so
threading the pending `Finished` through the `AwaitTableauPick` record would not
have survived the trip. Instead `PlayerState.PendingDungeonExit` is set when
`FinishResolved` suspends on a tableau with a `pendingFinished` in hand, and
`PickReward` consumes it on the last pick: clears the flag, clears
`CurrentDungeonId`, and returns `Finished(DungeonFinished)`.

`GameFunctions`' `pick_reward` needed no change — its `BuildExploringResponse` is
correct once the field is actually cleared.

**2. Self-heal on session load.** `GetOrCreateSession` clears `CurrentDungeonId`
when it is already in `CompletedDungeons`. That pairing is unreachable by design —
`enter_dungeon` rejects a completed dungeon — so it can only ever be residue from a
dropped exit. This recovers saves already stuck, including the one that found this,
with no data migration.

**Verification.** 523 tests pass, including 4 new ones in `TableauTests`: the exit
is parked on suspend, consumed on the last pick, held across an intermediate pick
when two levels are pending, and not falsely claimed when no dungeon was involved.

## Still open

`GameFunctions.cs:947` passes `pendingOutcome: null` into `PickReward`, so the
outcome the tableau was carrying is not handed back after the picks are spent. It
does no harm today — the tableau screen already displayed it — but it means
`PickReward`'s `pendingOutcome` parameter is dead on the server path, and any future
caller that relies on it will find it empty.
