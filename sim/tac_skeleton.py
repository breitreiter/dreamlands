#!/usr/bin/env python3
"""Generate balanced .tac skeleton files with FIXME placeholders.

Usage:
    python sim/tac_skeleton.py traverse bushcraft 1
    python sim/tac_skeleton.py combat combat 2 --seed 42
    python sim/tac_skeleton.py traverse cunning 3 --intent stealth
"""

import argparse
import random
import sys

# ---------------------------------------------------------------------------
# Archetype definitions: (id, progress_value, momentum_cost, spirits_cost)
# Only progress_value matters for path-sum balancing.
# ---------------------------------------------------------------------------

MOMENTUM_ARCHETYPES = [
    ("free_momentum_small", 1),   # +1 momentum, free
    ("free_momentum",       2),   # +2 momentum, free
    ("threat_to_momentum",  2),   # +2 momentum, ticks timer
    ("spirits_to_momentum", 3),   # +3 momentum, costs 1 spirits
]

PROGRESS_ARCHETYPES = [
    ("free_progress_small",         1),
    ("momentum_to_progress",        2),
    ("momentum_to_progress_large",  3),
    ("momentum_to_progress_huge",   5),
    ("spirits_to_progress",         3),
    ("spirits_to_progress_large",   5),
    ("threat_to_progress",          2),
    ("threat_to_progress_large",    3),
]

# Subset of progress archetypes safe for path cards (no threat-tick cards —
# those don't make sense on a deterministic path the PC walks through).
PATH_ARCHETYPES = [
    ("free_progress_small",         1),
    ("momentum_to_progress",        2),
    ("momentum_to_progress_large",  3),
    ("spirits_to_progress",         3),
]

CANCEL_ARCHETYPES = [
    "momentum_to_cancel",
    "spirits_to_cancel",
]

VALID_STATS = ["combat", "cunning", "negotiation", "bushcraft", "mercantile", "luck"]

# ---------------------------------------------------------------------------
# Tier tables
# ---------------------------------------------------------------------------

TIER = {
    1: {
        "resistance":       (6, 8),
        "timer_count":      (2, 3),
        "timer_draw":       (1, 2),
        "timer_countdown":  (3, 5),
        "timer_damage":     (1, 1),
        "openings_traverse": (9, 12),
        "openings_combat":  (10, 14),
        "path_cards":       (4, 6),
        "approach_direct_momentum": 2,
        "approach_wild_momentum":   4,
        "approach_base_timers":     1,
    },
    2: {
        "resistance":       (8, 12),
        "timer_count":      (3, 4),
        "timer_draw":       (2, 3),
        "timer_countdown":  (3, 4),
        "timer_damage":     (1, 2),
        "openings_traverse": (11, 14),
        "openings_combat":  (12, 16),
        "path_cards":       (5, 8),
        "approach_direct_momentum": 3,
        "approach_wild_momentum":   5,
        "approach_base_timers":     2,
    },
    3: {
        "resistance":       (12, 16),
        "timer_count":      (4, 6),
        "timer_draw":       (2, 3),
        "timer_countdown":  (2, 4),
        "timer_damage":     (1, 2),
        "openings_traverse": (13, 16),
        "openings_combat":  (14, 18),
        "path_cards":       (6, 10),
        "approach_direct_momentum": 4,
        "approach_wild_momentum":   6,
        "approach_base_timers":     2,
    },
}


def rand_between(rng, lo, hi):
    return rng.randint(lo, hi)


def pick_range(rng, tier_data, key):
    lo, hi = tier_data[key]
    return rng.randint(lo, hi)


# ---------------------------------------------------------------------------
# Timer generation
# ---------------------------------------------------------------------------

TIMER_EFFECTS = ["spirits", "resistance"]


def generate_timers(rng, tier_data):
    count = pick_range(rng, tier_data, "timer_count")
    draw = pick_range(rng, tier_data, "timer_draw")
    draw = min(draw, count)

    timers = []
    resistance_timer_count = 0
    for _ in range(count):
        # Bias toward spirits timers; resistance timers are rarer and interesting
        effect = "resistance" if rng.random() < 0.25 else "spirits"
        if effect == "resistance":
            resistance_timer_count += 1
        damage = pick_range(rng, tier_data, "timer_damage")
        countdown = pick_range(rng, tier_data, "timer_countdown")
        timers.append((effect, damage, countdown))

    return draw, timers, resistance_timer_count


# ---------------------------------------------------------------------------
# Opening generation — traverse (momentum only)
# ---------------------------------------------------------------------------

def generate_traverse_openings(rng, tier, count):
    """Mostly free_momentum_small with a few bigger cards."""
    cards = []

    # Guaranteed slots: a couple free_momentum and 1 threat_to_momentum
    cards.append("free_momentum")
    cards.append("free_momentum")
    cards.append("threat_to_momentum")

    # Maybe a spirits_to_momentum haymaker at higher tiers
    if tier >= 2 or rng.random() < 0.3:
        cards.append("spirits_to_momentum")

    # Maybe a second threat_to_momentum at higher tiers
    if tier >= 2 and rng.random() < 0.5:
        cards.append("threat_to_momentum")

    # Fill the rest with free_momentum_small
    while len(cards) < count:
        cards.append("free_momentum_small")

    rng.shuffle(cards)
    return cards


# ---------------------------------------------------------------------------
# Opening generation — combat (mixed)
# ---------------------------------------------------------------------------

def generate_combat_openings(rng, tier, count):
    """Mix of momentum generators, progress converters, and maybe a cancel.

    Philosophy: lots of small bumps, a few haymakers. Expensive cards are
    capped so you don't end up with 3x momentum_to_progress_large.
    """
    cards = []

    # --- Momentum generators (~35% of deck) ---
    momentum_count = max(4, round(count * 0.35))
    cards.append("free_momentum")
    cards.append("free_momentum_small")
    cards.append("free_momentum_small")
    cards.append("threat_to_momentum")
    momentum_extras = momentum_count - 4
    for _ in range(momentum_extras):
        if rng.random() < 0.3 and tier >= 2:
            cards.append("spirits_to_momentum")
        else:
            cards.append("free_momentum_small")

    # --- Cancel card (optional, more likely at higher tiers) ---
    if rng.random() < 0.2 + tier * 0.15:
        cards.append("momentum_to_cancel")

    # --- Progress converters (fill remaining slots) ---
    progress_count = count - len(cards)

    # Caps on expensive cards: at most 1 each of the big archetypes
    expensive_budget = {
        "momentum_to_progress_huge": 1 if tier >= 2 else 0,
        "spirits_to_progress_large": 1 if tier >= 3 else 0,
        "momentum_to_progress_large": 1,
        "spirits_to_progress": 1,
        "threat_to_progress_large": 1 if tier >= 2 else 0,
    }
    placed_expensive = {k: 0 for k in expensive_budget}

    # Guarantee one haymaker
    haymakers = [k for k, cap in expensive_budget.items() if cap > 0]
    pick = rng.choice(haymakers)
    cards.append(pick)
    placed_expensive[pick] += 1
    progress_count -= 1

    # Fill the rest — weighted toward cheap cards
    for _ in range(progress_count):
        roll = rng.random()
        if roll < 0.30:
            cards.append("free_progress_small")
        elif roll < 0.55:
            cards.append("momentum_to_progress")
        elif roll < 0.70:
            cards.append("threat_to_progress")
        else:
            # Try an expensive card, fall back to momentum_to_progress
            candidates = [k for k, cap in expensive_budget.items()
                          if placed_expensive[k] < cap]
            if candidates:
                pick = rng.choice(candidates)
                cards.append(pick)
                placed_expensive[pick] += 1
            else:
                cards.append("momentum_to_progress")

    rng.shuffle(cards)
    return cards


# ---------------------------------------------------------------------------
# Path generation — traverse only
# ---------------------------------------------------------------------------

def generate_path(rng, tier, target_progress, card_count):
    """Generate path cards that sum to exactly target_progress.

    Strategy: place workhorse cards to get close, then adjust with
    free_progress_small (+1) to hit the exact target.
    """
    # Available path archetypes and their progress values
    options = [
        ("momentum_to_progress",       2),
        ("momentum_to_progress_large", 3),
    ]
    # Sprinkle in a spirits_to_progress at higher tiers
    allow_spirits = tier >= 2 or rng.random() < 0.2

    cards = []
    remaining = target_progress

    # Fill card_count - 1 slots with workhorse cards, reserve last for adjustment
    for i in range(card_count):
        if remaining <= 0:
            break

        # Last card(s): use free_progress_small to fine-tune
        if remaining <= 2 and len(cards) < card_count:
            if remaining == 1:
                cards.append("free_progress_small")
                remaining -= 1
            else:
                cards.append("momentum_to_progress")
                remaining -= 2
            continue

        # Pick a card
        roll = rng.random()
        if roll < 0.15 and allow_spirits and remaining >= 3:
            cards.append("spirits_to_progress")
            remaining -= 3
        elif roll < 0.35 and remaining >= 3:
            cards.append("momentum_to_progress_large")
            remaining -= 3
        elif remaining >= 2:
            cards.append("momentum_to_progress")
            remaining -= 2
        else:
            cards.append("free_progress_small")
            remaining -= 1

    # If we still have remaining progress to cover, add more cards
    while remaining > 0:
        if remaining >= 2:
            cards.append("momentum_to_progress")
            remaining -= 2
        else:
            cards.append("free_progress_small")
            remaining -= 1

    return cards


# ---------------------------------------------------------------------------
# Approach generation — combat only
# ---------------------------------------------------------------------------

def generate_approaches(tier_data):
    base_t = tier_data["approach_base_timers"]
    return {
        "scout":  {"momentum": 0, "timers": base_t, "openings": 3},
        "direct": {"momentum": tier_data["approach_direct_momentum"], "timers": base_t},
        "wild":   {"momentum": tier_data["approach_wild_momentum"], "timers": base_t + 1},
    }


# ---------------------------------------------------------------------------
# Assembly
# ---------------------------------------------------------------------------

def format_skeleton(variant, stat, tier, intent, draw, timers, openings,
                    path=None, approaches=None):
    lines = []

    # Header
    lines.append("FIXME: Title")
    lines.append(f"[variant {variant}]")
    if intent:
        lines.append(f"[intent {intent}]")
    lines.append(f"[stat {stat}]")
    lines.append(f"[tier {tier}]")
    lines.append("")
    lines.append("FIXME: body text")

    # Stats
    resistance = sum(
        progress_value(a) for a in path
    ) if path else "FIXME"
    # For combat we already set resistance; for traverse it's the path sum
    # We'll compute this externally
    # ... actually resistance is an input to path generation, not derived from it
    # This gets filled in by the caller
    lines.append("")
    lines.append("stats:")
    lines.append("  resistance FIXME")

    # Timers
    lines.append("")
    lines.append("timers:")
    lines.append(f"  draw {draw}")
    for effect, damage, countdown in timers:
        lines.append(f"  * FIXME [counter FIXME]: {effect} {damage} every {countdown}")

    # Openings
    lines.append("")
    lines.append("openings:")
    for arch in openings:
        lines.append(f"  * FIXME: {arch}")

    # Path (traverse only)
    if path is not None:
        lines.append("")
        lines.append("path:")
        for arch in path:
            lines.append(f"  * FIXME: {arch}")

    # Approaches (combat only)
    if approaches is not None:
        lines.append("")
        lines.append("approaches:")
        for kind, params in approaches.items():
            parts = ", ".join(f"{k} {v}" for k, v in params.items())
            lines.append(f"  * {kind}: {parts}")

    # Failure
    lines.append("")
    lines.append("failure:")
    lines.append("  FIXME: failure text")
    lines.append("  FIXME: failure outcomes")
    lines.append("")

    return "\n".join(lines)


def progress_value(archetype):
    lookup = {
        "free_progress_small": 1,
        "momentum_to_progress": 2,
        "momentum_to_progress_large": 3,
        "momentum_to_progress_huge": 5,
        "spirits_to_progress": 3,
        "spirits_to_progress_large": 5,
        "threat_to_progress": 2,
        "threat_to_progress_large": 3,
    }
    return lookup.get(archetype, 0)


def estimate_resistance_timer_pressure(timers, resistance):
    """Estimate extra resistance from resistance-ticking timers.

    Assumes roughly (resistance / 2) turns to complete the encounter.
    Each resistance timer ticks once every `countdown` turns, adding `damage`.
    """
    turns = resistance // 2 + 1  # rough estimate
    extra = 0
    for effect, damage, countdown in timers:
        if effect == "resistance":
            ticks = turns // countdown
            extra += ticks * damage
    return extra


def generate(variant, stat, tier, intent=None, seed=None):
    rng = random.Random(seed)
    td = TIER[tier]

    # Timers
    draw, timers, res_timer_count = generate_timers(rng, td)

    # Resistance
    resistance = pick_range(rng, td, "resistance")

    if variant == "traverse":
        # Openings — momentum only
        opening_count = pick_range(rng, td, "openings_traverse")
        openings = generate_traverse_openings(rng, tier, opening_count)

        # Path — progress only, sum >= resistance + timer pressure
        card_count = pick_range(rng, td, "path_cards")
        extra_pressure = estimate_resistance_timer_pressure(timers, resistance)
        target = resistance + extra_pressure
        path = generate_path(rng, tier, target, card_count)
        actual_sum = sum(progress_value(a) for a in path)

        approaches = None

    elif variant == "combat":
        # Openings — mixed
        opening_count = pick_range(rng, td, "openings_combat")
        openings = generate_combat_openings(rng, tier, opening_count)

        path = None
        approaches = generate_approaches(td)

    else:
        raise ValueError(f"Unknown variant: {variant}")

    # Build the output
    lines = []
    lines.append("FIXME: Title")
    lines.append(f"[variant {variant}]")
    if intent:
        lines.append(f"[intent {intent}]")
    lines.append(f"[stat {stat}]")
    lines.append(f"[tier {tier}]")
    lines.append("")
    lines.append("FIXME: body text")
    lines.append("")
    lines.append("stats:")
    lines.append(f"  resistance {resistance}")
    lines.append("")
    lines.append("timers:")
    lines.append(f"  draw {draw}")
    for effect, damage, countdown in timers:
        lines.append(f"  * FIXME [counter FIXME]: {effect} {damage} every {countdown}")
    lines.append("")
    lines.append("openings:")
    for arch in openings:
        lines.append(f"  * FIXME: {arch}")

    if path is not None:
        lines.append("")
        lines.append("path:")
        for arch in path:
            lines.append(f"  * FIXME: {arch}")
        # Annotate the balance check
        lines.append(f"  # path sum: {actual_sum} (resistance: {resistance}"
                      + (f" + ~{extra_pressure} timer pressure" if extra_pressure else "")
                      + ")")

    if approaches is not None:
        lines.append("")
        lines.append("approaches:")
        for kind, params in approaches.items():
            parts = ", ".join(f"{k} {v}" for k, v in params.items())
            lines.append(f"  * {kind}: {parts}")

    lines.append("")
    lines.append("failure:")
    lines.append("  FIXME: failure text")
    lines.append("  FIXME: failure outcomes")
    lines.append("")

    return "\n".join(lines)


def main():
    parser = argparse.ArgumentParser(
        description="Generate balanced .tac skeleton files.",
        epilog="Example: python sim/tac_skeleton.py traverse bushcraft 1",
    )
    parser.add_argument("variant", choices=["combat", "traverse"])
    parser.add_argument("stat", choices=VALID_STATS)
    parser.add_argument("tier", type=int, choices=[1, 2, 3])
    parser.add_argument("--seed", type=int, default=None)
    parser.add_argument("--intent", type=str, default=None)

    args = parser.parse_args()
    output = generate(args.variant, args.stat, args.tier,
                      intent=args.intent, seed=args.seed)
    print(output)


if __name__ == "__main__":
    main()
