import dataclasses
import json
import random
from pathlib import Path
import torch
from model import ChessNet, Policy
from schedule import pairings, selection
from arena import play
import tactics


def atomic_save(value, path):
    temporary = path.with_suffix(path.suffix + ".tmp")
    torch.save(value, temporary)
    temporary.replace(path)


def benchmark(host, policy, best, config, rng, emit, check_stop):
    rows = []
    settings = dataclasses.replace(config, position_source="initial", teacher_depth=0)
    for depth in range(1, config.benchmark_max_depth + 1):
        check_stop()
        games = [(0, -1), (-1, 0)] * (config.benchmark_games // 2)
        results, timings = play(host, policy, [best], games, depth, settings, rng, emit, check_stop)
        wins = sum(r["result"] == ("1-0" if r["white"] == 0 else "0-1") for r in results)
        capped = sum(r["status"] == "move_limit" for r in results)
        draws = sum(r["result"] == "1/2-1/2" and r["status"] != "move_limit" for r in results)
        valid = len(results) - capped
        row = dict(depth=depth, wins=wins, draws=draws, losses=valid-wins-draws, capped=capped,
                   score=(wins + draws * .5) / valid if valid else None, mean_thinking_ms=timings[0])
        rows.append(row)
        emit(event="benchmark", **row)
        if row["score"] is None or row["score"] < .5:
            break
    return rows


def run(host, config, output, resume, emit, check_stop):
    rng = random.Random(config.seed)
    torch.manual_seed(config.seed)
    policy = Policy(config)
    checkpoint = output / "checkpoint.pt"
    history, first = [], 1
    if resume and checkpoint.exists():
        saved = torch.load(checkpoint, map_location="cpu", weights_only=True)
        population, first, history = saved["population"], saved["next_round"], saved["history"]
        if len(population) != config.population:
            raise ValueError("Resume requires the same population size.")
        rng.setstate(saved["random_state"])
        torch.set_rng_state(saved["torch_state"])
        best = saved["best"]
    else:
        population = [ChessNet().state_dict() for _ in range(config.population)]
        best = population[0]
    # Save before the first expensive round as well, so a cancelled first round can restart.
    def save(next_round):
        atomic_save(dict(population=population, next_round=next_round, history=history, best=best,
                         random_state=rng.getstate(), torch_state=torch.get_rng_state(), config=dataclasses.asdict(config)), checkpoint)
        atomic_save(best, output / "best.pt")
    save(first)
    for round_number in range(first, first + config.rounds):
        check_stop()
        fitness = [0.0] * config.population
        emit(event="round_start", round=round_number, total=first + config.rounds - 1)
        puzzles = tactics.generate(host, config, rng, emit, check_stop) if config.tactics else []
        games = pairings(config.population, config.peer_games, rng)
        for player in range(config.population):
            games.extend([(player, -1), (-1, player)] * (config.stockfish_games // 2))
        depth = min(64, config.stockfish_depth + (round_number - 1) // config.depth_every)
        results, timings = play(host, policy, population, games, depth, config, rng, emit, check_stop, fitness)
        if puzzles:
            tactics.score(host, policy, population, puzzles, config, emit, check_stop, fitness)
        ranked = sorted(range(config.population), key=lambda i: fitness[i], reverse=True)
        best = {name: value.clone() for name, value in population[ranked[0]].items()}
        count, sigma = selection(round_number, config)
        history.append(dict(round=round_number, best_score=fitness[ranked[0]], mean_score=sum(fitness)/len(fitness),
                            survivors=count, mutation=sigma, stockfish_depth=depth, games=len(results),
                            mean_thinking_ms=timings[ranked[0]], mate_tasks=len(puzzles)))
        survivors = [population[i] for i in ranked[:count]]
        offspring = list(survivors)
        while len(offspring) < config.population:
            parent = rng.choice(survivors)
            offspring.append({name: value + torch.randn_like(value) * max(.002, float(value.std(unbiased=False))) * sigma
                              for name, value in parent.items()})
        population = offspring
        save(round_number + 1)
        (output / "history.json").write_text(json.dumps(history, indent=2), encoding="utf-8")
        emit(event="round_complete", **history[-1])
    rows = benchmark(host, policy, best, config, rng, emit, check_stop)
    report = dict(history=history, benchmark=rows, note="Depth comparison under these settings; not an Elo rating.")
    (output / "report.json").write_text(json.dumps(report, indent=2), encoding="utf-8")
    return report
