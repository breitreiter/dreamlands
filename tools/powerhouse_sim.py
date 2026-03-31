"""
Powerhouse build simulator for tactical encounters.

New framing: powerhouse always wins the resistance kill. The questions are:
  1. How many spirits did you spend? (session budget)
  2. How many times did the condition timer fire? (did you get irradiated?)

Timers:
  - "drain" timers: spirits -1, annoying but manageable
  - "condition" timer: fires once = you got irradiated/poisoned/cursed, game-threatening

Turn structure:
  1. Timers tick, fire if they reach 0 (drain timers reset, condition fires once then stops)
  2. Gain 1 momentum
  3. See openings (1 if played last turn, 3 if dug last turn)
  4. Choose ONE: play a card OR dig (pay 2M or 2S)

Usage:
    python tools/powerhouse_sim.py
"""

import random
from dataclasses import dataclass, field
from collections import Counter

DECK_SIZE = 15
DIG_COST_M = 2
DIG_COST_S = 2
DIG_OPENINGS = 3
PLAY_OPENINGS = 1
MOMENTUM_PER_TURN = 1
TRIALS = 50_000


@dataclass
class Timer:
    countdown: int
    current: int
    effect: str        # "spirits" or "condition"
    amount: int
    stopped: bool = False
    fired_count: int = 0

    def tick(self):
        if self.stopped:
            return None
        self.current -= 1
        if self.current <= 0:
            self.fired_count += 1
            if self.effect == "condition":
                self.stopped = True  # condition fires once
            else:
                self.current = self.countdown  # drain resets
            return (self.effect, self.amount)
        return None


@dataclass
class Card:
    kind: str
    cost_kind: str     # "momentum", "spirits", "free", "tick"
    cost: int
    effect: str        # "damage", "momentum", "quick_momentum"
    amount: int


# Card templates
FREE_PROGRESS_SMALL = Card("free_progress_small", "free", 0, "damage", 1)
M_TO_PROGRESS = Card("m_to_progress", "momentum", 1, "damage", 2)
M_TO_PROGRESS_L = Card("m_to_progress_large", "momentum", 2, "damage", 3)
M_TO_PROGRESS_H = Card("m_to_progress_huge", "momentum", 3, "damage", 5)
S_TO_PROGRESS = Card("s_to_progress", "spirits", 1, "damage", 3)
S_TO_PROGRESS_L = Card("s_to_progress_large", "spirits", 2, "damage", 5)
THREAT_TO_PROGRESS = Card("threat_to_progress", "tick", 0, "damage", 2)
THREAT_TO_PROGRESS_L = Card("threat_to_progress_large", "tick", 0, "damage", 3)

FREE_MOMENTUM = Card("free_momentum", "free", 0, "momentum", 2)
FREE_MOMENTUM_SMALL = Card("free_momentum_small", "free", 0, "momentum", 1)
S_TO_MOMENTUM = Card("s_to_momentum", "spirits", 1, "momentum", 3)

QUICK_MOMENTUM = Card("quick_momentum", "free", 0, "quick_momentum", 1)


@dataclass
class SimState:
    deck: list[Card]
    draw_index: int = 0
    momentum: int = 0
    spirits_spent: int = 0     # track total spirits spent (on cards + dig)
    resistance: int = 6
    timers: list[Timer] = field(default_factory=list)
    turn: int = 0
    total_damage: int = 0
    conditioned: bool = False   # did the condition timer fire?

    def draw(self, rng: random.Random) -> Card:
        if self.draw_index >= len(self.deck):
            rng.shuffle(self.deck)
            self.draw_index = 0
        card = self.deck[self.draw_index]
        self.draw_index += 1
        return card

    def can_afford(self, card: Card) -> bool:
        if card.cost_kind in ("free", "tick"):
            return True
        if card.cost_kind == "momentum":
            return self.momentum >= card.cost
        if card.cost_kind == "spirits":
            return True  # always willing to spend spirits for damage
        return False

    def can_dig(self) -> bool:
        return self.momentum >= DIG_COST_M  # prefer momentum for dig

    def can_force_dig(self) -> bool:
        return True  # always *can* spend spirits to dig

    def pay_dig(self):
        if self.momentum >= DIG_COST_M:
            self.momentum -= DIG_COST_M
        else:
            self.spirits_spent += DIG_COST_S

    def play(self, card: Card):
        if card.cost_kind == "momentum":
            self.momentum -= card.cost
        elif card.cost_kind == "spirits":
            self.spirits_spent += card.cost

        if card.effect == "damage":
            self.resistance = max(0, self.resistance - card.amount)
            self.total_damage += card.amount
        elif card.effect == "momentum":
            self.momentum += card.amount


def build_deck(collection: list[Card], quick_m: int, rng: random.Random) -> list[Card]:
    cards = list(collection)
    cards.extend([QUICK_MOMENTUM] * quick_m)
    filler = [FREE_MOMENTUM_SMALL, FREE_PROGRESS_SMALL, M_TO_PROGRESS]
    while len(cards) < DECK_SIZE:
        cards.append(rng.choice(filler))
    rng.shuffle(cards)
    return cards


def pick_best_play(openings: list[Card], state: SimState) -> Card | None:
    """Powerhouse strategy: play highest damage affordable card, prefer momentum-cost over spirits-cost."""
    # Momentum-costed damage first (free resource)
    m_damage = [(c, c.amount) for c in openings
                if c.effect == "damage" and c.cost_kind in ("free", "momentum", "tick")
                and state.can_afford(c)]
    if m_damage:
        return max(m_damage, key=lambda x: x[1])[0]

    # Spirits-costed damage only if we need to race
    s_damage = [(c, c.amount) for c in openings
                if c.effect == "damage" and c.cost_kind == "spirits"]
    if s_damage:
        return max(s_damage, key=lambda x: x[1])[0]

    # Momentum generators
    momentum_cards = [c for c in openings if c.effect == "momentum" and state.can_afford(c)]
    if momentum_cards:
        return max(momentum_cards, key=lambda c: c.amount)

    return None


def simulate_once(
    collection: list[Card],
    resistance: int,
    drain_timers: list[tuple],      # [(countdown, amount), ...]
    condition_timer: int | None,     # countdown for the condition timer, or None
    approach_momentum: int = 0,
    approach_bonus: bool = True,
    rng: random.Random = None,
    trace: bool = False,
    quick_m: int = 0,
) -> dict:
    deck = build_deck(collection, quick_m, rng)

    timers = []
    for cd, amt in drain_timers:
        timers.append(Timer(countdown=cd, current=cd, effect="spirits", amount=amt))
    if condition_timer is not None:
        timers.append(Timer(countdown=condition_timer, current=condition_timer, effect="condition", amount=0))

    state = SimState(
        deck=deck,
        momentum=approach_momentum,
        resistance=resistance,
        timers=timers,
    )

    next_openings = DIG_OPENINGS if approach_bonus else PLAY_OPENINGS
    max_turns = 40

    while state.turn < max_turns:
        state.turn += 1

        # 1. Tick timers
        for timer in state.timers:
            result = timer.tick()
            if result:
                eff, amt = result
                if eff == "spirits":
                    state.spirits_spent += amt  # drain counts as spirits spent
                elif eff == "condition":
                    state.conditioned = True

        # 2. Passive momentum
        state.momentum += MOMENTUM_PER_TURN

        # 3. Draw
        openings = [state.draw(rng) for _ in range(next_openings)]

        # 4a. Resolve quick_momentum
        quick_count = sum(1 for c in openings if c.effect == "quick_momentum")
        state.momentum += quick_count
        remaining = [c for c in openings if c.effect != "quick_momentum"]

        quick_str = f" +{quick_count}qm" if quick_count else ""

        # 4b. Pick action
        best = pick_best_play(remaining, state)

        if best and best.effect == "damage":
            state.play(best)
            next_openings = PLAY_OPENINGS

            if trace:
                timer_str = " ".join(
                    f"{'X' if t.stopped else t.current}{'!' if t.effect == 'condition' else ''}"
                    for t in state.timers)
                print(f"  T{state.turn}: see {len(openings)}{quick_str}, PLAY {best.kind} (-{best.amount}R) "
                      f"M={state.momentum} spent={state.spirits_spent}S R={state.resistance} "
                      f"timers=[{timer_str}]{'  ☠ CONDITIONED' if state.conditioned else ''}")

            if state.resistance <= 0:
                return {
                    "turns": state.turn,
                    "spirits_spent": state.spirits_spent,
                    "conditioned": state.conditioned,
                }

        elif state.can_dig():
            state.pay_dig()
            next_openings = DIG_OPENINGS
            if trace:
                timer_str = " ".join(
                    f"{'X' if t.stopped else t.current}{'!' if t.effect == 'condition' else ''}"
                    for t in state.timers)
                print(f"  T{state.turn}: see {len(openings)}{quick_str}, DIG(M) "
                      f"M={state.momentum} spent={state.spirits_spent}S R={state.resistance} "
                      f"timers=[{timer_str}]")

        elif state.can_force_dig():
            state.pay_dig()
            next_openings = DIG_OPENINGS
            if trace:
                timer_str = " ".join(
                    f"{'X' if t.stopped else t.current}{'!' if t.effect == 'condition' else ''}"
                    for t in state.timers)
                print(f"  T{state.turn}: see {len(openings)}{quick_str}, DIG(S) "
                      f"M={state.momentum} spent={state.spirits_spent}S R={state.resistance} "
                      f"timers=[{timer_str}]")

        elif best:
            state.play(best)
            next_openings = PLAY_OPENINGS
            if trace:
                print(f"  T{state.turn}: see {len(openings)}{quick_str}, PLAY {best.kind} "
                      f"M={state.momentum} spent={state.spirits_spent}S R={state.resistance}")
        else:
            next_openings = PLAY_OPENINGS

    return {"turns": state.turn, "spirits_spent": state.spirits_spent, "conditioned": state.conditioned}


def run_scenario(
    label: str,
    collection: list[Card],
    resistance: int,
    drain_timers: list[tuple],
    condition_timer: int | None = None,
    approach: str = "direct",
    trials: int = TRIALS,
    trace_count: int = 0,
    quick_m: int = 0,
):
    approach_momentum = {"scout": 0, "direct": 3, "wild": 6}[approach]
    approach_bonus = approach == "scout"

    qm_desc = f" +{quick_m}qm" if quick_m else ""
    drain_desc = "/".join(f"cd{cd}" for cd, _ in drain_timers) if drain_timers else "none"
    cond_desc = f" cond@{condition_timer}" if condition_timer else ""

    rng = random.Random(42)

    if trace_count > 0:
        print(f"\n--- Traces: {label}{qm_desc} R={resistance} drain=[{drain_desc}]{cond_desc} [{approach}] ---")
        for i in range(trace_count):
            print(f"\n  Game {i+1}:")
            simulate_once(collection, resistance, drain_timers, condition_timer,
                          approach_momentum, approach_bonus,
                          random.Random(rng.randint(0, 999999)), trace=True,
                          quick_m=quick_m)

    rng = random.Random(42)
    results = []
    for _ in range(trials):
        results.append(simulate_once(
            collection, resistance, drain_timers, condition_timer,
            approach_momentum, approach_bonus, rng, quick_m=quick_m))

    turns = [r["turns"] for r in results]
    spirits = [r["spirits_spent"] for r in results]
    conditioned = sum(1 for r in results if r["conditioned"])

    turns.sort()
    spirits.sort()

    print(f"\n{'='*65}")
    print(f"  {label}{qm_desc}  R={resistance}  drain=[{drain_desc}]{cond_desc}  [{approach}]")
    print(f"{'='*65}")
    print(f"  Turns:   avg={sum(turns)/len(turns):.1f}  "
          f"p25={turns[len(turns)//4]}  p50={turns[len(turns)//2]}  p75={turns[3*len(turns)//4]}")
    print(f"  Spirits: avg={sum(spirits)/len(spirits):.1f}  "
          f"p25={spirits[len(spirits)//4]}  p50={spirits[len(spirits)//2]}  p75={spirits[3*len(spirits)//4]}")
    if condition_timer is not None:
        print(f"  CONDITIONED: {conditioned} ({100*conditioned/trials:.1f}%)")

    # Turn histogram
    hist = Counter(turns)
    max_count = max(hist.values())
    print(f"\n  Turn histogram:")
    for turn in range(min(turns), min(max(turns) + 1, 20)):
        count = hist.get(turn, 0)
        bar = "#" * int(40 * count / max_count) if max_count > 0 else ""
        print(f"    T{turn:>2}: {bar} {count}")

    # Spirits histogram
    s_hist = Counter(spirits)
    max_s = max(s_hist.values())
    print(f"\n  Spirits spent histogram:")
    for s in range(min(spirits), min(max(spirits) + 1, 15)):
        count = s_hist.get(s, 0)
        bar = "#" * int(40 * count / max_s) if max_s > 0 else ""
        print(f"    {s:>2}S: {bar} {count}")


if __name__ == "__main__":
    print("POWERHOUSE BUILD SIMULATOR")
    print("Tracks: turns to win, spirits spent, condition timer fires")
    print()

    # --- Deck archetypes ---
    berzerker = [
        M_TO_PROGRESS, M_TO_PROGRESS, M_TO_PROGRESS,
        M_TO_PROGRESS_L, M_TO_PROGRESS_L,
        M_TO_PROGRESS_H,
        FREE_MOMENTUM, FREE_MOMENTUM,
    ]

    grinder = [
        M_TO_PROGRESS, M_TO_PROGRESS, M_TO_PROGRESS, M_TO_PROGRESS,
        FREE_PROGRESS_SMALL, FREE_PROGRESS_SMALL,
        FREE_MOMENTUM, FREE_MOMENTUM,
    ]

    spirit_burner = [
        S_TO_PROGRESS, S_TO_PROGRESS,
        S_TO_PROGRESS_L,
        M_TO_PROGRESS, M_TO_PROGRESS,
        FREE_MOMENTUM, FREE_MOMENTUM,
        S_TO_MOMENTUM,
    ]

    threat_racer = [
        THREAT_TO_PROGRESS, THREAT_TO_PROGRESS,
        THREAT_TO_PROGRESS_L,
        M_TO_PROGRESS, M_TO_PROGRESS,
        M_TO_PROGRESS_L,
        FREE_MOMENTUM, FREE_MOMENTUM,
    ]

    # === Easy encounter: R=6, 2 drain timers, condition timer at 6 ===
    print("=== EASY: R=6, 2 drain (cd=4,5), condition@6 ===")
    drains_easy = [(4, 1), (5, 1)]
    for label, deck, approach in [
        ("berzerker", berzerker, "wild"),
        ("berzerker", berzerker, "direct"),
        ("grinder", grinder, "direct"),
        ("spirit_burner", spirit_burner, "direct"),
        ("threat_racer", threat_racer, "direct"),
    ]:
        run_scenario(label, deck, resistance=6, drain_timers=drains_easy,
                     condition_timer=6, approach=approach)

    # Trace a few berzerker wild games
    run_scenario("berzerker", berzerker, resistance=6, drain_timers=drains_easy,
                 condition_timer=6, approach="wild", trace_count=3)

    # === Medium: R=8, 2 drain, condition@5 ===
    print("\n\n=== MEDIUM: R=8, 2 drain (cd=3,4), condition@5 ===")
    drains_med = [(3, 1), (4, 1)]
    for label, deck, approach in [
        ("berzerker", berzerker, "wild"),
        ("berzerker", berzerker, "direct"),
        ("grinder", grinder, "direct"),
        ("spirit_burner", spirit_burner, "direct"),
        ("threat_racer", threat_racer, "direct"),
    ]:
        run_scenario(label, deck, resistance=8, drain_timers=drains_med,
                     condition_timer=5, approach=approach)

    # === Hard: R=8, 3 drain, condition@4 ===
    print("\n\n=== HARD: R=8, 3 drain (cd=3,4,5), condition@4 ===")
    drains_hard = [(3, 1), (4, 1), (5, 1)]
    for label, deck, approach in [
        ("berzerker", berzerker, "wild"),
        ("berzerker", berzerker, "direct"),
        ("spirit_burner", spirit_burner, "direct"),
        ("threat_racer", threat_racer, "direct"),
    ]:
        run_scenario(label, deck, resistance=8, drain_timers=drains_hard,
                     condition_timer=4, approach=approach)

    # Trace hard berzerker
    run_scenario("berzerker", berzerker, resistance=8, drain_timers=drains_hard,
                 condition_timer=4, approach="wild", trace_count=3)

    # === Quick momentum impact ===
    print("\n\n=== QUICK_M (berzerker, medium, wild) ===")
    for n in [0, 2, 4]:
        run_scenario("berzerker", berzerker, resistance=8, drain_timers=drains_med,
                     condition_timer=5, approach="wild", quick_m=n)

    # === Scary condition timers - how tight can we make it? ===
    print("\n\n=== CONDITION TIMER SWEEP (berzerker, R=6, 1 drain cd=4, wild) ===")
    for ct in [4, 5, 6, 7, 8]:
        run_scenario("berzerker", berzerker, resistance=6, drain_timers=[(4, 1)],
                     condition_timer=ct, approach="wild")
