"""Balanced pairings and evolutionary selection, independent of GPU and chess rules."""
import math


def pairings(population, games_per_player, rng):
    result = []
    for _ in range(games_per_player // 2):
        order = list(range(population))
        rng.shuffle(order)
        # One directed cycle: every member plays once as White and once as Black.
        result.extend((order[i], order[(i + 1) % population]) for i in range(population))
    return result


def selection(round_number, config):
    phase = (round_number - 1) % config.cycle_rounds
    ramp = min(1, phase / max(1, config.cycle_rounds - 6))
    fraction = config.min_survival + ramp * (config.max_survival - config.min_survival)
    sigma = config.high_mutation + ramp * (config.low_mutation - config.high_mutation)
    return max(1, math.ceil(config.population * fraction)), sigma


def reward(result, white, plies, status, config):
    if config.reward_overrides:
        from curves import value
        field="abort" if status=="move_limit" else "draw" if result=="1/2-1/2" else "win" if result==("1-0" if white else "0-1") else "loss"
        return value(config.reward_overrides.get(field),plies,{"abort":-15,"draw":-5,"win":100,"loss":-30}[field])
    if result == "1/2-1/2":
        return config.draw_reward - (config.limit_penalty if status == "move_limit" else 0)
    won = result == ("1-0" if white else "0-1")
    return config.win_reward - config.win_decay_per_ply * plies if won else config.loss_reward
