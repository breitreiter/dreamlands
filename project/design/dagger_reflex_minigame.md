# Dagger as Timing-Window Attack

**Status:** locked (2026-04-26). Calibrated via combat sim at `tools/combat-sim/`.

## The model

Dagger is the skilled-player weapon. Player attacks resolve via timing precision rather than d20 — but **defense is unchanged**. The dagger user wears armor and rolls AC like anyone else; the timing minigame only affects damage output, not survival.

### Attack outcomes (player → monster)

Four outcomes, determined by timing precision (no d20):

| Outcome | Damage | Effect |
|---|---|---|
| Miss | 0 | — |
| Hit | base die + damage bonus | — |
| Crit | 2x dice (regular crit) | — |
| Super-crit | 2x dice | Cancels monster's next turn |

Bands are **nested-cumulative**: tighter timing rolls under tighter bands. Outer band → hit; inner band → crit; atomic band → super-crit; outside outer band → miss.

### Defense (monster → player)

Standard d20 + monster ToHit vs player AC. **Identical to sword/axe.** Player's armor choice (light/medium/heavy) determines hit chance the same way. A dagger user can wear plate.

This decoupling solves the "miss-parry = die" cliff that kills reflex-only weapons in most games. Bad timing only costs damage throughput, not life.

### Super-crit cancellation

Cancels the immediately-following monster turn. Monster takes no action. The heavy cooldown still ticks per design — blanking a basic costs the monster a swing; blanking a heavy resets the cooldown without dealing damage. Either way, the monster loses one action.

### Tier identity

- **T1 / T2 dagger:** high-variance burst weapon. Wider crit band than sword/axe — daggers crit more often (this is the always-on identity at every tier).
- **T3+ dagger:** unlocks the super-crit band entirely. Better gear = wider bands across the board.

Sword stays the steady choice; axe is the snowball; dagger is the spike weapon, with the spike growing teeth at endgame.

## How character stats interact

- **DamageBonus (gear tier):** adds to the damage die, exactly like sword/axe. Combat investment matters via bigger numbers per hit.
- **AttackBonus (Combat skill + weapon + token):** **does not** affect timing-window outcomes. Player skill is the entire input for hit/crit/super-crit rate.
- **AC (armor):** standard. Independent of dagger usage.
- **Indirect Combat reward:** a higher-damage hit ends combats faster, which means fewer monster turns, which means less monster damage opportunity. Combat investment shortens fights even though it doesn't change hit rate.

## Sim calibration

Tested in `tools/combat-sim/TimingDaggerPolicy.cs` with 1d4 base die. Reference: sword T3 even-match is 10.2% fatality, 6.2 rounds, 6.90 dmg/r.

| Skill profile (hit/crit/super) | Fatality | Rounds | Dmg/r |
|---|---|---|---|
| Newbie (50/5/0) | 56.2% | 11.4 | 2.82 |
| Median, T1/T2 (70/20/0) | 29.1% | 8.9 | 4.28 |
| Median, T3 (70/20/5) | 26.3% | 9.0 | 4.29 |
| Skilled, T3 (85/40/10) | 12.3% | 7.4 | 5.60 |
| Master, T3 (100/80/40) | 2.7% | 5.8 | 7.47 |
| Pinball wizard (100/100/100) | 0.0% | 5.4 | 8.00 |

Key findings:

- **Median dagger is worse than sword (29% vs 10% fatality).** This is intentional — dagger is a niche weapon. Median players should pick sword.
- **Skilled tracks sword (12% vs 10%).** Viability line is around 80-85% hit / 40% crit. Above that, dagger pulls ahead.
- **Pinball-wizard tourist drops from 96.8% to 0% fatality.** The "stats-poor character + godlike reflexes = hero" design intent fires.
- **Skilled overmatched dagger beats sword overmatched (66% vs 88%).** Reflex partially substitutes for Combat investment.
- **T3 super-crit at median is small (+3pp survival) but scales hard with skill** (5% super at median → 10% at skilled → 40% at master). Gear unlocks a ceiling, doesn't lift the floor.

## Niche positioning

Dagger is the **skilled-player weapon**. The data shows it's a harder game for median players than sword. That's the design.

This needs to be communicated clearly so median players don't pick dagger expecting parity. Affordances:
- Item description should signal "rewards precision; harder than sword/axe to use effectively"
- Possibly a tutorial / practice mode so players can self-assess timing skill before committing
- Possibly a starting-character recommendation against dagger for first-time players

## Web-platform considerations

- **Resolve client-side, validate server-side with generous tolerance.** Don't authoritatively timestamp timing inputs on the server.
- **Browser jitter.** Bias timing windows 20-30ms wider than the equivalent native-game windows. JS GC pauses, event loop scheduling, and OS compositor buffering can add 10-50ms unpredictably, especially on low-spec hardware.
- **Always show the failure reason.** "Too early," "too late," "outside band." Players who can't tell why they failed blame the game.

## Open / TBD

- **Base die.** Sim uses 1d4. Could go 1d6 if we want median play to feel less punishing — but that smears the "rewards skill" identity, so currently not planned.
- **Crit band on T1/T2 daggers.** "Wider than sword crit band" is the intent; concrete numbers TBD via playtest.
- **Window widths in the actual UI.** Pixel/frame widths for outer / inner / atomic bands TBD. Calibrate against the skill-profile targets from the sim table.
- **Super-crit affordance.** Visual + audio cue design TBD. Should read instantly as "the special thing happened."
- **Accessibility.** None for dagger specifically — players who can't or don't want to play the minigame pick sword or axe. Confirmed not a dagger-design concern.

## What we explored and rejected

- **Auto-hit chip + GWM-style lunge.** Sim showed sword beats dagger even in the low-AC sponge niche dagger was supposed to live in. Tourists were *worse off* with the auto-hit dagger because variance is the underdog's only path to winning.
- **Parry-based defense (Clair Obscur-style).** Modeled in sim with parry-rate sweep. Cliff between 50% and 70% accuracy — at 50% (the median competent player per published research), dagger was meaningfully worse than sword on every axis. Decoupling defense from timing avoids this entirely.
- **Stat-modulated parry windows.** FromSoftware lesson: this feels bad both ways. Skilled players feel cheated when stats "should have" widened the window; unskilled players feel patronized when the game widens it for them. Avoided.

## Implementation pointers

- Sim: `tools/combat-sim/TimingDaggerPolicy.cs`. `TimingDaggerParams { BaseDie, HitRate, CritRate, SuperCritRate }`.
- Sim hook for super-crit cancellation: `WeaponPolicy.ConsumeMonsterTurnSkip()`. Sim wraps both monster-turn call sites in `Sim.ResolveOrSkipMonsterTurn`.
- Reference combat outcomes for sword/axe baseline: same sim file, `SwordPolicy` / `AxePolicy`.
