"""Local inference. Tensor-only neural weights or a data-only NEAT network; no imported model code."""
import argparse,json,sys
from pathlib import Path
p=argparse.ArgumentParser();p.add_argument("--weights",required=True);p.add_argument("--model",required=True);p.add_argument("--device",default="cuda");args=p.parse_args()
if args.model=="neat":
    from neat_policy import NeatPolicy,inputs,action_score
    policy=NeatPolicy(None);policy.load("inference",Path(args.weights))
else:
    import torch
    from config import Config
    from model import Policy,encode
    policy=Policy(Config(model=args.model,mode="benchmark",device=args.device,vram_gib=7.5));policy.load("inference",Path(args.weights))
for line in sys.stdin:
    try:
        request=json.loads(line);children=request["children"]
        if args.model=="neat":
            outputs=policy.network.activate(inputs(request["state"]));ordered=sorted(children,key=lambda child:action_score(outputs,child["index"]),reverse=True)
            def material(state):
                values=dict(P=100,N=320,B=330,R=500,Q=900,K=0)
                return sum(values.get(piece.upper(),0)*(1 if piece.isupper() else -1) for piece in state["board"])*(1 if state["white"] else -1)
            result=dict(ok=True,cp=material(request["state"]),lines=[dict(move=c["move"],cp=-material(c["state"])) for c in ordered])
        else:
            states=[request["state"]]+[child["state"] for child in children]
            with torch.inference_mode():logits,values=policy.net(encode(states,policy.device))
            ordered=sorted(enumerate(children),key=lambda pair:float(logits[0,pair[1]["index"]]),reverse=True)
            result=dict(ok=True,cp=int(float(values[0])*1000),lines=[dict(move=child["move"],cp=-int(float(values[i+1])*1000)) for i,child in ordered])
        print(json.dumps(result),flush=True)
    except Exception as error:print(json.dumps(dict(ok=False,error=str(error))),flush=True)
