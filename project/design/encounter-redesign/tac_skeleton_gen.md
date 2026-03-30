# .tac Skeleton Generator

Design plan for a Python tool that generates balanced, empty .tac skeletons. The LLM (or human) then fills in flavor text — card names, timer names, body prose, failure prose.

## Goal

Eliminate the tedious part of authoring .tac files: choosing how many cards, which archetypes, balancing path sums against resistance, tuning timer pressure. The creative work (naming, prose) stays human/LLM.

## Output format

A valid .tac file with `FIXME` placeholders where flavor text goes:

```
FIXME: Title
[variant traverse]
[stat bushcraft]
[tier 1]

FIXME: body text

stats:
  resistance 6

timers:
  draw 2
  * FIXME [counter FIXME]: spirits 1 every 3
  * FIXME [counter FIXME]: spirits 1 every 3

openings:
  * FIXME: free_momentum_small
  * FIXME: free_momentum_small
  * FIXME: spirits_to_momentum
  ...

path:
  * FIXME: momentum_to_progress
  * FIXME: free_progress_small
  ...

failure:
  FIXME: failure text
  FIXME: failure outcomes
```

## Inputs

Required:
- `variant`: combat | traverse
- `tier`: 1 | 2 | 3
- `stat`: combat | cunning | negotiation | bushcraft | mercantile | luck

Optional:
- `seed`: RNG seed for reproducibility
- `intent`: narrative tag (violence, stealth, etc.)

## Design rules

### Traverse encounters

**Openings are momentum-only.** The PC uses openings to build momentum, then spends it on path cards. The PC can also bring their own cards (from skills + gear) which may include progress cards, letting them skip path elements entirely. That's fine — the path is a floor, not a ceiling.

Allowed opening archetypes:
- `free_momentum_small` (+1) — bulk of the deck, small reliable bumps
- `free_momentum` (+2) — a few of these, solid utility
- `threat_to_momentum` (+2, ticks timer) — risky ramp, 1-2 per encounter
- `spirits_to_momentum` (+3, costs spirits) — haymaker, 0-1 per encounter

Distribution (tier 1, ~11 openings):
- 5-6× free_momentum_small
- 2-3× free_momentum
- 1-2× threat_to_momentum
- 0-1× spirits_to_momentum

**Path is progress-only.** Path cards are the authored sequence the PC plays through. Their progress values must sum to at least the encounter's resistance — if the PC plays every path card, they're guaranteed to escape.

Allowed path archetypes:
- `free_progress_small` (+1) — filler
- `momentum_to_progress` (+2, costs 1 momentum) — workhorse
- `momentum_to_progress_large` (+3, costs 2 momentum) — power move
- `spirits_to_progress` (+3, costs 1 spirits) — emergency option, rare

Path sum rule: `sum(path progress) >= resistance + resistance_timer_pressure`

Where `resistance_timer_pressure` is an estimate of how much resistance timers will add during a typical playthrough (roughly: `resistance_tick * expected_ticks`). This ensures the path remains completable even if timers are adding resistance.

**Timers** tick spirits or resistance damage on a countdown.

### Combat encounters

**Openings are mixed.** Combat has no path, so openings must contain momentum generators, progress converters, and optionally cancel cards. The PC's own cards supplement these.

Distribution follows a "lots of small, few big" philosophy:
- Momentum generators: ~4-5 cards (same pool as traverse)
- Progress converters: ~5-7 cards (free_progress_small through momentum_to_progress_huge)
- Cancel cards: 0-1 (momentum_to_cancel)
- Spirits-cost cards: 1-2 across both categories

**Approaches** follow a fixed template:
- scout: momentum 0, timers N, openings 3
- direct: momentum M, timers N
- wild: momentum M+2, timers N+1

Where M and N scale with tier.

### Tier scaling

| Parameter | Tier 1 | Tier 2 | Tier 3 |
|-----------|--------|--------|--------|
| Resistance | 6-8 | 8-12 | 12-16 |
| Timer count | 2-3 | 3-4 | 4-6 |
| Timer draw | 1-2 | 2-3 | 2-3 |
| Timer countdown | 3-5 | 3-4 | 2-4 |
| Openings (traverse) | 9-12 | 11-14 | 13-16 |
| Openings (combat) | 10-14 | 12-16 | 14-18 |
| Path cards | 4-6 | 5-8 | 6-10 |
| spirits_to_X frequency | rare | occasional | common |

### Timer effects

- `spirits N every C` — direct PC attrition. The default pressure type.
- `resistance N every C` — increases encounter length. Must compensate by adding extra path progress (traverse) or more/bigger progress cards (combat).

Resistance timers are interesting because they change the math: the PC can't just race through the path anymore. But they need careful handling — the path sum must account for expected resistance growth.

## Implementation

Single Python file: `sim/tac_skeleton.py`

Usage:
```bash
python sim/tac_skeleton.py traverse bushcraft 1
python sim/tac_skeleton.py combat combat 2 --seed 42
python sim/tac_skeleton.py traverse cunning 3 --intent stealth
```

Outputs the skeleton to stdout. Pipe to a file:
```bash
python sim/tac_skeleton.py traverse bushcraft 1 > "text/encounters/plains/tier1/New Encounter.tac"
```

No dependencies beyond Python stdlib (random, argparse, sys).
