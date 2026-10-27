"""Portable NEAT inference with a factorized 64-source/64-target/5-promotion policy."""
import json,math
from pathlib import Path
PIECES="PNBRQK"

def inputs(state):
    return [(PIECES.index(p.upper())+1)/6*(1 if p.isupper() else -1) if p.upper() in PIECES else 0 for p in state["board"]]+[int(state["white"])]+[int(c in state["castling"]) for c in "KQkq"]

def action_score(outputs,index):
    move,promotion=divmod(index,5);source,target=divmod(move,64)
    return outputs[source]+outputs[64+target]+outputs[128+promotion]

class PortableNetwork:
    def __init__(self,data):
        if data.get("format")!="cot-neat-v1" or len(data["inputs"])!=69 or len(data["outputs"])!=133:raise ValueError("Unsupported NEAT model.")
        self.data=data
    def activate(self,x):
        values=dict(zip(self.data["inputs"],x))|{key:0. for key in self.data["outputs"]}
        for node,bias,response,links in self.data["nodes"]:
            value=bias+response*sum(values.get(source,0)*weight for source,weight in links)
            values[node]=math.tanh(max(-60,min(60,2.5*value)))
        return [values.get(key,0.) for key in self.data["outputs"]]

def export(network,path):
    data=dict(format="cot-neat-v1",inputs=network.input_nodes,outputs=network.output_nodes,
              nodes=[[node,bias,response,links] for node,activation,aggregate,bias,response,links in network.node_evals])
    temporary=path.with_suffix(".tmp");temporary.write_text(json.dumps(data,separators=(",",":")),encoding="utf-8");temporary.replace(path)

class NeatPolicy:
    def __init__(self,config):self.config=config;self.loaded=None;self.network=None
    def load(self,identity,weights):
        if identity==self.loaded:return
        if isinstance(weights,(str,Path)):self.network=PortableNetwork(json.loads(Path(weights).read_text(encoding="utf-8")))
        else:
            import neat
            self.network=neat.nn.FeedForwardNetwork.create(weights,self.config)
        self.loaded=identity
    def choose(self,states):
        chosen=[]
        for state in states:
            outputs=self.network.activate(inputs(state));legal=state["legal"]
            move=max(legal,key=lambda m:action_score(outputs,m["index"]))
            source=max(range(64),key=lambda i:outputs[i]);target=max(range(64),key=lambda i:outputs[i+64]);promotion=max(range(5),key=lambda i:outputs[i+128]);preferred=(source*64+target)*5+promotion
            chosen.append((move["uci"],preferred not in {m["index"] for m in legal}))
        return chosen
