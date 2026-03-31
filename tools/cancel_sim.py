"""
Cancel-build simulator for tactical encounters.

Turn structure:
  1. Timers tick, fire if they reach 0
  2. Gain 1 momentum
  3. See openings: 1 if you played last turn, 3 if you dug (Press/Force) last turn
  4. Choose ONE: play a card OR dig (pay 2M or 2S, see 3 next turn)
  5. Unplayed cards are discarded

Press (2M) and Force (2S) are the same action, just different resource.
Scout approach: 0M, 3 openings turn 1.

Usage:
    python tools/cancel_sim.py
"""

import random
from dataclasses import dataclass, field
from collections import Counter

DECK_SIZE = 15
DIG_COST_M = 2        # Press costs 2 momentum
DIG_COST_S = 2        # Force costs 2 spirits
DIG_OPENINGS = 3
PLAY_OPENINGS = 1     # after playing a card, see 1 next turn
MOMENTUM_PER_TURN = 1
TRIALS = 50_000


@dataclass
class Timer:
    countdown: int
    current: int
    effect: str
    amount: int
    stopped: bool = False

    def tick(self):
        if self.stopped:
            return None
        self.current -= 1
        if self.current <= 0:
            self.current = self.countdown
            return (self.effect, self.amount)
        return None


@dataclass
class Card:
    kind: str
    cost_kind: str     # "momentum", "spirits", "free"
    cost: int
    effect: str        # "stop_timer", "momentum", "progress"
    effect_amount: int


CANCEL_M = Card("cancel_m", "momentum", 2, "stop_timer", 0)
CANCEL_S = Card("cancel_s", "spirits", 1, "stop_timer", 0)
CANCEL_F = Card("cancel_f", "free", 0, "stop_timer", 0)
FREE_MOMENTUM = Card("free_momentum", "free", 0, "momentum", 2)
FREE_MOMENTUM_SMALL = Card("free_momentum_small", "free", 0, "momentum", 1)
FREE_PROGRESS_SMALL = Card("free_progress_small", "free", 0, "progress", 1)
MOMENTUM_TO_PROGRESS = Card("momentum_to_progress", "momentum", 1, "progress", 2)
QUICK_MOMENTUM = Card("quick_momentum", "free", 0, "quick_momentum", 1)  # doesn't end turn


@dataclass
class SimState:
    deck: list[Card]
    draw_index: int = 0
    momentum: int = 0
    spirits: int = 10
    timers: list[Timer] = field(default_factory=list)
    turn: int = 0
    cancels_played: int = 0

    def draw(self, rng: random.Random) -> Card:
        if self.draw_index >= len(self.deck):
            rng.shuffle(self.deck)
            self.draw_index = 0
        card = self.deck[self.draw_index]
        self.draw_index += 1
        return card

    def active_timers(self) -> int:
        return sum(1 for t in self.timers if not t.stopped)

    def can_afford(self, card: Card) -> bool:
        if card.cost_kind == "free":
            return True
        if card.cost_kind == "momentum":
            return self.momentum >= card.cost
        if card.cost_kind == "spirits":
            return self.spirits >= card.cost
        return False

    def can_dig(self) -> bool:
        return self.momentum >= DIG_COST_M or self.spirits >= DIG_COST_S + 1  # keep 1 spirit buffer

    def pay_dig(self):
        if self.momentum >= DIG_COST_M:
            self.momentum -= DIG_COST_M
        else:
            self.spirits -= DIG_COST_S

    def play(self, card: Card):
        if card.cost_kind == "momentum":
            self.momentum -= card.cost
        elif card.cost_kind == "spirits":
            self.spirits -= card.cost

        if card.effect == "stop_timer":
            urgent = min((t for t in self.timers if not t.stopped), key=lambda t: t.current)
            urgent.stopped = True
            self.cancels_played += 1
        elif card.effect == "momentum":
            self.momentum += card.effect_amount
        elif card.effect == "progress":
            pass  # don't care about resistance for control kill sim


def build_deck(cancels: list[Card], quick_m: int, rng: random.Random) -> list[Card]:
    cards = list(cancels)
    cards.extend([QUICK_MOMENTUM] * quick_m)
    filler = [FREE_MOMENTUM, FREE_MOMENTUM_SMALL, FREE_PROGRESS_SMALL, MOMENTUM_TO_PROGRESS]
    while len(cards) < DECK_SIZE:
        cards.append(rng.choice(filler))
    rng.shuffle(cards)
    return cards


def make_timers(count: int, countdown: int, effect: str, amount: int,
                stagger: bool = False) -> list[Timer]:
    if stagger and count > 1:
        # Spread countdowns around the base, e.g. base=4, count=3 → [3,4,5]
        start = countdown - (count - 1) // 2
        start = max(2, start)  # floor at 2
        return [Timer(countdown=start + i, current=start + i, effect=effect, amount=amount)
                for i in range(count)]
    return [Timer(countdown=countdown, current=countdown, effect=effect, amount=amount)
            for _ in range(count)]


def simulate_once(
    cancels: list[Card],
    timer_count: int,
    timer_countdown: int,
    timer_effect: str,
    timer_amount: int,
    starting_spirits: int,
    rng: random.Random,
    trace: bool = False,
    stagger: bool = False,
    quick_m: int = 0,
) -> dict:
    deck = build_deck(cancels, quick_m, rng)
    state = SimState(
        deck=deck,
        momentum=0,
        spirits=starting_spirits,
        timers=make_timers(timer_count, timer_countdown, timer_effect, timer_amount, stagger),
    )

    # Scout: see 3 on turn 1
    next_openings = DIG_OPENINGS
    max_turns = 40
    cards_seen = 0

    while state.turn < max_turns:
        state.turn += 1

        # 1. Tick timers
        for timer in state.timers:
            result = timer.tick()
            if result:
                eff, amt = result
                if eff == "spirits":
                    state.spirits = max(0, state.spirits - amt)

        if state.spirits <= 0:
            if trace:
                print(f"  T{state.turn}: SPIRITS LOSS")
            return {"result": "spirits_loss", "turns": state.turn,
                    "cancels_played": state.cancels_played, "cards_seen": cards_seen}

        # 2. Passive momentum
        state.momentum += MOMENTUM_PER_TURN

        # 3. See openings
        openings = [state.draw(rng) for _ in range(next_openings)]
        cards_seen += len(openings)

        # 4a. Resolve all quick_momentum cards first (they don't end the turn)
        quick_count = sum(1 for c in openings if c.effect == "quick_momentum")
        state.momentum += quick_count
        remaining = [c for c in openings if c.effect != "quick_momentum"]

        # 4b. Now make the real decision with remaining openings
        affordable_cancels = [c for c in remaining if c.effect == "stop_timer" and state.can_afford(c)]

        quick_str = f" +{quick_count}qm" if quick_count else ""

        if affordable_cancels and state.active_timers() > 0:
            cancel = min(affordable_cancels, key=lambda c: (0 if c.cost_kind == "free" else c.cost))
            state.play(cancel)
            next_openings = PLAY_OPENINGS

            if trace:
                timer_str = " ".join(f"{'X' if t.stopped else t.current}" for t in state.timers)
                print(f"  T{state.turn}: see {len(openings)}{quick_str}, PLAY {cancel.kind} "
                      f"M={state.momentum} S={state.spirits} timers=[{timer_str}]")

            if state.active_timers() == 0:
                return {"result": "control_kill", "turns": state.turn,
                        "cancels_played": state.cancels_played, "spirits_remaining": state.spirits,
                        "cards_seen": cards_seen}

        elif state.can_dig() and state.active_timers() > 0:
            state.pay_dig()
            next_openings = DIG_OPENINGS

            if trace:
                timer_str = " ".join(f"{'X' if t.stopped else t.current}" for t in state.timers)
                print(f"  T{state.turn}: see {len(openings)}{quick_str}, DIG "
                      f"M={state.momentum} S={state.spirits} timers=[{timer_str}]")

        else:
            # Can't dig, play best filler from remaining
            momentum_cards = [c for c in remaining if c.effect == "momentum" and state.can_afford(c)]
            if momentum_cards:
                best = max(momentum_cards, key=lambda c: c.effect_amount)
                state.play(best)
                if trace:
                    print(f"  T{state.turn}: see {len(openings)}{quick_str}, PLAY {best.kind} "
                          f"M={state.momentum} S={state.spirits}")
            else:
                if trace:
                    print(f"  T{state.turn}: see {len(openings)}{quick_str}, WASTE "
                          f"M={state.momentum} S={state.spirits}")
            next_openings = PLAY_OPENINGS

    return {"result": "timeout", "turns": max_turns, "cancels_played": state.cancels_played,
            "cards_seen": cards_seen}


def run_scenario(
    cancels: list[Card],
    timer_count: int,
    timer_countdown: int = 4,
    timer_effect: str = "spirits",
    timer_amount: int = 1,
    starting_spirits: int = 10,
    trials: int = TRIALS,
    trace_count: int = 0,
    stagger: bool = False,
    quick_m: int = 0,
):
    cancel_desc = ", ".join(c.kind for c in cancels)
    stag_desc = " staggered" if stagger else ""
    qm_desc = f" +{quick_m}qm" if quick_m else ""
    rng = random.Random(42)

    if trace_count > 0:
        print(f"\n--- Traces: {cancel_desc}{qm_desc} vs {timer_count}t (cd={timer_countdown}{stag_desc}) ---")
        for i in range(trace_count):
            print(f"\n  Game {i+1}:")
            simulate_once(cancels, timer_count, timer_countdown, timer_effect, timer_amount,
                          starting_spirits, random.Random(rng.randint(0, 999999)), trace=True,
                          stagger=stagger, quick_m=quick_m)

    rng = random.Random(42)
    results = []
    for _ in range(trials):
        results.append(simulate_once(
            cancels, timer_count, timer_countdown, timer_effect, timer_amount,
            starting_spirits, rng, stagger=stagger, quick_m=quick_m))

    wins = [r for r in results if r["result"] == "control_kill"]
    losses = [r for r in results if r["result"] == "spirits_loss"]
    win_turns = [r["turns"] for r in wins]

    print(f"\n{'='*60}")
    print(f"  [{cancel_desc}{qm_desc}] vs {timer_count} timer(s)  "
          f"(cd={timer_countdown}{stag_desc}, {timer_effect} -{timer_amount})")
    print(f"  Spirits: {starting_spirits}  |  Deck: {DECK_SIZE}  |  Scout")
    print(f"{'='*60}")
    print(f"  Control kills: {len(wins):>6} ({100*len(wins)/trials:.1f}%)")
    print(f"  Spirits loss:  {len(losses):>6} ({100*len(losses)/trials:.1f}%)")

    if wins:
        avg_seen = sum(r["cards_seen"] for r in wins) / len(wins)
        print(f"  Avg cards seen (wins): {avg_seen:.1f}")

    if win_turns:
        win_turns.sort()
        p25 = win_turns[len(win_turns) // 4]
        p50 = win_turns[len(win_turns) // 2]
        p75 = win_turns[3 * len(win_turns) // 4]
        avg = sum(win_turns) / len(win_turns)
        print(f"\n  Win turns: avg={avg:.1f}  p25={p25}  p50={p50}  p75={p75}  "
              f"min={min(win_turns)}  max={max(win_turns)}")

        hist = Counter(win_turns)
        max_count = max(hist.values())
        print(f"\n  Histogram:")
        for turn in range(min(win_turns), min(max(win_turns) + 1, 25)):
            count = hist.get(turn, 0)
            bar = "#" * int(40 * count / max_count) if max_count > 0 else ""
            print(f"    T{turn:>2}: {bar} {count}")

    if losses:
        loss_turns = [r["turns"] for r in losses]
        print(f"\n  Loss turns: avg={sum(loss_turns)/len(loss_turns):.1f}  "
              f"p50={sorted(loss_turns)[len(loss_turns)//2]}")


if __name__ == "__main__":
    print("CANCEL BUILD SIMULATOR")
    print("Dig (Press/Force) = pay 2M or 2S, see 3 next turn")
    print("Play = use a card, see 1 next turn")
    print("Quick momentum = +1M, doesn't end turn (play before your real action)")

    # Baselines
    print("\n\n=== BASELINES (no quick_m) ===")
    run_scenario(cancels=[CANCEL_M, CANCEL_M], timer_count=2)
    run_scenario(cancels=[CANCEL_M, CANCEL_M, CANCEL_M], timer_count=3)
    run_scenario(cancels=[CANCEL_M, CANCEL_M, CANCEL_M], timer_count=4, stagger=True)

    # With 4 quick_momentum
    print("\n\n=== WITH 4 QUICK MOMENTUM ===")
    run_scenario(cancels=[CANCEL_M, CANCEL_M], timer_count=2, quick_m=4, trace_count=3)
    run_scenario(cancels=[CANCEL_M, CANCEL_M, CANCEL_M], timer_count=3, quick_m=4, trace_count=3)
    run_scenario(cancels=[CANCEL_M, CANCEL_M, CANCEL_M], timer_count=4, stagger=True, quick_m=4, trace_count=3)
    run_scenario(cancels=[CANCEL_M, CANCEL_M, CANCEL_M, CANCEL_M], timer_count=4, stagger=True, quick_m=4)

    # Sweep: how many quick_m matter?
    print("\n\n=== QUICK_M SWEEP (3c_m vs 3t, cd=4) ===")
    for n in range(0, 7):
        run_scenario(cancels=[CANCEL_M, CANCEL_M, CANCEL_M], timer_count=3, quick_m=n)

    print("\n\n=== QUICK_M SWEEP (3c_m vs 4t staggered, cd=4) ===")
    for n in range(0, 7):
        run_scenario(cancels=[CANCEL_M, CANCEL_M, CANCEL_M], timer_count=4, stagger=True, quick_m=n)
