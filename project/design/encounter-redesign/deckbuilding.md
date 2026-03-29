# Tactical Encounter Deckbuilding

Each tactical encounter assembles a **deck** of 15 openings at encounter start. The deck is shuffled and drawn from — no replacement until exhausted, then reshuffle. Deck composition depends on what the player brings (gear, skills) and what the encounter provides (filler). Better-prepared characters have better decks, not bigger ones.

## Core rules

- **Deck size is always 15.** Global constant. No exceptions.
- **One governing stat per encounter.** The `.tac` file declares a stat (combat, cunning, negotiation, bushcraft, etc.). Only that stat's level and relevant equipment contribute cards.
- **Your stat level = your card count.** Combat 0 = 0 collection cards. Combat 7 = 7 collection cards. Simple, linear, no compression.
- **True deck draw.** Shuffle once at start. Draw from top. See every card once per cycle. Reshuffle when exhausted. No duplicate draws within a cycle — this rewards card-counting and makes the system a hand management puzzle, not a slot machine.

## Deck assembly

### Step 1: Collection cards (from player loadout)

Your governing stat level determines how many cards you contribute. These come from two sources:

**Equipment openings.** Each equipped item explicitly lists what cards it contributes and which stat it counts toward. A +1 combat knife MUST supply a named opening — "Knife Thrust" or whatever. No implicit bonuses; every stat-contributing item has authored cards. Examples:

- Arming sword (combat): "Slash" (momentum 2 → +3 progress), "Parry" (free → +2 momentum)
- Heavy armor (combat): "Brace" (free → reduce next timer fire)
- Trail boots (bushcraft): "Sure Footing" (free → +2 progress)
- Ornate spyglass (cunning): "Read the Situation" (free → ×threat)

**Skill-intrinsic openings.** Each skill level may contribute openings that don't come from gear. These represent learned ability — a Combat 3 character knows "Feint and Riposte" regardless of what sword they're holding.

**Selection.** The engine gathers all collection cards matching the encounter's governing stat. If you have more cards available than your stat level allows, the engine selects the best N (or the player picks — TBD). If you have fewer, you contribute what you have and the remainder comes from filler.

### Step 2: Filler cards (from the .tac file)

The encounter provides a pool of filler openings. These fill remaining deck slots (15 minus collection cards). Filler comes in two tiers, prioritized:

**Gated filler.** Openings with `[requires has item_id]`. "Rope Swing" requires a climbing rope. "Torch Sweep" requires a torch. If the player has the item, these fillers are prioritized over generic chaff. This rewards bringing the right tools even when your governing stat is low.

**Generic filler (scenery + chaff).** Encounter-specific flavor openings ("Use the Terrain", "Desperate Lunge") and generic weak openings ("Scramble: free → +1 progress"). The encounter author writes as many or as few scenery fillers as the encounter needs for narrative texture. If the encounter doesn't supply enough filler, the engine pads with global chaff.

**Fill order:** Gated filler (if player qualifies) → encounter scenery → global chaff. Fill until deck = 15.

### Step 3: Counter openings (stop-threat)

**Stop-threat is a collection card, not free.** If your collection includes openings with the stop-threat effect, they go into the deck like any other card. At draw time, the engine re-themes the card to target a specific active timer (most urgent, or player's choice — TBD).

If you don't bring any stop-threat cards, you can't stop timers. You're forced into a resistance kill. Control kills are a build reward — you need to carry gear or have skills that provide counter cards.

This means the `.tac` file's timer `counterName` field is now the **re-theming label** — when your generic "Parry" card draws and targets the "Flanking Maneuver" timer, it displays as "Block the flank" (the timer's counterName).

## What this means in play

**Combat 0, no relevant gear (15 filler cards).** Every draw is scenery or chaff. Weak, linear, but functional. You grind toward a resistance kill with +1 progress cards. No counter cards means timers are inevitable — pure attrition race. The bear deck.

**Combat 4, decent sword (4 collection + 11 filler).** A mix. Your sword's "Slash" and "Parry" show up roughly every 4 draws. The rest is filler. You have some good turns and some bad turns. If your sword provides a stop-threat card, you can attempt control kills but it's inconsistent.

**Combat 8, excellent gear (8 collection + 7 filler).** More than half your deck is quality. Strong, flexible, you can pivot strategies. Multiple stop-threat cards means you can reliably hunt control kills. The filler turns are just pacing between power plays.

**Combat 10, best possible gear (10 collection + 5 filler).** The vintage deck. Every cycle through the deck, 10 of 15 draws are powerful. Filler is just breathing room. Timers barely matter because you'll draw a counter within a few turns. The encounter is still dangerous if resistance is high and timers are fast, but you have the tools.

**Combat 3 but you have a rope in a traverse encounter (3 collection + gated filler prioritized).** Your stat is low, but the rope unlocks "Rope Swing" (a good filler card) which gets priority over generic chaff. Smart packing matters even when your stats don't match.

## Card counting

Because the deck is drawn without replacement, players can (and should) count cards. If you know your "Decisive Blow" (+5 progress) is somewhere in the remaining 6 cards, that changes whether you Press the Advantage or take the weak opening in front of you. This is the depth — the encounter is a puzzle about managing what you know is coming, not just reacting to random draws.

After a reshuffle, the count resets. Long encounters (high resistance) cycle through the deck multiple times, giving the player repeated access to their best cards.

## Data locations

| Data | Lives in | Notes |
|------|----------|-------|
| Governing stat | `.tac` file front-matter | `[stat combat]` — one per encounter |
| Equipment openings | `ItemDef` (lib/Rules) | Explicitly authored per item. Must declare stat tag. |
| Skill-intrinsic openings | Skill definitions (lib/Rules) | Per-level, cumulative |
| Gated filler | `.tac` file openings section | `[requires has item_id]` on filler openings |
| Scenery filler | `.tac` file openings section | No requires — everyone gets these |
| Global chaff | Balance constant (lib/Rules) | ~6-8 weak generic openings, authored once |
| Counter re-theme labels | `.tac` file timers | `counterName` on each timer |
| Deck size | Balance constant | 15 (global, fixed) |

## Traverse: authored path, not drawn deck

Deckbuilding changes the traverse variant significantly. The queue (the visible path ahead) must be **authored in the .tac file**, not drawn randomly from a pool. The whole point of traverse is "you can see the terrain and plan your route." If the path came from a deckbuilt pool of collection cards and chaff, it would be unpredictable and character-dependent — breaking the fantasy.

**The path is the encounter.** The `.tac` file specifies an explicit sequence of openings that represent the terrain: wade, brace, push, rest, wade... Everyone sees the same path regardless of build. The encounter author controls the pacing, the cost curve, and the narrative of the crossing.

**Your deck is your escape hatch.** Press the Advantage and Force an Opening draw from your deckbuilt deck — the same 15-card assembly used in combat. When the path demands resources you don't have, you detour into your deck for better options, then return to the path.

This creates a clean split between variants:

| | Combat | Traverse |
|---|---|---|
| **Each turn** | Draw from your deck | Take the next step on the authored path |
| **Press/Force** | Draw 3 from your deck | Draw 3 from your deck (detour) |
| **Player skill affects** | Every single draw | Only detour draws |
| **Encounter author controls** | Timers, resistance, filler pool | Timers, resistance, the entire path |
| **Fantasy** | Reactive — reading your opponent | Proactive — planning a route |

**Format change needed.** The `.tac` traverse format needs a `path:` section — an ordered list of openings that defines the queue. The current `openings:` section would still exist for the Press/Force deck (or perhaps the authored path openings serve as the filler for deckbuilding, with the player's collection cards mixed into the Press/Force draws).

## Open questions

- Does the player choose which collection cards to include when they have more than their stat level allows, or is it automatic (best N)?
- How do combat approaches (scout/direct/wild) interact? Scout could let you look at the top N cards and reorder. Wild could shuffle a bonus power card into the deck but also shuffle in extra chaff.
- Should there be a way to "tutor" (search your deck for a specific card) as a high-cost action?
- Exact card counts per item and per skill level — needs playtesting.
- Whether armor's contribution is cards (active: "Brace") or passive (reduce timer damage) or both.
- Traverse: does the Press/Force deck use the same 15-card deckbuilding system, or a simpler pool? If it's a full deck, what fills it — the path openings as filler, or separate scenery?
- Traverse: how long should authored paths be? Fixed length, or does the path length = resistance value?
