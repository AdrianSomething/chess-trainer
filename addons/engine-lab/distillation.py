"""Supervised teacher imitation; one trainable network, bounded minibatches."""
import json
import random
import torch
from torch import nn
from model import Policy, encode
from evolution import atomic_save


def run(host, config, output, resume, emit, check_stop):
    rng = random.Random(config.seed)
    torch.manual_seed(config.seed)
    policy = Policy(config)
    best = output / "best.pt"
    if resume and best.exists():
        policy.net.load_state_dict(torch.load(best, map_location=policy.device, weights_only=True))
    optimizer = torch.optim.AdamW(policy.net.parameters(), lr=config.learning_rate)
    history = []
    with (output / "teacher-positions.jsonl").open("a", encoding="utf-8") as samples:
        for epoch in range(config.rounds):
            accumulated = 0.0
            for offset in range(0, config.distillation_positions, min(32, config.batch_size)):
                states, targets, values = [], [], []
                for _ in range(min(32, config.batch_size, config.distillation_positions - offset)):
                    check_stop()
                    request = dict(op="reset", id="distill", seed=rng.randrange(2**30), randomPlies=rng.randint(6, 80))
                    if config.position_source == "fen":
                        request.update(fen=config.fen, randomPlies=0)
                    elif config.position_source == "initial":
                        request["randomPlies"] = 0
                    state = host.call(**request)
                    if state["status"] != "playing":
                        continue
                    label = host.call(op="analyze", id="distill", depth=config.distillation_depth)["lines"][0]
                    states.append(state)
                    targets.append(next(m["index"] for m in state["legal"] if m["uci"] == label["move"]))
                    value = (1 if label["mate"] > 0 else -1) if label["mate"] is not None else max(-1, min(1, label["cp"]/1000))
                    values.append(value)
                    samples.write(json.dumps(dict(fen=state["fen"], teacher=label)) + "\n")
                if not states:
                    continue
                policy.net.train()
                logits, prediction = policy.net(encode(states, policy.device))
                loss = nn.functional.cross_entropy(logits, torch.tensor(targets, device=policy.device)) + nn.functional.mse_loss(prediction, torch.tensor(values, dtype=torch.float32, device=policy.device))
                optimizer.zero_grad(set_to_none=True)
                loss.backward()
                nn.utils.clip_grad_norm_(policy.net.parameters(), 1)
                optimizer.step()
                accumulated += float(loss.detach())
                emit(event="progress", stage="distillation", completed=min(offset+len(states), config.distillation_positions), total=config.distillation_positions,
                     fen=states[-1]["fen"] if config.visualize else None)
            atomic_save(policy.cpu_weights(), best)
            history.append(dict(epoch=epoch+1, accumulated_loss=accumulated))
            emit(event="round_complete", round=epoch+1, best_score=-accumulated)
    (output / "report.json").write_text(json.dumps(history, indent=2), encoding="utf-8")
    return dict(history=history)
