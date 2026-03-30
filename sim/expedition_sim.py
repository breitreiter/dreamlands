"""
Expedition survival simulator: returning from The City to Rankleburn.

Scenario: 9-day return trip, no food, no settlements.
Player has 3 severe conditions (injured, irradiated, lattice_sickness) each at 3 stacks.
Player carries 3 medical kits (one per condition type) as infinite-use pack items.

Medicine check: per-condition per-night, success removes 1 stack.
Failed check (or active severe condition not yet cleared) = "untreated" that night.
ANY untreated severe condition on a given night = -1 HP (binary).

Exhaustion: ambient resist each night, d20+8 vs DC 12.
Exhaustion drains 3 spirits/night while active. No natural cure in the field.
Disheartened at <10 spirits: disadvantage on all rolls.
No food = no spirits recovery.
"""

import random
from dataclasses import dataclass, field


@dataclass
class SimConfig:
    days: int = 9
    starting_hp: int = 4
    starting_spirits: int = 20
    disheartened_threshold: int = 10

    # Exhaustion resist: d20 + modifier vs DC
    exhaust_dc: int = 12
    exhaust_modifier: int = 8

    # Medicine check: d20 + skill vs DC
    medicine_dc: int = 8
    medicine_skill: int = 0  # varied in experiments

    # Severe conditions: each starts at 3 stacks
    severe_conditions: list[str] = field(
        default_factory=lambda: ["injured", "irradiated", "lattice_sickness"]
    )
    severe_stacks: int = 3

    # Exhaustion
    exhaustion_spirits_drain: int = 3

    # Toggle for experiments
    disheartened_enabled: bool = True
    starting_disheartened: bool = False


def roll_d20(rng: random.Random, disadvantage: bool = False) -> int:
    r1 = rng.randint(1, 20)
    if not disadvantage:
        return r1
    r2 = rng.randint(1, 20)
    return min(r1, r2)


def check_passes(natural: int, modifier: int, dc: int) -> bool:
    if natural == 1:
        return False
    if natural == 20:
        return True
    return natural + modifier >= dc


def run_trial(config: SimConfig, rng: random.Random) -> dict:
    hp = config.starting_hp
    spirits = config.starting_spirits
    exhausted = False
    disheartened = config.starting_disheartened
    if disheartened:
        spirits = min(spirits, config.disheartened_threshold - 1)

    # Track stacks per severe condition
    stacks = {c: config.severe_stacks for c in config.severe_conditions}

    medicine_modifier = config.medicine_skill

    log = []

    for day in range(1, config.days + 1):
        day_events = []

        # 1. Exhaustion resist check
        if not exhausted:
            nat = roll_d20(rng, disadvantage=disheartened)
            if check_passes(nat, config.exhaust_modifier, config.exhaust_dc):
                day_events.append(f"resist exhaust OK (rolled {nat})")
            else:
                exhausted = True
                day_events.append(f"EXHAUSTED (rolled {nat})")

        # 2. Exhaustion spirits drain
        if exhausted:
            spirits = max(0, spirits - config.exhaustion_spirits_drain)
            day_events.append(f"exhaust drain -3 spirits -> {spirits}")

        # 3. Medicine checks for active severe conditions
        any_untreated = False
        for cond in config.severe_conditions:
            if stacks[cond] <= 0:
                continue  # already cleared

            # Roll medicine check: d20 + skill vs DC, disadvantage if disheartened
            nat = roll_d20(rng, disadvantage=disheartened)
            if check_passes(nat, medicine_modifier, config.medicine_dc):
                stacks[cond] -= 1
                if stacks[cond] == 0:
                    day_events.append(f"CURED {cond}")
                else:
                    day_events.append(f"treated {cond} -> {stacks[cond]} stacks")
            else:
                any_untreated = True
                day_events.append(f"FAILED {cond} (nat {nat}+{medicine_modifier} vs DC {config.medicine_dc})")

        # Also mark untreated if any severe condition is still active but wasn't rolled
        # (shouldn't happen since we roll for all active ones, but just in case)

        # 4. HP drain: any untreated severe = -1 HP
        if any_untreated:
            hp -= 1
            day_events.append(f"HP drain -> {hp}")

        # 5. No food = no spirits recovery (skip)

        # 6. Disheartened check
        if config.disheartened_enabled:
            if spirits < config.disheartened_threshold and not disheartened:
                disheartened = True
                day_events.append("DISHEARTENED")
            elif spirits >= config.disheartened_threshold and disheartened:
                disheartened = False
                day_events.append("disheartened cleared")

        log.append((day, day_events))

        # 7. Death check
        if hp <= 0:
            return {
                "survived": False,
                "died_on_day": day,
                "hp": hp,
                "spirits": spirits,
                "stacks": dict(stacks),
                "exhausted": exhausted,
                "disheartened": disheartened,
                "log": log,
            }

    return {
        "survived": True,
        "died_on_day": None,
        "hp": hp,
        "spirits": spirits,
        "stacks": dict(stacks),
        "exhausted": exhausted,
        "disheartened": disheartened,
        "log": log,
    }


def run_experiment(config: SimConfig, trials: int = 100_000, seed: int = 42) -> dict:
    rng = random.Random(seed)
    survived = 0
    death_days = []
    final_hps = []

    for _ in range(trials):
        result = run_trial(config, rng)
        if result["survived"]:
            survived += 1
            final_hps.append(result["hp"])
        else:
            death_days.append(result["died_on_day"])

    survival_rate = survived / trials * 100
    avg_death_day = sum(death_days) / len(death_days) if death_days else 0
    avg_final_hp = sum(final_hps) / len(final_hps) if final_hps else 0

    return {
        "survival_rate": survival_rate,
        "avg_death_day": avg_death_day,
        "avg_final_hp": avg_final_hp,
        "trials": trials,
    }


def main():
    print("=" * 70)
    print("EXPEDITION SURVIVAL SIM: The City -> Rankleburn (9 days, no food)")
    print("3 severe conditions @ 3 stacks each, 3 medical kits")
    print(f"Medicine: d20 + skill vs DC {SimConfig.medicine_dc}. Exhaustion resist: d20+8 vs DC 12")
    print("=" * 70)
    print()

    # Header
    print(f"{'Skill':>6} {'Pass%':>6} {'Normal':>10} {'Start Dish.':>12} {'No Dish.':>10}")
    print("-" * 48)

    for skill in range(-2, 5):
        config_normal = SimConfig(medicine_skill=skill)
        config_start_d = SimConfig(medicine_skill=skill, starting_disheartened=True)
        config_no_d = SimConfig(medicine_skill=skill, disheartened_enabled=False)

        needed = config_dc_pass_rate(config_normal.medicine_dc, skill)

        r_normal = run_experiment(config_normal)
        r_start_d = run_experiment(config_start_d)
        r_no_d = run_experiment(config_no_d)
        print(
            f"{skill:>+5} {needed:>5}% {r_normal['survival_rate']:>9.1f}% "
            f"{r_start_d['survival_rate']:>11.1f}% {r_no_d['survival_rate']:>9.1f}%"
        )


def config_dc_pass_rate(dc: int, modifier: int) -> int:
    """Nominal d20 pass rate: P(d20 + mod >= dc), with nat1=fail, nat20=pass."""
    passes = 0
    for r in range(1, 21):
        if r == 1:
            continue  # nat 1 always fails
        elif r == 20:
            passes += 1  # nat 20 always passes
        elif r + modifier >= dc:
            passes += 1
    return passes * 5  # out of 20 = percentage


if __name__ == "__main__":
    main()
