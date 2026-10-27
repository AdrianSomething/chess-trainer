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
        reports=[]
        for run_index in range(config.runs):
            check_stop()
            directory=output if config.runs==1 else output/f"run-{run_index+1:03d}"
            directory.mkdir(exist_ok=True)
            run_config=dataclasses.replace(config,seed=config.seed+run_index,runs=1)
            (directory/"config.json").write_text(json.dumps(dataclasses.asdict(run_config),indent=2),encoding="utf-8")
            emit(event="run_start",run=run_index+1,runs=config.runs,directory=str(directory.resolve()))
            with Host(args.host) as host:
                if config.mode=="benchmark":
                    from model import Policy
                    from evolution import benchmark
                    report=benchmark(host,Policy(run_config),directory/"best.pt",run_config,random.Random(run_config.seed),emit,check_stop)
                    (directory/"benchmark.json").write_text(json.dumps(report,indent=2),encoding="utf-8")
                else:
                    import importlib
                    module=importlib.import_module({"evolution":"evolution","distillation":"distillation","selfplay":"selfplay","neat":"neat_training"}[config.mode])
                    report=module.run(host,run_config,directory,args.resume,emit,check_stop)
            reports.append(dict(run=run_index+1,directory=str(directory.resolve()),report=report))
            emit(event="run_complete",run=run_index+1,runs=config.runs)
        if config.runs>1:(output/"series-report.json").write_text(json.dumps(reports,indent=2),encoding="utf-8")
        emit(event="complete",report=reports[-1]["report"])
        return 0
    except Stopped:
        emit(event="stopped", message="Stopped. The last fully completed round remains resumable.")
        return 0
    except Exception as error:
        emit(event="error", message=str(error))
        return 1


if __name__ == "__main__":
    sys.exit(main())
