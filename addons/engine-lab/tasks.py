"""Round tasks and weighted fitness, independent of the setup UI."""
import dataclasses
from curves import value
from schedule import pairings
from arena import play
import tactics

def number(parameters,key,round_number,default):
    return value(parameters.get(key),round_number,default)

def default_tasks(c):
    return [dict(kind="tournament",enabled=c.peer_games>0,parameters=dict(count=c.peer_games,pairing="random",max_moves=c.max_moves)),
            dict(kind="stockfish",enabled=c.stockfish_games>0,parameters=dict(count=c.stockfish_games,depth=dict(points=[[1,c.stockfish_depth],[101,c.stockfish_depth+100/c.depth_every]]),max_moves=c.max_moves)),
            dict(kind="mate",enabled=c.tactics>0,parameters=dict(count=c.tactics,min_n=1,max_n=5,solution_reward=c.tactic_reward,source="selfplay"))]

def execute(host,policy,population,task,c,round_number,rng,emit,check_stop):
    p=task.get("parameters",{}); kind=task["kind"]; n=len(population)
    fitness=[0.0]*n; timings=[None]*n
    def num(key,default):return number(p,key,round_number,default)
    settings=dataclasses.replace(c,max_moves=max(1,int(num("max_moves",c.max_moves))),
         reward_overrides={k:p[k] for k in ("win","loss","draw","abort","move") if k in p} or None,
         stockfish_intelligence=max(0,min(100,num("intelligence",100))))
    count=max(0,int(num("count",10)))
    def matches(pairs,depth=1):return play(host,policy,population,pairs,depth,settings,rng,emit,check_stop,fitness)
    if kind=="tournament":
        if n<2:return fitness,timings
        if p.get("pairing","random")=="random":
            if count%2:raise ValueError("Random tournament game count must be even for balanced colors.")
            _,timings=matches(pairings(n,count,rng))
        else:
            losses=[0]*n;remaining=list(range(n));limit=2 if p.get("loser_tree",False) else 1
            while len(remaining)>1:
                check_stop();rng.shuffle(remaining)
                duels=list(zip(remaining[::2],remaining[1::2]));pairs=[pair for a,b in duels for pair in ((a,b),(b,a))]
                before=list(fitness);_,timings=matches(pairs)
                for a,b in duels:
                    da,db=fitness[a]-before[a],fitness[b]-before[b]
                    loser=b if da>db else a if db>da else rng.choice((a,b))
                    losses[loser]+=1
                remaining=[i for i in remaining if losses[i]<limit]
    elif kind=="stockfish":
        if count%2:raise ValueError("Stockfish game count must be even for balanced colors.")
        depth=int(num("depth",c.stockfish_depth+(round_number-1)//c.depth_every))
        pairs=[pair for i in range(n) for _ in range(count//2) for pair in ((i,-1),(-1,i))]
        if pairs:_,timings=matches(pairs,max(1,min(64,depth)))
    elif kind=="mate":
        low=max(1,int(num("min_n",1)));high=max(low,int(num("max_n",5)))
        settings=dataclasses.replace(settings,tactics=count,tactic_min=low,tactic_max=high,tactic_depth=max(c.tactic_depth,2*high+4),
            tactic_source=p.get("source","selfplay"),tactic_file=p.get("file",""),
            tactic_distribution_start=p.get("distribution_start",1),tactic_distribution_end=p.get("distribution_end",1),
            tactic_move_reward=p.get("correct_move",0),tactic_reward=num("solution_reward",20))
        if count:
            puzzles=tactics.generate(host,settings,rng,emit,check_stop)
            tactics.score(host,policy,population,puzzles,settings,emit,check_stop,fitness)
    elif kind=="teacher_tactics":
        depth=max(1,min(64,int(num("depth",10))));positions=[]
        for j in range(count):
            check_stop();state=host.call(op="reset",id="teacher-task",seed=rng.randrange(2**30),randomPlies=rng.randint(10,90))
            if state["status"]!="playing":continue
            lines=host.call(op="analyze",id="teacher-task",depth=depth,multi=len(state["legal"]))["lines"]
            def score(line):return (10000 if line["mate"]>0 else -10000) if line["mate"] is not None else line["cp"]
            best=score(lines[0]);allowed={l["move"] for l in lines if score(l)>=best-100*num("max_loss",.3)}
            positions.append((state,allowed))
        for i,weights in enumerate(population):
            check_stop();policy.load((i,id(weights)),weights)
            for state,allowed in positions:
                move,illegal=policy.choose([state])[0]
                fitness[i]+=(num("correct_move",10) if move in allowed else num("wrong_move",-1))-(c.illegal_penalty if illegal else 0)
            emit(event="progress",stage="teacher_tactics",completed=i+1,total=n)
        host.call(op="remove",id="teacher-task")
    else:raise ValueError("Unknown fitness task: "+kind)
    scale=num("weight",1)
    return [score*scale for score in fitness],timings
