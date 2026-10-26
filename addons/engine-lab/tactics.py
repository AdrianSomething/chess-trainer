"""Fresh shared mate tasks from varied Stockfish self-play; never substituted with invented solutions."""


def generate(host, config, rng, emit, check_stop):
    puzzles, seen = [], set()
    state = host.call(op="reset", id="tactic-source", seed=rng.randrange(2**30), randomPlies=12)
    for attempt in range(config.tactic_attempts):
        if len(puzzles) >= config.tactics:
            break
        check_stop()
        if state["status"] != "playing" or state["plies"] > 160:
            state = host.call(op="reset", id="tactic-source", seed=rng.randrange(2**30), randomPlies=rng.randint(6, 30))
            continue
        lines = host.call(op="analyze", id="tactic-source", depth=config.tactic_depth if attempt % 4 == 0 else 4, multi=3)["lines"]
        if not lines:
            raise RuntimeError("Stockfish could not analyze a generated position.")
        if lines[0]["mate"] is not None and 1 <= lines[0]["mate"] <= 5:
            key = " ".join(state["fen"].split()[:4])
            if key not in seen:
                confirmed = host.call(op="analyze", id="tactic-source", depth=config.tactic_depth)["lines"][0]
                if confirmed["mate"] is not None and 1 <= confirmed["mate"] <= 5:
                    seen.add(key)
                    puzzles.append(dict(fen=state["fen"], mate=confirmed["mate"]))
                    emit(event="progress", stage="tactics", completed=len(puzzles), total=config.tactics,
                         fen=state["fen"] if config.visualize else None)
                    state = host.call(op="reset", id="tactic-source", seed=rng.randrange(2**30), randomPlies=rng.randint(6, 30))
                    continue
        # Mostly sound play, with lower-ranked replies to produce realistic tactical mistakes.
        line = lines[0] if rng.random() < .65 else rng.choice(lines)
        state = host.call(op="step", id="tactic-source", move=line["move"])["state"]
    host.call(op="remove", id="tactic-source")
    if len(puzzles) != config.tactics:
        raise RuntimeError(f"Only {len(puzzles)}/{config.tactics} verified mate tasks found within the search budget. Increase tactic_attempts and resume; this round was not ranked.")
    return puzzles


def score(host, policy, population, puzzles, config, emit, check_stop, fitness):
    cache = {}
    def analysis(state):
        key = state["fen"]
        if key not in cache:
            cache[key] = host.call(op="analyze", fen=key, depth=config.tactic_depth)["lines"][0]
        return cache[key]
    for player, weights in enumerate(population):
        policy.load((player, id(weights)), weights)
        for puzzle in puzzles:
            check_stop()
            state = host.call(op="reset", id="solve", fen=puzzle["fen"])
            remaining = puzzle["mate"]
            while remaining > 0 and state["status"] == "playing":
                move, illegal = policy.choose([state])[0]
                fitness[player] -= config.illegal_penalty if illegal else 0
                state = host.call(op="step", id="solve", move=move)["state"]
                remaining -= 1
                if state["status"] == "checkmate":
                    fitness[player] += config.tactic_reward
                    break
                if state["status"] != "playing":
                    break
                reply = analysis(state)
                if reply["mate"] is None or reply["mate"] >= 0 or -reply["mate"] > remaining:
                    break
                state = host.call(op="step", id="solve", move=reply["move"])["state"]
            host.call(op="remove", id="solve")
        emit(event="progress", stage="tactic_evaluation", completed=player + 1, total=len(population))
