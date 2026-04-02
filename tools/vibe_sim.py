"""
Vibe Monte Carlo for tactical encounters.

Defines two platonic 15-card decks — one cancel-focused, one aggro-focused —
that represent the gold standard "this is what fun feels like." Then measures
how much vibe degrades as good cards are swapped for chaff.

Per-turn vibe metrics:
  - choice:  was there a meaningful decision? (0 = obvious, 1 = genuinely hard)
  - tension: how close is the timer to firing? (0 = safe, 1 = about to fire)
  - juice:   how satisfying was the card played? (0 = chaff, 1 = haymaker/cancel)
  - weight:  rolling frustration from consecutive bad turns (0 = fine, negative = oof)
  - triumph: did you clear a timer this turn? (0 or 1)

Usage:
    python3 tools/vibe_sim.py                          # both platonic decks
    python3 tools/vibe_sim.py --deck aggro             # just aggro
    python3 tools/vibe_sim.py --deck cancel --degrade 3  # swap 3 best cards for chaff
    python3 tools/vibe_sim.py --sweep                  # degrade 0..10 for both decks
"""

import random
import argparse
from dataclasses import dataclass, field
from typing import Optional

# ── Card archetypes ──────────────────────────────────────────────

ARCHETYPES = {
    "free_progress_small":        {"cost": ("free", 0),      "effect": ("progress", 1)},
    "momentum_to_progress":       {"cost": ("momentum", 1),  "effect": ("progress", 2)},
    "momentum_to_progress_large": {"cost": ("momentum", 2),  "effect": ("progress", 3)},
    "momentum_to_progress_huge":  {"cost": ("momentum", 3),  "effect": ("progress", 5)},
    "spirits_to_progress":        {"cost": ("spirits", 1),   "effect": ("progress", 3)},
    "threat_to_progress":         {"cost": ("threat", 0),    "effect": ("progress", 2)},
    "threat_to_progress_large":   {"cost": ("threat", 0),    "effect": ("progress", 3)},
    "free_momentum":              {"cost": ("free", 0),      "effect": ("momentum", 2)},
    "free_momentum_small":        {"cost": ("free", 0),      "effect": ("momentum", 1)},
    "spirits_to_momentum":        {"cost": ("spirits", 1),   "effect": ("momentum", 3)},
    "momentum_to_cancel":         {"cost": ("momentum", 2),  "effect": ("cancel", 0)},
    "spirits_to_cancel":          {"cost": ("spirits", 1),   "effect": ("cancel", 0)},
    "free_cancel":                {"cost": ("free", 0),      "effect": ("cancel", 0)},
}

# Juice ratings — how satisfying does playing this card feel?
JUICE = {
    "free_progress_small":        0.10,
    "free_momentum_small":        0.10,
    "free_momentum":              0.25,
    "momentum_to_progress":       0.40,
    "spirits_to_momentum":        0.20,
    "threat_to_progress":         0.50,
    "threat_to_progress_large":   0.65,
    "momentum_to_progress_large": 0.70,
    "spirits_to_progress":        0.55,
    "momentum_to_progress_huge":  1.00,
    "momentum_to_cancel":         0.85,
    "spirits_to_cancel":          0.80,
    "free_cancel":                1.00,
}

# Quality tier for degradation — higher = gets swapped out first
QUALITY_TIER = {
    "free_cancel":                5,
    "momentum_to_progress_huge":  5,
    "spirits_to_cancel":          4,
    "momentum_to_cancel":         4,
    "momentum_to_progress_large": 4,
    "threat_to_progress_large":   4,
    "spirits_to_progress":        3,
    "threat_to_progress":         3,
    "spirits_to_momentum":        2,
    "momentum_to_progress":       2,
    "free_momentum":              1,
    "free_momentum_small":        0,
    "free_progress_small":        0,
}

CHAFF_CARD = ("Chaff", "free_progress_small")


@dataclass
class Card:
    name: str
    archetype: str
    cost_kind: str
    cost_amount: int
    effect_kind: str
    effect_amount: int
    juice: float

    @staticmethod
    def from_archetype(name: str, archetype: str) -> "Card":
        a = ARCHETYPES[archetype]
        return Card(
            name=name, archetype=archetype,
            cost_kind=a["cost"][0], cost_amount=a["cost"][1],
            effect_kind=a["effect"][0], effect_amount=a["effect"][1],
            juice=JUICE.get(archetype, 0.3),
        )


# ── Platonic decks ───────────────────────────────────────────────
#
# These are hand-crafted "what does fun feel like" decks. 15 cards each.
# No gear, no skills — just the ideal card mix for each playstyle.

PLATONIC_CANCEL = [
    # The cancel engine (5 cards) — your win condition
    ("Find an opening", "momentum_to_cancel"),
    ("Exploit their weakness", "momentum_to_cancel"),
    ("Go for the throat", "spirits_to_cancel"),
    ("The killing strike", "free_cancel"),
    ("Slip past their guard", "momentum_to_cancel"),
    # Momentum fuel (4 cards) — feeds the cancel costs
    ("Circle and wait", "free_momentum"),
    ("Read the situation", "free_momentum"),
    ("Bide your time", "free_momentum"),
    ("Test their patience", "free_momentum"),
    # Light progress (3 cards) — chip damage between cancels
    ("Quick jab", "momentum_to_progress"),
    ("Probing strike", "momentum_to_progress"),
    ("Opportunistic cut", "momentum_to_progress"),
    # Chaff (3 cards) — the filler tax
    ("Desperate lunge", "free_progress_small"),
    ("Scramble forward", "free_progress_small"),
    ("Brace yourself", "free_progress_small"),
]

PLATONIC_AGGRO = [
    # The damage engine (5 cards) — your win condition
    ("Devastating blow", "momentum_to_progress_huge"),
    ("Brutal chop", "momentum_to_progress_large"),
    ("Committed strike", "momentum_to_progress_large"),
    ("Reckless swing", "threat_to_progress_large"),
    ("All-out assault", "momentum_to_progress_large"),
    # Momentum fuel (4 cards) — feeds the big spends
    ("Wind up", "free_momentum"),
    ("Shift your grip", "free_momentum"),
    ("Ready yourself", "free_momentum"),
    ("Plant your feet", "free_momentum"),
    # Bread and butter (3 cards) — solid mid-range damage
    ("Swing hard", "momentum_to_progress"),
    ("Press the attack", "momentum_to_progress"),
    ("Follow through", "momentum_to_progress"),
    # Chaff (3 cards) — the filler tax
    ("Wild swing", "free_progress_small"),
    ("Glancing blow", "free_progress_small"),
    ("Stumble forward", "free_progress_small"),
]


def build_platonic(deck_spec: list[tuple[str, str]]) -> list[Card]:
    return [Card.from_archetype(name, arch) for name, arch in deck_spec]


def degrade_deck(deck_spec: list[tuple[str, str]], n: int) -> list[Card]:
    """Replace the N highest-quality cards with chaff."""
    ranked = sorted(
        enumerate(deck_spec),
        key=lambda x: QUALITY_TIER.get(x[1][1], 0),
        reverse=True,
    )
    degraded = list(deck_spec)
    for i in range(min(n, len(ranked))):
        idx = ranked[i][0]
        degraded[idx] = CHAFF_CARD
    return [Card.from_archetype(name, arch) for name, arch in degraded]


# ── Timer ────────────────────────────────────────────────────────

@dataclass
class Timer:
    name: str
    effect: str  # "spirits" or "condition"
    amount: int
    countdown_max: int
    countdown: int
    resistance: int

    def tick(self):
        self.countdown -= 1
        return self.countdown <= 0

    def fire_and_reset(self):
        self.countdown = self.countdown_max


# ── Encounter ────────────────────────────────────────────────────

@dataclass
class Encounter:
    name: str
    timers: list[Timer]

    @staticmethod
    def platonic() -> "Encounter":
        return Encounter("Platonic", timers=[
            Timer("Spirits drain", "spirits", 1, 5, 5, resistance=6),
            Timer("Condition", "condition", 1, 6, 6, resistance=8),
        ])


# ── Simulation state ─────────────────────────────────────────────

@dataclass
class TurnVibe:
    turn: int
    choice: float
    tension: float
    juice: float
    weight: float
    triumph: float
    card_played: str
    action: str
    timer_fired: bool
    spirits_lost: int
    conditioned: bool


@dataclass
class SimState:
    deck: list[Card]
    draw_index: int = 0
    momentum: int = 0
    spirits: int = 20
    timer_index: int = 0
    bad_streak: int = 0
    turn: int = 0

    def draw(self, rng: random.Random) -> Card:
        if self.draw_index >= len(self.deck):
            rng.shuffle(self.deck)
            self.draw_index = 0
        card = self.deck[self.draw_index]
        self.draw_index += 1
        return card

    def draw_n(self, n: int, rng: random.Random) -> list[Card]:
        return [self.draw(rng) for _ in range(n)]


# ── Player AI ────────────────────────────────────────────────────

def can_play(card: Card, state: SimState) -> bool:
    if card.cost_kind == "free":
        return True
    if card.cost_kind == "momentum":
        return state.momentum >= card.cost_amount
    if card.cost_kind == "spirits":
        return state.spirits >= card.cost_amount
    if card.cost_kind == "threat":
        return True
    return False


def card_value(card: Card, state: SimState, timer: Timer) -> float:
    if not can_play(card, state):
        return -1.0
    base = card.juice
    if card.effect_kind == "cancel":
        urgency = 1.0 - (timer.countdown / timer.countdown_max)
        base = 0.7 + 0.3 * urgency
    if card.effect_kind == "progress" and card.effect_amount >= timer.resistance:
        base = min(1.0, base + 0.3)
    if card.effect_kind == "momentum" and state.momentum >= 4:
        base *= 0.6
    if card.cost_kind == "spirits":
        base *= 0.7
    if card.cost_kind == "threat":
        base *= 0.85
    return base


def pick_best(cards: list[Card], state: SimState, timer: Timer) -> Optional[Card]:
    playable = [(c, card_value(c, state, timer)) for c in cards if can_play(c, state)]
    if not playable:
        return None
    playable.sort(key=lambda x: x[1], reverse=True)
    return playable[0][0]


def choice_complexity(hand: list[Card], state: SimState, timer: Timer) -> float:
    values = sorted(
        [card_value(c, state, timer) for c in hand if can_play(c, state)],
        reverse=True,
    )
    if len(values) <= 1:
        return 0.0
    gap = values[0] - values[1]
    return max(0.0, min(1.0, 1.0 - gap * 2))


# ── Simulation ───────────────────────────────────────────────────

PRESS_COST = 2
FORCE_COST = 2
BONUS_DRAW = 2


def simulate(
    encounter: Encounter,
    deck: list[Card],
    approach: str,
    rng: random.Random,
    max_turns: int = 30,
) -> list[TurnVibe]:
    timers = [
        Timer(t.name, t.effect, t.amount, t.countdown_max, t.countdown_max, t.resistance)
        for t in encounter.timers
    ]
    state = SimState(deck=list(deck))
    rng.shuffle(state.deck)

    momentum_per_turn = 2 if approach == "aggressive" else 1
    cards_per_draw = 2 if approach == "cautious" else 1
    vibes = []

    while state.turn < max_turns and state.timer_index < len(timers):
        state.turn += 1
        timer = timers[state.timer_index]

        # 1. Timer ticks
        timer_fired = timer.tick()
        spirits_lost = 0
        conditioned = False
        if timer_fired:
            if timer.effect == "spirits":
                spirits_lost = timer.amount
                state.spirits -= timer.amount
            elif timer.effect == "condition":
                conditioned = True
            timer.fire_and_reset()

        # 2. Gain momentum
        state.momentum += momentum_per_turn

        # 3. Draw
        hand = state.draw_n(cards_per_draw, rng)

        # 4. Decide: play or press/force?
        best = pick_best(hand, state, timer)
        action = "play"

        should_dig = best is None or card_value(best, state, timer) < 0.35

        if should_dig and state.momentum >= PRESS_COST:
            state.momentum -= PRESS_COST
            hand.extend(state.draw_n(BONUS_DRAW, rng))
            best = pick_best(hand, state, timer)
            action = "press"
        elif should_dig and best is None and state.spirits > FORCE_COST:
            state.spirits -= FORCE_COST
            hand.extend(state.draw_n(BONUS_DRAW, rng))
            best = pick_best(hand, state, timer)
            action = "force"

        if best is None and action != "force" and state.spirits > FORCE_COST:
            state.spirits -= FORCE_COST
            hand.extend(state.draw_n(BONUS_DRAW, rng))
            best = pick_best(hand, state, timer)
            action = "force"

        if best is None:
            best = hand[0] if hand else Card.from_archetype("Flail", "free_progress_small")
            action = "stuck"

        # 5. Play
        played = best
        if played.cost_kind == "momentum":
            state.momentum -= played.cost_amount
        elif played.cost_kind == "spirits":
            state.spirits -= played.cost_amount
        elif played.cost_kind == "threat":
            timer.countdown -= 1

        triumph = 0.0
        if played.effect_kind == "progress":
            timer.resistance -= played.effect_amount
        elif played.effect_kind == "momentum":
            state.momentum += played.effect_amount
        elif played.effect_kind == "cancel":
            timer.resistance = 0

        if timer.resistance <= 0:
            triumph = 1.0
            state.timer_index += 1

        # 6. Vibe scoring
        tension = 1.0 - (timer.countdown / timer.countdown_max) if timer.countdown_max > 0 else 0.5
        juice = played.juice
        choice = choice_complexity(hand, state, timer)

        is_bad_turn = juice < 0.25 or action in ("force", "stuck")
        if is_bad_turn:
            state.bad_streak += 1
        else:
            state.bad_streak = 0

        weight = 0.0 if state.bad_streak == 0 else (
            -0.2 * state.bad_streak - 0.1 * max(0, state.bad_streak - 1)
        )

        vibes.append(TurnVibe(
            turn=state.turn, choice=choice, tension=tension, juice=juice,
            weight=weight, triumph=triumph, card_played=played.archetype,
            action=action, timer_fired=timer_fired, spirits_lost=spirits_lost,
            conditioned=conditioned,
        ))

    return vibes


# ── Analysis ─────────────────────────────────────────────────────

def analyze(all_vibes: list[list[TurnVibe]], label: str, compact: bool = False):
    n_runs = len(all_vibes)
    max_turn = max(len(v) for v in all_vibes)

    lengths = [len(v) for v in all_vibes]
    avg_len = sum(lengths) / n_runs
    conditions = sum(1 for v in all_vibes if any(t.conditioned for t in v))
    total_spirits = [sum(t.spirits_lost for t in v) for v in all_vibes]

    avg_juice_all = sum(t.juice for v in all_vibes for t in v) / sum(len(v) for v in all_vibes)
    avg_choice_all = sum(t.choice for v in all_vibes for t in v) / sum(len(v) for v in all_vibes)

    oof_runs = sum(1 for v in all_vibes if any(t.weight <= -0.7 for t in v))
    clicker_runs = sum(
        1 for v in all_vibes
        if any(
            all(v[i + j].choice < 0.15 for j in range(3))
            for i in range(max(0, len(v) - 2))
        )
    )
    drought_runs = sum(
        1 for v in all_vibes
        if any(
            all(v[i + j].juice < 0.25 for j in range(3))
            for i in range(max(0, len(v) - 2))
        )
    )

    if compact:
        print(
            f"  {label:<50}"
            f"  len={avg_len:>4.1f}"
            f"  juice={avg_juice_all:.2f}"
            f"  choice={avg_choice_all:.2f}"
            f"  cond={conditions / n_runs:>4.0%}"
            f"  oof={oof_runs / n_runs:>4.0%}"
            f"  clicker={clicker_runs / n_runs:>4.0%}"
            f"  drought={drought_runs / n_runs:>4.0%}"
        )
        return

    print(f"\n{'=' * 70}")
    print(f"  {label}  ({n_runs:,} runs)")
    print(f"{'=' * 70}")
    print(f"{'Turn':>4}  {'Choice':>7}  {'Tension':>7}  {'Juice':>7}  {'Weight':>7}  {'Triumph':>7}  {'Fired':>6}  {'Cond':>5}")
    print(f"{'─' * 4}  {'─' * 7}  {'─' * 7}  {'─' * 7}  {'─' * 7}  {'─' * 7}  {'─' * 6}  {'─' * 5}")

    for t in range(1, max_turn + 1):
        turns_at_t = [v[t - 1] for v in all_vibes if len(v) >= t]
        if not turns_at_t:
            break
        n = len(turns_at_t)
        pct_active = n / n_runs

        avg_choice = sum(v.choice for v in turns_at_t) / n
        avg_tension = sum(v.tension for v in turns_at_t) / n
        avg_juice = sum(v.juice for v in turns_at_t) / n
        avg_weight = sum(v.weight for v in turns_at_t) / n
        avg_triumph = sum(v.triumph for v in turns_at_t) / n
        pct_fired = sum(1 for v in turns_at_t if v.timer_fired) / n
        pct_cond = sum(1 for v in turns_at_t if v.conditioned) / n

        active_marker = f" ({pct_active:.0%})" if pct_active < 0.95 else ""
        print(
            f"  {t:>2}{active_marker:>5}"
            f"  {avg_choice:>7.2f}"
            f"  {avg_tension:>7.2f}"
            f"  {avg_juice:>7.2f}"
            f"  {avg_weight:>+7.2f}"
            f"  {avg_triumph:>7.2f}"
            f"  {pct_fired:>5.0%}"
            f"  {pct_cond:>4.0%}"
        )

    print(f"\n  Avg length:    {avg_len:.1f} turns")
    print(f"  Avg juice:     {avg_juice_all:.2f}")
    print(f"  Avg choice:    {avg_choice_all:.2f}")
    print(f"  Conditioned:   {conditions / n_runs:.1%}")
    print(f"  Spirits lost:  {sum(total_spirits) / n_runs:.1f} avg, {max(total_spirits)} worst")
    print(f"  Oof rate:      {oof_runs / n_runs:.1%} (weight <= -0.7)")
    print(f"  Clicker rate:  {clicker_runs / n_runs:.1%} (choice < 0.15 for 3+ turns)")
    print(f"  Juice drought: {drought_runs / n_runs:.1%} (juice < 0.25 for 3+ turns)")


# ── Main ─────────────────────────────────────────────────────────

def main():
    parser = argparse.ArgumentParser(description="Vibe Monte Carlo for tactical encounters")
    parser.add_argument("--trials", type=int, default=10_000)
    parser.add_argument("--deck", default=None, choices=["cancel", "aggro"])
    parser.add_argument("--approach", default=None, choices=["aggressive", "cautious"])
    parser.add_argument("--degrade", type=int, default=None, help="Replace N best cards with chaff")
    parser.add_argument("--sweep", action="store_true", help="Degrade 0..10 for both decks")
    parser.add_argument("--seed", type=int, default=42)
    args = parser.parse_args()

    decks = {
        "cancel": (PLATONIC_CANCEL, "cautious"),
        "aggro":  (PLATONIC_AGGRO, "aggressive"),
    }

    if args.sweep:
        for deck_name in ["aggro", "cancel"]:
            spec, natural_approach = decks[deck_name]
            approach = args.approach or natural_approach
            print(f"\n{'=' * 90}")
            print(f"  SWEEP: {deck_name} / {approach} — degrading 0..10 cards")
            print(f"{'=' * 90}")
            for n in range(11):
                deck = degrade_deck(spec, n) if n > 0 else build_platonic(spec)
                rng = random.Random(args.seed)
                all_vibes = [simulate(Encounter.platonic(), deck, approach, rng) for _ in range(args.trials)]
                label = f"degrade={n:>2}"
                analyze(all_vibes, label, compact=True)
        return

    deck_names = [args.deck] if args.deck else ["cancel", "aggro"]

    for deck_name in deck_names:
        spec, natural_approach = decks[deck_name]
        approach = args.approach or natural_approach

        if args.degrade is not None:
            deck = degrade_deck(spec, args.degrade)
            label = f"{deck_name} / {approach} / degrade={args.degrade}"
        else:
            deck = build_platonic(spec)
            label = f"{deck_name} / {approach} / platonic"

        rng = random.Random(args.seed)
        all_vibes = [simulate(Encounter.platonic(), deck, approach, rng) for _ in range(args.trials)]
        analyze(all_vibes, label)


if __name__ == "__main__":
    main()
