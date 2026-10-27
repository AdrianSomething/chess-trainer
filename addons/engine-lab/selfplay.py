"""Minimal on-policy actor-critic self-play; no search or teacher targets are used."""
import json,random,time
import torch
from torch import nn
from curves import value
from model import Policy,encode
from gradient_checkpoint import optimizer_for,restore,save


def run(host,config,output,resume,emit,check_stop):
    rng=random.Random(config.seed);torch.manual_seed(config.seed);policy=Policy(config)
    optimizer=optimizer_for(policy,config.learning_rate);first,history=restore(policy,optimizer,output,resume,rng)
    enabled=[t for t in (config.tasks if config.tasks is not None else [dict(kind="selfplay",parameters={})]) if t.get("enabled",True)]
    if not enabled or any(t["kind"]!="selfplay" for t in enabled):raise ValueError("Enable a self-play task.")
    for round_number in range(first,first+config.rounds):
        started=time.perf_counter();returns=[];losses=[];finished=0;games=0
        emit(event="round_start",round=round_number,total=first+config.rounds-1)
        for task_index,task in enumerate(enabled):
            p=task.get("parameters",{});num=lambda key,default:value(p.get(key),round_number,default)
            count=max(1,int(num("count",4)));cap=max(1,int(num("max_moves",100)));gamma=max(0,min(1,num("discount",.99)))
            batch=max(1,min(128,int(num("batch_size",16))));entropy_weight=max(0,num("entropy",.01));illegal_weight=max(0,num("illegal_weight",.001))
            for group in optimizer.param_groups:group["lr"]=num("learning_rate",.0003)
            emit(event="task_start",task=task.get("name","Actor-critic self-play"),task_index=task_index+1,tasks=len(enabled))
            for game in range(count):
                check_stop();state=host.call(op="reset",id="rl",maxMoves=cap);trajectory=[];policy.net.eval()
                while state["status"]=="playing":
                    check_stop()
                    with torch.inference_mode():
                        logits,_=policy.net(encode([state],policy.device));indices=torch.tensor([m["index"] for m in state["legal"]],device=policy.device)
                        selected=int(torch.distributions.Categorical(logits=logits[0,indices]).sample())
                    chosen=state["legal"][selected];trajectory.append((state,chosen["index"]))
                    update=host.call(op="step",id="rl",move=chosen["uci"])
                    if not update["accepted"]:raise RuntimeError("Self-play legal-mask mismatch.")
                    state=update["state"]
                    if config.visualize and len(trajectory)%8==0:emit(event="progress",stage="selfplay",completed=game,total=count,fen=state["fen"])
                games+=1;finished+=int(state["status"]!="move_limit")
                winner=1 if state["result"]=="1-0" else -1 if state["result"]=="0-1" else 0
                samples=[]
                for i,(position,action) in enumerate(trajectory):
                    terminal=(-.05 if state["status"]=="move_limit" else winner*(1 if position["white"] else -1))
                    target=terminal*gamma**(len(trajectory)-i-1);samples.append((position,action,target));returns.append(target)
                rng.shuffle(samples);policy.net.train()
                for offset in range(0,len(samples),batch):
                    check_stop();part=samples[offset:offset+batch];optimizer.zero_grad(set_to_none=True)
                    logits,prediction=policy.net(encode([s[0] for s in part],policy.device));targets=torch.tensor([s[2] for s in part],device=policy.device,dtype=torch.float32)
                    policies=[];entropies=[];invalid=[]
                    for j,(position,action,_) in enumerate(part):
                        ids=torch.tensor([m["index"] for m in position["legal"]],device=policy.device);legal=logits[j,ids]
                        distribution=torch.distributions.Categorical(logits=legal);selected=next(k for k,m in enumerate(position["legal"]) if m["index"]==action)
                        policies.append(distribution.log_prob(torch.tensor(selected,device=policy.device)));entropies.append(distribution.entropy())
                        invalid.append(torch.logsumexp(logits[j],0)-torch.logsumexp(legal,0))
                    advantage=targets-prediction.detach()
                    loss=-(torch.stack(policies)*advantage).mean()+.5*nn.functional.mse_loss(prediction,targets)-entropy_weight*torch.stack(entropies).mean()+illegal_weight*torch.stack(invalid).mean()
                    loss.backward();nn.utils.clip_grad_norm_(policy.net.parameters(),1,foreach=False);optimizer.step();losses.append(float(loss.detach()))
                host.call(op="remove",id="rl");emit(event="progress",stage="selfplay",completed=game+1,total=count,fen=state["fen"] if config.visualize else None)
        score=sum(returns)/max(1,len(returns));row=dict(round=round_number,best_engine=1,best_score=score,mean_score=score,loss=sum(losses)/max(1,len(losses)),games=games,finished_games=finished,seconds=time.perf_counter()-started)
        history.append(row);save(policy,optimizer,output,round_number,history,rng);(output/"history.json").write_text(json.dumps(history,indent=2),encoding="utf-8");emit(event="round_complete",**row)
    optimizer.zero_grad(set_to_none=True);del optimizer
    from result_tasks import run as results
    report=dict(history=history,results=results(host,policy,config,output,rng,emit,check_stop),model=config.model,score_definition="Mean discounted terminal return from each acting side. Self-play returns are not an independent strength estimate.")
    (output/"report.json").write_text(json.dumps(report,indent=2),encoding="utf-8");return report
