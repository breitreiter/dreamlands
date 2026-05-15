---
kind: plan
title: "Hall of Honor — Leaderboard"
state: shelved
created: 2026-03-01
updated: 2026-05-13
status: current
touches:
  features: [meta, scoring, leaderboard]
provenance:
  author: migration:M-001
---
> Migration note (M-001, 2026-05-13): Out of scope for current development.

After playing a while, it's obvious that there's a design gap between dreamlands and its spiritual predecessor, Trade Wars.

Trade Wars had a daily turn count. This meant that efficient play was rewarded. In Dreamlands, there is no time pressure. It's optimal to just fuss around the tier 1 area until you max out gear and get ludicrous money, then then steamroll everything else. There is a mild graph traversal problem, but with inns and medicine, that's solvable with money.

One approach to solving this would be to add a "hall of honor" or something.

Once you completed every dungeon, you'd get added to the Hall of Honor, ranked by turn count.

This obviously requires basic machinery like checking dungeon completions, a new screen for naming your run, persistence for high scores, new widget on the home screen, new screen for all runs ever.

However, most disruptively, it requires a new encounter check type which gates purely on total bonus. This ensures that dungeons are entirely deterministic. The challenge now becomes hitting known bonus caps before entering the dungeon.

It may also require a new game mode (ranked) that immediately shows bonus checks for all dungeons, so players can plan apporpriately.
