"""Batches ready games by model identity to avoid switching GPU weights after every move."""
import time
from schedule import reward


def play(host, policy, population, pairs, depth, config, rng, emit, check_stop, fitness=None):
    active, results = {}, []
    policy.loaded = None
    for index, (white, black) in enumerate(pairs):
        request = dict(op="reset", id=f"match-{index}", maxMoves=config.max_moves)
        if config.position_source == "fen":
            request["fen"] = config.fen
        elif config.position_source == "generated":
            request.update(seed=rng.randrange(2**30), randomPlies=rng.randint(8, 50))
        active[request["id"]] = dict(players=(white, black), state=host.call(**request))
    last_display = 0
    thinking = [0.0] * len(population)
    decisions = [0] * len(population)
    while active:
        check_stop()
        for identity, game in list(active.items()):
            state = game["state"]
            if state["status"] == "playing":
                continue
            if fitness is not None:
                for side, player in enumerate(game["players"]):
                    if player >= 0:
                        fitness[player] += reward(state["result"], side == 0, state["plies"], state["status"], config)
            results.append(dict(white=game["players"][0], black=game["players"][1], result=state["result"],
                                plies=state["plies"], status=state["status"]))
            host.call(op="remove", id=identity)
            del active[identity]
        if not active:
            break
        ready = {}
        for identity, game in active.items():
            side = 0 if game["state"]["white"] else 1
            ready.setdefault(game["players"][side], []).append(identity)
        player = max(ready, key=lambda key: len(ready[key]))
        group = ready[player][:config.batch_size]
        states = [active[identity]["state"] for identity in group]
        if player == -1:
            moves = []
            for identity in group:
                check_stop()
                lines = host.call(op="analyze", id=identity, depth=depth)["lines"]
                if not lines or not lines[0]["move"]:
                    raise RuntimeError("Stockfish returned no legal move.")
                moves.append((lines[0]["move"], False))
        else:
            start = time.perf_counter()
            policy.load((player, id(population[player])), population[player])
            moves = policy.choose(states)
            if config.record_thinking:
                thinking[player] += time.perf_counter() - start
                decisions[player] += len(states)
        updates = host.call(op="batch", items=[dict(op="step", id=identity, move=move) for identity, (move, _) in zip(group, moves)])
        for identity, (_, illegal), update in zip(group, moves, updates):
            if not update["accepted"]:
                raise RuntimeError("Legal-mask/host mismatch; match was not scored.")
            active[identity]["state"] = update["state"]
            if player >= 0 and fitness is not None:
                fitness[player] += config.survival_reward - (config.illegal_penalty if illegal else 0)
            if config.teacher_depth and update["state"]["status"] == "playing":
                feedback = host.call(op="analyze", id=identity, depth=config.teacher_depth)
                emit(event="teacher", game=identity, fen=update["state"]["fen"], lines=feedback["lines"])
        now = time.monotonic()
        if now - last_display >= .25:
            emit(event="progress", stage="games", completed=len(results), total=len(pairs),
                 fen=updates[-1]["state"]["fen"] if config.visualize else None)
            last_display = now
    return results, [1000 * t / n if n else None for t, n in zip(thinking, decisions)]
