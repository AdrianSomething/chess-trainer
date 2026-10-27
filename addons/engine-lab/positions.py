"""Shared position sources for teacher training; datasets stay local and read-only."""
import json
from pathlib import Path

class PositionSource:
    def __init__(self, parameters, rng):
        self.p=parameters;self.rng=rng;self.records=[]
        if parameters.get("source")=="file":
            text=Path(parameters.get("file","")).read_text(encoding="utf-8-sig")
            self.records=json.loads(text) if text.lstrip().startswith("[") else [json.loads(line) for line in text.splitlines() if line.strip()]
            if not self.records:raise ValueError("The position file is empty.")
    def next(self,host,identity="training-position"):
        source=self.p.get("source","generated");request=dict(op="reset",id=identity)
        if source=="file":request["fen"]=self.rng.choice(self.records)["fen"]
        elif source=="dataset":request["fen"]=host.call(op="dataset_sample",file=self.p.get("file",""),seed=self.rng.randrange(2**30),plies=self.rng.randint(8,120))["fen"]
        elif source=="fen":request["fen"]=self.p.get("fen","")
        elif source=="generated":request.update(seed=self.rng.randrange(2**30),randomPlies=self.rng.randint(6,80))
        elif source!="initial":raise ValueError("Unknown position source: "+source)
        return host.call(**request)
