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
    from curves import value, blend_distribution
    from population import create_member,check_space,retain_only
    import tasks, shutil
    rng=random.Random(config.seed);torch.manual_seed(config.seed)
    policy=Policy(config);checkpoint=output/"checkpoint.pt";members=output/"members";members.mkdir(exist_ok=True)
    history=[];first=1;next_id=1;best_id=None
    schedules=config.schedules or {}
    def count_at(round_number):return max(2,min(1000,int(value(schedules.get("population"),round_number,config.population))))
    if resume and checkpoint.exists():
        saved=torch.load(checkpoint,map_location="cpu",weights_only=True)
        first,history=saved["next_round"],saved["history"]
        rng.setstate(saved["random_state"]);torch.set_rng_state(saved["torch_state"])
        if "members" in saved:
            identities=saved["identities"];population=[members/name for name in saved["members"]];next_id=saved["next_id"];best_id=saved.get("best_id")
            if saved.get("model","compact")!=config.model:raise ValueError("Resume requires the original model architecture.")
        else:
            # Import a 0.27.0 compact checkpoint once, without retaining its whole population afterward.
            identities=list(range(1,len(saved["population"])+1));population=[]
            for identity,weights in zip(identities,saved["population"]):
                path=members/f"engine-{identity:08d}.pt";atomic_save(weights,path);population.append(path)
            next_id=len(population)+1
        del saved
    else:
        count=count_at(1);check_space(output,config.model,count)
        identities=list(range(1,count+1));population=[];next_id=count+1
        for identity in identities:
            check_stop();population.append(create_member(members,identity,config.model))
            emit(event="progress",stage="initialization",completed=len(population),total=count)
    def copy_best(path):
        temporary=output/"best.tmp";shutil.copyfile(path,temporary);temporary.replace(output/"best.pt")
    def save(next_round):
        atomic_save(dict(members=[p.name for p in population],identities=identities,next_id=next_id,best_id=best_id,model=config.model,
            next_round=next_round,history=history,random_state=rng.getstate(),torch_state=torch.get_rng_state(),config=dataclasses.asdict(config)),checkpoint)
    if not (output/"best.pt").exists():copy_best(population[0])
    save(first)
    selected_tasks=config.tasks if config.tasks is not None else tasks.default_tasks(config)
    enabled=[task for task in selected_tasks if task.get("enabled",True)]
    if not enabled:raise ValueError("Enable at least one fitness task.")
    for round_number in range(first,first+config.rounds):
        check_stop();fitness=[0.0]*len(population);timings=[None]*len(population)
        emit(event="round_start",round=round_number,total=first+config.rounds-1,best_engine=best_id,best_score=history[-1]["best_score"] if history else None)
        round_config=dataclasses.replace(config,engine_ids=identities,best_engine=best_id,round_fraction=(round_number-first)/max(1,config.rounds-1))
        for task_index,task in enumerate(enabled):
            emit(event="task_start",task=task.get("name",task["kind"]),task_index=task_index+1,tasks=len(enabled))
            scores,measured=tasks.execute(host,policy,population,task,round_config,round_number,rng,emit,check_stop)
            fitness=[a+b for a,b in zip(fitness,scores)]
            timings=[new if new is not None else old for new,old in zip(measured,timings)]
        ranked=sorted(range(len(population)),key=lambda i:fitness[i],reverse=True)
        best_id=identities[ranked[0]];copy_best(population[ranked[0]])
        default_count,default_sigma=selection(round_number,dataclasses.replace(config,population=len(population)))
        fraction=value(schedules.get("survivors"),round_number,default_count/len(population))
        sigma=max(0,min(1,value(schedules.get("mutation"),round_number,default_sigma)))
        target=count_at(round_number+1);count=max(1,min(len(population),target,__import__("math").ceil(len(population)*fraction)))
        history.append(dict(round=round_number,best_engine=best_id,best_score=fitness[ranked[0]],mean_score=sum(fitness)/len(fitness),survivors=count,mutation=sigma,population=len(population),mean_thinking_ms=timings[ranked[0]]))
        winners=ranked[:count];following=[population[i] for i in winners];following_ids=[identities[i] for i in winners]
        check_space(output,config.model,target-count)
        duplicate=[blend_distribution(schedules.get("duplication_start",1),schedules.get("duplication_end",1),round_config.round_fraction,i/max(1,count-1)) for i in range(count)]
        if sum(duplicate)<=0:raise ValueError("Duplication distribution must have positive weight.")
        while len(following)<target:
            check_stop();parent=rng.choices(following[:count],weights=duplicate,k=1)[0]
            following.append(create_member(members,next_id,config.model,parent,sigma));following_ids.append(next_id);next_id+=1
            emit(event="progress",stage="selection_mutation",completed=len(following),total=target)
        population,identities=following,following_ids;save(round_number+1);retain_only(members,population)
        (output/"history.json").write_text(json.dumps(history,indent=2),encoding="utf-8")
        emit(event="round_complete",**history[-1])
    from result_tasks import run as run_results
    reports=run_results(host,policy,config,output,rng,emit,check_stop,best_id,first+config.rounds-1)
    report=dict(history=history,results=reports,best_engine=best_id,model=config.model,note="Experimental depth/task comparison, not an Elo rating.")
    (output/"report.json").write_text(json.dumps(report,indent=2),encoding="utf-8")
    return report
