"""Stockfish policy/value imitation with scheduled tasks and resumable AdamW state."""
import json,random,time
import torch
from torch import nn
from curves import value
from model import Policy,encode
from positions import PositionSource
from gradient_checkpoint import optimizer_for,restore,save


def run(host,config,output,resume,emit,check_stop):
    rng=random.Random(config.seed);torch.manual_seed(config.seed);policy=Policy(config)
    optimizer=optimizer_for(policy,config.learning_rate);first,history=restore(policy,optimizer,output,resume,rng)
    enabled=[t for t in (config.tasks if config.tasks is not None else [dict(kind="distillation",parameters={})]) if t.get("enabled",True)]
    if not enabled or any(t["kind"]!="distillation" for t in enabled):raise ValueError("Enable at least one imitation task.")
    with (output/"teacher-positions.jsonl").open("a",encoding="utf-8") as samples:
        for round_number in range(first,first+config.rounds):
            check_stop();started=time.perf_counter();loss_total=0.;examples=0
            emit(event="round_start",round=round_number,total=first+config.rounds-1)
            for index,task in enumerate(enabled):
                p=task.get("parameters",{});num=lambda key,default:value(p.get(key),round_number,default)
                count=max(1,int(num("count",config.distillation_positions)));batch=max(1,min(128,int(num("batch_size",config.batch_size))))
                depth=max(1,min(64,int(num("depth",config.distillation_depth))));source=PositionSource(dict(source=config.position_source,fen=config.fen)|p,rng)
                for group in optimizer.param_groups:group["lr"]=num("learning_rate",config.learning_rate)
                emit(event="task_start",task=task.get("name","Stockfish imitation"),task_index=index+1,tasks=len(enabled))
                completed=0;attempts=0
                while completed<count:
                    states=[];targets=[];values=[]
                    while len(states)<min(batch,count-completed):
                        check_stop();attempts+=1
                        if attempts>count*50:raise ValueError("Position source produced too many terminal positions.")
                        state=source.next(host)
                        if state["status"]!="playing":continue
                        lines=host.call(op="analyze",id="training-position",depth=depth)["lines"]
                        if not lines:raise RuntimeError("The teacher returned no legal move.")
                        label=lines[0];states.append(state);targets.append(next(m["index"] for m in state["legal"] if m["uci"]==label["move"]))
                        values.append((1 if label["mate"]>0 else -1) if label["mate"] is not None else max(-1,min(1,label["cp"]/1000)))
                        samples.write(json.dumps(dict(fen=state["fen"],teacher=label))+"\n")
                    policy.net.train();optimizer.zero_grad(set_to_none=True)
                    logits,prediction=policy.net(encode(states,policy.device))
                    loss=nn.functional.cross_entropy(logits,torch.tensor(targets,device=policy.device))+nn.functional.mse_loss(prediction,torch.tensor(values,dtype=torch.float32,device=policy.device))
                    loss.backward();nn.utils.clip_grad_norm_(policy.net.parameters(),1,foreach=False);optimizer.step()
                    loss_total+=float(loss.detach())*len(states);examples+=len(states);completed+=len(states)
                    emit(event="progress",stage="distillation",completed=completed,total=count,fen=states[-1]["fen"] if config.visualize else None)
            score=-loss_total/max(1,examples);row=dict(round=round_number,best_engine=1,best_score=score,mean_score=score,loss=-score,examples=examples,seconds=time.perf_counter()-started)
            history.append(row);save(policy,optimizer,output,round_number,history,rng)
            (output/"history.json").write_text(json.dumps(history,indent=2),encoding="utf-8");emit(event="round_complete",**row)
    # Release gradients and Adam state before running the independent result measurements.
    optimizer.zero_grad(set_to_none=True);del optimizer
    from result_tasks import run as results
    report=dict(history=history,results=results(host,policy,config,output,rng,emit,check_stop),model=config.model,score_definition="Negative mean policy cross-entropy plus value MSE; higher is better.")
    (output/"report.json").write_text(json.dumps(report,indent=2),encoding="utf-8");return report
