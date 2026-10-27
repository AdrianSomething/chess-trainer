"""Optional post-training measurements; depth comparisons are not Elo estimates."""
import dataclasses
from curves import value
from evolution import benchmark
import tasks

def run(host,policy,config,output,rng,emit,check_stop,best_id=1,round_number=1):
    best_file=output/("best-neat.json" if config.model=="neat" else "best.pt")
    result_tasks=config.results if config.results is not None else [dict(kind="benchmark",enabled=True,parameters={})]
    reports=[]
    for task in result_tasks:
        if not task.get("enabled",True):continue
        emit(event="task_start",task=task.get("name",task["kind"]))
        parameters=task.get("parameters",{})
        if task["kind"]=="benchmark":
            settings=dataclasses.replace(config,benchmark_max_depth=max(1,int(value(parameters.get("max_depth"),1,config.benchmark_max_depth))),benchmark_games=max(2,int(value(parameters.get("count"),1,config.benchmark_games))),stockfish_intelligence=value(parameters.get("intelligence"),1,100),engine_ids=[best_id])
            reports.append(dict(kind="benchmark",rows=benchmark(host,policy,best_file,settings,rng,emit,check_stop)))
        elif task["kind"] in ("mate","teacher_tactics"):
            scores,timing=tasks.execute(host,policy,[best_file],task,dataclasses.replace(config,engine_ids=[best_id]),round_number,rng,emit,check_stop)
            reports.append(dict(kind=task["kind"],scores=scores,timing=timing))
        elif task["kind"]=="speed":
            import time
            state=host.call(op="reset",id="speed");policy.load("speed",best_file);policy.choose([state])
            count=max(1,int(value(parameters.get("count"),1,100)));start= time.perf_counter()
            for _ in range(count):check_stop();policy.choose([state])
            reports.append(dict(kind="speed",decisions=count,mean_ms=1000*(time.perf_counter()-start)/count))
            host.call(op="remove",id="speed")
    return reports
