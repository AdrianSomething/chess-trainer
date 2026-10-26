"""Optional command-line entry point. See README.md before starting a long experiment."""
import argparse
import dataclasses
import json
import random
import sys
from pathlib import Path
from config import Config
from client import Host


class Stopped(Exception):
    pass


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--host", required=True)
    parser.add_argument("--output", required=True)
    parser.add_argument("--config")
    parser.add_argument("--resume", action="store_true")
    parser.add_argument("--smoke", action="store_true")
    args = parser.parse_args()
    config = Config(**json.loads(Path(args.config).read_text(encoding="utf-8-sig"))) if args.config else Config()
    if args.smoke:
        config = dataclasses.replace(config, population=4, rounds=1, peer_games=2, stockfish_games=2, tactics=0,
                                     max_moves=2, benchmark_games=2, benchmark_max_depth=1)
    config.validate()
    output = Path(args.output)
    output.mkdir(parents=True, exist_ok=True)
    def emit(**event):
        print(json.dumps(event, separators=(",", ":")), flush=True)
    def check_stop():
        if (output / "stop.request").exists():
            raise Stopped()
    (output / "config.json").write_text(json.dumps(dataclasses.asdict(config), indent=2), encoding="utf-8")
    try:
        with Host(args.host) as host:
            # Evolution also ends with a Stockfish benchmark, even when round games omit it.
            if not host.has_teacher:
                raise RuntimeError("Stockfish is missing from the application package.")
            if config.mode == "distillation":
                import distillation
                report = distillation.run(host, config, output, args.resume, emit, check_stop)
            elif config.mode == "benchmark":
                import torch
                from model import Policy
                from evolution import benchmark
                best = torch.load(output / "best.pt", map_location="cpu", weights_only=True)
                report = benchmark(host, Policy(config), best, config, random.Random(config.seed), emit, check_stop)
                (output / "benchmark.json").write_text(json.dumps(report, indent=2), encoding="utf-8")
            else:
                import evolution
                report = evolution.run(host, config, output, args.resume, emit, check_stop)
        emit(event="complete", report=report)
        return 0
    except Stopped:
        emit(event="stopped", message="Stopped. The last fully completed round remains resumable.")
        return 0
    except Exception as error:
        emit(event="error", message=str(error))
        return 1


if __name__ == "__main__":
    sys.exit(main())
